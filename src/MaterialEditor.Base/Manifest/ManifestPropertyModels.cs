using System.Collections.Generic;
using System.Globalization;

namespace MaterialEditorAPI
{
    /// <summary>One numeric option displayed by an enum property editor.</summary>
    public sealed class MaterialEditorEnumOption
    {
        /// <summary>Create an enum option.</summary>
        public MaterialEditorEnumOption(float value, string displayName)
        {
            Value = value;
            DisplayName = string.IsNullOrEmpty(displayName)
                ? value.ToString(CultureInfo.InvariantCulture)
                : displayName;
        }

        /// <summary>Numeric shader value represented by this option.</summary>
        public float Value { get; }
        /// <summary>Label shown in the enum dropdown.</summary>
        public string DisplayName { get; }
    }

    /// <summary>
    /// Parsed optional metadata for one manifest property. This type remains
    /// independent from Unity so parsing and policy behavior can be tested directly.
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
