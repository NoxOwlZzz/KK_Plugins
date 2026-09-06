using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    // Owns only the three global presentation actions exposed from the title
    // bar. Side-panel visibility remains on its independent arrow controls.
    internal sealed class MaterialEditorPopupMenu : MonoBehaviour
    {
        private const float MenuWidth = 252f;
        private const int ActionCount = 3;

        private readonly Vector3[] _triggerCorners = new Vector3[4];
        private Image _root;
        private Image _menuPanel;
        private RectTransform _trigger;
        private bool _open;
        private Tooltip _categoriesTooltip;
        private Tooltip _sectionsTooltip;
        private Tooltip _themeTooltip;

        internal Button CollapseAllCategoriesButton { get; private set; }
        internal Button CollapseAllSectionsButton { get; private set; }
        internal Button ThemeButton { get; private set; }
        internal bool IsOpen => _open;

        internal void Initialize(
            Transform popupParent,
            RectTransform trigger,
            Action toggleAllCategories,
            Action toggleAllSections,
            Action toggleTheme)
        {
            _trigger = trigger;

            _root = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorGlobalMenu",
                popupParent,
                MaterialEditorPanelRole.TransparentRow);
            _root.transform.SetRect();
            _root.raycastTarget = false;

            // This is an input catcher, not a visual button. Giving it the
            // standard Button style as well as TransparentRow lets theme
            // reapplication make the full-canvas surface opaque.
            var dismissSurface = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorGlobalMenuDismissLayer",
                _root.transform,
                MaterialEditorPanelRole.TransparentRow);
            dismissSurface.transform.SetRect();
            dismissSurface.raycastTarget = true;
            var dismissLayer = dismissSurface.gameObject.AddComponent<Button>();
            dismissLayer.targetGraphic = dismissSurface;
            dismissLayer.transition = Selectable.Transition.None;
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
                MenuWidth);
            _menuPanel.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                MaterialEditorLayout.HeaderHeight * ActionCount
                + MaterialEditorTheme.Spacing.Control * 2f);

            CollapseAllCategoriesButton = CreateMenuButton(
                "CollapseAllCategoriesButton",
                0,
                "Collapse all categories",
                toggleAllCategories);
            _categoriesTooltip = TooltipManager.AddTooltip(
                CollapseAllCategoriesButton.gameObject,
                "Collapse all categories");

            CollapseAllSectionsButton = CreateMenuButton(
                "CollapseAllSectionsButton",
                1,
                "Collapse all renderer/material sections",
                toggleAllSections);
            _sectionsTooltip = TooltipManager.AddTooltip(
                CollapseAllSectionsButton.gameObject,
                "Collapse all renderer/material sections");

            ThemeButton = CreateMenuButton(
                "MaterialEditorThemeButton",
                2,
                string.Empty,
                toggleTheme);
            _themeTooltip = TooltipManager.AddTooltip(
                ThemeButton.gameObject,
                string.Empty);
            RefreshThemeState();

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

        internal void SetCollapseState(bool available, bool allCollapsed)
        {
            SetActionState(
                CollapseAllCategoriesButton,
                _categoriesTooltip,
                available,
                allCollapsed
                    ? "Expand all categories"
                    : "Collapse all categories",
                "No collapsible categories");
        }

        internal void SetSectionCollapseState(bool available, bool allCollapsed)
        {
            SetActionState(
                CollapseAllSectionsButton,
                _sectionsTooltip,
                available,
                allCollapsed
                    ? "Expand all renderer/material sections"
                    : "Collapse all renderer/material sections",
                "No collapsible renderer/material sections");
        }

        internal void RefreshThemeState()
        {
            var switchToDark = MaterialEditorTheme.Mode
                               == MaterialEditorThemeMode.Legacy;
            var text = switchToDark
                ? "Switch to Dark theme"
                : "Switch to Light theme";
            SetActionState(
                ThemeButton,
                _themeTooltip,
                true,
                text,
                text);
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

        private static void SetActionState(
            Button button,
            Tooltip tooltip,
            bool available,
            string activeText,
            string unavailableText)
        {
            if (button == null)
                return;

            var text = available ? activeText : unavailableText;
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = text;
                label.SetVerticesDirty();
            }
            MaterialEditorStyles.SetControlAvailability(button, available);
            tooltip?.SetStandardTooltipText(text);
        }
    }
}
