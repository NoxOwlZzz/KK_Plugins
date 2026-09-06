using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static partial class MaterialEditorCubemapConversion
    {
        internal static bool TryExport(
            Cubemap cubemap,
            out byte[] pngData,
            out string error)
        {
            return TryExportCore(cubemap, out pngData, out error);
        }

        private static bool TryExportCore(
            Cubemap cubemap,
            out byte[] pngData,
            out string error)
        {
            pngData = null;
            error = null;
            if (cubemap == null)
            {
                error = "No Cubemap is currently assigned.";
                return false;
            }
            if (cubemap.width > MaximumExportFaceSize)
            {
                error = "Cubemap export supports face sizes up to "
                        + MaximumExportFaceSize
                        + " pixels to keep runtime readback memory bounded.";
                return false;
            }

            long estimatedPeakBytes;
            if (!MaterialEditorCubemapMemoryBudget.TryValidateExport(
                    cubemap.width,
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
                Color[][] faces;
                if (!TryReadFaces(cubemap, out faces, out error))
                    return false;

                Texture2D panorama = null;
                try
                {
                    var faceSize = cubemap.width;
                    var width = faceSize * 4;
                    var height = faceSize * 2;
                    var output = new Color32[width * height];
                    for (var y = 0; y < height; y++)
                    {
                        var panoramaV = (y + 0.5) / height;
                        for (var x = 0; x < width; x++)
                        {
                            var panoramaU = (x + 0.5) / width;
                            double directionX;
                            double directionY;
                            double directionZ;
                            MaterialEditorCubemapProjection.DirectionForEquirectangularUv(
                                panoramaU,
                                panoramaV,
                                out directionX,
                                out directionY,
                                out directionZ);
                            MaterialEditorCubemapFace face;
                            double faceU;
                            double faceV;
                            MaterialEditorCubemapProjection.FaceUvForDirection(
                                directionX,
                                directionY,
                                directionZ,
                                out face,
                                out faceU,
                                out faceV);
                            output[y * width + x] = SampleCubemap(
                                faces,
                                faceSize,
                                face,
                                faceU,
                                faceV);
                        }
                    }

                    panorama = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    panorama.SetPixels32(output);
                    panorama.Apply(false, false);
                    pngData = panorama.EncodeToPNG();
                    if (pngData != null && pngData.Length > 0)
                        return true;

                    pngData = null;
                    error = "Cubemap export failed because Unity returned no PNG data.";
                    return false;
                }
                catch (Exception exception)
                {
                    error = "Cubemap export failed: " + exception.Message;
                    pngData = null;
                    return false;
                }
                finally
                {
                    if (panorama != null)
                        UnityEngine.Object.Destroy(panorama);
                }
            }
            finally
            {
                memoryReservation.Dispose();
            }
        }

        private static Color SampleCubemap(
            Color[][] faces,
            int size,
            MaterialEditorCubemapFace face,
            double u,
            double v)
        {
            var sourceX = u * size - 0.5;
            var sourceY = v * size - 0.5;
            var rawX0 = FloorToInt(sourceX);
            var rawY0 = FloorToInt(sourceY);
            var fractionX = (float)(sourceX - Math.Floor(sourceX));
            var fractionY = (float)(sourceY - Math.Floor(sourceY));
            var bottom = Lerp(
                SampleFaceTexel(faces, size, face, rawX0, rawY0),
                SampleFaceTexel(faces, size, face, rawX0 + 1, rawY0),
                fractionX);
            var top = Lerp(
                SampleFaceTexel(faces, size, face, rawX0, rawY0 + 1),
                SampleFaceTexel(faces, size, face, rawX0 + 1, rawY0 + 1),
                fractionX);
            return Lerp(bottom, top, fractionY);
        }

        private static Color SampleFaceTexel(
            Color[][] faces,
            int size,
            MaterialEditorCubemapFace face,
            int x,
            int y)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
                return faces[(int)face][y * size + x];

            double directionX;
            double directionY;
            double directionZ;
            MaterialEditorCubemapProjection.DirectionForFaceUv(
                face,
                (x + 0.5) / size,
                (y + 0.5) / size,
                out directionX,
                out directionY,
                out directionZ);
            MaterialEditorCubemapFace adjacentFace;
            double adjacentU;
            double adjacentV;
            MaterialEditorCubemapProjection.FaceUvForDirection(
                directionX,
                directionY,
                directionZ,
                out adjacentFace,
                out adjacentU,
                out adjacentV);
            var adjacentX = Clamp(FloorToInt(adjacentU * size), 0, size - 1);
            var adjacentY = Clamp(FloorToInt(adjacentV * size), 0, size - 1);
            return faces[(int)adjacentFace][adjacentY * size + adjacentX];
        }
    }
}
