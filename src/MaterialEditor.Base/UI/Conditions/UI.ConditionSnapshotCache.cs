using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
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
}
