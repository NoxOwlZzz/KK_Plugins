using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorInputStyles
    {
        private const int ToggleMarkTextureSize = 16;
        private const int ToggleMarkStrokeWidth = 2;
        private static Sprite _toggleMarkSprite;

        internal static void ApplyInputField(InputField inputField)
        {
            if (inputField == null)
                return;

            MaterialEditorControlStyleState.Assign(
                inputField,
                MaterialEditorControlStyleRole.InputField);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                inputField,
                MaterialEditorTheme.Colors.InputSurface,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                inputField.targetGraphic,
                MaterialEditorThemeColorRole.InputBorder);
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
                MaterialEditorPanelTextStyles.ApplyText(inputField.textComponent, MaterialEditorTextRole.Input);
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
                MaterialEditorPanelTextStyles.ApplyText(placeholder, MaterialEditorTextRole.Placeholder);
            }

            var numericInput = inputField.GetComponent<NumericInputView>();
            if (numericInput != null)
                numericInput.RestoreConfiguration();
        }

        internal static void ApplyToggle(Toggle toggle)
        {
            if (toggle == null)
                return;

            MaterialEditorControlStyleState.Assign(
                toggle,
                MaterialEditorControlStyleRole.Toggle);
            // Virtualized rows reuse the same Toggle for different values.
            // DefaultControls uses Fade, which can leave the previous check
            // visible while a recycled row is being rebound. The logical
            // value owns the mark immediately; hover only tints the surface.
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                toggle,
                MaterialEditorTheme.Colors.ControlNormal,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            ApplyToggleMark(toggle);
            foreach (var text in toggle.GetComponentsInChildren<Text>(true))
            {
                MaterialEditorPanelTextStyles.ApplyText(
                    text,
                    MaterialEditorPanelTextStyles.HasAssignedTextRole(text)
                        ? MaterialEditorPanelTextStyles.GetAssignedTextRole(text)
                        : MaterialEditorTextRole.Button);
            }
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
            graphic.canvasRenderer.SetAlpha(
                toggle.isOn
                    ? MaterialEditorTheme.States.VisibleAlpha
                    : MaterialEditorTheme.States.HiddenAlpha);
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

        internal static void ReapplyTheme(
            MaterialEditorControlStyleState state)
        {
            if (state == null)
                return;

            var selectable = state.GetComponent<Selectable>();
            switch (state.Role)
            {
                case MaterialEditorControlStyleRole.InputField:
                    var inputField = selectable as InputField;
                    ApplyInputField(inputField);
                    break;
                case MaterialEditorControlStyleRole.Toggle:
                    var toggle = selectable as Toggle;
                    ApplyToggle(toggle);
                    break;
                case MaterialEditorControlStyleRole.Slider:
                    ApplySlider(selectable as Slider);
                    break;
            }
        }

        internal static void ApplySlider(Slider slider)
        {
            if (slider == null)
                return;

            MaterialEditorControlStyleState.Assign(
                slider,
                MaterialEditorControlStyleRole.Slider);
            var legacy = MaterialEditorTheme.Mode
                         == MaterialEditorThemeMode.Legacy;
            var legacyColors = ColorBlock.defaultColorBlock;
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                slider,
                legacy
                    ? legacyColors.normalColor
                    : MaterialEditorTheme.Colors.SliderHandle,
                legacy
                    ? legacyColors.highlightedColor
                    : MaterialEditorTheme.Colors.Accent,
                legacy
                    ? legacyColors.pressedColor
                    : MaterialEditorTheme.Colors.SliderHandlePressed,
                legacy ? legacyColors.disabledColor : MaterialEditorTheme.Colors.ControlDisabled);

            var background = slider.transform.Find("Background");
            if (background != null)
            {
                var backgroundImage = background.GetComponent<Image>();
                if (backgroundImage != null)
                    backgroundImage.color = legacy ? Color.white : MaterialEditorTheme.Colors.SliderTrack;
            }
            if (slider.fillRect != null)
            {
                var fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = legacy ? Color.white : MaterialEditorTheme.Colors.SliderFill;
            }
            if (slider.handleRect != null)
            {
                var handleImage = slider.handleRect.GetComponent<Image>();
                if (handleImage != null)
                    handleImage.color = MaterialEditorTheme.Colors.TintIdentity;
            }
        }
    }
}
