using MaterialEditorAPI;
using System.Text.RegularExpressions;

internal static class OptimizationRegressionTests
{
    internal static void Run()
    {
        PreparedSearchMatchesLegacyRegex();
        ConditionValuesAreCachedWithoutCachingFailures();
        ProviderSnapshotsPreserveOrderingCallsAndFailures();
        Console.WriteLine("Search/provider/condition optimization regression tests passed.");
    }

    private static void PreparedSearchMatchesLegacyRegex()
    {
        var filters = new[]
        {
            string.Empty,
            "*",
            "?",
            "hair*",
            "Main?Tex",
            "[abc]",
            "A+B",
            "(test)",
            "^start$",
            @"path\value",
            "Español"
        };
        var texts = new[]
        {
            string.Empty,
            "HAIR_01",
            "MainXTex",
            "MainLongTex",
            "prefix[abc]suffix",
            "value A+B suffix",
            "(TEST)",
            "^start$",
            @"path\value",
            "ESPAÑOL"
        };

        foreach (var filter in filters)
        {
            var prepared = MaterialEditorFilter.Prepare(filter);
            foreach (var text in texts)
                Equal(
                    LegacyMatches(text, filter),
                    prepared.Matches(text),
                    "prepared wildcard differential for '" + filter + "'");
        }

        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(false, true, 5d, null, null);
        var measured = MaterialEditorFilter.Prepare("Property*");
        for (var index = 0; index < 25; index++)
            measured.Matches("Property" + index);
        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(
            1L,
            snapshot.GetCount(MaterialEditorPerformanceMetric.FilterPatternBuilds),
            "one prepared search pattern");
        Equal(
            25L,
            snapshot.GetCount(MaterialEditorPerformanceMetric.FilterMatchCalls),
            "prepared search match calls");
        DisablePerformanceCounters();
    }

    private static void ConditionValuesAreCachedWithoutCachingFailures()
    {
        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(false, true, 5d, null, null);
        var reads = 0;
        var values = new MaterialEditorConditionValueCache(
            name =>
            {
                reads++;
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.ConditionMaterialReads);
                return name == "Source" ? 1f : (float?)null;
            },
            2);
        var sourceIsOne = new MaterialEditorPropertyCondition(
            "Source",
            MaterialEditorConditionComparison.Equal,
            1f);
        var sourceIsTwo = new MaterialEditorPropertyCondition(
            "Source",
            MaterialEditorConditionComparison.Equal,
            2f);
        var missing = new MaterialEditorPropertyCondition(
            "Missing",
            MaterialEditorConditionComparison.Equal,
            1f);

        Equal(true, MaterialEditorConditionPolicy.Evaluate(
            sourceIsOne, values.Resolve, true), "cached condition first value");
        Equal(false, MaterialEditorConditionPolicy.Evaluate(
            sourceIsTwo, values.Resolve, true), "cached condition repeated value");
        Equal(true, MaterialEditorConditionPolicy.Evaluate(
            sourceIsOne, values.Resolve, true), "cached condition third value");
        Equal(true, MaterialEditorConditionPolicy.Evaluate(
            missing, values.Resolve, true), "missing condition fail-open");
        Equal(true, MaterialEditorConditionPolicy.Evaluate(
            missing, values.Resolve, true), "cached missing condition fail-open");

