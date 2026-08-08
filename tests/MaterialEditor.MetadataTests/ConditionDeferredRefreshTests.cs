internal static class ConditionDeferredRefreshTests
{
    internal static void Run()
    {
        var root = FindRepositoryRoot();
        var uiSource = ReadSource(root, "src", "MaterialEditor.Base", "UI", "UI.cs");
        var actionsSource = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.MaterialEditorPresenter.cs");
        var materialSource = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.MaterialSectionPresenter.cs");

        Contains(
            actionsSource,
            "RefreshConditionsDeferred",
            "condition refresh has a dedicated internal action");
        Contains(
            uiSource,
            "RefreshDeferred = (go, data, filter) =>\n                        StartCoroutine(PopulateListCoroutine(go, data, filter))",
            "shader refresh retains the protected ten-frame coroutine");
        Contains(
            uiSource,
            "RefreshConditionsDeferred = ScheduleConditionRefresh",
            "ShowIf refresh uses the coalescing coordinator");
        Equal(
            2,
            CountOccurrences(materialSource, "_actions.RefreshDeferred("),
            "only shader change/reset use the legacy deferred refresh");
        Equal(
            1,
            CountOccurrences(
                materialSource,
                "_actions.RefreshConditionsDeferred("),
            "condition sources use only the dedicated refresh action");

        var schedule = ExtractMethod(
            uiSource,
            "private void ScheduleConditionRefresh(");
        Contains(
            schedule,
            "if (!IsCurrentConditionRefreshContext(go, data, filter))",
            "stale targets are rejected before scheduling");
        var latestAssignment = schedule.IndexOf(
            "_conditionRefreshGameObject = go;",
            StringComparison.Ordinal);
        var workerGuard = schedule.IndexOf(
            "if (_conditionRefreshCoroutine != null)",
            StringComparison.Ordinal);
        Equal(
            true,
            latestAssignment >= 0 && workerGuard > latestAssignment,
            "latest request replaces pending data before the single-worker guard");
        Equal(
            1,
            CountOccurrences(uiSource, "StartCoroutine(ConditionRefreshWorker())"),
            "condition refresh has one worker start site");

        var worker = ExtractMethod(
            uiSource,
            "private IEnumerator ConditionRefreshWorker(");
        Contains(
            worker,
            "while (scheduledVersion != _conditionRefreshVersion);",
            "worker waits for one stable frame and coalesces newer requests");
        Contains(
            worker,
            "if (!IsCurrentConditionRefreshContext(go, data, filter))",
            "worker revalidates target/data/filter before applying");
        Contains(
            worker,
            "_conditionRefreshCoroutine = null;",
            "worker releases its gate before synchronous population");

        var contextGuard = ExtractMethod(
            uiSource,
            "private bool IsCurrentConditionRefreshContext(");
        Contains(contextGuard, "return Visible", "closed UI rejects refresh");
        Contains(contextGuard, "go != null", "destroyed target rejects refresh");
        Contains(
            contextGuard,
            "ReferenceEquals(go, CurrentGameObject)",
            "refresh requires the exact current GameObject");
        Contains(
            contextGuard,
            "ReferenceEquals(data, CurrentData)",
            "refresh requires the exact current data context");
        Contains(
            contextGuard,
            "CurrentFilter,\n                       StringComparison.Ordinal",
            "refresh requires the current filter context");

        var populate = ExtractMethod(
            uiSource,
            "protected void PopulateList(");
        Contains(
            populate,
            "CancelConditionRefresh();",
            "synchronous population cancels pending condition refresh");
        Contains(
            uiSource,
            "ActiveUi?.CancelConditionRefresh();",
            "closing the Material Editor cancels pending condition refresh");

        var legacyCoroutine = ExtractMethod(
            uiSource,
            "protected IEnumerator PopulateListCoroutine(");
        Equal(
            10,
            CountOccurrences(legacyCoroutine, "yield return null;"),
            "protected shader dropdown delay remains ten frames");
        Contains(
            legacyCoroutine,
            "PopulateList(go, data, filter);",
            "protected deferred API retains its existing population behavior");
    }

    private static string ReadSource(string root, params string[] parts)
    {
        var path = parts.Aggregate(root, Path.Combine);
        return File.ReadAllText(path).Replace("\r\n", "\n");
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
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
            throw new InvalidOperationException("Method not found: " + signature);
        var openingBrace = source.IndexOf('{', signatureIndex);
        var depth = 0;
        for (var index = openingBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(openingBrace, index - openingBrace + 1);
        }
        throw new InvalidOperationException("Unterminated method: " + signature);
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

    private static void Contains(string source, string value, string name) =>
        Equal(true, source.Contains(value, StringComparison.Ordinal), name);

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
