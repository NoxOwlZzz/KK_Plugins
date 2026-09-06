using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class ColorPropertyRowModel : RowModel
    {
        internal ColorPropertyRowModel(string labelText)
            : base(RowItemType.ColorProperty, labelText)
        {
        }

        internal Color Value { get; set; }
        internal Color OriginalValue { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action<Color> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
        internal Action<string, Color, Action<Color>> Edit { get; set; }
        internal Action<string, Color> SetToPalette { get; set; }
    }

    internal sealed class FloatPropertyRowModel : RowModel
    {
        internal FloatPropertyRowModel(string labelText)
            : base(RowItemType.FloatProperty, labelText)
        {
        }

        internal float Value { get; set; }
        internal float OriginalValue { get; set; }
        internal bool HasRange { get; set; }
        internal float SliderMinimum { get; set; }
        internal float SliderMaximum { get; set; } = 1f;
        internal Action SelectInterpolable { get; set; }
        internal Action<float> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }

    internal sealed class KeywordPropertyRowModel : BooleanValueRowModel
    {
        internal KeywordPropertyRowModel(string labelText)
            : base(RowItemType.KeywordProperty, labelText)
        {
        }
    }

    internal sealed class EnumPropertyRowModel : RowModel
    {
        internal EnumPropertyRowModel(string labelText)
            : base(RowItemType.EnumProperty, labelText)
        {
        }

        internal float Value { get; set; }
        internal float OriginalValue { get; set; }
        internal bool IsMixed { get; set; }
        internal IList<MaterialEditorEnumOption> Options { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action<float> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }

    internal sealed class VectorPropertyRowModel : RowModel
    {
        internal VectorPropertyRowModel(string labelText)
            : base(RowItemType.VectorProperty, labelText)
        {
        }

        internal Vector4 Value { get; set; }
        internal Vector4 OriginalValue { get; set; }
        internal int ComponentCount { get; set; } = 4;
        internal float? Minimum { get; set; }
        internal float? Maximum { get; set; }
        internal bool[] MixedComponents { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action<int, float> ComponentOnChange { get; set; }
        internal Action<Vector4> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }

    internal sealed class FloatTogglePropertyRowModel : RowModel
    {
        internal FloatTogglePropertyRowModel(string labelText)
            : base(RowItemType.FloatToggleProperty, labelText)
        {
        }

        internal float Value { get; set; }
        internal float OriginalValue { get; set; }
        internal float OffValue { get; set; }
        internal float OnValue { get; set; } = 1f;
        internal bool IsMixed { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action<float> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }
}
