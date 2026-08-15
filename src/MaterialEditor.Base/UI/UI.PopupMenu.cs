using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorPopupMenu : MonoBehaviour
    {
        private readonly Vector3[] _triggerCorners = new Vector3[4];
        private Image _root;
        private Image _menuPanel;
        private RectTransform _trigger;
        private bool _open;
        private bool _categoryNavigatorExpanded;
        private Tooltip _categoryNavigatorTooltip;

        internal Button CollapseAllCategoriesButton { get; private set; }
        internal Button CollapseAllSectionsButton { get; private set; }
        internal Button CategoryNavigatorButton { get; private set; }
        internal Button ViewListButton { get; private set; }
        internal bool IsOpen => _open;

        internal void Initialize(
            Transform popupParent,
            RectTransform trigger,
            Action toggleAllCategories,
            Action toggleAllSections,
            Action toggleCategoryNavigator,
            Action toggleSidePanels)
        {
            _trigger = trigger;

            _root = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorGlobalMenu",
                popupParent);
            _root.transform.SetRect();
            _root.color = MaterialEditorTheme.Colors.TransparentRow;
            _root.raycastTarget = false;

            var dismissLayer = MaterialEditorControlFactory.CreateButton(
                "MaterialEditorGlobalMenuDismissLayer",
                _root.transform,
                string.Empty);
            dismissLayer.transform.SetRect();
            dismissLayer.image.color = MaterialEditorTheme.Colors.TransparentRow;
            dismissLayer.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            dismissLayer.onClick.AddListener(Close);

            _menuPanel = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorGlobalMenuPanel",
                _root.transform,
                MaterialEditorPanelRole.Header);
            _menuPanel.rectTransform.anchorMin = Vector2.zero;
            _menuPanel.rectTransform.anchorMax = Vector2.zero;
            _menuPanel.rectTransform.pivot = new Vector2(1f, 1f);
            _menuPanel.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                280f);
            _menuPanel.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                MaterialEditorLayout.HeaderHeight * 4f
                + MaterialEditorTheme.Spacing.Control * 2f);

            CollapseAllCategoriesButton = CreateMenuButton(
                "CollapseAllCategoriesButton",
                0,
                "Collapse all categories",
                toggleAllCategories);
            CollapseAllSectionsButton = CreateMenuButton(
                "CollapseAllSectionsButton",
                1,
                "Collapse all renderer/material sections",
                toggleAllSections);
            CategoryNavigatorButton = CreateMenuButton(
                "CategoryNavigatorButton",
                2,
                "Show categories",
                toggleCategoryNavigator);
            _categoryNavigatorTooltip = TooltipManager.AddTooltip(
                CategoryNavigatorButton.gameObject,
                "Show or hide the categories panel");
            ViewListButton = CreateMenuButton(
                "ViewListButton",
                3,
                "Show/hide Renderers and Materials",
                toggleSidePanels);

            MaterialEditorStyles.ApplyTypography(_menuPanel.gameObject);
            _root.gameObject.SetActive(false);
            enabled = false;
        }

        internal void Toggle()
        {
            if (_open)
                Close();
            else
                Open();
        }

        internal void Open()
        {
            if (_root == null || _trigger == null)
                return;

            _trigger.GetWorldCorners(_triggerCorners);
            _menuPanel.rectTransform.position = _triggerCorners[3];
            _root.transform.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            _open = true;
            enabled = true;
        }

        internal void Close()
        {
            _open = false;
            if (_root != null)
                _root.gameObject.SetActive(false);
            enabled = false;
        }

        internal void SetCollapseState(bool hasCategories, bool allCollapsed)
        {
            CollapseAllCategoriesButton.interactable = hasCategories;
            SetButtonText(
                CollapseAllCategoriesButton,
                allCollapsed
                    ? "Expand all categories"
                    : "Collapse all categories");
        }

        internal void SetCategoryNavigatorExpanded(bool expanded)
        {
            _categoryNavigatorExpanded = expanded;
            ApplyCategoryNavigatorState();
        }

        internal void SetCategoryNavigatorConstrained(bool constrained)
        {
            // Kept as a compatibility bridge for TopBarView. Responsive layout
            // no longer overrides the user's navigator state.
            ApplyCategoryNavigatorState();
        }

        internal void SetSectionCollapseState(
            bool hasSections,
            bool allCollapsed)
        {
            CollapseAllSectionsButton.interactable = hasSections;
            SetButtonText(
                CollapseAllSectionsButton,
                allCollapsed
                    ? "Expand all renderer/material sections"
                    : "Collapse all renderer/material sections");
        }

        private void ApplyCategoryNavigatorState()
        {
            if (CategoryNavigatorButton == null)
                return;

            CategoryNavigatorButton.interactable = true;
            SetButtonText(
                CategoryNavigatorButton,
                _categoryNavigatorExpanded
                    ? "Hide categories"
                    : "Show categories");
            if (_categoryNavigatorTooltip != null)
                _categoryNavigatorTooltip.SetStandardTooltipText(
                    "Show or hide the categories panel");
        }

        private Button CreateMenuButton(
            string name,
            int row,
            string text,
            Action action)
        {
            var button = MaterialEditorControlFactory.CreateButton(
                name,
                _menuPanel.transform,
                text);
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            var top = -MaterialEditorTheme.Spacing.Control
                      - row * MaterialEditorLayout.HeaderHeight;
            button.transform.SetRect(
                0f,
                1f,
                1f,
                1f,
                MaterialEditorTheme.Spacing.Control,
                top - MaterialEditorLayout.HeaderHeight,
                -MaterialEditorTheme.Spacing.Control,
                top);
            button.onClick.AddListener(
                () =>
                {
                    Close();
                    action();
                });
            return button;
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button != null)
                button.GetComponentInChildren<Text>().text = text;
        }

        private void Update()
        {
            if (_open && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        private void OnDisable()
        {
            Close();
        }
    }
}
