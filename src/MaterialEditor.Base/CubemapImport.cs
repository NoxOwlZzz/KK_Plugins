using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Main-thread incremental panorama-to-Cubemap conversion. PNG Unity decode
    /// and object creation happen in TryBegin; Radiance RGBE decode and the
    /// expensive projection are divided into explicit row batches that a UI
    /// coroutine can process across frames.
    /// </summary>
    internal sealed class MaterialEditorCubemapImportOperation : IDisposable
    {
        private Texture2D _source;
        private Color32[] _sourcePixels;
        private byte[] _hdrSourcePixels;
        private MaterialEditorRadianceHdrDecodeOperation _hdrDecode;
        private Color[] _facePixels;
        private Cubemap _result;
        private readonly int _sourceWidth;
        private readonly int _sourceHeight;
        private readonly int _faceSize;
        private readonly int _decodeTotalScanlines;
        private int _decodeCompletedScanlines;
        private int _faceIndex;
        private int _row;
        private bool _disposed;

        private MaterialEditorCubemapImportOperation(
            Texture2D source,
            Color32[] sourcePixels,
            MaterialEditorRadianceHdrDecodeOperation hdrDecode,
            Cubemap result,
            int sourceWidth,
            int sourceHeight,
            int faceSize)
        {
            _source = source;
            _sourcePixels = sourcePixels;
            _hdrDecode = hdrDecode;
            _result = result;
            _sourceWidth = sourceWidth;
            _sourceHeight = sourceHeight;
            _faceSize = faceSize;
            _decodeTotalScanlines = hdrDecode == null ? 0 : hdrDecode.TotalScanlines;
            _facePixels = new Color[faceSize * faceSize];
        }

        internal bool IsComplete { get; private set; }

        internal int CompletedRows
        {
            get
            {
                var decoded = _hdrDecode == null
                    ? _decodeCompletedScanlines
                    : _hdrDecode.CompletedScanlines;
                return decoded + _faceIndex * _faceSize + _row;
            }
        }

        internal int TotalRows
        {
            get { return _decodeTotalScanlines + _faceSize * 6; }
        }

        internal static bool TryBegin(
            byte[] pngData,
            int expectedWidth,
            int expectedHeight,
            out MaterialEditorCubemapImportOperation operation,
            out string error)
        {
            return TryBegin(
                pngData,
                new MaterialEditorCubemapSourceInfo(
                    MaterialEditorCubemapSourceFormat.Png,
                    expectedWidth,
                    expectedHeight,
                    null),
                out operation,
                out error);
        }

        internal static bool TryBegin(
            byte[] encodedData,
            MaterialEditorCubemapSourceInfo sourceInfo,
            out MaterialEditorCubemapImportOperation operation,
            out string error)
        {
            operation = null;
            error = null;
            if (encodedData == null || sourceInfo == null)
            {
                error = "The selected Cubemap source contains no data.";
                return false;
            }

            var expectedWidth = sourceInfo.Width;
            var expectedHeight = sourceInfo.Height;

            int normalizedWidth;
            int normalizedHeight;
            MaterialEditorCubemapProjection.GetNormalizedEquirectangularSize(
                expectedWidth,
                expectedHeight,
                out normalizedWidth,
                out normalizedHeight);
            var faceSize = Math.Min(normalizedWidth / 4, normalizedHeight / 2);
            long estimatedPeakBytes;
            if (!MaterialEditorCubemapMemoryBudget.TryValidateImport(
                    encodedData.LongLength,
                    expectedWidth,
                    expectedHeight,
                    faceSize,
                    sourceInfo.IsHighDynamicRange,
                    out estimatedPeakBytes,
                    out error))
                return false;

            Texture2D source = null;
            Cubemap result = null;
            MaterialEditorRadianceHdrDecodeOperation hdrDecode = null;
            var startedAt = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.CubemapImportDecode);
            try
            {
                Color32[] sourcePixels = null;
                if (sourceInfo.Format == MaterialEditorCubemapSourceFormat.Png)
                {
                    source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!source.LoadImage(encodedData)
                        || source.width != expectedWidth
                        || source.height != expectedHeight)
                    {
                        error = "The selected PNG could not be decoded with its declared dimensions.";
                        return false;
                    }
                    sourcePixels = source.GetPixels32();
                    result = new Cubemap(faceSize, TextureFormat.RGBA32, true);
                }
                else
                {
                    if (!SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
                    {
                        error = "This system does not support RGBAHalf Cubemaps required for Radiance HDR import.";
                        return false;
                    }
                    if (!MaterialEditorRadianceHdrDecodeOperation.TryBegin(
                            encodedData,
                            sourceInfo.RadianceHeader,
                            out hdrDecode,
                            out error))
                        return false;
                    result = new Cubemap(faceSize, TextureFormat.RGBAHalf, true);
                }

                result.name = sourceInfo.IsHighDynamicRange
                    ? "MaterialEditor imported HDR cubemap"
                    : "MaterialEditor imported cubemap";
                result.filterMode = FilterMode.Bilinear;
                result.wrapMode = TextureWrapMode.Clamp;
                result.anisoLevel = 1;

                operation = new MaterialEditorCubemapImportOperation(
                    source,
                    sourcePixels,
                    hdrDecode,
                    result,
                    expectedWidth,
                    expectedHeight,
                    faceSize);
                source = null;
                hdrDecode = null;
                result = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Cubemap decode failed: " + exception.Message;
                return false;
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.CubemapImportDecode,
                    startedAt);
                if (result != null)
                    UnityEngine.Object.Destroy(result);
                if (hdrDecode != null)
                    hdrDecode.Dispose();
                if (source != null)
                    UnityEngine.Object.Destroy(source);
            }
        }

        /// <summary>
        /// Processes at most maxRows of projection work. Call once per coroutine
        /// frame with a bounded value. This method still runs on the main thread;
        /// it is incremental, not a background-thread Unity operation.
        /// </summary>
        internal bool ProcessRows(int maxRows, out string error)
        {
            error = null;
            if (_disposed)
            {
                error = "The Cubemap import operation has already been disposed.";
                return false;
            }
            if (IsComplete)
                return true;
            if (maxRows <= 0)
            {
                error = "Cubemap import row budget must be positive.";
                return false;
            }

            var startedAt = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.CubemapImportSampling);
            try
            {
                var remainingRows = maxRows;
                if (_hdrDecode != null)
                {
                    var before = _hdrDecode.CompletedScanlines;
                    if (!_hdrDecode.ProcessScanlines(remainingRows, out error))
                    {
                        Dispose();
                        return false;
                    }
                    var decodedNow = _hdrDecode.CompletedScanlines - before;
                    remainingRows -= decodedNow;
                    if (!_hdrDecode.IsComplete)
                        return true;

                    _hdrSourcePixels = _hdrDecode.TakePixels();
                    _decodeCompletedScanlines = _decodeTotalScanlines;
                    _hdrDecode.Dispose();
                    _hdrDecode = null;
                    if (_hdrSourcePixels == null)
                    {
                        error = "Radiance HDR decoding completed without pixels.";
                        Dispose();
                        return false;
                    }
                    if (remainingRows <= 0)
                        return true;
                }

                var processedRows = 0;
                while (_faceIndex < 6 && processedRows < remainingRows)
                {
                    var face = (MaterialEditorCubemapFace)_faceIndex;
                    var faceV = (_row + 0.5) / _faceSize;
                    for (var x = 0; x < _faceSize; x++)
                    {
                        var faceU = (x + 0.5) / _faceSize;
                        double directionX;
                        double directionY;
                        double directionZ;
                        MaterialEditorCubemapProjection.DirectionForFaceUv(
                            face,
                            faceU,
                            faceV,
                            out directionX,
                            out directionY,
                            out directionZ);
                        double panoramaU;
                        double panoramaV;
                        MaterialEditorCubemapProjection.EquirectangularUvForDirection(
                            directionX,
                            directionY,
                            directionZ,
                            out panoramaU,
                            out panoramaV);
                        _facePixels[_row * _faceSize + x] =
                            _hdrSourcePixels == null
                                ? MaterialEditorCubemapConversion.SamplePanorama(
                                    _sourcePixels,
                                    _sourceWidth,
                                    _sourceHeight,
                                    panoramaU,
                                    panoramaV)
                                : MaterialEditorCubemapConversion.SampleRadiancePanorama(
                                    _hdrSourcePixels,
                                    _sourceWidth,
                                    _sourceHeight,
                                    panoramaU,
                                    panoramaV);
                    }

                    _row++;
                    processedRows++;
                    if (_row < _faceSize)
                        continue;

                    _result.SetPixels(
                        _facePixels,
                        MaterialEditorCubemapConversion.ToUnityFace(face));
                    _faceIndex++;
                    _row = 0;
                }

                if (_faceIndex < 6)
                    return true;

                // Keep imported Cubemaps CPU-readable. Export must work even in
                // Unity builds that strip the built-in Skybox/Cubemap shader,
                // where the GPU readback fallback is unavailable.
                _result.Apply(true, false);
                IsComplete = true;
                ReleaseSourceMemory();
                return true;
            }
            catch (Exception exception)
            {
                error = "Cubemap conversion failed: " + exception.Message;
                Dispose();
                return false;
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.CubemapImportSampling,
                    startedAt);
            }
        }

        internal Cubemap TakeResult()
        {
            if (_disposed || !IsComplete)
                return null;
            var result = _result;
            _result = null;
            return result;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ReleaseSourceMemory();
            if (_result != null)
                UnityEngine.Object.Destroy(_result);
            _result = null;
        }

        private void ReleaseSourceMemory()
        {
            if (_source != null)
                UnityEngine.Object.Destroy(_source);
            _source = null;
            _sourcePixels = null;
            _hdrSourcePixels = null;
            if (_hdrDecode != null)
                _hdrDecode.Dispose();
            _hdrDecode = null;
            _facePixels = null;
        }
    }

    internal static partial class MaterialEditorCubemapConversion
    {
        internal static bool TryImport(
            byte[] pngData,
            int expectedWidth,
            int expectedHeight,
            out Cubemap cubemap,
            out string error)
        {
            cubemap = null;
            int normalizedWidth;
            int normalizedHeight;
            MaterialEditorCubemapProjection.GetNormalizedEquirectangularSize(
                expectedWidth,
                expectedHeight,
                out normalizedWidth,
                out normalizedHeight);
            var faceSize = Math.Min(normalizedWidth / 4, normalizedHeight / 2);
            long estimatedPeakBytes;
            if (!MaterialEditorCubemapMemoryBudget.TryValidateImport(
                    pngData == null ? 0L : pngData.LongLength,
                    expectedWidth,
                    expectedHeight,
                    faceSize,
                    out estimatedPeakBytes,
                    out error))
                return false;

            MaterialEditorCubemapMemoryReservation memoryReservation;
            if (!MaterialEditorCubemapMemoryBudget.TryReserveConversion(
                    estimatedPeakBytes,
                    out memoryReservation,
                    out error))
                return false;

            try
            {
                MaterialEditorCubemapImportOperation operation;
                if (!MaterialEditorCubemapImportOperation.TryBegin(
                        pngData,
                        expectedWidth,
                        expectedHeight,
                        out operation,
                        out error))
                    return false;

                using (operation)
                {
                    if (!operation.ProcessRows(operation.TotalRows, out error))
                        return false;
                    cubemap = operation.TakeResult();
                    if (cubemap != null)
                        return true;
                    error = "Cubemap conversion completed without a result.";
                    return false;
                }
            }
            finally
            {
                memoryReservation.Dispose();
            }
        }

        internal static Color SamplePanorama(
            Color32[] pixels,
            int width,
            int height,
            double u,
            double v)
        {
            var sourceX = u * width - 0.5;
            var sourceY = v * height - 0.5;
            var x0 = FloorToInt(sourceX);
            var rawY0 = FloorToInt(sourceY);
            var fractionX = (float)(sourceX - Math.Floor(sourceX));
            var fractionY = (float)(sourceY - Math.Floor(sourceY));
            var x1 = Wrap(x0 + 1, width);
            x0 = Wrap(x0, width);
            var y0 = Clamp(rawY0, 0, height - 1);
            var y1 = Clamp(rawY0 + 1, 0, height - 1);

            var bottom = Lerp(
                pixels[y0 * width + x0],
                pixels[y0 * width + x1],
                fractionX);
            var top = Lerp(
                pixels[y1 * width + x0],
                pixels[y1 * width + x1],
                fractionX);
            return Lerp(bottom, top, fractionY);
        }

        internal static Color SampleRadiancePanorama(
            byte[] pixels,
            int width,
            int height,
            double u,
            double v)
        {
            var sourceX = u * width - 0.5;
            var sourceY = v * height - 0.5;
            var x0 = FloorToInt(sourceX);
            var rawY0 = FloorToInt(sourceY);
            var fractionX = (float)(sourceX - Math.Floor(sourceX));
            var fractionY = (float)(sourceY - Math.Floor(sourceY));
            var x1 = Wrap(x0 + 1, width);
            x0 = Wrap(x0, width);
            var y0 = Clamp(rawY0, 0, height - 1);
            var y1 = Clamp(rawY0 + 1, 0, height - 1);

            var bottom = Lerp(
                DecodeRadiancePixel(pixels, y0 * width + x0),
                DecodeRadiancePixel(pixels, y0 * width + x1),
                fractionX);
            var top = Lerp(
                DecodeRadiancePixel(pixels, y1 * width + x0),
                DecodeRadiancePixel(pixels, y1 * width + x1),
                fractionX);
            return Lerp(bottom, top, fractionY);
        }

        private static Color DecodeRadiancePixel(byte[] pixels, int pixelIndex)
        {
            const float maximumHalfValue = 65504f;
            var offset = pixelIndex * 4;
            float red;
            float green;
            float blue;
            MaterialEditorRadianceHdrDecoder.DecodeRgbe(
                pixels[offset],
                pixels[offset + 1],
                pixels[offset + 2],
                pixels[offset + 3],
                out red,
                out green,
                out blue);
            return new Color(
                Math.Min(red, maximumHalfValue),
                Math.Min(green, maximumHalfValue),
                Math.Min(blue, maximumHalfValue),
                1f);
        }
    }
}
