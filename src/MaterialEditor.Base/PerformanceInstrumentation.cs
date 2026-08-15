using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MaterialEditorAPI
{
    // Internal-only development metrics. Append new entries before Count so
    // snapshots remain straightforward to compare within one build.
    internal enum MaterialEditorPerformanceMetric
    {
        ManifestParsing,
        MetadataNormalization,
        PropertyOrganization,
        Filtering,
        SourceEnumerations,
        FilterMatchCalls,
        FilterPatternBuilds,
        Search,
        CategoryRebuild,
        PresentationRebuild,
        RowModelCreation,
        RowViewCreation,
        RowViewReuse,
        Bind,
        Unbind,
        ConditionalEvaluation,
        ConditionSourceKindBuilds,
        ConditionResolveCalls,
        ConditionMaterialReads,
        ConditionCacheHits,
        ProviderSnapshotBuilds,
        ProviderCalls,
        EditorFactoryCalls,
        ProviderEnumeratedDescriptors,
        ProviderRegistrations,
        ProviderRemovals,
        MixedStateCopies,
        MixedComponentsCopied,
        MixedCalculations,
        MixedComparisons,
        PropertyToIdCalls,
        TextureHashing,
        TextureEncoding,
        Save,
        Load,
        CacheInvalidations,
        ListenerRegistrations,
        ListenerRemovals,
        RefreshRequests,
        RefreshCoalesced,
        RefreshExecuted,
        VisibleRowsInvalidations,
        LayoutInvalidations,
        DropdownOptionRebuilds,
        ViewportProgrammaticSelections,
        ViewportManualSelections,
        CubemapCacheHits,
        CubemapCacheMisses,
        CubemapImportDecode,
        CubemapImportSampling,
        CubemapExport,
        Count
    }

    internal interface IMaterialEditorPerformanceProfiler
    {
        void BeginSample(MaterialEditorPerformanceMetric metric);
        void EndSample();
    }

    internal sealed class MaterialEditorPerformanceSnapshot
    {
        private readonly long[] _counts;
        private readonly long[] _elapsedTicks;

        internal MaterialEditorPerformanceSnapshot(
            long[] counts,
            long[] elapsedTicks,
            long totalMemory,
            int gen0Collections,
            int gen1Collections,
            int gen2Collections)
        {
            _counts = counts;
            _elapsedTicks = elapsedTicks;
            TotalMemory = totalMemory;
            Gen0Collections = gen0Collections;
            Gen1Collections = gen1Collections;
            Gen2Collections = gen2Collections;
        }

        internal long TotalMemory { get; private set; }
        internal int Gen0Collections { get; private set; }
        internal int Gen1Collections { get; private set; }
        internal int Gen2Collections { get; private set; }

        internal long GetCount(MaterialEditorPerformanceMetric metric)
        {
            return _counts[(int)metric];
        }

        internal long GetElapsedTicks(MaterialEditorPerformanceMetric metric)
        {
            return _elapsedTicks[(int)metric];
        }

        internal double GetElapsedMilliseconds(MaterialEditorPerformanceMetric metric)
        {
            return GetElapsedTicks(metric) * 1000d / Stopwatch.Frequency;
        }
    }

    internal static class MaterialEditorPerformance
    {
        private const int CountersMode = 1;
        private const int TimingMode = 2;
        private static readonly int MetricCount =
            (int)MaterialEditorPerformanceMetric.Count;
        private static readonly long[] Counts = new long[MetricCount];
        private static readonly long[] ElapsedTicks = new long[MetricCount];

        private static volatile int _mode;
        private static double _logThresholdMilliseconds = 5d;
        private static Action<MaterialEditorPerformanceMetric, double> _slowSample;
        private static IMaterialEditorPerformanceProfiler _profiler;

        internal static bool Enabled
        {
            get { return _mode != 0; }
        }

        internal static void Configure(
            bool diagnosticsEnabled,
            bool countersEnabled,
            double logThresholdMilliseconds,
            Action<MaterialEditorPerformanceMetric, double> slowSample,
            IMaterialEditorPerformanceProfiler profiler)
        {
            _logThresholdMilliseconds = logThresholdMilliseconds < 0d
                ? 0d
                : logThresholdMilliseconds;
            _slowSample = diagnosticsEnabled ? slowSample : null;
            _profiler = diagnosticsEnabled ? profiler : null;
            _mode = (countersEnabled ? CountersMode : 0)
                    | (diagnosticsEnabled ? TimingMode : 0);
        }

        // Disabled: one volatile branch, then return. Stopwatch, strings,
        // profiler APIs, arrays, and Interlocked are not touched.
        internal static bool TryStart(
            MaterialEditorPerformanceMetric metric,
            out long startedAt)
        {
            var mode = _mode;
            if (mode == 0)
            {
                startedAt = 0L;
                return false;
            }

            Interlocked.Increment(ref Counts[(int)metric]);
            if ((mode & TimingMode) == 0)
            {
                startedAt = 0L;
                return true;
            }
            var profiler = _profiler;
            if (profiler != null)
                profiler.BeginSample(metric);
            startedAt = Stopwatch.GetTimestamp();
            return true;
        }

        internal static long Start(MaterialEditorPerformanceMetric metric)
        {
            long startedAt;
            TryStart(metric, out startedAt);
            return startedAt;
        }

        internal static void Stop(
            MaterialEditorPerformanceMetric metric,
            long startedAt)
        {
            if (startedAt == 0L)
                return;

            var elapsed = Stopwatch.GetTimestamp() - startedAt;
            Interlocked.Add(ref ElapsedTicks[(int)metric], elapsed);

            var profiler = _profiler;
            if (profiler != null)
                profiler.EndSample();

            var slowSample = _slowSample;
            if (slowSample == null)
                return;
            var elapsedMilliseconds = elapsed * 1000d / Stopwatch.Frequency;
            if (elapsedMilliseconds >= _logThresholdMilliseconds)
                slowSample(metric, elapsedMilliseconds);
        }

        internal static void Increment(
            MaterialEditorPerformanceMetric metric,
            long amount = 1L)
        {
            if (_mode == 0)
                return;
            Interlocked.Add(ref Counts[(int)metric], amount);
        }

        internal static MaterialEditorPerformanceSnapshot CaptureSnapshot(
            bool forceCollection = false)
        {
            var counts = new long[MetricCount];
            var elapsedTicks = new long[MetricCount];
            for (var index = 0; index < MetricCount; index++)
            {
                counts[index] = Interlocked.Read(ref Counts[index]);
                elapsedTicks[index] = Interlocked.Read(ref ElapsedTicks[index]);
            }

            return new MaterialEditorPerformanceSnapshot(
                counts,
                elapsedTicks,
                GC.GetTotalMemory(forceCollection),
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2));
        }

        internal static void Reset()
        {
            for (var index = 0; index < MetricCount; index++)
            {
                Interlocked.Exchange(ref Counts[index], 0L);
                Interlocked.Exchange(ref ElapsedTicks[index], 0L);
            }
        }

        internal static string GetMetricName(MaterialEditorPerformanceMetric metric)
        {
            switch (metric)
            {
                case MaterialEditorPerformanceMetric.ManifestParsing: return "manifest.parse";
                case MaterialEditorPerformanceMetric.MetadataNormalization: return "metadata.normalize";
                case MaterialEditorPerformanceMetric.PropertyOrganization: return "property.organize";
                case MaterialEditorPerformanceMetric.Filtering: return "filter";
                case MaterialEditorPerformanceMetric.SourceEnumerations: return "source.enumerate";
                case MaterialEditorPerformanceMetric.FilterMatchCalls: return "filter.matches";
                case MaterialEditorPerformanceMetric.FilterPatternBuilds: return "filter.pattern";
                case MaterialEditorPerformanceMetric.Search: return "search";
                case MaterialEditorPerformanceMetric.CategoryRebuild: return "category.rebuild";
                case MaterialEditorPerformanceMetric.PresentationRebuild: return "presentation.rebuild";
                case MaterialEditorPerformanceMetric.RowModelCreation: return "row-model.create";
                case MaterialEditorPerformanceMetric.RowViewCreation: return "row-view.create";
                case MaterialEditorPerformanceMetric.RowViewReuse: return "row-view.reuse";
                case MaterialEditorPerformanceMetric.Bind: return "row.bind";
                case MaterialEditorPerformanceMetric.Unbind: return "row.unbind";
                case MaterialEditorPerformanceMetric.ConditionalEvaluation: return "condition.evaluate";
                case MaterialEditorPerformanceMetric.ConditionSourceKindBuilds: return "condition.sources";
                case MaterialEditorPerformanceMetric.ConditionResolveCalls: return "condition.resolve";
                case MaterialEditorPerformanceMetric.ConditionMaterialReads: return "condition.material-read";
                case MaterialEditorPerformanceMetric.ConditionCacheHits: return "condition.cache-hit";
                case MaterialEditorPerformanceMetric.ProviderSnapshotBuilds: return "provider.snapshot";
                case MaterialEditorPerformanceMetric.ProviderCalls: return "provider.call";
                case MaterialEditorPerformanceMetric.EditorFactoryCalls: return "editor-factory.call";
                case MaterialEditorPerformanceMetric.ProviderEnumeratedDescriptors: return "provider.descriptors";
                case MaterialEditorPerformanceMetric.ProviderRegistrations: return "provider.register";
                case MaterialEditorPerformanceMetric.ProviderRemovals: return "provider.remove";
                case MaterialEditorPerformanceMetric.MixedStateCopies: return "mixed-state.copy";
                case MaterialEditorPerformanceMetric.MixedComponentsCopied: return "mixed-component.copy";
                case MaterialEditorPerformanceMetric.MixedCalculations: return "mixed.calculate";
                case MaterialEditorPerformanceMetric.MixedComparisons: return "mixed.compare";
                case MaterialEditorPerformanceMetric.PropertyToIdCalls: return "property-to-id";
                case MaterialEditorPerformanceMetric.TextureHashing: return "texture.hash";
                case MaterialEditorPerformanceMetric.TextureEncoding: return "texture.encode";
                case MaterialEditorPerformanceMetric.Save: return "save";
                case MaterialEditorPerformanceMetric.Load: return "load";
                case MaterialEditorPerformanceMetric.CacheInvalidations: return "cache.invalidate";
                case MaterialEditorPerformanceMetric.ListenerRegistrations: return "listener.add";
                case MaterialEditorPerformanceMetric.ListenerRemovals: return "listener.remove";
                case MaterialEditorPerformanceMetric.RefreshRequests: return "refresh.request";
                case MaterialEditorPerformanceMetric.RefreshCoalesced: return "refresh.coalesced";
                case MaterialEditorPerformanceMetric.RefreshExecuted: return "refresh.executed";
                case MaterialEditorPerformanceMetric.VisibleRowsInvalidations: return "visible-rows.invalidate";
                case MaterialEditorPerformanceMetric.LayoutInvalidations: return "layout.invalidate";
                case MaterialEditorPerformanceMetric.DropdownOptionRebuilds: return "dropdown-options.rebuild";
                case MaterialEditorPerformanceMetric.ViewportProgrammaticSelections: return "viewport.programmatic-selection";
                case MaterialEditorPerformanceMetric.ViewportManualSelections: return "viewport.manual-selection";
                case MaterialEditorPerformanceMetric.CubemapCacheHits: return "cubemap.cache-hit";
                case MaterialEditorPerformanceMetric.CubemapCacheMisses: return "cubemap.cache-miss";
                case MaterialEditorPerformanceMetric.CubemapImportDecode: return "cubemap.import-decode";
                case MaterialEditorPerformanceMetric.CubemapImportSampling: return "cubemap.import-sampling";
                case MaterialEditorPerformanceMetric.CubemapExport: return "cubemap.export";
                default: return "unknown";
            }
        }
    }

    internal static class MaterialEditorMixedStatePerformance
    {
        internal static int GetComponentCopyCount(
            IList<bool> source,
            int destinationLength)
        {
            if (source == null)
                return 0;
            return Math.Min(source.Count, destinationLength);
        }

        internal static bool HasMixed(bool[] values, int count)
        {
            long startedAt;
            if (!MaterialEditorPerformance.TryStart(
                    MaterialEditorPerformanceMetric.MixedCalculations,
                    out startedAt))
            {
                for (var index = 0; index < count; index++)
                    if (values[index])
                        return true;
                return false;
            }

            var comparisons = 0;
            try
            {
                for (var index = 0; index < count; index++)
                {
                    var isMixed = values[index];
                    comparisons++;
                    if (isMixed)
                        return true;
                }
                return false;
            }
            finally
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.MixedComparisons,
                    comparisons);
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.MixedCalculations,
                    startedAt);
            }
        }
    }
}
