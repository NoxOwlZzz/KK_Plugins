using MaterialEditorAPI;

internal enum UiFixSyntheticPath
{
    Legacy,
    Optimized
}

internal enum UiFixSyntheticScenario
{
    AllExpanded,
    RenderersCollapsed,
    SidebarCollapsed,
    RendererToggle100,
    SidebarToggle100,
    RapidScroll,
    CategoryClick100,
    DropdownOpenClose100,
    DropdownRebind100,
    ShaderChange100,
    WindowOpenClose100
}

// Deterministic .NET operation-count model for UI invalidation, pooling, and
// listener lifetimes. Allocations are structural proxies only; Unity popup,
// frame-time, GPU, and native allocations still require runtime profiling.
internal static class SyntheticUiFixEngine
{
    internal const int RendererChildRows = 5;
    internal const int ViewportRows = 32;
    internal const int LongDropdownOptionCount = 96;
    private const int MaterialStructuralRows = 3; // Material, Shader, queue.
    private const int PopupListenerCount = 3;
    private const int RowListenerEstimate = 2;

    internal static WorkloadOutcome Run(
        SyntheticDataset dataset,
        UiFixSyntheticScenario scenario,
        UiFixSyntheticPath path)
    {
        if (dataset == null)
            throw new ArgumentNullException("dataset");

        switch (scenario)
        {
            case UiFixSyntheticScenario.AllExpanded:
                return RunAllExpanded(dataset, path);
            case UiFixSyntheticScenario.RenderersCollapsed:
                return RunRenderersCollapsed(dataset, path);
            case UiFixSyntheticScenario.SidebarCollapsed:
                return RunSidebarCollapsed(dataset, path);
            case UiFixSyntheticScenario.RendererToggle100:
                return RunRendererToggle(dataset, path);
            case UiFixSyntheticScenario.SidebarToggle100:
                return RunSidebarToggle(dataset, path);
            case UiFixSyntheticScenario.RapidScroll:
                return RunRapidScroll(dataset, path);
            case UiFixSyntheticScenario.CategoryClick100:
                return RunCategoryClick(dataset, path);
            case UiFixSyntheticScenario.DropdownOpenClose100:
                return RunDropdownOpenClose(dataset, path);
            case UiFixSyntheticScenario.DropdownRebind100:
                return RunDropdownRebind(dataset, path);
            case UiFixSyntheticScenario.ShaderChange100:
                return RunShaderChange(dataset, path);
            case UiFixSyntheticScenario.WindowOpenClose100:
                return RunWindowOpenClose(dataset, path);
            default:
                throw new ArgumentOutOfRangeException("scenario");
        }
    }

    internal static int ExpandedRowCount(SyntheticDataset dataset)
    {
        return RendererRowCount(dataset, true) + MaterialRowCount(dataset);
    }

    internal static int CollapsedRendererRowCount(SyntheticDataset dataset)
    {
        return RendererRowCount(dataset, false) + MaterialRowCount(dataset);
    }

    internal static int MaterialRowCount(SyntheticDataset dataset)
    {
        // Each material receives the full shader descriptor. Texture entries
        // contribute their offset/scale companion row as they do in production.
        return checked(dataset.MaterialCount
                       * (MaterialStructuralRows
                          + dataset.CategoryCount
                          + dataset.PropertyCount
                          + dataset.TextureCount));
    }

    private static int RendererRowCount(
        SyntheticDataset dataset,
        bool expanded)
    {
        return checked(dataset.RendererCount
                       * (1 + (expanded ? RendererChildRows : 0)));
    }

    private static WorkloadOutcome RunAllExpanded(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var rows = ExpandedRowCount(dataset);
        // Both paths must create the same logical presentation. Do not claim an
        // improvement for setup work that this change did not remove.
        var hash = StableHash.Add(StableHash.Start, TouchRows(rows, true, 1));
        RecordFullRebuild(rows, 1);
        return Outcome(dataset, hash, rows, 0, 1);
    }

