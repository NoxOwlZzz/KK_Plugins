using System.Collections.Generic;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
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
}
