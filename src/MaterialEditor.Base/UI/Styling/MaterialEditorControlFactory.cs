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
}
