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
        private readonly PropertyTextureRowBuilder _textures;
        private readonly PropertyValueRowBuilder _values;
        private readonly ExtensionPropertyRowBuilder _extensions;

        internal PropertyRowModelFactory(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _textures = new PropertyTextureRowBuilder(editService, actions);
            _values = new PropertyValueRowBuilder(editService, actions);
            _extensions = new ExtensionPropertyRowBuilder(actions);
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
                rows = new[] { _values.CreateEnumRow(descriptor) };
            }
            else if (descriptor.Type == ShaderPropertyType.Float
                     && editorId == MaterialEditorPropertyEditorIds.Toggle)
            {
                rows = new[] { _values.CreateFloatToggleRow(descriptor) };
            }
            else if ((descriptor.Type == ShaderPropertyType.Vector
                      || descriptor.Type == ShaderPropertyType.Color)
                     && PropertyValueRowBuilder.IsVectorEditor(editorId))
            {
                rows = new[] { _values.CreateVectorRow(descriptor) };
            }
            else switch (descriptor.Type)
            {
                case ShaderPropertyType.Texture:
                    rows = _textures.CreateTextureRows(descriptor);
                    break;
                case ShaderPropertyType.Cubemap:
                    rows = new[] { _textures.CreateCubemapRow(descriptor) };
                    break;
                case ShaderPropertyType.Color:
                    rows = new[] { _values.CreateColorRow(descriptor) };
                    break;
                case ShaderPropertyType.Float:
                    rows = new[] { _values.CreateFloatRow(descriptor) };
                    break;
                case ShaderPropertyType.Keyword:
                    rows = new[] { _values.CreateKeywordRow(descriptor) };
                    break;
                case ShaderPropertyType.Vector:
                    rows = new[] { _values.CreateVectorRow(descriptor) };
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
                _extensions.CreateRows(context, descriptor, editor),
                descriptor.TooltipText);
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
            return PropertyValueRowBuilder.CreateFloatRow(
                descriptor,
                value,
                original,
                minValue,
                maxValue,
                selectInterpolable,
                changeValue,
                resetValue);
        }
    }

    internal static class FloatPropertyRangePolicy
    {
        internal static bool HasUsableRange(float? minimum, float? maximum)
        {
            return minimum.HasValue
                   && maximum.HasValue
                   && !float.IsNaN(minimum.Value)
                   && !float.IsInfinity(minimum.Value)
                   && !float.IsNaN(maximum.Value)
                   && !float.IsInfinity(maximum.Value)
                   && maximum.Value > minimum.Value;
        }
    }
}
