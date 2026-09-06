using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorTopBarView
    {
        private const float ContextSlotLeft = -244f;
        private const float ContextSlotRight = -62f;
        private const float HeaderContentRight = -62f;
        private const float PersistSearchWidth = 88f;

        private readonly MaterialEditorPopupMenu _globalMenu;
        private readonly Image _headerContextSlot;
        private bool _headerContextControlVisible;

        internal Image HeaderPanel { get; private set; }
        internal Image ModePanel { get; private set; }
        internal Text HeaderTitle { get; private set; }
        internal InputField FilterInputField { get; private set; }
        internal Toggle PersistSearchToggle { get; private set; }
        internal Button CategoryNavigatorButton { get; private set; }
        internal Button ViewListButton { get; private set; }
        internal Button CollapseAllCategoriesButton =>
            _globalMenu.CollapseAllCategoriesButton;
        internal Button CollapseAllSectionsButton =>
            _globalMenu.CollapseAllSectionsButton;
        internal Button ThemeButton => _globalMenu.ThemeButton;
        internal Transform HeaderContextSlot => _headerContextSlot.transform;

        internal MaterialEditorTopBarView(
            Transform mainPanel,
            string filter,
            Action<string> filterChanged,
            Action close,
            Action toggleCategoriesPanel,
            Action toggleSelectionPanels,
            Action toggleAllCategories,
            Action toggleAllSections)
        {
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

            _headerContextSlot = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorStudioContextSlot",
                HeaderPanel.transform,
                MaterialEditorPanelRole.TransparentRow);
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
                -60f,
                1f,
                -41f,
                -1f);
            SetNavigationNone(globalMenuButton);
            TooltipManager.AddTooltip(
                globalMenuButton.gameObject,
                "Global view and theme options");

            var popupParent = mainPanel.GetComponentInParent<Canvas>().transform;
            _globalMenu = popupParent.gameObject
                .AddComponent<MaterialEditorPopupMenu>();
            _globalMenu.Initialize(
                popupParent,
                globalMenuButton.transform as RectTransform,
                toggleAllCategories,
                toggleAllSections,
                ToggleUITheme);
            globalMenuButton.onClick.AddListener(_globalMenu.Toggle);

            CategoryNavigatorButton =
                MaterialEditorControlFactory.CreateButton(
                    "CategoryNavigatorButton",
                    HeaderPanel.transform,
                    MaterialEditorTheme.Glyphs.ChevronLeft);
            CategoryNavigatorButton.transform.SetRect(
                0f, 0f, 0f, 1f,
                1f, 1f,
                MaterialEditorLayout.SmallButtonWidth, -1f);
            SetNavigationNone(CategoryNavigatorButton);
            TooltipManager.AddTooltip(
                CategoryNavigatorButton.gameObject,
                "Show or hide Categories");
            CategoryNavigatorButton.onClick.AddListener(
                () =>
                {
                    _globalMenu.Close();
                    toggleCategoriesPanel();
                });

            ViewListButton = MaterialEditorControlFactory.CreateButton(
                "ViewListButton",
                HeaderPanel.transform,
                MaterialEditorTheme.Glyphs.ChevronRight);
            ViewListButton.transform.SetRect(
                1f, 0f, 1f, 1f,
                -MaterialEditorLayout.SmallButtonWidth, 1f,
                -1f, -1f);
            SetNavigationNone(ViewListButton);
            TooltipManager.AddTooltip(
                ViewListButton.gameObject,
                "Show or hide Renderers and Materials");
            ViewListButton.onClick.AddListener(
                () =>
                {
                    _globalMenu.Close();
                    toggleSelectionPanels();
                });

            var closeButton = MaterialEditorControlFactory.CreateButton(
                "CloseButton",
                HeaderPanel.transform,
                string.Empty);
            closeButton.transform.SetRect(
                1f,
                0f,
                1f,
                1f,
                -40f,
                1f,
                -21f,
                -1f);
            CreateCloseGlyph(closeButton.transform);

            closeButton.onClick.AddListener(
                () =>
                {
                    _globalMenu.Close();
                    close();
                });
            SetHeaderTitleRect();

            BuildModeRow(
                mainPanel,
                filter,
                filterChanged);

            MaterialEditorStyles.ApplyTypography(HeaderPanel.gameObject);
            MaterialEditorStyles.ApplyTypography(ModePanel.gameObject);
            SetPresentation(null);
        }

        internal void PrepareForDisplay(string filter)
        {
            FilterInputField.Set(filter);
        }


        internal void SetPresentation(MaterialEditorPresentation presentation)
        {
            var hasNavigatorCategories = HasNavigatorCategories(presentation);
            MaterialEditorStyles.SetControlAvailability(
                CategoryNavigatorButton, hasNavigatorCategories);
            var canCollapseCategories =
                CanCollapseCategories(presentation);
            _globalMenu.SetCollapseState(
                canCollapseCategories,
                canCollapseCategories && presentation.AllCategoriesCollapsed);
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
            MaterialEditorStyles.SetControlAvailability(
                CategoryNavigatorButton, false);
            _globalMenu.SetCollapseState(false, false);
            _globalMenu.SetSectionCollapseState(false, false);
        }
        internal void RefreshThemeButton()
        {
            _globalMenu.RefreshThemeState();
        }
        internal void SetHeaderContextControlVisible(bool visible)
        {
            _headerContextControlVisible = visible;
            _headerContextSlot.gameObject.SetActive(visible);
            SetHeaderTitleRect();
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

            var filterRight = -PersistSearchWidth
                              - MaterialEditorTheme.Spacing.TopBarHorizontalInset
                              - MaterialEditorTheme.Spacing.Control;

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
                filterRight,
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
                MaterialEditorStyles.ApplyText(
                    persistSearchText,
                    MaterialEditorTextRole.Chrome);
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

        private void SetHeaderTitleRect()
        {
            var actionRight = _headerContextControlVisible
                ? ContextSlotLeft - MaterialEditorTheme.Spacing.Control
                : HeaderContentRight;
            HeaderTitle.transform.SetRect(
                0f,
                0f,
                1f,
                1f,
                MaterialEditorLayout.SmallButtonWidth
                + MaterialEditorTheme.Spacing.Control,
                0f,
                actionRight - MaterialEditorTheme.Spacing.Horizontal,
                0f);
        }
        private static bool HasNavigatorCategories(
            MaterialEditorPresentation presentation)
        {
            if (presentation == null)
                return false;
            foreach (var section in presentation.MaterialSections)
                foreach (var category in section.Categories)
                    return true;
            return false;
        }

        private static bool CanCollapseCategories(
            MaterialEditorPresentation presentation)
        {
            if (presentation == null || presentation.HasPropertyFilter)
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
            MaterialEditorStyles.ApplyGraphicColor(
                firstLine,
                MaterialEditorThemeColorRole.PrimaryText);

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
            MaterialEditorStyles.ApplyGraphicColor(
                secondLine,
                MaterialEditorThemeColorRole.PrimaryText);
        }
    }
}
