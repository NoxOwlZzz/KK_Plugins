using MaterialEditorAPI;
using System.Text;

internal static class RadianceHdrDecoderTests
{
    internal static void Run()
    {
        AcceptsSupportedSignaturesAndLineEndings();
        AcceptsLegacyHeadersWithoutFormat();
        DecodesModernMixedRlePackets();
        DecodesMixedScanlineEncodings();
        DecodesFlatAndLegacyRlePixels();
        DecodesLegacyMultiByteRepeats();
        RejectsLegacyRepeatsAcrossScanlines();
        ReordersStandardAndXFirstOrientations();
        PreservesLinearHdrValuesAboveOne();
        RejectsMalformedHeadersAndUnsupportedXyze();
        RejectsMalformedAndTruncatedRleData();
        RejectsSourcesOutsideSharedLimits();
        EveryTruncationOfAValidRleImageFailsCleanly();
    }

    private static void AcceptsSupportedSignaturesAndLineEndings()
    {
        foreach (var signature in new[] { "#?RADIANCE", "#?RGBE" })
        foreach (var lineEnding in new[] { "\n", "\r\n" })
        {
            var source = BuildHdr(
                signature,
                lineEnding,
                "-Y 2 +X 4",
                Repeat(Pixel(10, 20, 30, 128), 8),
                "FORMAT=32-bit_rle_rgbe",
                "EXPOSURE=2",
                "EXPOSURE=0.5",
                "GAMMA=2.2");

            MaterialEditorCubemapSourceInfo info;
            string warning;
            string error;
            True(
                MaterialEditorCubemapSourceParser.TryInspect(
                    source,
                    out info,
                    out warning,
                    out error),
                signature + " " + Escape(lineEnding) + ": " + error);
            Equal(MaterialEditorCubemapSourceFormat.RadianceHdr, info.Format, "source format");
            Equal(4, info.Width, "header width");
            Equal(2, info.Height, "header height");
            True(info.IsHighDynamicRange, "HDR source flag");
            Near(1.0, info.RadianceHeader.Exposure, 1e-12, "cumulative exposure");
            Near(2.2, info.RadianceHeader.Gamma, 1e-12, "gamma metadata");
            Equal(null, warning, "canonical 4x2 source warning");
        }
    }

    private static void AcceptsLegacyHeadersWithoutFormat()
    {
        var source = BuildHdr(
            "#?RADIANCE",
            "\n",
            "-Y 2 +X 4",
            Repeat(Pixel(1, 2, 3, 129), 8));

        MaterialEditorCubemapSourceInfo info;
        string warning;
        string error;
        True(
            MaterialEditorCubemapSourceParser.TryInspect(
                source,
                out info,
                out warning,
                out error),
            "legacy header without FORMAT: " + error);
        True(
            warning != null
            && warning.IndexOf("no FORMAT", StringComparison.OrdinalIgnoreCase) >= 0,
            "legacy header reports its compatibility assumption");
        Equal(false, info.RadianceHeader.HasFormat, "legacy header format flag");
    }

    private static void DecodesModernMixedRlePackets()
    {
        var body = new byte[]
        {
            2, 2, 0, 8,
            4, 10, 20, 30, 40, 132, 50,
            131, 7, 5, 8, 9, 10, 11, 12,
            8, 1, 2, 3, 4, 5, 6, 7, 8,
            136, 129
        };
        var pixels = Decode(BuildHdr(
            "#?RADIANCE",
            "\n",
            "-Y 1 +X 8",
            body,
            "FORMAT=32-bit_rle_rgbe"));

        var red = new byte[] { 10, 20, 30, 40, 50, 50, 50, 50 };
        var green = new byte[] { 7, 7, 7, 8, 9, 10, 11, 12 };
        for (var index = 0; index < 8; index++)
            AssertPixel(pixels, index, red[index], green[index], (byte)(index + 1), 129);
    }

    private static void DecodesMixedScanlineEncodings()
    {
        var top = Pixel(10, 20, 30, 132);
        var bottom = Pixel(40, 50, 60, 133);
        var modernTopScanline = new byte[]
        {
            2, 2, 0, 8,
            136, top[0],
            136, top[1],
            136, top[2],
            136, top[3]
        };
        var source = BuildHdr(
            "#?RADIANCE",
            "\n",
            "-Y 2 +X 8",
            Join(modernTopScanline, Repeat(bottom, 8)),
            "FORMAT=32-bit_rle_rgbe");

        BytesEqual(
            Join(Repeat(bottom, 8), Repeat(top, 8)),
            Decode(source),
            "modern and flat scanlines may coexist in one Radiance image");
    }

