using MaterialEditorAPI;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal sealed class BenchmarkResult
{
    public string Name { get; set; }
    public int Iterations { get; set; }
    public double MedianMilliseconds { get; set; }
    public double P95Milliseconds { get; set; }
    public long MedianAllocatedBytes { get; set; }
    public long P95AllocatedBytes { get; set; }
    public long ApproximateMemoryBytes { get; set; }
    public long RetainedMemoryDeltaBytes { get; set; }
    public string OutcomeFingerprint { get; set; }
    public int VisibleRows { get; set; }
    public int MixedRows { get; set; }
    public int MissingRows { get; set; }
    public int VariantRows { get; set; }
    public int PoolPeak { get; set; }
    public int ActiveListeners { get; set; }
    public int ProviderRegistrations { get; set; }
    public int LogicalRebuilds { get; set; }
    public bool SemanticChecksPassed { get; set; }
    public bool BaselineIndependent { get; set; }
    public Dictionary<string, long> Counters { get; set; } =
        new Dictionary<string, long>(StringComparer.Ordinal);

    internal long Counter(string name)
    {
        long value;
        return Counters.TryGetValue(name, out value) ? value : 0L;
    }
}

internal sealed class InvariantResult
{
    public string Name { get; set; }
    public bool Passed { get; set; }
    public string Detail { get; set; }
}

internal sealed class HarnessReport
{
    public int SchemaVersion { get; set; } = 2;
    public string Strategy { get; set; } =
        "linked-production-helpers-plus-synthetic-ui-operation-count-model";
    public string Runtime { get; set; }
    public DateTime GeneratedUtc { get; set; }
    public string SemanticFingerprint { get; set; }
    public List<BenchmarkResult> Results { get; set; } = new List<BenchmarkResult>();
    public List<InvariantResult> Invariants { get; set; } = new List<InvariantResult>();
}

internal static class BenchmarkRunner
{
    internal static BenchmarkResult Measure(
        string name,
        int warmupIterations,
        int iterations,
        Func<WorkloadOutcome> operation,
        bool baselineIndependent = false)
    {
        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        for (var index = 0; index < warmupIterations; index++)
            operation();
        SyntheticEngine.ResetOptimizationState();

        var elapsed = new double[iterations];
        var allocated = new long[iterations];
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var retainedMemoryBefore = GC.GetTotalMemory(true);
        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(
            diagnosticsEnabled: true,
            countersEnabled: true,
            logThresholdMilliseconds: double.MaxValue,
            slowSample: null,
            profiler: null);

        WorkloadOutcome outcome = null;
        for (var index = 0; index < iterations; index++)
        {
            var allocationStart = GC.GetAllocatedBytesForCurrentThread();
            var startedAt = Stopwatch.GetTimestamp();
            outcome = operation();
            var endedAt = Stopwatch.GetTimestamp();
            allocated[index] = Math.Max(
                0L,
                GC.GetAllocatedBytesForCurrentThread() - allocationStart);
            elapsed[index] = (endedAt - startedAt) * 1000d / Stopwatch.Frequency;
        }

        // The timing/allocation sample arrays were allocated before the first
        // forced collection, so this delta describes objects retained by the
        // measured operation after warm-up rather than total process heap.
        var retainedMemoryAfter = GC.GetTotalMemory(true);
        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        var result = new BenchmarkResult
        {
            Name = name,
            Iterations = iterations,
            MedianMilliseconds = Percentile(elapsed, 0.50d),
            P95Milliseconds = Percentile(elapsed, 0.95d),
            MedianAllocatedBytes = Percentile(allocated, 0.50d),
            P95AllocatedBytes = Percentile(allocated, 0.95d),
            ApproximateMemoryBytes = snapshot.TotalMemory,
            RetainedMemoryDeltaBytes =
                retainedMemoryAfter - retainedMemoryBefore,
            OutcomeFingerprint = outcome == null ? string.Empty : outcome.Fingerprint,
            VisibleRows = outcome == null ? 0 : outcome.VisibleRows,
            MixedRows = outcome == null ? 0 : outcome.MixedRows,
            MissingRows = outcome == null ? 0 : outcome.MissingRows,
            VariantRows = outcome == null ? 0 : outcome.VariantRows,
            PoolPeak = outcome == null ? 0 : outcome.PoolPeak,
            ActiveListeners = outcome == null ? 0 : outcome.ActiveListeners,
            ProviderRegistrations = outcome == null ? 0 : outcome.ProviderRegistrations,
            LogicalRebuilds = outcome == null ? 0 : outcome.LogicalRebuilds,
            SemanticChecksPassed = outcome != null && outcome.SemanticChecksPassed,
            BaselineIndependent = baselineIndependent
        };

        foreach (MaterialEditorPerformanceMetric metric in Enum.GetValues(
                     typeof(MaterialEditorPerformanceMetric)))
        {
            if (metric == MaterialEditorPerformanceMetric.Count)
                continue;
            var count = snapshot.GetCount(metric);
            if (count != 0L)
                result.Counters[MaterialEditorPerformance.GetMetricName(metric)] = count;
        }
        return result;
    }

    private static double Percentile(double[] source, double percentile)
    {
        var copy = (double[])source.Clone();
        Array.Sort(copy);
        var index = Math.Max(
            0,
            Math.Min(copy.Length - 1, (int)Math.Ceiling(copy.Length * percentile) - 1));
        return copy[index];
    }

    private static long Percentile(long[] source, double percentile)
    {
        var copy = (long[])source.Clone();
        Array.Sort(copy);
        var index = Math.Max(
            0,
            Math.Min(copy.Length - 1, (int)Math.Ceiling(copy.Length * percentile) - 1));
        return copy[index];
    }
}

