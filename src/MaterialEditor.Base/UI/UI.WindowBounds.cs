namespace MaterialEditorAPI
{
    internal enum MaterialEditorWindowDragMode
    {
        NoLimits,
        KeepHeaderInside,
        KeepWholeWindowInside
    }

    internal struct MaterialEditorWindowDragBounds
    {
        internal MaterialEditorWindowDragBounds(
            float minimumX,
            float maximumX,
            float minimumY,
            float maximumY)
        {
            MinimumX = minimumX;
            MaximumX = maximumX;
            MinimumY = minimumY;
            MaximumY = maximumY;
        }

        internal float MinimumX { get; }
        internal float MaximumX { get; }
        internal float MinimumY { get; }
        internal float MaximumY { get; }

        internal void Clamp(ref float x, ref float y)
        {
            x = Clamp(x, MinimumX, MaximumX);
            y = Clamp(y, MinimumY, MaximumY);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (maximum < minimum)
                return maximum;
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }
    }

    internal static class MaterialEditorWindowBoundsPolicy
    {
        internal static MaterialEditorWindowDragMode FromLegacy(
            bool preventDragout)
        {
            return preventDragout
                ? MaterialEditorWindowDragMode.KeepHeaderInside
                : MaterialEditorWindowDragMode.NoLimits;
        }

        internal static void ClampDragOffset(
            MaterialEditorWindowDragMode mode,
            MaterialEditorWindowDragBounds headerBounds,
            MaterialEditorWindowDragBounds wholeBounds,
            ref float x,
            ref float y)
        {
            switch (mode)
            {
                case MaterialEditorWindowDragMode.KeepHeaderInside:
                    headerBounds.Clamp(ref x, ref y);
                    break;
                case MaterialEditorWindowDragMode.KeepWholeWindowInside:
                    wholeBounds.Clamp(ref x, ref y);
                    break;
            }
        }

        internal static bool ToLegacyBoolean(
            MaterialEditorWindowDragMode mode)
        {
            return mode != MaterialEditorWindowDragMode.NoLimits;
        }
    }
}