    private static void DecodesFlatAndLegacyRlePixels()
    {
        var first = Pixel(12, 34, 56, 129);
        var second = Pixel(78, 90, 12, 130);
        var third = Pixel(23, 45, 67, 131);
        BytesEqual(
            Join(first, second, third),
            Decode(BuildHdr(
                "#?RGBE",
                "\n",
                "-Y 1 +X 3",
                Join(first, second, third),
                "FORMAT=32-bit_rle_rgbe")),
            "flat RGBE pixels");

        var repeated = Decode(BuildHdr(
            "#?RADIANCE",
            "\n",
            "-Y 1 +X 5",
            Join(first, new byte[] { 1, 1, 1, 4 }),
            "FORMAT=32-bit_rle_rgbe"));
        BytesEqual(Repeat(first, 5), repeated, "legacy single-byte repeat");
    }

    private static void DecodesLegacyMultiByteRepeats()
    {
        var pixel = Pixel(64, 32, 16, 132);
        var source = BuildHdr(
            "#?RADIANCE",
            "\n",
            "-Y 1 +X 257",
            Join(
                pixel,
                new byte[] { 1, 1, 1, 0 },
                new byte[] { 1, 1, 1, 1 }),
            "FORMAT=32-bit_rle_rgbe");

        BytesEqual(Repeat(pixel, 257), Decode(source), "legacy 256-pixel repeat");
    }

    private static void RejectsLegacyRepeatsAcrossScanlines()
    {
        var first = Pixel(10, 20, 30, 129);
        var second = Pixel(40, 50, 60, 130);

        RejectDecode(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 2 +X 2",
                Join(first, second, new byte[] { 1, 1, 1, 2 }),
                "FORMAT=32-bit_rle_rgbe"),
            "starts with a repeat marker",
            "legacy repeat state is reset at every scanline");