internal static class HarnessReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static void Complete(HarnessReport report)
    {
        report.Runtime = Environment.Version.ToString();
        report.GeneratedUtc = DateTime.UtcNow;
        var semantic = new StringBuilder();
        foreach (var result in report.Results.OrderBy(item => item.Name))
        {
            semantic.Append(result.Name)
                .Append('|')
                .Append(result.OutcomeFingerprint)
                .Append('|')
                .Append(result.SemanticChecksPassed)
                .Append('\n');
        }
        foreach (var invariant in report.Invariants.OrderBy(item => item.Name))
            semantic.Append(invariant.Name).Append('|').Append(invariant.Passed).Append('\n');
        using (var sha = SHA256.Create())
        {
            var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(semantic.ToString()));
            report.SemanticFingerprint = BitConverter.ToString(digest)
                .Replace("-", string.Empty);
        }
    }

    internal static void Print(HarnessReport report)
    {
        Console.WriteLine(
            "Scenario                              Median ms   P95 ms   Median alloc   P95 alloc   Retained delta B   Parse Rebuild Models Views Provider Condition Mixed Listeners");
        foreach (var result in report.Results)
        {
            Console.WriteLine(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0,-36} {1,9:F3} {2,8:F3} {3,14} {4,11} {5,13} {6,5} {7,7} {8,6} {9,5} {10,8} {11,9} {12,5} {13,9}",
                    result.Name,
                    result.MedianMilliseconds,
                    result.P95Milliseconds,
                    result.MedianAllocatedBytes,
                    result.P95AllocatedBytes,
                    result.RetainedMemoryDeltaBytes,
                    result.Counter("manifest.parse"),
                    result.Counter("presentation.rebuild"),
                    result.Counter("row-model.create"),
                    result.Counter("row-view.create"),
                    result.Counter("provider.call"),
                    result.Counter("condition.evaluate"),
                    result.Counter("mixed.compare"),
                    result.Counter("listener.add") - result.Counter("listener.remove")));
        }
        Console.WriteLine();
        foreach (var invariant in report.Invariants)
            Console.WriteLine(
                (invariant.Passed ? "PASS " : "FAIL ")
                + invariant.Name
                + " - "
                + invariant.Detail);
        Console.WriteLine("Semantic fingerprint: " + report.SemanticFingerprint);
    }

    internal static void Save(HarnessReport report, string path)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(fullPath, JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine("Harness JSON: " + fullPath);
    }

    internal static HarnessReport Load(string path)
    {
        return JsonSerializer.Deserialize<HarnessReport>(File.ReadAllText(path), JsonOptions);
    }

    internal static bool CompareSemanticBaseline(
        HarnessReport current,
        HarnessReport baseline)
    {
        if (baseline == null)
        {
            Console.Error.WriteLine("Baseline JSON could not be read.");
            return false;
        }

        var passed = true;
        if (current.SchemaVersion != 2)
        {
            Console.Error.WriteLine(
                "Unsupported current harness schema: " + current.SchemaVersion);
            passed = false;
        }
        if (baseline.SchemaVersion < 1 || baseline.SchemaVersion > 2)
        {
            Console.Error.WriteLine(
                "Unsupported baseline harness schema: " + baseline.SchemaVersion);
            passed = false;
        }
        if (current.Results == null || baseline.Results == null)
        {
            Console.Error.WriteLine("Current or baseline results are missing.");
            return false;
        }

        passed &= ValidateUniqueScenarioNames(current.Results, "current");
        passed &= ValidateUniqueScenarioNames(baseline.Results, "baseline");
        if (!passed)
            return false;

        var currentByName = current.Results.ToDictionary(
            item => item.Name,
            StringComparer.Ordinal);
        var baselineByName = baseline.Results.ToDictionary(
            item => item.Name,
            StringComparer.Ordinal);

        // Comparison is deliberately bidirectional: additive scenarios may be
        // opted out below, but a frozen baseline scenario may never disappear.
        foreach (var previous in baseline.Results)
        {
            if (currentByName.ContainsKey(previous.Name))
                continue;
            Console.Error.WriteLine(
                "Current report is missing baseline scenario: " + previous.Name);
            passed = false;
        }

        foreach (var result in current.Results)
        {
            BenchmarkResult previous;
            if (!baselineByName.TryGetValue(result.Name, out previous))
            {
                if (result.BaselineIndependent)
                {
                    Console.WriteLine("NEW baseline-independent scenario: " + result.Name);
                    continue;
                }
                Console.Error.WriteLine("Baseline is missing scenario: " + result.Name);
                passed = false;
                continue;
            }
            if (!StringComparer.Ordinal.Equals(
                    previous.OutcomeFingerprint,
                    result.OutcomeFingerprint))
            {
                Console.Error.WriteLine("Semantic fingerprint changed: " + result.Name);
                passed = false;
            }
            Console.WriteLine(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "BASELINE {0}: median {1:F3}->{2:F3} ms, P95 alloc {3}->{4} bytes",
                    result.Name,
                    previous.MedianMilliseconds,
                    result.MedianMilliseconds,
                    previous.P95AllocatedBytes,
                    result.P95AllocatedBytes));
        }
        return passed;
    }

    private static bool ValidateUniqueScenarioNames(
        IList<BenchmarkResult> results,
        string reportName)
    {
        var passed = true;
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var result in results)
        {
            if (result == null || string.IsNullOrEmpty(result.Name))
            {
                Console.Error.WriteLine(
                    reportName + " report contains a result without a name.");
                passed = false;
                continue;
            }
            if (names.Add(result.Name))
                continue;
            Console.Error.WriteLine(
                reportName + " report contains duplicate scenario: " + result.Name);
            passed = false;
        }
        return passed;
    }
}
