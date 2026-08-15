using MaterialEditorAPI;

internal static class SearchInvalidationIntegrationTests
{
    internal static void Run()
    {
        FiveHundredKeystrokesUseOneWorkerAndOneRebuild();
        ReentrantSearchWaitsForTheFollowingFrame();
        SearchThenShaderLeavesOnlyLiveFilterShaderWork();
        ShaderThenSearchLeavesOnlySearchWork();
        SynchronousRefreshCancelsBothWorkers();
        NullWorkerStartFallsBackOnceAndClearsPending();
        ThrowingWorkerStartFallsBackOnceAndClearsPending();
        ThrowDuringFlushKeepsReentrantWorkOnTheSameLease();
        ProductionWiringPreservesOrderingAndPublicSemantics();
        Console.WriteLine("Search invalidation integration tests passed.");
    }

    private static void FiveHundredKeystrokesUseOneWorkerAndOneRebuild()
    {
        MaterialEditorPerformance.Configure(false, true, 0d, null, null);
        MaterialEditorPerformance.Reset();
        try
        {
            var coordinator = new PresentationInvalidationCoordinator<object>();
            var workerStarts = 0;
            var rebuilds = 0;
            var lease = new PresentationInvalidationWorkerLease();

            for (var key = 0; key < 500; key++)
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.RefreshRequests);
                if (!coordinator.RequestSearch())
                {
                    MaterialEditorPerformance.Increment(
                        MaterialEditorPerformanceMetric.RefreshCoalesced);
                }
                PresentationInvalidationWorkerLease candidate;
                if (coordinator.TryAcquireWorker(out candidate))
                {
                    lease = candidate;
                    workerStarts++;
                }
            }

            PresentationInvalidationBatch<object> batch;
            if (coordinator.TryBeginFlush(lease, 100, out batch))
            {
                try
                {
                    if (coordinator.IsGenerationCurrent(batch.Generation)
                        && (batch.Reason & PresentationInvalidationReason.Search) != 0)
                    {
                        MaterialEditorPerformance.Increment(
                            MaterialEditorPerformanceMetric.RefreshExecuted);
                        rebuilds++;
                    }
                }
                finally
                {
                    coordinator.CompleteFlush(lease);
                }
            }