        var snapshot = MaterialEditorPerformance.CaptureSnapshot();
        Equal(2, reads, "unique condition source reads");
        Equal(5L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.ConditionalEvaluation),
            "condition evaluation count preserved");
        Equal(5L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.ConditionResolveCalls),
            "condition resolve calls preserved");
        Equal(2L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.ConditionMaterialReads),
            "condition material reads reduced to unique sources");
        Equal(3L, snapshot.GetCount(
            MaterialEditorPerformanceMetric.ConditionCacheHits),
            "condition cache hits");
        DisablePerformanceCounters();

        var attempts = 0;
        var retrying = new MaterialEditorConditionValueCache(name =>
        {
            attempts++;
            if (attempts == 1)
                throw new InvalidOperationException("transient read");
            return 0f;
        });
        Equal(true, MaterialEditorConditionPolicy.Evaluate(
            sourceIsOne, retrying.Resolve, true), "condition exception fail-open");
        Equal(false, MaterialEditorConditionPolicy.Evaluate(
            sourceIsOne, retrying.Resolve, true), "condition exception retry");
        Equal(false, MaterialEditorConditionPolicy.Evaluate(
            sourceIsOne, retrying.Resolve, true), "condition successful retry cached");
        Equal(2, attempts, "condition exceptions are not cached");
    }

    private static void ProviderSnapshotsPreserveOrderingCallsAndFailures()
    {
        var calls = new List<string>();
        IDisposable low = null;
        IDisposable highFirst = null;
        IDisposable highSecond = null;
        IDisposable middle = null;
        IDisposable throwing = null;
        try
        {
            low = RegisterProvider("optimization.low", 0, "low", calls);
            highFirst = RegisterProvider(
                "optimization.high-first", 10, "high-first", calls);
            highSecond = RegisterProvider(
                "optimization.high-second", 10, "high-second", calls);
            var context = new MaterialEditorPropertyContext(null, "Material", "Shader");

            MaterialEditorPerformance.Reset();
            MaterialEditorPerformance.Configure(false, true, 5d, null, null);
            for (var material = 0; material < 4; material++)
            {
                var descriptors = MaterialEditorExtensionRegistry
                    .GetPropertyDescriptors(context);
                Equal(
                    "high-first,high-second,low",
                    string.Join(",", descriptors.Select(item => item.Id)),
                    "provider priority and sequence order");
            }
            Equal(
                "high-first,high-second,low," +
                "high-first,high-second,low," +
                "high-first,high-second,low," +
                "high-first,high-second,low",
                string.Join(",", calls),
                "provider P*M invocation order");

            var snapshot = MaterialEditorPerformance.CaptureSnapshot();
            Equal(1L, snapshot.GetCount(
                MaterialEditorPerformanceMetric.ProviderSnapshotBuilds),
                "one provider snapshot for stable generation");
            Equal(12L, snapshot.GetCount(
                MaterialEditorPerformanceMetric.ProviderCalls),
                "every provider called for every material");

            middle = RegisterProvider(
                "optimization.middle", 5, "middle", calls);
            var withMiddle = MaterialEditorExtensionRegistry
                .GetPropertyDescriptors(context);
            Equal(
                "high-first,high-second,middle,low",
                string.Join(",", withMiddle.Select(item => item.Id)),
                "provider registration invalidates generation");
            middle.Dispose();
            middle = null;
            var withoutMiddle = MaterialEditorExtensionRegistry
                .GetPropertyDescriptors(context);
            Equal(
                "high-first,high-second,low",
                string.Join(",", withoutMiddle.Select(item => item.Id)),
                "provider removal invalidates generation");

            throwing = MaterialEditorExtensionApi.RegisterPropertyDescriptorProvider(
                "optimization.throwing",
                ignored => YieldThenThrow(calls),
                100);
            var afterFailure = MaterialEditorExtensionRegistry
                .GetPropertyDescriptors(context);
            Equal(
                "high-first,high-second,low",
                string.Join(",", afterFailure.Select(item => item.Id)),
                "failed provider partial enumeration is atomic");
            Equal(
                "throwing,high-first,high-second,low",
                string.Join(",", calls.Skip(calls.Count - 4)),
                "failed provider does not stop lower-priority providers");

            snapshot = MaterialEditorPerformance.CaptureSnapshot();
            Equal(4L, snapshot.GetCount(
                MaterialEditorPerformanceMetric.ProviderSnapshotBuilds),
                "provider snapshot once per observed generation");
            Equal(23L, snapshot.GetCount(
                MaterialEditorPerformanceMetric.ProviderCalls),
                "provider call lifecycle remains P*M after invalidations");
            DisablePerformanceCounters();
        }
        finally
        {
            DisablePerformanceCounters();
            throwing?.Dispose();
            middle?.Dispose();
            highSecond?.Dispose();
            highFirst?.Dispose();
            low?.Dispose();
        }
    }

    private static IDisposable RegisterProvider(
        string owner,
        int priority,
        string id,
        ICollection<string> calls)
    {
        return MaterialEditorExtensionApi.RegisterPropertyDescriptorProvider(
            owner,
            ignored =>
            {
                calls.Add(id);
                return new[]
                {
                    new MaterialEditorPropertyDescriptor(
                        id,
                        id,
                        MaterialEditorPropertyEditorIds.Float)
                };
            },
            priority);
    }

    private static IEnumerable<MaterialEditorPropertyDescriptor> YieldThenThrow(
        ICollection<string> calls)
    {
        calls.Add("throwing");
        yield return new MaterialEditorPropertyDescriptor(
            "partial",
            "partial",
            MaterialEditorPropertyEditorIds.Float);
        throw new InvalidOperationException("deferred provider failure");
    }

    private static bool LegacyMatches(string text, string filter)
    {
        var regex =
            "^.*"
            + Regex.Escape(filter).Replace("\\?", ".").Replace("\\*", ".*")
            + ".*$";
        return Regex.IsMatch(text, regex, RegexOptions.IgnoreCase);
    }

    private static void DisablePerformanceCounters()
    {
        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        MaterialEditorPerformance.Reset();
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
