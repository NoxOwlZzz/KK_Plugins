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
}
