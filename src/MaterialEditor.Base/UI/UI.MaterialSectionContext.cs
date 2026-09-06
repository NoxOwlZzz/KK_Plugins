using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class MaterialSectionContext
    {
        internal MaterialSectionContext(
            MaterialEditService editService,
            MaterialEditorPresentation presentation,
            GameObject gameObject,
            object data,
            string filter,
            IEnumerable<Renderer> allRenderers,
            IList<MaterialEditorFilterPattern> propertyFilter,
            Material material,
            Projector projector)
        {
            if (editService == null)
                throw new ArgumentNullException(nameof(editService));
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            GameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
            Data = data;
            Filter = filter;
            AllRenderers = allRenderers ?? throw new ArgumentNullException(nameof(allRenderers));
            PropertyFilter = propertyFilter ?? throw new ArgumentNullException(nameof(propertyFilter));
            Material = material ?? throw new ArgumentNullException(nameof(material));
            Projector = projector;

            MaterialName = material.NameFormatted();
            ShaderName = material.shader.NameFormatted();
            MaterialKey = MaterialEditorSectionKeys.Material(gameObject, material);
            ShaderKey = MaterialEditorSectionKeys.Shader(gameObject, material, ShaderName);
            Edits = new MaterialEditorEditService(editService, gameObject, data);
        }

        internal MaterialEditorPresentation Presentation { get; }
        internal ICollection<RowModel> Rows => Presentation.Rows;
        internal GameObject GameObject { get; }
        internal object Data { get; }
        internal string Filter { get; }
        internal IEnumerable<Renderer> AllRenderers { get; }
        internal IList<MaterialEditorFilterPattern> PropertyFilter { get; }
        internal Material Material { get; }
        internal Projector Projector { get; }
        internal string MaterialName { get; }
        internal string ShaderName { get; }
        internal string MaterialKey { get; }
        internal string ShaderKey { get; }
        internal MaterialEditorEditService Edits { get; }
    }

    internal sealed class PropertyCategorySection
    {
        internal PropertyCategorySection(
            CategoryNavigationTarget navigationTarget,
            bool rowsCollapsed)
        {
            NavigationTarget = navigationTarget;
            RowsCollapsed = rowsCollapsed;
        }

        internal CategoryNavigationTarget NavigationTarget { get; }
        internal bool RowsCollapsed { get; }

        internal void RecordFirstRow(int rowIndex)
        {
            NavigationTarget?.RecordRowIndex(rowIndex);
        }
    }

    internal sealed class PropertyCategorySectionBuilder
    {
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorPresentationActions _actions;

        internal PropertyCategorySectionBuilder(
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        internal PropertyCategorySection Add(
            MaterialSectionContext context,
            MaterialSectionPresentation materialSection,
            string sourceId,
            string categoryName,
            bool namedCategory,
            bool includeRows,
            string categoryIdentity = null)
        {
            var showHeader = namedCategory && context.PropertyFilter.Count == 0;
            var identity = string.IsNullOrEmpty(categoryIdentity)
                ? categoryName
                : categoryIdentity;
            var categoryKey = MaterialEditorSectionKeys.Category(
                context.GameObject,
                context.Material,
                context.ShaderName,
                sourceId,
                identity);
            var storedCollapsed = namedCategory
                && MaterialEditorSessionState.IsCollapsed(
                    _session.CollapsedPropertyCategories,
                    categoryKey);
            var tooltip = namedCategory
                ? ShaderUiMetadataRegistry.GetCategoryTooltip(
                    context.ShaderName,
                    categoryName)
                : null;
            CategoryNavigationTarget navigationTarget = null;
            if (namedCategory)
            {
                navigationTarget = materialSection.AddCategory(
                    sourceId + ":" + identity,
                    categoryName,
                    -1,
                    categoryKey,
                    () => MaterialEditorSessionState.IsCollapsed(
                        _session.CollapsedPropertyCategories,
                        categoryKey),
                    value => MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedPropertyCategories,
                        categoryKey,
                        value),
                    tooltip);
            }

            if (includeRows && showHeader)
            {
                navigationTarget?.RecordRowIndex(context.Rows.Count);
                var row = new PropertyCategoryRowModel(categoryName)
                {
                    Collapsed = storedCollapsed,
                    TooltipText = tooltip,
                    CollapsedOnChange = value =>
                    {
                        MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedPropertyCategories,
                            categoryKey,
                            value);
                        _actions.Refresh(
                            context.GameObject,
                            context.Data,
                            context.Filter);
                    }
                };
                context.Rows.Add(row);
            }

            // Filtering temporarily flattens categories, so a collapsed category
            // cannot hide the matching results returned by the filter.
            return new PropertyCategorySection(
                navigationTarget,
                showHeader && storedCollapsed);
        }

        internal bool AddSubcategory(
            MaterialSectionContext context,
            string sourceId,
            string categoryId,
            OrganizedPropertySubcategory subcategory,
            bool includeRows)
        {
            var showHeader = context.PropertyFilter.Count == 0;
            var stateKey = MaterialEditorSectionKeys.Subcategory(
                context.GameObject,
                context.Material,
                context.ShaderName,
                sourceId,
                categoryId,
                subcategory.Id);
            var storedCollapsed = showHeader
                                  && MaterialEditorSessionState.IsCollapsed(
                                      _session.CollapsedPropertySubcategories,
                                      stateKey);
            if (includeRows && showHeader)
            {
                var row = new PropertySubcategoryRowModel(subcategory.Name)
                {
                    HierarchyDepth = 1,
                    Collapsed = storedCollapsed,
                    CollapsedOnChange = value =>
                        {
                            MaterialEditorSessionState.SetCollapsed(
                                _session.CollapsedPropertySubcategories,
                                stateKey,
                                value);
                            _actions.Refresh(
                                context.GameObject,
                                context.Data,
                                context.Filter);
                        }
                };
                context.Rows.Add(row);
            }
            return showHeader && storedCollapsed;
        }
    }
}
