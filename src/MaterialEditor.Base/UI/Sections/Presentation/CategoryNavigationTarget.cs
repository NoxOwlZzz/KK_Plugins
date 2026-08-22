using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class CategoryNavigationTarget
    {
        private readonly List<CategoryCollapseState> _collapseStates =
            new List<CategoryCollapseState>();
        private readonly List<int> _rowAnchors = new List<int>();
        private readonly Func<bool> _ensureParentsExpanded;

        internal CategoryNavigationTarget(
            string id,
            string sectionId,
            string name,
            int rowIndex,
            Func<bool> ensureParentsExpanded,
            string tooltipText)
        {
            Id = id;
            SectionId = sectionId;
            Name = name ?? string.Empty;
            RowIndex = -1;
            RecordRowIndex(rowIndex);
            _ensureParentsExpanded = ensureParentsExpanded;
            TooltipText = tooltipText;
        }

        internal string Id { get; }
        internal string SectionId { get; }
        internal string Name { get; }
        internal int RowIndex { get; private set; }
        internal string TooltipText { get; set; }
        internal bool CanCollapse => _collapseStates.Count > 0;

        internal bool Collapsed
        {
            get
            {
                if (_collapseStates.Count == 0)
                    return false;
                foreach (var state in _collapseStates)
                    if (!state.IsCollapsed())
                        return false;
                return true;
            }
        }

        internal void RecordRowIndex(int rowIndex)
        {
            if (rowIndex < 0)
                return;
            if (_rowAnchors.Count == 0
                || _rowAnchors[_rowAnchors.Count - 1] != rowIndex)
                _rowAnchors.Add(rowIndex);
            if (RowIndex < 0 || rowIndex < RowIndex)
                RowIndex = rowIndex;
        }

        internal int FindRowAnchorAtOrBefore(int rowIndex)
        {
            // Manifest and extension properties can contribute non-contiguous
            // spans to the same logical category navigation target.
            var result = -1;
            foreach (var anchor in _rowAnchors)
                if (anchor <= rowIndex && anchor > result)
                    result = anchor;
            return result;
        }

        internal void ShiftRowIndexes(int startIndex, int delta)
        {
            RowIndex = -1;
            for (var index = 0; index < _rowAnchors.Count; index++)
            {
                if (_rowAnchors[index] >= startIndex)
                    _rowAnchors[index] += delta;
                if (RowIndex < 0 || _rowAnchors[index] < RowIndex)
                    RowIndex = _rowAnchors[index];
            }
        }

        internal void AddCollapseState(
            string id,
            Func<bool> isCollapsed,
            Action<bool> setCollapsed)
        {
            foreach (var state in _collapseStates)
                if (state.Id == id)
                    return;
            _collapseStates.Add(new CategoryCollapseState(id, isCollapsed, setCollapsed));
        }

        internal bool EnsureParentsExpanded()
        {
            return _ensureParentsExpanded != null
                   && _ensureParentsExpanded();
        }

        internal void SetCollapsed(bool collapsed)
        {
            foreach (var state in _collapseStates)
                state.SetCollapsed(collapsed);
        }

        private sealed class CategoryCollapseState
        {
            internal CategoryCollapseState(
                string id,
                Func<bool> isCollapsed,
                Action<bool> setCollapsed)
            {
                Id = id;
                IsCollapsed = isCollapsed;
                SetCollapsed = setCollapsed;
            }

            internal string Id { get; }
            internal Func<bool> IsCollapsed { get; }
            internal Action<bool> SetCollapsed { get; }
        }
    }
}
