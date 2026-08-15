using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class MaterialShaderRowTypeBinder : IRowTypeBinder
    {
        private readonly RowControlSet _controls;

        internal MaterialShaderRowTypeBinder(RowControlSet controls)
        {
            _controls = controls;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            switch (item.ItemType)
            {
                case RowModel.RowItemType.Material:
                    BindMaterial((MaterialRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.Shader:
                    BindShader((ShaderRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.ShaderRenderQueue:
                    BindRenderQueue((ShaderRenderQueueRowModel)item, listeners);
                    break;
            }
        }

        private void BindMaterial(MaterialRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Material;
            controls.SetVisible(true);
            controls.CollapseButton.GetComponentInChildren<Text>().text =
                item.Collapsed
                    ? MaterialEditorTheme.Glyphs.MaterialCollapsed
                    : MaterialEditorTheme.Glyphs.MaterialExpanded;
            listeners.Listen(
                controls.CollapseButton, () => item.CollapsedOnChange(!item.Collapsed));
            controls.Name.text = item.MaterialName;
            TooltipBinding.Bind(
                controls.Name.gameObject,
                item.TooltipText,
                item.MaterialName,
                controls.Name);
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.Material,
                () => item.MaterialName);

            System.Action refreshPasteAvailability = () =>
            {
                var clipboard = MaterialEditorPluginBase.CopyData;
                var canPaste = MaterialEditorClipboardPolicy.CanPaste(
                    clipboard,
                    item.Material,
                    item.Projector);
                controls.PasteEditsButton.interactable = canPaste;
                if (controls.PasteEditsLabel != null)
                {
                    controls.PasteEditsLabel.color = canPaste
                        ? MaterialEditorTheme.Colors.PrimaryText
                        : MaterialEditorTheme.Colors.DisabledText;
                }
                if (controls.PasteEditsTooltip != null)
                {
                    controls.PasteEditsTooltip.SetStandardTooltipText(
                        canPaste
                            ? "Paste all copied edits into this material"
                            : clipboard == null || clipboard.IsEmpty
                                ? "Copy material edits before pasting"
                                : "Copied edits are not compatible with this material");
                }
            };
            refreshPasteAvailability();
            _controls.Owner.ListenForClipboardChanges(
                listeners,
                refreshPasteAvailability);

            listeners.Listen(controls.CopyEditsButton, () =>
            {
                item.Copy();
            });
            listeners.Listen(controls.PasteEditsButton, () =>
            {
                if (MaterialEditorClipboardPolicy.CanPaste(
                    MaterialEditorPluginBase.CopyData,
                    item.Material,
                    item.Projector))
                    item.Paste();
            });

            listeners.Listen(
                controls.ActionMenuButton,
                () => _controls.Owner.OpenMaterialActionMenu(
                    controls.ActionMenuButton.transform as RectTransform,
                    item.CopyOrRemove,
                    item.MaterialName.Contains(MaterialAPI.MaterialCopyPostfix)
                        ? "Remove Material"
                        : "Copy Material",
                    item.Rename));
        }

        private void BindShader(ShaderRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Shader;
            controls.SetVisible(true);
            TooltipBinding.Bind(
                controls.Label.gameObject,
                item.TooltipText,
                item.ShaderName,
                controls.Label);
            controls.CollapseButton.GetComponentInChildren<Text>().text =
                item.Collapsed ? FoldGlyphs.Collapsed : FoldGlyphs.Expanded;
            listeners.Listen(
                controls.CollapseButton, () => item.CollapsedOnChange(!item.Collapsed));

            controls.CategoriesCollapseButton.gameObject.SetActive(item.HasCategories);
            controls.CategoriesCollapseButton.GetComponentInChildren<Text>().text =
                item.AllCategoriesCollapsed
                    ? FoldGlyphs.AllCollapsed
                    : FoldGlyphs.AllExpanded;
            if (item.HasCategories)
                listeners.Listen(
                    controls.CategoriesCollapseButton,
                    () => item.CategoriesCollapsedOnChange(!item.AllCategoriesCollapsed));

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.ShaderName != item.OriginalShaderName,
                    controls.ResetButton,
                    controls.Panel);

            var selectedIndex = controls.OptionCache.PrepareSelection(item.ShaderName);
            if (selectedIndex >= 0)
                controls.Dropdown.Set(selectedIndex);
            controls.Dropdown.captionText.text = item.ShaderName;
            refresh();

            listeners.Listen(controls.Dropdown, value =>
            {
                var selected = controls.Dropdown.OptionText(value);
                if (value == 0 || selected.IsNullOrEmpty())
                    selected = item.OriginalShaderName;
                item.ShaderName = selected;

                if (item.ShaderName == item.OriginalShaderName)
                    item.ShaderNameOnReset();
                else
                    item.ShaderNameOnChange(item.ShaderName);
                MaterialEditorExtensionRegistry.RaiseRowSelection(
                    item,
                    MaterialEditorSelectionType.Shader,
                    MaterialEditorSelectionAction.Selected,
                    item.ShaderName);
                refresh();
            });
            listeners.Listen(
                controls.ResetButton,
                () =>
                {
                    var resetIndex = controls.OptionCache.PrepareSelection(
                        item.OriginalShaderName);
                    if (resetIndex >= 0)
                        controls.Dropdown.value = resetIndex;
                });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolable());

            AutoScrollToSelectionWithDropdown.Setup(controls.Dropdown);
            DropdownFilter.AddFilterUI(controls.Dropdown, "ShaderDropDown");
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.Shader,
                () => item.ShaderName);
        }

        private void BindRenderQueue(ShaderRenderQueueRowModel item, ListenerScope listeners)
        {
            var controls = _controls.ShaderRenderQueue;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);

            InputFieldBinding.BindInt(
                listeners,
                controls.Input,
                () => item.Value,
                value =>
                {
                    item.Value = value;
                    if (item.Value == item.OriginalValue)
                        item.ValueOnReset();
                    else
                        item.ValueOnChange(item.Value);
                    refresh();
                });
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                controls.Input.Set(item.Value.ToString(), false);
                item.ValueOnReset();
                refresh();
            });
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.ShaderRenderQueue,
                () => item.LabelText);
        }
    }
}
