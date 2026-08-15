using MaterialEditorAPI;

internal static class UiRightPanelsPhaseFiveContractTests
{
    internal static void Run()
    {
        SelectionListsAreClosedByDefaultAndInitializedOnce();
        SelectionPanelsExposeRealCountsFiltersAndSelectionState();
        IndividualAndGlobalCollapseRemainViewOnly();
        RenameOverlayPreservesTheGlobalListPreference();
        EntryFilteringAndReleaseStayLinearAndLeakFree();
        Console.WriteLine("Phase 5 right-panel regression guards passed.");
    }

    private static void SelectionListsAreClosedByDefaultAndInitializedOnce()
    {
        var session = new MaterialEditorSessionState();
        Equal(false, session.ListsVisible, "selection lists default closed");
        var renderer = new UnityEngine.Renderer();
        var material = new UnityEngine.Material();
        session.SelectedRenderers.Add(renderer);
        session.SelectedMaterials.Add(material);
        session.ListsVisible = true;
        Equal(renderer, session.SelectedRenderers[0],
            "horizontal preference change preserves renderer selection");
        Equal(material, session.SelectedMaterials[0],
            "horizontal preference change preserves material selection");
        session.ClearTargetReferences();
        Equal(true, session.ListsVisible,
            "target release preserves the user's global view preference");

        var ui = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.cs");
        Equal(1,
            CountOccurrences(ui, "_selectionController.InitializeViewState();"),
            "one initial selection-panel state application");

        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var visibility = Slice(
            window,
            "private void UpdateRightPanelVisibility()",
            "private MaterialEditorResponsiveLayout CalculateResponsiveLayout()");
        Contains(visibility,
            "RendererList.ToggleVisibility(showSelectionLists);",
            "renderer list shares the visible selection surface");
        Contains(visibility,
            "MaterialList.ToggleVisibility(showSelectionLists);",
            "material list shares the visible selection surface");
        Contains(visibility,
            "_selectionListsVisible && !_renameListVisible",
            "rename is a temporary overlay over the list preference");
        Contains(visibility,
            "_rightPanelToggleButton.gameObject.SetActive(true);",
            "one edge button remains available in every right-panel state");
        DoesNotContain(window,
            "MaterialEditorSelectionListsCollapsedRail",
            "closed right panel has no full-height rail surface");

        var controller = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectionController.cs");
        var initialization = Slice(
            controller,
            "internal void InitializeViewState()",
            "internal void ToggleSidePanels()");
        Equal(1,
            CountOccurrences(initialization, "ApplyRightPanelState();"),
            "initialization applies both right-panel flags once");
        var stateBridge = Slice(
            controller,
            "private void ApplyRightPanelState()",
            "private void PopulateRenameList(");
        Contains(stateBridge,
            "_session.ListsVisible,",
            "combined state uses the stored global preference");
        Contains(stateBridge,
            "_session.RenameListVisible);",
            "combined state uses the rename overlay flag");
        Equal(1,
            CountOccurrences(stateBridge, "_view.SetRightPanelState("),
            "controller has one right-panel presentation bridge");

        var globalToggle = Slice(
            controller,
            "internal void ToggleSidePanels()",
            "internal void HideSidePanels()");
        var globalView = Slice(
            window,
            "internal void SetRightPanelState(",
            "internal void SetHeaderTitleHorizontalOffset(");
        Contains(globalView,
            "if (_rightPanelStateInitialized",
            "unchanged right-panel state has a no-op guard");
        Contains(globalView,
            "&& _selectionListsVisible == selectionListsVisible",
            "no-op guard compares global-list visibility");
        Contains(globalView,
            "&& _renameListVisible == renameListVisible)",
            "no-op guard compares rename visibility");
        Equal(true,
            globalView.IndexOf("return;", StringComparison.Ordinal)
            < globalView.IndexOf(
                "UpdateRightPanelVisibility();",
                StringComparison.Ordinal),
            "no-op exits before any visibility or layout work");
        Equal(1,
            CountOccurrences(globalView, "UpdateRightPanelVisibility();"),
            "changed state updates visibility once");
        Equal(1,
            CountOccurrences(globalView, "ApplySettings();"),
            "changed state applies responsive settings exactly once");
        Equal(1,
            CountOccurrences(globalView,
                "MaterialEditorPerformanceMetric.LayoutInvalidations"),
            "changed state records one production layout invalidation");
        DoesNotContain(window,
            "SetSelectionListsVisible(",
            "split global-list state setter");
        DoesNotContain(window,
            "SetRenameListVisible(",
            "split rename state setter");

        foreach (var callback in new[] { globalToggle, stateBridge, globalView })
        foreach (var forbidden in new[]
                 {
                     "_refresh",
                     "_editService",
                     "MaterialEditService",
                     "ClearList",
                     "ReleaseEntries",
                     "ClearSelections",
                     "SelectedRenderers",
                     "SelectedMaterials",
                     "FilterInputField",
                     "ScrollRect",
                     "SetMaterial"
                 })
            DoesNotContain(callback, forbidden,
                forbidden + " forbidden global-collapse mutation");
    }

