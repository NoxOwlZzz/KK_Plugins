using MaterialEditorAPI;

internal static class PerformanceInstrumentationTests
{
    internal static void Run()
    {
        DisabledFastPathDoesNotRecordOrProfile();
        TryStartPreservesCounterAndTimingSemantics();
        MixedStateCountersTrackActualReads();
        MixedComponentCopyCountIsExact();
        CounterOnlyModeDoesNotStartTimingOrProfiler();
        DiagnosticsProduceBalancedSnapshotsAndProfilerSamples();
        ConfigurationDefaultsAreStructurallyPinned();
        HarnessSourceHasNoMojibake();
        Console.WriteLine("Performance instrumentation regression tests passed.");
    }

    private static void DisabledFastPathDoesNotRecordOrProfile()
    {
        var profiler = new RecordingProfiler();
        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(false, false, 5d, null, profiler);

        for (var index = 0; index < 100; index++)
            ExerciseDisabledFastPath();
        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10000; index++)
            ExerciseDisabledFastPath();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        var startedAt = MaterialEditorPerformance.Start(
            MaterialEditorPerformanceMetric.ManifestParsing);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.RowModelCreation,
            12L);
        MaterialEditorPerformance.Stop(
            MaterialEditorPerformanceMetric.ManifestParsing,
            startedAt);

        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(0L, startedAt, "disabled Start sentinel");
        Equal(0L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.ManifestParsing), "disabled sample count");
        Equal(0L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.RowModelCreation), "disabled explicit count");
        Equal(0L, snapshot.GetElapsedTicks(
            MaterialEditorPerformanceMetric.ManifestParsing), "disabled elapsed ticks");
        Equal(0, profiler.BeginCount, "disabled profiler begins");
        Equal(0, profiler.EndCount, "disabled profiler ends");
        Equal(0L, allocated, "disabled fast-path allocations");
    }

    private static void ExerciseDisabledFastPath()
    {
        long startedAt;
        if (MaterialEditorPerformance.TryStart(
                MaterialEditorPerformanceMetric.Search,
                out startedAt))
            MaterialEditorPerformance.Stop(
                MaterialEditorPerformanceMetric.Search,
                startedAt);
    }

    private static void TryStartPreservesCounterAndTimingSemantics()
    {
        var profiler = new RecordingProfiler();
        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(false, false, 5d, null, profiler);
        long startedAt;
        Equal(false, MaterialEditorPerformance.TryStart(
            MaterialEditorPerformanceMetric.Search,
            out startedAt), "disabled TryStart result");
        Equal(0L, startedAt, "disabled TryStart sentinel");

        MaterialEditorPerformance.Configure(false, true, 5d, null, profiler);
        Equal(true, MaterialEditorPerformance.TryStart(
            MaterialEditorPerformanceMetric.Search,
            out startedAt), "counter-only TryStart result");
        Equal(0L, startedAt, "counter-only TryStart sentinel");
        MaterialEditorPerformance.Stop(
            MaterialEditorPerformanceMetric.Search,
            startedAt);
        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(1L, snapshot.GetCount(MaterialEditorPerformanceMetric.Search),
            "counter-only TryStart count");
        Equal(0L, snapshot.GetElapsedTicks(MaterialEditorPerformanceMetric.Search),
            "counter-only TryStart elapsed ticks");
        Equal(0, profiler.BeginCount, "counter-only TryStart profiler begins");
        Equal(0, profiler.EndCount, "counter-only TryStart profiler ends");

        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(true, false, double.MaxValue, null, profiler);
        Equal(true, MaterialEditorPerformance.TryStart(
            MaterialEditorPerformanceMetric.Search,
            out startedAt), "timed TryStart result");
        MaterialEditorPerformance.Stop(
            MaterialEditorPerformanceMetric.Search,
            startedAt);
        snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(1L, snapshot.GetCount(MaterialEditorPerformanceMetric.Search),
            "timed TryStart count");
        Equal(true, snapshot.GetElapsedTicks(MaterialEditorPerformanceMetric.Search) >= 0L,
            "timed TryStart elapsed ticks");
        Equal(1, profiler.BeginCount, "timed TryStart profiler begins");
        Equal(1, profiler.EndCount, "timed TryStart profiler ends");

        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        MaterialEditorPerformance.Reset();
    }

    private static void MixedStateCountersTrackActualReads()
    {
        MaterialEditorPerformance.Configure(false, true, 5d, null, null);
        AssertMixedCase(
            new[] { false, false, false, false },
            false,
            4L,
            "four false flags");
        AssertMixedCase(
            new[] { true, false, false, false },
            true,
            1L,
            "first true flag");
        AssertMixedCase(
            new[] { false, true, false, false },
            true,
            2L,
            "second true flag");

        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        MaterialEditorPerformance.Reset();
        var disabledValues = new[] { false, false, false, false };
        for (var warmup = 0; warmup < 100; warmup++)
            MaterialEditorMixedStatePerformance.HasMixed(disabledValues, 4);
        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 10000; iteration++)
            MaterialEditorMixedStatePerformance.HasMixed(disabledValues, 4);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(0L, snapshot.GetCount(MaterialEditorPerformanceMetric.MixedCalculations),
            "disabled mixed calculation count");
        Equal(0L, snapshot.GetCount(MaterialEditorPerformanceMetric.MixedComparisons),
            "disabled mixed comparison count");
        Equal(0L, allocated, "disabled mixed fast-path allocations");
    }

    private static void AssertMixedCase(
        bool[] values,
        bool expected,
        long expectedComparisons,
        string name)
    {
        MaterialEditorPerformance.Reset();
        Equal(expected, MaterialEditorMixedStatePerformance.HasMixed(values, 4),
            name + " result");
        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(1L, snapshot.GetCount(MaterialEditorPerformanceMetric.MixedCalculations),
            name + " calculation count");
        Equal(expectedComparisons,
            snapshot.GetCount(MaterialEditorPerformanceMetric.MixedComparisons),
            name + " comparison count");
    }

    private static void MixedComponentCopyCountIsExact()
    {
        Equal(0, MaterialEditorMixedStatePerformance.GetComponentCopyCount(null, 4),
            "null mixed component source copy count");
        Equal(0, MaterialEditorMixedStatePerformance.GetComponentCopyCount(
            new List<bool>(), 4), "empty mixed component source copy count");
        Equal(2, MaterialEditorMixedStatePerformance.GetComponentCopyCount(
            new List<bool> { true, false }, 4), "short mixed component copy count");
        Equal(4, MaterialEditorMixedStatePerformance.GetComponentCopyCount(
            new List<bool> { true, false, true, false, true }, 4),
            "long mixed component copy count is clamped");

        MaterialEditorPerformance.Configure(false, true, 5d, null, null);
        MaterialEditorPerformance.Reset();
        var copyCount = MaterialEditorMixedStatePerformance.GetComponentCopyCount(
            new List<bool> { true, false, true },
            4);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.MixedComponentsCopied,
            copyCount);
        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(3L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.MixedComponentsCopied),
            "aggregate mixed component metric matches exact copy count");
        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        MaterialEditorPerformance.Reset();
    }

    private static void CounterOnlyModeDoesNotStartTimingOrProfiler()
    {
        var profiler = new RecordingProfiler();
        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(false, true, 5d, null, profiler);

        var startedAt = MaterialEditorPerformance.Start(
            MaterialEditorPerformanceMetric.ProviderCalls);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ProviderEnumeratedDescriptors,
            7L);
        MaterialEditorPerformance.Stop(
            MaterialEditorPerformanceMetric.ProviderCalls,
            startedAt);

        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(0L, startedAt, "counter-only Start sentinel");
        Equal(1L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.ProviderCalls), "counter-only call count");
        Equal(7L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.ProviderEnumeratedDescriptors),
            "counter-only descriptor count");
        Equal(0L, snapshot.GetElapsedTicks(
            MaterialEditorPerformanceMetric.ProviderCalls), "counter-only elapsed ticks");
        Equal(0, profiler.BeginCount, "counter-only profiler begins");
        Equal(0, profiler.EndCount, "counter-only profiler ends");
    }

    private static void DiagnosticsProduceBalancedSnapshotsAndProfilerSamples()
    {
        var profiler = new RecordingProfiler();
        var slowSamples = 0;
        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(
            true,
            false,
            0d,
            (metric, elapsed) => slowSamples++,
            profiler);

        var startedAt = MaterialEditorPerformance.Start(
            MaterialEditorPerformanceMetric.MetadataNormalization);
        MaterialEditorPerformance.Stop(
            MaterialEditorPerformanceMetric.MetadataNormalization,
            startedAt);

        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(1L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.MetadataNormalization),
            "diagnostic sample count");
        Equal(true, snapshot.GetElapsedTicks(
            MaterialEditorPerformanceMetric.MetadataNormalization) >= 0L,
            "diagnostic elapsed ticks available");
        Equal(1, profiler.BeginCount, "diagnostic profiler begins");
        Equal(1, profiler.EndCount, "diagnostic profiler ends");
        Equal(1, slowSamples, "zero-threshold slow sample callback");

        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        MaterialEditorPerformance.Reset();
    }

    private static void ConfigurationDefaultsAreStructurallyPinned()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "MaterialEditor.Base",
            "PluginBase.cs")).Replace("\r\n", "\n");
        Contains(source, "\"PerformanceDiagnostics\",\n                false", "diagnostics default");
        Contains(source, "\"PerformanceCountersEnabled\",\n                false", "counters default");
        Contains(source, "\"PerformanceLogThresholdMs\",\n                5f", "threshold default");
        Contains(source, "\"PerformanceSummaryOnClose\",\n                false", "summary default");
        Contains(
            source,
            "|| !MaterialEditorPerformance.Enabled)\n                return;\n\n            var snapshot = MaterialEditorPerformance.CaptureSnapshot();",
            "summary disabled allocation guard");

        var instrumentationSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "MaterialEditor.Base",
            "PerformanceInstrumentation.cs")).Replace("\r\n", "\n");
        var tryStart = ExtractMethod(
            instrumentationSource,
            "internal static bool TryStart(");
        Equal(1, CountOccurrences(tryStart, "_mode"),
            "TryStart performs one volatile mode read");
        Contains(
            tryStart,
            "var mode = _mode;\n            if (mode == 0)\n            {\n                startedAt = 0L;\n                return false;",
            "disabled TryStart fast path");
        var start = ExtractMethod(
            instrumentationSource,
            "internal static long Start(");
        Equal(0, CountOccurrences(start, "_mode"),
            "Start delegates without a second mode read");
        Contains(start, "TryStart(metric, out startedAt);",
            "Start preserves semantics through TryStart");

        var binderSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowBinder.EnumVectorToggle.cs")).Replace("\r\n", "\n");
        var binderHasMixed = ExtractMethod(
            binderSource,
            "private static bool HasMixed(");
        Contains(
            binderHasMixed,
            "MaterialEditorMixedStatePerformance.HasMixed(values, count)",
            "row binder uses the production-linked mixed-state measurement");

        var descriptorSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.PropertyDescriptor.cs")).Replace("\r\n", "\n");
        var copyMixed = ExtractMethod(
            descriptorSource,
            "private static bool[] CopyMixedComponents(");
        Equal(1, CountOccurrences(
            copyMixed,
            "MaterialEditorPerformanceMetric.MixedComponentsCopied"),
            "mixed component copy records one aggregate increment");
        Contains(
            copyMixed,
            "MaterialEditorPerformanceMetric.MixedComponentsCopied,\n                    copyCount);",
            "mixed component copy reports the exact copied count");
        Contains(copyMixed, "for (var index = 0; index < copyCount; index++)",
            "mixed component copy loops over the exact bounded count");
        Equal(1, CountOccurrences(
            ExtractMethod(instrumentationSource, "internal static bool HasMixed("),
            "MaterialEditorPerformanceMetric.MixedComparisons"),
            "mixed comparisons are recorded once after the loop");
    }

    private static void HarnessSourceHasNoMojibake()
    {
        var harnessDirectory = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "MaterialEditor.PerformanceHarness");
        foreach (var path in Directory.EnumerateFiles(harnessDirectory, "*.cs"))
        {
            var source = File.ReadAllText(path);
            if (source.IndexOf('\uFFFD') >= 0
                || source.IndexOf('\u00C3') >= 0
                || source.IndexOf('\u00C2') >= 0
                || source.IndexOf('\u00E2') >= 0)
            {
                throw new InvalidOperationException(
                    "Mojibake or a replacement character was found in " + path + ".");
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "src",
                    "MaterialEditor.Base",
                    "PluginBase.cs")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Repository root was not found.");
    }

    private static void Contains(string value, string expected, string name)
    {
        if (!value.Contains(expected, StringComparison.Ordinal))
            throw new InvalidOperationException(name + ": expected source fragment was not found.");
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

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }

    private sealed class RecordingProfiler : IMaterialEditorPerformanceProfiler
    {
        internal int BeginCount;
        internal int EndCount;

        public void BeginSample(MaterialEditorPerformanceMetric metric)
        {
            BeginCount++;
        }

        public void EndSample()
        {
            EndCount++;
        }
    }
}
