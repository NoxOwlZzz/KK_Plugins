using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns the displayed option/value snapshot for one pooled enum row.
    /// Option sources are projected once per binding context; value changes
    /// only move selection while the content fingerprint stays valid.
    /// </summary>
    internal sealed class EnumDropdownOptionCache
    {
        internal const int RetainedOptionDataLimit = 128;

        private enum ProjectionKind
        {
            Known,
            Mixed,
            Unknown
        }

        private readonly Dropdown _dropdown;
        private readonly List<float?> _indexValues = new List<float?>();
        private readonly List<Dropdown.OptionData> _optionData =
            new List<Dropdown.OptionData>();
        private int _projectionSourceCount;
        private ulong _projectionSourceFingerprint;
        private ProjectionKind _projectionKind;
        private bool _hasProjection;
        private bool _lastIsMixed;
        private float _lastValue;
        private bool _hasUnknownText;
        private float _unknownValue;
        private string _unknownText;
        private int _projectionRebuildCount;

        internal EnumDropdownOptionCache(Dropdown dropdown)
        {
            _dropdown = dropdown;
        }

        internal int RetainedOptionDataCount => _optionData.Count;
        internal int IndexValueCapacity => _indexValues.Capacity;
        internal int ProjectionRebuildCount => _projectionRebuildCount;

        internal void Rebuild(
            IList<MaterialEditorEnumOption> options,
            float value,
            bool isMixed)
        {
            var sourceCount = options == null ? 0 : options.Count;
            var sourceFingerprint = ComputeFingerprint(options);
            var sameSource = _hasProjection
                             && sourceCount == _projectionSourceCount
                             && sourceFingerprint == _projectionSourceFingerprint;
            // MaterialEditorEnumOption is immutable, but its public containing
            // list is mutable. Compare content so an in-place replacement with
            // the same count cannot leave a pooled dropdown projection stale.
            if (sameSource
                && _lastIsMixed == isMixed
                && (isMixed || _lastValue.Equals(value)))
            {
                return;
            }

            var kind = GetProjectionKind(options, value, isMixed);
            var projectionChanged = !sameSource
                                    || kind != _projectionKind
                                    || (kind == ProjectionKind.Unknown
                                        && (!_hasUnknownText
                                            || !_unknownValue.Equals(value)));
            int selectedIndex;
            if (projectionChanged)
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.DropdownOptionRebuilds);
                selectedIndex = RebuildProjection(options, value, kind);
                _projectionSourceCount = sourceCount;
                _projectionSourceFingerprint = sourceFingerprint;
                _projectionKind = kind;
                _hasProjection = true;
                _projectionRebuildCount++;
            }
            else
            {
                selectedIndex = FindSelectedIndex(value, kind);
            }

            selectedIndex = Mathf.Max(0, selectedIndex);
            if (projectionChanged || _dropdown.value != selectedIndex)
                _dropdown.Set(selectedIndex);
            MaterialEditorDropdownCaptionFitter.Refresh(_dropdown);
            _lastValue = value;
            _lastIsMixed = isMixed;
        }

        /// <summary>
        /// Drop references to the previous row context while retaining only a
        /// bounded set of neutral OptionData shells for future pooled reuse.
        /// </summary>
        internal void ReleaseContext()
        {
            if (!_hasProjection
                && _indexValues.Count == 0
                && _dropdown.options.Count == 0)
                return;

            _indexValues.Clear();
            _dropdown.options.Clear();
            NeutralizeRetainedTail(0);
            if (_dropdown.options.Capacity > RetainedOptionDataLimit)
                _dropdown.options.Capacity = RetainedOptionDataLimit;
            if (_indexValues.Capacity > RetainedOptionDataLimit)
                _indexValues.Capacity = RetainedOptionDataLimit;

            _projectionSourceCount = 0;
            _projectionSourceFingerprint = 0UL;
            _hasProjection = false;
            _lastIsMixed = false;
            _lastValue = 0f;
            _hasUnknownText = false;
            _unknownValue = 0f;
            _unknownText = null;
        }

        internal bool TryGetValue(int index, out float value)
        {
            if (index < 0
                || index >= _indexValues.Count
                || !_indexValues[index].HasValue)
            {
                value = 0f;
                return false;
            }

            value = _indexValues[index].Value;
            return true;
        }

        private int RebuildProjection(
            IList<MaterialEditorEnumOption> options,
            float value,
            ProjectionKind kind)
        {
            _indexValues.Clear();
            var selectedIndex = -1;
            if (kind == ProjectionKind.Mixed)
            {
                Append("Mixed", null);
                selectedIndex = 0;
            }
            else if (kind == ProjectionKind.Unknown)
            {
                Append(GetUnknownText(value), null);
                selectedIndex = 0;
            }

            if (options != null)
            {
                for (var index = 0; index < options.Count; index++)
                {
                    var option = options[index];
                    if (option == null)
                        continue;
                    if (kind == ProjectionKind.Known && option.Value == value)
                        selectedIndex = _indexValues.Count;
                    Append(option.DisplayName, option.Value);
                }
            }

            var displayedOptions = _dropdown.options;
            if (displayedOptions.Count > _indexValues.Count)
            {
                displayedOptions.RemoveRange(
                    _indexValues.Count,
                    displayedOptions.Count - _indexValues.Count);
            }
            NeutralizeRetainedTail(_indexValues.Count);
            CompactAfterShrink(displayedOptions);
            return selectedIndex;
        }

        private int FindSelectedIndex(float value, ProjectionKind kind)
        {
            if (kind != ProjectionKind.Known)
                return 0;

            var selectedIndex = -1;
            for (var index = 0; index < _indexValues.Count; index++)
            {
                if (_indexValues[index].HasValue
                    && _indexValues[index].Value == value)
                {
                    selectedIndex = index;
                }
            }
            return selectedIndex;
        }

        private static ProjectionKind GetProjectionKind(
            IList<MaterialEditorEnumOption> options,
            float value,
            bool isMixed)
        {
            if (isMixed)
                return ProjectionKind.Mixed;
            return MaterialEditorSemanticValuePolicy.FindEnumOptionIndex(
                       options,
                       value) >= 0
                ? ProjectionKind.Known
                : ProjectionKind.Unknown;
        }

        private static ulong ComputeFingerprint(
            IList<MaterialEditorEnumOption> options)
        {
            unchecked
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;
                var count = options == null ? 0 : options.Count;
                var hash = (offset ^ (uint)count) * prime;
                for (var index = 0; index < count; index++)
                {
                    var option = options[index];
                    if (option == null)
                    {
                        hash = (hash ^ uint.MaxValue) * prime;
                        continue;
                    }

                    hash = (hash ^ (uint)option.Value.GetHashCode()) * prime;
                    var text = option.DisplayName;
                    var length = text == null ? -1 : text.Length;
                    hash = (hash ^ (uint)length) * prime;
                    for (var character = 0; character < length; character++)
                        hash = (hash ^ text[character]) * prime;
                }
                return hash;
            }
        }

        private void Append(string text, float? value)
        {
            var slot = _indexValues.Count;
            var displayedOptions = _dropdown.options;
            Dropdown.OptionData option;
            if (slot < _optionData.Count)
            {
                option = _optionData[slot];
            }
            else
            {
                option = slot < displayedOptions.Count
                    ? displayedOptions[slot]
                    : null;
                if (option == null)
                    option = new Dropdown.OptionData(text);
                if (slot < RetainedOptionDataLimit)
                    _optionData.Add(option);
            }

            option.text = text;
            option.image = null;
            if (slot < displayedOptions.Count)
                displayedOptions[slot] = option;
            else
                displayedOptions.Add(option);
            _indexValues.Add(value);
        }

        private void NeutralizeRetainedTail(int activeCount)
        {
            for (var index = activeCount; index < _optionData.Count; index++)
            {
                _optionData[index].text = string.Empty;
                _optionData[index].image = null;
            }
        }

        private void CompactAfterShrink(List<Dropdown.OptionData> displayedOptions)
        {
            if (_indexValues.Count > RetainedOptionDataLimit)
                return;

            if (_indexValues.Capacity > RetainedOptionDataLimit)
                _indexValues.Capacity = RetainedOptionDataLimit;
            if (displayedOptions.Capacity > RetainedOptionDataLimit)
                displayedOptions.Capacity = RetainedOptionDataLimit;
        }

        private string GetUnknownText(float value)
        {
            // Unknown text is derived dynamically from the current value. Retain
            // exactly one value/string pair per RowView and replace it only when
            // that float changes.
            if (!_hasUnknownText || !_unknownValue.Equals(value))
            {
                _unknownValue = value;
                _unknownText = "Unknown (" + value + ")";
                _hasUnknownText = true;
            }
            return _unknownText;
        }
    }
}
