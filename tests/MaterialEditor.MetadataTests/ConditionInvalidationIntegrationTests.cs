using MaterialEditorAPI;

internal static class ConditionInvalidationIntegrationTests
{
    internal static void Run()
    {
        MultipleVisibilitySourcesDeduplicateIntoOneSelectiveBatch();
        VisibilityRecoveryRequestsOneRebuild();
        ReentrantRecoveryDrainsWithoutAnOrphanedGate();
        ProductionBuildAllocatesGraphStateOnlyWhenConditionsExist();
        ProductionGroupedResolverSupportsFloatAndKeyword();
        ProductionWiringRejectsStaleHandlesAndDefersOneDecision();
        Console.WriteLine("Condition invalidation integration tests passed.");
    }

    private static void MultipleVisibilitySourcesDeduplicateIntoOneSelectiveBatch()
    {
        var firstValue = 0f;
        var secondValue = 0f;
        Func<string, float?> raw = name => name == "First"
            ? firstValue
            : secondValue;
        var initial = new MaterialEditorConditionValueCache(raw, 2);
        var graph = new MaterialConditionDependencyGraph(
            1,
            "Shader/Multiple",
            raw,
            null);
        var first = graph.RegisterProperty(
            "FirstDependent",
            Equal("First", 1f),
            initial.Resolve);
        var second = graph.RegisterProperty(
            "SecondDependent",
            Equal("Second", 1f),
            initial.Resolve);
        graph.CompleteBuild(
            Supported("First", "Second"),
            Known("First", "Second"));
        MaterialConditionInvalidationHandle firstHandle;
        MaterialConditionInvalidationHandle secondHandle;
        graph.TryCreateHandle("First", out firstHandle);
        graph.TryCreateHandle("Second", out secondHandle);
        var coordinator =
            new PresentationInvalidationCoordinator<
                MaterialConditionInvalidationHandle>();
        var changedRequests = 0;
        for (var index = 0; index < 250; index++)
        {
            if (coordinator.RequestCondition(firstHandle))
                changedRequests++;
            if (coordinator.RequestCondition(secondHandle))
                changedRequests++;
        }
        Equal(2, changedRequests,
            "500 requests retain two distinct material/source handles");
        Equal(2, coordinator.PendingConditionCount,
            "multiple sources deduplicate independently");

        firstValue = 1f;
        secondValue = 1f;
        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "one condition worker lease");
        PresentationInvalidationBatch<MaterialConditionInvalidationHandle> batch;
        Equal(true, coordinator.TryBeginFlush(lease, 100, out batch),
            "one end-of-frame condition batch");
        var result = graph.EvaluateHandles(batch.ConditionSources);
        Equal(2, result.DependentsEvaluated,
            "only the two requested direct dependents are evaluated");
        Equal(2, result.VisibilityChangeCount,
            "both direct visibility dependents change");
        Equal(true, result.VisibilityChanged,
            "a visibility batch requests one presentation rebuild");
        Equal(false, coordinator.CompleteFlush(lease),
            "single batch releases its worker");
    }

    private static void VisibilityRecoveryRequestsOneRebuild()
    {
        var value = 0f;
        Func<string, float?> raw = name => value;
        var graph = new MaterialConditionDependencyGraph(
            2,
            "Shader/Recovery",
            raw,
            null);
        var state = graph.RegisterProperty(
            "Dependent",
            Equal("Gate", 1f),
            raw);
        graph.CompleteBuild(Supported("Gate"), Known("Gate"));
        Equal(false, state.Visible, "initial visibility state");
        MaterialConditionInvalidationHandle handle;
        graph.TryCreateHandle("Gate", out handle);

        var coordinator =
            new PresentationInvalidationCoordinator<
                MaterialConditionInvalidationHandle>();
        coordinator.RequestCondition(handle);
        PresentationInvalidationWorkerLease lease;
        coordinator.TryAcquireWorker(out lease);
        value = 1f;

        PresentationInvalidationBatch<MaterialConditionInvalidationHandle> batch;
        Equal(true, coordinator.TryBeginRecoveryFlush(lease, out batch),
            "failed coroutine start can synchronously take Conditions batch");
        Equal(PresentationInvalidationReason.Conditions, batch.Reason,
            "recovery preserves Conditions reason");
        var result = graph.EvaluateHandles(batch.ConditionSources);
        Equal(1, result.DependentsEvaluated,
            "recovery evaluates the exact visibility dependent");
        Equal(true, result.VisibilityChanged,
            "visibility recovery requests one presentation rebuild");
        Equal(false, state.Visible,
            "the old graph remains immutable until its replacement is built");
        Equal(false, coordinator.CompleteFlush(lease),
            "recovery consumes pending work and releases gate");
        Equal(false, coordinator.HasPending,
            "recovery leaves no pending batch");
        Equal(false, coordinator.HasActiveWorker,
            "recovery leaves no orphaned worker");
    }

    private static void ReentrantRecoveryDrainsWithoutAnOrphanedGate()
    {
        var coordinator =
            new PresentationInvalidationCoordinator<string>(
                StringComparer.Ordinal);
        coordinator.RequestCondition("first");
        PresentationInvalidationWorkerLease lease;
        coordinator.TryAcquireWorker(out lease);
        PresentationInvalidationBatch<string> first;
        Equal(true, coordinator.TryBeginRecoveryFlush(lease, out first),
            "first recovery batch begins");
        coordinator.RequestCondition("second");
        Equal(true, coordinator.CompleteFlush(lease),
            "reentrant recovery retains the same lease");

        PresentationInvalidationBatch<string> second;
        Equal(true, coordinator.TryBeginRecoveryFlush(lease, out second),
            "reentrant batch is synchronously drainable");
        Equal("second", second.ConditionSources[0],
            "reentrant source survives first completion");
        Equal(false, coordinator.CompleteFlush(lease),
            "second recovery batch releases lease");
        Equal(false, coordinator.HasPending,
            "reentrant recovery leaves no pending work");
        Equal(false, coordinator.HasActiveWorker,
            "reentrant recovery leaves no gate");
    }

    private static void ProductionBuildAllocatesGraphStateOnlyWhenConditionsExist()
    {
        var source = ReadRepositorySource(
            Path.Combine(
                "src",
                "MaterialEditor.Base",
                "UI",
                "UI.MaterialSectionPresenter.cs"));
        var addRows = ExtractMethod(source, "private void AddPropertyRows(");
        Equal(0, CountOccurrences(
                addRows,
                "manifestPropertyMap.Values.ToList"),
            "manifest definitions are not copied for graph construction");
        var conditionalStart = addRows.IndexOf(
            "if (conditionDependencies.Count > 0)",
            StringComparison.Ordinal);
        var graphCreation = addRows.IndexOf(
            "new MaterialConditionDependencyGraph(",
            StringComparison.Ordinal);
        Equal(true, conditionalStart >= 0 && graphCreation > conditionalStart,
            "zero-condition materials allocate no graph/resolver/catalog");
        Contains(addRows,
            "if (definition.ShowIf == null)\n                        continue;",
            "manifest state exists only for conditional dependents");
        Contains(addRows,
            "if (descriptor.VisibilityCondition == null)\n                        continue;",
            "extension state exists only for conditional descriptor ordinals");
        Ordered(addRows,
            ".GetPropertyDescriptors(propertyContext);",
            "new MaterialConditionDependencyGraph(",
            "providers materialize atomically before graph construction");
        Ordered(addRows,
            "conditionGraph.CompleteBuild(",
            "foreach (var category in organizedCategories)",
            "graph completes before compatibility, mode, and search filtering");
        Contains(addRows,
            "Dictionary<int, MaterialConditionPropertyState>",
            "extension dependents retain instance ordinal rather than descriptor ID");
    }

    private static void ProductionGroupedResolverSupportsFloatAndKeyword()
    {
        var source = ReadRepositorySource(
            Path.Combine(
                "src",
                "MaterialEditor.Base",
                "UI",
                "UI.MaterialSectionPresenter.cs"));
        var addRows = ExtractMethod(source, "private void AddPropertyRows(");
        Contains(addRows,
            "CaptureConditionMaterialGroup(\n                    context);",
            "condition materials are captured once per section build");
        Contains(addRows,
            "new MaterialEditorConditionValueSnapshotCache(",
            "initial dependents share one source snapshot cache");

        var capture = ExtractMethod(
            source,
            "private static IList<Material> CaptureConditionMaterialGroup(");
        Contains(capture, "context.AllRenderers",
            "same-name group comes from the section renderer snapshot");
        Contains(capture,
            "material.NameFormatted() != context.MaterialName",
            "only materials edited by the same material-name section join the group");
        Contains(capture, "AddConditionMaterial(materials, material);",
            "matching group members are captured once");

        var resolve = ExtractMethod(
            source,
            "private static MaterialConditionValueSnapshot ResolveConditionValues(");
        Contains(resolve,
            "kind == MaterialConditionSourceKind.Keyword",
            "Keyword ShowIf sources are read for every group member");
        Contains(resolve, "material.IsKeywordEnabled(",
            "Keyword source preserves Material Editor keyword semantics");
        Contains(resolve, "MaterialPropertyAccess.HasProperty(",
            "Float source checks per-material availability");
        Contains(resolve, "MaterialPropertyAccess.GetFloat(",
            "Float source reads every available group member");
    }

    private static void ProductionWiringRejectsStaleHandlesAndDefersOneDecision()
    {
        var source = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.cs"));
        Contains(source,
            "RequestCondition = HandleConditionChanged,",
            "row condition action is connected internally");
        var handler = ExtractMethod(
            source,
            "private void HandleConditionChanged(");
        Ordered(handler,
            "!presentation.Owns(handle)",
            "CancelDeferredPopulate();",
            "stale/closed handle is rejected before cross-cancel");
        Ordered(handler,
            "!presentation.Owns(handle)",
            "MaterialEditorPerformanceMetric.RefreshRequests",
            "stale/closed handle is rejected before metrics");
        Contains(handler,
            "_presentationInvalidation.RequestCondition(handle)",
            "Conditions share the presentation coordinator");
        Contains(handler,
            "TryStartPresentationInvalidationWorker();",
            "Conditions share one EOF worker");

        var worker = ExtractMethod(
            source,
            "private IEnumerator PresentationInvalidationWorker(");
        Contains(worker,
            "ApplyPresentationInvalidationBatch(batch);",
            "worker dispatches Search and Conditions through one batch path");
        var dispatch = ExtractMethod(
            source,
            "private void ApplyPresentationInvalidationBatch(");
        Contains(dispatch,
            "PresentationInvalidationReason.Search",
            "Search remains dominant");
        Contains(dispatch,
            "PresentationInvalidationReason.Conditions",
            "Conditions use selective path");

        var apply = ExtractMethod(source, "private void ApplyConditionRefresh(");
        Contains(apply, "presentation.Owns(handle)",
            "reentrant old-presentation handles are rejected at flush");
        Ordered(apply,
            "foreach (var entry in grouped)",
            "if (visibilityChanged)",
            "all graphs finish before one presentation decision");
        Ordered(apply,
            "if (visibilityChanged)",
            "VirtualList.CaptureTopRowAnchor();",
            "ShowIf captures the viewport only when a rebuild is required");
        Ordered(apply,
            "VirtualList.CaptureTopRowAnchor();",
            "PopulateListCore(",
            "ShowIf captures the viewport before rebuilding");
        Contains(apply,
            "CurrentFilter,\n                    topRowAnchor,",
            "ShowIf restores the captured anchor through PopulateListCore");
        Equal(1, CountOccurrences(apply, "PopulateListCore("),
            "a ShowIf batch has exactly one rebuild site");

        var scheduleShader = ExtractMethod(
            source,
            "private int ScheduleDeferredPopulate(");
        Ordered(scheduleShader,
            "CancelPresentationInvalidation();",
            "_deferredRefresh.Schedule(go, data, filter);",
            "later shader still supersedes Search/Conditions");
        var recovery = ExtractMethod(
            source,
            "private void RecoverPresentationInvalidationWorkerStart(");
        Contains(recovery, "TryBeginRecoveryFlush(",
            "null/throw start drains typed pending batch");
        Contains(recovery, "ApplyPresentationInvalidationBatch(batch);",
            "recovery preserves selective ShowIf handling");
        Equal(0, CountOccurrences(recovery, "ApplySearchRefresh("),
            "recovery has no unconditional full-rebuild path");
    }

    private static MaterialEditorPropertyCondition Equal(
        string source,
        float value)
    {
        return new MaterialEditorPropertyCondition(
            source,
            MaterialEditorConditionComparison.Equal,
            value);
    }

    private static Dictionary<string, MaterialConditionSourceKind> Supported(
        params string[] names)
    {
        var result = new Dictionary<string, MaterialConditionSourceKind>(
            StringComparer.Ordinal);
        foreach (var name in names)
            result.Add(name, MaterialConditionSourceKind.Float);
        return result;
    }

    private static HashSet<string> Known(params string[] names)
    {
        return new HashSet<string>(names, StringComparer.Ordinal);
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
            throw new InvalidOperationException("Method not found: " + signature);
        var openingBrace = source.IndexOf('{', signatureIndex);
        if (openingBrace < 0)
            throw new InvalidOperationException("Method body not found: " + signature);
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

    private static string ReadRepositorySource(string relativePath)
    {
        return File.ReadAllText(
                Path.Combine(FindRepositoryRoot(), relativePath))
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
        throw new DirectoryNotFoundException(
            "Could not locate the KK_Plugins repository root.");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(
                   value,
                   index,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Ordered(
        string source,
        string first,
        string second,
        string message)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        Equal(true,
            firstIndex >= 0 && secondIndex > firstIndex,
            message);
    }

    private static void Contains(
        string source,
        string expected,
        string message)
    {
        Equal(true, source.Contains(expected, StringComparison.Ordinal), message);
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                message + ": expected " + expected + ", got " + actual);
        }
    }
}
