using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class PropertyValueRowBuilder
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;

        internal PropertyValueRowBuilder(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
        }

        internal ColorPropertyRowModel CreateColorRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = MaterialPropertyAccess.GetColor(
                material,
                descriptor.PropertyHandle);
            var original =
                _editService.GetMaterialColorPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return new ColorPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = descriptor.Projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                ValueOnChange = newValue =>
                    _editService.SetMaterialColorProperty(data, material, propertyName, newValue, gameObject),
                ValueOnReset = () =>
                    _editService.RemoveMaterialColorProperty(data, material, propertyName, gameObject),
                Edit = (title, currentValue, onChanged) =>
                    _actions.EditColor(data, material, $"Material Editor - {title}", currentValue, onChanged),
                SetToPalette = (title, currentValue) =>
                    _actions.SetColorToPalette(data, material, $"Material Editor - {title}", currentValue),
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.ColorProperty,
                        descriptor.MaterialName,
                        propertyName,
                        string.Empty)
            };
        }

        internal FloatPropertyRowModel CreateFloatRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = MaterialPropertyAccess.GetFloat(
                material,
                descriptor.PropertyHandle);
            var original =
                _editService.GetMaterialFloatPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return CreateFloatRow(
                descriptor,
                value,
                original,
                descriptor.MinValue,
                descriptor.MaxValue,
                () => _actions.SelectInterpolable(
                    gameObject,
                    RowModel.RowItemType.FloatProperty,
                    descriptor.MaterialName,
                    propertyName,
                    string.Empty),
                newValue =>
                    _editService.SetMaterialFloatProperty(data, material, propertyName, newValue, gameObject),
                () => _editService.RemoveMaterialFloatProperty(data, material, propertyName, gameObject));
        }

        internal EnumPropertyRowModel CreateEnumRow(PropertyDescriptor descriptor)
        {
            var value = MaterialPropertyAccess.GetFloat(
                descriptor.Material,
                descriptor.PropertyHandle);
            var original =
                _editService.GetMaterialFloatPropertyValueOriginal(
                    descriptor.Data,
                    descriptor.Material,
                    descriptor.Name,
                    descriptor.GameObject)
                ?? value;
            return new EnumPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = descriptor.GameObject,
                Data = descriptor.Data,
                Material = descriptor.Material,
                Projector = descriptor.Projector,
                PropertyName = descriptor.Name,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                Options = descriptor.PublicDescriptor.EnumOptions,
                SelectInterpolable = () => _actions.SelectInterpolable(
                    descriptor.GameObject,
                    RowModel.RowItemType.FloatProperty,
                    descriptor.MaterialName,
                    descriptor.Name,
                    string.Empty),
                ValueOnChange = newValue =>
                    _editService.SetMaterialFloatProperty(
                        descriptor.Data,
                        descriptor.Material,
                        descriptor.Name,
                        newValue,
                        descriptor.GameObject),
                ValueOnReset = () =>
                    _editService.RemoveMaterialFloatProperty(
                        descriptor.Data,
                        descriptor.Material,
                        descriptor.Name,
                        descriptor.GameObject)
            };
        }

        internal FloatTogglePropertyRowModel CreateFloatToggleRow(
            PropertyDescriptor descriptor)
        {
            var value = MaterialPropertyAccess.GetFloat(
                descriptor.Material,
                descriptor.PropertyHandle);
            var original =
                _editService.GetMaterialFloatPropertyValueOriginal(
                    descriptor.Data,
                    descriptor.Material,
                    descriptor.Name,
                    descriptor.GameObject)
                ?? value;
            return new FloatTogglePropertyRowModel(descriptor.DisplayName)
            {
                GameObject = descriptor.GameObject,
                Data = descriptor.Data,
                Material = descriptor.Material,
                Projector = descriptor.Projector,
                PropertyName = descriptor.Name,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                OffValue = descriptor.PublicDescriptor.OffValue,
                OnValue = descriptor.PublicDescriptor.OnValue,
                SelectInterpolable = () => _actions.SelectInterpolable(
                    descriptor.GameObject,
                    RowModel.RowItemType.FloatProperty,
                    descriptor.MaterialName,
                    descriptor.Name,
                    string.Empty),
                ValueOnChange = newValue =>
                    _editService.SetMaterialFloatProperty(
                        descriptor.Data,
                        descriptor.Material,
                        descriptor.Name,
                        newValue,
                        descriptor.GameObject),
                ValueOnReset = () =>
                    _editService.RemoveMaterialFloatProperty(
                        descriptor.Data,
                        descriptor.Material,
                        descriptor.Name,
                        descriptor.GameObject)
            };
        }

        internal VectorPropertyRowModel CreateVectorRow(PropertyDescriptor descriptor)
        {
            var value = MaterialPropertyAccess.GetVector(
                descriptor.Material,
                descriptor.PropertyHandle);
            Vector4 original;
            System.Action<Vector4> setValue;
            System.Action resetValue;
            if (descriptor.Type == ShaderPropertyType.Color)
            {
                var originalColor = _editService.GetMaterialColorPropertyValueOriginal(
                    descriptor.Data,
                    descriptor.Material,
                    descriptor.Name,
                    descriptor.GameObject);
                original = originalColor.HasValue
                    ? (Vector4)originalColor.Value
                    : value;
                setValue = newValue => _editService.SetMaterialColorProperty(
                    descriptor.Data,
                    descriptor.Material,
                    descriptor.Name,
                    new Color(newValue.x, newValue.y, newValue.z, newValue.w),
                    descriptor.GameObject);
                resetValue = () => _editService.RemoveMaterialColorProperty(
                    descriptor.Data,
                    descriptor.Material,
                    descriptor.Name,
                    descriptor.GameObject);
            }
            else
            {
                original = _editService.GetMaterialVectorPropertyValueOriginal(
                               descriptor.Data,
                               descriptor.Material,
                               descriptor.Name,
                               descriptor.GameObject)
                           ?? value;
                setValue = newValue => _editService.SetMaterialVectorProperty(
                    descriptor.Data,
                    descriptor.Material,
                    descriptor.Name,
                    newValue,
                    descriptor.GameObject);
                resetValue = () => _editService.RemoveMaterialVectorProperty(
                    descriptor.Data,
                    descriptor.Material,
                    descriptor.Name,
                    descriptor.GameObject);
            }

            return new VectorPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = descriptor.GameObject,
                Data = descriptor.Data,
                Material = descriptor.Material,
                Projector = descriptor.Projector,
                PropertyName = descriptor.Name,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                ComponentCount = GetVectorComponentCount(descriptor.PublicDescriptor),
                Minimum = descriptor.MinValue,
                Maximum = descriptor.MaxValue,
                SelectInterpolable = () => _actions.SelectInterpolable(
                    descriptor.GameObject,
                    descriptor.Type == ShaderPropertyType.Color
                        ? RowModel.RowItemType.ColorProperty
                        : RowModel.RowItemType.VectorProperty,
                    descriptor.MaterialName,
                    descriptor.Name,
                    string.Empty),
                ValueOnChange = setValue,
                ValueOnReset = resetValue
            };
        }

        internal static FloatPropertyRowModel CreateFloatRow(
            PropertyDescriptor descriptor,
            float value,
            float original,
            float? minValue,
            float? maxValue,
            System.Action selectInterpolable,
            System.Action<float> changeValue,
            System.Action resetValue)
        {
            var hasRange = FloatPropertyRangePolicy.HasUsableRange(
                minValue,
                maxValue);
            var item = new FloatPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = descriptor.GameObject,
                Data = descriptor.Data,
                Material = descriptor.Material,
                Projector = descriptor.Projector,
                PropertyName = descriptor.Name,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                HasRange = hasRange,
                SelectInterpolable = selectInterpolable,
                ValueOnChange = changeValue,
                ValueOnReset = resetValue
            };
            if (hasRange)
            {
                item.SliderMinimum = minValue.Value;
                item.SliderMaximum = maxValue.Value;
            }
            return item;
        }

        internal KeywordPropertyRowModel CreateKeywordRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = material.IsKeywordEnabled($"_{propertyName}");
            var original =
                _editService.GetMaterialKeywordPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return new KeywordPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = descriptor.Projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                ValueOnChange = newValue =>
                    _editService.SetMaterialKeywordProperty(data, material, propertyName, newValue, gameObject),
                ValueOnReset = () =>
                    _editService.RemoveMaterialKeywordProperty(data, material, propertyName, gameObject)
            };
        }

        internal static bool IsVectorEditor(string editorId)
        {
            return editorId == MaterialEditorPropertyEditorIds.Vector2
                   || editorId == MaterialEditorPropertyEditorIds.Vector3
                   || editorId == MaterialEditorPropertyEditorIds.Vector4;
        }

        private static int GetVectorComponentCount(
            MaterialEditorPropertyDescriptor descriptor)
        {
            if (descriptor.VectorComponentCount.HasValue
                && descriptor.VectorComponentCount.Value >= 2
                && descriptor.VectorComponentCount.Value <= 4)
                return descriptor.VectorComponentCount.Value;
            if (descriptor.EditorId == MaterialEditorPropertyEditorIds.Vector2)
                return 2;
            if (descriptor.EditorId == MaterialEditorPropertyEditorIds.Vector3)
                return 3;
            return 4;
        }
    }
}
