using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class PropertyDescriptor
    {
        internal PropertyDescriptor(
            GameObject gameObject,
            object data,
            Material material,
            Projector projector,
            string materialName,
            ShaderPropertyData definition,
            string category)
        {
            GameObject = gameObject;
            Data = data;
            Material = material;
            Projector = projector;
            MaterialName = materialName;
            Name = definition.Name;
            var currentDisplayName = string.IsNullOrEmpty(definition.DisplayName)
                ? definition.Name
                : definition.DisplayName;
            var shaderName = material.shader.NameFormatted();
            var catalogDisplayName = ShaderUiMetadataRegistry.GetPropertyDisplayName(
                shaderName,
                definition.Name);
            var hasExplicitDisplayName = definition.HasExplicitDisplayName
                || (!string.IsNullOrEmpty(definition.DisplayName)
                    && definition.DisplayName != definition.Name);
            DisplayName = hasExplicitDisplayName
                ? currentDisplayName
                : string.IsNullOrEmpty(catalogDisplayName)
                    ? currentDisplayName
                    : catalogDisplayName;
            Type = definition.Type;
            PropertyHandle = Type == ShaderPropertyType.Keyword
                ? default(MaterialPropertyHandle)
                : MaterialPropertyIdCache.Get(Name);
            MinValue = definition.MinValue;
            MaxValue = definition.MaxValue;
            var editorId = string.IsNullOrEmpty(definition.EditorId)
                ? ShaderPropertyEditorPolicy.GetDefaultEditorId(definition.Type)
                : definition.EditorId;
            var catalogTooltip = ShaderUiMetadataRegistry.GetPropertyTooltip(
                shaderName,
                definition.Name);
            var publicDescriptor = new MaterialEditorPropertyDescriptor(
                definition.Name,
                DisplayName,
                editorId)
            {
                PropertyName = definition.Name,
                Category = category ?? string.Empty,
                Order = definition.Order ?? 0,
                Group = definition.Group ?? string.Empty,
                VisibilityCondition = definition.ShowIf,
                VectorComponentCount = definition.VectorComponentCount,
                OffValue = definition.OffValue,
                OnValue = definition.OnValue,
                Minimum = definition.MinValue,
                Maximum = definition.MaxValue,
                TooltipText = string.IsNullOrEmpty(catalogTooltip)
                    ? definition.TooltipText
                    : catalogTooltip
            };
            if (definition.EnumOptions != null)
                ((List<MaterialEditorEnumOption>)publicDescriptor.EnumOptions)
                    .AddRange(definition.EnumOptions);
            PublicDescriptor = publicDescriptor;
        }

        internal PropertyDescriptor(
            GameObject gameObject,
            object data,
            Material material,
            Projector projector,
            string materialName,
            MaterialEditorPropertyDescriptor descriptor,
            ShaderPropertyType type)
        {
            GameObject = gameObject;
            Data = data;
            Material = material;
            Projector = projector;
            MaterialName = materialName;
            Name = string.IsNullOrEmpty(descriptor.PropertyName)
                ? descriptor.Id
                : descriptor.PropertyName;
            DisplayName = string.IsNullOrEmpty(descriptor.DisplayName)
                ? descriptor.Id
                : descriptor.DisplayName;
            Type = type;
            PropertyHandle = Type == ShaderPropertyType.Keyword
                ? default(MaterialPropertyHandle)
                : MaterialPropertyIdCache.Get(Name);
            MinValue = descriptor.Minimum;
            MaxValue = descriptor.Maximum;
            PublicDescriptor = descriptor;
        }

        internal GameObject GameObject { get; }
        internal object Data { get; }
        internal Material Material { get; }
        internal Projector Projector { get; }
        internal string MaterialName { get; }
        internal string Name { get; }
        internal string DisplayName { get; }
        internal ShaderPropertyType Type { get; }
        internal MaterialPropertyHandle PropertyHandle { get; }
        internal float? MinValue { get; }
        internal float? MaxValue { get; }
        internal MaterialEditorPropertyDescriptor PublicDescriptor { get; }

    }

    internal sealed class PropertyRowModelFactory
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;

        internal PropertyRowModelFactory(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
        }

        internal IEnumerable<RowModel> Create(PropertyDescriptor descriptor)
        {
            IEnumerable<RowModel> rows;
            var editorId = descriptor.PublicDescriptor?.EditorId;
            if (descriptor.Type == ShaderPropertyType.Float
                && editorId == MaterialEditorPropertyEditorIds.Enum
                && descriptor.PublicDescriptor.EnumOptions != null
                && descriptor.PublicDescriptor.EnumOptions.Count > 0)
            {
                rows = new[] { CreateEnumRow(descriptor) };
            }
            else if (descriptor.Type == ShaderPropertyType.Float
                     && editorId == MaterialEditorPropertyEditorIds.Toggle)
            {
                rows = new[] { CreateFloatToggleRow(descriptor) };
            }
            else if ((descriptor.Type == ShaderPropertyType.Vector
                      || descriptor.Type == ShaderPropertyType.Color)
                     && IsVectorEditor(editorId))
            {
                rows = new[] { CreateVectorRow(descriptor) };
            }
            else switch (descriptor.Type)
            {
                case ShaderPropertyType.Texture:
                    rows = CreateTextureRows(descriptor);
                    break;
                case ShaderPropertyType.Cubemap:
                    rows = new[] { CreateCubemapRow(descriptor) };
                    break;
                case ShaderPropertyType.Color:
                    rows = new[] { CreateColorRow(descriptor) };
                    break;
                case ShaderPropertyType.Float:
                    rows = new[] { CreateFloatRow(descriptor) };
                    break;
                case ShaderPropertyType.Keyword:
                    rows = new[] { CreateKeywordRow(descriptor) };
                    break;
                case ShaderPropertyType.Vector:
                    rows = new[] { CreateVectorRow(descriptor) };
                    break;
                default:
                    rows = new RowModel[0];
                    break;
            }
            return WithMetadata(
                rows,
                descriptor.PublicDescriptor?.TooltipText);
        }

        internal IEnumerable<RowModel> CreateExtension(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor)
        {
            var editor = MaterialEditorExtensionRegistry.CreatePropertyEditor(
                context,
                descriptor);
            if (editor == null)
                return new RowModel[0];

            return WithMetadata(
                CreateExtensionRows(context, descriptor, editor),
                descriptor.TooltipText);
        }

        private IEnumerable<RowModel> CreateExtensionRows(
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
                return CreateExtensionTextureRows(context, descriptor, textureEditor);

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

        private static IEnumerable<RowModel> WithMetadata(
            IEnumerable<RowModel> rows,
            string tooltipText)
        {
            foreach (var row in rows)
            {
                row.TooltipText = tooltipText;
                yield return row;
            }
        }

        private IEnumerable<RowModel> CreateTextureRows(
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

        private CubemapPropertyRowModel CreateCubemapRow(PropertyDescriptor descriptor)
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

        private ColorPropertyRowModel CreateColorRow(PropertyDescriptor descriptor)
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

        private FloatPropertyRowModel CreateFloatRow(PropertyDescriptor descriptor)
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

        private EnumPropertyRowModel CreateEnumRow(PropertyDescriptor descriptor)
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

        private FloatTogglePropertyRowModel CreateFloatToggleRow(
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

        private VectorPropertyRowModel CreateVectorRow(PropertyDescriptor descriptor)
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
                SelectInterpolable = descriptor.Type == ShaderPropertyType.Color
                    ? (System.Action)(() => _actions.SelectInterpolable(
                        descriptor.GameObject,
                        RowModel.RowItemType.ColorProperty,
                        descriptor.MaterialName,
                        descriptor.Name,
                        string.Empty))
                    : null,
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

        private KeywordPropertyRowModel CreateKeywordRow(PropertyDescriptor descriptor)
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
                SelectInterpolable = editor.SelectInterpolable ?? (() => { }),
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
                SelectInterpolable = editor.SelectInterpolable ?? (() => { }),
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

        private static IEnumerable<RowModel> CreateExtensionTextureRows(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorTexturePropertyEditor editor)
        {
            var select = editor.SelectInterpolable ?? (() => { });
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
                SelectInterpolable = select,
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

        private static bool IsVectorEditor(string editorId)
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

        private static bool[] CopyMixedComponents(IList<bool> source)
        {
            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.MixedStateCopies);
            try
            {
                var result = new bool[4];
                var copyCount = MaterialEditorMixedStatePerformance
                    .GetComponentCopyCount(source, result.Length);
                for (var index = 0; index < copyCount; index++)
                    result[index] = source[index];
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.MixedComponentsCopied,
                    copyCount);
                return result;
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.MixedStateCopies,
                    performanceSample);
            }
        }
    }
}
