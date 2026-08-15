using MaterialEditorAPI;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

internal static class UiResponsivePhaseTenContractTests
{
    private const float RowHeight = MaterialEditorTheme.Metrics.RowHeight;

    internal static void Run()
    {
        ResponsiveMatrixStaysFiniteOrderedAndOnCanvas();
        CanvasAspectChangesUseRealBoundsWithoutUnboundedHeight();
        CollapsedSurfacesKeepTheCentralWorkspaceFixed();
        ManualCollapseKeepsTheCentralRectAcrossConfigurations();
        ClosedLeftOverlayUsesOnlyItsVisibleDragFootprint();
        ClosedRightOverlayUsesOnlyItsVisibleDragFootprint();
        HeightSideAndDragClampsUseRealBounds();
        CachePolicyIsDerivedBoundedAndOverscanned();
        VirtualListGrowsAndShrinksActiveCapacityWithoutLeakingBindings();
        ResponsiveIntegrationAddsNoPollingPersistenceOrSplitter();
        Console.WriteLine("Phase 10 responsive regression guards passed.");
    }

    private static void CanvasAspectChangesUseRealBoundsWithoutUnboundedHeight()
    {
        var aspects = new[]
        {
            new[] { 1920f, 1080f },
            new[] { 1920f, 1440f },
            new[] { 2520f, 1080f }
        };
        foreach (var scale in new[] { 1f, 1.75f, 3f })
        foreach (var aspect in aspects)
        {
            var canvasWidth = aspect[0] / scale;
            var canvasHeight = aspect[1] / scale;
            var layout = MaterialEditorResponsiveLayoutPolicy.Calculate(
                scale,
                1f,
                1f,
                500f,
                MaterialEditorResponsiveSideState.Expanded,
                MaterialEditorResponsiveSideState.Expanded,
                canvasWidth,
                canvasHeight);
            var name = scale + "/" + aspect[0] + "x" + aspect[1];
            Equal(canvasWidth, layout.CanvasWidth, name + " real canvas width");
            Equal(canvasHeight, layout.CanvasHeight, name + " real canvas height");
            Near(
                Math.Max(
                    MaterialEditorTheme.Metrics.ResponsiveMinimumMainWidth,
                    (scale
                     - MaterialEditorTheme.Metrics
                         .ResponsiveOuterMarginFraction)
                    * canvasWidth),
                layout.MainWidth,
                0.001f,
                name + " historical UIWidth anchor semantics");
            Equal(true,
                layout.MainLeftAnchor >= 0f
                && layout.MainLeftAnchor < layout.MainRightAnchor
                && layout.MainBottomAnchor >= 0f
                && layout.MainTopAnchor <= 1f,
                name + " Main anchors remain ordered with recoverable overflow");
            Equal(true,
                layout.MainHeight <=
                MaterialEditorTheme.Metrics.CanvasReferenceHeight
                * (1f - 2f
                    * MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction),
                name + " aspect height respects absolute pool bound");
            Equal(true,
                VirtualListCachePolicy.RequiredViewCount(
                    layout.ViewportHeight,
                    RowHeight,
                    int.MaxValue)
                <= VirtualListCachePolicy.MaximumViewCount,
                name + " viewport fits bounded cache");
        }

        var invalid = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1f,
            0.8f,
            0.58f,
            180f,
            MaterialEditorResponsiveSideState.Hidden,
            MaterialEditorResponsiveSideState.Collapsed,
            float.NaN,
            -1f);
        Equal(1920f, invalid.CanvasWidth, "invalid canvas width fallback");
        Equal(1080f, invalid.CanvasHeight, "invalid canvas height fallback");
    }

    private static void ResponsiveMatrixStaysFiniteOrderedAndOnCanvas()
    {
        var scales = new[] { 1f, 1.75f, 3f };
        var widths = new[] { 0f, 0.33f, 1f };
        var heights = new[] { 0f, 0.3f, 1f };
        var sideWidths = new[] { 100f, 180f, 500f };
        var leftStates = new[]
        {
            MaterialEditorResponsiveSideState.Hidden,
            MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveSideState.Expanded
        };
        var rightStates = new[]
        {
            MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveSideState.Expanded
        };

        foreach (var scale in scales)
        foreach (var width in widths)
        foreach (var height in heights)
        foreach (var sideWidth in sideWidths)
        foreach (var left in leftStates)
        foreach (var right in rightStates)
        {
            var layout = MaterialEditorResponsiveLayoutPolicy.Calculate(
                scale, width, height, sideWidth, left, right);
            var name = scale + "/" + width + "/" + height + "/"
                       + sideWidth + "/" + left + "/" + right;

            Finite(layout.MainLeftAnchor, name + " left");
            Finite(layout.MainRightAnchor, name + " right");
            Finite(layout.MainBottomAnchor, name + " bottom");
            Finite(layout.MainTopAnchor, name + " top");
            Equal(true,
                layout.MainLeftAnchor >= 0f
                && layout.MainLeftAnchor < layout.MainRightAnchor,
                name + " horizontal anchors");
            Equal(true,
                layout.MainBottomAnchor >= 0f
                && layout.MainBottomAnchor < layout.MainTopAnchor
                && layout.MainTopAnchor <= 1f,
                name + " vertical anchors");
            Equal(true,
                layout.MainWidth + 0.001f >=
                MaterialEditorTheme.Metrics.ResponsiveMinimumMainWidth,
                name + " configured Main width is never side-clamped");
            Equal(true,
                layout.MainHeight >=
                MaterialEditorTheme.Metrics.ResponsiveMinimumMainHeight,
                name + " usable center height");
            Equal(true,
                layout.RightPanelWidth >=
                MaterialEditorTheme.Metrics.SidePanelMinimumWidth
                && layout.RightPanelWidth <=
                MaterialEditorTheme.Metrics.SidePanelMaximumWidth,
                name + " side width bounds");
            Finite(layout.MinimumDragOffsetX, name + " whole minimum X");
            Finite(layout.MaximumDragOffsetX, name + " whole maximum X");
            Finite(layout.MinimumDragOffsetY, name + " whole minimum Y");
            Finite(layout.MaximumDragOffsetY, name + " whole maximum Y");
            Finite(layout.HeaderDragBounds.MinimumX,
                name + " header minimum X");
            Finite(layout.HeaderDragBounds.MaximumX,
                name + " header maximum X");
        }
    }

    private static void CollapsedSurfacesKeepTheCentralWorkspaceFixed()
    {
        var expanded = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Expanded);
        var leftCollapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveSideState.Expanded);
        var rightCollapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Collapsed);
        var bothCollapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveSideState.Collapsed);
        AssertSameCentralRect(
            expanded,
            leftCollapsed,
            "left collapse keeps the central workspace fixed");
        AssertSameCentralRect(
            expanded,
            rightCollapsed,
            "right collapse keeps the central workspace fixed");
        AssertSameCentralRect(
            expanded,
            bothCollapsed,
            "combined side collapse keeps the central workspace fixed");
        var bothHidden = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Hidden,
            MaterialEditorResponsiveSideState.Hidden);
        AssertSameCentralRect(
            expanded,
            bothHidden,
            "even absent side surfaces do not change historical Main geometry");

        var constrained = MaterialEditorResponsiveLayoutPolicy.Calculate(
            3f,
            0.33f,
            0.3f,
            500f,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Expanded);
        var constrainedLeftCollapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            3f,
            0.33f,
            0.3f,
            500f,
            MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveSideState.Expanded);
        var constrainedRightCollapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            3f,
            0.33f,
            0.3f,
            500f,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Collapsed);
        var constrainedBothCollapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            3f,
            0.33f,
            0.3f,
            500f,
            MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveSideState.Collapsed);
        var policy = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.ResponsiveLayout.cs");
        DoesNotContain(policy,
            "CollapseLeftForSpace",
            "responsive layout exposes no automatic navigator override");
        Equal(true,
            constrained.RightPanelWidth >= 100f
            && constrained.RightPanelWidth < 500f,
            "right side width remains finite and capped independently");
        AssertSameCentralRect(
            constrained,
            constrainedLeftCollapsed,
            "forced compact layout ignores the logical left fold");
        AssertSameCentralRect(
            constrained,
            constrainedRightCollapsed,
            "forced compact layout ignores the logical right fold");
        AssertSameCentralRect(
            constrained,
            constrainedBothCollapsed,
            "small-canvas Main remains stable when both overlays are folded");

        var leftHidden = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Hidden,
            MaterialEditorResponsiveSideState.Expanded);
        AssertSameCentralRect(
            expanded,
            leftHidden,
            "HasCategories/Hidden cannot move or widen Main");

        var category = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");
        DoesNotContain(category,
            "_responsiveCollapsed",
            "responsive code cannot own category visual state");
        DoesNotContain(category,
            "VisuallyExpanded",
            "logical expansion is the only expansion state");
        DoesNotContain(category,
            "CategoryNavigatorCollapsedRail",
            "closed navigator has no full-height rail");
        Contains(category,
            "Panel.gameObject.SetActive(_expanded);",
            "the logical user preference alone controls the panel");
        Contains(category,
            "_expandButton.gameObject.SetActive(!_expanded);",
            "closed navigator exposes only its edge overlay button");

        var popup = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.PopupMenu.cs");
        Contains(popup,
            "CategoryNavigatorButton.interactable = true;",
            "global category action remains available at every canvas size");
        DoesNotContain(popup,
            "Categories need more space",
            "global menu has no responsive override state");
    }

    private static void ManualCollapseKeepsTheCentralRectAcrossConfigurations()
    {
        var aspects = new[]
        {
            new[] { 1920f, 1080f },
            new[] { 1920f, 1440f },
            new[] { 2520f, 1080f }
        };
        foreach (var scale in new[] { 1f, 1.75f, 3f })
        foreach (var width in new[] { 0f, 0.33f, 1f })
        foreach (var sideWidth in new[] { 100f, 180f, 500f })
        foreach (var aspect in aspects)
        {
            var canvasWidth = aspect[0] / scale;
            var canvasHeight = aspect[1] / scale;
            var expanded = MaterialEditorResponsiveLayoutPolicy.Calculate(
                scale, width, 0.3f, sideWidth,
                MaterialEditorResponsiveSideState.Expanded,
                MaterialEditorResponsiveSideState.Expanded,
                canvasWidth, canvasHeight);
            var name = scale + "/" + width + "/" + sideWidth + "/"
                       + aspect[0] + "x" + aspect[1];

            foreach (var state in new[]
                     {
                         new[]
                         {
                             MaterialEditorResponsiveSideState.Collapsed,
                             MaterialEditorResponsiveSideState.Expanded
                         },
                         new[]
                         {
                             MaterialEditorResponsiveSideState.Expanded,
                             MaterialEditorResponsiveSideState.Collapsed
                         },
                         new[]
                         {
                             MaterialEditorResponsiveSideState.Collapsed,
                             MaterialEditorResponsiveSideState.Collapsed
                         }
                     })
            {
                var collapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
                    scale, width, 0.3f, sideWidth,
                    state[0], state[1], canvasWidth, canvasHeight);
                AssertSameCentralRect(
                    expanded,
                    collapsed,
                    name + "/" + state[0] + "/" + state[1]);
            }
        }
    }

    private static void ClosedRightOverlayUsesOnlyItsVisibleDragFootprint()
    {
        var expanded = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Expanded);
        var collapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Collapsed);

        AssertSameCentralRect(
            expanded,
            collapsed,
            "right edge overlay keeps the central workspace fixed");
        Near(
            expanded.RightPanelWidth
            + MaterialEditorTheme.Metrics.Margin
            - MaterialEditorTheme.Metrics.SelectionPanelCollapsedWidth,
            collapsed.MaximumDragOffsetX - expanded.MaximumDragOffsetX,
            0.001f,
            "hidden right content releases only its invisible drag footprint");
        Near(
            collapsed.CanvasWidth
            - (collapsed.MainRightAnchor * collapsed.CanvasWidth
               + MaterialEditorTheme.Metrics.SelectionPanelCollapsedWidth),
            collapsed.MaximumDragOffsetX,
            0.001f,
            "closed right clamp counts only the visible overlay button");

        var collapsedEdge = float.PositiveInfinity;
        var collapsedY = 0f;
        collapsed.ClampDragOffset(ref collapsedEdge, ref collapsedY);
        Equal(collapsed.MaximumDragOffsetX,
            collapsedEdge,
            "closed overlay can reach the right canvas edge");

        var reopenedOffset = collapsedEdge;
        var reopenedY = 0f;
        expanded.ClampDragOffset(ref reopenedOffset, ref reopenedY);
        Equal(expanded.MaximumDragOffsetX,
            reopenedOffset,
            "reopening reclamps the full right aggregate");

        Equal(MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveLayoutPolicy.GetRightSideState(false, false),
            "no right content exposes only the closed overlay");
        Equal(MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveLayoutPolicy.GetRightSideState(true, false),
            "selection lists use the full right footprint");
        Equal(MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveLayoutPolicy.GetRightSideState(false, true),
            "Rename uses the full right footprint");

        var policy = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.ResponsiveLayout.cs");
        DoesNotContain(policy,
            "GetRightLayoutFootprint",
            "right state cannot reserve or resize the central layout");
        Contains(policy,
            "MaterialEditorTheme.Metrics.SelectionPanelCollapsedWidth",
            "drag footprint counts the visible edge overlay");

        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        Contains(window,
            "MaterialEditorResponsiveLayoutPolicy.GetRightSideState(\n"
            + "                    _selectionListsVisible,\n"
            + "                    _renameListVisible);",
            "window resolves selection and Rename visibility explicitly");
        var stateChange = Slice(
            window,
            "internal void SetRightPanelState(",
            "internal void SetHeaderTitleHorizontalOffset(");
        Contains(stateChange,
            "_selectionListsVisible = selectionListsVisible;",
            "right state updates selection visibility before layout");
        Contains(stateChange,
            "_renameListVisible = renameListVisible;",
            "right state updates Rename visibility before layout");
        Contains(stateChange,
            "ApplySettings();",
            "right visibility changes recalculate and reclamp the aggregate");
    }

    private static void ClosedLeftOverlayUsesOnlyItsVisibleDragFootprint()
    {
        var expanded = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Expanded);
        var collapsed = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1.75f,
            0.33f,
            0.3f,
            180f,
            MaterialEditorResponsiveSideState.Collapsed,
            MaterialEditorResponsiveSideState.Expanded);

        AssertSameCentralRect(
            expanded,
            collapsed,
            "left edge overlay keeps the central workspace fixed");
        Near(
            MaterialEditorTheme.Metrics.CategoryNavigatorWidth
            + MaterialEditorTheme.Metrics.Margin
            - MaterialEditorTheme.Metrics.CategoryNavigatorCollapsedWidth,
            expanded.MinimumDragOffsetX - collapsed.MinimumDragOffsetX,
            0.001f,
            "hidden left content releases only its invisible drag footprint");
        Near(
            -(collapsed.MainLeftAnchor * collapsed.CanvasWidth
              - MaterialEditorTheme.Metrics.CategoryNavigatorCollapsedWidth),
            collapsed.MinimumDragOffsetX,
            0.001f,
            "closed left clamp counts only the visible overlay button");

        var collapsedEdge = float.NegativeInfinity;
        var collapsedY = 0f;
        collapsed.ClampDragOffset(ref collapsedEdge, ref collapsedY);
        Equal(collapsed.MinimumDragOffsetX,
            collapsedEdge,
            "closed overlay can reach the left canvas edge");

        var reopenedOffset = collapsedEdge;
        var reopenedY = 0f;
        expanded.ClampDragOffset(ref reopenedOffset, ref reopenedY);
        Equal(expanded.MinimumDragOffsetX,
            reopenedOffset,
            "reopening reclamps the full left aggregate");

        var policy = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.ResponsiveLayout.cs");
        Contains(policy,
            "GetLeftVisibleFootprint(leftState)",
            "left drag clamp uses the visible navigator footprint");
        Contains(policy,
            "MaterialEditorTheme.Metrics.CategoryNavigatorCollapsedWidth",
            "left drag footprint counts the visible edge overlay");

        var category = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");
        var stateChange = Slice(
            category,
            "private void SetExpanded(bool expanded)",
            "private void ApplyViewportAnchor(");
        Contains(stateChange,
            "_expandedChanged?.Invoke(_expanded);",
            "left visibility publishes the changed footprint");

        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        Contains(window,
            "toggleCategory,\n                SetCategoryNavigatorGlyph);",
            "category visibility callback is wired to the window layout");
        var layoutChange = Slice(
            window,
            "private void SetCategoryNavigatorGlyph(bool expanded)",
            "private void BuildSelectionPanels(");
        Contains(layoutChange,
            "ApplySettings();",
            "left visibility changes recalculate and reclamp the aggregate");
    }

    private static void HeightSideAndDragClampsUseRealBounds()
    {
        var minimum = MaterialEditorResponsiveLayoutPolicy.Calculate(
            3f,
            float.NaN,
            0f,
            float.PositiveInfinity,
            MaterialEditorResponsiveSideState.Expanded,
            MaterialEditorResponsiveSideState.Expanded);
        Equal(3f, minimum.UiScale, "scale clamp");
        Equal(138f, minimum.MainHeight, "minimum main height");
        Equal(true,
            minimum.ViewportHeight >= 3f * RowHeight,
            "minimum center contains three rows");
        var selectionViewportHeight =
            (minimum.MainHeight - MaterialEditorTheme.Metrics.Margin) / 2f
            - MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight
            - MaterialEditorTheme.Metrics.SelectionPanelFilterHeight
            - 2f * MaterialEditorTheme.Spacing.SelectionPanelContentInset;
        Equal(true,
            selectionViewportHeight >= RowHeight,
            "minimum selection half contains one complete row");
        var renamePanelHeight =
            minimum.MainHeight / 2f
            - MaterialEditorTheme.Metrics.Margin / 2f;
        var renameViewportHeight =
            renamePanelHeight
            - 42.5f
            - MaterialEditorTheme.Spacing.SelectionPanelContentInset;
        Equal(true,
            renameViewportHeight >= RowHeight,
            "minimum Rename viewport contains one complete row");
        Equal(true,
            minimum.RightPanelWidth >= 100f,
            "invalid side width safe fallback/cap");

        var x = -10000f;
        var y = 10000f;
        minimum.ClampDragOffset(ref x, ref y);
        Equal(
            minimum.MaximumDragOffsetX < minimum.MinimumDragOffsetX
                ? minimum.MaximumDragOffsetX
                : minimum.MinimumDragOffsetX,
            x,
            "whole-window drag clamp handles an aggregate wider than canvas");
        Equal(minimum.MaximumDragOffsetY, y, "top drag clamp");

        var maximum = MaterialEditorResponsiveLayoutPolicy.Calculate(
            1f,
            1f,
            1f,
            500f,
            MaterialEditorResponsiveSideState.Hidden,
            MaterialEditorResponsiveSideState.Collapsed);
        Equal(true, maximum.MainTopAnchor <= 0.95f, "top outer clamp");
        Near(
            1f * maximum.UiScale
            + maximum.MainLeftAnchor
            - MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction,
            maximum.MainRightAnchor,
            0.001f,
            "right anchor preserves historical configured-width semantics");

        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        Contains(window,
            "MaterialEditorResponsiveLayoutPolicy.GetRightSideState(",
            "Rename and selection resolve the right reservation centrally");
        Contains(window,
            "+ _responsiveLayout.RightPanelWidth",
            "Rename uses responsive side width");
        Contains(window,
            "_responsiveLayout.ClampDragOffset(\n"
            + "                    GetWindowDragMode(),",
            "configured bounds policy clamps preserved drag offset");
        Contains(window,
            "MainPanel.rectTransform,\n                false);",
            "UILib dragout clamp is disabled in favor of one policy");
        Equal(1,
            CountOccurrences(window, "OnDragEvent += ClampResponsiveDrag;"),
            "aggregate drag clamp has one permanent event subscription");
        Contains(window,
            "internal void ClampResponsiveDragPosition()",
            "live drag path applies the pure aggregate clamp");
        Contains(window,
            "AddComponent<MaterialEditorResponsiveCanvasWatcher>()",
            "Canvas root owns the event-driven aspect watcher");
        Equal(true,
            minimum.HeaderDragBounds.MinimumX
            <= minimum.HeaderDragBounds.MaximumX,
            "recoverable header has an ordered horizontal range");
        Equal(true,
            minimum.HeaderDragBounds.MinimumY
            <= minimum.HeaderDragBounds.MaximumY,
            "recoverable header has an ordered vertical range");
        Near(
            MaterialEditorTheme.Metrics.WindowHeaderRecoveryWidth
            - minimum.MainRightAnchor * minimum.CanvasWidth,
            minimum.HeaderDragBounds.MinimumX,
            0.001f,
            "left overflow retains a draggable header span beyond chrome");
        Near(
            minimum.CanvasWidth
            - MaterialEditorTheme.Metrics.WindowHeaderRecoveryWidth
            - minimum.MainLeftAnchor * minimum.CanvasWidth,
            minimum.HeaderDragBounds.MaximumX,
            0.001f,
            "right overflow retains a draggable header span beyond chrome");
    }

    private static void CachePolicyIsDerivedBoundedAndOverscanned()
    {
        var maximumMainHeight =
            MaterialEditorTheme.Metrics.CanvasReferenceHeight
            * (1f - 2f
                * MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction);
        var maximumViewportHeight =
            maximumMainHeight
            - MaterialEditorTheme.Metrics.TopBarHeight
            - MaterialEditorTheme.Metrics.Margin * 1.5f;
        var expectedMaximum =
            (int)Math.Ceiling(maximumViewportHeight / RowHeight) + 1;
        Equal(expectedMaximum,
            VirtualListCachePolicy.MaximumViewCount,
            "cache cap derives from responsive maximum viewport");
        Equal(4,
            VirtualListCachePolicy.RequiredViewCount(
                3f * RowHeight,
                RowHeight,
                100),
            "fractional-scroll overscan slot");
        Equal(2,
            VirtualListCachePolicy.RequiredViewCount(
                20f * RowHeight,
                RowHeight,
                2),
            "model count bounds pool");
        Equal(expectedMaximum,
            VirtualListCachePolicy.RequiredViewCount(
                100000f,
                RowHeight,
                int.MaxValue),
            "extreme viewport remains bounded");
        Equal(0,
            VirtualListCachePolicy.RequiredViewCount(
                float.NaN,
                RowHeight,
                100),
            "invalid viewport creates no rows");
    }

    private static void VirtualListGrowsAndShrinksActiveCapacityWithoutLeakingBindings()
    {
        RectTransform viewport;
        var list = CreateInitializedVirtualList(RowHeight, out viewport);
        var rows = CreateRows(100);
        list.SetList(rows, false);
        InvokeUpdate(list);
        Equal(2, list.ActiveViewCapacity, "initial active capacity + overscan");
        Equal(2, list.CachedViewCount, "template is not an active pooled row");

        var highWater = list.CachedViewCount;
        for (var iteration = 0; iteration < 100; iteration++)
        {
            var rowCount = 1 + iteration % 44;
            var height = rowCount * RowHeight - (iteration % 2 == 0 ? 0f : 0.5f);
            viewport.rect = new Rect(0f, 0f, 300f, height);
            if (iteration % 10 == 0)
                list.SetList(rows, false);
            InvokeUpdate(list);

            Equal(true,
                list.CachedViewCount >= highWater,
                "pool only grows at iteration " + iteration);
            highWater = list.CachedViewCount;
            Equal(true,
                highWater <= VirtualListCachePolicy.MaximumViewCount,
                "bounded high-water at iteration " + iteration);
            Equal(
                VirtualListCachePolicy.RequiredViewCount(
                    height,
                    RowHeight,
                    rows.Count),
                list.ActiveViewCapacity,
                "active capacity at iteration " + iteration);
        }

        var cache = CachedViews(list);
        viewport.rect = new Rect(0f, 0f, 300f, 11f * RowHeight);
        InvokeUpdate(list);
        Equal(12, list.ActiveViewCapacity, "shrunken active capacity");
        Equal(highWater, list.CachedViewCount, "shrink retains high-water pool");
        for (var index = 0; index < cache.Count; index++)
        {
            var active = index < list.ActiveViewCapacity;
            Equal(active, cache[index].CurrentModel != null,
                "model binding matches capacity " + index);
            Equal(active, cache[index].ListenersActive,
                "listener binding matches capacity " + index);
            Equal(active, cache[index].Visible,
                "visibility matches capacity " + index);
        }

        var bindsBefore = cache.Sum(view => view.BindCount);
        var poolBefore = list.CachedViewCount;
        LayoutRebuilder.Reset();
        for (var frame = 0; frame < 100; frame++)
            InvokeUpdate(list);
        Equal(poolBefore, list.CachedViewCount, "idle frames never grow pool");
        Equal(bindsBefore, cache.Sum(view => view.BindCount),
            "idle frames never rebind rows");
        Equal(0, LayoutRebuilder.MarkCount,
            "idle frames perform no layout work");

        list.ReleaseContent();
        Equal(true,
            cache.All(view => view.CurrentModel == null
                              && !view.ListenersActive
                              && !view.Visible),
            "release clears every high-water row");
    }

    private static void ResponsiveIntegrationAddsNoPollingPersistenceOrSplitter()
    {
        var policy = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.ResponsiveLayout.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var category = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");
        var list = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.VirtualList.cs");

        Contains(window,
            "VirtualList?.EnsureViewportCapacity(",
            "settings event grows pool explicitly");
        Contains(window,
            "_responsiveLayout.ViewportHeight);",
            "settings capacity uses the responsive viewport");
        Contains(list,
            "EnsureViewportCapacity(GetViewportHeight());",
            "new list grows against current viewport");
        Contains(list,
            "if (viewportHeight != _lastViewportHeight)\n                EnsureViewportCapacity(viewportHeight);",
            "existing viewport loop grows capacity after a real resize");
        Contains(policy,
            "private void OnRectTransformDimensionsChange()",
            "Canvas dimension changes are event-driven");
        DoesNotContain(list,
            "Destroy(EntryTemplate);",
            "clean template destruction before future growth");
        Contains(list,
            "entry.Release();",
            "new pooled row starts unbound");
        Contains(list,
            "_models.Count - _activeViewCapacity",
            "scroll math uses active capacity");
        Contains(list,
            "activeViewCapacity * PanelHeight",
            "padding math ignores inactive high-water tail");
        Equal(1,
            CountOccurrences(list, "private void Update()"),
            "only the pre-existing VirtualList update loop");

        foreach (var forbidden in new[]
                 {
                     "void Update()",
                     "void LateUpdate()",
                     "ConfigEntry",
                     "PlayerPrefs",
                     "Splitter",
                     "IDragHandler",
                     "SetMaterial",
                     "StartCoroutine"
                 })
        {
            DoesNotContain(policy + window + category,
                forbidden,
                forbidden + " responsive behavior");
        }
    }

    private static VirtualList CreateInitializedVirtualList(
        float viewportHeight,
        out RectTransform viewport)
    {
        var contentObject = new GameObject();
        var content = contentObject.AddComponent<RectTransform>();
        content.rect = new Rect(0f, 0f, 300f, 4000f);
        contentObject.AddComponent<VerticalLayoutGroup>();

        var viewportObject = new GameObject();
        viewport = viewportObject.AddComponent<RectTransform>();
        viewport.rect = new Rect(0f, 0f, 300f, viewportHeight);

        var scrollObject = new GameObject();
        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;

        var template = new GameObject();
        template.transform.parent = content;

        var listObject = new GameObject();
        var list = listObject.AddComponent<VirtualList>();
        list.ScrollRect = scrollRect;
        list.EntryTemplate = template;
        list.Initialize();
        return list;
    }

    private static List<RowModel> CreateRows(int count)
    {
        var rows = new List<RowModel>(count);
        for (var index = 0; index < count; index++)
            rows.Add(new TestPropertyRowModel(
                RowModel.RowItemType.FloatProperty,
                "Property " + index,
                "Property" + index));
        return rows;
    }

    private static List<RowView> CachedViews(VirtualList list)
    {
        return GetField<List<RowView>>(list, "_cachedViews");
    }

    private static T GetField<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("Field not found: " + name);
        return (T)field.GetValue(instance);
    }

    private static void InvokeUpdate(VirtualList list)
    {
        var update = typeof(VirtualList).GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (update == null)
            throw new InvalidOperationException("VirtualList.Update not found.");
        update.Invoke(list, null);
    }

    private static string ReadSource(params string[] segments)
    {
        return File.ReadAllText(
                Path.Combine(new[] { FindRepositoryRoot() }.Concat(segments).ToArray()))
            .Replace("\r\n", "\n");
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[]
                 {
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory
                 })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(
                        directory.FullName,
                        "tests",
                        "MaterialEditor.MetadataTests",
                        "MaterialEditor.MetadataTests.csproj")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        var endIndex = source.IndexOf(
            end,
            startIndex + start.Length,
            StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("Could not isolate source contract.");
        return source.Substring(startIndex, endIndex - startIndex);
    }

    private static void AssertSameCentralRect(
        MaterialEditorResponsiveLayout expected,
        MaterialEditorResponsiveLayout actual,
        string name)
    {
        Equal(expected.MainLeftAnchor, actual.MainLeftAnchor, name + " left");
        Equal(expected.MainBottomAnchor, actual.MainBottomAnchor, name + " bottom");
        Equal(expected.MainRightAnchor, actual.MainRightAnchor, name + " right");
        Equal(expected.MainTopAnchor, actual.MainTopAnchor, name + " top");
        Equal(expected.MainWidth, actual.MainWidth, name + " width");
        Equal(expected.MainHeight, actual.MainHeight, name + " height");
        Equal(expected.ViewportHeight, actual.ViewportHeight, name + " viewport");
        Equal(expected.RightPanelWidth, actual.RightPanelWidth, name + " right reservation");
        Equal(expected.MinimumDragOffsetY, actual.MinimumDragOffsetY, name + " minimum drag Y");
        Equal(expected.MaximumDragOffsetY, actual.MaximumDragOffsetY, name + " maximum drag Y");
    }

    private static void Finite(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new InvalidOperationException(name + " is not finite.");
    }

    private static void Contains(string source, string value, string name)
    {
        if (!source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " missing.");
    }

    private static void DoesNotContain(string source, string value, string name)
    {
        if (source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " present.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }

    private static void Near(
        float expected,
        float actual,
        float tolerance,
        string name)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
