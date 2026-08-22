using System;
using System.Runtime.CompilerServices;

namespace MaterialEditorAPI
{
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
}
