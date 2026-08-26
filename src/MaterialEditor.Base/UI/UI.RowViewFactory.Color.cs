using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class ColorRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "ColorPanel",
                parent,
                ItemColor,
                true);
            panel.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;

            var label = RowViewFactorySupport.CreateLabel(
                "ColorLabel",
                panel.transform,
                string.Empty,
                0f,
                0f);
            RowViewFactorySupport.ConfigurePropertyLabel(label);
            label.gameObject.AddComponent<LabelClickTrigger>();

            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableColorButton",
                panel.transform,
                "Select currently selected color property as interpolable in timeline",
                true);

            var editorGroup = CreateEditorGroup(panel.transform);
            var red = CreateChannel(editorGroup, "R", "ColorRText", "ColorRInput");
            var green = CreateChannel(editorGroup, "G", "ColorGText", "ColorGInput");
            var blue = CreateChannel(editorGroup, "B", "ColorBText", "ColorBInput");
            var alpha = CreateChannel(editorGroup, "A", "ColorAText", "ColorAInput");

            var edit = MaterialEditorControlFactory.CreateSwatchButton(
                "ColorEditButton",
                editorGroup);
            RowViewFactorySupport.SetWidth(
                edit,
                MaterialEditorLayout.ColorEditButtonWidth);

            var reset = MaterialEditorControlFactory.CreateButton(
                "ColorResetButton",
                panel.transform,
                MaterialEditorTheme.Glyphs.Reset);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset the selected property to its original value");

            red.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                red.Input.InputField,
                new[] { green.Input.InputField, blue.Input.InputField });
            green.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                green.Input.InputField,
                new[] { red.Input.InputField, blue.Input.InputField });
            blue.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                blue.Input.InputField,
                new[] { red.Input.InputField, green.Input.InputField });
            alpha.Label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                alpha.Input.InputField);
        }

        private static Transform CreateEditorGroup(Transform parent)
        {
            var groupObject = new GameObject(
                "ColorEditorGroup",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            groupObject.transform.SetParent(parent, false);

            var layout = groupObject.GetComponent<HorizontalLayoutGroup>();
            // RGBA shares the compact 316 px editor budget. Keep its channels
            // gapless so the common Timeline/editor anchor is not shifted by
            // the eight child elements.
            layout.padding = new RectOffset(
                0,
                (int)MaterialEditorTheme.Spacing.Control,
                0,
                0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 0f;
            return groupObject.transform;
        }

        private static ColorChannelControls CreateChannel(
            Transform parent,
            string channel,
            string labelName,
            string inputName)
        {
            var label = MaterialEditorControlFactory.CreateText(labelName, parent, channel);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = MaterialEditorTheme.Colors.PrimaryText;
            RowViewFactorySupport.SetWidth(
                label,
                MaterialEditorLayout.ColorLabelWidth);

            var input = MaterialEditorControlFactory.CreateNumericInput(
                inputName,
                parent,
                NumericInputSpec.FloatingPoint);
            input.SetValue(0f);
            RowViewFactorySupport.SetWidth(
                input,
                MaterialEditorLayout.ColorInputWidth);

            return new ColorChannelControls(label, input);
        }

        private sealed class ColorChannelControls
        {
            internal ColorChannelControls(Text label, NumericInputView input)
            {
                Label = label;
                Input = input;
            }

            internal Text Label { get; }
            internal NumericInputView Input { get; }
        }
    }
}
