using System;
using System.Globalization;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Canonical mapping between shader storage types and built-in property
    /// editor identifiers. Manifest parsing and presentation adapters must use
    /// this policy instead of maintaining parallel string/type tables.
    /// </summary>
    internal static class ShaderPropertyEditorPolicy
    {
        internal static string GetDefaultEditorId(ShaderPropertyType type)
        {
            switch (type)
            {
                case ShaderPropertyType.Texture:
                    return MaterialEditorPropertyEditorIds.Texture;
                case ShaderPropertyType.Cubemap:
                    return MaterialEditorPropertyEditorIds.Cubemap;
                case ShaderPropertyType.Color:
                    return MaterialEditorPropertyEditorIds.Color;
                case ShaderPropertyType.Float:
                    return MaterialEditorPropertyEditorIds.Float;
                case ShaderPropertyType.Keyword:
                    return MaterialEditorPropertyEditorIds.Boolean;
                case ShaderPropertyType.Vector:
                    return MaterialEditorPropertyEditorIds.Vector4;
                default:
                    return string.Empty;
            }
        }

        internal static bool TryGetBuiltInPropertyType(
            string editorId,
            out ShaderPropertyType type)
        {
            if (editorId == MaterialEditorPropertyEditorIds.Texture)
            {
                type = ShaderPropertyType.Texture;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Cubemap)
            {
                type = ShaderPropertyType.Cubemap;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Color)
            {
                type = ShaderPropertyType.Color;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Float
                || editorId == MaterialEditorPropertyEditorIds.Enum
                || editorId == MaterialEditorPropertyEditorIds.Toggle)
            {
                type = ShaderPropertyType.Float;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Boolean)
            {
                type = ShaderPropertyType.Keyword;
                return true;
            }
            if (editorId == MaterialEditorPropertyEditorIds.Vector2
                || editorId == MaterialEditorPropertyEditorIds.Vector3
                || editorId == MaterialEditorPropertyEditorIds.Vector4)
            {
                type = ShaderPropertyType.Vector;
                return true;
            }

            type = default(ShaderPropertyType);
            return false;
        }

        internal static bool IsKnownEditorId(string editorId)
        {
            ShaderPropertyType ignored;
            return TryGetBuiltInPropertyType(editorId, out ignored);
        }

        internal static bool IsCompatible(
            string editorId,
            ShaderPropertyType propertyType)
        {
            if (string.IsNullOrEmpty(editorId))
                return true;

            if (editorId == MaterialEditorPropertyEditorIds.Vector2
                || editorId == MaterialEditorPropertyEditorIds.Vector3
                || editorId == MaterialEditorPropertyEditorIds.Vector4)
            {
                // Color remains accepted as a deliberate compatibility bridge
                // for manifests that previously stored four-float data as Color.
                return propertyType == ShaderPropertyType.Vector
                       || propertyType == ShaderPropertyType.Color;
            }

            ShaderPropertyType editorType;
            return !TryGetBuiltInPropertyType(editorId, out editorType)
                   || editorType == propertyType;
        }

        internal static bool TryNormalizeManifestEditor(
            string value,
            int? requestedVectorComponentCount,
            out string editorId,
            out int? vectorComponentCount)
        {
            editorId = null;
            vectorComponentCount = requestedVectorComponentCount;
            if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                return false;

            var editor = value.Trim();
            if (EqualsAny(editor, "Float", MaterialEditorPropertyEditorIds.Float))
                editorId = MaterialEditorPropertyEditorIds.Float;
            else if (EqualsAny(editor, "Color", MaterialEditorPropertyEditorIds.Color))
                editorId = MaterialEditorPropertyEditorIds.Color;
            else if (EqualsAny(editor, "Texture", MaterialEditorPropertyEditorIds.Texture))
                editorId = MaterialEditorPropertyEditorIds.Texture;
            else if (EqualsAny(editor, "Cubemap", MaterialEditorPropertyEditorIds.Cubemap))
                editorId = MaterialEditorPropertyEditorIds.Cubemap;
            else if (EqualsAny(
                         editor,
                         "Keyword",
                         "Boolean",
                         MaterialEditorPropertyEditorIds.Boolean))
                editorId = MaterialEditorPropertyEditorIds.Boolean;
            else if (EqualsAny(editor, "Enum", MaterialEditorPropertyEditorIds.Enum))
                editorId = MaterialEditorPropertyEditorIds.Enum;
            else if (EqualsAny(editor, "Vector2", MaterialEditorPropertyEditorIds.Vector2))
            {
                editorId = MaterialEditorPropertyEditorIds.Vector2;
                vectorComponentCount = 2;
            }
            else if (EqualsAny(editor, "Vector3", MaterialEditorPropertyEditorIds.Vector3))
            {
                editorId = MaterialEditorPropertyEditorIds.Vector3;
                vectorComponentCount = 3;
            }
            else if (EqualsAny(editor, "Vector4", MaterialEditorPropertyEditorIds.Vector4))
            {
                editorId = MaterialEditorPropertyEditorIds.Vector4;
                vectorComponentCount = 4;
            }
            else if (string.Equals(editor, "Vector", StringComparison.OrdinalIgnoreCase))
            {
                var count = requestedVectorComponentCount ?? 4;
                vectorComponentCount = count;
                editorId = "materialeditor.vector"
                           + count.ToString(CultureInfo.InvariantCulture);
            }

            return editorId != null;
        }

        private static bool EqualsAny(string value, params string[] candidates)
        {
            for (var index = 0; index < candidates.Length; index++)
            {
                if (string.Equals(
                        value,
                        candidates[index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
