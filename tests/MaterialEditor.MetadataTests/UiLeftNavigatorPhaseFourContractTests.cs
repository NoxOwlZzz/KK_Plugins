using MaterialEditorAPI;

internal static class UiLeftNavigatorPhaseFourContractTests
{
    internal static void Run()
    {
        ExpandedPanelAndClosedOverlayAreMutuallyExclusive();
        CategoryRowsAreLegibleAndAccessibleWithoutColorAlone();
        EntriesUseHighWaterRebindingAndPermanentListeners();
        CategoryCallbacksRemainSingleAndMaterialReadOnly();
        CategoryDiagnosticsAreEventDrivenAndDisabledByDefault();
        NavigatorKeepsItsExistingReadOnlyBoundaries();
        Console.WriteLine("Phase 4 left-navigator regression guards passed.");
    }

    private static void ExpandedPanelAndClosedOverlayAreMutuallyExclusive()
    {
        Equal(24f,
            MaterialEditorTheme.Metrics.CategoryNavigatorCollapsedWidth,
            "collapsed rail width");
        var navigator = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");

        foreach (var stableName in new[]
                 {
                     "CategoryNavigatorHeader",
                     "CategoryNavigatorTitle",
                     "CategoryNavigatorPanelCollapse",
                     "CategoryNavigatorExpand"
                 })
            Contains(navigator, "\"" + stableName + "\"", stableName);

        Contains(navigator,
            "private bool _expanded;",
            "navigator starts closed without a second visibility preference");
        Contains(navigator,
            "Panel.gameObject.SetActive(_expanded);",
            "expanded panel follows only the user preference");
        Contains(navigator,
            "_expandButton.gameObject.SetActive(!_expanded);",
            "closed navigator shows only its edge overlay");
        DoesNotContain(navigator,
            "CategoryNavigatorCollapsedRail",
            "closed navigator has no full-height rail surface");
        DoesNotContain(navigator,
            "_expanded && _hasCategories",
            "category availability cannot override the user's preference");
        Contains(navigator,
            "MaterialEditorTheme.Glyphs.ChevronLeft",
            "open left panel hides toward the left");
        Contains(navigator,
            "MaterialEditorTheme.Glyphs.ChevronRight",
            "closed left overlay opens toward the right");
        Contains(navigator,
            "_expandedChanged?.Invoke(_expanded);",
            "single expansion-state notification");
        Equal(1,
            CountOccurrences(navigator, "_expandedChanged?.Invoke(_expanded);"),
            "one category expansion-state publication site");
        Contains(window,
            "toggleCategory,",
            "existing category callback remains wired");
        Contains(window,
            "SetCategoryNavigatorGlyph);",
            "topbar expansion-state integration");
        Contains(window,
            "CategoryNavigator.ToggleExpanded();",
            "global-menu toggle uses the same navigator state");
        var glyphUpdate = Slice(
            window,
            "private void SetCategoryNavigatorGlyph(bool expanded)",
            "private void BuildSelectionPanels(");
        Equal(1,
            CountOccurrences(glyphUpdate, "ApplySettings();"),
            "category fold applies one stable layout transition");
    }

