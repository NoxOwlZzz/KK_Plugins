using System;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorCubemapSourceFormat
    {
        Png,
        RadianceHdr
    }

    internal sealed class MaterialEditorCubemapSourceInfo
    {
        internal MaterialEditorCubemapSourceInfo(
            MaterialEditorCubemapSourceFormat format,
            int width,
            int height,
            MaterialEditorRadianceHdrHeader radianceHeader)
        {
            Format = format;
            Width = width;
            Height = height;
            RadianceHeader = radianceHeader;
        }

        internal MaterialEditorCubemapSourceFormat Format { get; private set; }

        internal int Width { get; private set; }

        internal int Height { get; private set; }

        internal bool IsHighDynamicRange
        {
            get { return Format == MaterialEditorCubemapSourceFormat.RadianceHdr; }
        }

        internal MaterialEditorRadianceHdrHeader RadianceHeader { get; private set; }
    }

    /// <summary>
    /// Content-based inspection for supported equirectangular Cubemap sources.
    /// File extensions are deliberately not trusted because persisted edits
    /// retain the original encoded bytes rather than a path or format tag.
    /// </summary>
    internal static class MaterialEditorCubemapSourceParser
    {
        internal static bool TryInspect(
            byte[] data,
            out MaterialEditorCubemapSourceInfo info,
            out string warning,
            out string error)
        {
            info = null;
            warning = null;
            error = null;
            if (data == null || data.Length == 0)
            {
                error = "The selected Cubemap source contains no data.";
                return false;
            }
            if (!MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                    data.LongLength,
                    out error))
                return false;

            if (HasPngSignature(data))
            {
                int width;
                int height;
                if (!MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                        data,
                        out width,
                        out height,
                        out warning,
                        out error))
                    return false;
                info = new MaterialEditorCubemapSourceInfo(
                    MaterialEditorCubemapSourceFormat.Png,
                    width,
                    height,
                    null);
                return true;
            }

            if (MaterialEditorRadianceHdrDecoder.HasSignature(data))
            {
                MaterialEditorRadianceHdrHeader header;
                string headerWarning;
                if (!MaterialEditorRadianceHdrDecoder.TryReadHeader(
                        data,
                        out header,
                        out headerWarning,
                        out error))
                    return false;

                string dimensionsWarning;
                if (!MaterialEditorCubemapProjection.TryValidateEquirectangularDimensions(
                        header.Width,
                        header.Height,
                        "Radiance HDR",
                        out dimensionsWarning,
                        out error))
                    return false;

                warning = CombineWarnings(headerWarning, dimensionsWarning);
                info = new MaterialEditorCubemapSourceInfo(
                    MaterialEditorCubemapSourceFormat.RadianceHdr,
                    header.Width,
                    header.Height,
                    header);
                return true;
            }

            error = "Cubemap import supports PNG and Radiance RGBE (.hdr) images only.";
            return false;
        }

        private static bool HasPngSignature(byte[] data)
        {
            return data.Length >= 8
                   && data[0] == 0x89
                   && data[1] == 0x50
                   && data[2] == 0x4E
                   && data[3] == 0x47
                   && data[4] == 0x0D
                   && data[5] == 0x0A
                   && data[6] == 0x1A
                   && data[7] == 0x0A;
        }

        private static string CombineWarnings(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
                return second;
            return string.IsNullOrEmpty(second) ? first : first + " " + second;
        }
    }
}
