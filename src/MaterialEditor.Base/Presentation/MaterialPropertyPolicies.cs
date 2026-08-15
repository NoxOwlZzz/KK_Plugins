using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorConditionPolicy
    {
        internal static bool Evaluate(
            MaterialEditorPropertyCondition condition,
            Func<string, float?> resolveValue,
            bool fallback = true)
        {
            if (condition == null)
                return true;
            if (resolveValue == null)
                return fallback;
            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.ConditionalEvaluation);
            try
            {
                var value = resolveValue(condition.PropertyName);
                return value.HasValue ? condition.Evaluate(value.Value) : fallback;
            }
            catch
            {
                return fallback;
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.ConditionalEvaluation,
                    performanceSample);
            }
        }
    }

    // Scoped to one material presentation rebuild. Successful values (including
    // an unavailable/null value) are read once; exceptions are deliberately not
    // retained so the existing fail-open policy can retry a transient failure.
    internal sealed class MaterialEditorConditionValueCache
    {
        private readonly Func<string, float?> _resolveValue;
        private readonly int _capacity;
        private Dictionary<string, float?> _values;

        internal MaterialEditorConditionValueCache(
            Func<string, float?> resolveValue,
            int capacity = 0)
        {
            _resolveValue = resolveValue
                ?? throw new ArgumentNullException(nameof(resolveValue));
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        internal float? Resolve(string propertyName)
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ConditionResolveCalls);
            float? value;
            if (_values != null && _values.TryGetValue(propertyName, out value))
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.ConditionCacheHits);
                return value;
            }

            value = _resolveValue(propertyName);
            if (_values == null)
                _values = new Dictionary<string, float?>(
                    _capacity,
                    StringComparer.Ordinal);
            _values.Add(propertyName, value);
            return value;
        }
    }

    internal static class MaterialEditorPropertyVisibilityPolicy
    {
        internal static bool IsVisible(
            bool hidden,
            bool visibilityConditionSatisfied)
        {
            // Hidden remains an explicit authoring exclusion and ShowIf is the
            // only dynamic condition allowed to remove a property from view.
            return !hidden && visibilityConditionSatisfied;
        }
    }

    /// <summary>
    /// Pure, allocation-free decisions shared by the presenter and regression
    /// tests. Keeping these decisions outside Unity UI guarantees that a hidden
    /// property never needs a RowModel or RowView.
    /// </summary>
    internal static class MaterialEditorPropertyPresentationPolicy
    {
        internal static bool ShouldCreateModel(
            bool hidden,
            bool compatible,
            bool blacklisted,
            bool visibilityConditionSatisfied,
            bool searchMatches)
        {
            return compatible
                   && !blacklisted
                   && searchMatches
                   && MaterialEditorPropertyVisibilityPolicy.IsVisible(
                       hidden,
                       visibilityConditionSatisfied);
        }
    }

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
}
