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

            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.ConditionalEvaluation);
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
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.ConditionalEvaluation,
                    performanceSample);
            }
        }
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
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ConditionResolveCalls);
            MaterialConditionValueSnapshot value;
            if (_values != null
                && _values.TryGetValue(propertyName, out value))
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.ConditionCacheHits);
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

    // One direct dependency graph belongs to one material section in one
    // presentation. Build-time graph traversal is used only for diagnostics;
    // runtime evaluation visits the direct edges of requested source ordinals.
    internal sealed class MaterialConditionDependencyGraph
    {
        private const int CycleDiagnosticBudget = 4096;

        private sealed class Dependency
        {
            internal MaterialEditorPropertyCondition Condition;
            internal MaterialConditionPropertyState Property;
        }

        private sealed class Source
        {
            internal Source(int ordinal, string propertyName)
            {
                Ordinal = ordinal;
                PropertyName = propertyName;
            }

            internal int Ordinal;
            internal string PropertyName;
            internal readonly List<Dependency> Dependents =
                new List<Dependency>();
        }

        private sealed class DiagnosticFrame
        {
            internal DiagnosticFrame(string propertyName, string[] targets)
            {
                PropertyName = propertyName;
                Targets = targets;
            }

            internal string PropertyName;
            internal string[] Targets;
            internal int NextTargetIndex;
        }

        private readonly int _ownerToken;
        private readonly string _diagnosticScope;
        private readonly Func<string, MaterialConditionValueSnapshot>
            _resolveValues;
        private readonly Action<string> _warning;
        private readonly Dictionary<string, Source> _sourcesByName =
            new Dictionary<string, Source>(StringComparer.Ordinal);
        private readonly List<Source> _sources = new List<Source>();
        private int _nextPropertyOrdinal;
        private bool _buildCompleted;

        internal MaterialConditionDependencyGraph(
            int ownerToken,
            string diagnosticScope,
            Func<string, float?> resolveValue,
            Action<string> warning)
            : this(
                ownerToken,
                diagnosticScope,
                WrapSingleValueResolver(resolveValue),
                warning)
        {
        }

        internal MaterialConditionDependencyGraph(
            int ownerToken,
            string diagnosticScope,
            Func<string, MaterialConditionValueSnapshot> resolveValues,
            Action<string> warning)
        {
            if (ownerToken == 0)
                throw new ArgumentOutOfRangeException(nameof(ownerToken));
            _ownerToken = ownerToken;
            _diagnosticScope = diagnosticScope ?? string.Empty;
            _resolveValues = resolveValues
                ?? throw new ArgumentNullException(nameof(resolveValues));
            _warning = warning;
        }

        internal int OwnerToken
        {
            get { return _ownerToken; }
        }

        internal int SourceCount
        {
            get { return _sources.Count; }
        }

        internal int EdgeCount { get; private set; }

        internal MaterialConditionPropertyState RegisterProperty(
            string propertyName,
            MaterialEditorPropertyCondition visibilityCondition,
            Func<string, float?> initialResolveValue)
        {
            return RegisterProperty(
                propertyName,
                visibilityCondition,
                WrapSingleValueResolver(initialResolveValue));
        }

        internal MaterialConditionPropertyState RegisterProperty(
            string propertyName,
            MaterialEditorPropertyCondition visibilityCondition,
            Func<string, MaterialConditionValueSnapshot>
                initialResolveValues)
        {
            if (_buildCompleted)
                throw new InvalidOperationException(
                    "Condition graph construction is already complete.");

            var property = new MaterialConditionPropertyState(
                _nextPropertyOrdinal++,
                propertyName);

            if (visibilityCondition != null)
            {
                var aggregate = EvaluateCondition(
                    visibilityCondition,
                    initialResolveValues);
                property.Visible = aggregate.Visible;
                property.AggregateState = aggregate.State;
                AddDependency(
                    visibilityCondition,
                    property);
            }

            return property;
        }

        internal void CompleteBuild(
            IDictionary<string, MaterialConditionSourceKind> sourceKinds,
            ICollection<string> knownPropertyNames)
        {
            if (_buildCompleted)
                return;
            _buildCompleted = true;
            if (_warning == null)
                return;

            for (var sourceIndex = 0;
                 sourceIndex < _sources.Count;
                 sourceIndex++)
            {
                var source = _sources[sourceIndex];
                if (!sourceKinds.ContainsKey(source.PropertyName))
                {
                    var reason = knownPropertyNames.Contains(source.PropertyName)
                        ? "incompatible"
                        : "missing";
                    WarnOnce(
                        reason + "|" + _diagnosticScope + "|"
                        + source.PropertyName,
                        "Material Editor condition source '"
                        + source.PropertyName + "' is " + reason
                        + " for '" + _diagnosticScope
                        + "'; conditions remain fail-open.");
                }

                for (var edgeIndex = 0;
                     edgeIndex < source.Dependents.Count;
                     edgeIndex++)
                {
                    var dependent = source.Dependents[edgeIndex].Property;
                    if (source.PropertyName != dependent.PropertyName)
                        continue;
                    WarnOnce(
                        "self|" + _diagnosticScope + "|"
                        + source.PropertyName + "|" + dependent.Ordinal,
                        "Material Editor condition self-reference '"
                        + source.PropertyName + "' was detected for '"
                        + _diagnosticScope
                        + "'; runtime evaluation remains direct.");
                }
            }

            if (_sources.Count > CycleDiagnosticBudget
                || EdgeCount > CycleDiagnosticBudget)
            {
                WarnOnce(
                    "cycle-budget|" + _diagnosticScope,
                    "Material Editor condition cycle diagnostics for '"
                    + _diagnosticScope + "' exceeded the bounded "
                    + CycleDiagnosticBudget
                    + "-node/edge budget; runtime evaluation remains direct.");
                return;
            }

            var cycleSignature = BuildGraphSignature();
            if (MaterialConditionWarningCache.TryRecord(
                    "cycle-scan|" + _diagnosticScope + "|"
                    + cycleSignature))
                DiagnoseCycles();
        }

        internal bool TryCreateHandle(
            string propertyName,
            out MaterialConditionInvalidationHandle handle)
        {
            Source source;
            if (string.IsNullOrEmpty(propertyName)
                || !_buildCompleted
                || !_sourcesByName.TryGetValue(propertyName, out source))
            {
                handle = new MaterialConditionInvalidationHandle();
                return false;
            }

            handle = new MaterialConditionInvalidationHandle(
                _ownerToken,
                this,
                source.Ordinal);
            return true;
        }

        internal MaterialConditionEvaluationResult EvaluateHandles(
            IEnumerable<MaterialConditionInvalidationHandle> handles)
        {
            if (!_buildCompleted)
                throw new InvalidOperationException(
                    "Condition graph construction is not complete.");

            var result = new MaterialConditionEvaluationResult();
            if (handles == null || _sources.Count == 0)
                return result;

            var seen = new HashSet<int>();
            var sourceOrdinals = new List<int>();
            foreach (var handle in handles)
            {
                var sourceOrdinal = handle.SourceOrdinal;
                if (handle.OwnerToken != _ownerToken
                    || !ReferenceEquals(handle.Graph, this)
                    || sourceOrdinal < 0
                    || sourceOrdinal >= _sources.Count
                    || !seen.Add(sourceOrdinal))
                    continue;
                sourceOrdinals.Add(sourceOrdinal);
            }
            if (sourceOrdinals.Count == 0)
                return result;

            var values = new MaterialEditorConditionValueSnapshotCache(
                _resolveValues,
                sourceOrdinals.Count);
            Func<string, MaterialConditionValueSnapshot> resolve =
                values.Resolve;
            foreach (var sourceOrdinal in sourceOrdinals)
            {
                var source = _sources[sourceOrdinal];
                for (var index = 0;
                     index < source.Dependents.Count;
                     index++)
                {
                    var dependent = source.Dependents[index];
                    var next = EvaluateCondition(
                        dependent.Condition,
                        resolve);
                    result.DependentsEvaluated++;
                    if (next.State == MaterialConditionAggregateState.Mixed)
                        result.MixedCount++;
                    else if (next.State
                             == MaterialConditionAggregateState.Missing)
                        result.MissingCount++;

                    if (next.Visible == dependent.Property.Visible)
                        continue;
                    result.VisibilityChanged = true;
                    result.VisibilityChangeCount++;
                }
            }

            return result;
        }

        private static MaterialConditionAggregateResult EvaluateCondition(
            MaterialEditorPropertyCondition condition,
            Func<string, MaterialConditionValueSnapshot> resolveValues)
        {
            try
            {
                return MaterialConditionGroupPolicy.Evaluate(
                    condition,
                    resolveValues == null
                        ? null
                        : resolveValues(condition.PropertyName),
                    true);
            }
            catch
            {
                return new MaterialConditionAggregateResult(
                    MaterialConditionAggregateState.Missing,
                    true,
                    0,
                    0,
                    0);
            }
        }

        private static Func<string, MaterialConditionValueSnapshot>
            WrapSingleValueResolver(Func<string, float?> resolveValue)
        {
            if (resolveValue == null)
                throw new ArgumentNullException(nameof(resolveValue));
            return propertyName => MaterialConditionValueSnapshot.Single(
                resolveValue(propertyName));
        }

        private string BuildGraphSignature()
        {
            unchecked
            {
                uint hash = 2166136261;
                for (var sourceIndex = 0;
                     sourceIndex < _sources.Count;
                     sourceIndex++)
                {
                    var source = _sources[sourceIndex];
                    AddSignatureValue(ref hash, source.PropertyName);
                    for (var edgeIndex = 0;
                         edgeIndex < source.Dependents.Count;
                         edgeIndex++)
                    {
                        var edge = source.Dependents[edgeIndex];
                        AddSignatureValue(
                            ref hash,
                            edge.Property.PropertyName);
                    }
                }
                return _sources.Count + ":" + EdgeCount + ":"
                       + hash.ToString("X8");
            }
        }

        private static void AddSignatureValue(ref uint hash, string value)
        {
            unchecked
            {
                for (var index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= 16777619;
                }
                hash ^= 0xff;
                hash *= 16777619;
            }
        }

        private void AddDependency(
            MaterialEditorPropertyCondition condition,
            MaterialConditionPropertyState property)
        {
            Source source;
            if (!_sourcesByName.TryGetValue(
                    condition.PropertyName,
                    out source))
            {
                source = new Source(
                    _sources.Count,
                    condition.PropertyName);
                _sourcesByName.Add(source.PropertyName, source);
                _sources.Add(source);
            }

            source.Dependents.Add(new Dependency
            {
                Condition = condition,
                Property = property
            });
            EdgeCount++;
        }

        private void DiagnoseCycles()
        {
            var adjacency = new Dictionary<string, HashSet<string>>(
                StringComparer.Ordinal);
            for (var sourceIndex = 0;
                 sourceIndex < _sources.Count;
                 sourceIndex++)
            {
                var source = _sources[sourceIndex];
                HashSet<string> targets;
                if (!adjacency.TryGetValue(source.PropertyName, out targets))
                {
                    targets = new HashSet<string>(StringComparer.Ordinal);
                    adjacency.Add(source.PropertyName, targets);
                }

                for (var edgeIndex = 0;
                     edgeIndex < source.Dependents.Count;
                     edgeIndex++)
                {
                    var target = source.Dependents[edgeIndex]
                        .Property.PropertyName;
                    if (target != source.PropertyName
                        && _sourcesByName.ContainsKey(target))
                        targets.Add(target);
                }
            }

            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var source in _sources)
            {
                int sourceState;
                if (state.TryGetValue(source.PropertyName, out sourceState)
                    && sourceState != 0)
                    continue;
                DiagnoseCyclesFrom(source.PropertyName, adjacency, state);
            }
        }

        private void DiagnoseCyclesFrom(
            string propertyName,
            IDictionary<string, HashSet<string>> adjacency,
            IDictionary<string, int> state)
        {
            var stack = new List<DiagnosticFrame>();
            state[propertyName] = 1;
            stack.Add(new DiagnosticFrame(
                propertyName,
                GetDiagnosticTargets(propertyName, adjacency)));

            while (stack.Count != 0)
            {
                var frame = stack[stack.Count - 1];
                if (frame.NextTargetIndex >= frame.Targets.Length)
                {
                    state[frame.PropertyName] = 2;
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }

                var target = frame.Targets[frame.NextTargetIndex++];
                int targetState;
                state.TryGetValue(target, out targetState);
                if (targetState == 0)
                {
                    state[target] = 1;
                    stack.Add(new DiagnosticFrame(
                        target,
                        GetDiagnosticTargets(target, adjacency)));
                    continue;
                }
                if (targetState != 1)
                    continue;

                var cycle = new List<string>();
                for (var index = stack.Count - 1; index >= 0; index--)
                {
                    cycle.Add(stack[index].PropertyName);
                    if (stack[index].PropertyName == target)
                        break;
                }
                cycle.Sort(StringComparer.Ordinal);
                var cycleKey = BuildCycleSignature(cycle);
                WarnOnce(
                    "cycle|" + _diagnosticScope + "|" + cycleKey,
                    "Material Editor condition cycle ["
                    + cycleKey + "] was detected for '"
                    + _diagnosticScope
                    + "'; runtime evaluation remains direct.");
            }
        }

        private static string[] GetDiagnosticTargets(
            string propertyName,
            IDictionary<string, HashSet<string>> adjacency)
        {
            HashSet<string> targets;
            if (!adjacency.TryGetValue(propertyName, out targets)
                || targets.Count == 0)
                return new string[0];
            var result = new string[targets.Count];
            targets.CopyTo(result);
            return result;
        }

        private static string BuildCycleSignature(IList<string> cycle)
        {
            const int displayedNameCount = 8;
            unchecked
            {
                uint hash = 2166136261;
                for (var index = 0; index < cycle.Count; index++)
                {
                    var value = cycle[index];
                    for (var character = 0;
                         character < value.Length;
                         character++)
                    {
                        hash ^= value[character];
                        hash *= 16777619;
                    }
                    hash ^= 0xff;
                    hash *= 16777619;
                }

                var displayed = new List<string>(Math.Min(
                    displayedNameCount,
                    cycle.Count));
                for (var index = 0;
                     index < cycle.Count && index < displayedNameCount;
                     index++)
                    displayed.Add(cycle[index]);
                var names = string.Join(",", displayed.ToArray());
                if (cycle.Count > displayedNameCount)
                    names += ",...";
                return names + "|count=" + cycle.Count
                       + "|hash=" + hash.ToString("X8");
            }
        }

        private void WarnOnce(string key, string message)
        {
            if (_warning == null
                || !MaterialConditionWarningCache.TryRecord(key))
                return;
            try
            {
                _warning(message);
            }
            catch
            {
                // Diagnostics must never alter condition presentation semantics.
            }
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

        internal static void ResetForTests()
        {
            lock (Sync)
            {
                Keys.Clear();
                Order.Clear();
            }
        }
    }
}
