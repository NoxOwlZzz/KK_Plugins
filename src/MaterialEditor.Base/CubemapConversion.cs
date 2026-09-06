using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Shared facade for Cubemap import, export and GPU readback.
    /// </summary>
    internal static partial class MaterialEditorCubemapConversion
    {
        private const int MaximumExportFaceSize = 1024;

        internal static CubemapFace ToUnityFace(MaterialEditorCubemapFace face)
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
