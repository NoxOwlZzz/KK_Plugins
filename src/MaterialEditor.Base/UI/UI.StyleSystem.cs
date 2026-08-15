using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorLayout
    {
        internal const float Margin = MaterialEditorTheme.Metrics.Margin;
        internal const float HeaderHeight = MaterialEditorTheme.Metrics.HeaderHeight;
        internal const float ScrollbarOffset = MaterialEditorTheme.Metrics.ScrollbarOffset;
        internal const float RowHeight = MaterialEditorTheme.Metrics.RowHeight;
        internal const float CategoryNavigatorWidth = MaterialEditorTheme.Metrics.CategoryNavigatorWidth;
        internal const int PropertyLabelInset = MaterialEditorTheme.Spacing.PropertyLabelInset;

        internal const float LabelWidth = MaterialEditorTheme.Metrics.LabelWidth;
        internal const float ButtonWidth = MaterialEditorTheme.Metrics.ButtonWidth;
        internal const float SmallButtonWidth = MaterialEditorTheme.Metrics.SmallButtonWidth;
        internal const float ResetButtonWidth = MaterialEditorTheme.Metrics.ResetButtonWidth;
        internal const float InterpolableButtonWidth = MaterialEditorTheme.Metrics.InterpolableButtonWidth;
        internal const float ContentWidth = MaterialEditorTheme.Metrics.ContentWidth;

        internal const float RendererButtonWidth = MaterialEditorTheme.Metrics.RendererButtonWidth;
        internal const float RendererToggleWidth = MaterialEditorTheme.Metrics.RendererToggleWidth;
        internal const float RendererDropdownWidth = MaterialEditorTheme.Metrics.RendererDropdownWidth;
        internal const float MaterialButtonWidth = MaterialEditorTheme.Metrics.MaterialButtonWidth;
        internal const float MaterialRenameButtonWidth = MaterialEditorTheme.Metrics.MaterialRenameButtonWidth;
        internal const float ShaderLabelMinimumWidth = MaterialEditorTheme.Metrics.ShaderLabelMinimumWidth;
        internal const float ShaderDropdownMinimumWidth = MaterialEditorTheme.Metrics.ShaderDropdownMinimumWidth;
        internal const float ShaderDropdownWidth = MaterialEditorTheme.Metrics.ShaderDropdownWidth;
        internal const float RenderQueueInputWidth = MaterialEditorTheme.Metrics.RenderQueueInputWidth;
        internal const float OffsetScaleLabelXWidth = MaterialEditorTheme.Metrics.OffsetScaleLabelXWidth;
        internal const float OffsetScaleLabelYWidth = MaterialEditorTheme.Metrics.OffsetScaleLabelYWidth;
        internal const float OffsetScaleInputWidth = MaterialEditorTheme.Metrics.OffsetScaleInputWidth;
        internal const float ColorLabelWidth = MaterialEditorTheme.Metrics.ColorLabelWidth;
        internal const float ColorInputWidth = MaterialEditorTheme.Metrics.ColorInputWidth;
        internal const float ColorEditButtonWidth = MaterialEditorTheme.Metrics.ColorEditButtonWidth;
        internal const float FloatSliderWidth = MaterialEditorTheme.Metrics.FloatSliderWidth;
        internal const float FloatInputWidth = MaterialEditorTheme.Metrics.FloatInputWidth;
        internal const float VectorComponentLabelWidth = MaterialEditorTheme.Metrics.VectorComponentLabelWidth;
        internal const float VectorComponentInputWidth = MaterialEditorTheme.Metrics.VectorComponentInputWidth;
        internal const int VectorComponentFontSize = MaterialEditorTheme.Typography.VectorComponentFontSize;
        internal const int VectorComponentMinimumFontSize = MaterialEditorTheme.Typography.VectorComponentMinimumFontSize;
        internal const float KeywordToggleWidth = MaterialEditorTheme.Metrics.KeywordToggleWidth;

        internal const int DropdownFontSize = MaterialEditorTheme.Typography.DropdownFontSize;
        internal const int DropdownMinimumFontSize = MaterialEditorTheme.Typography.DropdownMinimumFontSize;
        internal const float DropdownTextVerticalInset = MaterialEditorTheme.Spacing.DropdownTextVerticalInset;

        internal static readonly RectOffset RowPadding = new RectOffset(
            MaterialEditorTheme.Spacing.RowPaddingLeft,
            MaterialEditorTheme.Spacing.RowPaddingRight,
            MaterialEditorTheme.Spacing.RowPaddingTop,
            MaterialEditorTheme.Spacing.RowPaddingBottom);
    }

    internal enum MaterialEditorTextRole
    {
        PreserveHorizontal,
        Title,
        Label,
        CenteredLabel,
        Button,
        Input,
        Placeholder,
        Tooltip
    }

    internal enum MaterialEditorPanelRole
    {
        Default,
        Window,
        Main,
        CenterPanel,
        Header,
        SidePanel,
        LeftPanel,
        RightPanel,
        Row,
        PropertyRow,
        AlternatePropertyRow,
        RendererRow,
        MaterialRow,
        ShaderRow,
        CategoryRow,
        SubcategoryRow,
        SelectedRow,
        HoverRow,
        DisabledRow,
        ModifiedRow,
        TransparentRow
    }

    internal static class MaterialEditorStyles
    {
        internal static readonly Color WindowColor = MaterialEditorTheme.Colors.Window;
        internal static readonly Color LeftPanelColor = MaterialEditorTheme.Colors.LeftPanel;
        internal static readonly Color CenterPanelColor = MaterialEditorTheme.Colors.CenterPanel;
        internal static readonly Color RightPanelColor = MaterialEditorTheme.Colors.RightPanel;
        internal static readonly Color MainPanelColor = MaterialEditorTheme.Colors.MainPanel;
        internal static readonly Color HeaderColor = MaterialEditorTheme.Colors.Header;
        internal static readonly Color SidePanelColor = MaterialEditorTheme.Colors.SidePanel;
        internal static readonly Color NavigatorShaderHeaderColor = MaterialEditorTheme.Colors.NavigatorShaderHeader;
        internal static readonly Color RowColor = MaterialEditorTheme.Colors.Row;
        internal static readonly Color RendererColor = MaterialEditorTheme.Colors.RendererRow;
        internal static readonly Color MaterialColor = MaterialEditorTheme.Colors.MaterialRow;
        internal static readonly Color ShaderColor = MaterialEditorTheme.Colors.ShaderRow;
        internal static readonly Color CategoryColor = MaterialEditorTheme.Colors.CategoryRow;
        internal static readonly Color SubcategoryColor = MaterialEditorTheme.Colors.SubcategoryRow;
        internal static readonly Color PropertyColor = MaterialEditorTheme.Colors.PropertyRow;
        internal static readonly Color AlternatePropertyColor = MaterialEditorTheme.Colors.PropertyRowAlternate;
        internal static readonly Color TransparentRowColor = MaterialEditorTheme.Colors.TransparentRow;
        internal static readonly Color ChangedRowColor = MaterialEditorTheme.Colors.ChangedRow;
        internal static readonly Color ScrollbarColor = MaterialEditorTheme.Colors.Scrollbar;
        internal static readonly Color ShaderHintUnderlineColor = MaterialEditorTheme.Colors.ShaderHintUnderline;

        private const int ToggleMarkTextureSize = 16;
        private const int ToggleMarkStrokeWidth = 2;
        private static Sprite _toggleMarkSprite;

        internal static void ApplyPanel(Image panel, MaterialEditorPanelRole role)
        {
            if (panel == null)
                return;

            switch (role)
            {
                case MaterialEditorPanelRole.Default:
                case MaterialEditorPanelRole.Window:
                    panel.color = WindowColor;
                    break;
                case MaterialEditorPanelRole.Main:
                case MaterialEditorPanelRole.CenterPanel:
                    panel.color = MainPanelColor;
                    break;
                case MaterialEditorPanelRole.Header:
                    panel.color = HeaderColor;
                    break;
                case MaterialEditorPanelRole.SidePanel:
                case MaterialEditorPanelRole.RightPanel:
                    panel.color = SidePanelColor;
                    break;
                case MaterialEditorPanelRole.LeftPanel:
                    panel.color = LeftPanelColor;
                    break;
                case MaterialEditorPanelRole.Row:
                case MaterialEditorPanelRole.PropertyRow:
                    panel.color = PropertyColor;
                    break;
                case MaterialEditorPanelRole.AlternatePropertyRow:
                    panel.color = AlternatePropertyColor;
                    break;
                case MaterialEditorPanelRole.RendererRow:
                    panel.color = RendererColor;
                    break;
                case MaterialEditorPanelRole.MaterialRow:
                    panel.color = MaterialColor;
                    break;
                case MaterialEditorPanelRole.ShaderRow:
                    panel.color = ShaderColor;
                    break;
                case MaterialEditorPanelRole.CategoryRow:
                    panel.color = CategoryColor;
                    break;
                case MaterialEditorPanelRole.SubcategoryRow:
                    panel.color = SubcategoryColor;
                    break;
                case MaterialEditorPanelRole.SelectedRow:
                    panel.color = MaterialEditorTheme.Colors.Selected;
                    break;
                case MaterialEditorPanelRole.HoverRow:
                    panel.color = MaterialEditorTheme.Colors.Hover;
                    break;
                case MaterialEditorPanelRole.DisabledRow:
                    panel.color = MaterialEditorTheme.Colors.DisabledSurface;
                    break;
                case MaterialEditorPanelRole.ModifiedRow:
                    panel.color = MaterialEditorTheme.Colors.ModifiedSurface;
                    break;
                case MaterialEditorPanelRole.TransparentRow:
                    panel.color = TransparentRowColor;
                    break;
            }
        }

        internal static void ApplyText(Text text, MaterialEditorTextRole role = MaterialEditorTextRole.PreserveHorizontal)
        {
            if (text == null)
                return;

            text.alignment = GetAlignment(text.alignment, role);
            text.color = GetTextColor(role);
            ApplyTextRendering(text, null);
            text.fontSize = Mathf.Min(
                text.fontSize,
                MaterialEditorTheme.Typography.PrimaryFontSize);
            if (text.resizeTextForBestFit)
                text.resizeTextMaxSize = Mathf.Min(
                    text.resizeTextMaxSize,
                    MaterialEditorTheme.Typography.PrimaryFontSize);

            var styleState = text.GetComponent<MaterialEditorTextStyleState>()
                             ?? text.gameObject.AddComponent<MaterialEditorTextStyleState>();
            styleState.SetRole(role);

            var visualCenter = text.GetComponent<RowTextVisualCenter>();
            if (visualCenter == null)
                visualCenter = text.gameObject.AddComponent<RowTextVisualCenter>();
            visualCenter.SetMode(
                role == MaterialEditorTextRole.Tooltip
                    ? TextVisualCenterMode.VisibleBounds
                    : TextVisualCenterMode.TypographicBody);
            visualCenter.enabled = true;

            text.SetVerticesDirty();
        }

        internal static void ApplyTypography(GameObject root)
        {
            if (root == null)
                return;

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var styleState = text.GetComponent<MaterialEditorTextStyleState>();
                ApplyText(
                    text,
                    styleState != null
                        ? styleState.Role
                        : MaterialEditorTextRole.PreserveHorizontal);
            }
        }

        internal static void ApplyButton(Button button)
        {
            if (button == null)
                return;

            ApplySelectable(
                button,
                MaterialEditorTheme.Colors.ControlNormal,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            foreach (var text in button.GetComponentsInChildren<Text>(true))
                ApplyText(text, MaterialEditorTextRole.Button);
        }

        internal static void ApplyPropertyCategoryButton(Button button)
        {
            if (button == null)
                return;

            ApplySelectable(
                button,
                MaterialEditorTheme.Colors.CategoryRow,
                MaterialEditorTheme.Colors.CategoryHeaderHover,
                MaterialEditorTheme.Colors.CategoryHeaderExpanded,
                MaterialEditorTheme.Colors.ControlDisabled);
        }

        internal static void SetPropertyCategoryExpanded(
            Button button,
            bool expanded)
        {
            if (button == null)
                return;

            var colors = button.colors;
            colors.normalColor = expanded
                ? MaterialEditorTheme.Colors.CategoryHeaderExpanded
                : MaterialEditorTheme.Colors.CategoryRow;
            button.colors = colors;
        }

        internal static void ApplyPropertySubcategoryButton(Button button)
        {
            if (button == null)
                return;

            ApplySelectable(
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

            var colors = button.colors;
            colors.normalColor = expanded
                ? MaterialEditorTheme.Colors.SubcategoryHeaderExpanded
                : MaterialEditorTheme.Colors.SubcategoryRow;
            button.colors = colors;
        }

        internal static void ApplyCategoryNavigationButton(Button button)
        {
            if (button == null)
                return;

            ApplySelectable(
                button,
                MaterialEditorTheme.Colors.TransparentRow,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            foreach (var text in button.GetComponentsInChildren<Text>(true))
                ApplyText(text, MaterialEditorTextRole.Label);
        }

        internal static void ApplySelectionListRowButton(Button button)
        {
            if (button == null)
                return;

            ApplySelectable(
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
        }

        internal static void SetSelectionListSelected(
            Button button,
            bool selected)
        {
            if (button == null)
                return;

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
        }

        internal static void ApplySwatchButton(Button button)
        {
            if (button == null)
                return;

            // The binder owns this image color. Disabling Selectable tinting keeps
            // hover/press transitions from replacing the bound material color.
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic != null)
                button.targetGraphic.color = MaterialEditorTheme.Colors.ControlNormal;
            foreach (var text in button.GetComponentsInChildren<Text>(true))
                ApplyText(text, MaterialEditorTextRole.Button);
        }

        internal static void ApplyInputField(InputField inputField)
        {
            if (inputField == null)
                return;

            ApplySelectable(
                inputField,
                MaterialEditorTheme.Colors.InputSurface,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            ApplyControlOutline(
                inputField.targetGraphic,
                MaterialEditorTheme.Colors.InputBorder);
            inputField.lineType = InputField.LineType.SingleLine;
            if (inputField.textComponent != null)
            {
                inputField.textComponent.resizeTextForBestFit = true;
                inputField.textComponent.resizeTextMinSize =
                    MaterialEditorTheme.Typography.InputMinimumFontSize;
                inputField.textComponent.resizeTextMaxSize =
                    MaterialEditorTheme.Typography.PrimaryFontSize;
                inputField.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                inputField.textComponent.verticalOverflow = VerticalWrapMode.Truncate;
                inputField.textComponent.maskable = true;
                inputField.textComponent.raycastTarget = false;
                ApplyText(inputField.textComponent, MaterialEditorTextRole.Input);
            }
            if (inputField.placeholder is Text placeholder)
            {
                placeholder.resizeTextForBestFit = true;
                placeholder.resizeTextMinSize =
                    MaterialEditorTheme.Typography.InputMinimumFontSize;
                placeholder.resizeTextMaxSize =
                    MaterialEditorTheme.Typography.PrimaryFontSize;
                placeholder.maskable = true;
                placeholder.raycastTarget = false;
                ApplyText(placeholder, MaterialEditorTextRole.Placeholder);
            }
        }

        internal static void ApplyToggle(Toggle toggle)
        {
            if (toggle == null)
                return;

            ApplySelectable(
                toggle,
                MaterialEditorTheme.Colors.ControlNormal,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            ApplyToggleMark(toggle);
            foreach (var text in toggle.GetComponentsInChildren<Text>(true))
                ApplyText(text, GetAssignedTextRole(text));
        }

        private static void ApplyToggleMark(Toggle toggle)
        {
            var graphic = toggle.graphic;
            if (graphic == null)
                return;

            var image = graphic as Image;
            if (image != null)
            {
                // The bundled DefaultControls checkmark has dark source pixels.
                // A white tint only multiplies those pixels, so use a white mask
                // whose visible color is owned entirely by the theme.
                image.sprite = GetToggleMarkSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }

            graphic.color = MaterialEditorTheme.Colors.ToggleMark;
            graphic.canvasRenderer.SetColor(MaterialEditorTheme.Colors.TintIdentity);
            graphic.canvasRenderer.SetAlpha(MaterialEditorTheme.States.VisibleAlpha);
            graphic.SetVerticesDirty();
        }

        private static Sprite GetToggleMarkSprite()
        {
            if (_toggleMarkSprite != null)
                return _toggleMarkSprite;

            var pixels = new Color32[ToggleMarkTextureSize * ToggleMarkTextureSize];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0xFF, 0xFF, 0xFF, 0x00);
            DrawToggleMarkStroke(pixels, 2, 8, 6, 4);
            DrawToggleMarkStroke(pixels, 6, 4, 14, 12);

            var texture = new Texture2D(
                ToggleMarkTextureSize,
                ToggleMarkTextureSize,
                TextureFormat.ARGB32,
                false)
            {
                name = "MaterialEditorToggleMarkTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply();

            _toggleMarkSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, ToggleMarkTextureSize, ToggleMarkTextureSize),
                new Vector2(0.5f, 0.5f),
                ToggleMarkTextureSize);
            _toggleMarkSprite.name = "MaterialEditorToggleMark";
            _toggleMarkSprite.hideFlags = HideFlags.HideAndDontSave;
            return _toggleMarkSprite;
        }

        private static void DrawToggleMarkStroke(
            Color32[] pixels,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            var steps = Mathf.Max(
                Mathf.Abs(endX - startX),
                Mathf.Abs(endY - startY));
            for (var step = 0; step <= steps; step++)
            {
                var amount = steps == 0 ? 0f : (float)step / steps;
                var x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, amount));
                var y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, amount));
                for (var offsetY = 0; offsetY < ToggleMarkStrokeWidth; offsetY++)
                {
                    var pixelY = y + offsetY;
                    if (x < 0 || x >= ToggleMarkTextureSize
                        || pixelY < 0 || pixelY >= ToggleMarkTextureSize)
                        continue;
                    pixels[pixelY * ToggleMarkTextureSize + x] =
                        new Color32(0xFF, 0xFF, 0xFF, 0xFF);
                }
            }
        }

        internal static void ApplyDropdown(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            ApplySelectable(
                dropdown,
                MaterialEditorTheme.Colors.DropdownSurface,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            ApplyControlOutline(
                dropdown.targetGraphic,
                MaterialEditorTheme.Colors.InputBorder);
            ApplyDropdownText(dropdown.captionText, null);
            ApplyDropdownText(dropdown.itemText, dropdown.captionText);

            var arrow = dropdown.transform.Find("Arrow");
            if (arrow != null)
            {
                var arrowImage = arrow.GetComponent<Image>();
                if (arrowImage != null)
                    arrowImage.color = MaterialEditorTheme.Colors.SecondaryText;
            }

            if (dropdown.template != null)
            {
                var templateImage = dropdown.template.GetComponent<Image>();
                if (templateImage != null)
                {
                    templateImage.color = MaterialEditorTheme.Colors.PopupSurface;
                    templateImage.canvasRenderer.SetAlpha(
                        MaterialEditorTheme.States.VisibleAlpha);
                    templateImage.maskable = true;
                    ApplyControlOutline(
                        templateImage,
                        MaterialEditorTheme.Colors.StrongBorder);
                }

                var templateScroll = dropdown.template.GetComponentInChildren<ScrollRect>(true);
                if (templateScroll != null)
                    ApplyDropdownScrollView(templateScroll);

                var popupStyle = dropdown.template.GetComponent<MaterialEditorDropdownPopupStyle>()
                                 ?? dropdown.template.gameObject.AddComponent<MaterialEditorDropdownPopupStyle>();
                popupStyle.Configure(dropdown);
            }

            if (dropdown.itemText != null)
            {
                var itemToggle = dropdown.itemText.GetComponentInParent<Toggle>();
                if (itemToggle != null)
                {
                    ApplyDropdownItem(itemToggle, dropdown.itemText);
                    ApplyDropdownText(dropdown.itemText, dropdown.captionText);
                }
            }
            ApplyTypography(dropdown.gameObject);
            ApplyDropdownText(dropdown.captionText, null);
            ApplyDropdownText(dropdown.itemText, dropdown.captionText);
        }

        internal static void ApplyDropdownPopup(
            Dropdown dropdown,
            Transform popupRoot,
            InputField filter,
            Button clearButton)
        {
            if (popupRoot == null)
                return;

            var popupImage = popupRoot.GetComponent<Image>();
            if (popupImage != null)
            {
                popupImage.color = MaterialEditorTheme.Colors.PopupSurface;
                popupImage.canvasRenderer.SetAlpha(
                    MaterialEditorTheme.States.VisibleAlpha);
                popupImage.maskable = true;
                ApplyControlOutline(
                    popupImage,
                    MaterialEditorTheme.Colors.StrongBorder);
            }

            var scroll = popupRoot.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null)
                ApplyDropdownScrollView(scroll);

            if (filter != null)
                ApplyInputField(filter);
            if (clearButton != null)
                ApplyButton(clearButton);

            if (dropdown != null)
            {
                ApplyDropdownText(dropdown.captionText, null);
                ApplyDropdownText(dropdown.itemText, dropdown.captionText);
            }

            var items = popupRoot.GetComponentsInChildren<Toggle>(true);
            for (var i = 0; i < items.Length; i++)
            {
                var itemText = items[i].GetComponentInChildren<Text>(true);
                ApplyDropdownItem(items[i], itemText);
                ApplyDropdownItemState(items[i], itemText, items[i].isOn);
            }
        }

        private static void ApplyDropdownText(Text text, Text renderingSource)
        {
            if (text == null)
                return;

            ApplyText(text, MaterialEditorTextRole.Input);
            ApplyTextRendering(text, renderingSource);
            text.color = MaterialEditorTheme.Colors.PrimaryText;
            text.canvasRenderer.SetAlpha(MaterialEditorTheme.States.VisibleAlpha);
            text.maskable = true;
            text.raycastTarget = false;
            text.fontSize = MaterialEditorLayout.DropdownFontSize;
            // Dropdown captions and popup entries occupy fixed-height rows.
            // Best Fit makes otherwise identical controls render at different
            // sizes and can visibly rescale pooled rows as their content changes.
            // Keep one stable type size and let the existing row bounds truncate
            // text that does not fit.
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = MaterialEditorLayout.DropdownFontSize;
            text.resizeTextMaxSize = MaterialEditorLayout.DropdownFontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = text.rectTransform;
            rect.offsetMin = new Vector2(
                rect.offsetMin.x,
                MaterialEditorLayout.DropdownTextVerticalInset);
            rect.offsetMax = new Vector2(
                rect.offsetMax.x,
                -MaterialEditorLayout.DropdownTextVerticalInset);

            text.SetVerticesDirty();
        }

        private static void ApplyDropdownItem(Toggle toggle, Text text)
        {
            if (toggle == null)
                return;

            ApplySelectable(
                toggle,
                MaterialEditorTheme.Colors.PopupSurface,
                MaterialEditorTheme.Colors.Hover,
                MaterialEditorTheme.Colors.Pressed,
                MaterialEditorTheme.Colors.DisabledSurface);
            if (toggle.graphic != null)
            {
                toggle.graphic.color = MaterialEditorTheme.Colors.SelectedText;
                toggle.graphic.canvasRenderer.SetAlpha(
                    MaterialEditorTheme.States.VisibleAlpha);
            }
            ApplyDropdownText(text, null);

            var state = toggle.GetComponent<MaterialEditorDropdownItemStyle>()
                        ?? toggle.gameObject.AddComponent<MaterialEditorDropdownItemStyle>();
            state.Configure(toggle, text);
        }

        internal static void ApplyDropdownItemState(
            Toggle toggle,
            Text text,
            bool selected)
        {
            if (toggle == null)
                return;

            var colors = toggle.colors;
            colors.normalColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.PopupSurface;
            colors.highlightedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.Hover;
            colors.pressedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.Pressed;
            colors.disabledColor = MaterialEditorTheme.Colors.DisabledSurface;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            toggle.colors = colors;

            if (toggle.targetGraphic != null)
            {
                var surface = !toggle.interactable
                    ? MaterialEditorTheme.Colors.DisabledSurface
                    : selected
                        ? MaterialEditorTheme.Colors.Selected
                        : MaterialEditorTheme.Colors.PopupSurface;
                toggle.targetGraphic.canvasRenderer.SetColor(surface);
            }

            if (text != null)
            {
                text.color = !toggle.interactable
                    ? MaterialEditorTheme.Colors.DisabledText
                    : selected
                        ? MaterialEditorTheme.Colors.SelectedText
                        : MaterialEditorTheme.Colors.PrimaryText;
                text.canvasRenderer.SetAlpha(
                    MaterialEditorTheme.States.VisibleAlpha);
                text.SetVerticesDirty();
            }
        }

        private static void ApplyDropdownScrollView(ScrollRect scrollRect)
        {
            if (scrollRect == null)
                return;

            var surface = scrollRect.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = MaterialEditorTheme.Colors.PopupSurface;
                surface.canvasRenderer.SetAlpha(
                    MaterialEditorTheme.States.VisibleAlpha);
                surface.maskable = true;
            }

            if (scrollRect.viewport != null)
            {
                var viewportSurface = scrollRect.viewport.GetComponent<Image>();
                if (viewportSurface != null)
                {
                    viewportSurface.color = MaterialEditorTheme.Colors.PopupSurface;
                    viewportSurface.canvasRenderer.SetAlpha(
                        MaterialEditorTheme.States.VisibleAlpha);
                    viewportSurface.maskable = true;
                    var mask = scrollRect.viewport.GetComponent<Mask>()
                               ?? scrollRect.viewport.gameObject.AddComponent<Mask>();
                    mask.showMaskGraphic = true;
                }
            }

            ApplyScrollbar(scrollRect.horizontalScrollbar);
            ApplyScrollbar(scrollRect.verticalScrollbar);
        }

        internal static void ApplyScrollView(ScrollRect scrollRect)
        {
            if (scrollRect == null)
                return;

            var surface = scrollRect.GetComponent<Image>();
            if (surface != null)
                surface.color = MaterialEditorTheme.Colors.ScrollSurface;
            if (scrollRect.viewport != null)
            {
                var viewportSurface = scrollRect.viewport.GetComponent<Image>();
                if (viewportSurface != null)
                    viewportSurface.color = MaterialEditorTheme.Colors.ScrollSurface;
            }

            ApplyScrollbar(scrollRect.horizontalScrollbar);
            ApplyScrollbar(scrollRect.verticalScrollbar);
        }

        internal static void ApplySlider(Slider slider)
        {
            if (slider == null)
                return;

            ApplySelectable(
                slider,
                MaterialEditorTheme.Colors.SliderHandle,
                MaterialEditorTheme.Colors.Accent,
                MaterialEditorTheme.Colors.SliderHandlePressed,
                MaterialEditorTheme.Colors.ControlDisabled);

            var background = slider.transform.Find("Background");
            if (background != null)
            {
                var backgroundImage = background.GetComponent<Image>();
                if (backgroundImage != null)
                    backgroundImage.color = MaterialEditorTheme.Colors.SliderTrack;
            }
            if (slider.fillRect != null)
            {
                var fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = MaterialEditorTheme.Colors.SliderFill;
            }
            if (slider.handleRect != null)
            {
                var handleImage = slider.handleRect.GetComponent<Image>();
                if (handleImage != null)
                    handleImage.color = MaterialEditorTheme.Colors.TintIdentity;
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

            ApplyTypography(row);
        }

        private static TextAnchor GetAlignment(TextAnchor current, MaterialEditorTextRole role)
        {
            switch (role)
            {
                case MaterialEditorTextRole.Title:
                case MaterialEditorTextRole.CenteredLabel:
                case MaterialEditorTextRole.Button:
                    return TextAnchor.MiddleCenter;
                case MaterialEditorTextRole.Label:
                case MaterialEditorTextRole.Input:
                case MaterialEditorTextRole.Placeholder:
                    return TextAnchor.MiddleLeft;
                default:
                    return WithMiddleVerticalAlignment(current);
            }
        }

        private static MaterialEditorTextRole GetAssignedTextRole(Text text)
        {
            var styleState = text.GetComponent<MaterialEditorTextStyleState>();
            return styleState != null
                ? styleState.Role
                : MaterialEditorTextRole.PreserveHorizontal;
        }

        private static Color GetTextColor(MaterialEditorTextRole role)
        {
            switch (role)
            {
                case MaterialEditorTextRole.Label:
                    return MaterialEditorTheme.Colors.SecondaryText;
                case MaterialEditorTextRole.Placeholder:
                    return MaterialEditorTheme.Colors.PlaceholderText;
                default:
                    return MaterialEditorTheme.Colors.PrimaryText;
            }
        }

        private static void ApplyTextRendering(Text text, Text renderingSource)
        {
            if (renderingSource != null)
            {
                if (renderingSource.font != null)
                    text.font = renderingSource.font;
                if (renderingSource.material != null)
                    text.material = renderingSource.material;
            }
            if (text.font == null)
                text.font = UIUtility.defaultFont;
            if (text.material == null && text.font != null)
                text.material = text.font.material;

            text.maskable = true;
            text.canvasRenderer.SetAlpha(MaterialEditorTheme.States.VisibleAlpha);
        }

        private static void ApplyControlOutline(Graphic graphic, Color color)
        {
            if (graphic == null)
                return;

            var outline = graphic.GetComponent<Outline>()
                          ?? graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }

        private static void ApplyScrollbar(Scrollbar scrollbar)
        {
            if (scrollbar == null)
                return;

            var track = scrollbar.GetComponent<Image>();
            if (track != null)
                track.color = MaterialEditorTheme.Colors.ScrollbarTrack;
            ApplySelectable(
                scrollbar,
                MaterialEditorTheme.Colors.ScrollbarHandle,
                MaterialEditorTheme.Colors.Accent,
                MaterialEditorTheme.Colors.ScrollbarHandlePressed,
                MaterialEditorTheme.Colors.ControlDisabled);
        }

        private static void ApplySelectable(
            Selectable selectable,
            Color normal,
            Color highlighted,
            Color pressed,
            Color disabled)
        {
            selectable.transition = Selectable.Transition.ColorTint;
            var colors = selectable.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.disabledColor = disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            selectable.colors = colors;
            if (selectable.targetGraphic != null)
                selectable.targetGraphic.color = MaterialEditorTheme.Colors.TintIdentity;
        }

        private static TextAnchor WithMiddleVerticalAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return TextAnchor.MiddleCenter;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return TextAnchor.MiddleRight;
                default:
                    return TextAnchor.MiddleLeft;
            }
        }
    }

    // Unity clones Dropdown.Template into a separate runtime hierarchy. The
    // clone can restore DefaultControls colors, so every dropdown reapplies the
    // shared popup style after Unity has finished creating its option items.
    internal sealed class MaterialEditorDropdownPopupStyle : MonoBehaviour
    {
        private static int _openPopupCount;

        [SerializeField] private Dropdown _dropdown;
        private bool _started;
        private bool _registeredOpen;

        internal static bool AnyPopupOpen => _openPopupCount > 0;

        internal void Configure(Dropdown dropdown)
        {
            _dropdown = dropdown;
            if (_started && isActiveAndEnabled)
                Apply();
        }

        private void OnEnable()
        {
            RegisterRuntimePopup();
            if (_started)
                Apply();
        }

        private void Start()
        {
            _started = true;
            RegisterRuntimePopup();
            Apply();
        }

        private void OnDisable()
        {
            UnregisterRuntimePopup();
        }

        private void OnDestroy()
        {
            UnregisterRuntimePopup();
        }

        private void RegisterRuntimePopup()
        {
            // uGUI renames the cloned template root before activating it. The
            // original disabled template remains named "Template" and is never
            // counted, while every Material Editor dropdown clone is tracked.
            if (_registeredOpen || gameObject.name != "Dropdown List")
                return;
            _registeredOpen = true;
            _openPopupCount++;
        }

        private void UnregisterRuntimePopup()
        {
            if (!_registeredOpen)
                return;
            _registeredOpen = false;
            if (_openPopupCount > 0)
                _openPopupCount--;
        }

        private void Apply()
        {
            MaterialEditorStyles.ApplyDropdownPopup(
                _dropdown,
                transform,
                null,
                null);
        }
    }

    // Dropdown item templates are cloned by uGUI. Each clone owns exactly one
    // guarded listener and releases it as soon as the popup item is disabled.
    internal sealed class MaterialEditorDropdownItemStyle : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Text _text;
        private bool _listening;

        internal void Configure(Toggle toggle, Text text)
        {
            if (_listening && !ReferenceEquals(_toggle, toggle))
                Unbind();
            _toggle = toggle;
            _text = text;
            if (isActiveAndEnabled)
                Bind();
            Refresh();
        }

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_listening || _toggle == null)
                return;
            _toggle.onValueChanged.AddListener(OnValueChanged);
            _listening = true;
        }

        private void Unbind()
        {
            if (!_listening)
                return;
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(OnValueChanged);
            _listening = false;
        }

        private void OnValueChanged(bool selected)
        {
            MaterialEditorStyles.ApplyDropdownItemState(_toggle, _text, selected);
        }

        private void Refresh()
        {
            if (_toggle != null)
                MaterialEditorStyles.ApplyDropdownItemState(
                    _toggle,
                    _text,
                    _toggle.isOn);
        }
    }

    internal sealed class MaterialEditorTextStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorTextRole _role;

        internal MaterialEditorTextRole Role => _role;

        internal void SetRole(MaterialEditorTextRole role)
        {
            _role = role;
        }
    }

    internal static class MaterialEditorControlFactory
    {
        internal static Canvas CreateNewUISystem(string name)
        {
            return UIUtility.CreateNewUISystem(name);
        }

        internal static Image CreatePanel(string name, Transform parent, MaterialEditorPanelRole role = MaterialEditorPanelRole.Default)
        {
            var panel = UIUtility.CreatePanel(name, parent);
            MaterialEditorStyles.ApplyPanel(panel, role);
            return panel;
        }

        internal static Text CreateText(string name, Transform parent, string value = "", MaterialEditorTextRole role = MaterialEditorTextRole.PreserveHorizontal)
        {
            var text = UIUtility.CreateText(name, parent, value);
            MaterialEditorStyles.ApplyText(text, role);
            return text;
        }

        internal static Button CreateButton(string name, Transform parent, string value)
        {
            var button = UIUtility.CreateButton(name, parent, value);
            MaterialEditorStyles.ApplyButton(button);
            return button;
        }

        internal static Button CreateSwatchButton(string name, Transform parent)
        {
            var button = UIUtility.CreateButton(name, parent, string.Empty);
            MaterialEditorStyles.ApplySwatchButton(button);
            return button;
        }

        internal static InputField CreateInputField(
            string name,
            Transform parent,
            string placeholder = "")
        {
            var inputField = UIUtility.CreateInputField(name, parent, placeholder);
            MaterialEditorStyles.ApplyInputField(inputField);
            return inputField;
        }

        internal static NumericInputView CreateNumericInput(
            string name,
            Transform parent,
            NumericInputSpec spec)
        {
            var inputField = CreateInputField(name, parent);
            var view = inputField.gameObject.AddComponent<NumericInputView>();
            view.Initialize(spec);
            return view;
        }

        internal static Toggle CreateToggle(string name, Transform parent, string value)
        {
            var toggle = UIUtility.CreateToggle(name, parent, value);
            MaterialEditorStyles.ApplyToggle(toggle);
            return toggle;
        }

        internal static Dropdown CreateDropdown(string name, Transform parent)
        {
            var dropdown = UIUtility.CreateDropdown(name, parent);
            MaterialEditorStyles.ApplyDropdown(dropdown);
            return dropdown;
        }

        internal static Slider CreateSlider(string name, Transform parent)
        {
            var slider = UIUtility.CreateSlider(name, parent);
            MaterialEditorStyles.ApplySlider(slider);
            return slider;
        }

        internal static ScrollRect CreateScrollView(string name, Transform parent)
        {
            var scrollView = UIUtility.CreateScrollView(name, parent);
            MaterialEditorStyles.ApplyScrollView(scrollView);
            return scrollView;
        }
    }
}
