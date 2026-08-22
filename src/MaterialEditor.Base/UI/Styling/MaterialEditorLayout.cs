using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorLayout
    {
        internal const float Margin = MaterialEditorTheme.Metrics.Margin;
        internal const float HeaderHeight = MaterialEditorTheme.Metrics.HeaderHeight;
        internal const float ScrollbarOffset = MaterialEditorTheme.Metrics.ScrollbarOffset;
        internal const float RowHeight = MaterialEditorTheme.Metrics.RowHeight;
        internal const float CategoryNavigatorWidth = MaterialEditorTheme.Metrics.CategoryNavigatorWidth;
        internal const int PropertyLabelInset = MaterialEditorTheme.Spacing.PropertyLabelInset;

        internal const float LabelWidth = MaterialEditorTheme.Metrics.LabelWidth;
        internal const float ButtonWidth = MaterialEditorTheme.Metrics.ButtonWidth;
        internal const float SmallButtonWidth = MaterialEditorTheme.Metrics.SmallButtonWidth;
        internal const float ResetButtonWidth = MaterialEditorTheme.Metrics.ResetButtonWidth;
        internal const float InterpolableButtonWidth = MaterialEditorTheme.Metrics.InterpolableButtonWidth;
        internal const float ContentWidth = MaterialEditorTheme.Metrics.ContentWidth;

        internal const float RendererButtonWidth = MaterialEditorTheme.Metrics.RendererButtonWidth;
        internal const float RendererToggleWidth = MaterialEditorTheme.Metrics.RendererToggleWidth;
        internal const float RendererDropdownWidth = MaterialEditorTheme.Metrics.RendererDropdownWidth;
        internal const float MaterialButtonWidth = MaterialEditorTheme.Metrics.MaterialButtonWidth;
        internal const float MaterialRenameButtonWidth = MaterialEditorTheme.Metrics.MaterialRenameButtonWidth;
        internal const float ShaderLabelMinimumWidth = MaterialEditorTheme.Metrics.ShaderLabelMinimumWidth;
        internal const float ShaderDropdownMinimumWidth = MaterialEditorTheme.Metrics.ShaderDropdownMinimumWidth;
        internal const float ShaderDropdownWidth = MaterialEditorTheme.Metrics.ShaderDropdownWidth;
        internal const float RenderQueueInputWidth = MaterialEditorTheme.Metrics.RenderQueueInputWidth;
        internal const float OffsetScaleLabelXWidth = MaterialEditorTheme.Metrics.OffsetScaleLabelXWidth;
        internal const float OffsetScaleLabelYWidth = MaterialEditorTheme.Metrics.OffsetScaleLabelYWidth;
        internal const float OffsetScaleInputWidth = MaterialEditorTheme.Metrics.OffsetScaleInputWidth;
        internal const float ColorLabelWidth = MaterialEditorTheme.Metrics.ColorLabelWidth;
        internal const float ColorInputWidth = MaterialEditorTheme.Metrics.ColorInputWidth;
        internal const float ColorEditButtonWidth = MaterialEditorTheme.Metrics.ColorEditButtonWidth;
        internal const float FloatSliderWidth = MaterialEditorTheme.Metrics.FloatSliderWidth;
        internal const float FloatInputWidth = MaterialEditorTheme.Metrics.FloatInputWidth;
        internal const float VectorComponentLabelWidth = MaterialEditorTheme.Metrics.VectorComponentLabelWidth;
        internal const float VectorComponentInputWidth = MaterialEditorTheme.Metrics.VectorComponentInputWidth;
        internal const int VectorComponentFontSize = MaterialEditorTheme.Typography.VectorComponentFontSize;
        internal const int VectorComponentMinimumFontSize = MaterialEditorTheme.Typography.VectorComponentMinimumFontSize;
        internal const float KeywordToggleWidth = MaterialEditorTheme.Metrics.KeywordToggleWidth;

        internal const int DropdownFontSize = MaterialEditorTheme.Typography.DropdownFontSize;
        internal const int DropdownMinimumFontSize = MaterialEditorTheme.Typography.DropdownMinimumFontSize;
        internal const float DropdownTextVerticalInset = MaterialEditorTheme.Spacing.DropdownTextVerticalInset;

        internal static readonly RectOffset RowPadding = new RectOffset(
            MaterialEditorTheme.Spacing.RowPaddingLeft,
            MaterialEditorTheme.Spacing.RowPaddingRight,
            MaterialEditorTheme.Spacing.RowPaddingTop,
            MaterialEditorTheme.Spacing.RowPaddingBottom);
    }
}
