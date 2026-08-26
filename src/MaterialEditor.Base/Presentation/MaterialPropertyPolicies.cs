using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorSemanticValuePolicy
    {
        internal static int FindEnumOptionIndex(
            IList<MaterialEditorEnumOption> options,
            float value)
        {
            if (options == null)
                return -1;
            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                if (option != null && option.Value == value)
                    return index;
            }
            return -1;
        }

        internal static float SelectToggleValue(
            bool enabled,
            float offValue,
            float onValue)
        {
            return enabled ? onValue : offValue;
        }

        internal static void PersistExplicitEnumSelection(
            Action removeOverride,
            Action<float> setOverride,
            float selectedValue)
        {
            if (removeOverride == null)
                throw new ArgumentNullException(nameof(removeOverride));
            if (setOverride == null)
                throw new ArgumentNullException(nameof(setOverride));

            // The legacy backends remove an existing float override when its
            // value matches ValueOriginal. Removing first makes an explicit
            // Mixed-state selection persist even when it has that value.
            removeOverride();
            setOverride(selectedValue);
        }

        internal static void SetVectorComponent(
            ref float x,
            ref float y,
            ref float z,
            ref float w,
            int componentIndex,
            float value)
        {
            switch (componentIndex)
            {
                case 0:
                    x = value;
                    break;
                case 1:
                    y = value;
                    break;
                case 2:
                    z = value;
                    break;
                case 3:
                    w = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentIndex));
            }
        }
    }

    internal static class MaterialEditorMixedStatePerformance
    {
        internal static int GetComponentCopyCount(
            IList<bool> source,
            int destinationLength)
        {
            if (source == null)
                return 0;
            return Math.Min(source.Count, destinationLength);
        }

        internal static bool HasMixed(bool[] values, int count)
        {
            for (var index = 0; index < count; index++)
                if (values[index])
                    return true;
            return false;
        }
    }
}
