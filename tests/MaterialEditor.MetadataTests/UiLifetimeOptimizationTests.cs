using MaterialEditorAPI;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

internal static class UiLifetimeOptimizationTests
{
    private const float RowHeight = MaterialEditorUI.PanelHeight;

    internal static void Run()
    {
        DeferredRefreshUsesOneRestartableLatestWinsWorker();
        DeferredRefreshCancellationDropsTheTarget();
        ListenerScopeBalancesFiveHundredReuseCycles();
        VirtualListReleaseDropsModelsAndListenersWithoutMovingContext();
        VirtualListReleasesExcessViewsImmediately();
        VirtualListIdleFramesDoNoBindingOrLayoutWork();
        ReleasedSelectionPrunesMissingObjectsAndPreservesOrder();
        ProductionIntegrationPreservesCloseAndReopenSemantics();
        Console.WriteLine("UI lifetime and deferred-refresh regression tests passed.");
    }

    private static void DeferredRefreshUsesOneRestartableLatestWinsWorker()
    {
        var coordinator = new DeferredRefreshCoordinator();
        var initialTarget = new object();
        var initialVersion = coordinator.Schedule(initialTarget, "initial-data", "initial");
        var workerStarts = coordinator.TryStartWorker() ? 1 : 0;

        object target;
        object data;
        string filter;
        for (var frame = 0; frame < 9; frame++)
            Equal(false, coordinator.AdvanceFrame(10, out target, out data, out filter),
                "initial countdown frame " + frame);

        object latestTarget = null;
        for (var request = 0; request < 100; request++)
        {
            latestTarget = new object();
            coordinator.Schedule(latestTarget, "data-" + request, "filter-" + request);
            if (coordinator.TryStartWorker())
                workerStarts++;
        }

        Equal(1, workerStarts, "100 replacements keep one worker");
        Equal(false, coordinator.IsCurrent(initialVersion), "initial request superseded");
        for (var frame = 0; frame < 9; frame++)
            Equal(false, coordinator.AdvanceFrame(10, out target, out data, out filter),
                "replacement restarts full countdown frame " + frame);

        Equal(true, coordinator.AdvanceFrame(10, out target, out data, out filter),
            "latest request becomes ready on its tenth frame");
        Equal(true, ReferenceEquals(latestTarget, target), "only latest target returned");
        Equal("data-99", data, "only latest data returned");
        Equal("filter-99", filter, "only latest filter returned");
        Equal(false, coordinator.HasPending, "completed request releases retained fields");
        coordinator.WorkerStopped();
    }

    private static void DeferredRefreshCancellationDropsTheTarget()
    {
        var coordinator = new DeferredRefreshCoordinator();
        var targetToRelease = new object();
        coordinator.Schedule(targetToRelease, new object(), "close");
        Equal(true, coordinator.TryStartWorker(), "close test worker starts");

        object target;
        object data;
        string filter;
        for (var frame = 0; frame < 9; frame++)
            coordinator.AdvanceFrame(10, out target, out data, out filter);

        coordinator.Cancel();
        Equal(false, coordinator.HasPending, "close/synchronous cancel clears pending request");
        Equal(false, coordinator.AdvanceFrame(10, out target, out data, out filter),
            "cancelled request never becomes ready");
        Equal(null, target, "cancelled target is not returned");
        Equal(false, coordinator.TryStartWorker(), "cancelled state cannot restart a worker");

        coordinator.Schedule(new object(), new object(), "retry");
        Equal(true, coordinator.TryStartWorker(), "retry worker starts");
        coordinator.WorkerStopped();
        Equal(true, coordinator.TryStartWorker(),
            "failed/null coroutine start can release its lease and retry");
    }

    private static void ListenerScopeBalancesFiveHundredReuseCycles()
    {
        MaterialEditorPerformance.Configure(false, true, 0d, null, null);
        MaterialEditorPerformance.Reset();
        try
        {
            var scope = new ListenerScope();
            var button = new GameObject().AddComponent<Button>();
            var invocations = 0;
            for (var cycle = 0; cycle < 500; cycle++)
            {
                UnityAction listener = () => invocations++;
                scope.Listen(button, listener);
                Equal(1, button.onClick.ListenerCount, "one active listener in cycle " + cycle);
                button.onClick.Invoke();
                scope.Clear();
                Equal(0, button.onClick.ListenerCount, "listener removed in cycle " + cycle);
                button.onClick.Invoke();
            }

            Equal(500, invocations, "released listeners are never invoked again");
            var snapshot = MaterialEditorPerformance.CaptureSnapshot();
            Equal(500L, snapshot.GetCount(MaterialEditorPerformanceMetric.ListenerRegistrations),
                "listener registrations");
            Equal(500L, snapshot.GetCount(MaterialEditorPerformanceMetric.ListenerRemovals),
                "listener removals");

            var binderSource = ReadRepositorySource(
                Path.Combine("src", "MaterialEditor.Base", "UI", "UI.RowBinder.cs"));
            Equal(1, CountOccurrences(binderSource, "new ListenerScope()"),
                "one reusable ListenerScope per RowBinder");
            Equal(0, CountOccurrences(binderSource, "RemoveAllListeners"),
                "row binding never removes unrelated listeners");
        }
        finally
        {
            MaterialEditorPerformance.Configure(false, false, 0d, null, null);
            MaterialEditorPerformance.Reset();
        }
    }

