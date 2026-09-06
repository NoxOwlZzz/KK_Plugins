using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class ExtensionPropertyRowBuilder
    {
        private readonly MaterialEditorPresentationActions _actions;

        internal ExtensionPropertyRowBuilder(
            MaterialEditorPresentationActions actions)
        {
            _actions = actions;
        }

        internal IEnumerable<RowModel> CreateRows(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorPropertyEditor editor)
        {
            var floatEditor = editor as MaterialEditorFloatPropertyEditor;
            if (floatEditor != null)
                return new[] { CreateExtensionFloatRow(context, descriptor, floatEditor) };

            var colorEditor = editor as MaterialEditorColorPropertyEditor;
            if (colorEditor != null)
                return new[] { CreateExtensionColorRow(context, descriptor, colorEditor) };

            var booleanEditor = editor as MaterialEditorBooleanPropertyEditor;
            if (booleanEditor != null)
                return new[] { CreateExtensionBooleanRow(context, descriptor, booleanEditor) };

            var textureEditor = editor as MaterialEditorTexturePropertyEditor;
            if (textureEditor != null)
                return PropertyTextureRowBuilder.CreateExtensionTextureRows(context, descriptor, textureEditor);

            var enumEditor = editor as MaterialEditorEnumPropertyEditor;
            if (enumEditor != null)
                return new[] { CreateExtensionEnumRow(context, descriptor, enumEditor) };

            var vectorEditor = editor as MaterialEditorVectorPropertyEditor;
            if (vectorEditor != null)
                return new[] { CreateExtensionVectorRow(context, descriptor, vectorEditor) };

            var toggleEditor = editor as MaterialEditorTogglePropertyEditor;
            if (toggleEditor != null)
                return new[] { CreateExtensionToggleRow(context, descriptor, toggleEditor) };

            MaterialEditorPluginBase.Logger?.LogWarning(
                $"Property editor '{descriptor.EditorId}' returned an unsupported editor type.");
            return new RowModel[0];
        }

        private static FloatPropertyRowModel CreateExtensionFloatRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorFloatPropertyEditor editor)
        {
            return new FloatPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                HasRange = true,
                SliderMinimum = editor.Minimum,
                SliderMaximum = editor.Maximum,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            };
        }

        private ColorPropertyRowModel CreateExtensionColorRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorColorPropertyEditor editor)
        {
            return new ColorPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset,
                Edit = (title, value, changed) =>
                    _actions.EditColor(
                        context.Target.Data,
                        context.Target.Material,
                        $"Material Editor - {title}",
                        value,
                        changed),
                SetToPalette = (title, value) =>
                    _actions.SetColorToPalette(
                        context.Target.Data,
                        context.Target.Material,
                        $"Material Editor - {title}",
                        value)
            };
        }

        private static KeywordPropertyRowModel CreateExtensionBooleanRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorBooleanPropertyEditor editor)
        {
            return new KeywordPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            };
        }

        private static EnumPropertyRowModel CreateExtensionEnumRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorEnumPropertyEditor editor)
        {
            return new EnumPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                IsMixed = editor.IsMixed,
                Options = editor.Options,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            };
        }

        private static VectorPropertyRowModel CreateExtensionVectorRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorVectorPropertyEditor editor)
        {
            return new VectorPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                ComponentCount = editor.ComponentCount,
                Minimum = descriptor.Minimum,
                Maximum = descriptor.Maximum,
                MixedComponents = CopyMixedComponents(editor.MixedComponents),
                SelectInterpolable = editor.SelectInterpolable,
                ComponentOnChange = editor.ComponentChanged,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            };
        }

        private static FloatTogglePropertyRowModel CreateExtensionToggleRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorTogglePropertyEditor editor)
        {
            return new FloatTogglePropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                OffValue = editor.OffValue,
                OnValue = editor.OnValue,
                IsMixed = editor.IsMixed,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            };
        }

        private static bool[] CopyMixedComponents(IList<bool> source)
        {
            var result = new bool[4];
            var copyCount = MaterialEditorMixedStatePerformance
                .GetComponentCopyCount(source, result.Length);
            for (var index = 0; index < copyCount; index++)
                result[index] = source[index];
            return result;
        }
    }

    internal sealed class PropertyTextureRowBuilder
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;

        internal PropertyTextureRowBuilder(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
        }

        internal IEnumerable<RowModel> CreateTextureRows(
            PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var projector = descriptor.Projector;
            var propertyName = descriptor.Name;
            var currentTexture = MaterialPropertyAccess.GetTexture(
                material,
                descriptor.PropertyHandle);

            var textureItem = new TexturePropertyRowModel(descriptor.DisplayName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Changed = !_editService.GetMaterialTextureValueOriginal(data, material, propertyName, gameObject),
                Exists = currentTexture != null,
                Export = () => _actions.ExportTexture(material, propertyName),
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.TextureProperty,
                        descriptor.MaterialName,
                        propertyName,
                        string.Empty)
            };
            textureItem.Import = () =>
                _actions.ImportTexture(textureItem, gameObject, data, material, propertyName);
            textureItem.Reset = () =>
            {
                _editService.RemoveMaterialTexture(
                    data,
                    material,
                    propertyName,
                    gameObject);
                textureItem.Changed =
                    !_editService.GetMaterialTextureValueOriginal(
                        data,
                        material,
                        propertyName,
                        gameObject);
                var restoredTexture = MaterialPropertyAccess.GetTexture(
                    material,
                    descriptor.PropertyHandle);
                textureItem.Exists = restoredTexture != null;
            };

            var textureOffset = MaterialPropertyAccess.GetTextureOffset(
                material,
                descriptor.PropertyHandle);
            var textureOffsetOriginal =
                _editService.GetMaterialTextureOffsetOriginal(data, material, propertyName, gameObject)
                ?? textureOffset;
            var textureScale = MaterialPropertyAccess.GetTextureScale(
                material,
                descriptor.PropertyHandle);
            var textureScaleOriginal =
                _editService.GetMaterialTextureScaleOriginal(data, material, propertyName, gameObject)
                ?? textureScale;

            var textureOffsetScaleItem = new TextureOffsetScaleRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Offset = textureOffset,
                OriginalOffset = textureOffsetOriginal,
                OffsetOnChange = value =>
                    _editService.SetMaterialTextureOffset(data, material, propertyName, value, gameObject),
                OffsetOnReset = () =>
                    _editService.RemoveMaterialTextureOffset(data, material, propertyName, gameObject),
                Scale = textureScale,
                OriginalScale = textureScaleOriginal,
                ScaleOnChange = value =>
                    _editService.SetMaterialTextureScale(data, material, propertyName, value, gameObject),
                ScaleOnReset = () =>
                    _editService.RemoveMaterialTextureScale(data, material, propertyName, gameObject)
            };

            return new RowModel[] { textureItem, textureOffsetScaleItem };
        }

        internal CubemapPropertyRowModel CreateCubemapRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var cubemapItem = new CubemapPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = descriptor.Projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Changed = !_editService.GetMaterialCubemapValueOriginal(
                    data,
                    material,
                    propertyName,
                    gameObject),
                Exists = MaterialPropertyAccess.GetTexture(
                    material,
                    descriptor.PropertyHandle) is Cubemap,
                Export = () => _actions.ExportCubemap(material, propertyName)
            };
            cubemapItem.Import = () =>
                _actions.ImportCubemap(
                    cubemapItem,
                    gameObject,
                    data,
                    material,
                    propertyName);
            cubemapItem.Reset = () =>
            {
                _editService.RemoveMaterialCubemap(
                    data,
                    material,
                    propertyName,
                    gameObject);
                cubemapItem.Changed =
                    !_editService.GetMaterialCubemapValueOriginal(
                        data,
                        material,
                        propertyName,
                        gameObject);
                cubemapItem.Exists = MaterialPropertyAccess.GetTexture(
                    material,
                    descriptor.PropertyHandle) is Cubemap;
            };
            return cubemapItem;
        }


        internal static IEnumerable<RowModel> CreateExtensionTextureRows(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorTexturePropertyEditor editor)
        {
            var texture = new TexturePropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Changed = editor.Changed,
                Exists = editor.Exists,
                SelectInterpolable = editor.SelectInterpolable,
                Export = editor.Export ?? (() => { }),
                Import = editor.Import ?? (() => { }),
                Reset = editor.Reset ?? (() => { })
            };
            var transform = new TextureOffsetScaleRowModel
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Offset = editor.Offset,
                OriginalOffset = editor.OriginalOffset,
                OffsetOnChange = editor.OffsetChanged ?? (_ => { }),
                OffsetOnReset = editor.ResetOffset ?? (() => { }),
                Scale = editor.Scale,
                OriginalScale = editor.OriginalScale,
                ScaleOnChange = editor.ScaleChanged ?? (_ => { }),
                ScaleOnReset = editor.ResetScale ?? (() => { })
            };
            return new RowModel[] { texture, transform };
        }
    }

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
            var hasExplicitRange = FloatPropertyRangePolicy.HasUsableRange(
                minValue,
                maxValue);
            var showSlider = hasExplicitRange
                             || (!minValue.HasValue && !maxValue.HasValue);
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
                HasRange = showSlider,
                SelectInterpolable = selectInterpolable,
                ValueOnChange = changeValue,
                ValueOnReset = resetValue
            };
            if (hasExplicitRange)
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
