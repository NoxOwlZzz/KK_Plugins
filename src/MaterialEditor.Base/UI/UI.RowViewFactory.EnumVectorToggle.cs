using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class EnumVectorToggleRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            CreateEnumRow(parent);
            CreateVectorRow(parent);
            CreateFloatToggleRow(parent);
        }

        private static void CreateEnumRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "EnumPanel", parent, ItemColor, true);
            var label = RowViewFactorySupport.CreateLabel(
                "EnumLabel", panel.transform, string.Empty, LabelWidth, 1f);
            RowViewFactorySupport.ConfigurePropertyLabel(label);
            label.gameObject.AddComponent<LabelClickTrigger>();
            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableEnumButton",
                panel.transform,
                "Select currently selected enum property as interpolable in timeline",
                timelineCapable: false);
            var dropdown = MaterialEditorControlFactory.CreateDropdown(
                "EnumDropdown", panel.transform);
            RowViewFactorySupport.SetWidth(dropdown, ContentFullWidth);
            var reset = MaterialEditorControlFactory.CreateButton(
                "EnumResetButton", panel.transform, MaterialEditorTheme.Glyphs.Reset);
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset the selected property to its original value");
        }

        private static void CreateVectorRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "VectorPanel", parent, ItemColor, true);
            var label = RowViewFactorySupport.CreateLabel(
                "VectorLabel", panel.transform, string.Empty, LabelWidth, 1f);
            RowViewFactorySupport.ConfigurePropertyLabel(label);
            label.gameObject.AddComponent<LabelClickTrigger>();
            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableVectorButton",
                panel.transform,
                "Select currently selected vector property as interpolable in timeline",
                timelineCapable: false);

            CreateVectorComponent(panel.transform, "X", "VectorXText", "VectorXInput");
            CreateVectorComponent(panel.transform, "Y", "VectorYText", "VectorYInput");
            CreateVectorComponent(panel.transform, "Z", "VectorZText", "VectorZInput");
            CreateVectorComponent(panel.transform, "W", "VectorWText", "VectorWInput");

            var reset = MaterialEditorControlFactory.CreateButton(
                "VectorResetButton", panel.transform, MaterialEditorTheme.Glyphs.Reset);
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset the selected property to its original value");
        }

        private static void CreateVectorComponent(
            Transform parent,
            string labelText,
            string labelName,
            string inputName)
        {
            var label = RowViewFactorySupport.CreateLabel(
                labelName,
                parent,
                labelText,
                MaterialEditorLayout.VectorComponentLabelWidth,
                0f);
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                label,
                MaterialEditorLayout.VectorComponentFontSize);
            var input = MaterialEditorControlFactory.CreateNumericInput(
                inputName, parent, NumericInputSpec.FloatingPoint);
            input.SetValue(0f);
            RowViewFactorySupport.SetWidth(
                input,
                MaterialEditorLayout.VectorComponentInputWidth);
            label.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                input.InputField);
        }

        private static void CreateFloatToggleRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "FloatTogglePanel", parent, ItemColor, true);
            var label = RowViewFactorySupport.CreateLabel(
                "FloatToggleLabel", panel.transform, string.Empty, LabelWidth, 1f);
            RowViewFactorySupport.ConfigurePropertyLabel(label);
            label.gameObject.AddComponent<LabelClickTrigger>();
            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableFloatToggleButton",
                panel.transform,
                "Select currently selected toggle property as interpolable in timeline",
                timelineCapable: false);
            var toggle = MaterialEditorControlFactory.CreateToggle(
                "FloatToggleToggle", panel.transform, string.Empty);
            RowViewFactorySupport.SetWidth(toggle, KeywordToggleWidth);
            var reset = MaterialEditorControlFactory.CreateButton(
                "FloatToggleResetButton", panel.transform, MaterialEditorTheme.Glyphs.Reset);
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset the selected property to its original value");
        }
    }
}
