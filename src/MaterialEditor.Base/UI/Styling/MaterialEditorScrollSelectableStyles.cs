using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorScrollSelectableStyles
    {
        internal static void ApplyDropdownScrollView(ScrollRect scrollRect)
        {
            if (scrollRect == null)
                return;

            MaterialEditorScrollStyleState.Assign(scrollRect, true);
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

            MaterialEditorScrollStyleState.Assign(scrollRect, false);
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

        internal static void ApplyControlOutline(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return;

            var outline = graphic.GetComponent<Outline>()
                          ?? graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = MaterialEditorTheme.Colors.Resolve(role);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
            MaterialEditorOutlineStyleState.Assign(graphic, role);
        }

        internal static void ReapplyTheme(MaterialEditorScrollStyleState state)
        {
            if (state == null)
                return;

            var scrollRect = state.GetComponent<ScrollRect>();
            if (state.Popup)
                ApplyDropdownScrollView(scrollRect);
            else
                ApplyScrollView(scrollRect);
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

        internal static void ApplySelectable(
            Selectable selectable,
            Color normal,
            Color highlighted,
            Color pressed,
            Color disabled)
        {
            if (selectable == null)
                return;

            selectable.transition = Selectable.Transition.ColorTint;
            if (selectable.targetGraphic != null)
                selectable.targetGraphic.color =
                    MaterialEditorTheme.Colors.TintIdentity;

            var colors = selectable.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.disabledColor = disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration =
                MaterialEditorTheme.States.SelectableFadeDuration;
            selectable.colors = colors;
            SynchronizeCurrentState(selectable);
        }

        internal static void SynchronizeCurrentState(Selectable selectable)
        {
            if (selectable == null || selectable.targetGraphic == null)
                return;

            var colors = selectable.colors;
            var effectiveColor = selectable.interactable
                ? colors.normalColor
                : colors.disabledColor;
            var graphic = selectable.targetGraphic;
            graphic.color = MaterialEditorTheme.Colors.TintIdentity;
            // SetColor already carries the complete RGBA value. Calling
            // SetAlpha afterwards destroys semantic alpha (notably the
            // half-alpha Legacy category and transparent navigation rows).
            graphic.canvasRenderer.SetColor(effectiveColor);
            graphic.SetMaterialDirty();
            graphic.SetVerticesDirty();
        }
    }
}