    private static void VirtualListReleaseDropsModelsAndListenersWithoutMovingContext()
    {
        var views = new List<RowView>();
        var list = CreateVirtualList(3 * RowHeight, 3, views);
        var rows = CreateRows(8);
        SetScrollPosition(list, 2 * RowHeight + 4.5f);
        list.SetList(rows, false);
        InvokeUpdate(list);
        var anchor = list.CaptureTopRowAnchor();
        list.RestoreTopRowAnchor(anchor, false);

        var scrollBefore = ScrollPosition(list);
        var anchorBefore = list.ViewportAnchorIndex;
        var poolBefore = CachedViews(list).Count;
        var notifications = 0;
        list.ViewportAnchorIndexChanged += _ => notifications++;
        list.ReleaseContent();

        Equal(0, Models(list).Count, "ReleaseContent clears models immediately");
        Equal(poolBefore, CachedViews(list).Count, "ReleaseContent preserves view pool");
        Equal(scrollBefore, ScrollPosition(list), "ReleaseContent preserves scroll");
        Equal(anchorBefore, list.ViewportAnchorIndex, "ReleaseContent preserves published anchor");
        Equal(0, notifications, "ReleaseContent publishes no anchor event");
        foreach (var view in views)
        {
            Equal(null, view.CurrentModel, "released view has no model");
            Equal(false, view.ListenersActive, "released view has no listeners");
            Equal(false, view.Visible, "released view is hidden");
        }

        for (var frame = 0; frame < 100; frame++)
            InvokeUpdate(list);
        list.RunPendingCoroutines();
        Equal(scrollBefore, ScrollPosition(list), "cancelled restore cannot move released content");
        Equal(anchorBefore, list.ViewportAnchorIndex, "cancelled restore cannot replace anchor");
        Equal(0, notifications, "cancelled restore publishes no event");
    }

    private static void VirtualListReleasesExcessViewsImmediately()
    {
        var views = new List<RowView>();
        var list = CreateVirtualList(4 * RowHeight, 4, views);
        list.SetList(CreateRows(4), false);
        InvokeUpdate(list);
        Equal(true, views.All(view => view.CurrentModel != null), "pool initially bound");

        var poolBefore = CachedViews(list).Count;
        list.SetList(CreateRows(2), false);
        Equal(poolBefore, CachedViews(list).Count, "list shrink keeps pool capacity");
        for (var index = 2; index < views.Count; index++)
        {
            Equal(null, views[index].CurrentModel, "excess model released immediately " + index);
            Equal(false, views[index].ListenersActive, "excess listeners released immediately " + index);
            Equal(false, views[index].Visible, "excess view hidden immediately " + index);
        }
    }

    private static void VirtualListIdleFramesDoNoBindingOrLayoutWork()
    {
        var views = new List<RowView>();
        var list = CreateVirtualList(3 * RowHeight, 3, views);
        list.SetList(CreateRows(8), false);
        InvokeUpdate(list);
        var bindsBefore = views.Sum(view => view.BindCount);
        LayoutRebuilder.Reset();

        for (var frame = 0; frame < 100; frame++)
            InvokeUpdate(list);

        Equal(bindsBefore, views.Sum(view => view.BindCount),
            "100 unchanged frames perform no binds");
        Equal(0, LayoutRebuilder.MarkCount,
            "100 unchanged frames perform no layout rebuilds");

        SetScrollPosition(list, 2 * RowHeight);
        InvokeUpdate(list);
        Equal(true, views.Sum(view => view.BindCount) > bindsBefore,
            "scroll change across rows still refreshes the viewport");
    }

    private static void ReleasedSelectionPrunesMissingObjectsAndPreservesOrder()
    {
        var first = new GameObject().AddComponent<Renderer>();
        var second = new GameObject().AddComponent<Renderer>();
        var replaced = new GameObject().AddComponent<Renderer>();
        var selected = new List<Renderer> { replaced, second, null, first };
        var available = new List<Renderer> { first, second };

        MaterialEditorSessionState.PruneUnavailableUnityObjects(
            selected,
            available);

        Equal(2, selected.Count, "missing renderer selections pruned");
        Equal(true, ReferenceEquals(second, selected[0]),
            "valid renderer selection order preserved first");
        Equal(true, ReferenceEquals(first, selected[1]),
            "valid renderer selection order preserved second");

        var currentMaterial = new Material();
        var staleMaterial = new Material();
        var selectedMaterials = new List<Material>
        {
            staleMaterial,
            currentMaterial
        };
        MaterialEditorSessionState.PruneUnavailableUnityObjects(
            selectedMaterials,
            new List<Material> { currentMaterial });
        Equal(1, selectedMaterials.Count, "replaced material selection pruned");
        Equal(true, ReferenceEquals(currentMaterial, selectedMaterials[0]),
            "current material selection retained");
    }

