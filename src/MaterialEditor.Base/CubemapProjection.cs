using System;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorCubemapFace
    {
        PositiveX = 0,
        NegativeX = 1,
        PositiveY = 2,
        NegativeY = 3,
        PositiveZ = 4,
        NegativeZ = 5
    }

    internal static class MaterialEditorCubemapProjection
    {
        private const double TwoPi = Math.PI * 2.0;
        internal const int MaximumSourceDimension = 8192;
        internal const long MaximumSourcePixels = 4096L * 2048L;
        internal const long MaximumSourceFileBytes = 64L * 1024L * 1024L;

        internal static bool TryReadPngEquirectangularSize(
            byte[] data,
            out int width,
            out int height,
            out string error)
        {
            string warning;
            return TryReadPngEquirectangularSize(
                data,
                out width,
                out height,
                out warning,
                out error);
        }

        internal static bool TryReadPngEquirectangularSize(
            byte[] data,
            out int width,
            out int height,
            out string warning,
            out string error)
        {
            width = 0;
            height = 0;
            warning = null;
            error = null;
            if (data == null || data.Length < 24)
            {
                error = "The selected file is not a valid PNG image.";
                return false;
            }
            if (!TryValidateSourceFileLength(data.LongLength, out error))
                return false;

            var signature = new byte[]
            {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
            };
            for (var index = 0; index < signature.Length; index++)
            {
                if (data[index] == signature[index])
                    continue;
                error = "Cubemap import currently supports PNG images only.";
                return false;
            }

            if (data[12] != (byte)'I'
                || data[13] != (byte)'H'
                || data[14] != (byte)'D'
                || data[15] != (byte)'R')
            {
                error = "The selected PNG does not contain a valid IHDR block.";
                return false;
            }

            width = ReadBigEndianInt32(data, 16);
            height = ReadBigEndianInt32(data, 20);
            return TryValidateEquirectangularDimensions(
                width,
                height,
                "PNG",
                out warning,
                out error);
        }

        internal static bool TryValidateEquirectangularDimensions(
            int width,
            int height,
            string sourceName,
            out string warning,
            out string error)
        {
            warning = null;
            error = null;
            var displayName = string.IsNullOrEmpty(sourceName)
                ? "Cubemap source"
                : sourceName + " Cubemap source";
            if (width <= 0 || height <= 0)
            {
                error = "The selected " + displayName + " has invalid dimensions.";
                return false;
            }
            if (width > MaximumSourceDimension
                || height > MaximumSourceDimension
                || (long)width * height > MaximumSourcePixels)
            {
                error = "Cubemap import supports " + displayName
                        + " images with at most " + MaximumSourcePixels
                        + " decoded pixels and no dimension above "
                        + MaximumSourceDimension
                        + " pixels, to keep runtime conversion memory bounded.";
                return false;
            }

            int normalizedWidth;
            int normalizedHeight;
            GetNormalizedEquirectangularSize(
                width,
                height,
                out normalizedWidth,
                out normalizedHeight);
            if (width != normalizedWidth || height != normalizedHeight)
            {
                warning = (long)width == (long)height * 2L
                    ? "Cubemap source " + width + "x" + height
                      + " uses a 2:1 aspect ratio, but its dimensions do not divide evenly into Cubemap faces. "
                      + "The full image will be resampled to "
                      + normalizedWidth + "x" + normalizedHeight + "."
                    : "Cubemap source " + width + "x" + height
                      + " does not use the preferred 2:1 aspect ratio. "
                      + "The full image will be stretched and resampled to "
                      + normalizedWidth + "x" + normalizedHeight + ".";
            }

            return true;
        }

        internal static bool TryValidateSourceFileLength(
            long fileLength,
            out string error)
        {
            error = null;
            if (fileLength < 0)
            {
                error = "The selected Cubemap source has an invalid file length.";
                return false;
            }
            if (fileLength <= MaximumSourceFileBytes)
                return true;

            error = "Cubemap import supports source files up to 64 MiB to keep runtime memory bounded.";
            return false;
        }

        internal static void GetNormalizedEquirectangularSize(
            int sourceWidth,
            int sourceHeight,
            out int width,
            out int height)
        {
            // A Cubemap face consumes one quarter of the panorama width and
            // one half of its height. Use the largest face supported by both
            // source axes so normalization never invents resolution merely to
            // satisfy the 2:1 layout. Very small valid PNGs still produce the
            // minimum native Cubemap size instead of being rejected by shape.
            var faceSize = Math.Max(
                1,
                Math.Min(sourceWidth / 4, sourceHeight / 2));
            width = faceSize * 4;
            height = faceSize * 2;
        }

        internal static void DirectionForFaceUv(
            MaterialEditorCubemapFace face,
            double u,
            double v,
            out double x,
            out double y,
            out double z)
        {
            var horizontal = u * 2.0 - 1.0;
            var vertical = v * 2.0 - 1.0;
            switch (face)
            {
                case MaterialEditorCubemapFace.PositiveX:
                    x = 1.0;
                    y = vertical;
                    z = -horizontal;
                    break;
                case MaterialEditorCubemapFace.NegativeX:
                    x = -1.0;
                    y = vertical;
                    z = horizontal;
                    break;
                case MaterialEditorCubemapFace.PositiveY:
                    x = horizontal;
                    y = 1.0;
                    z = -vertical;
                    break;
                case MaterialEditorCubemapFace.NegativeY:
                    x = horizontal;
                    y = -1.0;
                    z = vertical;
                    break;
                case MaterialEditorCubemapFace.PositiveZ:
                    x = horizontal;
                    y = vertical;
                    z = 1.0;
                    break;
                default:
                    x = -horizontal;
                    y = vertical;
                    z = -1.0;
                    break;
            }

            Normalize(ref x, ref y, ref z);
        }

        internal static void EquirectangularUvForDirection(
            double x,
            double y,
            double z,
            out double u,
            out double v)
        {
            Normalize(ref x, ref y, ref z);
            u = 0.5 + Math.Atan2(z, x) / TwoPi;
            u -= Math.Floor(u);
            v = 0.5 + Math.Asin(Clamp(y, -1.0, 1.0)) / Math.PI;
            v = Clamp(v, 0.0, 1.0);
        }

        internal static void DirectionForEquirectangularUv(
            double u,
            double v,
            out double x,
            out double y,
            out double z)
        {
            var longitude = (u - 0.5) * TwoPi;
            var latitude = (v - 0.5) * Math.PI;
            var latitudeCosine = Math.Cos(latitude);
            x = latitudeCosine * Math.Cos(longitude);
            y = Math.Sin(latitude);
            z = latitudeCosine * Math.Sin(longitude);
        }

        internal static void FaceUvForDirection(
            double x,
            double y,
            double z,
            out MaterialEditorCubemapFace face,
            out double u,
            out double v)
        {
            var absoluteX = Math.Abs(x);
            var absoluteY = Math.Abs(y);
            var absoluteZ = Math.Abs(z);
            double horizontal;
            double vertical;

            if (absoluteX >= absoluteY && absoluteX >= absoluteZ)
            {
                if (x >= 0.0)
                {
                    face = MaterialEditorCubemapFace.PositiveX;
                    horizontal = -z / absoluteX;
                }
                else
                {
                    face = MaterialEditorCubemapFace.NegativeX;
                    horizontal = z / absoluteX;
                }
                vertical = y / absoluteX;
            }
            else if (absoluteY >= absoluteX && absoluteY >= absoluteZ)
            {
                if (y >= 0.0)
                {
                    face = MaterialEditorCubemapFace.PositiveY;
                    horizontal = x / absoluteY;
                    vertical = -z / absoluteY;
                }
                else
                {
                    face = MaterialEditorCubemapFace.NegativeY;
                    horizontal = x / absoluteY;
                    vertical = z / absoluteY;
                }
            }
            else
            {
                if (z >= 0.0)
                {
                    face = MaterialEditorCubemapFace.PositiveZ;
                    horizontal = x / absoluteZ;
                }
                else
                {
                    face = MaterialEditorCubemapFace.NegativeZ;
                    horizontal = -x / absoluteZ;
                }
                vertical = y / absoluteZ;
            }

            u = Clamp(horizontal * 0.5 + 0.5, 0.0, 1.0);
            v = Clamp(vertical * 0.5 + 0.5, 0.0, 1.0);
        }

        private static int ReadBigEndianInt32(byte[] data, int offset)
        {
            return (data[offset] << 24)
                   | (data[offset + 1] << 16)
                   | (data[offset + 2] << 8)
                   | data[offset + 3];
        }

        private static void Normalize(ref double x, ref double y, ref double z)
        {
            var magnitude = Math.Sqrt(x * x + y * y + z * z);
            if (magnitude <= double.Epsilon)
                return;
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
                return minimum;
            return value > maximum ? maximum : value;
        }
    }
}
