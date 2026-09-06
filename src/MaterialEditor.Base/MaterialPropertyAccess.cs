using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Provides target-specific Material property access. PlayHome's Unity version
    /// lacks the integer texture offset/scale overloads; other operations use cached
    /// property IDs on every supported target.
    /// </summary>
    internal static class MaterialPropertyAccess
    {
        internal static bool HasProperty(
            Material material,
            MaterialPropertyHandle property)
        {
            return material.HasProperty(property.Id);
        }

        internal static float GetFloat(
            Material material,
            MaterialPropertyHandle property)
        {
            return material.GetFloat(property.Id);
        }

        internal static void SetFloat(
            Material material,
            MaterialPropertyHandle property,
            float value)
        {
            material.SetFloat(property.Id, value);
        }

        internal static Color GetColor(
            Material material,
            MaterialPropertyHandle property)
        {
            return material.GetColor(property.Id);
        }

        internal static void SetColor(
            Material material,
            MaterialPropertyHandle property,
            Color value)
        {
            material.SetColor(property.Id, value);
        }

        internal static Vector4 GetVector(
            Material material,
            MaterialPropertyHandle property)
        {
            return material.GetVector(property.Id);
        }

        internal static void SetVector(
            Material material,
            MaterialPropertyHandle property,
            Vector4 value)
        {
            material.SetVector(property.Id, value);
        }

        internal static Texture GetTexture(
            Material material,
            MaterialPropertyHandle property)
        {
            return material.GetTexture(property.Id);
        }

        internal static void SetTexture(
            Material material,
            MaterialPropertyHandle property,
            Texture value)
        {
            material.SetTexture(property.Id, value);
        }

        internal static Vector2 GetTextureOffset(
            Material material,
            MaterialPropertyHandle property)
        {
#if PH
            return material.GetTextureOffset(property.FullName);
#else
            return material.GetTextureOffset(property.Id);
#endif
        }

        internal static void SetTextureOffset(
            Material material,
            MaterialPropertyHandle property,
            Vector2 value)
        {
#if PH
            material.SetTextureOffset(property.FullName, value);
#else
            material.SetTextureOffset(property.Id, value);
#endif
        }

        internal static Vector2 GetTextureScale(
            Material material,
            MaterialPropertyHandle property)
        {
#if PH
            return material.GetTextureScale(property.FullName);
#else
            return material.GetTextureScale(property.Id);
#endif
        }

        internal static void SetTextureScale(
            Material material,
            MaterialPropertyHandle property,
            Vector2 value)
        {
#if PH
            material.SetTextureScale(property.FullName, value);
#else
            material.SetTextureScale(property.Id, value);
#endif
        }
    }
}
