using System;
using System.Globalization;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorRadianceHdrHeader
    {
        internal int Width;
        internal int Height;
        internal int DataOffset;
        internal char FirstAxis;
        internal bool FirstAxisPositive;
        internal int FirstLength;
        internal char SecondAxis;
        internal bool SecondAxisPositive;
        internal int SecondLength;
        internal bool HasFormat;
        internal double Exposure = 1.0;
        internal double Gamma = 1.0;
    }

    /// <summary>
    /// Incremental, Unity-free Radiance RGBE decoder. Decoded pixels remain in
    /// packed RGBE form (four bytes per pixel) and are reordered to Unity's
    /// canonical bottom-left origin. Keeping RGBE packed avoids a 16-byte Color
    /// allocation for every source pixel while retaining values above one.
    /// </summary>
    internal sealed class MaterialEditorRadianceHdrDecodeOperation : IDisposable
    {
        private byte[] _data;
        private readonly MaterialEditorRadianceHdrHeader _header;
        private byte[] _pixels;
        private byte[] _scanline;
        private int _offset;
        private int _scanlineIndex;
        private readonly byte[] _oldPrevious = new byte[4];
        private bool _oldHasPrevious;
        private int _oldRunShift;
        private bool _disposed;

        private MaterialEditorRadianceHdrDecodeOperation(
            byte[] data,
            MaterialEditorRadianceHdrHeader header)
        {
            _data = data;
            _header = header;
            _offset = header.DataOffset;
            _pixels = new byte[checked(header.Width * header.Height * 4)];
            _scanline = new byte[checked(header.SecondLength * 4)];
        }

        internal int CompletedScanlines
        {
            get { return _scanlineIndex; }
        }

        internal int TotalScanlines
        {
            get { return _header.FirstLength; }
        }

        internal bool IsComplete
        {
            get { return _scanlineIndex >= _header.FirstLength; }
        }

        internal static bool TryBegin(
            byte[] data,
            MaterialEditorRadianceHdrHeader header,
            out MaterialEditorRadianceHdrDecodeOperation operation,
            out string error)
        {
            operation = null;
            error = null;
            if (data == null || header == null)
            {
                error = "The Radiance HDR decoder received no source information.";
                return false;
            }
            if (header.DataOffset < 0 || header.DataOffset >= data.Length)
            {
                error = "The Radiance HDR source contains no pixel data.";
                return false;
            }

            try
            {
                operation = new MaterialEditorRadianceHdrDecodeOperation(data, header);
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not allocate the Radiance HDR decode buffer: "
                        + exception.Message;
                return false;
            }
        }

        internal bool ProcessScanlines(int maximumScanlines, out string error)
        {
            error = null;
            if (_disposed)
            {
                error = "The Radiance HDR decode operation has been disposed.";
                return false;
            }
            if (maximumScanlines <= 0)
            {
                error = "The Radiance HDR scanline budget must be positive.";
                return false;
            }

            var processed = 0;
            while (!IsComplete && processed < maximumScanlines)
            {
                // Radiance readers determine the encoding per scanline. Mixed
                // old/new files are unusual but valid and cost nothing to
                // support here because detection only peeks at four bytes.
                var modernRle = DetectModernRle(out error);
                if (!string.IsNullOrEmpty(error))
                    return false;

                var decoded = modernRle
                    ? DecodeModernScanline(out error)
                    : DecodeOldScanline(out error);
                if (!decoded)
                    return false;

                CopyScanlineToCanonicalImage();
                _scanlineIndex++;
                processed++;
            }
            return true;
        }

        internal byte[] TakePixels()
        {
            if (_disposed || !IsComplete)
                return null;
            var pixels = _pixels;
            _pixels = null;
            return pixels;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _data = null;
            _pixels = null;
            _scanline = null;
        }

        private bool DetectModernRle(out string error)
        {
            error = null;
            if (_header.SecondLength < 8 || _header.SecondLength > 0x7fff)
                return false;
            if (_offset > _data.Length - 4)
            {
                error = "The Radiance HDR pixel data is truncated.";
                return false;
            }
            if (_data[_offset] != 2 || _data[_offset + 1] != 2
                || (_data[_offset + 2] & 0x80) != 0)
                return false;

            var declaredLength = (_data[_offset + 2] << 8) | _data[_offset + 3];
            if (declaredLength != _header.SecondLength)
            {
                error = "The Radiance HDR RLE scanline length does not match its resolution.";
                return false;
            }
            return true;
        }

        private bool DecodeModernScanline(out string error)
        {
            error = null;
            if (_offset > _data.Length - 4
                || _data[_offset] != 2
                || _data[_offset + 1] != 2
                || (_data[_offset + 2] & 0x80) != 0
                || ((_data[_offset + 2] << 8) | _data[_offset + 3])
                   != _header.SecondLength)
            {
                error = "The Radiance HDR source contains an invalid RLE scanline marker.";
                return false;
            }
            _offset += 4;

            for (var channel = 0; channel < 4; channel++)
            {
                var pixel = 0;
                while (pixel < _header.SecondLength)
                {
                    if (_offset >= _data.Length)
                    {
                        error = "The Radiance HDR RLE scanline is truncated.";
                        return false;
                    }
                    var code = _data[_offset++];
                    if (code == 0)
                    {
                        error = "The Radiance HDR RLE scanline contains a zero-length packet.";
                        return false;
                    }

                    if (code > 128)
                    {
                        var count = code - 128;
                        if (pixel > _header.SecondLength - count
                            || _offset >= _data.Length)
                        {
                            error = "The Radiance HDR RLE run exceeds its scanline.";
                            return false;
                        }
                        var value = _data[_offset++];
                        for (var index = 0; index < count; index++)
                            _scanline[(pixel++ * 4) + channel] = value;
                    }
                    else
                    {
                        var count = code;
                        if (pixel > _header.SecondLength - count
                            || _offset > _data.Length - count)
                        {
                            error = "The Radiance HDR RLE literal exceeds or truncates its scanline.";
                            return false;
                        }
                        for (var index = 0; index < count; index++)
                            _scanline[(pixel++ * 4) + channel] = _data[_offset++];
                    }
                }
            }
            return true;
        }

        private bool DecodeOldScanline(out string error)
        {
            error = null;
            _oldHasPrevious = false;
            _oldRunShift = 0;
            var pixel = 0;
            while (pixel < _header.SecondLength)
            {
                if (_offset > _data.Length - 4)
                {
                    error = "The Radiance HDR flat pixel data is truncated.";
                    return false;
                }

                var red = _data[_offset++];
                var green = _data[_offset++];
                var blue = _data[_offset++];
                var exponent = _data[_offset++];
                if (red == 1 && green == 1 && blue == 1)
                {
                    if (!_oldHasPrevious)
                    {
                        error = "The Radiance HDR old-RLE stream starts with a repeat marker.";
                        return false;
                    }
                    if (_oldRunShift > 24)
                    {
                        error = "The Radiance HDR old-RLE repeat counter overflowed.";
                        return false;
                    }
                    var count = ((long)exponent) << _oldRunShift;
                    // A zero low byte is valid in the legacy multi-byte
                    // repeat representation (for example 0, then 1 encodes
                    // 256). The consumed marker still advances the shift.
                    if (count > _header.SecondLength - pixel)
                    {
                        error = "The Radiance HDR old-RLE repeat exceeds its scanline.";
                        return false;
                    }
                    for (var index = 0L; index < count; index++)
                        CopyOldPrevious(pixel++);
                    _oldRunShift += 8;
                    continue;
                }

                _oldPrevious[0] = red;
                _oldPrevious[1] = green;
                _oldPrevious[2] = blue;
                _oldPrevious[3] = exponent;
                _oldHasPrevious = true;
                _oldRunShift = 0;
                CopyOldPrevious(pixel++);
            }
            return true;
        }

        private void CopyOldPrevious(int pixel)
        {
            var target = pixel * 4;
            _scanline[target] = _oldPrevious[0];
            _scanline[target + 1] = _oldPrevious[1];
            _scanline[target + 2] = _oldPrevious[2];
            _scanline[target + 3] = _oldPrevious[3];
        }

        private void CopyScanlineToCanonicalImage()
        {
            for (var secondIndex = 0;
                 secondIndex < _header.SecondLength;
                 secondIndex++)
            {
                var x = 0;
                var y = 0;
                SetAxisCoordinate(
                    _header.FirstAxis,
                    _header.FirstAxisPositive,
                    _scanlineIndex,
                    ref x,
                    ref y);
                SetAxisCoordinate(
                    _header.SecondAxis,
                    _header.SecondAxisPositive,
                    secondIndex,
                    ref x,
                    ref y);
                var source = secondIndex * 4;
                var target = (y * _header.Width + x) * 4;
                _pixels[target] = _scanline[source];
                _pixels[target + 1] = _scanline[source + 1];
                _pixels[target + 2] = _scanline[source + 2];
                _pixels[target + 3] = _scanline[source + 3];
            }
        }

        private void SetAxisCoordinate(
            char axis,
            bool positive,
            int index,
            ref int x,
            ref int y)
        {
            if (axis == 'X')
                x = positive ? index : _header.Width - 1 - index;
            else
                y = positive ? index : _header.Height - 1 - index;
        }
    }

    internal static class MaterialEditorRadianceHdrDecoder
    {
        private const int MaximumHeaderBytes = 64 * 1024;
        private const int MaximumHeaderLineBytes = 4096;
        private static readonly float[] ExponentScales = CreateExponentScales();

        internal static bool HasSignature(byte[] data)
        {
            return StartsWithAscii(data, "#?RADIANCE")
                   || StartsWithAscii(data, "#?RGBE");
        }

        internal static bool TryReadHeader(
            byte[] data,
            out MaterialEditorRadianceHdrHeader header,
            out string warning,
            out string error)
        {
            header = null;
            warning = null;
            error = null;
            if (!HasSignature(data))
            {
                error = "The selected file is not a Radiance RGBE image.";
                return false;
            }

            var offset = 0;
            string line;
            if (!TryReadLine(data, ref offset, out line, out error))
                return false;
            if (!string.Equals(line, "#?RADIANCE", StringComparison.Ordinal)
                && !string.Equals(line, "#?RGBE", StringComparison.Ordinal))
            {
                error = "The Radiance HDR identifier is invalid.";
                return false;
            }

            var parsed = new MaterialEditorRadianceHdrHeader();
            string format = null;
            var foundHeaderEnd = false;
            while (offset < data.Length && offset <= MaximumHeaderBytes)
            {
                if (!TryReadLine(data, ref offset, out line, out error))
                    return false;
                if (line.Length == 0)
                {
                    foundHeaderEnd = true;
                    break;
                }
                if (line[0] == '#')
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;
                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                if (string.Equals(key, "FORMAT", StringComparison.OrdinalIgnoreCase))
                {
                    if (format != null)
                    {
                        error = "The Radiance HDR header contains more than one FORMAT declaration.";
                        return false;
                    }
                    format = value;
                    parsed.HasFormat = true;
                }
                else if (string.Equals(key, "EXPOSURE", StringComparison.OrdinalIgnoreCase))
                {
                    double exposure;
                    if (!TryReadPositiveFinite(value, out exposure))
                    {
                        error = "The Radiance HDR header contains an invalid EXPOSURE value.";
                        return false;
                    }
                    parsed.Exposure *= exposure;
                    if (double.IsNaN(parsed.Exposure)
                        || double.IsInfinity(parsed.Exposure))
                    {
                        error = "The Radiance HDR cumulative EXPOSURE value overflowed.";
                        return false;
                    }
                }
                else if (string.Equals(key, "GAMMA", StringComparison.OrdinalIgnoreCase))
                {
                    double gamma;
                    if (!TryReadPositiveFinite(value, out gamma))
                    {
                        error = "The Radiance HDR header contains an invalid GAMMA value.";
                        return false;
                    }
                    parsed.Gamma = gamma;
                }
            }

            if (!foundHeaderEnd)
            {
                error = "The Radiance HDR header is missing its blank-line terminator or is too large.";
                return false;
            }
            if (format != null
                && !string.Equals(
                    format,
                    "32-bit_rle_rgbe",
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "Radiance HDR import supports FORMAT=32-bit_rle_rgbe only.";
                return false;
            }
            if (format == null)
                warning = "The Radiance HDR source has no FORMAT declaration; RGBE was assumed for legacy compatibility.";

            if (!TryReadLine(data, ref offset, out line, out error))
            {
                error = "The Radiance HDR source is missing its resolution line.";
                return false;
            }
            if (!TryParseResolution(line, parsed, out error))
                return false;
            parsed.DataOffset = offset;
            if (parsed.DataOffset >= data.Length)
            {
                error = "The Radiance HDR source contains no pixel data.";
                return false;
            }

            header = parsed;
            return true;
        }

        internal static void DecodeRgbe(
            byte red,
            byte green,
            byte blue,
            byte exponent,
            out float decodedRed,
            out float decodedGreen,
            out float decodedBlue)
        {
            if (exponent == 0)
            {
                decodedRed = 0f;
                decodedGreen = 0f;
                decodedBlue = 0f;
                return;
            }
            var scale = ExponentScales[exponent];
            decodedRed = (red + 0.5f) * scale;
            decodedGreen = (green + 0.5f) * scale;
            decodedBlue = (blue + 0.5f) * scale;
        }

        private static bool TryParseResolution(
            string line,
            MaterialEditorRadianceHdrHeader header,
            out string error)
        {
            error = null;
            var tokens = line.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 4)
            {
                error = "The Radiance HDR resolution line is invalid.";
                return false;
            }

            char firstAxis;
            bool firstPositive;
            char secondAxis;
            bool secondPositive;
            int firstLength;
            int secondLength;
            if (!TryParseAxis(tokens[0], out firstAxis, out firstPositive)
                || !int.TryParse(
                    tokens[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out firstLength)
                || !TryParseAxis(tokens[2], out secondAxis, out secondPositive)
                || !int.TryParse(
                    tokens[3],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out secondLength)
                || firstAxis == secondAxis
                || firstLength <= 0
                || secondLength <= 0)
            {
                error = "The Radiance HDR resolution line is invalid.";
                return false;
            }

            header.FirstAxis = firstAxis;
            header.FirstAxisPositive = firstPositive;
            header.FirstLength = firstLength;
            header.SecondAxis = secondAxis;
            header.SecondAxisPositive = secondPositive;
            header.SecondLength = secondLength;
            header.Width = firstAxis == 'X' ? firstLength : secondLength;
            header.Height = firstAxis == 'Y' ? firstLength : secondLength;
            return true;
        }

        private static bool TryParseAxis(
            string value,
            out char axis,
            out bool positive)
        {
            axis = '\0';
            positive = false;
            if (value == null || value.Length != 2
                || (value[0] != '+' && value[0] != '-'))
                return false;
            axis = char.ToUpperInvariant(value[1]);
            if (axis != 'X' && axis != 'Y')
                return false;
            positive = value[0] == '+';
            return true;
        }

        private static bool TryReadLine(
            byte[] data,
            ref int offset,
            out string line,
            out string error)
        {
            line = null;
            error = null;
            if (data == null || offset < 0 || offset >= data.Length)
            {
                error = "The Radiance HDR header is truncated.";
                return false;
            }
            var start = offset;
            while (offset < data.Length && data[offset] != (byte)'\n')
            {
                if (offset - start >= MaximumHeaderLineBytes
                    || offset >= MaximumHeaderBytes)
                {
                    error = "The Radiance HDR header contains an oversized line.";
                    return false;
                }
                offset++;
            }
            if (offset >= data.Length)
            {
                error = "The Radiance HDR header is truncated.";
                return false;
            }
            var end = offset;
            offset++;
            if (end > start && data[end - 1] == (byte)'\r')
                end--;
            var characters = new char[end - start];
            for (var index = 0; index < characters.Length; index++)
            {
                var value = data[start + index];
                if (value == 0 || value > 0x7f)
                {
                    error = "The Radiance HDR header contains non-ASCII data.";
                    return false;
                }
                characters[index] = (char)value;
            }
            line = new string(characters);
            return true;
        }

        private static bool TryReadPositiveFinite(string value, out double result)
        {
            return double.TryParse(
                       value,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out result)
                   && result > 0.0
                   && !double.IsNaN(result)
                   && !double.IsInfinity(result);
        }

        private static bool StartsWithAscii(byte[] data, string value)
        {
            if (data == null || value == null || data.Length < value.Length)
                return false;
            for (var index = 0; index < value.Length; index++)
                if (data[index] != (byte)value[index])
                    return false;
            return true;
        }

        private static float[] CreateExponentScales()
        {
            var scales = new float[256];
            for (var exponent = 1; exponent < scales.Length; exponent++)
                scales[exponent] = (float)Math.Pow(2.0, exponent - 136);
            return scales;
        }
    }
}
