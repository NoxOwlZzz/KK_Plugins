using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorSectionKeys
    {
        internal static string Renderer(
            GameObject gameObject,
            UnityEngine.Renderer renderer,
            string relativePath,
            int componentIndex)
        {
            var path = relativePath ?? string.Empty;
            return GetInstanceId(gameObject)
                   + ":renderer:"
                   + path.Length
                   + ":"
                   + path
                   + ":"
                   + componentIndex
                   + ":"
                   + GetInstanceId(renderer);
        }

        internal static string Material(GameObject gameObject, UnityEngine.Material material)
        {
            return GetInstanceId(gameObject) + ":" + GetInstanceId(material);
        }

        internal static string Shader(
            GameObject gameObject,
            UnityEngine.Material material,
            string shaderName)
        {
            return Material(gameObject, material) + ":" + (shaderName ?? string.Empty);
        }

        internal static string Category(
            GameObject gameObject,
            UnityEngine.Material material,
            string shaderName,
            string source,
            string categoryName)
        {
            return Shader(gameObject, material, shaderName)
                   + ":" + (source ?? string.Empty)
                   + ":" + (categoryName ?? string.Empty);
        }

        internal static string Subcategory(
            GameObject gameObject,
            UnityEngine.Material material,
            string shaderName,
            string source,
            string categoryId,
            string subcategoryId)
        {
            return Category(
                       gameObject,
                       material,
                       shaderName,
                       source,
                       categoryId)
                   + ":subcategory:"
                   + (subcategoryId ?? string.Empty);
        }

        private static int GetInstanceId(UnityEngine.Object value)
        {
            return value == null ? 0 : value.GetInstanceID();
        }
    }

    internal sealed class MaterialEditorPresentation
    {
        private HashSet<MaterialConditionDependencyGraph> _conditionGraphs;

        internal MaterialEditorPresentation(int ownerToken)
        {
            if (ownerToken == 0)
                throw new ArgumentOutOfRangeException(nameof(ownerToken));
            OwnerToken = ownerToken;
        }

        internal int OwnerToken { get; private set; }
        internal readonly List<RowModel> Rows = new List<RowModel>();
        internal readonly List<RendererSectionPresentation> RendererSections =
            new List<RendererSectionPresentation>();
        internal readonly List<MaterialSectionPresentation> MaterialSections =
            new List<MaterialSectionPresentation>();
        internal bool HasActiveFilter;
        // Renderer/material filters narrow the target set but do not flatten
        // property hierarchy. Keep that distinction explicit so category
        // disclosure remains available unless property search is active.
        internal bool HasPropertyFilter;

        internal void RegisterConditionGraph(
            MaterialConditionDependencyGraph graph)
        {
            if (graph == null || graph.OwnerToken != OwnerToken)
                throw new ArgumentException(
                    "Condition graph does not belong to this presentation.",
                    nameof(graph));
            if (_conditionGraphs == null)
            {
                _conditionGraphs =
                    new HashSet<MaterialConditionDependencyGraph>();
            }
            _conditionGraphs.Add(graph);
        }

        internal bool Owns(MaterialConditionInvalidationHandle handle)
        {
            return handle.OwnerToken == OwnerToken
                   && handle.Graph != null
                   && _conditionGraphs != null
                   && _conditionGraphs.Contains(handle.Graph);
        }

        internal bool Owns(RendererSectionPresentation section)
        {
            return section != null
                   && section.OwnerToken == OwnerToken
                   && RendererSections.Contains(section);
        }

        internal bool TrySetRendererCollapsed(
            RendererSectionPresentation section,
            bool collapsed,
            out int replaceStartIndex,
            out int removeCount,
            out IList<RowModel> replacementRows)
        {
            replaceStartIndex = -1;
            removeCount = 0;
            replacementRows = null;
            if (!Owns(section) || section.Collapsed == collapsed)
                return false;

            replaceStartIndex = section.HeaderRowIndex + 1;
            removeCount = section.VisibleChildCount;
            replacementRows = section.GetRowsForState(collapsed);
            section.ApplyCollapsed(collapsed);

            if (removeCount != 0)
                Rows.RemoveRange(replaceStartIndex, removeCount);
            if (replacementRows.Count != 0)
                Rows.InsertRange(replaceStartIndex, replacementRows);

            ShiftRowIndexes(
                replaceStartIndex,
                replacementRows.Count - removeCount);
            return true;
        }

        internal void ShiftRowIndexes(int startIndex, int delta)
        {
            if (delta == 0)
                return;
            foreach (var rendererSection in RendererSections)
                rendererSection.ShiftRowIndex(startIndex, delta);
            foreach (var materialSection in MaterialSections)
                materialSection.ShiftRowIndexes(startIndex, delta);
        }

        internal MaterialSectionPresentation FindSectionAtRow(int rowIndex)
        {
            foreach (var section in MaterialSections)
            {
                if (rowIndex < section.MaterialRowIndex)
                    break;
                if (section.MaterialRowIndex >= 0
                    && section.MaterialRowIndex < Rows.Count
                    && Rows[section.MaterialRowIndex] is MaterialRowModel
                    && rowIndex <= section.EndRowIndex)
                    return section;
            }
            return null;
        }

        internal CategoryNavigationTarget FindCategory(
            string sectionId,
            string categoryId)
        {
            foreach (var section in MaterialSections)
            {
                if (section.Id != sectionId)
                    continue;
                foreach (var category in section.Categories)
                    if (category.Id == categoryId)
                        return category;
            }
            return null;
        }

        internal bool AllCategoriesCollapsed
        {
            get
            {
                var hasCategories = false;
                foreach (var section in MaterialSections)
                {
                    if (section.Categories.Count == 0)
                        continue;
                    hasCategories = true;
                    if (!section.AllCategoriesCollapsed)
                        return false;
                }
                return hasCategories;
            }
        }

        internal void SetAllCategoriesCollapsed(bool collapsed)
        {
            foreach (var section in MaterialSections)
                section.SetAllCategoriesCollapsed(collapsed);
        }

        internal bool HasCollapsibleSections
        {
            get
            {
                if (RendererSections.Count != 0)
                    return true;
                foreach (var section in MaterialSections)
                    if (section.CanCollapse)
                        return true;
                return false;
            }
        }

        internal bool CanToggleSections =>
            !HasPropertyFilter && HasCollapsibleSections;

        internal bool AllSectionsCollapsed
        {
            get
            {
                var hasSections = false;
                foreach (var section in RendererSections)
                {
                    hasSections = true;
                    if (!section.Collapsed)
                        return false;
                }
                foreach (var section in MaterialSections)
                {
                    if (!section.CanCollapse)
                        continue;
                    hasSections = true;
                    if (!section.Collapsed)
                        return false;
                }
                return hasSections;
            }
        }

        internal void SetAllSectionsCollapsed(bool collapsed)
        {
            foreach (var section in RendererSections)
                if (section.Collapsed != collapsed)
                    section.ApplyCollapsed(collapsed);
            foreach (var section in MaterialSections)
                if (section.CanCollapse && section.Collapsed != collapsed)
                    section.SetCollapsed(collapsed);
        }
    }

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

    internal sealed class MaterialSectionPresentation
    {
        private readonly Func<bool> _ensureParentsExpanded;
        private readonly Func<bool> _isCollapsed;
        private readonly Action<bool> _setCollapsed;

        internal MaterialSectionPresentation(
            string id,
            string materialName,
            string shaderName,
            int materialRowIndex,
            Func<bool> ensureParentsExpanded,
            Func<bool> isCollapsed = null,
            Action<bool> setCollapsed = null)
        {
            Id = id;
            MaterialName = materialName ?? string.Empty;
            ShaderName = shaderName ?? string.Empty;
            MaterialRowIndex = materialRowIndex;
            EndRowIndex = materialRowIndex;
            _ensureParentsExpanded = ensureParentsExpanded;
            _isCollapsed = isCollapsed;
            _setCollapsed = setCollapsed;
        }

        internal string Id { get; }
        internal string MaterialName { get; }
        internal string ShaderName { get; }
        internal int MaterialRowIndex { get; private set; }
        internal int EndRowIndex { get; set; }
        internal MaterialConditionDependencyGraph ConditionGraph { get; set; }
        internal bool CanCollapse => _isCollapsed != null
                                     && _setCollapsed != null;
        internal bool Collapsed => CanCollapse && _isCollapsed();
        internal readonly List<CategoryNavigationTarget> Categories =
            new List<CategoryNavigationTarget>();

        internal void SetCollapsed(bool collapsed)
        {
            if (CanCollapse)
                _setCollapsed(collapsed);
        }

        internal CategoryNavigationTarget AddCategory(
            string name,
            int rowIndex,
            string stateId,
            Func<bool> isCollapsed,
            Action<bool> setCollapsed,
            string tooltipText)
        {
            return AddCategory(
                name,
                name,
                rowIndex,
                stateId,
                isCollapsed,
                setCollapsed,
                tooltipText);
        }

        internal CategoryNavigationTarget AddCategory(
            string identity,
            string name,
            int rowIndex,
            string stateId,
            Func<bool> isCollapsed,
            Action<bool> setCollapsed,
            string tooltipText)
        {
            var targetId = Id + ":" + (identity ?? string.Empty);
            CategoryNavigationTarget target = null;
            foreach (var existing in Categories)
            {
                if (existing.Id == targetId)
                {
                    target = existing;
                    break;
                }
            }

            if (target == null)
            {
                target = new CategoryNavigationTarget(
                    targetId,
                    Id,
                    name,
                    rowIndex,
                    _ensureParentsExpanded,
                    tooltipText);
                Categories.Add(target);
            }
            else
            {
                target.RecordRowIndex(rowIndex);
                if (string.IsNullOrEmpty(target.TooltipText))
                    target.TooltipText = tooltipText;
            }

            if (isCollapsed != null && setCollapsed != null)
                target.AddCollapseState(stateId, isCollapsed, setCollapsed);
            return target;
        }

        internal bool AllCategoriesCollapsed
        {
            get
            {
                var hasCollapsibleCategory = false;
                foreach (var category in Categories)
                {
                    if (!category.CanCollapse)
                        continue;
                    hasCollapsibleCategory = true;
                    if (!category.Collapsed)
                        return false;
                }
                return hasCollapsibleCategory;
            }
        }

        internal void SetAllCategoriesCollapsed(bool collapsed)
        {
            foreach (var category in Categories)
                if (category.CanCollapse)
                    category.SetCollapsed(collapsed);
        }

        internal void ShiftRowIndexes(int startIndex, int delta)
        {
            if (MaterialRowIndex >= startIndex)
                MaterialRowIndex += delta;
            if (EndRowIndex >= startIndex)
                EndRowIndex += delta;
            foreach (var category in Categories)
                category.ShiftRowIndexes(startIndex, delta);
        }

        internal CategoryNavigationTarget FindCategoryAtRow(int rowIndex)
        {
            CategoryNavigationTarget result = null;
            var bestAnchor = -1;
            foreach (var category in Categories)
            {
                var anchor = category.FindRowAnchorAtOrBefore(rowIndex);
                if (anchor <= bestAnchor)
                    continue;
                bestAnchor = anchor;
                result = category;
            }
            return result ?? (Categories.Count == 0 ? null : Categories[0]);
        }
    }

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
