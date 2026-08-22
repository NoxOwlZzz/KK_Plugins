using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class RendererSectionPresentation
    {
        private static readonly IList<RowModel> EmptyRows = new RowModel[0];
        private readonly Func<IList<RowModel>> _buildChildRows;
        private readonly Action<bool> _collapsedChanged;
        private IList<RowModel> _childRows;

        internal RendererSectionPresentation(
            int ownerToken,
            string id,
            int headerRowIndex,
            bool collapsed,
            Func<IList<RowModel>> buildChildRows,
            Action<bool> collapsedChanged)
        {
            if (ownerToken == 0)
                throw new ArgumentOutOfRangeException(nameof(ownerToken));
            if (headerRowIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(headerRowIndex));
            OwnerToken = ownerToken;
            Id = id ?? string.Empty;
            HeaderRowIndex = headerRowIndex;
            Collapsed = collapsed;
            _buildChildRows = buildChildRows
                              ?? throw new ArgumentNullException(nameof(buildChildRows));
            _collapsedChanged = collapsedChanged
                                ?? throw new ArgumentNullException(nameof(collapsedChanged));
        }

        internal int OwnerToken { get; }
        internal string Id { get; }
        internal int HeaderRowIndex { get; private set; }
        internal bool Collapsed { get; private set; }
        internal bool ChildRowsCreated => _childRows != null;
        internal int CachedChildRowCount => _childRows == null ? 0 : _childRows.Count;
        internal int VisibleChildCount => Collapsed ? 0 : CachedChildRowCount;

        internal IList<RowModel> GetRowsForState(bool collapsed)
        {
            if (collapsed)
                return EmptyRows;
            if (_childRows == null)
                _childRows = _buildChildRows() ?? EmptyRows;
            return _childRows;
        }

        internal void ApplyCollapsed(bool collapsed)
        {
            Collapsed = collapsed;
            _collapsedChanged(collapsed);
        }

        internal void ShiftRowIndex(int startIndex, int delta)
        {
            if (HeaderRowIndex >= startIndex)
                HeaderRowIndex += delta;
        }
    }

}
