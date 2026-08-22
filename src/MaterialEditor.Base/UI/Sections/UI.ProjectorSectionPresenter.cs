using System;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
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
