using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
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

}
