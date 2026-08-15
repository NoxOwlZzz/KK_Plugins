namespace MaterialEditorAPI
{
    internal static class FloatPropertyRangePolicy
    {
        internal static bool HasUsableRange(float? minimum, float? maximum)
        {
            return minimum.HasValue
                   && maximum.HasValue
                   && !float.IsNaN(minimum.Value)
                   && !float.IsInfinity(minimum.Value)
                   && !float.IsNaN(maximum.Value)
                   && !float.IsInfinity(maximum.Value)
                   && maximum.Value > minimum.Value;
        }
    }
}
