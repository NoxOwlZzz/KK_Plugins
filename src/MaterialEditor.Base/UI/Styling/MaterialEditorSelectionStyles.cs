using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorSelectionStyles
    {
        internal static void ApplyButton(Button button)
        {
            if (button == null)
                return;

            MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.Button);

            var legacyReset = MaterialEditorTheme.Mode
                              == MaterialEditorThemeMode.Legacy
                              && IsResetButton(button);
            var legacyColors = ColorBlock.defaultColorBlock;
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                button,
                legacyReset
                    ? legacyColors.normalColor
                    : MaterialEditorTheme.Colors.ControlNormal,
                legacyReset
                    ? legacyColors.highlightedColor
                    : MaterialEditorTheme.Colors.ControlHover,
                legacyReset
                    ? legacyColors.pressedColor
                    : MaterialEditorTheme.Colors.ControlPressed,
                legacyReset
                    ? legacyColors.disabledColor
                    : MaterialEditorTheme.Colors.ControlDisabled);
            foreach (var text in button.GetComponentsInChildren<Text>(true))
            {
                MaterialEditorPanelTextStyles.ApplyText(
                    text,
                    MaterialEditorPanelTextStyles.HasAssignedTextRole(text)
                        ? MaterialEditorPanelTextStyles.GetAssignedTextRole(text)
                        : MaterialEditorTextRole.Button);
            }
        }

        private static bool IsResetButton(Button button)
        {
            return button != null
                   && button.name != null
                   && button.name.EndsWith("ResetButton");
        }

        internal static void ApplyPropertyCategoryButton(Button button)
        {
            if (button == null)
                return;

            MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.PropertyCategory);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                button,
                MaterialEditorTheme.Colors.CategoryRow,
                MaterialEditorTheme.Colors.CategoryHeaderHover,
                MaterialEditorTheme.Colors.CategoryHeaderExpanded,
                MaterialEditorTheme.Colors.ControlDisabled);
            ApplyPropertyCategoryTypography(button);
        }

        internal static void SetPropertyCategoryExpanded(
            Button button,
            bool expanded)
        {
            if (button == null)
                return;

            var state = MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.PropertyCategory);
            state.SetLogicalState(expanded);
            var colors = button.colors;
            colors.normalColor = expanded
                ? MaterialEditorTheme.Colors.CategoryHeaderExpanded
                : MaterialEditorTheme.Colors.CategoryRow;
            button.colors = colors;
            ApplyPropertyCategoryTypography(button);
            MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(button);
        }

        internal static void ApplyPropertySubcategoryButton(Button button)
        {
            if (button == null)
                return;

            MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.PropertySubcategory);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                button,
                MaterialEditorTheme.Colors.SubcategoryRow,
                MaterialEditorTheme.Colors.SubcategoryHeaderHover,
                MaterialEditorTheme.Colors.SubcategoryHeaderExpanded,
                MaterialEditorTheme.Colors.ControlDisabled);
        }

        internal static void SetPropertySubcategoryExpanded(
            Button button,
            bool expanded)
        {
            if (button == null)
                return;

            var state = MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.PropertySubcategory);
            state.SetLogicalState(expanded);
            var colors = button.colors;
            colors.normalColor = expanded
                ? MaterialEditorTheme.Colors.SubcategoryHeaderExpanded
                : MaterialEditorTheme.Colors.SubcategoryRow;
            button.colors = colors;
            MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(button);
        }

        internal static void ApplyCategoryNavigationButton(Button button)
        {
            if (button == null)
                return;

            var state = MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.CategoryNavigation);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                button,
                MaterialEditorTheme.Colors.TransparentRow,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            foreach (var text in button.GetComponentsInChildren<Text>(true))
                MaterialEditorPanelTextStyles.ApplyText(text, MaterialEditorTextRole.Label);
            SetCategoryNavigationSelected(
                button,
                state != null && state.LogicalState);
        }

        internal static void ApplySelectionListRowButton(Button button)
        {
            if (button == null)
                return;

            MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.SelectionListRow);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                button,
                MaterialEditorTheme.Colors.TransparentRow,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
        }

        internal static void SetCategoryNavigationSelected(
            Button button,
            bool selected)
        {
            if (button == null)
                return;

            var state = MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.CategoryNavigation);
            state.SetLogicalState(selected);
            var colors = button.colors;
            colors.normalColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.TransparentRow;
            colors.highlightedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.ControlHover;
            colors.pressedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.ControlPressed;
            button.colors = colors;
            MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(button);
            ApplySelectionText(button, selected);
        }

        internal static void SetSelectionListSelected(
            Button button,
            bool selected)
        {
            if (button == null)
                return;

            var state = MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.SelectionListRow);
            state.SetLogicalState(selected);
            var colors = button.colors;
            colors.normalColor = selected
                ? MaterialEditorTheme.States.SelectedSurface
                : MaterialEditorTheme.Colors.TransparentRow;
            colors.highlightedColor = selected
                ? MaterialEditorTheme.States.SelectedSurface
                : MaterialEditorTheme.Colors.ControlHover;
            colors.pressedColor = selected
                ? MaterialEditorTheme.States.SelectedSurface
                : MaterialEditorTheme.Colors.ControlPressed;
            button.colors = colors;
            MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(button);
            ApplySelectionText(button, selected);
        }

        internal static void ApplySwatchButton(Button button)
        {
            if (button == null)
                return;

            MaterialEditorControlStyleState.Assign(
                button,
                MaterialEditorControlStyleRole.Swatch);
            // The binder owns this image color. Disabling Selectable tinting keeps
            // hover/press transitions from replacing the bound material color.
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic != null)
                button.targetGraphic.color = MaterialEditorTheme.Colors.ControlNormal;
            foreach (var text in button.GetComponentsInChildren<Text>(true))
            {
                MaterialEditorPanelTextStyles.ApplyText(
                    text,
                    MaterialEditorPanelTextStyles.HasAssignedTextRole(text)
                        ? MaterialEditorPanelTextStyles.GetAssignedTextRole(text)
                        : MaterialEditorTextRole.Button);
            }
        }

        internal static void ApplyRow(GameObject row)
        {
            if (row == null)
                return;

            foreach (var layout in row.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = true;

                var panelRect = layout.GetComponent<RectTransform>();
                if (panelRect == null)
                    continue;

                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                panelRect.localScale = Vector3.one;

                var inset = layout.GetComponent<RowPanelInset>();
                if (inset != null)
                    inset.Apply();
            }

            MaterialEditorPanelTextStyles.ApplyTypography(row);
        }

        internal static void ReapplyTheme(
            MaterialEditorControlStyleState state)
        {
            if (state == null)
                return;

            var selectable = state.GetComponent<Selectable>();
            switch (state.Role)
            {
                case MaterialEditorControlStyleRole.Button:
                    var button = selectable as Button;
                    ApplyButton(button);
                    break;
                case MaterialEditorControlStyleRole.PropertyCategory:
                    ApplyPropertyCategoryButton(selectable as Button);
                    SetPropertyCategoryExpanded(
                        selectable as Button,
                        state.LogicalState);
                    break;
                case MaterialEditorControlStyleRole.PropertySubcategory:
                    ApplyPropertySubcategoryButton(selectable as Button);
                    SetPropertySubcategoryExpanded(
                        selectable as Button,
                        state.LogicalState);
                    break;
                case MaterialEditorControlStyleRole.CategoryNavigation:
                    ApplyCategoryNavigationButton(selectable as Button);
                    SetCategoryNavigationSelected(
                        selectable as Button,
                        state.LogicalState);
                    break;
                case MaterialEditorControlStyleRole.SelectionListRow:
                    ApplySelectionListRowButton(selectable as Button);
                    SetSelectionListSelected(
                        selectable as Button,
                        state.LogicalState);
                    break;
                case MaterialEditorControlStyleRole.Swatch:
                    if (selectable != null)
                        selectable.transition = Selectable.Transition.None;
                    break;
            }
        }

        private static void ApplyPropertyCategoryTypography(Button button)
        {
            if (button == null)
                return;

            foreach (var text in button.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name != "PropertyCategoryLabel")
                    continue;
                text.fontStyle = MaterialEditorTheme.Mode
                                 == MaterialEditorThemeMode.Legacy
                    ? FontStyle.Normal
                    : FontStyle.Bold;
                text.SetVerticesDirty();
            }
        }
        private static void ApplySelectionText(Button button, bool selected)
        {
            if (button == null)
                return;

            foreach (var text in button.GetComponentsInChildren<Text>(true))
            {
                if (!button.interactable
                    && MaterialEditorTheme.Mode == MaterialEditorThemeMode.Dark)
                {
                    text.color = MaterialEditorTheme.Colors.DisabledText;
                }
                else if (selected)
                {
                    text.color = MaterialEditorTheme.Colors.SelectedText;
                }
                else if (MaterialEditorTheme.Mode == MaterialEditorThemeMode.Dark)
                {
                    text.color = MaterialEditorTheme.Colors.SecondaryText;
                }
                else
                {
                    text.color = MaterialEditorPanelTextStyles.ResolveTextColor(
                        MaterialEditorPanelTextStyles.GetAssignedTextRole(text));
                }
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
            }
        }
    }
}
