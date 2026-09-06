using System;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorCubemapMemoryReservation : IDisposable
    {
        private MaterialEditorCubemapMemoryAdmission _owner;
        private readonly long _bytes;

        internal MaterialEditorCubemapMemoryReservation(
            MaterialEditorCubemapMemoryAdmission owner,
            long bytes)
        {
            _owner = owner;
            _bytes = bytes;
        }

        public void Dispose()
        {
            var owner = _owner;
            if (owner == null)
                return;
            _owner = null;
            owner.Release(_bytes);
        }
    }

    /// <summary>
    /// Short, Unity-free admission gate preventing simultaneous conversions
    /// from each satisfying the per-operation limit while exceeding it in sum.
    /// </summary>
    internal sealed class MaterialEditorCubemapMemoryAdmission
    {
        private readonly object _sync = new object();
        private readonly long _limitBytes;
        private long _reservedBytes;

        internal MaterialEditorCubemapMemoryAdmission(long limitBytes)
        {
            if (limitBytes <= 0L)
                throw new ArgumentOutOfRangeException("limitBytes");
            _limitBytes = limitBytes;
        }

        internal long ReservedBytes
        {
            get
            {
                lock (_sync)
                    return _reservedBytes;
            }
        }

        internal bool TryReserve(
            long bytes,
            out MaterialEditorCubemapMemoryReservation reservation,
            out string error)
        {
            reservation = null;
            error = null;
            if (bytes <= 0L || bytes > _limitBytes)
            {
                error = "The Cubemap conversion memory reservation is invalid.";
                return false;
            }

            lock (_sync)
            {
                if (_reservedBytes > _limitBytes - bytes)
                {
                    error = "Another Cubemap conversion is already using the temporary-memory budget. "
                            + "Wait for it to finish before importing another Cubemap.";
                    return false;
                }
                _reservedBytes += bytes;
            }

            reservation = new MaterialEditorCubemapMemoryReservation(this, bytes);
            return true;
        }

        internal void Release(long bytes)
        {
            lock (_sync)
            {
                _reservedBytes -= bytes;
                if (_reservedBytes < 0L)
                    _reservedBytes = 0L;
            }
        }
    }

    /// <summary>
    /// Conservative, deterministic estimates for temporary Cubemap conversion
    /// memory. These estimates are a guardrail; runtime Unity profiling remains
    /// necessary because native decoder and driver allocations are opaque.
    /// </summary>
    internal static class MaterialEditorCubemapMemoryBudget
    {
        internal const long DefaultPeakBytes = 320L * 1024L * 1024L;
        private static readonly MaterialEditorCubemapMemoryAdmission SharedAdmission =
            new MaterialEditorCubemapMemoryAdmission(DefaultPeakBytes);
        private const long Color32Bytes = 4L;
        private const long ColorBytes = 16L;
        private const long FaceCount = 6L;

        internal static long EstimateImportPeakBytes(
            long encodedBytes,
            int sourceWidth,
            int sourceHeight,
            int faceSize)
        {
            return EstimateImportPeakBytes(
                encodedBytes,
                sourceWidth,
                sourceHeight,
                faceSize,
                false);
        }

        internal static long EstimateImportPeakBytes(
            long encodedBytes,
            int sourceWidth,
            int sourceHeight,
            int faceSize,
            bool highDynamicRange)
        {
            if (encodedBytes < 0L
                || sourceWidth <= 0
                || sourceHeight <= 0
                || faceSize <= 0)
                return long.MaxValue;

            var sourcePixels = Multiply(sourceWidth, sourceHeight);
            var sourceRgba = Multiply(sourcePixels, Color32Bytes);
            var facePixels = Multiply(faceSize, faceSize);
            var faceBuffer = Multiply(facePixels, ColorBytes);
            var cubemapBytesPerPixel = highDynamicRange ? 8L : Color32Bytes;
            var cubemapBase = Multiply(
                Multiply(facePixels, FaceCount),
                cubemapBytesPerPixel);
            var cubemapWithMipmaps = Add(Multiply(cubemapBase, 4L), 2L) / 3L;

            if (highDynamicRange)
            {
                // Radiance RGBE stays packed at four bytes per source pixel.
                // Only one scanline is unpacked at a time; SetPixels converts
                // the reusable Color face buffer into an RGBAHalf Cubemap.
                var scanline = Multiply(
                    Math.Max(sourceWidth, sourceHeight),
                    Color32Bytes);
                return Sum(
                    encodedBytes,
                    sourceRgba,
                    scanline,
                    faceBuffer,
                    Multiply(cubemapWithMipmaps, 2L));
            }

            // Encoded input, decoded Texture2D CPU/GPU copies, GetPixels32 copy,
            // decoder scratch reserve, one reusable Color face buffer, and the
            // Cubemap CPU upload/GPU copies including mipmaps.
            return Sum(
                encodedBytes,
                Multiply(sourceRgba, 2L),
                sourceRgba,
                sourceRgba,
                faceBuffer,
                Multiply(cubemapWithMipmaps, 2L));
        }

        internal static long EstimateExportPeakBytes(
            int faceSize,
            bool includeGpuReadback)
        {
            if (faceSize <= 0)
                return long.MaxValue;

            var facePixels = Multiply(faceSize, faceSize);
            var panoramaPixels = Multiply(facePixels, 8L);
            var panoramaRgba = Multiply(panoramaPixels, Color32Bytes);
            var retainedFaceArrays = Multiply(Multiply(facePixels, FaceCount), ColorBytes);
            var panoramaTextureCpuAndGpu = Multiply(panoramaRgba, 2L);
            var encoderAndEncodedOutputReserve = Multiply(panoramaRgba, 2L);
            var gpuReadbackWorkingSet = includeGpuReadback
                ? Multiply(facePixels, ColorBytes + Color32Bytes + Color32Bytes)
                : 0L;

            return Sum(
                retainedFaceArrays,
                panoramaRgba,
                panoramaTextureCpuAndGpu,
                encoderAndEncodedOutputReserve,
                gpuReadbackWorkingSet);
        }

        internal static bool TryValidateImport(
            long encodedBytes,
            int sourceWidth,
            int sourceHeight,
            int faceSize,
            out long estimatedPeakBytes,
            out string error)
        {
            return TryValidateImport(
                encodedBytes,
                sourceWidth,
                sourceHeight,
                faceSize,
                false,
                out estimatedPeakBytes,
                out error);
        }

        internal static bool TryValidateImport(
            long encodedBytes,
            int sourceWidth,
            int sourceHeight,
            int faceSize,
            bool highDynamicRange,
            out long estimatedPeakBytes,
            out string error)
        {
            estimatedPeakBytes = EstimateImportPeakBytes(
                encodedBytes,
                sourceWidth,
                sourceHeight,
                faceSize,
                highDynamicRange);
            return TryValidate(estimatedPeakBytes, "import", out error);
        }

        internal static bool TryValidateExport(
            int faceSize,
            out long estimatedPeakBytes,
            out string error)
        {
            estimatedPeakBytes = EstimateExportPeakBytes(faceSize, true);
            return TryValidate(estimatedPeakBytes, "export", out error);
        }

        internal static bool TryReserveConversion(
            long estimatedPeakBytes,
            out MaterialEditorCubemapMemoryReservation reservation,
            out string error)
        {
            return SharedAdmission.TryReserve(
                estimatedPeakBytes,
                out reservation,
                out error);
        }

        private static bool TryValidate(
            long estimatedPeakBytes,
            string operation,
            out string error)
        {
            error = null;
            if (estimatedPeakBytes <= DefaultPeakBytes)
                return true;

            error = "Cubemap " + operation + " is estimated to require "
                    + ToMebibytes(estimatedPeakBytes)
                    + " MiB of temporary memory, exceeding the "
                    + ToMebibytes(DefaultPeakBytes) + " MiB conversion budget.";
            return false;
        }

        private static long ToMebibytes(long bytes)
        {
            if (bytes == long.MaxValue)
                return long.MaxValue;
            return (bytes + 1024L * 1024L - 1L) / (1024L * 1024L);
        }

        private static long Sum(params long[] values)
        {
            var result = 0L;
            for (var index = 0; index < values.Length; index++)
                result = Add(result, values[index]);
            return result;
        }

        private static long Add(long left, long right)
        {
            if (left < 0L || right < 0L || left > long.MaxValue - right)
                return long.MaxValue;
            return left + right;
        }

        private static long Multiply(long left, long right)
        {
            if (left < 0L || right < 0L)
                return long.MaxValue;
            if (left == 0L || right == 0L)
                return 0L;
            return left > long.MaxValue / right ? long.MaxValue : left * right;
        }
    }
}
