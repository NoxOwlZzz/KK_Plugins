using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal abstract class RowControls
    {
        private bool _visible;
        private bool _enabled = true;
        private readonly RowPanelInset _hierarchyInset;

        protected RowControls(CanvasGroup panel)
        {
            Panel = panel;
            _hierarchyInset = panel.GetComponent<RowPanelInset>()
                              ?? panel.gameObject.AddComponent<RowPanelInset>();
        }

        internal CanvasGroup Panel { get; }

        internal void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyState();
        }

        internal void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            ApplyState();
        }

        internal void SetHierarchyInset(float left, float right, float vertical)
        {
            _hierarchyInset.Configure(left, right, vertical);
        }

        private void ApplyState()
        {
            Panel.alpha = _visible
                ? (_enabled
                    ? MaterialEditorTheme.States.VisibleAlpha
                    : MaterialEditorTheme.States.DisabledAlpha)
                : MaterialEditorTheme.States.HiddenAlpha;
            Panel.interactable = _enabled;
            Panel.blocksRaycasts = _visible && _enabled;
        }
    }

    internal sealed class ToggleRowControls : RowControls
    {
        internal ToggleRowControls(
            CanvasGroup panel,
            Text label,
            Toggle toggle,
            Button resetButton,
            LabelClickTrigger labelClickTrigger = null,
            Button selectInterpolableButton = null)
            : base(panel)
        {
            Label = label;
            Toggle = toggle;
            ResetButton = resetButton;
            LabelClickTrigger = labelClickTrigger;
            SelectInterpolableButton = selectInterpolableButton;
        }

        internal Text Label { get; }
        internal Toggle Toggle { get; }
        internal Button ResetButton { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
    }

    internal sealed class DropdownRowControls : RowControls
    {
        internal DropdownRowControls(
            CanvasGroup panel,
            Text label,
            Dropdown dropdown,
            Button resetButton)
            : base(panel)
        {
            Label = label;
            Dropdown = dropdown;
            ResetButton = resetButton;
        }

        internal Text Label { get; }
        internal Dropdown Dropdown { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class InputRowControls : RowControls
    {
        internal InputRowControls(
            CanvasGroup panel,
            Text label,
            LabelClickTrigger labelClickTrigger,
            InputField input,
            Button resetButton)
            : base(panel)
        {
            Label = label;
            LabelClickTrigger = labelClickTrigger;
            Input = input;
            ResetButton = resetButton;
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal InputField Input { get; }
        internal Button ResetButton { get; }
    }

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

    internal sealed class TextureRowControls : RowControls
    {
        internal TextureRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("TexturePanel"))
        {
            Label = owner.GetUIComponent<Text>("TextureLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("TextureLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableTextureButton");
            ExportButton = owner.GetUIComponent<Button>("TextureExportButton");
            ImportButton = owner.GetUIComponent<Button>("TextureImportButton");
            ResetButton = owner.GetUIComponent<Button>("TextureResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ExportButton { get; }
        internal Button ImportButton { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class OffsetScaleRowControls : RowControls
    {
        internal OffsetScaleRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("OffsetScalePanel"))
        {
            Label = owner.GetUIComponent<Text>("OffsetScaleLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("OffsetScaleLabel");
            OffsetXLabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("OffsetXText");
            OffsetXInput = owner.GetUIComponent<NumericInputView>("OffsetXInput");
            OffsetYInput = owner.GetUIComponent<NumericInputView>("OffsetYInput");
            ScaleXInput = owner.GetUIComponent<NumericInputView>("ScaleXInput");
            ScaleYInput = owner.GetUIComponent<NumericInputView>("ScaleYInput");
            ResetButton = owner.GetUIComponent<Button>("OffsetScaleResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal LabelClickTrigger OffsetXLabelClickTrigger { get; }
        internal NumericInputView OffsetXInput { get; }
        internal NumericInputView OffsetYInput { get; }
        internal NumericInputView ScaleXInput { get; }
        internal NumericInputView ScaleYInput { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class ColorRowControls : RowControls
    {
        internal ColorRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("ColorPanel"))
        {
            Label = owner.GetUIComponent<Text>("ColorLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("ColorLabel");
            RInput = owner.GetUIComponent<NumericInputView>("ColorRInput");
            GInput = owner.GetUIComponent<NumericInputView>("ColorGInput");
            BInput = owner.GetUIComponent<NumericInputView>("ColorBInput");
            AInput = owner.GetUIComponent<NumericInputView>("ColorAInput");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableColorButton");
            ResetButton = owner.GetUIComponent<Button>("ColorResetButton");
            EditButton = owner.GetUIComponent<Button>("ColorEditButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal NumericInputView RInput { get; }
        internal NumericInputView GInput { get; }
        internal NumericInputView BInput { get; }
        internal NumericInputView AInput { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ResetButton { get; }
        internal Button EditButton { get; }
    }

    internal sealed class FloatRowControls : RowControls
    {
        internal FloatRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("FloatPanel"))
        {
            Label = owner.GetUIComponent<Text>("FloatLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("FloatLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableFloatButton");
            Slider = owner.GetUIComponent<Slider>("FloatSlider");
            Input = owner.GetUIComponent<NumericInputView>("FloatInputField");
            InputLayout = Input.GetComponent<RowColumnLayoutOverride>();
            ResetButton = owner.GetUIComponent<Button>("FloatResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Slider Slider { get; }
        internal NumericInputView Input { get; }
        internal RowColumnLayoutOverride InputLayout { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class EnumRowControls : RowControls
    {
        internal EnumRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("EnumPanel"))
        {
            Label = owner.GetUIComponent<Text>("EnumLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("EnumLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableEnumButton");
            Dropdown = owner.GetUIComponent<Dropdown>("EnumDropdown");
            OptionCache = new EnumDropdownOptionCache(Dropdown);
            ResetButton = owner.GetUIComponent<Button>("EnumResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Dropdown Dropdown { get; }
        internal EnumDropdownOptionCache OptionCache { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class VectorRowControls : RowControls
    {
        internal VectorRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("VectorPanel"))
        {
            Label = owner.GetUIComponent<Text>("VectorLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("VectorLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableVectorButton");
            ComponentLabels = new[]
            {
                owner.GetUIComponent<Text>("VectorXText"),
                owner.GetUIComponent<Text>("VectorYText"),
                owner.GetUIComponent<Text>("VectorZText"),
                owner.GetUIComponent<Text>("VectorWText")
            };
            ComponentInputs = new[]
            {
                owner.GetUIComponent<NumericInputView>("VectorXInput"),
                owner.GetUIComponent<NumericInputView>("VectorYInput"),
                owner.GetUIComponent<NumericInputView>("VectorZInput"),
                owner.GetUIComponent<NumericInputView>("VectorWInput")
            };
            ResetButton = owner.GetUIComponent<Button>("VectorResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Text[] ComponentLabels { get; }
        internal NumericInputView[] ComponentInputs { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class RowControlSet
    {
        private readonly List<RowControls> _rows;
        private readonly RowPanelInset _rowBackdropInset;

        private RowControlSet(RowBinder owner)
        {
            Owner = owner;
            _rowBackdropInset = owner.GetUIComponent<RowPanelInset>("RowBackdrop");
            Renderer = new RendererRowControls(owner);
            RendererEnabled = CreateToggle(owner, "RendererEnabled");
            RendererShadowCastingMode = new DropdownRowControls(
                owner.GetUIComponent<CanvasGroup>("RendererShadowCastingModePanel"),
                owner.GetUIComponent<Text>("RendererShadowCastingModeLabel"),
                owner.GetUIComponent<Dropdown>("RendererShadowCastingModeDropdown"),
                owner.GetUIComponent<Button>("RendererShadowCastingModeResetButton"));
            RendererReceiveShadows = CreateToggle(owner, "RendererReceiveShadows");
            RendererUpdateWhenOffscreen = CreateToggle(owner, "RendererUpdateWhenOffscreen");
            RendererRecalculateNormals = CreateToggle(owner, "RendererRecalculateNormals");
            Material = new MaterialRowControls(owner);
            Shader = new ShaderRowControls(owner);
            ShaderRenderQueue = new InputRowControls(
                owner.GetUIComponent<CanvasGroup>("ShaderRenderQueuePanel"),
                owner.GetUIComponent<Text>("ShaderRenderQueueLabel"),
                owner.GetUIComponent<LabelClickTrigger>("ShaderRenderQueueLabel"),
                owner.GetUIComponent<InputField>("ShaderRenderQueueInput"),
                owner.GetUIComponent<Button>("ShaderRenderQueueResetButton"));
            PropertyCategory = new PropertyCategoryRowControls(owner);
            PropertySubcategory = new PropertySubcategoryRowControls(owner);
            Texture = new TextureRowControls(owner);
            OffsetScale = new OffsetScaleRowControls(owner);
            Color = new ColorRowControls(owner);
            Float = new FloatRowControls(owner);
            Keyword = CreateToggle(owner, "Keyword", "KeywordLabel");
            FloatToggle = CreateToggle(
                owner,
                "FloatToggle",
                "FloatToggleLabel",
                "SelectInterpolableFloatToggleButton");
            Enum = new EnumRowControls(owner);
            Vector = new VectorRowControls(owner);

            _rows = new List<RowControls>
            {
                Renderer,
                RendererEnabled,
                RendererShadowCastingMode,
                RendererReceiveShadows,
                RendererUpdateWhenOffscreen,
                RendererRecalculateNormals,
                Material,
                Shader,
                ShaderRenderQueue,
                PropertyCategory,
                PropertySubcategory,
                Texture,
                OffsetScale,
                Color,
                Float,
                Keyword,
                FloatToggle,
                Enum,
                Vector
            };
        }

        internal RendererRowControls Renderer { get; }
        internal RowBinder Owner { get; }
        internal ToggleRowControls RendererEnabled { get; }
        internal DropdownRowControls RendererShadowCastingMode { get; }
        internal ToggleRowControls RendererReceiveShadows { get; }
        internal ToggleRowControls RendererUpdateWhenOffscreen { get; }
        internal ToggleRowControls RendererRecalculateNormals { get; }
        internal MaterialRowControls Material { get; }
        internal ShaderRowControls Shader { get; }
        internal InputRowControls ShaderRenderQueue { get; }
        internal PropertyCategoryRowControls PropertyCategory { get; }
        internal PropertySubcategoryRowControls PropertySubcategory { get; }
        internal TextureRowControls Texture { get; }
        internal OffsetScaleRowControls OffsetScale { get; }
        internal ColorRowControls Color { get; }
        internal FloatRowControls Float { get; }
        internal ToggleRowControls Keyword { get; }
        internal ToggleRowControls FloatToggle { get; }
        internal EnumRowControls Enum { get; }
        internal VectorRowControls Vector { get; }

        internal static RowControlSet Create(RowBinder owner)
        {
            return new RowControlSet(owner);
        }

        internal void HideAll()
        {
            foreach (var row in _rows)
                row.SetVisible(false);
        }

        // Apply a model's enabled state only to its active row family in the
        // pooled view.
        internal void SetEnabled(
            RowModel.RowItemType itemType,
            bool enabled)
        {
            // Category and Subcategory are structural controls and must stay
            // usable independently of a property row's enabled state.
            if (itemType == RowModel.RowItemType.PropertyCategory
                || itemType == RowModel.RowItemType.PropertySubcategory)
                return;

            RowControls row;
            if (!TryGetRow(itemType, out row))
                return;
            row.SetEnabled(enabled);
        }

        internal void SetHierarchyDepth(
            RowModel.RowItemType itemType,
            int depth)
        {
            var normalizedDepth = depth > 0 ? depth : 0;
            var left = MaterialEditorTheme.Metrics.SubcategoryContentIndent
                       * normalizedDepth;
            RowControls row;
            if (!TryGetRow(itemType, out row))
                return;
            var right = normalizedDepth > 0
                ? MaterialEditorTheme.Metrics.SubcategoryContentRightInset
                : 0f;
            var vertical = normalizedDepth > 0
                           && itemType == RowModel.RowItemType.PropertySubcategory
                ? MaterialEditorTheme.Metrics.SubcategoryHeaderVerticalInset
                : 0f;
            row.SetHierarchyInset(
                left,
                right,
                vertical);
            _rowBackdropInset.Configure(left, right, vertical);
        }

        private bool TryGetRow(
            RowModel.RowItemType itemType,
            out RowControls row)
        {
            switch (itemType)
            {
                case RowModel.RowItemType.Renderer:
                    row = Renderer;
                    break;
                case RowModel.RowItemType.RendererEnabled:
                    row = RendererEnabled;
                    break;
                case RowModel.RowItemType.RendererShadowCastingMode:
                    row = RendererShadowCastingMode;
                    break;
                case RowModel.RowItemType.RendererReceiveShadows:
                    row = RendererReceiveShadows;
                    break;
                case RowModel.RowItemType.RendererUpdateWhenOffscreen:
                    row = RendererUpdateWhenOffscreen;
                    break;
                case RowModel.RowItemType.RendererRecalculateNormals:
                    row = RendererRecalculateNormals;
                    break;
                case RowModel.RowItemType.Material:
                    row = Material;
                    break;
                case RowModel.RowItemType.Shader:
                    row = Shader;
                    break;
                case RowModel.RowItemType.ShaderRenderQueue:
                    row = ShaderRenderQueue;
                    break;
                case RowModel.RowItemType.PropertyCategory:
                    row = PropertyCategory;
                    break;
                case RowModel.RowItemType.PropertySubcategory:
                    row = PropertySubcategory;
                    break;
                case RowModel.RowItemType.TextureProperty:
                case RowModel.RowItemType.CubemapProperty:
                    row = Texture;
                    break;
                case RowModel.RowItemType.TextureOffsetScale:
                    row = OffsetScale;
                    break;
                case RowModel.RowItemType.ColorProperty:
                    row = Color;
                    break;
                case RowModel.RowItemType.FloatProperty:
                    row = Float;
                    break;
                case RowModel.RowItemType.KeywordProperty:
                    row = Keyword;
                    break;
                case RowModel.RowItemType.EnumProperty:
                    row = Enum;
                    break;
                case RowModel.RowItemType.VectorProperty:
                    row = Vector;
                    break;
                case RowModel.RowItemType.FloatToggleProperty:
                    row = FloatToggle;
                    break;
                default:
                    row = null;
                    return false;
            }
            return true;
        }

        private static ToggleRowControls CreateToggle(
            RowBinder owner,
            string prefix,
            string labelClickObjectName = null,
            string selectInterpolableObjectName = null)
        {
            return new ToggleRowControls(
                owner.GetUIComponent<CanvasGroup>($"{prefix}Panel"),
                owner.GetUIComponent<Text>($"{prefix}Label"),
                owner.GetUIComponent<Toggle>($"{prefix}Toggle"),
                owner.GetUIComponent<Button>($"{prefix}ResetButton"),
                labelClickObjectName == null
                    ? null
                    : owner.GetUIComponent<LabelClickTrigger>(labelClickObjectName),
                selectInterpolableObjectName == null
                    ? null
                    : owner.GetUIComponent<Button>(selectInterpolableObjectName));
        }

    }
}