    private static void ProductionIntegrationPreservesCloseAndReopenSemantics()
    {
        var source = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.cs"));
        Contains(source, "RefreshDeferred = SchedulePopulateList,",
            "deferred actions use the single coordinator");
        Equal(1, CountOccurrences(source, "StartCoroutine(DeferredPopulateWorker())"),
            "only one worker start site");
        Contains(source, "if (_deferredRefreshCoroutine == null)\n                        _deferredRefresh.WorkerStopped();",
            "null coroutine releases the worker lease");
        Contains(source, "catch\n                {\n                    _deferredRefresh.WorkerStopped();",
            "failed coroutine start releases the worker lease");

        var synchronousPopulate = ExtractMethod(source, "protected void PopulateList(");
        var cancel = synchronousPopulate.IndexOf("CancelPendingRefreshes();", StringComparison.Ordinal);
        var rebuild = synchronousPopulate.IndexOf("PopulateListCore(", StringComparison.Ordinal);
        Equal(true, cancel >= 0 && rebuild > cancel,
            "synchronous refresh cancels deferred work before rebuilding");

        var release = ExtractMethod(source, "private void ReleaseTransientUiContent(");
        Contains(release, "CancelPendingRefreshes();", "close cancels all refresh workers");
        var cancelAll = ExtractMethod(source, "private void CancelPendingRefreshes(");
        Contains(cancelAll, "CancelDeferredPopulate();", "synchronous paths cancel shader refresh");
        Contains(cancelAll, "CancelPresentationInvalidation();", "synchronous paths cancel search refresh");
        Contains(release, "VirtualList?.ReleaseContent();", "close releases virtual rows");
        Contains(release, "_selectionController?.ReleaseTransientContent();",
            "close releases side-list closures");
        Contains(release, "_windowView?.ReleasePresentation();",
            "close releases category presentation");
        Contains(release, "_presentation = null;", "close releases main presentation");

        var selectionSource = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.SelectionController.cs"));
        Contains(selectionSource, "MaterialEditorSessionState.PruneUnavailableUnityObjects(",
            "reopen prunes destroyed or replaced renderer/material selections");

        Contains(source, "else if (!wasVisible)\n                    ActiveUi?.RestoreTransientUiContent();",
            "Visible=true restores released content");
        var restore = ExtractMethod(source, "private void RestoreTransientUiContent(");
        Contains(restore, "PopulateListCore(gameObject, data, filter, null, true);",
            "direct reopen rebuilds retained target while preserving rename panel");

        var disposeWatcher = ExtractMethod(source, "internal static void DisposeTexChangeWatcher(");
        var clearWatcher = disposeWatcher.IndexOf("TexChangeWatcher = null;", StringComparison.Ordinal);
        var dispose = disposeWatcher.IndexOf("watcher?.Dispose();", StringComparison.Ordinal);
        Equal(true, clearWatcher >= 0 && dispose > clearWatcher,
            "texture watcher is nulled before disposal");
    }

    private static VirtualList CreateVirtualList(
        float viewportHeight,
        int viewCount,
        ICollection<RowView> views)
    {
        var contentObject = new GameObject();
        var content = contentObject.AddComponent<RectTransform>();
        content.rect = new Rect(0f, 0f, 300f, 4000f);
        var layout = contentObject.AddComponent<VerticalLayoutGroup>();

        var viewportObject = new GameObject();
        var viewport = viewportObject.AddComponent<RectTransform>();
        viewport.rect = new Rect(0f, 0f, 300f, viewportHeight);

        var scrollObject = new GameObject();
        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;

        var listObject = new GameObject();
        var list = listObject.AddComponent<VirtualList>();
        list.ScrollRect = scrollRect;
        SetField(list, "_verticalLayoutGroup", layout);

        var cache = CachedViews(list);
        for (var index = 0; index < viewCount; index++)
        {
            var view = new GameObject().AddComponent<RowView>();
            cache.Add(view);
            views.Add(view);
        }
        SetField(list, "_activeViewCapacity", viewCount);
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

    private static List<RowView> CachedViews(VirtualList list) =>
        GetField<List<RowView>>(list, "_cachedViews");

    private static List<RowModel> Models(VirtualList list) =>
        GetField<List<RowModel>>(list, "_models");

    private static T GetField<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("Field not found: " + name);
        return (T)field.GetValue(instance);
    }

    private static void SetField(object instance, string name, object value)
    {
        var field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("Field not found: " + name);
        field.SetValue(instance, value);
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

    private static float ScrollPosition(VirtualList list) =>
        list.ScrollRect.content.localPosition.y;

    private static void SetScrollPosition(VirtualList list, float value)
    {
        var position = list.ScrollRect.content.localPosition;
        position.y = value;
        list.ScrollRect.content.localPosition = position;
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

    private static string ReadRepositorySource(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath))
            .Replace("\r\n", "\n");

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
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Contains(string source, string expected, string name) =>
        Equal(true, source.Contains(expected, StringComparison.Ordinal), name);

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
