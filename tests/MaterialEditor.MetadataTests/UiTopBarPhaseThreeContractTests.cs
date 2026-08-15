using MaterialEditorAPI;

internal static class UiTopBarPhaseThreeContractTests
{
    internal static void Run()
    {
        ShaderContextUsesOnlyPresentationShaderNames();
        TopBarKeepsStableHierarchyAndHeight();
        GlobalMenuIsOneReusableViewOnlyPopup();
        SearchKeepsOneImmediateCallback();
        StudioDropdownUsesTheExplicitLifetimeSafeSlot();
        Console.WriteLine("Phase 3 topbar regression guards passed.");
    }

    private static void ShaderContextUsesOnlyPresentationShaderNames()
    {
        AssertShaderContext(
            MaterialEditorShaderContextKind.None,
            string.Empty);
        AssertShaderContext(
            MaterialEditorShaderContextKind.None,
            string.Empty,
            string.Empty,
            string.Empty);
        AssertShaderContext(
            MaterialEditorShaderContextKind.Single,
            "KKLT/Opaque",
            "KKLT/Opaque");
        AssertShaderContext(
            MaterialEditorShaderContextKind.Single,
            "KKLT/Opaque",
            "KKLT/Opaque",
            "KKLT/Opaque");
        AssertShaderContext(
            MaterialEditorShaderContextKind.Multiple,
            string.Empty,
            "A",
            "B");
        AssertShaderContext(
            MaterialEditorShaderContextKind.Multiple,
            string.Empty,
            string.Empty,
            "A");
    }

    private static void TopBarKeepsStableHierarchyAndHeight()
    {
        var topBar = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.TopBarView.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var ui = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.cs");
        var projectItems = ReadSource(
            "src", "MaterialEditor.Base", "MaterialEditor.Base.projitems");

        foreach (var name in new[]
                 {
                     "Draggable",
                     "Nametext",
                     "MaterialEditorShaderContext",
                     "MaterialEditorStudioContextSlot",
                     "MaterialEditorGlobalMenuButton",
                     "CloseButton",
                     "MaterialEditorModePanel",
                     "Filter",
                     "PersistSearch"
                 })
            Contains(topBar, "\"" + name + "\"", name + " stable object name");

        Contains(window,
            "HeaderPanel = _topBar.HeaderPanel;",
            "WindowView retains the Draggable header contract");
        Contains(window,
            "UIUtility.MakeObjectDraggable(",
            "Draggable remains the main-panel drag handle");
        Contains(ui,
            "DragPanel = _windowView.HeaderPanel;",
            "legacy DragPanel still points at Draggable");
        Contains(topBar,
            "-MaterialEditorTheme.Metrics.TopBarHeight",
            "the two topbar rows use the fixed 40px theme height");
        Contains(window,
            "-MaterialEditorTheme.Metrics.TopBarHeight",
            "the viewport reserves exactly the fixed topbar height");
        Contains(topBar,
            "MaterialEditorTheme.Spacing.TopBarHorizontalInset",
            "search and Persist use the shared outer gutter");
        Contains(topBar,
            "ModePanel.transform,\n                \"Persist\"",
            "Persist label belongs to the Toggle hit target");
        DoesNotContain(topBar,
            "\"PersistSearchText\"",
            "Persist has no detached sibling label");
        Equal(1,
            CountOccurrences(topBar,
                "PersistSearchToggle.onValueChanged.AddListener("),
            "Persist has one edit listener");

        foreach (var file in new[]
                 {
                     "UI\\UI.TopBarState.cs",
                     "UI\\UI.TopBarView.cs",
                     "UI\\UI.PopupMenu.cs"
                 })
            Contains(projectItems, file, file + " production include");
    }

