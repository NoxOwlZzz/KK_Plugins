using UnityEngine;
using UnityEngine.UI;
using UILib;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class TextureRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            CreateCategoryRow(parent);
            CreateSubcategoryRow(parent);
            CreateTextureRow(parent);
            CreateOffsetScaleRow(parent);
        }

        private static void CreateCategoryRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "PropertyCategoryPanel",
                parent,
                CategoryColor);
            panel.GetComponent<HorizontalLayoutGroup>().spacing =
                MaterialEditorTheme.Spacing.PropertyCategorySpacing;

            var headerButton = panel.gameObject.AddComponent<Button>();
            panel.raycastTarget = true;
            headerButton.targetGraphic = panel;
            MaterialEditorStyles.ApplyPropertyCategoryButton(headerButton);

            // PropertyCategoryCollapseButton remains the lookup name used by UI
            // hooks. The glyph is passive; the panel is the sole clickable surface.
            var collapseIndicator = MaterialEditorControlFactory.CreateText(
                "PropertyCategoryCollapseButton",
                panel.transform,
                FoldGlyphs.Expanded,
                MaterialEditorTextRole.Button);
            collapseIndicator.alignment = TextAnchor.MiddleCenter;
            collapseIndicator.raycastTarget = false;
            RowViewFactorySupport.SetWidth(
                collapseIndicator,
                MaterialEditorTheme.Metrics.FoldIndicatorWidth);

            var label = RowViewFactorySupport.CreateLabel(
                "PropertyCategoryLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            label.fontStyle = MaterialEditorTheme.Mode
                              == MaterialEditorThemeMode.Legacy
                ? FontStyle.Normal
                : FontStyle.Bold;
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                label,
                MaterialEditorTheme.Typography.PrimaryFontSize);
            label.raycastTarget = false;

        }

        private static void CreateSubcategoryRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "PropertySubcategoryPanel",
                parent,
                SubcategoryColor);
            panel.GetComponent<HorizontalLayoutGroup>().spacing =
                MaterialEditorTheme.Spacing.PropertySubcategorySpacing;

            var headerButton = panel.gameObject.AddComponent<Button>();
            panel.raycastTarget = true;
            headerButton.targetGraphic = panel;
            MaterialEditorStyles.ApplyPropertySubcategoryButton(headerButton);

            var collapseIndicator = MaterialEditorControlFactory.CreateText(
                "PropertySubcategoryCollapseButton",
                panel.transform,
                FoldGlyphs.Expanded,
                MaterialEditorTextRole.Button);
            collapseIndicator.alignment = TextAnchor.MiddleCenter;
            collapseIndicator.raycastTarget = false;
            RowViewFactorySupport.SetWidth(
                collapseIndicator,
                MaterialEditorTheme.Metrics.FoldIndicatorWidth);

            var label = RowViewFactorySupport.CreateLabel(
                "PropertySubcategoryLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            label.fontStyle = FontStyle.Normal;
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                label,
                MaterialEditorTheme.Typography.SecondaryFontSize);
            label.raycastTarget = false;

        }

        private static void CreateTextureRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "TexturePanel",
                parent,
                ItemColor,
                true);
            var label = RowViewFactorySupport.CreateLabel(
                "TextureLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            RowViewFactorySupport.ConfigurePropertyLabel(label);
            label.gameObject.AddComponent<LabelClickTrigger>();

            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableTextureButton",
                panel.transform,
                "Select the currently selected texture property and its offset and scale properties as interpolables in timeline");

            var export = MaterialEditorControlFactory.CreateButton(
                "TextureExportButton",
                panel.transform,
                "Export Texture");
            RowViewFactorySupport.SetWidth(export, TextureButtonWidth);

            var import = MaterialEditorControlFactory.CreateButton(
                "TextureImportButton",
                panel.transform,
                "Import Texture");
            RowViewFactorySupport.SetWidth(import, TextureButtonWidth);

            var reset = MaterialEditorControlFactory.CreateButton(
                "TextureResetButton",
                panel.transform,
                MaterialEditorTheme.Glyphs.Reset);
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
        }

        private static void CreateOffsetScaleRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "OffsetScalePanel",
                parent,
                ItemColor,
                true);
            // The coordinate labels and inputs must fit without shifting OffsetX
            // into the Timeline ("O") column. Use zero inter-control spacing; the
            // explicit group spacer is the sole Offset/Scale separation.
            panel.GetComponent<HorizontalLayoutGroup>().spacing = 0f;

            var label = MaterialEditorControlFactory.CreateText(
                "OffsetScaleLabel",
                panel.transform,
                string.Empty);
            label.gameObject.AddComponent<LabelClickTrigger>();
            label.alignment = TextAnchor.MiddleLeft;
            label.color = MaterialEditorTheme.Colors.PrimaryText;
            RowViewFactorySupport.ConfigurePropertyLabel(label);

            var emptySpace = MaterialEditorControlFactory.CreateText(
                "EmptySpace",
                panel.transform,
                string.Empty);
            emptySpace.alignment = TextAnchor.MiddleLeft;

            var offsetXLabel = CreateCoordinateLabel(
                "OffsetXText",
                panel.transform,
                "OffsetX");
            offsetXLabel.gameObject.AddComponent<LabelClickTrigger>();
            var offsetX = CreateNumericInput(
                "OffsetXInput",
                panel.transform,
                "Adjust the horizontal offset of the texture. It can move the texture left or right.");

            var offsetYLabel = CreateCoordinateLabel(
                "OffsetYText",
                panel.transform,
                "Y");
            var offsetY = CreateNumericInput(
                "OffsetYInput",
                panel.transform,
                "Adjust the vertical offset of the texture. It can move the texture up or down.");

            offsetXLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                offsetX.InputField,
                new[] { offsetY.InputField });
            offsetYLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                offsetY.InputField,
                new[] { offsetX.InputField });

            var scaleGroupSpacer = MaterialEditorControlFactory.CreateText(
                "OffsetScaleGroupSpacer",
                panel.transform,
                string.Empty);
            scaleGroupSpacer.raycastTarget = false;

            var scaleXLabel = CreateCoordinateLabel(
                "ScaleXText",
                panel.transform,
                "ScaleX");
            var scaleX = CreateNumericInput(
                "ScaleXInput",
                panel.transform,
                "Adjust the horizontal scale of the texture. Values greater than 1 make the texture appear smaller horizontally, values less than 1 make it appear larger horizontally.");

            var scaleYLabel = CreateCoordinateLabel(
                "ScaleYText",
                panel.transform,
                "Y");
            var scaleY = CreateNumericInput(
                "ScaleYInput",
                panel.transform,
                "Adjust the vertical scale of the texture. Values greater than 1 make the texture appear smaller vertically, values less than 1 make it appear larger vertically.");

            var reset = MaterialEditorControlFactory.CreateButton(
                "OffsetScaleResetButton",
                panel.transform,
                MaterialEditorTheme.Glyphs.Reset);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset both the scale and offset properties to their original values");

            scaleXLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                scaleX.InputField,
                new[] { scaleY.InputField });
            scaleYLabel.gameObject.AddComponent<FloatLabelDragTrigger>().Initialize(
                scaleY.InputField,
                new[] { scaleX.InputField });
        }

        private static Text CreateCoordinateLabel(
            string name,
            Transform parent,
            string text)
        {
            var label = MaterialEditorControlFactory.CreateText(name, parent, text);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = MaterialEditorTheme.Colors.PrimaryText;
            return label;
        }

        private static NumericInputView CreateNumericInput(
            string name,
            Transform parent,
            string tooltip)
        {
            var input = MaterialEditorControlFactory.CreateNumericInput(
                name,
                parent,
                NumericInputSpec.FloatingPoint);
            input.SetValue(0f);
            TooltipManager.AddTooltip(input.gameObject, tooltip);
            return input;
        }
    }
}