    private static void SelectionPanelsExposeRealCountsFiltersAndSelectionState()
    {
        Equal(24f,
            MaterialEditorTheme.Metrics.SelectionPanelCollapsedWidth,
            "closed edge-overlay width");
        Equal(MaterialEditorTheme.Metrics.HeaderHeight,
            MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight,
            "compact selection header height");
        var panel = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectListPanel.cs");
        var style = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");

        Contains(panel,
            "_titleText.text = _title + \" · \" + _listItems.Count;",
            "header count comes from populated entries");
        Contains(panel,
            "_filterInputField.onValueChanged.AddListener(FilterList);",
            "each panel owns its filter input");
        Contains(panel,
            "_filterPattern = MaterialEditorFilter.Prepare(filter);",
            "one cached filter pattern per filter change");
        Contains(panel,
            "toggle.Set(selected, false);",
            "restored selection does not invoke edit callbacks");
        Contains(panel,
            "MaterialEditorStyles.SetSelectionListSelected(",
            "selected row surface");
        Contains(panel,
            "FontStyle.Bold",
            "selected state is distinguishable without color alone");
        Contains(style,
            "MaterialEditorTheme.States.SelectedSurface",
            "semantic selected-row surface token");
        Contains(panel,
            "TooltipBinding.Bind(rowButton.gameObject, null, name);",
            "full-name tooltip fallback");
        var entryLabel = Slice(
            panel,
            "private static void ConfigureEntryLabel(Text label)",
            "private static void ApplySelectedState(");
        Contains(entryLabel,
            "label.resizeTextForBestFit = false;",
            "selection rows keep the configured UI font size");
        Contains(entryLabel,
            "label.fontSize = MaterialEditorTheme.Typography.PrimaryFontSize;",
            "selection rows use the normal primary font size");
        DoesNotContain(entryLabel,
            "SelectionNameMinimumFontSize",
            "long names truncate instead of shrinking below the UI font");

        var applyRowIndex = panel.IndexOf(
            "MaterialEditorStyles.ApplyRow(contentList.gameObject);",
            StringComparison.Ordinal);
        var selectedIndex = panel.IndexOf(
            "ApplySelectedState(entry, selected);",
            StringComparison.Ordinal);
        Equal(true, applyRowIndex >= 0 && applyRowIndex < selectedIndex,
            "row typography precedes selected-label styling");
    }

