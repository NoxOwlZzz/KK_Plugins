using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorResponsiveSideState
    {
        Hidden,
        Collapsed,
        Expanded
    }

    internal sealed class MaterialEditorResponsiveLayout
    {
        internal float UiScale;
        internal float CanvasWidth;
        internal float CanvasHeight;
        internal float MainLeftAnchor;
        internal float MainBottomAnchor;
        internal float MainRightAnchor;
        internal float MainTopAnchor;
        internal float MainWidth;
        internal float MainHeight;
        internal float ViewportHeight;
        internal float RightPanelWidth;
        internal float MinimumDragOffsetX;
        internal float MaximumDragOffsetX;
        internal float MinimumDragOffsetY;
        internal float MaximumDragOffsetY;
        internal MaterialEditorWindowDragBounds HeaderDragBounds;
        internal MaterialEditorWindowDragBounds WholeDragBounds;

        internal void ClampDragOffset(ref float x, ref float y)
        {
            WholeDragBounds.Clamp(ref x, ref y);
        }

        internal void ClampDragOffset(
            MaterialEditorWindowDragMode mode,
            ref float x,
            ref float y)
        {
            MaterialEditorWindowBoundsPolicy.ClampDragOffset(
                mode,
                HeaderDragBounds,
                WholeDragBounds,
                ref x,
                ref y);
        }
    }

    internal static class MaterialEditorResponsiveLayoutPolicy
    {
        internal static MaterialEditorResponsiveLayout Calculate(
            float uiScale,
            float uiWidth,
            float uiHeight,
            float requestedRightPanelWidth,
            MaterialEditorResponsiveSideState leftState,
            MaterialEditorResponsiveSideState rightState)
        {
            var normalizedScale = ClampFinite(
                uiScale,
                MaterialEditorTheme.Metrics.UiScaleMinimum,
                MaterialEditorTheme.Metrics.UiScaleMaximum,
                MaterialEditorTheme.Metrics.UiScaleDefault);
            return Calculate(
                uiScale,
                uiWidth,
                uiHeight,
                requestedRightPanelWidth,
                leftState,
                rightState,
                MaterialEditorTheme.Metrics.CanvasReferenceWidth
                / normalizedScale,
                MaterialEditorTheme.Metrics.CanvasReferenceHeight
                / normalizedScale);
        }

        internal static MaterialEditorResponsiveLayout Calculate(
            float uiScale,
            float uiWidth,
            float uiHeight,
            float requestedRightPanelWidth,
            MaterialEditorResponsiveSideState leftState,
            MaterialEditorResponsiveSideState rightState,
            float actualCanvasWidth,
            float actualCanvasHeight)
        {
            uiScale = ClampFinite(
                uiScale,
                MaterialEditorTheme.Metrics.UiScaleMinimum,
                MaterialEditorTheme.Metrics.UiScaleMaximum,
                MaterialEditorTheme.Metrics.UiScaleDefault);
            uiWidth = ClampFinite(
                uiWidth,
                MaterialEditorTheme.Metrics.WindowWidthMinimum,
                MaterialEditorTheme.Metrics.WindowWidthMaximum,
                MaterialEditorTheme.Metrics.WindowWidthDefault);
            uiHeight = ClampFinite(
                uiHeight,
                MaterialEditorTheme.Metrics.WindowHeightMinimum,
                MaterialEditorTheme.Metrics.WindowHeightMaximum,
                MaterialEditorTheme.Metrics.WindowHeightDefault);
            requestedRightPanelWidth = ClampFinite(
                requestedRightPanelWidth,
                MaterialEditorTheme.Metrics.SidePanelMinimumWidth,
                MaterialEditorTheme.Metrics.SidePanelMaximumWidth,
                MaterialEditorTheme.Metrics.SidePanelDefaultWidth);

            var referenceCanvasWidth =
                MaterialEditorTheme.Metrics.CanvasReferenceWidth / uiScale;
            var referenceCanvasHeight =
                MaterialEditorTheme.Metrics.CanvasReferenceHeight / uiScale;
            var canvasWidth = PositiveFiniteOrFallback(
                actualCanvasWidth,
                referenceCanvasWidth);
            var canvasHeight = PositiveFiniteOrFallback(
                actualCanvasHeight,
                referenceCanvasHeight);
            var outerX = canvasWidth
                         * MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction;
            var outerY = canvasHeight
                         * MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction;

            // Keep the central editor on the historical UIWidth geometry.
            // Side-panel state controls only which surface is visible outside
            // the Main panel; it must never move or resize Main itself.
            var mainLeft = GetMainLeft(
                outerX,
                MaterialEditorTheme.Metrics.CategoryNavigatorWidth);
            var expandedRightWidth = CapExpandedRightWidth(
                requestedRightPanelWidth,
                canvasWidth,
                mainLeft);
            // Preserve the legacy normalized-anchor meaning of UIWidth:
            // right = UIWidth * UIScale + left - outerFraction. Do not shrink
            // the requested Main width to make optional side panels fit.
            var baseMainWidth = Math.Max(
                MaterialEditorTheme.Metrics.ResponsiveMinimumMainWidth,
                (uiWidth * uiScale
                 - MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction)
                * canvasWidth);
            var mainWidth = baseMainWidth;

            var maximumMainHeight = Math.Min(
                Math.Max(0f, canvasHeight - outerY * 2f),
                MaterialEditorTheme.Metrics.CanvasReferenceHeight
                * (1f - 2f
                    * MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction));
            var minimumMainHeight = Math.Min(
                MaterialEditorTheme.Metrics.ResponsiveMinimumMainHeight,
                maximumMainHeight);
            var requestedMainHeight =
                uiHeight * MaterialEditorTheme.Metrics.CanvasReferenceHeight
                - outerY;
            var mainHeight = Clamp(
                requestedMainHeight,
                minimumMainHeight,
                maximumMainHeight);
            var viewportHeight = Math.Max(
                0f,
                mainHeight
                - MaterialEditorTheme.Metrics.TopBarHeight
                - MaterialEditorTheme.Metrics.Margin * 1.5f);

            var mainRight = mainLeft + mainWidth;
            var mainTop = outerY + mainHeight;
            var leftFootprint = GetLeftVisibleFootprint(leftState);
            var rightFootprint = GetRightVisibleFootprint(
                expandedRightWidth,
                rightState);
            var aggregateLeft = mainLeft - leftFootprint;
            var aggregateRight = mainRight + rightFootprint;
            var wholeBounds = new MaterialEditorWindowDragBounds(
                -aggregateLeft,
                canvasWidth - aggregateRight,
                -outerY,
                canvasHeight - mainTop);
            var recoverableHeaderWidth = Math.Min(
                MaterialEditorTheme.Metrics.WindowHeaderRecoveryWidth,
                mainWidth);
            var headerBounds = new MaterialEditorWindowDragBounds(
                recoverableHeaderWidth - mainRight,
                canvasWidth - recoverableHeaderWidth - mainLeft,
                -(mainTop - MaterialEditorTheme.Metrics.HeaderHeight),
                canvasHeight - mainTop);

            return new MaterialEditorResponsiveLayout
            {
                UiScale = uiScale,
                CanvasWidth = canvasWidth,
                CanvasHeight = canvasHeight,
                MainLeftAnchor = mainLeft / canvasWidth,
                MainBottomAnchor = outerY / canvasHeight,
                MainRightAnchor = mainRight / canvasWidth,
                MainTopAnchor = mainTop / canvasHeight,
                MainWidth = mainWidth,
                MainHeight = mainHeight,
                ViewportHeight = viewportHeight,
                RightPanelWidth = expandedRightWidth,
                MinimumDragOffsetX = wholeBounds.MinimumX,
                MaximumDragOffsetX = wholeBounds.MaximumX,
                MinimumDragOffsetY = wholeBounds.MinimumY,
                MaximumDragOffsetY = wholeBounds.MaximumY,
                HeaderDragBounds = headerBounds,
                WholeDragBounds = wholeBounds
            };
        }

        private static float GetMainLeft(float outerX, float leftWidth)
        {
            if (leftWidth <= 0f)
                return outerX;
            return Math.Max(
                outerX,
                leftWidth + MaterialEditorTheme.Metrics.Margin * 2f);
        }

        private static float GetLeftVisibleFootprint(
            MaterialEditorResponsiveSideState state)
        {
            if (state == MaterialEditorResponsiveSideState.Hidden)
                return 0f;
            return state == MaterialEditorResponsiveSideState.Collapsed
                ? MaterialEditorTheme.Metrics.CategoryNavigatorCollapsedWidth
                : MaterialEditorTheme.Metrics.CategoryNavigatorWidth
                  + MaterialEditorTheme.Metrics.Margin;
        }

        private static float GetRightVisibleFootprint(
            float rightWidth,
            MaterialEditorResponsiveSideState state)
        {
            if (state == MaterialEditorResponsiveSideState.Hidden)
                return 0f;
            return state == MaterialEditorResponsiveSideState.Collapsed
                ? MaterialEditorTheme.Metrics.SelectionPanelCollapsedWidth
                : rightWidth + MaterialEditorTheme.Metrics.Margin;
        }

        internal static MaterialEditorResponsiveSideState GetRightSideState(
            bool selectionListsVisible,
            bool renameListVisible)
        {
            return selectionListsVisible || renameListVisible
                ? MaterialEditorResponsiveSideState.Expanded
                : MaterialEditorResponsiveSideState.Collapsed;
        }

        private static float CapExpandedRightWidth(
            float requestedWidth,
            float canvasWidth,
            float mainLeft)
        {
            var maximumWidth =
                canvasWidth
                - mainLeft
                - MaterialEditorTheme.Metrics.ResponsiveMinimumMainWidth
                - MaterialEditorTheme.Metrics.Margin;
            if (maximumWidth < MaterialEditorTheme.Metrics.SidePanelMinimumWidth)
                return MaterialEditorTheme.Metrics.SidePanelMinimumWidth;
            return Math.Min(requestedWidth, maximumWidth);
        }

        private static float ClampFinite(
            float value,
            float minimum,
            float maximum,
            float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                value = fallback;
            return Clamp(value, minimum, maximum);
        }

        private static float PositiveFiniteOrFallback(
            float value,
            float fallback)
        {
            return float.IsNaN(value)
                   || float.IsInfinity(value)
                   || value <= 0f
                ? fallback
                : value;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (maximum < minimum)
                return maximum;
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }
    }

    internal sealed class MaterialEditorResponsiveCanvasWatcher : MonoBehaviour
    {
        private Action _dimensionsChanged;

        internal void Initialize(Action dimensionsChanged)
        {
            _dimensionsChanged = dimensionsChanged;
        }

        private void OnRectTransformDimensionsChange()
        {
            var dimensionsChanged = _dimensionsChanged;
            if (dimensionsChanged != null)
                dimensionsChanged();
        }

        private void OnDestroy()
        {
            _dimensionsChanged = null;
        }
    }

    internal static class VirtualListCachePolicy
    {
        internal static int MaximumViewCount
        {
            get
            {
                var maximumMainHeight =
                    MaterialEditorTheme.Metrics.CanvasReferenceHeight
                    * (1f - 2f
                        * MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction);
                var maximumViewportHeight = Math.Max(
                    0f,
                    maximumMainHeight
                    - MaterialEditorTheme.Metrics.TopBarHeight
                    - MaterialEditorTheme.Metrics.Margin * 1.5f);
                return UnboundedViewCount(
                    maximumViewportHeight,
                    MaterialEditorTheme.Metrics.RowHeight);
            }
        }

        internal static int RequiredViewCount(
            float viewportHeight,
            float rowHeight,
            int modelCount)
        {
            if (modelCount <= 0
                || viewportHeight <= 0f
                || rowHeight <= 0f
                || float.IsNaN(viewportHeight)
                || float.IsInfinity(viewportHeight)
                || float.IsNaN(rowHeight)
                || float.IsInfinity(rowHeight))
                return 0;

            return Math.Min(
                modelCount,
                Math.Min(
                    MaximumViewCount,
                    UnboundedViewCount(viewportHeight, rowHeight)));
        }

        private static int UnboundedViewCount(
            float viewportHeight,
            float rowHeight)
        {
            return (int)Math.Ceiling(viewportHeight / rowHeight) + 1;
        }
    }
}