    private static void CategoryRowsAreLegibleAndAccessibleWithoutColorAlone()
    {
        Equal(3f,
            MaterialEditorTheme.Metrics.CategoryActiveMarkerWidth,
            "active marker width");
        var navigator = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");
        var style = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");

        Contains(navigator, "\"Categories\"", "clear panel title");
        Contains(navigator,
            "MaterialEditorPanelRole.TransparentRow",
            "neutral category-row surface");
        Contains(navigator,
            "entry.ActiveMarker.enabled = active;",
            "selected accent marker without layout shift");
        Contains(navigator,
            "activeMarker.raycastTarget = false;",
            "accent marker cannot block category interaction");
        Contains(navigator,
            "root.raycastTarget = true;",
            "entry background is an explicit category hit surface");
        Contains(navigator,
            "rootButton.onClick.AddListener(binding.InvokeNavigate);",
            "marker, padding, and background navigate through one target snapshot");
        Contains(navigator,
            "collapseText.raycastTarget = false;",
            "collapse glyph delegates to its button hitbox");
        Contains(navigator,
            "navigateText.raycastTarget = false;",
            "category text delegates to its button hitbox");
        Contains(navigator,
            "FontStyle.Bold : FontStyle.Normal",
            "selected text weight");
        Contains(style,
            "SetCategoryNavigationSelected(",
            "selected navigation surface");
        Contains(style,
            "MaterialEditorTheme.Colors.Selected",
            "selected semantic color");
        Contains(navigator,
            "text.verticalOverflow = VerticalWrapMode.Truncate;",
            "fixed-height long-name truncation");
        Contains(navigator,
            "target.TooltipText,",
            "optional category metadata tooltip");
        Contains(navigator,
            "target.Name);",
            "full category name standard-tooltip fallback");
        Contains(navigator,
            "rootLayout.preferredHeight = MaterialEditorLayout.RowHeight;",
            "fixed category-row height");
        Contains(navigator,
            "MaterialEditorControlFactory.CreateScrollView(",
            "shared themed scroll style");
        Contains(navigator,
            "_scrollRect.horizontal = false;",
            "category list is vertical only");
        Contains(navigator,
            "&& _activeEntry.Target.Id == active.Id)",
            "same-category anchors do not restyle every entry");
        Contains(navigator,
            "_deferredPresentationRebuild = true;",
            "atomic presentation swap records a pending entry rebind");
        Contains(navigator,
            "var forceRebuild = _deferredPresentationRebuild;",
            "the next published anchor consumes the pending entry rebind");
    }

    private static void NavigatorKeepsItsExistingReadOnlyBoundaries()
    {
        var navigator = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");

        Contains(navigator,
            "if (hadSection)",
            "hidden empty context releases the previous section entries");
        Equal(1,
            CountOccurrences(navigator, "LayoutRebuilder.ForceRebuildLayoutImmediate("),
            "layout rebuild remains section-rebuild only");

        foreach (var forbidden in new[]
                 {
                     "ConfigEntry",
                     "SetMaterial",
                     "ModifiedCount",
                     "WarningBadge",
                     "MasterToggle",
                     "Update()",
                     "LateUpdate()"
                 })
            DoesNotContain(navigator, forbidden, forbidden + " forbidden navigator behavior");
    }

    private static void EntriesUseHighWaterRebindingAndPermanentListeners()
    {
        var navigator = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");

        Contains(navigator,
            "while (_entries.Count < section.Categories.Count)",
            "high-water entry growth");
        Equal(1,
            CountOccurrences(navigator, "_entries.Add(CreateEntry());"),
            "one high-water growth site");
        Equal(1,
            CountOccurrences(navigator,
                "collapse.onClick.AddListener(binding.InvokeToggle);"),
            "one structural collapse-listener installation site");
        Equal(1,
            CountOccurrences(navigator,
                "navigate.onClick.AddListener(binding.InvokeNavigate);"),
            "one structural navigation-listener installation site");
        Equal(1,
            CountOccurrences(navigator,
                "rootButton.onClick.AddListener(binding.InvokeNavigate);"),
            "one structural background-listener installation site");
        Contains(navigator,
            "entry.Binding.Bind(null);",
            "released pooled entry clears target");
        Contains(navigator,
            "entry.Target == null || entry.Target.Id != active.Id",
            "inactive pool tail is safe during highlight lookup");
        Contains(navigator,
            "entry.Root.gameObject.SetActive(false);",
            "released pooled entry is hidden");
        Contains(navigator,
            "BindEntry(entry, null);",
            "release path unbinds retained entries");
        DoesNotContain(navigator,
            "RemoveAllListeners",
            "rebind must not mutate listener registration");
        DoesNotContain(navigator,
            "UnityEngine.Object.Destroy(entry.Root.gameObject)",
            "rebind must not destroy pooled rows");
        DoesNotContain(navigator,
            "_entries.Clear();",
            "release retains high-water capacity");
    }

