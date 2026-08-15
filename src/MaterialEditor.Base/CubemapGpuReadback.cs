using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static partial class MaterialEditorCubemapConversion
    {
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

            return TryReadFacesFromGpu(cubemap, out faces, out error);
        }

        private static bool TryReadFacesFromGpu(
            Cubemap cubemap,
            out Color[][] faces,
            out string error)
        {
            faces = null;
            error = null;
            RenderTexture temporary = null;
            Texture2D readable = null;
            GameObject cameraObject = null;
            Material skyboxMaterial = null;
            var previous = RenderTexture.active;
            try
            {
                // Keep the existing project-dependent color behavior until the
                // CPU/GPU paths are compared on every supported Unity target.
                // Selecting Linear or sRGB here without that evidence can alter
                // exported values; Default is therefore an explicit test gate.
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
                readable = new Texture2D(
                    cubemap.width,
                    cubemap.width,
                    TextureFormat.RGBA32,
                    false);

                // Reuse the same Camera, Skybox, material, RenderTexture and
                // readable Texture2D for all six synchronous readbacks.
                var result = new Color[6][];
                for (var index = 0; index < result.Length; index++)
                {
                    camera.transform.rotation = RotationForFace(
                        (MaterialEditorCubemapFace)index);
                    camera.Render();
                    RenderTexture.active = temporary;
                    readable.ReadPixels(
                        new Rect(0, 0, cubemap.width, cubemap.width),
                        0,
                        0,
                        false);
                    readable.Apply(false, false);
                    result[index] = readable.GetPixels();
                }
                faces = result;
                return true;
            }
            catch (Exception exception)
            {
                error = "The assigned Cubemap is not readable and GPU readback failed: "
                        + exception.Message;
                faces = null;
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
    }
}
