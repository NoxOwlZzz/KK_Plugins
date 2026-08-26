using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorControlFactory
    {
        internal static Canvas CreateNewUISystem(string name)
        {
            return UIUtility.CreateNewUISystem(name);
        }

        internal static Image CreatePanel(string name, Transform parent, MaterialEditorPanelRole role = MaterialEditorPanelRole.Default)
        {
            var panel = UIUtility.CreatePanel(name, parent);
            MaterialEditorStyles.ApplyPanel(panel, role);
            return panel;
        }

        internal static Image CreateStencilMaskPanel(string name, Transform parent)
        {
            var panel = CreatePanel(
                name,
                parent,
                MaterialEditorPanelRole.StencilMask);
            var mask = panel.GetComponent<Mask>()
                       ?? panel.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            return panel;
        }

        internal static Image CreateRowStencilMaskPanel(
            string name,
            Transform parent)
        {
            var panel = CreatePanel(
                name,
                parent,
                MaterialEditorPanelRole.RowStencilMask);
            var mask = panel.GetComponent<Mask>()
                       ?? panel.gameObject.AddComponent<Mask>();
            // CreatePanel applied the role before Mask existed.
            MaterialEditorStyles.ApplyPanel(
                panel,
                MaterialEditorPanelRole.RowStencilMask);
            return panel;
        }

        internal static Text CreateText(string name, Transform parent, string value = "", MaterialEditorTextRole role = MaterialEditorTextRole.PreserveHorizontal)
        {
            var text = UIUtility.CreateText(name, parent, value);
            MaterialEditorStyles.ApplyText(text, role);
            return text;
        }

        internal static Button CreateButton(string name, Transform parent, string value)
        {
            var button = UIUtility.CreateButton(name, parent, value);
            MaterialEditorStyles.ApplyButton(button);
            return button;
        }

        internal static Button CreateSwatchButton(string name, Transform parent)
        {
            var button = UIUtility.CreateButton(name, parent, string.Empty);
            MaterialEditorStyles.ApplySwatchButton(button);
            return button;
        }

        internal static InputField CreateInputField(
            string name,
            Transform parent,
            string placeholder = "")
        {
            var inputField = UIUtility.CreateInputField(name, parent, placeholder);
            MaterialEditorStyles.ApplyInputField(inputField);
            return inputField;
        }

        internal static NumericInputView CreateNumericInput(
            string name,
            Transform parent,
            NumericInputSpec spec)
        {
            var inputField = CreateInputField(name, parent);
            var view = inputField.gameObject.AddComponent<NumericInputView>();
            view.Initialize(spec);
            return view;
        }

        internal static Toggle CreateToggle(string name, Transform parent, string value)
        {
            var toggle = UIUtility.CreateToggle(name, parent, value);
            MaterialEditorStyles.ApplyToggle(toggle);
            return toggle;
        }

        internal static Dropdown CreateDropdown(string name, Transform parent)
        {
            var dropdown = UIUtility.CreateDropdown(name, parent);
            MaterialEditorStyles.ApplyDropdown(dropdown);
            return dropdown;
        }

        internal static Slider CreateSlider(string name, Transform parent)
        {
            var slider = UIUtility.CreateSlider(name, parent);
            MaterialEditorStyles.ApplySlider(slider);
            return slider;
        }

        internal static ScrollRect CreateScrollView(string name, Transform parent)
        {
            var scrollView = UIUtility.CreateScrollView(name, parent);
            MaterialEditorStyles.ApplyScrollView(scrollView);
            return scrollView;
        }
    }

    internal static class MaterialEditorLayout
    {
        internal const float Margin = MaterialEditorTheme.Metrics.Margin;
        internal const float HeaderHeight = MaterialEditorTheme.Metrics.HeaderHeight;
        internal const float ScrollbarOffset = MaterialEditorTheme.Metrics.ScrollbarOffset;
        internal const float RowHeight = MaterialEditorTheme.Metrics.RowHeight;
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