    private static void IndividualAndGlobalCollapseRemainViewOnly()
    {
        var panel = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectListPanel.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");

        foreach (var source in new[] { panel, window })
        {
            DoesNotContain(source, "void Update()",
                "selection panels must not add an Update polling loop");
            DoesNotContain(source, "void LateUpdate()",
                "selection panels must not add a LateUpdate polling loop");
            DoesNotContain(source, "ConfigEntry",
                "selection-panel chrome must not add configuration persistence");
            DoesNotContain(source, "PlayerPrefs",
                "selection-panel chrome must not add local persistence");
        }

        var setExpanded = Slice(
            panel,
            "internal void SetExpanded(bool expanded)",
            "private void ConfigureSelectionChrome()");
        Contains(setExpanded,
            "ApplyExpandedVisual();",
            "individual collapse only updates panel presentation");
        Contains(setExpanded,
            "_expandedChanged?.Invoke(_expanded);",
            "layout receives one state notification");
        Equal(1,
            CountOccurrences(setExpanded, "_expandedChanged?.Invoke(_expanded);"),
            "individual fold publishes one layout-state notification");
        DoesNotContain(setExpanded,
            "ApplySettings",
            "individual vertical folds cannot resize the central workspace");
        foreach (var forbidden in new[]
                 {
                     "ClearList",
                     "ReleaseEntries",
                     "MaterialEditService",
                     "_refresh",
                     "SelectedRenderers",
                     "SelectedMaterials"
                 })
            DoesNotContain(setExpanded, forbidden,
                forbidden + " forbidden individual-collapse mutation");

        var build = Slice(
            window,
            "private void BuildSelectionPanels(",
            "private void BuildRenamePanel()");
        Contains(build,
            "\"MaterialEditorSelectionListsToggleHorizontal\"",
            "one stable horizontal toggle for every right-panel mode");
        Contains(build,
            "if (_selectionListsVisible || _renameListVisible)",
            "common chrome distinguishes open from closed presentation");
        Contains(build,
            "hideSidePanels();",
            "open and Rename states use the explicit hide callback");
        Contains(build,
            "toggleSidePanels();",
            "closed state uses the existing show callback");
        Equal(2,
            CountOccurrences(build, "expanded => ApplySelectionPanelLayout()"),
            "renderer and material folds only redistribute right-panel height");
        Contains(build,
            "MaterialEditorTheme.Glyphs.ChevronLeft",
            "closed right overlay opens toward the left");
        Contains(build,
            "-MaterialEditorTheme.Metrics.SelectionPanelHeaderHeight",
            "edge action occupies only its compact header");
        DoesNotContain(build,
            "MaterialEditorSelectionListsCollapsedRail",
            "closed state has no full-height rail");
        DoesNotContain(build,
            "UIUtility.AddOutlineToObject",
            "closed overlay adds no rail outline");
        foreach (var forbidden in new[]
                 {
                     "Splitter",
                     "IDragHandler",
                     "EventTrigger",
                     "MakeObjectDraggable"
                 })
            DoesNotContain(build, forbidden,
                forbidden + " forbidden selection-panel resize behavior");

        var toggleVisibility = Slice(
            panel,
            "public void ToggleVisibility(bool visible)",
            "internal void SetExpanded(bool expanded)");
        Contains(toggleVisibility,
            "Panel.gameObject.SetActive(visible);",
            "horizontal collapse only toggles the existing panel surface");
        foreach (var forbidden in new[]
                 {
                     "ClearList",
                     "ReleaseEntries",
                     "FilterList",
                     "SetExpanded",
                     "verticalNormalizedPosition",
                     "SelectedRenderers",
                     "SelectedMaterials"
                 })
            DoesNotContain(toggleVisibility, forbidden,
                forbidden + " forbidden visibility side effect");

        var layout = Slice(
            window,
            "private void ApplySelectionPanelLayout()",
            "private void UpdateRightPanelVisibility()");
        Contains(layout,
            "if (RendererList.Expanded && MaterialList.Expanded)",
            "simultaneous default layout");
        Contains(layout,
            "if (!RendererList.Expanded && MaterialList.Expanded)",
            "renderer-only collapse layout");
        Contains(layout,
            "if (RendererList.Expanded && !MaterialList.Expanded)",
            "material-only collapse layout");
        Contains(layout,
            "left, 0f, right, -headerHeight);",
            "collapsed renderer meets the expanded material panel without a gap");
        Contains(layout,
            "left, headerHeight, right, 0f);",
            "expanded renderer meets the collapsed material panel without a gap");
        DoesNotContain(layout,
            "headerHeight + gap",
            "single collapsed material header cannot add vertical separation");
        DoesNotContain(layout,
            "headerHeight - gap",
            "single collapsed renderer header cannot add vertical separation");
        Contains(layout,
            "left, -2f * headerHeight, right, -headerHeight",
            "two collapsed headers stack without a gap");
        DoesNotContain(layout,
            "SetMainRectWithMemory",
            "vertical fold layout cannot mutate the central rectangle");
        DoesNotContain(layout,
            "MainPanel",
            "vertical fold layout remains inside the right sidebar");
        DoesNotContain(setExpanded + layout,
            "LayoutRebuilder.ForceRebuildLayoutImmediate",
            "vertical folds add no forced full-layout pass");
    }

