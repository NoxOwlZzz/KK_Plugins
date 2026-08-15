using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorTopBarView
    {
        private const float ContextSlotLeft = -224f;
        private const float ContextSlotRight = -42f;
        private const float HeaderContentRight = -42f;
        private const float ShaderContextLeft = 118f;
        private const float PersistSearchWidth = 88f;

        private readonly MaterialEditorPopupMenu _globalMenu;
        private readonly Action _beforeGlobalMenuOpen;
        private readonly Text _shaderContextText;
        private readonly Tooltip _shaderContextTooltip;
        private readonly Image _headerContextSlot;

        internal Image HeaderPanel { get; private set; }
        internal Image ModePanel { get; private set; }
        internal Text HeaderTitle { get; private set; }
        internal InputField FilterInputField { get; private set; }
        internal Toggle PersistSearchToggle { get; private set; }
        internal Button CategoryNavigatorButton =>
            _globalMenu.CategoryNavigatorButton;
        internal Button CollapseAllCategoriesButton =>
            _globalMenu.CollapseAllCategoriesButton;
        internal Button CollapseAllSectionsButton =>
            _globalMenu.CollapseAllSectionsButton;
        internal Button ViewListButton => _globalMenu.ViewListButton;
        internal Transform HeaderContextSlot => _headerContextSlot.transform;
        internal bool IsGlobalMenuOpen => _globalMenu.IsOpen;

        internal MaterialEditorTopBarView(
            Transform mainPanel,
            Transform popupParent,
            string filter,
            Action<string> filterChanged,
            Action close,
            Action toggleSidePanels,
            Action toggleAllCategories,
            Action toggleAllSections,
            Action toggleCategoryNavigator,
            Action beforeGlobalMenuOpen)
        {
            _beforeGlobalMenuOpen = beforeGlobalMenuOpen;
            HeaderPanel = MaterialEditorControlFactory.CreatePanel(
                "Draggable",
                mainPanel,
                MaterialEditorPanelRole.Header);
            HeaderPanel.transform.SetRect(
                0f,
                1f,
                1f,
                1f,
                0f,
                -MaterialEditorLayout.HeaderHeight);

            HeaderTitle = MaterialEditorControlFactory.CreateText(
                "Nametext",
                HeaderPanel.transform,
                "Material Editor",
                MaterialEditorTextRole.Title);
            HeaderTitle.alignment = TextAnchor.MiddleLeft;
            HeaderTitle.transform.SetRect(
                0f,
                0f,
                0f,
                1f,
                MaterialEditorTheme.Spacing.Horizontal * 2f,
                0f,
                114f,
                0f);

            _shaderContextText = MaterialEditorControlFactory.CreateText(
                "MaterialEditorShaderContext",
                HeaderPanel.transform,
                string.Empty,
                MaterialEditorTextRole.Label);
            _shaderContextText.alignment = TextAnchor.MiddleLeft;
            SetShaderContextRect(false);
            _shaderContextTooltip = TooltipManager.AddTooltip(
                _shaderContextText.gameObject,
                string.Empty);
            _shaderContextText.gameObject.SetActive(false);

            _headerContextSlot = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorStudioContextSlot",
                HeaderPanel.transform);
            _headerContextSlot.transform.SetRect(
                1f,
                0f,
                1f,
                1f,
                ContextSlotLeft,
                1f,
                ContextSlotRight,
                -1f);
            _headerContextSlot.color =
                MaterialEditorTheme.Colors.TransparentRow;
            _headerContextSlot.raycastTarget = false;
            _headerContextSlot.gameObject.SetActive(false);

            var globalMenuButton = MaterialEditorControlFactory.CreateButton(
                "MaterialEditorGlobalMenuButton",
                HeaderPanel.transform,
                "...");
            globalMenuButton.transform.SetRect(
                1f,
                0f,
                1f,
                1f,
                -40f,
                1f,
                -21f,
                -1f);
            TooltipManager.AddTooltip(
                globalMenuButton.gameObject,
                "Global view options");

            var closeButton = MaterialEditorControlFactory.CreateButton(
                "CloseButton",
                HeaderPanel.transform,
                string.Empty);
            closeButton.transform.SetRect(
                1f,
                0f,
                1f,
                1f,
                -20f,
                1f,
                -1f,
                -1f);
            CreateCloseGlyph(closeButton.transform);

            _globalMenu = popupParent.gameObject
                .AddComponent<MaterialEditorPopupMenu>();
            _globalMenu.Initialize(
                popupParent,
                globalMenuButton.transform as RectTransform,
                toggleAllCategories,
                toggleAllSections,
                toggleCategoryNavigator,
                toggleSidePanels);
            globalMenuButton.onClick.AddListener(ToggleGlobalMenu);
            closeButton.onClick.AddListener(
                () =>
                {
                    _globalMenu.Close();
                    close();
                });

            BuildModeRow(mainPanel, filter, filterChanged);

            MaterialEditorStyles.ApplyTypography(HeaderPanel.gameObject);
            MaterialEditorStyles.ApplyTypography(ModePanel.gameObject);
            SetPresentation(null);
        }

        internal void PrepareForDisplay(string filter)
        {
            FilterInputField.Set(filter);
        }

        internal void CloseGlobalMenu()
        {
            _globalMenu.Close();
        }

        private void ToggleGlobalMenu()
        {
            _beforeGlobalMenuOpen?.Invoke();
            _globalMenu.Toggle();
        }

        internal void SetPresentation(MaterialEditorPresentation presentation)
        {
            var shaderContext = new MaterialEditorShaderContextAccumulator();
            if (presentation != null)
                foreach (var section in presentation.MaterialSections)
                    shaderContext.Add(section.ShaderName);

            string shaderName;
            var contextKind = shaderContext.Resolve(out shaderName);
            SetShaderContext(contextKind, shaderName);

            var hasCategories = HasCategories(presentation);
            _globalMenu.SetCollapseState(
                hasCategories,
                hasCategories && presentation.AllCategoriesCollapsed);
            RefreshSectionCollapseState(presentation);
        }

        internal void RefreshSectionCollapseState(
            MaterialEditorPresentation presentation)
        {
            var canToggleSections = presentation != null
                                    && presentation.CanToggleSections;
            _globalMenu.SetSectionCollapseState(
                canToggleSections,
                canToggleSections && presentation.AllSectionsCollapsed);
        }

        internal void ReleasePresentation()
        {
            _globalMenu.Close();
            SetShaderContext(
                MaterialEditorShaderContextKind.None,
                string.Empty);
            _globalMenu.SetCollapseState(false, false);
            _globalMenu.SetSectionCollapseState(false, false);
        }

        internal void SetCategoryNavigatorExpanded(bool expanded)
        {
            _globalMenu.SetCategoryNavigatorExpanded(expanded);
        }

        internal void SetCategoryNavigatorConstrained(bool constrained)
        {
            _globalMenu.SetCategoryNavigatorConstrained(constrained);
        }

        internal void SetHeaderContextControlVisible(bool visible)
        {
            _headerContextSlot.gameObject.SetActive(visible);
            SetShaderContextRect(visible);
        }

        internal void SetHeaderTitleHorizontalOffset(float offset)
        {
            var rect = HeaderTitle.rectTransform;
            rect.anchoredPosition = new Vector2(
                offset,
                rect.anchoredPosition.y);
        }

        private void BuildModeRow(
            Transform mainPanel,
            string filter,
            Action<string> filterChanged)
        {
            ModePanel = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorModePanel",
                mainPanel,
                MaterialEditorPanelRole.Header);
            ModePanel.transform.SetRect(
                0f,
                1f,
                1f,
                1f,
                0f,
                -MaterialEditorTheme.Metrics.TopBarHeight,
                0f,
                -MaterialEditorLayout.HeaderHeight);

            FilterInputField = MaterialEditorControlFactory.CreateInputField(
                "Filter",
                ModePanel.transform,
                "Search");
            FilterInputField.text = filter;
            FilterInputField.transform.SetRect(
                0f,
                0f,
                1f,
                1f,
                MaterialEditorTheme.Spacing.TopBarHorizontalInset,
                1f,
                -PersistSearchWidth
                - MaterialEditorTheme.Spacing.TopBarHorizontalInset
                - MaterialEditorTheme.Spacing.Control,
                -1f);
            FilterInputField.onValueChanged.AddListener(
                value => filterChanged(value));
            TooltipManager.AddTooltip(
                FilterInputField.gameObject,
                @"Filter visible items in the window.

- Searches for renderers, materials and projectors
- Searches starting with '_' will search for material properties
- Combine multiple statements using a comma (an entry just has to match any of the search terms)
- Use a '*' as a wildcard for any amount of characters (e.g. ""_pattern*1"" will find the ""PatternMask1"" property)
- Use a '?' as a wildcard for a single character");

            PersistSearchToggle = MaterialEditorControlFactory.CreateToggle(
                "PersistSearch",
                ModePanel.transform,
                "Persist");
            PersistSearchToggle.transform.SetRect(
                1f,
                0f,
                1f,
                1f,
                -PersistSearchWidth
                - MaterialEditorTheme.Spacing.TopBarHorizontalInset,
                1f,
                -MaterialEditorTheme.Spacing.TopBarHorizontalInset,
                -1f);
            PersistSearchToggle.Set(PersistFilter.Value);
            var toggleBackground = PersistSearchToggle.targetGraphic;
            if (toggleBackground != null)
            {
                toggleBackground.rectTransform.SetRect(
                    0f,
                    0f,
                    0f,
                    1f,
                    0f,
                    0f,
                    MaterialEditorTheme.Metrics.SelectionToggleSize,
                    0f);
            }
            var persistSearchText = PersistSearchToggle
                .GetComponentInChildren<Text>(true);
            if (persistSearchText != null)
            {
                persistSearchText.alignment = TextAnchor.MiddleLeft;
                persistSearchText.rectTransform.SetRect(
                    0f,
                    0f,
                    1f,
                    1f,
                    MaterialEditorTheme.Metrics.SelectionToggleSize
                    + MaterialEditorTheme.Spacing.Control,
                    0f,
                    0f,
                    0f);
            }
            PersistSearchToggle.onValueChanged.AddListener(
                value => PersistFilter.Value = value);
            TooltipManager.AddTooltip(
                PersistSearchToggle.gameObject,
                "Keeps the filter between instances of this window instead of resetting them");

        }

        private void SetShaderContext(
            MaterialEditorShaderContextKind kind,
            string shaderName)
        {
            string text;
            switch (kind)
            {
                case MaterialEditorShaderContextKind.Single:
                    text = "Shader: " + shaderName;
                    break;
                case MaterialEditorShaderContextKind.Multiple:
                    text = "Multiple shaders";
                    break;
                default:
                    text = string.Empty;
                    break;
            }

            _shaderContextText.text = text;
            _shaderContextTooltip.SetStandardTooltipText(text);
            _shaderContextText.gameObject.SetActive(
                kind != MaterialEditorShaderContextKind.None);
        }

        private void SetShaderContextRect(bool contextControlVisible)
        {
            _shaderContextText.transform.SetRect(
                0f,
                0f,
                1f,
                1f,
                ShaderContextLeft,
                0f,
                contextControlVisible
                    ? ContextSlotLeft - MaterialEditorTheme.Spacing.Horizontal
                    : HeaderContentRight,
                0f);
        }

        private static bool HasCategories(
            MaterialEditorPresentation presentation)
        {
            if (presentation == null)
                return false;
            if (presentation.HasPropertyFilter)
                return false;
            foreach (var section in presentation.MaterialSections)
                foreach (var category in section.Categories)
                    if (category.CanCollapse)
                        return true;
            return false;
        }

        private static void SetNavigationNone(Selectable selectable)
        {
            selectable.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
        }

        private static void CreateCloseGlyph(Transform parent)
        {
            var firstLine = MaterialEditorControlFactory.CreatePanel(
                "x1",
                parent);
            firstLine.transform.SetRect(
                0f, 0f, 1f, 1f,
                MaterialEditorTheme.Metrics.CloseGlyphInset,
                0f,
                -MaterialEditorTheme.Metrics.CloseGlyphInset);
            firstLine.rectTransform.eulerAngles = new Vector3(
                0f,
                0f,
                MaterialEditorTheme.Metrics.CloseGlyphAngle);
            firstLine.color = MaterialEditorTheme.Colors.PrimaryText;

            var secondLine = MaterialEditorControlFactory.CreatePanel(
                "x2",
                parent);
            secondLine.transform.SetRect(
                0f, 0f, 1f, 1f,
                MaterialEditorTheme.Metrics.CloseGlyphInset,
                0f,
                -MaterialEditorTheme.Metrics.CloseGlyphInset);
            secondLine.rectTransform.eulerAngles = new Vector3(
                0f,
                0f,
                -MaterialEditorTheme.Metrics.CloseGlyphAngle);
            secondLine.color = MaterialEditorTheme.Colors.PrimaryText;
        }
    }
}
