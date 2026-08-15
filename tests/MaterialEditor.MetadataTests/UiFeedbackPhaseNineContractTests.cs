using MaterialEditorAPI;

internal static class UiFeedbackPhaseNineContractTests
{
    internal static void Run()
    {
        EmptyStatePolicyUsesOnlyExactCountsAndFilterContext();
        CenterSurfaceOwnsOnePassiveReusableMessage();
        SelectionListsTrackVisibleEntriesInsideExistingPasses();
        RenameAndSelectionLifecycleRemainIndependent();
        UnsupportedWarningsFooterAndPollingStayAbsent();
        Console.WriteLine("Phase 9 feedback regression guards passed.");
    }

    private static void EmptyStatePolicyUsesOnlyExactCountsAndFilterContext()
    {
        Equal(null,
            MaterialEditorEmptyState.ForPresentation(1, false),
            "non-empty presentation");
        Equal("No rows in this view",
            MaterialEditorEmptyState.ForPresentation(0, false),
            "unfiltered empty presentation");
        Equal("No matches",
            MaterialEditorEmptyState.ForPresentation(0, true),
            "filtered empty presentation");

        Equal("No renderers",
            MaterialEditorEmptyState.ForSelectionList(0, 0, "No renderers"),
            "empty renderer source");
        Equal("No materials",
            MaterialEditorEmptyState.ForSelectionList(0, 0, "No materials"),
            "empty material source");
        Equal("No matches",
            MaterialEditorEmptyState.ForSelectionList(4, 0, "No renderers"),
            "filtered selection list");
        Equal(null,
            MaterialEditorEmptyState.ForSelectionList(4, 2, "No renderers"),
            "visible selection entries");
    }

    private static void CenterSurfaceOwnsOnePassiveReusableMessage()
    {
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var presenter = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialEditorPresenter.cs");
        var presentation = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Presentation.cs");
        var setPresentation = Slice(
            window,
            "internal void SetPresentation(",
            "internal void ReleasePresentation()");
        var releasePresentation = Slice(
            window,
            "internal void ReleasePresentation()",
            "private void SetEmptyState(string text)");

        Equal(1,
            CountOccurrences(window, "\"MaterialEditorEmptyState\""),
            "single central empty-state object");
        Contains(window,
            "ScrollableUI.viewport,\n                string.Empty,",
            "central state is outside VirtualList content");
        Contains(window,
            "_emptyStateText.raycastTarget = false;",
            "central state does not intercept input");
        Contains(setPresentation,
            "MaterialEditorEmptyState.ForPresentation(\n                        presentation.Rows.Count,\n                        presentation.HasActiveFilter)",
            "central state uses current presentation rows and filter context");
        Contains(releasePresentation,
            "SetEmptyState(null);",
            "central state clears with presentation lifetime");
        Contains(presentation,
            "internal bool HasActiveFilter;",
            "presentation carries exact filter context");
        Contains(presenter,
            "rendererFilter.Count != 0 || propertyFilter.Count != 0",
            "filter context comes from parsed renderer/property tokens");
    }

    private static void SelectionListsTrackVisibleEntriesInsideExistingPasses()
    {
        var panel = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectListPanel.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var add = Slice(
            panel,
            "internal void AddEntry(",
            "public void ClearList()");
        var release = Slice(
            panel,
            "internal void ReleaseEntries()",
            "public void ToggleVisibility(bool visible)");
        var filter = Slice(
            panel,
            "private void FilterList(string filter)",
            "private void UpdateHeader()");

        Equal(1,
            CountOccurrences(panel, "name + \"EmptyState\""),
            "one reusable empty-state template per selection panel");
        Contains(panel,
            "if (selectionChrome)\n            {\n                _emptyStateText",
            "empty state is selection-chrome only");
        Contains(panel,
            "_emptyStateText.raycastTarget = false;",
            "selection empty state does not intercept input");
        Contains(window, "\"No renderers\"", "renderer empty noun");
        Contains(window, "\"No materials\"", "material empty noun");

        Contains(add,
            "contentList.gameObject.SetActive(_filterPattern.Matches(name));",
            "new entry evaluates the retained filter once");
        Contains(add,
            "if (contentList.gameObject.activeSelf)\n                _visibleEntryCount++;",
            "new visible entry updates count");
        Equal(1,
            CountOccurrences(filter, "foreach (var entry in _listItems.Values)"),
            "single filtering pass");
        Contains(filter,
            "_visibleEntryCount = visibleEntryCount;",
            "filter publishes its visible count");
        Contains(release,
            "_visibleEntryCount = 0;",
            "release clears visible count");
        Contains(panel,
            "MaterialEditorEmptyState.ForSelectionList(\n                _listItems.Count,\n                _visibleEntryCount,",
            "selection message uses real total and visible counts");
    }

    private static void RenameAndSelectionLifecycleRemainIndependent()
    {
        var panel = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectListPanel.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var constructor = Slice(
            panel,
            "public SelectListPanel(Transform parent, string name, string title)",
            "internal SelectListPanel(");

        Contains(constructor,
            ": this(parent, name, title, null, false, null)",
            "legacy rename panel opts out of empty-state chrome");
        Contains(window,
            "RenameList = new SelectListPanel(MainPanel.transform, \"MaterialRenameList\", \"Mat. Renderers\");",
            "rename panel keeps legacy constructor");
        Contains(panel,
            "_filterInputField.onValueChanged.AddListener(FilterList);",
            "single persistent filter listener");
        Equal(1,
            CountOccurrences(panel,
                "_filterInputField.onValueChanged.AddListener(FilterList);"),
            "one filter listener per panel");
        Contains(panel,
            "var visible = _expanded && !string.IsNullOrEmpty(text);",
            "collapsed panel suppresses empty feedback");
    }

    private static void UnsupportedWarningsFooterAndPollingStayAbsent()
    {
        var policy = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.EmptyState.cs");
        var panel = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectListPanel.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");

        foreach (var forbidden in new[]
                 {
                     "LogWarning",
                     "Logger",
                     "MaterialEditService",
                     "ConfigEntry",
                     "void Update()",
                     "void LateUpdate()",
                     "StartCoroutine",
                     "available",
                     "ModifiedCount"
                 })
        {
            DoesNotContain(policy + panel, forbidden, forbidden + " feedback source");
        }

        DoesNotContain(window, "Footer", "invented footer surface");
        DoesNotContain(policy, "Warning", "invented warning state");
        DoesNotContain(policy, "public ", "new public feedback API");
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