    private static void RenameOverlayPreservesTheGlobalListPreference()
    {
        var controller = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectionController.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");

        var showRename = Slice(
            controller,
            "internal void ShowRenamePanel(",
            "internal void PopulateRendererList(");
        Contains(showRename,
            "_session.RenameListVisible = true;",
            "rename overlay state");
        Contains(showRename,
            "ApplyRightPanelState();",
            "rename overlay presentation");
        DoesNotContain(showRename,
            "ListsVisible",
            "rename must not mutate the global list preference");

        var closeRename = Slice(
            controller,
            "internal void CloseRenamePanel()",
            "internal void ShowRenamePanel(");
        Contains(closeRename,
            "ApplyRightPanelState();",
            "closing rename restores the stored list presentation");
        DoesNotContain(closeRename,
            "ListsVisible",
            "closing rename must not guess the global list preference");

        var hideAll = Slice(
            controller,
            "internal void HideSidePanels()",
            "internal void CloseRenamePanel()");
        Contains(hideAll,
            "_session.ListsVisible = false;",
            "quick-hide closes the normal lists");
        Contains(hideAll,
            "_session.RenameListVisible = false;",
            "quick-hide also closes Rename");
        Equal(1,
            CountOccurrences(hideAll, "ApplyRightPanelState();"),
            "quick-hide publishes one combined right-panel transition");
        Contains(hideAll,
            "if (renameWasVisible)\n                ReleaseRenameContext();",
            "quick-hide releases transient Rename bindings");

        var visibility = Slice(
            window,
            "private void UpdateRightPanelVisibility()",
            "private MaterialEditorResponsiveLayout CalculateResponsiveLayout()");
        Contains(visibility,
            "var expanded = showSelectionLists || _renameListVisible;",
            "common chevron stays in its open state during Rename");
        Contains(visibility,
            "? MaterialEditorTheme.Glyphs.ChevronRight\n"
            + "                        : MaterialEditorTheme.Glyphs.ChevronLeft;",
            "right arrows are exact for open and closed states");

        var buildRename = Slice(
            window,
            "private void BuildRenamePanel()",
            "private void ApplySelectionPanelLayout()");
        Contains(buildRename,
            "new SelectListPanel(MainPanel.transform, \"MaterialRenameList\", \"Mat. Renderers\")",
            "rename keeps the legacy three-argument chrome");
        Contains(buildRename,
            "GetChild(0)",
            "legacy rename title hierarchy");
        Contains(buildRename,
            "GetChild(1)",
            "legacy rename filter hierarchy");
        Contains(buildRename,
            "GetChild(2)",
            "legacy rename list hierarchy");
    }

    private static void EntryFilteringAndReleaseStayLinearAndLeakFree()
    {
        var panel = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectListPanel.cs");
        var addEntry = Slice(
            panel,
            "internal void AddEntry(",
            "public void ClearList()");
        Contains(addEntry,
            "contentList.gameObject.SetActive(_filterPattern.Matches(name));",
            "new entry receives one cached-pattern match");
        DoesNotContain(addEntry,
            "FilterList(",
            "AddEntry must not rescan all existing entries");

        var release = Slice(
            panel,
            "internal void ReleaseEntries()",
            "public void ToggleVisibility(bool visible)");
        Contains(release,
            "entry.Toggle.onValueChanged.RemoveAllListeners();",
            "toggle closures released before destroy");
        Contains(release,
            "entry.RowButton.onClick.RemoveAllListeners();",
            "row-button closures released before destroy");
        Contains(release,
            "TooltipBinding.Bind(entry.RowButton.gameObject, null, null);",
            "tooltip binding released before destroy");
        Contains(release,
            "_listItems.Clear();",
            "entry references released");
        Equal(true,
            release.IndexOf("RemoveAllListeners", StringComparison.Ordinal)
            < release.IndexOf("UnityEngine.Object.Destroy", StringComparison.Ordinal),
            "listeners are removed before deferred Unity destruction");
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
        var endIndex = source.IndexOf(
            end,
            startIndex + start.Length,
            StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("Could not isolate source contract.");
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
