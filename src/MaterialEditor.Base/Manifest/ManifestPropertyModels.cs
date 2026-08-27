using System.Collections.Generic;
using System.Globalization;

namespace MaterialEditorAPI
{
    /// <summary>One numeric option displayed by an enum property editor.</summary>
    public sealed class MaterialEditorEnumOption
    {
        public MaterialEditorEnumOption(float value, string displayName)
        {
            Value = value;
            DisplayName = string.IsNullOrEmpty(displayName)
                ? value.ToString(CultureInfo.InvariantCulture)
                : displayName;
        }

        public float Value { get; }
        public string DisplayName { get; }
    }

    /// <summary>
    /// Unity-independent metadata parsed for one manifest property.
    /// </summary>
    internal sealed class ShaderPropertyUiMetadata
    {
        internal string DisplayName;
        internal int? Order;
        internal int? CategoryOrder;
        internal string EditorId;
        internal string TooltipText;
        internal string Group;
        internal MaterialEditorPropertyCondition ShowIf;
        internal readonly List<MaterialEditorEnumOption> EnumOptions =
            new List<MaterialEditorEnumOption>();
        internal int? VectorComponentCount;
        internal bool Invert;
        internal float OffValue;
        internal float OnValue = 1f;
    }
}
