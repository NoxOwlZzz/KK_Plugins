using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorCubemapLease : IDisposable
    {
        private readonly string _key;
        private bool _disposed;

        internal MaterialEditorCubemapLease(string key, Cubemap cubemap)
        {
            _key = key;
            Cubemap = cubemap;
        }

        internal Cubemap Cubemap { get; private set; }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            MaterialEditorCubemapCache.Release(_key);
            Cubemap = null;
        }
    }

    internal static class MaterialEditorCubemapCache
    {
        private sealed class Entry
        {
            internal Cubemap Cubemap;
            internal int References;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Entry> Entries =
            new Dictionary<string, Entry>(StringComparer.Ordinal);

        internal static bool TryAcquire(
            byte[] pngData,
            out MaterialEditorCubemapLease lease,
            out string error)
        {
            string warning;
            return TryAcquire(
                pngData,
                out lease,
                out warning,
                out error);
        }

        internal static bool TryAcquire(
            byte[] pngData,
            out MaterialEditorCubemapLease lease,
            out string warning,
            out string error)
        {
            lease = null;
            warning = null;
            error = null;
            int width;
            int height;
            if (!MaterialEditorCubemapProjection.TryReadPngEquirectangularSize(
                    pngData,
                    out width,
                    out height,
                    out warning,
                    out error))
                return false;

            string key;
            using (var sha256 = SHA256.Create())
                key = Convert.ToBase64String(sha256.ComputeHash(pngData));

            lock (Sync)
            {
                Entry entry;
                if (Entries.TryGetValue(key, out entry))
                {
                    entry.References++;
                    lease = new MaterialEditorCubemapLease(key, entry.Cubemap);
                    return true;
                }

                Cubemap cubemap;
                if (!MaterialEditorCubemapConversion.TryImport(
                        pngData,
                        width,
                        height,
                        out cubemap,
                        out error))
                    return false;

                Entries.Add(
                    key,
                    new Entry
                    {
                        Cubemap = cubemap,
                        References = 1
                    });
                lease = new MaterialEditorCubemapLease(key, cubemap);
                return true;
            }
        }

        internal static void Release(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            Cubemap destroy = null;
            lock (Sync)
            {
                Entry entry;
                if (!Entries.TryGetValue(key, out entry))
                    return;
                entry.References--;
                if (entry.References > 0)
                    return;
                Entries.Remove(key);
                destroy = entry.Cubemap;
            }

            if (destroy != null)
                UnityEngine.Object.Destroy(destroy);
        }
    }

    internal static class MaterialEditorCubemapConversion
    {
        private const int MaximumExportFaceSize = 1024;

        internal static bool TryImport(
            byte[] pngData,
            int expectedWidth,
            int expectedHeight,
            out Cubemap cubemap,
            out string error)
        {
            cubemap = null;
            error = null;
            Texture2D source = null;
            Cubemap result = null;
            try
            {
                source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!source.LoadImage(pngData)
                    || source.width != expectedWidth
                    || source.height != expectedHeight)
                {
                    error = "The selected PNG could not be decoded with its declared dimensions.";
                    return false;
                }

                var sourcePixels = source.GetPixels32();
                int normalizedWidth;
                int normalizedHeight;
                MaterialEditorCubemapProjection.GetNormalizedEquirectangularSize(
                    expectedWidth,
                    expectedHeight,
                    out normalizedWidth,
                    out normalizedHeight);
                var faceSize = Math.Min(
                    normalizedWidth / 4,
                    normalizedHeight / 2);
                result = new Cubemap(faceSize, TextureFormat.RGBA32, true);
                result.name = "MaterialEditor imported cubemap";
                result.filterMode = FilterMode.Bilinear;
                result.wrapMode = TextureWrapMode.Clamp;
                result.anisoLevel = 1;

                var facePixels = new Color[faceSize * faceSize];
                for (var faceIndex = 0; faceIndex < 6; faceIndex++)
                {
                    var face = (MaterialEditorCubemapFace)faceIndex;
                    for (var y = 0; y < faceSize; y++)
                    {
                        var faceV = (y + 0.5) / faceSize;
                        for (var x = 0; x < faceSize; x++)
                        {
                            var faceU = (x + 0.5) / faceSize;
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
                            facePixels[y * faceSize + x] = SamplePanorama(
                                sourcePixels,
                                expectedWidth,
                                expectedHeight,
                                panoramaU,
                                panoramaV);
                        }
                    }
                    result.SetPixels(facePixels, ToUnityFace(face));
                }

                // The cached Cubemap is subsequently exported through GPU
                // readback when needed, so retaining six CPU-side face copies
                // would only multiply memory for every cached resource.
                result.Apply(true, true);
                cubemap = result;
                result = null;
                return true;
            }
            catch (Exception exception)
            {
                if (result != null)
                    UnityEngine.Object.Destroy(result);
                cubemap = null;
                error = "Cubemap conversion failed: " + exception.Message;
                return false;
            }
            finally
            {
                if (source != null)
                    UnityEngine.Object.Destroy(source);
            }
        }

        internal static bool TryExport(
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

        private static bool TryReadFaces(
            Cubemap cubemap,
            out Color[][] faces,
            out string error)
        {
            faces = new Color[6][];
            error = null;
            try
            {
                for (var index = 0; index < faces.Length; index++)
                    faces[index] = cubemap.GetPixels(
                        ToUnityFace((MaterialEditorCubemapFace)index));
                return true;
            }
            catch
            {
                // AssetBundle Cubemaps are commonly not CPU-readable. Read each
                // face back from the GPU without altering the source asset.
            }

            for (var index = 0; index < faces.Length; index++)
            {
                if (!TryReadFaceFromGpu(
                        cubemap,
                        (MaterialEditorCubemapFace)index,
                        out faces[index],
                        out error))
                {
                    faces = null;
                    return false;
                }
            }
            return true;
        }

        private static bool TryReadFaceFromGpu(
            Cubemap cubemap,
            MaterialEditorCubemapFace face,
            out Color[] pixels,
            out string error)
        {
            pixels = null;
            error = null;
            RenderTexture temporary = null;
            Texture2D readable = null;
            GameObject cameraObject = null;
            Material skyboxMaterial = null;
            var previous = RenderTexture.active;
            try
            {
                temporary = RenderTexture.GetTemporary(
                    cubemap.width,
                    cubemap.width,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default);
                var skyboxShader = Shader.Find("Skybox/Cubemap");
                if (skyboxShader == null)
                    throw new InvalidOperationException(
                        "Unity's Skybox/Cubemap shader is unavailable.");

                skyboxMaterial = new Material(skyboxShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                skyboxMaterial.SetTexture("_Tex", cubemap);
                if (skyboxMaterial.HasProperty("_Exposure"))
                    skyboxMaterial.SetFloat("_Exposure", 1f);
                if (skyboxMaterial.HasProperty("_Rotation"))
                    skyboxMaterial.SetFloat("_Rotation", 0f);

                cameraObject = new GameObject("MaterialEditor Cubemap Readback")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                var camera = cameraObject.AddComponent<Camera>();
                var skybox = cameraObject.AddComponent<Skybox>();
                skybox.material = skyboxMaterial;
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.cullingMask = 0;
                camera.fieldOfView = 90f;
                camera.aspect = 1f;
                camera.targetTexture = temporary;
                camera.transform.rotation = RotationForFace(face);
                camera.Render();
                RenderTexture.active = temporary;
                readable = new Texture2D(
                    cubemap.width,
                    cubemap.width,
                    TextureFormat.RGBA32,
                    false);
                readable.ReadPixels(
                    new Rect(0, 0, cubemap.width, cubemap.width),
                    0,
                    0,
                    false);
                readable.Apply(false, false);
                pixels = readable.GetPixels();
                return true;
            }
            catch (Exception exception)
            {
                error = "The assigned Cubemap is not readable and GPU readback failed: "
                        + exception.Message;
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                if (readable != null)
                    UnityEngine.Object.Destroy(readable);
                if (cameraObject != null)
                    UnityEngine.Object.Destroy(cameraObject);
                if (skyboxMaterial != null)
                    UnityEngine.Object.Destroy(skyboxMaterial);
                if (temporary != null)
                    RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static Quaternion RotationForFace(MaterialEditorCubemapFace face)
        {
            switch (face)
            {
                case MaterialEditorCubemapFace.PositiveX:
                    return Quaternion.LookRotation(Vector3.right, Vector3.up);
                case MaterialEditorCubemapFace.NegativeX:
                    return Quaternion.LookRotation(Vector3.left, Vector3.up);
                case MaterialEditorCubemapFace.PositiveY:
                    return Quaternion.LookRotation(Vector3.up, Vector3.back);
                case MaterialEditorCubemapFace.NegativeY:
                    return Quaternion.LookRotation(Vector3.down, Vector3.forward);
                case MaterialEditorCubemapFace.PositiveZ:
                    return Quaternion.LookRotation(Vector3.forward, Vector3.up);
                default:
                    return Quaternion.LookRotation(Vector3.back, Vector3.up);
            }
        }

        private static Color SamplePanorama(
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

        private static CubemapFace ToUnityFace(MaterialEditorCubemapFace face)
        {
            switch (face)
            {
                case MaterialEditorCubemapFace.PositiveX:
                    return CubemapFace.PositiveX;
                case MaterialEditorCubemapFace.NegativeX:
                    return CubemapFace.NegativeX;
                case MaterialEditorCubemapFace.PositiveY:
                    return CubemapFace.PositiveY;
                case MaterialEditorCubemapFace.NegativeY:
                    return CubemapFace.NegativeY;
                case MaterialEditorCubemapFace.PositiveZ:
                    return CubemapFace.PositiveZ;
                default:
                    return CubemapFace.NegativeZ;
            }
        }

        private static Color Lerp(Color32 left, Color32 right, float amount)
        {
            return Lerp((Color)left, (Color)right, amount);
        }

        private static Color Lerp(Color left, Color right, float amount)
        {
            return new Color(
                left.r + (right.r - left.r) * amount,
                left.g + (right.g - left.g) * amount,
                left.b + (right.b - left.b) * amount,
                left.a + (right.a - left.a) * amount);
        }

        private static int FloorToInt(double value)
        {
            return (int)Math.Floor(value);
        }

        private static int Wrap(int value, int maximum)
        {
            value %= maximum;
            return value < 0 ? value + maximum : value;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
                return minimum;
            return value > maximum ? maximum : value;
        }
    }
}
