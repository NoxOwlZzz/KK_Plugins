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

}
