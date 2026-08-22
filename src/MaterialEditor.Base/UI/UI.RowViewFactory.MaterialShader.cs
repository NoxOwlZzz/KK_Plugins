using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class MaterialShaderRowViewFactory
    {
        private const float CompactMaterialActionWidth = 60f;
        private const float MaterialCopyOrRemoveActionWidth = 78f;

        internal static void CreateRows(Transform parent)
        {
            CreateMaterialRow(parent);
            CreateShaderRow(parent);
            CreateRenderQueueRow(parent);
        }

        private static void CreateMaterialRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel("MaterialPanel", parent, MaterialColor);
            var collapse = MaterialEditorControlFactory.CreateButton(
                "MaterialCollapseButton",
                panel.transform,
                FoldGlyphs.Expanded);
            RowViewFactorySupport.SetWidth(collapse, SmallButtonWidth);
            TooltipManager.AddTooltip(
                collapse.gameObject,
                "Expand or collapse this material section");
            var materialName = RowViewFactorySupport.CreateLabel(
                "MaterialText",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            RowViewFactorySupport.ConfigurePropertyLabel(materialName);
            materialName.gameObject.AddComponent<LabelClickTrigger>();
            TooltipManager.AddTooltip(materialName.gameObject, "Material name");

            var rename = MaterialEditorControlFactory.CreateButton(
                "MaterialRenameButton",
                panel.transform,
                "Rename");
            RowViewFactorySupport.SetWidth(rename, CompactMaterialActionWidth);
            TooltipManager.AddTooltip(
                rename.gameObject,
                "Rename material instances");

            var copyEdits = MaterialEditorControlFactory.CreateButton(
                "MaterialCopyEditsButton",
                panel.transform,
                "Copy");
            RowViewFactorySupport.SetWidth(copyEdits, CompactMaterialActionWidth);
            TooltipManager.AddTooltip(
                copyEdits.gameObject,
                "Copy all edits from this material");

            var pasteEdits = MaterialEditorControlFactory.CreateButton(
                "MaterialPasteEditsButton",
                panel.transform,
                "Paste");
            RowViewFactorySupport.SetWidth(pasteEdits, CompactMaterialActionWidth);
            TooltipManager.AddTooltip(
                pasteEdits.gameObject,
                "Copy material edits before pasting");

            var copyOrRemove = MaterialEditorControlFactory.CreateButton(
                "MaterialCopyOrRemoveButton",
                panel.transform,
                "Duplicate");
            RowViewFactorySupport.SetWidth(
                copyOrRemove,
                MaterialCopyOrRemoveActionWidth);
            TooltipManager.AddTooltip(
                copyOrRemove.gameObject,
                "Make a copy of this material.\n\nUseful for overlaying different effects onto an object with different material shaders/properties");
        }
        private static void CreateShaderRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "ShaderPanel",
                parent,
                MaterialEditorStyles.ShaderColor);

            var label = RowViewFactorySupport.CreateLabel(
                "ShaderLabel",
                panel.transform,
                string.Empty,
                ShaderLabelMinimumWidth,
                1f);
            RowViewFactorySupport.ConfigurePropertyLabel(label);
            label.gameObject.AddComponent<LabelClickTrigger>();

            var categories = MaterialEditorControlFactory.CreateButton(
                "ShaderCategoriesCollapseButton",
                panel.transform,
                FoldGlyphs.AllExpanded);
            RowViewFactorySupport.SetWidth(categories, SmallButtonWidth);
            TooltipManager.AddTooltip(
                categories.gameObject,
                "Expand or collapse all property categories");

            var dropdown = MaterialEditorControlFactory.CreateDropdown(
                "ShaderDropdown",
                panel.transform);
            dropdown.transform.SetRect(
                0f, 0f, 0f, 1f,
                0f, 0f,
                MaterialEditorTheme.Metrics.DropdownTemplateWidth);
            dropdown.captionText.transform.SetRect(
                0f, 0f, 1f, 1f,
                MaterialEditorTheme.Spacing.DropdownCaptionLeftInset,
                MaterialEditorTheme.Spacing.DropdownCaptionVerticalInset,
                -MaterialEditorTheme.Spacing.DropdownCaptionRightInset,
                -MaterialEditorTheme.Spacing.DropdownCaptionVerticalInset);
            dropdown.captionText.alignment = TextAnchor.MiddleLeft;
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("Reset"));
            foreach (var shader in MaterialEditorPluginBase.XMLShaderProperties)
                if (shader.Key != "default")
                    dropdown.options.Add(new Dropdown.OptionData(shader.Key));
            var dropdownLayout = RowViewFactorySupport.SetWidth(
                dropdown,
                ShaderDropdownWidth);
            dropdownLayout.minWidth = ShaderDropdownMinimumWidth;

            var reset = MaterialEditorControlFactory.CreateButton(
                "ShaderResetButton",
                panel.transform,
                MaterialEditorTheme.Glyphs.Reset);
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset this property to its original value.\n\nIf the original shader is not one known by Material Editor, it will not be able to reset the shader to its original value. In order for the reset to take effect you to either save and re-load the scene, or copy the object and delete the old one");
        }

        private static void CreateRenderQueueRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "ShaderRenderQueuePanel",
                parent,
                ItemColor,
                true);
            var label = MaterialEditorControlFactory.CreateText(
                "ShaderRenderQueueLabel",
                panel.transform,
                string.Empty);
            label.gameObject.AddComponent<LabelClickTrigger>();
            label.alignment = TextAnchor.MiddleLeft;
            label.color = MaterialEditorTheme.Colors.PrimaryText;
            RowViewFactorySupport.ConfigurePropertyLabel(label);

            var input = MaterialEditorControlFactory.CreateInputField(
                "ShaderRenderQueueInput",
                panel.transform);
            input.text = "0";
            TooltipManager.AddTooltip(
                input.gameObject,
                "The order in which a material is rendered. Higher render queues get rendered later");

            var reset = MaterialEditorControlFactory.CreateButton(
                "ShaderRenderQueueResetButton",
                panel.transform,
                MaterialEditorTheme.Glyphs.Reset);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset this property to its original value");
        }

    }
}
