using UnityEngine;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorClipboardPolicy
    {
        internal static bool CanPaste(
            CopyContainer clipboard,
            Material material,
            Projector projector)
        {
            if (clipboard == null || material == null)
                return false;

            if (HasShaderEdit(clipboard)
                || HasKeywordEdit(clipboard)
                || (projector != null && HasProjectorEdit(clipboard)))
                return true;

            return HasCompatibleFloat(clipboard, material)
                   || HasCompatibleColor(clipboard, material)
                   || HasCompatibleVector(clipboard, material)
                   || HasCompatibleTexture(clipboard, material)
                   || HasCompatibleCubemap(clipboard, material);
        }

        private static bool HasShaderEdit(CopyContainer clipboard)
        {
            if (clipboard.MaterialShaderList == null)
                return false;
            for (var index = 0; index < clipboard.MaterialShaderList.Count; index++)
            {
                var edit = clipboard.MaterialShaderList[index];
                if (edit != null && !edit.NullCheck())
                    return true;
            }
            return false;
        }

        private static bool HasKeywordEdit(CopyContainer clipboard)
        {
            if (clipboard.MaterialKeywordPropertyList == null)
                return false;
            for (var index = 0;
                 index < clipboard.MaterialKeywordPropertyList.Count;
                 index++)
            {
                if (clipboard.MaterialKeywordPropertyList[index] != null)
                    return true;
            }
            return false;
        }

        private static bool HasProjectorEdit(CopyContainer clipboard)
        {
            if (clipboard.ProjectorPropertyList == null)
                return false;
            for (var index = 0; index < clipboard.ProjectorPropertyList.Count; index++)
            {
                if (clipboard.ProjectorPropertyList[index] != null)
                    return true;
            }
            return false;
        }

        private static bool HasCompatibleFloat(
            CopyContainer clipboard,
            Material material)
        {
            if (clipboard.MaterialFloatPropertyList == null)
                return false;
            for (var index = 0; index < clipboard.MaterialFloatPropertyList.Count; index++)
            {
                var edit = clipboard.MaterialFloatPropertyList[index];
                if (edit != null && HasProperty(material, edit.Property))
                    return true;
            }
            return false;
        }

        private static bool HasCompatibleColor(
            CopyContainer clipboard,
            Material material)
        {
            if (clipboard.MaterialColorPropertyList == null)
                return false;
            for (var index = 0; index < clipboard.MaterialColorPropertyList.Count; index++)
            {
                var edit = clipboard.MaterialColorPropertyList[index];
                if (edit != null && HasProperty(material, edit.Property))
                    return true;
            }
            return false;
        }

        private static bool HasCompatibleVector(
            CopyContainer clipboard,
            Material material)
        {
            if (clipboard.MaterialVectorPropertyList == null)
                return false;
            for (var index = 0; index < clipboard.MaterialVectorPropertyList.Count; index++)
            {
                var edit = clipboard.MaterialVectorPropertyList[index];
                if (edit != null && HasProperty(material, edit.Property))
                    return true;
            }
            return false;
        }

        private static bool HasCompatibleTexture(
            CopyContainer clipboard,
            Material material)
        {
            if (clipboard.MaterialTexturePropertyList == null)
                return false;
            for (var index = 0; index < clipboard.MaterialTexturePropertyList.Count; index++)
            {
                var edit = clipboard.MaterialTexturePropertyList[index];
                if (edit != null
                    && IsCompatibleTextureProperty(material, edit.Property))
                    return true;
            }
            return false;
        }

        private static bool HasCompatibleCubemap(
            CopyContainer clipboard,
            Material material)
        {
            if (clipboard.MaterialCubemapPropertyList == null)
                return false;
            for (var index = 0;
                 index < clipboard.MaterialCubemapPropertyList.Count;
                 index++)
            {
                var edit = clipboard.MaterialCubemapPropertyList[index];
                if (edit != null
                    && IsCompatibleCubemapProperty(material, edit.Property))
                    return true;
            }
            return false;
        }

        internal static bool IsCompatibleTextureProperty(
            Material material,
            string propertyName)
        {
            return IsCompatibleTextureProperty(
                material,
                propertyName,
                null);
        }

        internal static bool IsCompatibleTextureProperty(
            Material material,
            string propertyName,
            string targetShaderName)
        {
            return IsCompatibleTextureProperty(
                material,
                propertyName,
                targetShaderName,
                MaterialAPI.ShaderPropertyType.Texture);
        }

        internal static bool IsCompatibleCubemapProperty(
            Material material,
            string propertyName)
        {
            return IsCompatibleCubemapProperty(
                material,
                propertyName,
                null);
        }

        internal static bool IsCompatibleCubemapProperty(
            Material material,
            string propertyName,
            string targetShaderName)
        {
            return IsCompatibleTextureProperty(
                material,
                propertyName,
                targetShaderName,
                MaterialAPI.ShaderPropertyType.Cubemap);
        }

        private static bool IsCompatibleTextureProperty(
            Material material,
            string propertyName,
            string targetShaderName,
            MaterialAPI.ShaderPropertyType expectedType)
        {
            if (!HasProperty(material, propertyName))
                return false;

            MaterialEditorPluginBase.ShaderPropertyData definition;
            if (TryGetTargetPropertyDefinition(
                    material,
                    propertyName,
                    targetShaderName,
                    out definition))
            {
                return definition.Type == expectedType;
            }

            // A legacy/unknown shader may not have manifest metadata. Preserve
            // the established Texture2D paste behavior, but never infer that
            // such a property is a Cubemap merely from a matching name.
            return expectedType == MaterialAPI.ShaderPropertyType.Texture;
        }

        private static bool TryGetTargetPropertyDefinition(
            Material material,
            string propertyName,
            string targetShaderName,
            out MaterialEditorPluginBase.ShaderPropertyData definition)
        {
            definition = null;
            if (material == null
                || material.shader == null
                || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var shaderName = string.IsNullOrEmpty(targetShaderName)
                ? material.shader.name
                : targetShaderName;
            shaderName = shaderName == null
                ? string.Empty
                : shaderName
                    .Replace("(Instance)", string.Empty)
                    .Replace(" Instance", string.Empty)
                    .Trim();
            System.Collections.Generic.Dictionary<
                string,
                MaterialEditorPluginBase.ShaderPropertyData> definitions;
            if (!string.IsNullOrEmpty(shaderName)
                && MaterialEditorPluginBase.XMLShaderProperties.TryGetValue(
                    shaderName,
                    out definitions)
                && definitions != null)
            {
                return definitions.TryGetValue(propertyName, out definition);
            }

            // The "default" catalog is a cross-shader union, so it cannot
            // safely distinguish Texture2D from Cubemap for an unknown shader.
            return false;
        }

        private static bool HasProperty(Material material, string propertyName)
        {
            return !string.IsNullOrEmpty(propertyName)
                   && material.HasProperty("_" + propertyName);
        }
    }
}