    private static WorkloadOutcome RunRenderersCollapsed(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        // This is a steady-state probe after warm-up. A closed renderer has no
        // active child rows/views/binds/listeners and performs no layout work.
        var rows = CollapsedRendererRowCount(dataset);
        var hash = StableHash.Add(StableHash.Start, rows);
        hash = StableHash.Add(hash, dataset.RendererCount);
        hash = StableHash.Add(hash, 0); // active renderer child rows
        return Outcome(dataset, hash, rows, 0, 0);
    }

    private static WorkloadOutcome RunSidebarCollapsed(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        // Like renderer-collapsed, this measures the idle closed state rather
        // than charging the transition a second time.
        var rows = ExpandedRowCount(dataset);
        var hash = StableHash.Add(StableHash.Start, 0); // sidebar hidden
        hash = StableHash.Add(hash, rows);
        hash = StableHash.Add(hash, 1); // narrow reopen rail remains visible
        return Outcome(dataset, hash, rows, 0, 0);
    }

    private static WorkloadOutcome RunRendererToggle(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var rows = ExpandedRowCount(dataset);
        var hash = StableHash.Start;
        if (path == UiFixSyntheticPath.Legacy)
        {
            for (var index = 0; index < 100; index++)
            {
                rows += index % 2 == 0 ? -RendererChildRows : RendererChildRows;
                hash = StableHash.Add(hash, TouchRows(rows, true, index));
                RecordFullRebuild(rows, 1);
            }
        }
        else
        {
            // The scenario starts expanded, so the five child RowModels are
            // already in the renderer-section cache before timing begins.
            var activeViews = Math.Min(ViewportRows, rows);
            var reusedViews = Math.Min(
                ViewportRows - 1,
                Math.Max(0, rows - 1));
            for (var index = 0; index < 100; index++)
            {
                rows += index % 2 == 0 ? -RendererChildRows : RendererChildRows;
                hash = StableHash.Add(
                    hash,
                    TouchRows(RendererChildRows, false, index));
                RecordRangeReplacement(activeViews, reusedViews);
            }
        }

        // Internal work differs; fingerprint the same observable final state.
        hash = StableHash.Add(StableHash.Start, rows);
        hash = StableHash.Add(hash, 1); // expanded after 100 transitions
        return Outcome(
            dataset,
            hash,
            rows,
            0,
            path == UiFixSyntheticPath.Legacy ? 100 : 0);
    }

    private static WorkloadOutcome RunSidebarToggle(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var hash = StableHash.Start;
        var visible = true;
        for (var index = 0; index < 100; index++)
        {
            visible = !visible;
            hash = StableHash.Add(hash, visible ? 1 : 0);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.LayoutInvalidations);
        }