            Equal(1, workerStarts, "500 keystrokes acquire one worker");
            Equal(1, rebuilds, "500 keystrokes cause one end-of-frame rebuild");
            Equal(false, coordinator.HasPending, "search batch fully consumed");
            var metrics = MaterialEditorPerformance.CaptureSnapshot();
            Equal(500L, metrics.GetCount(
                MaterialEditorPerformanceMetric.RefreshRequests),
                "500 search refresh requests recorded");
            Equal(499L, metrics.GetCount(
                MaterialEditorPerformanceMetric.RefreshCoalesced),
                "499 search refresh requests coalesced");
            Equal(1L, metrics.GetCount(
                MaterialEditorPerformanceMetric.RefreshExecuted),
                "one search refresh executed");
        }
        finally
        {
            MaterialEditorPerformance.Configure(false, false, 0d, null, null);
            MaterialEditorPerformance.Reset();
        }
    }

    private static void ReentrantSearchWaitsForTheFollowingFrame()
    {
        var coordinator = new PresentationInvalidationCoordinator<object>();
        coordinator.RequestSearch();
        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "reentrant search worker starts");

        PresentationInvalidationBatch<object> first;
        Equal(true, coordinator.TryBeginFlush(lease, 200, out first),
            "first search begins");
        coordinator.RequestSearch();
        Equal(true, coordinator.CompleteFlush(lease),
            "reentrant search retains the worker lease");

        PresentationInvalidationBatch<object> sameFrame;
        Equal(false, coordinator.TryBeginFlush(lease, 200, out sameFrame),
            "reentrant search cannot rebuild in the current frame");
        PresentationInvalidationBatch<object> nextFrame;
        Equal(true, coordinator.TryBeginFlush(lease, 201, out nextFrame),
            "reentrant search rebuilds in the following frame");
        Equal(PresentationInvalidationReason.Search, nextFrame.Reason,
            "reentrant batch remains a search");
        coordinator.CompleteFlush(lease);
    }

    private static void SearchThenShaderLeavesOnlyLiveFilterShaderWork()
    {
        var presentation = new PresentationInvalidationCoordinator<object>();
        var shader = new DeferredRefreshCoordinator();
        var target = new object();
        var data = new object();
        var currentFilter = "typed-live-filter";

        presentation.RequestSearch();
        PresentationInvalidationWorkerLease searchLease;
        presentation.TryAcquireWorker(out searchLease);

        // Mirrors ScheduleDeferredPopulate after its current-target filter has
        // been resolved by SchedulePopulateList.
        presentation.Cancel();
        shader.Schedule(target, data, currentFilter);
        Equal(true, shader.TryStartWorker(), "later shader worker starts");

        PresentationInvalidationBatch<object> cancelledSearch;
        Equal(false, presentation.TryBeginFlush(searchLease, 300, out cancelledSearch),
            "later shader cancels pending search");

        object flushedTarget = null;
        object flushedData = null;
        string flushedFilter = null;
        for (var frame = 0; frame < 10; frame++)
            shader.AdvanceFrame(10, out flushedTarget, out flushedData, out flushedFilter);
        Equal(true, ReferenceEquals(target, flushedTarget), "shader retains current target");
        Equal(true, ReferenceEquals(data, flushedData), "shader retains current data");
        Equal(currentFilter, flushedFilter, "shader uses live rather than captured filter");
    }

    private static void ShaderThenSearchLeavesOnlySearchWork()
    {
        var presentation = new PresentationInvalidationCoordinator<object>();
        var shader = new DeferredRefreshCoordinator();
        shader.Schedule(new object(), new object(), "shader-filter");
        Equal(true, shader.TryStartWorker(), "initial shader worker starts");

        // Mirrors HandleFilterChanged: the later search cancels shader first.
        shader.Cancel();
        presentation.RequestSearch();
        PresentationInvalidationWorkerLease searchLease;
        Equal(true, presentation.TryAcquireWorker(out searchLease),
            "later search worker starts");

        object target;
        object data;
        string filter;
        Equal(false, shader.AdvanceFrame(10, out target, out data, out filter),
            "cancelled shader never flushes");
        PresentationInvalidationBatch<object> searchBatch;
        Equal(true, presentation.TryBeginFlush(searchLease, 400, out searchBatch),
            "later search wins at end of frame");
        Equal(PresentationInvalidationReason.Search, searchBatch.Reason,
            "winning batch is search");
        presentation.CompleteFlush(searchLease);
    }

    private static void SynchronousRefreshCancelsBothWorkers()
    {
        var presentation = new PresentationInvalidationCoordinator<object>();
        var shader = new DeferredRefreshCoordinator();
        presentation.RequestSearch();
        PresentationInvalidationWorkerLease searchLease;
        presentation.TryAcquireWorker(out searchLease);
        shader.Schedule(new object(), new object(), "pending");
        shader.TryStartWorker();

        shader.Cancel();
        presentation.Cancel();

        PresentationInvalidationBatch<object> searchBatch;
        Equal(false, presentation.TryBeginFlush(searchLease, 500, out searchBatch),
            "synchronous refresh invalidates search worker");
        object target;
        object data;
        string filter;
        Equal(false, shader.AdvanceFrame(10, out target, out data, out filter),
            "synchronous refresh invalidates shader worker");
    }

    private static void NullWorkerStartFallsBackOnceAndClearsPending()
    {
        var coordinator = new PresentationInvalidationCoordinator<object>();
        coordinator.RequestSearch();
        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "null-start policy acquires worker lease");

        object coroutine = null;
        var fallbacks = coroutine == null
            ? RecoverFailedWorkerStart(coordinator, lease, null)
            : 0;
        Equal(1, fallbacks, "null coroutine start has one synchronous fallback");
        Equal(false, coordinator.HasPending,
            "null coroutine start cancels pending generation");
        Equal(false, coordinator.HasActiveWorker,
            "null coroutine start leaves no worker gate");
        Equal(false, coordinator.IsWorkerLeaseCurrent(lease),
            "null coroutine start invalidates stale lease");
    }

    private static void ThrowingWorkerStartFallsBackOnceAndClearsPending()
    {
        var coordinator = new PresentationInvalidationCoordinator<object>();
        coordinator.RequestSearch();
        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "throw-start policy acquires worker lease");

        var logged = 0;
        var fallbacks = 0;
        try
        {
            throw new InvalidOperationException("synthetic start failure");
        }
        catch (Exception ex)
        {
            fallbacks = RecoverFailedWorkerStart(
                coordinator,
                lease,
                ex,
                () => logged++);
        }
        Equal(1, logged, "throwing coroutine start is logged once");
        Equal(1, fallbacks, "throwing coroutine start has one synchronous fallback");
        Equal(false, coordinator.HasPending,
            "throwing coroutine start cancels pending generation");
        Equal(false, coordinator.HasActiveWorker,
            "throwing coroutine start leaves no worker gate");
        Equal(false, coordinator.IsWorkerLeaseCurrent(lease),
            "throwing coroutine start invalidates stale lease");
    }

    private static void ThrowDuringFlushKeepsReentrantWorkOnTheSameLease()
    {
        var coordinator = new PresentationInvalidationCoordinator<object>();
        coordinator.RequestSearch();
        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "throw-during-flush policy acquires worker");
        PresentationInvalidationBatch<object> first;
        Equal(true, coordinator.TryBeginFlush(lease, 600, out first),
            "throw-during-flush first batch begins");

        var logged = 0;
        var waitForNextFrame = false;
        try
        {
            coordinator.RequestSearch();
            throw new InvalidOperationException("synthetic apply failure");
        }
        catch (InvalidOperationException)
        {
            logged++;
        }
        finally
        {
            waitForNextFrame = coordinator.CompleteFlush(lease);
        }

        Equal(1, logged, "apply failure is logged/swallowed once");
        Equal(true, waitForNextFrame,
            "reentrant request keeps the same worker after apply failure");
        Equal(true, coordinator.IsWorkerLeaseCurrent(lease),
            "reentrant request has a live worker rather than an orphaned gate");
        PresentationInvalidationBatch<object> sameFrame;
        Equal(false, coordinator.TryBeginFlush(lease, 600, out sameFrame),
            "failed flush cannot process reentrant work in the same frame");
        PresentationInvalidationBatch<object> nextFrame;
        Equal(true, coordinator.TryBeginFlush(lease, 601, out nextFrame),
            "reentrant work survives for the following frame");
        Equal(false, coordinator.CompleteFlush(lease),
            "worker releases after surviving reentrant batch");
        Equal(false, coordinator.HasPending,
            "throw-during-flush policy leaves no pending work");
        Equal(false, coordinator.HasActiveWorker,
            "throw-during-flush policy leaves no orphaned gate");
    }

    private static int RecoverFailedWorkerStart(
        PresentationInvalidationCoordinator<object> coordinator,
        PresentationInvalidationWorkerLease lease,
        Exception exception,
        Action log = null)
    {
        if (exception != null)
            log?.Invoke();
        PresentationInvalidationBatch<object> batch;
        if (!coordinator.TryBeginRecoveryFlush(lease, out batch))
        {
            coordinator.AbandonWorker(lease);
            coordinator.Cancel();
            return 0;
        }
        coordinator.CompleteFlush(lease);
        return 1;
    }

    private static void ProductionWiringPreservesOrderingAndPublicSemantics()
    {
        var source = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.cs"));
        var window = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.WindowView.cs"));
        var topBar = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.TopBarView.cs"));

        Contains(source, "CurrentFilter,\n                HandleFilterChanged,",
            "window receives coalesced search callback");
        Contains(topBar,
            "FilterInputField.onValueChanged.AddListener(\n                value => filterChanged(value));",
            "raw InputField value reaches search callback immediately");
        Equal(1,
            CountOccurrences(window + topBar,
                "FilterInputField.onValueChanged.AddListener("),
            "topbar owns one immediate search listener");

        var search = ExtractMethod(source, "private void HandleFilterChanged(");
        Ordered(search,
            "CurrentFilter = filter;",
            "CancelDeferredPopulate();",
            "search stores the live filter before cancelling shader");
        Ordered(search,
            "CancelDeferredPopulate();",
            "if (!Visible || CurrentGameObject == null)",
            "search cancels shader before checking whether UI work is possible");
        Contains(search, "CancelPresentationInvalidation();\n                return;",
            "closed or targetless search cannot leave a worker");
        Ordered(search,
            "if (!Visible || CurrentGameObject == null)",
            "MaterialEditorPerformanceMetric.RefreshRequests",
            "ineligible search does not record or schedule UI work");
        Ordered(search,
            "MaterialEditorPerformanceMetric.RefreshRequests",
            "_presentationInvalidation.RequestSearch()",
            "eligible search records request before coalescing");
        Contains(search, "MaterialEditorPerformanceMetric.RefreshCoalesced",
            "unchanged search batch records coalescing");
        Contains(search, "TryStartPresentationInvalidationWorker();",
            "search requests its single worker");

        var startWorker = ExtractMethod(
            source,
            "private void TryStartPresentationInvalidationWorker(");
        Equal(1,
            CountOccurrences(source,
                "StartCoroutine(PresentationInvalidationWorker(lease))"),
            "one presentation worker start site");
        Contains(startWorker,
            "catch (Exception ex)\n            {\n                RecoverPresentationInvalidationWorkerStart(lease, ex);\n                return;",
            "throwing coroutine start recovers without rethrowing");
        Contains(startWorker,
            "if (coroutine == null)\n            {\n                RecoverPresentationInvalidationWorkerStart(lease, null);\n                return;",
            "null coroutine start uses one recovery path");
        Equal(0, CountOccurrences(startWorker, "throw;"),
            "worker-start exception is not rethrown after fallback");

        var recoverStart = ExtractMethod(
            source,
            "private void RecoverPresentationInvalidationWorkerStart(");
        Contains(recoverStart, "if (exception != null)",
            "throwing start is logged conditionally");
        Contains(recoverStart, "TryBeginRecoveryFlush(",
            "failed start drains the acquired pending lease");
        Contains(recoverStart, "ApplyPresentationInvalidationBatch(batch);",
            "failed start preserves Search/Conditions batch semantics");
        Contains(recoverStart, "_presentationInvalidation.CompleteFlush(lease);",
            "failed start always completes a begun recovery flush");
        Contains(recoverStart, "const int recoveryFlushLimit = 16;",
            "reentrant synchronous recovery is bounded");
        Contains(recoverStart, "_presentationInvalidation.AbandonWorker(lease);",
            "failed start cannot leave an acquired gate");
        Contains(recoverStart, "_presentationInvalidation.Cancel();",
            "bounded recovery tail clears any otherwise orphaned pending work");
        Equal(0, CountOccurrences(recoverStart, "ApplySearchRefresh();"),
            "failed start never forces Conditions through Search/full rebuild");

        var worker = ExtractMethod(
            source,
            "private IEnumerator PresentationInvalidationWorker(");
        Contains(worker, "yield return PresentationEndOfFrame;",
            "search flushes at end of frame without debounce");
        Contains(worker, "Time.frameCount,", "worker supplies frame identity");
        Contains(worker, "IsGenerationCurrent(", "worker validates batch generation");
        Contains(worker, "ApplyPresentationInvalidationBatch(batch);",
            "worker preserves Search/Conditions batch semantics");
        Contains(worker, "catch (Exception ex)",
            "apply exceptions are captured inside the worker");
        Contains(worker,
            "Exception while applying a coalesced Material Editor",
            "apply exceptions are logged without escaping worker");
        Contains(worker, "finally", "worker protects completion with finally");
        Contains(worker, "_presentationInvalidation.CompleteFlush(lease);",
            "worker always completes begun flush");
        Equal(0, CountOccurrences(worker, "throw;"),
            "apply exception does not escape and orphan reentrant work");

        var applySearch = ExtractMethod(source, "private void ApplySearchRefresh(");
        Ordered(applySearch,
            "MaterialEditorPerformanceMetric.RefreshExecuted",
            "PopulateListCore(",
            "both worker and fallback record execution before applying Search");
        Contains(applySearch, "CurrentFilter,",
            "worker and fallback rebuild with the live filter");

        var scheduleShader = ExtractMethod(source, "private void SchedulePopulateList(");
        Contains(scheduleShader, "ResolveFilterForCurrentTarget(go, data, filter)",
            "internal shader callback resolves current live filter");
        var scheduleDeferred = ExtractMethod(source, "private int ScheduleDeferredPopulate(");
        Ordered(scheduleDeferred,
            "CancelPresentationInvalidation();",
            "_deferredRefresh.Schedule(go, data, filter);",
            "later shader cancels search before scheduling");

        var protectedCoroutine = ExtractMethod(
            source,
            "protected IEnumerator PopulateListCoroutine(");
        Contains(protectedCoroutine,
            "ScheduleDeferredPopulate(go, data, filter);",
            "protected iterator preserves its explicit filter");
        Contains(protectedCoroutine, "yield return WaitForDeferredPopulate(version);",
            "protected iterator preserves laziness and ten-frame wait");
        Equal(0, CountOccurrences(protectedCoroutine, "ResolveFilterForCurrentTarget"),
            "protected iterator never substitutes its explicit filter");

        var cancelAll = ExtractMethod(source, "private void CancelPendingRefreshes(");
        Contains(cancelAll, "CancelDeferredPopulate();",
            "synchronous refresh cancels shader");
        Contains(cancelAll, "CancelPresentationInvalidation();",
            "synchronous refresh cancels search");
        var publicPopulate = ExtractMethod(source, "protected void PopulateList(");
        Contains(publicPopulate, "CancelPendingRefreshes();",
            "shipped synchronous PopulateList cancels both workers");
        var close = ExtractMethod(source, "private void ReleaseTransientUiContent(");
        Contains(close, "CancelPendingRefreshes();", "close cancels both workers");

        Contains(source,
            "public void RefreshUI(string filterText) => PopulateList(CurrentGameObject, CurrentData, filterText);",
            "public RefreshUI remains synchronous with explicit filter");
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

    private static string ReadRepositorySource(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath))
            .Replace("\r\n", "\n");
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
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

    private static void Ordered(
        string source,
        string first,
        string second,
        string name)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        Equal(true, firstIndex >= 0 && secondIndex > firstIndex, name);
    }

    private static void Contains(string source, string value, string name)
    {
        Equal(true, source.Contains(value, StringComparison.Ordinal), name);
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