        RejectDecode(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 2 +X 2",
                Join(
                    first,
                    new byte[] { 1, 1, 1, 2 },
                    second,
                    second),
                "FORMAT=32-bit_rle_rgbe"),
            "exceeds its scanline",
            "legacy repeats cannot cross a scanline boundary");
    }

    private static void ReordersStandardAndXFirstOrientations()
    {
        var bottomLeft = Pixel(1, 10, 20, 128);
        var bottomRight = Pixel(2, 20, 30, 129);
        var topLeft = Pixel(3, 30, 40, 130);
        var topRight = Pixel(4, 40, 50, 131);
        var canonical = Join(bottomLeft, bottomRight, topLeft, topRight);

        var standard = Decode(BuildHdr(
            "#?RADIANCE",
            "\n",
            "-Y 2 +X 2",
            Join(topLeft, topRight, bottomLeft, bottomRight),
            "FORMAT=32-bit_rle_rgbe"));
        BytesEqual(canonical, standard, "standard -Y +X orientation");

        var xFirst = Decode(BuildHdr(
            "#?RADIANCE",
            "\n",
            "+X 2 +Y 2",
            Join(bottomLeft, topLeft, bottomRight, topRight),
            "FORMAT=32-bit_rle_rgbe"));
        BytesEqual(canonical, xFirst, "X-first +X +Y orientation");
    }

    private static void PreservesLinearHdrValuesAboveOne()
    {
        float red;
        float green;
        float blue;
        MaterialEditorRadianceHdrDecoder.DecodeRgbe(
            255,
            128,
            64,
            130,
            out red,
            out green,
            out blue);

        Near(255.5 / 64.0, red, 1e-6, "HDR red channel");
        Near(128.5 / 64.0, green, 1e-6, "HDR green channel");
        Near(64.5 / 64.0, blue, 1e-6, "HDR blue channel");
        True(red > 1f && green > 1f && blue > 1f, "RGBE decode remains HDR");

        MaterialEditorRadianceHdrDecoder.DecodeRgbe(255, 255, 255, 0, out red, out green, out blue);
        Equal(0f, red, "zero exponent red");
        Equal(0f, green, "zero exponent green");
        Equal(0f, blue, "zero exponent blue");
    }

    private static void RejectsMalformedHeadersAndUnsupportedXyze()
    {
        RejectHeader(
            BuildHdr(
                "#?NOT_RADIANCE",
                "\n",
                "-Y 1 +X 1",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_rgbe"),
            "not a Radiance",
            "invalid signature");
        RejectHeader(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 1 +X 1",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_rgbe",
                "FORMAT=32-bit_rle_rgbe"),
            "more than one FORMAT",
            "duplicate FORMAT");
        RejectHeader(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 1 +X 1",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_xyze"),
            "32-bit_rle_rgbe only",
            "XYZE is unsupported");
        RejectHeader(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 1 +X 1",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_rgbe",
                "EXPOSURE=0"),
            "invalid EXPOSURE",
            "non-positive exposure");
        RejectHeader(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 1 +X 1",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_rgbe",
                "GAMMA=NaN"),
            "invalid GAMMA",
            "non-finite gamma");
        RejectHeader(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 1 +Y 1",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_rgbe"),
            "resolution line is invalid",
            "duplicate resolution axes");

        var oversizedLine = Join(
            Encoding.ASCII.GetBytes(
                "#?RADIANCE\n#" + new string('A', 4096) + "\n\n-Y 1 +X 1\n"),
            Pixel(1, 2, 3, 128));
        RejectHeader(oversizedLine, "oversized line", "bounded header line");
    }

    private static void RejectsMalformedAndTruncatedRleData()
    {
        RejectDecode(
            RleSource(new byte[] { 2, 2, 0, 7, 136, 1 }),
            "length does not match",
            "wrong modern RLE scanline length");
        RejectDecode(
            RleSource(new byte[] { 2, 2, 0, 8, 0 }),
            "zero-length packet",
            "zero modern RLE packet");
        RejectDecode(
            RleSource(new byte[] { 2, 2, 0, 8, 137, 1 }),
            "run exceeds",
            "modern RLE run overrun");
        RejectDecode(
            RleSource(new byte[] { 2, 2, 0, 8, 136, 20 }),
            "truncated",
            "truncated modern RLE channels");
        RejectDecode(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 1 +X 3",
                new byte[] { 1, 1, 1, 2 },
                "FORMAT=32-bit_rle_rgbe"),
            "starts with a repeat marker",
            "old RLE stream cannot start with a repeat");
    }

    private static void RejectsSourcesOutsideSharedLimits()
    {
        RejectInspect(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 4096 +X 4096",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_rgbe"),
            "decoded pixels",
            "shared decoded-pixel limit");
        RejectInspect(
            BuildHdr(
                "#?RADIANCE",
                "\n",
                "-Y 1 +X 8193",
                Pixel(1, 2, 3, 128),
                "FORMAT=32-bit_rle_rgbe"),
            "no dimension above",
            "shared dimension limit");

        string error;
        True(
            !MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                MaterialEditorCubemapProjection.MaximumSourceFileBytes + 1,
                out error),
            "shared encoded-size limit");
        Contains(error, "64 MiB", "shared encoded-size error");
    }

    private static void EveryTruncationOfAValidRleImageFailsCleanly()
    {
        var valid = RleSource(new byte[]
        {
            2, 2, 0, 8,
            136, 10,
            136, 20,
            136, 30,
            136, 129
        });
        MaterialEditorRadianceHdrHeader header;
        string warning;
        string error;
        True(
            MaterialEditorRadianceHdrDecoder.TryReadHeader(
                valid,
                out header,
                out warning,
                out error),
            "valid truncation fixture header: " + error);

        for (var length = header.DataOffset; length < valid.Length; length++)
        {
            var truncated = new byte[length];
            Buffer.BlockCopy(valid, 0, truncated, 0, length);
            RejectDecode(truncated, null, "truncation at byte " + length);
        }
        BytesEqual(Repeat(Pixel(10, 20, 30, 129), 8), Decode(valid), "untruncated RLE fixture");
    }

    private static byte[] RleSource(byte[] body)
    {
        return BuildHdr(
            "#?RADIANCE",
            "\n",
            "-Y 1 +X 8",
            body,
            "FORMAT=32-bit_rle_rgbe");
    }

    private static byte[] BuildHdr(
        string signature,
        string lineEnding,
        string resolution,
        byte[] body,
        params string[] headerFields)
    {
        var header = new StringBuilder();
        header.Append(signature).Append(lineEnding);
        foreach (var field in headerFields)
            header.Append(field).Append(lineEnding);
        header.Append(lineEnding);
        header.Append(resolution).Append(lineEnding);
        return Join(Encoding.ASCII.GetBytes(header.ToString()), body);
    }

    private static byte[] Decode(byte[] source)
    {
        MaterialEditorRadianceHdrHeader header;
        string warning;
        string error;
        if (!MaterialEditorRadianceHdrDecoder.TryReadHeader(
                source,
                out header,
                out warning,
                out error))
            throw new InvalidOperationException("HDR header failed: " + error);

        MaterialEditorRadianceHdrDecodeOperation operation;
        if (!MaterialEditorRadianceHdrDecodeOperation.TryBegin(
                source,
                header,
                out operation,
                out error))
            throw new InvalidOperationException("HDR decode setup failed: " + error);

        using (operation)
        {
            while (!operation.IsComplete)
                if (!operation.ProcessScanlines(3, out error))
                    throw new InvalidOperationException("HDR decode failed: " + error);
            var pixels = operation.TakePixels();
            if (pixels == null)
                throw new InvalidOperationException("Completed HDR decode returned no pixels.");
            return pixels;
        }
    }

    private static void RejectHeader(byte[] source, string expectedError, string name)
    {
        MaterialEditorRadianceHdrHeader header;
        string warning;
        string error;
        False(
            MaterialEditorRadianceHdrDecoder.TryReadHeader(
                source,
                out header,
                out warning,
                out error),
            name + " unexpectedly parsed");
        Contains(error, expectedError, name + " error");
    }

    private static void RejectInspect(byte[] source, string expectedError, string name)
    {
        MaterialEditorCubemapSourceInfo info;
        string warning;
        string error;
        False(
            MaterialEditorCubemapSourceParser.TryInspect(
                source,
                out info,
                out warning,
                out error),
            name + " unexpectedly inspected");
        Contains(error, expectedError, name + " error");
    }

    private static void RejectDecode(byte[] source, string expectedError, string name)
    {
        MaterialEditorRadianceHdrHeader header;
        string warning;
        string error;
        if (!MaterialEditorRadianceHdrDecoder.TryReadHeader(
                source,
                out header,
                out warning,
                out error))
        {
            Contains(error, expectedError, name + " header error");
            return;
        }

        MaterialEditorRadianceHdrDecodeOperation operation;
        if (!MaterialEditorRadianceHdrDecodeOperation.TryBegin(
                source,
                header,
                out operation,
                out error))
        {
            Contains(error, expectedError, name + " setup error");
            return;
        }

        using (operation)
        {
            while (!operation.IsComplete)
            {
                if (!operation.ProcessScanlines(2, out error))
                {
                    Contains(error, expectedError, name + " decode error");
                    return;
                }
            }
        }
        throw new InvalidOperationException(name + " unexpectedly decoded.");
    }

    private static byte[] Pixel(byte red, byte green, byte blue, byte exponent)
    {
        return new[] { red, green, blue, exponent };
    }

    private static byte[] Repeat(byte[] value, int count)
    {
        var result = new byte[checked(value.Length * count)];
        for (var index = 0; index < count; index++)
            Buffer.BlockCopy(value, 0, result, index * value.Length, value.Length);
        return result;
    }

    private static byte[] Join(params byte[][] values)
    {
        var length = 0;
        foreach (var value in values)
            length = checked(length + value.Length);
        var result = new byte[length];
        var offset = 0;
        foreach (var value in values)
        {
            Buffer.BlockCopy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }
        return result;
    }

    private static void AssertPixel(
        byte[] pixels,
        int index,
        byte red,
        byte green,
        byte blue,
        byte exponent)
    {
        Equal(red, pixels[index * 4], "pixel " + index + " red");
        Equal(green, pixels[index * 4 + 1], "pixel " + index + " green");
        Equal(blue, pixels[index * 4 + 2], "pixel " + index + " blue");
        Equal(exponent, pixels[index * 4 + 3], "pixel " + index + " exponent");
    }

    private static void BytesEqual(byte[] expected, byte[] actual, string name)
    {
        Equal(expected.Length, actual.Length, name + " length");
        for (var index = 0; index < expected.Length; index++)
            if (expected[index] != actual[index])
                throw new InvalidOperationException(
                    name + ": byte " + index + " expected " + expected[index]
                    + ", got " + actual[index] + ".");
    }

    private static void Contains(string value, string expected, string name)
    {
        if (expected == null)
            return;
        if (value == null
            || value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException(
                name + ": expected text '" + expected + "', got '" + value + "'.");
    }

    private static void Near(double expected, double actual, double tolerance, string name)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException(
                name + ": expected " + expected + ", got " + actual + ".");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException(name + ": expected true.");
    }

    private static void False(bool value, string name)
    {
        if (value)
            throw new InvalidOperationException(name + ": expected false.");
    }

    private static string Escape(string value)
    {
        return value.Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
