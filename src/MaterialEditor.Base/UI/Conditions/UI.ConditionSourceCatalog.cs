using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    internal static class MaterialConditionSourceCatalog
    {
        internal static Dictionary<string, MaterialConditionSourceKind> Build(
            IEnumerable<MaterialEditorPluginBase.ShaderPropertyData> definitions,
            IEnumerable<MaterialEditorPropertyDescriptor> descriptors)
        {
            return Build(definitions, descriptors, null);
        }

        internal static Dictionary<string, MaterialConditionSourceKind> Build(
            IEnumerable<MaterialEditorPluginBase.ShaderPropertyData> definitions,
            IEnumerable<MaterialEditorPropertyDescriptor> descriptors,
            ICollection<string> requestedSources)
        {
            var result = new Dictionary<string, MaterialConditionSourceKind>(
                StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (requestedSources != null
                    && !requestedSources.Contains(definition.Name))
                    continue;
                if (definition.Type == MaterialAPI.ShaderPropertyType.Float)
                {
                    result[definition.Name] =
                        MaterialConditionSourceKind.Float;
                }
                else if (definition.Type
                         == MaterialAPI.ShaderPropertyType.Keyword)
                {
                    result[definition.Name] =
                        MaterialConditionSourceKind.Keyword;
                }
            }

            foreach (var descriptor in descriptors)
            {
                var propertyName = string.IsNullOrEmpty(
                    descriptor.PropertyName)
                    ? descriptor.Id
                    : descriptor.PropertyName;
                if (requestedSources != null
                    && !requestedSources.Contains(propertyName))
                    continue;
                if (result.ContainsKey(propertyName))
                    continue;
                MaterialAPI.ShaderPropertyType editorType;
                if (ShaderPropertyEditorPolicy.TryGetBuiltInPropertyType(
                        descriptor.EditorId,
                        out editorType)
                    && editorType == MaterialAPI.ShaderPropertyType.Float)
                {
                    result[propertyName] =
                        MaterialConditionSourceKind.Float;
                }
                else if (editorType == MaterialAPI.ShaderPropertyType.Keyword)
                {
                    result[propertyName] =
                        MaterialConditionSourceKind.Keyword;
                }
            }
            return result;
        }
    }
}
