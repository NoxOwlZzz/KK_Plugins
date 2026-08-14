using MaterialEditorAPI;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
        /// <summary>
        /// Import and persist a native Cubemap from an equirectangular PNG file.
        /// </summary>
        public void SetMaterialCubemapFromFile(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            string filePath,
            GameObject go)
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
                slot,
                objectType,
                material,
                propertyName,
                File.ReadAllBytes(filePath),
                go,
                true);
        }

        /// <summary>
        /// Import and persist a native Cubemap from encoded equirectangular PNG data.
        /// </summary>
        public void SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            GameObject go)
        {
            SetMaterialCubemap(slot, objectType, material, propertyName, data, go, false);
        }

        private void SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            GameObject go,
            bool logNormalizationWarning)
        {
            if (data == null)
                return;

            MaterialEditorCubemapLease lease;
            string warning;
            string error;
            if (!MaterialEditorCubemapCache.TryAcquire(data, out lease, out warning, out error))
            {
                MaterialEditorPlugin.Logger.LogMessage(error);
                return;
            }
            if (logNormalizationWarning && !string.IsNullOrEmpty(warning))
                MaterialEditorPlugin.Logger.LogWarning(warning);

            var texID = SetAndGetTextureID(data);
            CacheCubemapLease(texID, lease);

            var cubemapProperty = FindMaterialCubemapProperty(
                slot,
                objectType,
                material,
                propertyName);
            if (cubemapProperty == null)
            {
                cubemapProperty = new MaterialCubemapProperty(
                    objectType,
                    GetCoordinateIndex(objectType),
                    slot,
                    material.NameFormatted(),
                    propertyName,
                    texID);
                MaterialCubemapPropertyList.Add(cubemapProperty);
            }
            else
            {
                cubemapProperty.TexID = texID;
            }

            SetCubemapWithProperty(go, cubemapProperty);
            PurgeUnusedCubemapLeases();
        }

        /// <summary>
        /// Get the persisted native Cubemap value, or null when no override exists.
        /// </summary>
        public Cubemap GetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go)
        {
            var cubemapProperty = FindMaterialCubemapProperty(
                slot,
                objectType,
                material,
                propertyName);
            if (cubemapProperty == null || !cubemapProperty.TexID.HasValue)
                return null;

            Cubemap cubemap;
            string error;
            return TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error)
                ? cubemap
                : null;
        }

        /// <summary>
        /// Get whether the Cubemap property is still in its original state.
        /// </summary>
        public bool GetMaterialCubemapOriginal(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go)
        {
            return FindMaterialCubemapProperty(slot, objectType, material, propertyName)?.TexID == null;
        }

        /// <summary>
        /// Remove a persisted Cubemap override and restore the exact original value.
        /// </summary>
        public void RemoveMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go,
            bool setProperty = true)
        {
            var cubemapProperty = FindMaterialCubemapProperty(
                slot,
                objectType,
                material,
                propertyName);
            if (cubemapProperty == null)
                return;

            if (setProperty)
            {
                cubemapProperty.SynchronizeCubemapOriginalSnapshot(go);
                MaterialCubemapOriginalSnapshot.RestoreByMaterialReference(
                    go,
                    cubemapProperty.MaterialName,
                    cubemapProperty.Property,
                    cubemapProperty.CubemapOriginalMaterials);
            }

            cubemapProperty.ClearCubemapOriginalSnapshot();
            cubemapProperty.TexID = null;
            RemoveCubemapPropertyIfNull(cubemapProperty);
            PurgeUnusedCubemapLeases();
        }

        private bool SetCubemapWithProperty(GameObject go, MaterialCubemapProperty cubemapProperty)
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

            cubemapProperty.SynchronizeCubemapOriginalSnapshot(go);
            return SetCubemap(
                go,
                cubemapProperty.MaterialName,
                cubemapProperty.Property,
                cubemap);
        }

        private MaterialCubemapProperty FindMaterialCubemapProperty(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName)
        {
            var coordinateIndex = GetCoordinateIndex(objectType);
            var materialName = material.NameFormatted();
            return MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ObjectType == objectType
                && x.CoordinateIndex == coordinateIndex
                && x.Slot == slot
                && x.Property == propertyName
                && x.MaterialName == materialName);
        }

        private void RemoveCubemapPropertyIfNull(MaterialCubemapProperty cubemapProperty)
        {
            if (!cubemapProperty.NullCheck())
                return;
            MaterialCubemapPropertyList.Remove(cubemapProperty);
        }
    }
}
