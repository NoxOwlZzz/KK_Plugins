using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    internal static class ShaderPropertyFallbackPolicy
    {
        internal const string ReservedShaderName = "default";

        internal static bool IsReservedShaderName(string shaderName)
        {
            return string.Equals(
                shaderName,
                ReservedShaderName,
                StringComparison.Ordinal);
        }

        internal static MaterialEditorPluginBase.ShaderPropertyData Create(
            MaterialEditorPluginBase.ShaderPropertyData shaderSpecific)
        {
            if (shaderSpecific == null)
                throw new ArgumentNullException(nameof(shaderSpecific));

            return new MaterialEditorPluginBase.ShaderPropertyData(
                shaderSpecific.Name,
                shaderSpecific.Type)
            {
                DefaultValue = shaderSpecific.DefaultValue,
                DefaultValueAssetBundle = shaderSpecific.DefaultValueAssetBundle,
                AnisoLevel = shaderSpecific.AnisoLevel,
                FilterMode = shaderSpecific.FilterMode,
                WrapMode = shaderSpecific.WrapMode,
                MinValue = shaderSpecific.MinValue,
                MaxValue = shaderSpecific.MaxValue,
                Hidden = shaderSpecific.Hidden,
                Category = shaderSpecific.Category,
                CategoryOrder = null,
                DeclarationOrder = 0,
                DisplayName = shaderSpecific.Name,
                Order = null,
                EditorId = null,
                UiLevel = MaterialEditorPropertyUiLevel.Basic,
                ShowIf = null,
                EnumOptions = new List<MaterialEditorEnumOption>(),
                OffValue = 0f,
                OnValue = 1f
            };
        }

        internal static MaterialEditorPluginBase.ShaderPropertyData MergeInto(
            IDictionary<string, MaterialEditorPluginBase.ShaderPropertyData>
                fallbackProperties,
            MaterialEditorPluginBase.ShaderPropertyData shaderSpecific)
        {
            if (fallbackProperties == null)
                throw new ArgumentNullException(nameof(fallbackProperties));

            var fallback = Create(shaderSpecific);
            fallbackProperties[fallback.Name] = fallback;
            return fallback;
        }
    }
}
