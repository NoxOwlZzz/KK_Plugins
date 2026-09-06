using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class RowViewFactorySupport
    {
        internal static Image CreatePanel(
            string name,
            Transform parent,
            Color color,
            bool insetPropertyLabel = false)
        {
            var panel = MaterialEditorControlFactory.CreatePanel(
                name,
                parent,
                ResolvePanelRole(name, color));
            panel.gameObject.AddComponent<CanvasGroup>();

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = insetPropertyLabel
                ? new RectOffset(
                    Padding.left + MaterialEditorLayout.PropertyLabelInset,
                    Padding.right,
                    Padding.top,
                    Padding.bottom)
                : Padding;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = MaterialEditorTheme.Spacing.Control;
            return panel;
        }

        private static MaterialEditorPanelRole ResolvePanelRole(
            string name,
            Color color)
        {
            if (name == "RendererPanel" || color == RendererColor)
                return MaterialEditorPanelRole.RendererRow;
            if (name == "MaterialPanel" || color == MaterialColor)
                return MaterialEditorPanelRole.MaterialRow;
            if (name == "ShaderPanel"
                || color == MaterialEditorStyles.ShaderColor)
                return MaterialEditorPanelRole.ShaderRow;
            if (name == "PropertyCategoryPanel" || color == CategoryColor)
                return MaterialEditorPanelRole.CategoryRow;
            if (name == "PropertySubcategoryPanel"
                || color == SubcategoryColor)
                return MaterialEditorPanelRole.SubcategoryRow;
            return MaterialEditorPanelRole.PropertyRow;
        }

        internal static Text CreateLabel(
            string name,
            Transform parent,
            string value,
            float width,
            float flexibleWidth)
        {
            var label = MaterialEditorControlFactory.CreateText(name, parent, value);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = MaterialEditorTheme.Colors.PrimaryText;
            SetWidth(label, width, flexibleWidth);
            return label;
        }

        internal static void ConfigurePropertyLabel(Text label)
        {
            if (label == null)
                return;

            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                label,
                MaterialEditorTheme.Typography.PrimaryFontSize);
        }

        internal static LayoutElement SetWidth(
            Component component,
            float width,
            float flexibleWidth = 0f)
        {
            var layout = component.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = flexibleWidth;
            return layout;
        }

        internal static void CreateInterpolableButton(
            string objectName,
            Transform parent,
            string tooltipText,
            bool layoutOwnedBySpec = false,
            bool timelineCapable = true)
        {
            var button = MaterialEditorControlFactory.CreateButton(
                objectName,
                parent,
                MaterialEditorTheme.Glyphs.Interpolable);
            if (!layoutOwnedBySpec)
                SetWidth(button, InterpolableButtonWidth);

            button.gameObject.SetActive(false);
            TooltipManager.AddTooltip(button.gameObject, tooltipText);

#if !API && !EC
            if (timelineCapable && TimelineCompatibilityHelper.IsTimelineAvailable())
                button.gameObject.SetActive(true);
#endif
        }
    }
}
