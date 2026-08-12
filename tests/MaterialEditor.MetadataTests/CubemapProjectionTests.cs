using MaterialEditorAPI;

internal static class CubemapProjectionTests
{
    internal static void Run()
    {
        AcceptsEquirectangularPngDimensions();
        AcceptsAndNormalizesNonTwoToOneDimensions();
        WarnsAccuratelyForIndivisibleTwoToOneDimensions();
        NormalizesTinyImagesToTheMinimumFaceSize();
        AcceptsSourcesAtTheDecodedPixelBudget();
        RejectsUnboundedEquirectangularDimensions();
        RejectsOversizedSourceFilesBeforeDecode();
        FaceCentersUseUnityAxes();
        FaceCentersMapToKnownLatLongCoordinates();
        FaceProjectionRoundTripsAwayFromEdges();
        PanoramaSeamWrapsContinuously();
    }

    private static void AcceptsEquirectangularPngDimensions()
    {
        var png = CreatePngHeader(2048, 1024);
        int width;
        int height;
        string warning;
        string error;
        True(
            MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                png,
                out width,
                out height,
                out warning,
                out error),
            "2:1 PNG accepted");
        Equal(2048, width, "PNG width");
        Equal(1024, height, "PNG height");
        Equal(null, warning, "valid 2:1 PNG warning");
        Equal(null, error, "valid PNG error");
    }

    private static void AcceptsAndNormalizesNonTwoToOneDimensions()
    {
        AcceptsWithNormalization(1024, 1024, 1024, 512, "square PNG");
        AcceptsWithNormalization(3000, 1000, 2000, 1000, "wide PNG");
        AcceptsWithNormalization(500, 1000, 500, 250, "tall PNG");
    }

    private static void NormalizesTinyImagesToTheMinimumFaceSize()
    {
        AcceptsWithNormalization(1, 1, 4, 2, "tiny PNG");
    }

    private static void WarnsAccuratelyForIndivisibleTwoToOneDimensions()
    {
        int width;
        int height;
        string warning;
        string error;
        True(
            MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                CreatePngHeader(10, 5),
                out width,
                out height,
                out warning,
                out error),
            "indivisible 2:1 PNG accepted");
        Equal(null, error, "indivisible 2:1 PNG error");
        True(warning != null && warning.Contains("uses a 2:1 aspect ratio"),
            "indivisible 2:1 warning remains truthful");
        True(warning.Contains("8x4"), "indivisible 2:1 normalized dimensions");
    }

    private static void AcceptsSourcesAtTheDecodedPixelBudget()
    {
        AcceptsWithNormalization(8192, 1024, 2048, 1024, "wide budget-edge PNG");
        AcceptsWithNormalization(1024, 8192, 1024, 512, "tall budget-edge PNG");
    }

    private static void RejectsUnboundedEquirectangularDimensions()
    {
        int width;
        int height;
        string warning;
        string error;
        True(
            !MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                CreatePngHeader(8192, 4096),
                out width,
                out height,
                out warning,
                out error),
            "oversized 2:1 PNG rejected");
        True(
            error != null
            && error.Contains("8388608")
            && error.Contains("8192"),
            "oversized PNG error states pixel and dimension limits");
        Equal(null, warning, "oversized PNG warning");

        True(
            !MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                CreatePngHeader(4096, 2049),
                out width,
                out height,
                out warning,
                out error),
            "source-height cap preserved");
        True(error != null && error.Contains("8388608"), "pixel-budget error");

        True(
            !MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                CreatePngHeader(8193, 1),
                out width,
                out height,
                out warning,
                out error),
            "maximum individual dimension preserved");
        True(error != null && error.Contains("8192"), "dimension-cap error");
    }

    private static void RejectsOversizedSourceFilesBeforeDecode()
    {
        string error;
        True(
            MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                MaterialEditorCubemapProjection.MaximumSourceFileBytes,
                out error),
            "64 MiB file accepted");
        Equal(null, error, "64 MiB file error");
        True(
            !MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                MaterialEditorCubemapProjection.MaximumSourceFileBytes + 1,
                out error),
            "file above 64 MiB rejected");
        True(error != null && error.Contains("64 MiB"), "file-size cap error");
        True(
            !MaterialEditorCubemapProjection.TryValidateSourceFileLength(-1, out error),
            "negative file length rejected");
        True(error != null && error.Contains("invalid"), "negative file-length error");
    }

    private static void FaceCentersUseUnityAxes()
    {
        var expected = new[]
        {
            new[] { 1.0, 0.0, 0.0 },
            new[] { -1.0, 0.0, 0.0 },
            new[] { 0.0, 1.0, 0.0 },
            new[] { 0.0, -1.0, 0.0 },
            new[] { 0.0, 0.0, 1.0 },
            new[] { 0.0, 0.0, -1.0 }
        };
        for (var index = 0; index < expected.Length; index++)
        {
            double x;
            double y;
            double z;
            MaterialEditorCubemapProjection.DirectionForFaceUv(
                (MaterialEditorCubemapFace)index,
                0.5,
                0.5,
                out x,
                out y,
                out z);
            Near(expected[index][0], x, "face center x " + index);
            Near(expected[index][1], y, "face center y " + index);
            Near(expected[index][2], z, "face center z " + index);
        }
    }

    private static void FaceCentersMapToKnownLatLongCoordinates()
    {
        var expected = new[]
        {
            new[] { 0.5, 0.5 },
            new[] { 0.0, 0.5 },
            new[] { 0.5, 1.0 },
            new[] { 0.5, 0.0 },
            new[] { 0.75, 0.5 },
            new[] { 0.25, 0.5 }
        };
        for (var index = 0; index < expected.Length; index++)
        {
            double x;
            double y;
            double z;
            MaterialEditorCubemapProjection.DirectionForFaceUv(
                (MaterialEditorCubemapFace)index,
                0.5,
                0.5,
                out x,
                out y,
                out z);
            double u;
            double v;
            MaterialEditorCubemapProjection.EquirectangularUvForDirection(
                x,
                y,
                z,
                out u,
                out v);
            Near(expected[index][0], u, "known panorama u " + index);
            Near(expected[index][1], v, "known panorama v " + index);
        }
    }

    private static void FaceProjectionRoundTripsAwayFromEdges()
    {
        for (var index = 0; index < 6; index++)
        {
            double x;
            double y;
            double z;
            MaterialEditorCubemapProjection.DirectionForFaceUv(
                (MaterialEditorCubemapFace)index,
                0.23,
                0.71,
                out x,
                out y,
                out z);
            MaterialEditorCubemapFace face;
            double u;
            double v;
            MaterialEditorCubemapProjection.FaceUvForDirection(
                x,
                y,
                z,
                out face,
                out u,
                out v);
            Equal((MaterialEditorCubemapFace)index, face, "round-trip face " + index);
            Near(0.23, u, "round-trip u " + index);
            Near(0.71, v, "round-trip v " + index);
        }
    }

    private static void PanoramaSeamWrapsContinuously()
    {
        double leftX;
        double leftY;
        double leftZ;
        double rightX;
        double rightY;
        double rightZ;
        MaterialEditorCubemapProjection.DirectionForEquirectangularUv(
            0.0,
            0.5,
            out leftX,
            out leftY,
            out leftZ);
        MaterialEditorCubemapProjection.DirectionForEquirectangularUv(
            1.0,
            0.5,
            out rightX,
            out rightY,
            out rightZ);
        Near(leftX, rightX, "seam x");
        Near(leftY, rightY, "seam y");
        Near(leftZ, rightZ, "seam z");
    }

    private static byte[] CreatePngHeader(int width, int height)
    {
        var data = new byte[24];
        var signature = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
        };
        Array.Copy(signature, data, signature.Length);
        data[12] = (byte)'I';
        data[13] = (byte)'H';
        data[14] = (byte)'D';
        data[15] = (byte)'R';
        WriteBigEndian(data, 16, width);
        WriteBigEndian(data, 20, height);
        return data;
    }

    private static void AcceptsWithNormalization(
        int sourceWidth,
        int sourceHeight,
        int expectedWidth,
        int expectedHeight,
        string message)
    {
        int width;
        int height;
        string warning;
        string error;
        True(
            MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                CreatePngHeader(sourceWidth, sourceHeight),
                out width,
                out height,
                out warning,
                out error),
            message + " accepted");
        Equal(sourceWidth, width, message + " source width");
        Equal(sourceHeight, height, message + " source height");
        Equal(null, error, message + " error");
        True(warning != null && warning.Contains("resampled"), message + " warning");
        if ((long)sourceWidth != (long)sourceHeight * 2L)
            True(warning.Contains("stretched"), message + " stretch warning");

        int normalizedWidth;
        int normalizedHeight;
        MaterialEditorCubemapProjection.GetNormalizedEquirectangularSize(
            width,
            height,
            out normalizedWidth,
            out normalizedHeight);
        Equal(expectedWidth, normalizedWidth, message + " normalized width");
        Equal(expectedHeight, normalizedHeight, message + " normalized height");
        True(
            warning.Contains(expectedWidth + "x" + expectedHeight),
            message + " warning states normalized dimensions");
    }

    private static void WriteBigEndian(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void Near(double expected, double actual, string message)
    {
        if (Math.Abs(expected - actual) > 0.000001)
            throw new InvalidOperationException(
                message + ": expected " + expected + ", actual " + actual);
    }

    private static void True(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                message + ": expected " + expected + ", actual " + actual);
    }
}
