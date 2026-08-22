using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorPanelTextStyles
    {
        internal static Color WindowColor => MaterialEditorTheme.Colors.Window;
        internal static Color LeftPanelColor => MaterialEditorTheme.Colors.LeftPanel;
        internal static Color CenterPanelColor => MaterialEditorTheme.Colors.CenterPanel;
        internal static Color RightPanelColor => MaterialEditorTheme.Colors.RightPanel;
        internal static Color MainPanelColor => MaterialEditorTheme.Colors.MainPanel;
        internal static Color HeaderColor => MaterialEditorTheme.Colors.Header;
        internal static Color SidePanelColor => MaterialEditorTheme.Colors.SidePanel;
        internal static Color NavigatorShaderHeaderColor => MaterialEditorTheme.Colors.NavigatorShaderHeader;
        internal static Color RowColor => MaterialEditorTheme.Colors.Row;
        internal static Color RendererColor => MaterialEditorTheme.Colors.RendererRow;
        internal static Color MaterialColor => MaterialEditorTheme.Colors.MaterialRow;
        internal static Color ShaderColor => MaterialEditorTheme.Colors.ShaderRow;
        internal static Color CategoryColor => MaterialEditorTheme.Colors.CategoryRow;
        internal static Color SubcategoryColor => MaterialEditorTheme.Colors.SubcategoryRow;
        internal static Color PropertyColor => MaterialEditorTheme.Colors.PropertyRow;
        internal static Color AlternatePropertyColor => MaterialEditorTheme.Colors.PropertyRowAlternate;
        internal static Color TransparentRowColor => MaterialEditorTheme.Colors.TransparentRow;
        internal static Color ChangedRowColor => MaterialEditorTheme.Colors.ChangedRow;
        internal static Color ScrollbarColor => MaterialEditorTheme.Colors.Scrollbar;
        internal static Color ShaderHintUnderlineColor => MaterialEditorTheme.Colors.ShaderHintUnderline;

        internal static void ApplyPanel(Image panel, MaterialEditorPanelRole role)
        {
            if (panel == null)
                return;

            // Graphic colors are multiplied by CanvasRenderer tint. A
            // Selectable transition from the previous theme can leave that
            // tint behind, which made the Legacy palette render light content
            // as effectively transparent. Panels own their semantic color, so
            // always reset the renderer multiplier before applying it.
            panel.canvasRenderer.SetColor(Color.white);
            panel.canvasRenderer.SetAlpha(
                MaterialEditorTheme.States.VisibleAlpha);

            var styleState =
                panel.GetComponent<MaterialEditorPanelStyleState>()
                ?? panel.gameObject.AddComponent<MaterialEditorPanelStyleState>();
            styleState.SetRole(role);

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
                case MaterialEditorPanelRole.RowBackdrop:
                    // The backdrop owns the visible pre-401 row surface while
                    // the ListEntry Image remains an invisible stencil source.
                    // Its RectTransform follows hierarchy depth, so nested
                    // rows keep a visible inset in Legacy. Dark already draws
                    // its surface through the active semantic row panel.
                    panel.color = MaterialEditorTheme.Mode
                                  == MaterialEditorThemeMode.Legacy
                        ? RowColor
                        : TransparentRowColor;
                    break;
                case MaterialEditorPanelRole.RowStencilMask:
                {
                    // Keep the stencil source opaque but out of the color
                    // buffer. RowBackdrop now owns the visible Legacy edge and
                    // can be inset independently without moving or weakening
                    // the mask used to clip the active row controls.
                    var mask = panel.GetComponent<Mask>();
                    panel.color = Color.white;
                    if (mask != null)
                        mask.showMaskGraphic = false;
                    break;
                }
                case MaterialEditorPanelRole.StencilMask:
                    // A zero-alpha Mask graphic is culled before it can write
                    // the stencil on the Unity versions used by the games.
                    // Mask.showMaskGraphic hides this opaque source from the
                    // color buffer; its alpha must nevertheless remain one so
                    // descendants are clipped instead of disappearing.
                    panel.color = Color.white;
                    break;
            }
        }

        internal static void ApplyText(Text text, MaterialEditorTextRole role = MaterialEditorTextRole.PreserveHorizontal)
        {
            if (text == null)
                return;

            text.alignment = GetAlignment(text.alignment, role);
            text.color = ResolveTextColor(role);
            text.canvasRenderer.SetColor(Color.white);
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

            RefreshTextRendering(text);
        }

        // Text.color is baked into uGUI's generated vertices, while
        // CanvasRenderer keeps a second runtime tint. Older Unity UI versions
        // ignore dirty requests made while a pooled or cloned hierarchy is
        // inactive. Refresh both render layers without invalidating row data or
        // forcing a layout pass.
        internal static void RefreshTextRendering(Text text)
        {
            if (text == null)
                return;

            text.canvasRenderer.SetColor(Color.white);
            text.canvasRenderer.SetAlpha(
                MaterialEditorTheme.States.VisibleAlpha);
            text.SetMaterialDirty();
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

        internal static bool HasAssignedTextRole(Text text)
        {
            if (text == null)
                return false;
            var state = text.GetComponent<MaterialEditorTextStyleState>();
            return state != null && state.Assigned;
        }

        internal static MaterialEditorTextRole GetAssignedTextRole(Text text)
        {
            var styleState = text.GetComponent<MaterialEditorTextStyleState>();
            return styleState != null
                ? styleState.Role
                : MaterialEditorTextRole.PreserveHorizontal;
        }

        internal static Color ResolveTextColor(MaterialEditorTextRole role)
        {
            if (MaterialEditorTheme.Mode == MaterialEditorThemeMode.Legacy)
            {
                switch (role)
                {
                    case MaterialEditorTextRole.Title:
                    case MaterialEditorTextRole.Chrome:
                    case MaterialEditorTextRole.SecondaryChrome:
                    case MaterialEditorTextRole.Tooltip:
                        return Color.white;
                    case MaterialEditorTextRole.Button:
                    case MaterialEditorTextRole.Input:
                    case MaterialEditorTextRole.Placeholder:
                        return MaterialEditorTheme.Colors.NativeControlText;
                    default:
                        return MaterialEditorTheme.Colors.PrimaryText;
                }
            }

            switch (role)
            {
                case MaterialEditorTextRole.SecondaryChrome:
                case MaterialEditorTextRole.Label:
                    return MaterialEditorTheme.Colors.SecondaryText;
                case MaterialEditorTextRole.Placeholder:
                    return MaterialEditorTheme.Colors.PlaceholderText;
                default:
                    return MaterialEditorTheme.Colors.PrimaryText;
            }
        }
        internal static void ApplyTextRendering(Text text, Text renderingSource)
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
}
