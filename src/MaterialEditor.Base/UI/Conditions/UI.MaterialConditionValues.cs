using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

    internal struct MaterialConditionInvalidationHandle :
        IEquatable<MaterialConditionInvalidationHandle>
    {
        private readonly int _ownerToken;
        private readonly MaterialConditionDependencyGraph _graph;
        private readonly int _sourceOrdinal;

        internal MaterialConditionInvalidationHandle(
            int ownerToken,
            MaterialConditionDependencyGraph graph,
            int sourceOrdinal)
        {
            _ownerToken = ownerToken;
            _graph = graph;
            _sourceOrdinal = sourceOrdinal;
        }

        internal int OwnerToken
        {
            get { return _ownerToken; }
        }

        internal MaterialConditionDependencyGraph Graph
        {
            get { return _graph; }
        }

        internal int SourceOrdinal
        {
            get { return _sourceOrdinal; }
        }

        public bool Equals(MaterialConditionInvalidationHandle other)
        {
            return _ownerToken == other._ownerToken
                   && ReferenceEquals(_graph, other._graph)
                   && _sourceOrdinal == other._sourceOrdinal;
        }

        public override bool Equals(object obj)
        {
            return obj is MaterialConditionInvalidationHandle
                   && Equals((MaterialConditionInvalidationHandle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var graphHash = ReferenceEquals(_graph, null)
                    ? 0
                    : RuntimeHelpers.GetHashCode(_graph);
                return ((_ownerToken * 397) ^ graphHash) * 397
                       ^ _sourceOrdinal;
            }
        }
    }

    internal sealed class MaterialConditionPropertyState
    {
        internal MaterialConditionPropertyState(
            int ordinal,
            string propertyName)
        {
            Ordinal = ordinal;
            PropertyName = propertyName ?? string.Empty;
            Visible = true;
            AggregateState = MaterialConditionAggregateState.AllSatisfied;
        }

        internal int Ordinal { get; private set; }
        internal string PropertyName { get; private set; }
        internal bool Visible { get; set; }
        internal MaterialConditionAggregateState AggregateState
        {
            get;
            set;
        }
    }

    internal sealed class MaterialConditionEvaluationResult
    {
        internal bool VisibilityChanged { get; set; }
        internal int DependentsEvaluated { get; set; }
        internal int VisibilityChangeCount { get; set; }
        internal int MixedCount { get; set; }
        internal int MissingCount { get; set; }
    }

    // Scoped to one presentation build or one selective condition batch. The
    // material group is captured by the presenter once, and this cache ensures
    // each changed dependency scans that group at most once per evaluation.
    internal sealed class MaterialEditorConditionValueSnapshotCache
    {
        private readonly Func<string, MaterialConditionValueSnapshot>
            _resolveValues;
        private readonly int _capacity;
        private Dictionary<string, MaterialConditionValueSnapshot> _values;

        internal MaterialEditorConditionValueSnapshotCache(
            Func<string, MaterialConditionValueSnapshot> resolveValues,
            int capacity = 0)
        {
            _resolveValues = resolveValues
                ?? throw new ArgumentNullException(nameof(resolveValues));
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        internal MaterialConditionValueSnapshot Resolve(string propertyName)
        {
            MaterialConditionValueSnapshot value;
            if (_values != null
                && _values.TryGetValue(propertyName, out value))
            {
                return value;
            }

            value = _resolveValues(propertyName);
            if (_values == null)
            {
                _values = new Dictionary<
                    string,
                    MaterialConditionValueSnapshot>(
                    _capacity,
                    StringComparer.Ordinal);
            }
            _values.Add(propertyName, value);
            return value;
        }
    }

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

    // Process-wide warning-once cache. Only bounded ordinal strings are retained;
    // no material, row, target, graph, or other scene reference can escape here.
    internal static class MaterialConditionWarningCache
    {
        private const int Capacity = 256;
        private static readonly object Sync = new object();
        private static readonly HashSet<string> Keys =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly Queue<string> Order = new Queue<string>();

        internal static int Count
        {
            get
            {
                lock (Sync)
                    return Keys.Count;
            }
        }

        internal static bool TryRecord(string key)
        {
            lock (Sync)
            {
                if (!Keys.Add(key))
                    return false;
                Order.Enqueue(key);
                while (Order.Count > Capacity)
                    Keys.Remove(Order.Dequeue());
                return true;
            }
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