    private static void CategoryCallbacksRemainSingleAndMaterialReadOnly()
    {
        var ui = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.cs");
        var navigate = Slice(
            ui,
            "private void NavigateToCategory(",
            "private void ToggleCategory(");
        var toggle = Slice(
            ui,
            "private void ToggleCategory(",
            "private void SetRendererCollapsed(");
        var rebuild = Slice(
            ui,
            "private void RebuildAndScrollToCategory(",
            "/// <summary>").Replace("\r\n", "\n");

        Contains(navigate,
            "if (target.EnsureParentsExpanded())",
            "navigation rebuilds only when a parent actually expands");
        Contains(navigate,
            "VirtualList.ScrollToIndex(target.RowIndex);",
            "already-expanded navigation is a targeted scroll only");
        Equal(1,
            CountOccurrences(navigate, "RebuildAndScrollToCategory("),
            "parent expansion has one synchronous targeted rebuild");
        Equal(0,
            CountOccurrences(navigate, "PopulateList("),
            "already-expanded navigation never rebuilds directly");
        Equal(0,
            CountOccurrences(navigate, "StartCoroutine("),
            "category navigation does not expose an intermediate frame");
        Equal(1,
            CountOccurrences(toggle, "target.SetCollapsed(!target.Collapsed);"),
            "one category-collapse callback");
        Equal(1,
            CountOccurrences(toggle, "RebuildAndScrollToCategory("),
            "one collapse presentation rebuild and targeted scroll");
        Equal(1,
            CountOccurrences(rebuild, "PopulateListCore("),
            "targeted rebuild installs one presentation");
        Contains(rebuild,
            "false,\n                false);",
            "targeted rebuild suppresses the restored-anchor publication");
        Equal(1,
            CountOccurrences(rebuild, "VirtualList.ScrollToIndex("),
            "resolved target publishes its exact programmatic anchor once");
        Equal(1,
            CountOccurrences(rebuild, "VirtualList.PublishViewportAnchor();"),
            "missing targets publish only the final fallback anchor");
        Equal(0,
            CountOccurrences(rebuild, "StartCoroutine("),
            "targeted rebuild is atomic without a one-frame coroutine");

        foreach (var callback in new[] { navigate, toggle })
        foreach (var forbidden in new[]
                 {
                     "MaterialEditService",
                     "Repository",
                     "SetMaterial",
                     "ConfigEntry"
                 })
            DoesNotContain(
                callback,
                forbidden,
                    forbidden + " forbidden category callback write");
    }

    private static void CategoryDiagnosticsAreEventDrivenAndDisabledByDefault()
    {
        var diagnostics = ReadSource(
            "src", "MaterialEditor.Base", "UI",
            "UI.CategoryInteractionDiagnostics.cs");
        var navigator = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");

        Contains(diagnostics,
            "var setting = MaterialEditorPluginBase.PerformanceDiagnostics;",
            "diagnostics reuse the existing disabled-by-default switch");
        Contains(diagnostics,
            "if (!Enabled)",
            "diagnostic formatting is behind the first branch");
        foreach (var field in new[]
                 {
                     "pointer=(",
                     "visualRowIndex=",
                     "navigatorEntryIndex=",
                     "stableCategoryKey=",
                     "listenerKey=",
                     "clickedStableKey=",
                     "navigationKey=",
                     "highlightKey=",
                     "rect=(",
                     "centralScroll=",
                     "scrollOrigin="
                 })
            Contains(diagnostics, field, "diagnostic field " + field);
        DoesNotContain(diagnostics, "Update()", "no continuous diagnostic loop");
        DoesNotContain(diagnostics, "LateUpdate()", "no late diagnostic loop");
        Contains(navigator,
            "LogEntryDiagnostic(\"navigate\"",
            "navigation click diagnostic is event-driven");
        Contains(navigator,
            "LogHighlightDiagnostic(",
            "highlight diagnostic is event-driven");
    }

    private static string ReadSource(params string[] segments)
    {
        return File.ReadAllText(
            Path.Combine(new[] { FindRepositoryRoot() }.Concat(segments).ToArray()));
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
        var endIndex = source.IndexOf(end, startIndex + start.Length,
            StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("Could not isolate callback source.");
        return source.Substring(startIndex, endIndex - startIndex);
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
}