    private static void GlobalMenuIsOneReusableViewOnlyPopup()
    {
        var popup = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.PopupMenu.cs");
        var topBar = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.TopBarView.cs");
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.cs");
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.cs");

        Equal(1,
            CountOccurrences(popup, "\"MaterialEditorGlobalMenu\""),
            "one reusable global-menu host");
        Contains(popup, "Collapse all categories", "collapse-all view action");
        Contains(popup,
            "Collapse all renderer/material sections",
            "renderer/material section view action");
        Contains(popup,
            "Expand all renderer/material sections",
            "renderer/material section inverse action");
        Contains(topBar,
            "presentation.CanToggleSections",
            "property search disables renderer/material section action");
        Contains(topBar,
            "internal void RefreshSectionCollapseState(",
            "targeted folds can refresh only global section state");
        Contains(popup, "Show categories", "category-navigator view action");
        Contains(popup,
            "Show/hide Renderers and Materials",
            "selection-panel view action");
        Contains(popup, "MaterialEditorGlobalMenuDismissLayer", "outside-click blocker");
        Contains(popup, "SetAsLastSibling();", "popup raises one reusable surface");
        Contains(popup, "Input.GetKeyDown(KeyCode.Escape)", "Escape dismisses popup");
        Contains(popup, "enabled = false;", "closed popup has no frame polling");
        Contains(popup, "enabled = true;", "open popup enables only Escape polling");
        Contains(topBar, "_globalMenu.Close();", "presentation/close paths dismiss popup");

        foreach (var forbidden in new[]
                 {
                     "MaterialEditService",
                     "RowModel",
                     "Reset",
                     "Copy",
                     "Paste",
                     "Import",
                     "Export",
                     "Recalculate",
                     "Technical"
                 })
            DoesNotContain(popup, forbidden, forbidden + " forbidden global action");

        DoesNotContain(factory, "MaterialEditorGlobalMenu", "row factory owns no popup");
        DoesNotContain(binder, "MaterialEditorGlobalMenu", "row binder owns no popup");
    }

    private static void SearchKeepsOneImmediateCallback()
    {
        var topBar = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.TopBarView.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");

        Equal(1,
            CountOccurrences(topBar + window,
                "FilterInputField.onValueChanged.AddListener("),
            "one immediate search listener");
        Contains(topBar,
            "value => filterChanged(value)",
            "search delegates to the existing coalesced callback");
        Contains(topBar,
            "FilterInputField.Set(filter);",
            "display refresh uses the existing non-notifying setter");
        DoesNotContain(topBar, "PopulateList(", "topbar owns no rebuild backend");
        DoesNotContain(topBar, "StartCoroutine(", "topbar owns no debounce worker");
    }

    private static void StudioDropdownUsesTheExplicitLifetimeSafeSlot()
    {
        var studio = ReadSource(
            "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.Studio.cs");
        var ui = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.cs");

        Contains(studio,
            "HeaderContextSlot);",
            "Studio ItemType dropdown is created in the explicit slot");
        Contains(studio,
            "SetItemTypeDropdownVisible(false);",
            "item/release paths hide dropdown and slot");
        Contains(studio,
            "SetItemTypeDropdownVisible(true);",
            "character path shows dropdown and slot");
        Contains(studio,
            "dropdown.onValueChanged.RemoveAllListeners();",
            "target replacement clears captured listeners");
        Contains(studio,
            "dropdown.options.Clear();",
            "target release clears option context");
        Contains(studio,
            "_itemTypeTarget = null;",
            "target release clears the retained character");
        Contains(ui,
            "private protected Transform HeaderContextSlot",
            "base UI exposes only the named view slot to Studio");
        DoesNotContain(studio, "DragPanel.transform", "no magic DragPanel injection");
        DoesNotContain(studio, "CharacterHeaderTitleOffset", "no title compensation");
        DoesNotContain(studio, "-242f", "no dropdown left magic offset");
        DoesNotContain(studio, "-61f", "no dropdown right magic offset");
    }

    private static void AssertShaderContext(
        MaterialEditorShaderContextKind expectedKind,
        string expectedShaderName,
        params string[] shaderNames)
    {
        var accumulator = new MaterialEditorShaderContextAccumulator();
        foreach (var shaderName in shaderNames)
            accumulator.Add(shaderName);
        string resolvedName;
        var resolvedKind = accumulator.Resolve(out resolvedName);
        Equal(expectedKind, resolvedKind, "shader context kind");
        Equal(expectedShaderName, resolvedName, "shader context name");
    }

    private static string ReadSource(params string[] segments) =>
        File.ReadAllText(Path.Combine(
            new[] { FindRepositoryRoot() }.Concat(segments).ToArray()));

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
                name + ": expected " + expected + ", actual " + actual + ".");
    }
}
