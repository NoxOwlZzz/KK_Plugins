using System;

namespace MaterialEditorAPI
{
    internal enum MaterialConditionSourceKind
    {
        Float,
        Keyword
    }

    // ShowIf is evaluated for the complete same-name material group edited by
    // one Material Editor section. A mixed group remains visible (ANY), while
    // an unavailable source remains visible through the existing fail-open
    // policy.
    internal enum MaterialConditionAggregateState
    {
        AllSatisfied,
        NoneSatisfied,
        Mixed,
        Missing
    }

    internal struct MaterialConditionAggregateResult
    {
        internal MaterialConditionAggregateResult(
            MaterialConditionAggregateState state,
            bool visible,
            int satisfiedCount,
            int unsatisfiedCount,
            int missingCount)
        {
            State = state;
            Visible = visible;
            SatisfiedCount = satisfiedCount;
            UnsatisfiedCount = unsatisfiedCount;
            MissingCount = missingCount;
        }

        internal MaterialConditionAggregateState State { get; private set; }
        internal bool Visible { get; private set; }
        internal int SatisfiedCount { get; private set; }
        internal int UnsatisfiedCount { get; private set; }
        internal int MissingCount { get; private set; }
    }

    // Immutable values for one condition source across the material group.
    // Ownership of the supplied array transfers to the snapshot.
    internal sealed class MaterialConditionValueSnapshot
    {
        private static readonly float?[] NoValues = new float?[0];
        private readonly float?[] _values;

        internal MaterialConditionValueSnapshot(float?[] values)
        {
            _values = values ?? NoValues;
        }

        internal int Count
        {
            get { return _values.Length; }
        }

        internal float? this[int index]
        {
            get { return _values[index]; }
        }

        internal static MaterialConditionValueSnapshot Single(float? value)
        {
            return new MaterialConditionValueSnapshot(
                new[] { value });
        }
    }

    internal static class MaterialConditionGroupPolicy
    {
        internal static MaterialConditionAggregateResult Evaluate(
            MaterialEditorPropertyCondition condition,
            MaterialConditionValueSnapshot values,
            bool fallback = true)
        {
            if (condition == null)
            {
                return new MaterialConditionAggregateResult(
                    MaterialConditionAggregateState.AllSatisfied,
                    true,
                    0,
                    0,
                    0);
            }

            try
            {
                if (values == null || values.Count == 0)
                {
                    return new MaterialConditionAggregateResult(
                        MaterialConditionAggregateState.Missing,
                        fallback,
                        0,
                        0,
                        0);
                }

                var satisfied = 0;
                var unsatisfied = 0;
                var missing = 0;
                for (var index = 0; index < values.Count; index++)
                {
                    var value = values[index];
                    if (!value.HasValue)
                    {
                        missing++;
                        continue;
                    }
                    if (condition.Evaluate(value.Value))
                        satisfied++;
                    else
                        unsatisfied++;
                }

                if (satisfied == 0 && unsatisfied == 0)
                {
                    return new MaterialConditionAggregateResult(
                        MaterialConditionAggregateState.Missing,
                        fallback,
                        0,
                        0,
                        missing);
                }

                // A partially unavailable group is intentionally fail-open.
                // It is Mixed rather than Missing because at least one member
                // produced a real condition value.
                if (missing != 0 || (satisfied != 0 && unsatisfied != 0))
                {
                    return new MaterialConditionAggregateResult(
                        MaterialConditionAggregateState.Mixed,
                        satisfied != 0 || fallback,
                        satisfied,
                        unsatisfied,
                        missing);
                }

                if (satisfied != 0)
                {
                    return new MaterialConditionAggregateResult(
                        MaterialConditionAggregateState.AllSatisfied,
                        true,
                        satisfied,
                        0,
                        0);
                }

                return new MaterialConditionAggregateResult(
                    MaterialConditionAggregateState.NoneSatisfied,
                    false,
                    0,
                    unsatisfied,
                    0);
            }
            catch
            {
                return new MaterialConditionAggregateResult(
                    MaterialConditionAggregateState.Missing,
                    fallback,
                    0,
                    0,
                    0);
            }
        }
    }
}
