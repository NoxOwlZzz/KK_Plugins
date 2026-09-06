using System;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

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
    }

    internal sealed class MaterialShaderSectionPresenter
    {
        private readonly MaterialEditorPresentationActions _actions;

        internal MaterialShaderSectionPresenter(
            MaterialEditorPresentationActions actions)
        {
            _actions = actions;
        }

        internal ShaderRowModel AddRows(MaterialSectionContext context)
        {
            var originalShaderName = context.Edits.GetOriginalShader(
                context.Material);
            if (originalShaderName.IsNullOrEmpty())
                originalShaderName = context.ShaderName;
            var shaderItem = new ShaderRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                ShaderName = context.ShaderName,
                OriginalShaderName = originalShaderName,
                TooltipText = ShaderUiMetadataRegistry.GetShaderTooltip(
                    context.ShaderName),

                ShaderNameOnChange = value =>
                {
                    context.Edits.SetShader(
                        context.Material,
                        value);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                ShaderNameOnReset = () =>
                {
                    context.Edits.ResetShader(context.Material);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        context.GameObject,
                        RowModel.RowItemType.Shader,
                        context.MaterialName,
                        string.Empty,
                        string.Empty)
            };
            context.Rows.Add(shaderItem);

            var originalRenderQueue =
                context.Edits.GetOriginalRenderQueue(context.Material)
                ?? context.Material.renderQueue;
            context.Rows.Add(new ShaderRenderQueueRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                Value = context.Material.renderQueue,
                OriginalValue = originalRenderQueue,
                ValueOnChange = value =>
                    context.Edits.SetRenderQueue(
                        context.Material,
                        value),
                ValueOnReset = () =>
                    context.Edits.ResetRenderQueue(context.Material)
            });
            return shaderItem;
        }
    }

    internal sealed class ProjectorSectionPresenter
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;

        internal ProjectorSectionPresenter(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
        }

        internal void AddRows(MaterialSectionContext context)
        {
            foreach (var property in Enum.GetValues(typeof(ProjectorProperties)).Cast<ProjectorProperties>())
            {
                string name;
                float value;
                float maxValue;
                GetProjectorPresentation(
                    context.Projector,
                    property,
                    out name,
                    out value,
                    out maxValue);

                if (context.PropertyFilter.Count > 0)
                {
                    var matches = false;
                    foreach (var filterPattern in context.PropertyFilter)
                    {
                        if (!filterPattern.Matches(name))
                            continue;
                        matches = true;
                        break;
                    }
                    if (!matches)
                        continue;
                }

                var original =
                    context.Edits.GetOriginalProjectorProperty(
                        context.Projector,
                        property)
                    ?? value;
                context.Rows.Add(CreateFloatRow(
                    context.GameObject,
                    context.Data,
                    null,
                    context.Projector,
                    name,
                    value,
                    original,
                    0f,
                    maxValue,
                    () => _actions.SelectProjectorInterpolable(
                        context.GameObject,
                        property,
                        context.Projector.NameFormatted()),
                    newValue =>
                        _editService.SetProjectorProperty(
                            context.Data,
                            context.Projector,
                            property,
                            newValue,
                            context.Projector.gameObject),
                    () =>
                        _editService.RemoveProjectorProperty(
                            context.Data,
                            context.Projector,
                            property,
                            context.Projector.gameObject)));
            }
        }

        private static FloatPropertyRowModel CreateFloatRow(
            GameObject gameObject,
            object data,
            Material material,
            Projector projector,
            string propertyName,
            float value,
            float original,
            float? minValue,
            float? maxValue,
            Action selectInterpolable,
            Action<float> changeValue,
            Action resetValue)
        {
            var item = new FloatPropertyRowModel(propertyName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                Value = value,
                OriginalValue = original,
                HasRange = true,
                SelectInterpolable = selectInterpolable,
                ValueOnChange = changeValue,
                ValueOnReset = resetValue
            };
            if (minValue != null)
                item.SliderMinimum = minValue.Value;
            if (maxValue != null)
                item.SliderMaximum = maxValue.Value;
            return item;
        }

        private static void GetProjectorPresentation(
            Projector projector,
            ProjectorProperties property,
            out string name,
            out float value,
            out float maxValue)
        {
            name = string.Empty;
            value = 0f;
            maxValue = 100f;
            switch (property)
            {
                case ProjectorProperties.Enabled:
                    name = "Enabled";
                    value = Convert.ToSingle(projector.enabled);
                    maxValue = 1f;
                    break;
                case ProjectorProperties.NearClipPlane:
                    name = "Near Clip Plane";
                    value = projector.nearClipPlane;
                    maxValue = ProjectorNearClipPlaneMax.Value;
                    break;
                case ProjectorProperties.FarClipPlane:
                    name = "Far Clip Plane";
                    value = projector.farClipPlane;
                    maxValue = ProjectorFarClipPlaneMax.Value;
                    break;
                case ProjectorProperties.FieldOfView:
                    name = "Field Of View";
                    value = projector.fieldOfView;
                    maxValue = ProjectorFieldOfViewMax.Value;
                    break;
                case ProjectorProperties.AspectRatio:
                    name = "Aspect Ratio";
                    value = projector.aspectRatio;
                    maxValue = ProjectorAspectRatioMax.Value;
                    break;
                case ProjectorProperties.Orthographic:
                    name = "Orthographic";
                    value = Convert.ToSingle(projector.orthographic);
                    maxValue = 1f;
                    break;
                case ProjectorProperties.OrthographicSize:
                    name = "Orthographic Size";
                    value = projector.orthographicSize;
                    maxValue = ProjectorOrthographicSizeMax.Value;
                    break;
                case ProjectorProperties.IgnoreMapLayer:
                    name = "Ignore Map layer";
                    value = Convert.ToSingle(
                        projector.ignoreLayers == (projector.ignoreLayers | (1 << 11)));
                    maxValue = 1f;
                    break;
                case ProjectorProperties.IgnoreCharaLayer:
                    name = "Ignore Chara Layer";
                    value = Convert.ToSingle(
                        projector.ignoreLayers == (projector.ignoreLayers | (1 << 10)));
                    maxValue = 1f;
                    break;
            }
        }
    }
}
