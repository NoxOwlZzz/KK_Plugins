using System;
using System.Linq;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Coordinates the owners that build one material section.
    /// </summary>
    internal sealed class MaterialSectionPresenter
    {
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorPresentationActions _actions;
        private readonly MaterialShaderSectionPresenter _shaders;
        private readonly MaterialPropertySectionPresenter _properties;
        private readonly ProjectorSectionPresenter _projectors;

        internal MaterialSectionPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _session = session;
            _actions = actions;
            _shaders = new MaterialShaderSectionPresenter(actions);
            _properties = new MaterialPropertySectionPresenter(
                editService,
                session,
                actions);
            _projectors = new ProjectorSectionPresenter(
                editService,
                actions);
        }

        internal void AddRows(MaterialSectionContext context)
        {
            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.CategoryRebuild);
            try
            {
                var materialCollapsed = MaterialEditorSessionState.IsCollapsed(
                    _session.CollapsedMaterialSections,
                    context.MaterialKey);

                var hasSearch = context.PropertyFilter.Count > 0;
                var materialRowsCollapsed = materialCollapsed && !hasSearch;
                var section = new MaterialSectionPresentation(
                    context.ShaderKey,
                    context.MaterialName,
                    context.ShaderName,
                    context.Rows.Count,
                    () =>
                    {
                        var changed = MaterialEditorSessionState.IsCollapsed(
                            _session.CollapsedMaterialSections,
                            context.MaterialKey);
                        MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedMaterialSections,
                            context.MaterialKey,
                            false);
                        return changed;
                    },
                    () => MaterialEditorSessionState.IsCollapsed(
                        _session.CollapsedMaterialSections,
                        context.MaterialKey),
                    value => MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedMaterialSections,
                        context.MaterialKey,
                        value));
                context.Presentation.MaterialSections.Add(section);

                var materialItem = new MaterialRowModel()
                {
                    GameObject = context.GameObject,
                    Data = context.Data,
                    Material = context.Material,
                    Projector = context.Projector,
                    MaterialName = context.MaterialName,
                    Collapsed = materialRowsCollapsed,
                    CollapsedOnChange = value =>
                    {
                        MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedMaterialSections, context.MaterialKey, value);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                    },
                    Copy = () => context.Edits.CopyMaterialEdits(
                        context.Material,
                        context.Projector),
                    Paste = () =>
                    {
                        context.Edits.PasteMaterialEdits(
                            context.Material,
                            context.Projector);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                    },
                    Rename = () => _actions.ShowRename(
                        context.GameObject,
                        context.Material,
                        context.Data)
                };
                if (context.Projector == null)
                {
                    materialItem.CopyOrRemove = () =>
                    {
                        context.Edits.CopyOrRemoveMaterial(context.Material);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                        _actions.RefreshMaterialSelection(
                            context.GameObject,
                            context.Data,
                            context.AllRenderers);
                    };
                }
                context.Rows.Add(materialItem);

                ShaderRowModel shaderItem = null;
                if (!materialRowsCollapsed && context.Projector != null)
                    _projectors.AddRows(context);

                if (!materialRowsCollapsed)
                    shaderItem = _shaders.AddRows(context);

                _properties.AddRows(
                    context,
                    section,
                    !materialRowsCollapsed);

                if (shaderItem != null)
                {
                    shaderItem.HasCategories = !hasSearch
                                               && section.Categories.Any(
                                                   category =>
                                                       category.CanCollapse);
                    shaderItem.AllCategoriesCollapsed = !hasSearch
                                                        && section.AllCategoriesCollapsed;
                    shaderItem.CategoriesCollapsedOnChange = value =>
                    {
                        section.SetAllCategoriesCollapsed(value);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                    };
                }
                section.EndRowIndex = Math.Max(
                    section.MaterialRowIndex,
                    context.Rows.Count - 1);
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.CategoryRebuild,
                    performanceSample);
            }
        }
    }
}
