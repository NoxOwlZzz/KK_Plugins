using MaterialEditorAPI;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class SceneController
    {
        public void SetMaterialCubemapFromFile(
            int id,
            Material material,
            string propertyName,
            string filePath)
        {
            if (!File.Exists(filePath))
                return;

            string fileError;
            if (!MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                    new FileInfo(filePath).Length,
                    out fileError))
            {
                MaterialEditorPlugin.Logger.LogMessage(fileError);
                return;
            }

            SetMaterialCubemap(
                id,
                material,
                propertyName,
                File.ReadAllBytes(filePath),
                true);
        }

        public void SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data)
        {
            SetMaterialCubemap(id, material, propertyName, data, false);
        }

        private void SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data,
            bool logNormalizationWarning)
        {
            if (data == null)
                return;

            MaterialEditorCubemapLease lease;
            string warning;
            string error;
            if (!MaterialEditorCubemapCache.TryAcquire(
                    data,
                    out lease,
                    out warning,
                    out error))
            {
                MaterialEditorPlugin.Logger.LogMessage(error);
                return;
            }
            if (logNormalizationWarning && !string.IsNullOrEmpty(warning))
                MaterialEditorPlugin.Logger.LogWarning(warning);

            var texID = SetAndGetTextureID(data);
            CacheCubemapLease(texID, lease);

            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ID == id
                && x.Property == propertyName
                && x.MaterialName == material.NameFormatted());
            if (cubemapProperty == null)
            {
                cubemapProperty = new MaterialCubemapProperty(
                    id,
                    material.NameFormatted(),
                    propertyName,
                    texID);
                MaterialCubemapPropertyList.Add(cubemapProperty);
            }
            else
                cubemapProperty.TexID = texID;

            SetCubemapWithProperty(GetObjectByID(id), cubemapProperty);
            PurgeUnusedCubemapLeases();
        }

        private bool SetCubemapWithProperty(
            GameObject gameObject,
            MaterialCubemapProperty cubemapProperty)
        {
            if (cubemapProperty == null
                || !cubemapProperty.TexID.HasValue
                || cubemapProperty.NullCheck())
                return false;

            Cubemap cubemap;
            string error;
            if (!TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error))
            {
                MaterialEditorPluginBase.Logger.LogWarning(error);
                return false;
            }

            cubemapProperty.SynchronizeCubemapOriginalSnapshot(gameObject);
            return SetCubemap(
                gameObject,
                cubemapProperty.MaterialName,
                cubemapProperty.Property,
                cubemap);
        }

        public Cubemap GetMaterialCubemap(
            int id,
            Material material,
            string propertyName)
        {
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ID == id
                && x.MaterialName == material.NameFormatted()
                && x.Property == propertyName);
            if (cubemapProperty == null || !cubemapProperty.TexID.HasValue)
                return null;

            Cubemap cubemap;
            string error;
            return TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error)
                ? cubemap
                : null;
        }

        public bool GetMaterialCubemapOriginal(
            int id,
            Material material,
            string propertyName)
        {
            return MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ID == id
                && x.MaterialName == material.NameFormatted()
                && x.Property == propertyName)?.TexID == null;
        }

        public void RemoveMaterialCubemap(
            int id,
            Material material,
            string propertyName)
        {
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ID == id
                && x.MaterialName == material.NameFormatted()
                && x.Property == propertyName);
            if (cubemapProperty == null)
                return;

            var gameObject = GetObjectByID(id);
            cubemapProperty.SynchronizeCubemapOriginalSnapshot(gameObject);
            MaterialCubemapOriginalSnapshot.RestoreByMaterialReference(
                gameObject,
                cubemapProperty.MaterialName,
                cubemapProperty.Property,
                cubemapProperty.CubemapOriginalMaterials);
            cubemapProperty.ClearCubemapOriginalSnapshot();
            cubemapProperty.TexID = null;
            if (cubemapProperty.NullCheck())
                MaterialCubemapPropertyList.Remove(cubemapProperty);
            PurgeUnusedCubemapLeases();
        }
    }
}
