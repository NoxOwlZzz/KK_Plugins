using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class RendererRowControls : RowControls
    {
        internal RendererRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("RendererPanel"))
        {
            HeaderButton = owner.GetUIComponent<Button>("RendererPanel");
            CollapseButton = owner.GetUIComponent<Button>("RendererCollapseButton");
            Name = owner.GetUIComponent<Text>("RendererText");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("RendererText");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableRendererButton");
            ExportUvsButton = owner.GetUIComponent<Button>(
                "RendererExportUvsButton");
            ExportMeshButton = owner.GetUIComponent<Button>(
                "RendererExportMeshButton");
        }

        internal Button HeaderButton { get; }
        internal Button CollapseButton { get; }
        internal Text Name { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ExportUvsButton { get; }
        internal Button ExportMeshButton { get; }
    }

    internal sealed class MaterialRowControls : RowControls
    {
        internal MaterialRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("MaterialPanel"))
        {
            CollapseButton = owner.GetUIComponent<Button>("MaterialCollapseButton");
            Name = owner.GetUIComponent<Text>("MaterialText");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("MaterialText");
            RenameButton = owner.GetUIComponent<Button>("MaterialRenameButton");
            CopyEditsButton = owner.GetUIComponent<Button>("MaterialCopyEditsButton");
            PasteEditsButton = owner.GetUIComponent<Button>("MaterialPasteEditsButton");
            PasteEditsLabel = PasteEditsButton.GetComponentInChildren<Text>();
            PasteEditsTooltip = PasteEditsButton.GetComponent<Tooltip>();
            CopyOrRemoveButton = owner.GetUIComponent<Button>(
                "MaterialCopyOrRemoveButton");
            CopyOrRemoveLabel = CopyOrRemoveButton.GetComponentInChildren<Text>();
            CopyOrRemoveTooltip = CopyOrRemoveButton.GetComponent<Tooltip>();
        }

        internal Button CollapseButton { get; }
        internal Text Name { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button RenameButton { get; }
        internal Button CopyEditsButton { get; }
        internal Button PasteEditsButton { get; }
        internal Text PasteEditsLabel { get; }
        internal Tooltip PasteEditsTooltip { get; }
        internal Button CopyOrRemoveButton { get; }
        internal Text CopyOrRemoveLabel { get; }
        internal Tooltip CopyOrRemoveTooltip { get; }
    }
    internal sealed class ShaderRowControls : RowControls
    {
        internal ShaderRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("ShaderPanel"))
        {
            CategoriesCollapseButton = owner.GetUIComponent<Button>("ShaderCategoriesCollapseButton");
            Label = owner.GetUIComponent<Text>("ShaderLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("ShaderLabel");
            Dropdown = owner.GetUIComponent<Dropdown>("ShaderDropdown");
            OptionCache = new ShaderDropdownOptionCache(Dropdown);
            ResetButton = owner.GetUIComponent<Button>("ShaderResetButton");
        }

        internal Button CategoriesCollapseButton { get; }
        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Dropdown Dropdown { get; }
        internal ShaderDropdownOptionCache OptionCache { get; }
        internal Button ResetButton { get; }
    }

    /// <summary>
    /// Keeps an unavailable current shader selectable without mistaking the
    /// Reset sentinel at option zero for that shader. The temporary option is
    /// neutralized whenever the pooled row leaves its shader context.
    /// </summary>
    internal sealed class ShaderDropdownOptionCache
    {
        private readonly Dropdown _dropdown;
        private Dropdown.OptionData _unavailableOption;
        private bool _unavailableOptionActive;

        internal ShaderDropdownOptionCache(Dropdown dropdown)
        {
            _dropdown = dropdown;
        }

        internal int PrepareSelection(string shaderName)
        {
            RemoveUnavailableOption();
            for (var index = 1; index < _dropdown.options.Count; index++)
            {
                var option = _dropdown.options[index];
                if (option != null
                    && string.Equals(option.text, shaderName,
                        System.StringComparison.Ordinal))
                    return index;
            }

            if (string.IsNullOrEmpty(shaderName))
                return _dropdown.options.Count == 0 ? -1 : 0;

            if (_unavailableOption == null)
                _unavailableOption = new Dropdown.OptionData();
            _unavailableOption.text = shaderName;
            _unavailableOption.image = null;
            _dropdown.options.Add(_unavailableOption);
            _unavailableOptionActive = true;
            return _dropdown.options.Count - 1;
        }

        internal void ReleaseContext()
        {
            RemoveUnavailableOption();
            if (_unavailableOption == null)
                return;
            _unavailableOption.text = string.Empty;
            _unavailableOption.image = null;
        }

        private void RemoveUnavailableOption()
        {
            if (!_unavailableOptionActive)
                return;
            for (var index = _dropdown.options.Count - 1; index >= 0; index--)
            {
                if (!ReferenceEquals(_dropdown.options[index], _unavailableOption))
                    continue;
                _dropdown.options.RemoveAt(index);
                break;
            }
            _unavailableOptionActive = false;
        }
    }

    internal sealed class PropertyCategoryRowControls : RowControls
    {
        internal PropertyCategoryRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("PropertyCategoryPanel"))
        {
            HeaderButton = owner.GetUIComponent<Button>("PropertyCategoryPanel");
            CollapseIndicator = owner.GetUIComponent<Text>("PropertyCategoryCollapseButton");
            Label = owner.GetUIComponent<Text>("PropertyCategoryLabel");
        }

        internal Button HeaderButton { get; }
        internal Text CollapseIndicator { get; }
        internal Text Label { get; }
    }

    internal sealed class PropertySubcategoryRowControls : RowControls
    {
        internal PropertySubcategoryRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("PropertySubcategoryPanel"))
        {
            HeaderButton = owner.GetUIComponent<Button>("PropertySubcategoryPanel");
            CollapseIndicator = owner.GetUIComponent<Text>(
                "PropertySubcategoryCollapseButton");
            Label = owner.GetUIComponent<Text>("PropertySubcategoryLabel");
        }

        internal Button HeaderButton { get; }
        internal Text CollapseIndicator { get; }
        internal Text Label { get; }
    }
}