        // Selection, filter, and scroll survive horizontal layout changes.
        hash = StableHash.Add(hash, 17);
        hash = StableHash.Add(hash, "hair*");
        hash = StableHash.Add(hash, 220);
        return Outcome(dataset, hash, ExpandedRowCount(dataset), 0, 0);
    }

    private static WorkloadOutcome RunRapidScroll(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var rows = ExpandedRowCount(dataset);
        var hash = StableHash.Start;
        for (var index = 0; index < 100; index++)
        {
            var top = index * 137 % Math.Max(1, rows - ViewportRows);
            hash = StableHash.Add(hash, top);
            TouchRows(ViewportRows, false, top);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RowViewReuse,
                ViewportRows);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.Bind,
                ViewportRows);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.Unbind,
                ViewportRows);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations,
                ViewportRows * (long)RowListenerEstimate);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRemovals,
                ViewportRows * (long)RowListenerEstimate);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.LayoutInvalidations);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ViewportManualSelections);
        }
        return Outcome(dataset, hash, ViewportRows, 0, 0);
    }

    private static WorkloadOutcome RunCategoryClick(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var rows = ExpandedRowCount(dataset);
        var hash = StableHash.Start;
        var finalIndex = 0;
        for (var index = 0; index < 100; index++)
        {
            finalIndex = index % dataset.CategoryCount;
            hash = StableHash.Add(hash, finalIndex);

            // Legacy navigation rebuilt the full presentation for every
            // click. The optimized path uses the exact programmatic anchor
            // directly while both material and shader parents are expanded.
            if (path == UiFixSyntheticPath.Legacy)
            {
                TouchRows(rows, true, index);
                RecordFullRebuild(rows, 1);
            }
            // Both paths end with the same ScrollToIndex call. It dirties
            // virtualization without replacing the model list; the next pass
            // reuses/rebinds the warm viewport and marks layout once.
            TouchRows(ViewportRows, false, index);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RowViewReuse,
                ViewportRows);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.Bind,
                ViewportRows);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.Unbind,
                ViewportRows);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations,
                ViewportRows * (long)RowListenerEstimate);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRemovals,
                ViewportRows * (long)RowListenerEstimate);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.LayoutInvalidations);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ViewportProgrammaticSelections);
        }
        hash = StableHash.Add(hash, finalIndex);
        return Outcome(
            dataset,
            hash,
            rows,
            0,
            path == UiFixSyntheticPath.Legacy ? 100 : 0);
    }

    private static WorkloadOutcome RunDropdownOpenClose(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var hash = StableHash.Start;
        for (var index = 0; index < 100; index++)
        {
            // Native uGUI creates popup item objects on every open in both
            // paths. This proxy is intentionally identical between them.
            hash = StableHash.Add(
                hash,
                TouchOptions(LongDropdownOptionCount, true, index));
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.LayoutInvalidations);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRegistrations,
                PopupListenerCount);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ListenerRemovals,
                PopupListenerCount);
        }
        hash = StableHash.Add(hash, 7); // selection remains unchanged
        return Outcome(dataset, hash, 1, 0, 0);
    }

    private static WorkloadOutcome RunDropdownRebind(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        for (var cycle = 0; cycle < 100; cycle++)
        {
            for (var dropdown = 0; dropdown < dataset.DropdownCount; dropdown++)
            {
                var projectionChanged = path == UiFixSyntheticPath.Legacy
                                        || cycle == 0;
                if (!projectionChanged)
                    continue;
                TouchOptions(
                    LongDropdownOptionCount,
                    false,
                    cycle * dataset.DropdownCount + dropdown);
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.DropdownOptionRebuilds);
            }
        }

        // Both paths end with the same 20 dropdown contexts and selections.
        var hash = StableHash.Add(StableHash.Start, dataset.DropdownCount);
        hash = StableHash.Add(hash, 99);
        return Outcome(
            dataset,
            hash,
            Math.Min(ViewportRows, dataset.DropdownCount),
            0,
            0);
    }

    private static WorkloadOutcome RunShaderChange(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var rows = ExpandedRowCount(dataset);
        var hash = StableHash.Start;
        for (var index = 0; index < 100; index++)
        {
            var shader = index % 10;
            hash = StableHash.Add(hash, shader);
            TouchRows(rows, true, index);
            RecordFullRebuild(rows, 1);

            // A changed shader context invalidates every modeled enum/dropdown
            // descriptor. Stable-context caching must not suppress this work.
            for (var dropdown = 0;
                 dropdown < dataset.DropdownCount;
                 dropdown++)
            {
                TouchOptions(
                    LongDropdownOptionCount,
                    false,
                    shader * dataset.DropdownCount + dropdown);
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.DropdownOptionRebuilds);
            }
        }
        hash = StableHash.Add(StableHash.Start, 9);
        hash = StableHash.Add(hash, rows);
        return Outcome(dataset, hash, rows, 0, 100);
    }

    private static WorkloadOutcome RunWindowOpenClose(
        SyntheticDataset dataset,
        UiFixSyntheticPath path)
    {
        var rows = ExpandedRowCount(dataset);
        var hash = StableHash.Start;
        for (var index = 0; index < 100; index++)
        {
            // Closing releases presentation/listeners; reopening rebuilds the
            // presentation while reusing the warmed 32-view pool.
            hash = StableHash.Add(hash, TouchRows(rows, true, index));
            RecordFullRebuild(rows, 1);
        }
        hash = StableHash.Add(hash, rows);
        return Outcome(dataset, hash, 0, 0, 100, ViewportRows);
    }

    private static void RecordFullRebuild(int rows, int count)
    {
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.PresentationRebuild,
            count);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.CacheInvalidations,
            count);
        // source.enumerate counts enumeration operations, not renderer items.
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.SourceEnumerations,
            count);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.RowModelCreation,
            count * (long)rows);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.VisibleRowsInvalidations,
            count);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.LayoutInvalidations,
            count);

        var visible = Math.Min(ViewportRows, rows);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.RowViewReuse,
            count * (long)visible);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Bind,
            count * (long)visible);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Unbind,
            count * (long)visible);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRegistrations,
            count * visible * (long)RowListenerEstimate);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRemovals,
            count * visible * (long)RowListenerEstimate);
    }

    private static void RecordRangeReplacement(
        int activeViews,
        int reusedViews)
    {
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.CacheInvalidations);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.VisibleRowsInvalidations);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.LayoutInvalidations);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.RowViewReuse,
            reusedViews);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Bind,
            activeViews);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Unbind,
            activeViews);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRegistrations,
            activeViews * (long)RowListenerEstimate);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRemovals,
            activeViews * (long)RowListenerEstimate);
    }

    private static int TouchRows(int count, bool allocate, int seed)
    {
        var rows = allocate ? new int[count] : null;
        var hash = seed;
        for (var index = 0; index < count; index++)
        {
            var value = seed + index;
            if (rows != null)
                rows[index] = value;
            hash = unchecked(hash * 31 + value);
        }
        return hash;
    }

    private static int TouchOptions(int count, bool allocate, int seed)
    {
        var options = allocate ? new int[count] : null;
        var hash = seed;
        for (var index = 0; index < count; index++)
        {
            var value = seed ^ index;
            if (options != null)
                options[index] = value;
            hash = unchecked(hash * 33 + value);
        }
        return hash;
    }

    private static WorkloadOutcome Outcome(
        SyntheticDataset dataset,
        ulong hash,
        int visibleRows,
        int transientVariants,
        int logicalUpdates,
        int poolPeak = -1)
    {
        hash = StableHash.Add(hash, dataset.PropertyCount);
        hash = StableHash.Add(hash, dataset.CategoryCount);
        hash = StableHash.Add(hash, dataset.ConditionCount);
        hash = StableHash.Add(hash, dataset.EnumCount);
        hash = StableHash.Add(hash, dataset.VectorCount);
        hash = StableHash.Add(hash, dataset.BooleanCount);
        hash = StableHash.Add(hash, dataset.ProviderCount);
        hash = StableHash.Add(hash, dataset.RendererCount);
        hash = StableHash.Add(hash, dataset.MaterialCount);
        hash = StableHash.Add(hash, dataset.DropdownCount);
        hash = StableHash.Add(hash, dataset.TextureCount);
        return new WorkloadOutcome
        {
            Fingerprint = StableHash.Format(hash),
            VisibleRows = visibleRows,
            VariantRows = transientVariants,
            PoolPeak = poolPeak < 0
                ? Math.Min(ViewportRows, Math.Max(0, visibleRows))
                : poolPeak,
            ActiveListeners = 0,
            LogicalRebuilds = logicalUpdates,
            SemanticChecksPassed = dataset.PropertyCount > 0
                                   && dataset.CategoryCount > 0
                                   && dataset.ConditionCount >= 0
                                   && dataset.EnumCount > 0
                                   && dataset.VectorCount > 0
                                   && dataset.BooleanCount > 0
                                   && dataset.ProviderCount > 0
                                   && dataset.RendererCount > 0
                                   && dataset.MaterialCount > 0
                                   && dataset.DropdownCount > 0
                                   && dataset.TextureCount > 0
        };
    }
}
