using MaterialEditorAPI;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            string jsonPath = null;
            string saveBaselinePath = null;
            string compareBaselinePath = null;
            for (var index = 0; index < args.Length; index++)
            {
                var argument = args[index];
                if (argument == "--json")
                    jsonPath = RequireValue(args, ref index, argument);
                else if (argument == "--save-baseline")
                    saveBaselinePath = RequireValue(args, ref index, argument);
                else if (argument == "--compare-baseline")
                    compareBaselinePath = RequireValue(args, ref index, argument);
                else
                    throw new ArgumentException("Unknown harness argument: " + argument);
            }

            var report = RunHarness();
            HarnessReportWriter.Complete(report);
            HarnessReportWriter.Print(report);

            if (!string.IsNullOrEmpty(jsonPath))
                HarnessReportWriter.Save(report, jsonPath);
            if (!string.IsNullOrEmpty(saveBaselinePath))
                HarnessReportWriter.Save(report, saveBaselinePath);

            var passed = report.Invariants.All(item => item.Passed);
            if (!string.IsNullOrEmpty(compareBaselinePath))
            {
                var baseline = HarnessReportWriter.Load(compareBaselinePath);
                passed &= HarnessReportWriter.CompareSemanticBaseline(report, baseline);
            }

            Console.WriteLine(passed
                ? "Material Editor performance harness passed."
                : "Material Editor performance harness failed.");
            return passed ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        }
    }

    private static HarnessReport RunHarness()
    {
        var simple = SyntheticDatasetFactory.Simple();
        var kklt = SyntheticDatasetFactory.KkltLike();
        MaterialEditorPerformance.Configure(false, false, 5d, null, null);
        var simpleProperties = SyntheticEngine.ParseManifest(simple);
        var kkltProperties = SyntheticEngine.ParseManifest(kklt);

        var report = new HarnessReport();
        report.Results.Add(BenchmarkRunner.Measure(
            "simple.parse",
            warmupIterations: 2,
            iterations: 15,
            () =>
            {
                var properties = SyntheticEngine.ParseManifest(simple);
                return SyntheticEngine.DescribeParse(simple, properties);
            }));
        report.Results.Add(BenchmarkRunner.Measure(
            "simple.rebuild",
            warmupIterations: 2,
            iterations: 20,
            () => SyntheticEngine.Rebuild(
                simple,
                simpleProperties,
                string.Empty,
                1,
                MultiTargetShape.Equal)));
        report.Results.Add(BenchmarkRunner.Measure(
            "kklt.parse",
            warmupIterations: 1,
            iterations: 10,
            () =>
            {
                var properties = SyntheticEngine.ParseManifest(kklt);
                return SyntheticEngine.DescribeParse(kklt, properties);
            }));
        report.Results.Add(BenchmarkRunner.Measure(
            "kklt.rebuild.full.first",
            warmupIterations: 2,
            iterations: 15,
            () => SyntheticEngine.Rebuild(
                kklt,
                kkltProperties,
                string.Empty,
                1,
                MultiTargetShape.Equal)));
        report.Results.Add(BenchmarkRunner.Measure(
            "kklt.rebuild.full.repeat",
            warmupIterations: 2,
            iterations: 15,
            () => SyntheticEngine.Rebuild(
                kklt,
                kkltProperties,
                string.Empty,
                1,
                MultiTargetShape.Equal)));
        report.Results.Add(BenchmarkRunner.Measure(
            "kklt.search",
            warmupIterations: 2,
            iterations: 15,
            () => SyntheticEngine.Rebuild(
                kklt,
                kkltProperties,
                "Property 1",
                1,
                MultiTargetShape.Equal)));

        var targetCounts = new[] { 1, 5, 20, 100 };
        var shapes = new[]
        {
            MultiTargetShape.Equal,
            MultiTargetShape.Mixed,
            MultiTargetShape.Missing,
            MultiTargetShape.Variants
        };
        foreach (var targetCount in targetCounts)
        foreach (var shape in shapes)
        {
            var capturedTargetCount = targetCount;
            var capturedShape = shape;
            report.Results.Add(BenchmarkRunner.Measure(
                "multi."
                + capturedTargetCount
                + "."
                + capturedShape.ToString().ToLowerInvariant(),
                warmupIterations: 1,
                iterations: 6,
                () => SyntheticEngine.Rebuild(
                    kklt,
                    kkltProperties,
                    string.Empty,
                    capturedTargetCount,
                    capturedShape)));
        }

        report.Results.Add(BenchmarkRunner.Measure(
            "longevity",
            warmupIterations: 0,
            iterations: 1,
            () => SyntheticEngine.RunLongevity(kklt)));

        AddUiFixBenchmarks(report, kklt);

        AddInvariants(report, simple, simpleProperties, kklt, kkltProperties);
        return report;
    }

    private static void AddUiFixBenchmarks(
        HarnessReport report,
        SyntheticDataset dataset)
    {
        var scenarios = new[]
        {
            UiFixSyntheticScenario.AllExpanded,
            UiFixSyntheticScenario.RenderersCollapsed,
            UiFixSyntheticScenario.SidebarCollapsed,
            UiFixSyntheticScenario.RendererToggle100,
            UiFixSyntheticScenario.SidebarToggle100,
            UiFixSyntheticScenario.RapidScroll,
            UiFixSyntheticScenario.CategoryClick100,
            UiFixSyntheticScenario.DropdownOpenClose100,
            UiFixSyntheticScenario.DropdownRebind100,
            UiFixSyntheticScenario.ShaderChange100,
            UiFixSyntheticScenario.WindowOpenClose100
        };
        var paths = new[]
        {
            UiFixSyntheticPath.Legacy,
            UiFixSyntheticPath.Optimized
        };

        foreach (var scenario in scenarios)
        foreach (var path in paths)
        {
            var capturedScenario = scenario;
            var capturedPath = path;
            report.Results.Add(BenchmarkRunner.Measure(
                "ui-fixes."
                + UiFixScenarioName(capturedScenario)
                + "."
                + capturedPath.ToString().ToLowerInvariant(),
                warmupIterations: 1,
                iterations: 8,
                () => SyntheticUiFixEngine.Run(
                    dataset,
                    capturedScenario,
                    capturedPath),
                baselineIndependent: true));
        }
    }

    private static string UiFixScenarioName(UiFixSyntheticScenario scenario)
    {
        switch (scenario)
        {
            case UiFixSyntheticScenario.AllExpanded:
                return "all-expanded";
            case UiFixSyntheticScenario.RenderersCollapsed:
                return "renderers-collapsed";
            case UiFixSyntheticScenario.SidebarCollapsed:
                return "sidebar-collapsed";
            case UiFixSyntheticScenario.RendererToggle100:
                return "renderer-toggle-100";
            case UiFixSyntheticScenario.SidebarToggle100:
                return "sidebar-toggle-100";
            case UiFixSyntheticScenario.RapidScroll:
                return "rapid-scroll";
            case UiFixSyntheticScenario.CategoryClick100:
                return "category-click-100";
            case UiFixSyntheticScenario.DropdownOpenClose100:
                return "dropdown-open-close-100";
            case UiFixSyntheticScenario.DropdownRebind100:
                return "dropdown-rebind-100";
            case UiFixSyntheticScenario.ShaderChange100:
                return "shader-change-100";
            case UiFixSyntheticScenario.WindowOpenClose100:
                return "window-open-close-100";
            default:
                throw new ArgumentOutOfRangeException("scenario");
        }
    }

    private static void AddInvariants(
        HarnessReport report,
        SyntheticDataset simple,
        IList<SyntheticProperty> simpleProperties,
        SyntheticDataset kklt,
        IList<SyntheticProperty> kkltProperties)
    {
        AddInvariant(
            report,
            "simple dataset shape",
            SyntheticEngine.DescribeParse(simple, simpleProperties).SemanticChecksPassed,
            "20 properties, 4 categories, one material");
        AddInvariant(
            report,
            "KKLT-like dataset shape",
            SyntheticEngine.DescribeParse(kklt, kkltProperties).SemanticChecksPassed,
            "250 properties, 30 categories, 40 conditions, 20 enums, 10 vectors, 15 booleans, tooltips, and hidden rows");
        AddInvariant(
            report,
            "scenario semantic fingerprints",
            report.Results.All(item => item.SemanticChecksPassed),
            "every measured operation preserved its deterministic outcome");
        AddInvariant(
            report,
            "listener balance",
            report.Results.All(item =>
                item.Counter("listener.add") == item.Counter("listener.remove")),
            "registrations equal removals after each complete scenario");
        AddInvariant(
            report,
            "bounded synthetic row pool",
            report.Results.All(item => item.PoolPeak <= 32),
            "no scenario grows beyond the 32-view viewport pool");

        var propertyId = PropertyIdHarness.Measure();
        AddInvariant(
            report,
            "PropertyToID semantic differential",
            propertyId.SameIds,
            "legacy and cached paths resolve the same 250 exact prefixed names");
        AddInvariant(
            report,
            "PropertyToID bounded resolver calls",
            propertyId.LegacyResolverCalls == 100250
            && propertyId.CachedResolverCalls == 250,
            "100k KKLT-like accesses: legacy="
            + propertyId.LegacyResolverCalls
            + ", cached="
            + propertyId.CachedResolverCalls);
        AddInvariant(
            report,
            "PropertyToID allocation reduction",
            propertyId.CachedAllocatedBytes < propertyId.LegacyAllocatedBytes,
            "100k KKLT-like accesses: legacy="
            + propertyId.LegacyAllocatedBytes
            + " B, cached="
            + propertyId.CachedAllocatedBytes
            + " B; benchmark is outside scenario fingerprints");

        var rebuild = Find(report, "kklt.rebuild.full.repeat");
        AddInvariant(
            report,
            "current source enumeration baseline",
            rebuild.Counter("source.enumerate") == rebuild.Iterations,
            "materialized renderer/projector sources are reused by each rebuild");
        AddInvariant(
            report,
            "generation-cached provider snapshot",
            rebuild.Counter("provider.snapshot") == 1L
            && rebuild.Counter("provider.call") ==
            rebuild.Iterations * kklt.ProviderCount,
            "one sorted snapshot is built for the stable provider generation; dynamic providers still run P*M times");
        AddInvariant(
            report,
            "per-material condition value cache",
            rebuild.Counter("condition.evaluate") ==
            rebuild.Iterations * kklt.ConditionCount
            && rebuild.Counter("condition.material-read") ==
            rebuild.Iterations * 5L
            && rebuild.Counter("condition.cache-hit") ==
            rebuild.Iterations * (kklt.ConditionCount - 5L),
            "40 condition evaluations remain, while five unique sources are read once per rebuild");

        var search = Find(report, "kklt.search");
        AddInvariant(
            report,
            "one search pattern per rebuild term",
            search.Counter("filter.pattern") == search.Iterations,
            "the production-linked wildcard Regex is prepared once for the single term in each rebuild");

        var rendererTerms = new List<string>();
        var propertyTerms = new List<string>();
        MaterialEditorFilter.Parse(
            " Hair*, _Main?Tex, [abc], A+B, ___ ",
            rendererTerms,
            propertyTerms);
        var preparedLiteral = MaterialEditorFilter.Prepare("[abc]");
        var preparedWildcard = MaterialEditorFilter.Prepare("main?tex");
        AddInvariant(
            report,
            "legacy search differential",
            rendererTerms.SequenceEqual(new[] { "Hair*", "[abc]", "A+B" })
            && propertyTerms.SequenceEqual(new[] { "Main?Tex" })
            && MaterialEditorFilter.Matches("HAIR_01", "hair*")
            && MaterialEditorFilter.Matches("MainXTex", "main?tex")
            && MaterialEditorFilter.Matches("prefix[abc]suffix", "[abc]")
            && MaterialEditorFilter.Matches("value A+B suffix", "a+b")
            && !MaterialEditorFilter.Matches("MainLongTex", "Main?Tex")
            && preparedLiteral.Matches("prefix[abc]suffix")
            && !preparedLiteral.Matches("prefixasuffix")
            && preparedWildcard.Matches("MainXTex")
            && !preparedWildcard.Matches("MainLongTex"),
            "production filter preserves comma/_ tokenization, *, ?, case-insensitivity, and Regex escaping");

        foreach (var targetCount in new[] { 1, 5, 20, 100 })
        {
            var equal = Find(report, "multi." + targetCount + ".equal");
            var mixed = Find(report, "multi." + targetCount + ".mixed");
            var missing = Find(report, "multi." + targetCount + ".missing");
            var variants = Find(report, "multi." + targetCount + ".variants");
            var expectedSingle = targetCount == 1;
            AddInvariant(
                report,
                "multi-target semantics " + targetCount,
                equal.MixedRows == 0
                && (expectedSingle || mixed.MixedRows > 0)
                && (expectedSingle || missing.MissingRows > 0)
                && (expectedSingle || variants.VariantRows > 0),
                "equal, Mixed, absent-property, and shader-variant inputs remain distinguishable");
        }

        var multiHundred = Find(report, "multi.100.equal");
        AddInvariant(
            report,
            "provider P*M lifecycle with generation snapshot",
            multiHundred.Counter("provider.snapshot") == 1L
            && multiHundred.Counter("provider.call") ==
            multiHundred.Iterations * 100L * kklt.ProviderCount,
            "one stable-generation snapshot serves 100 materials while every provider still runs for every material");

        var longevity = Find(report, "longevity");
        AddInvariant(
            report,
            "longevity operation counts",
            longevity.LogicalRebuilds == 500
            && longevity.Counter("presentation.rebuild") == 500L
            && longevity.Counter("cache.invalidate") == 100L,
            "100 open/close + 200 material + 100 refresh + 100 search; 100 conditional invalidations");
        AddInvariant(
            report,
            "provider registration lifetime",
            longevity.Counter("provider.register") == 100L
            && longevity.Counter("provider.remove") == 100L
            && longevity.ProviderRegistrations == 0,
            "100 provider registrations are fully released");
        AddInvariant(
            report,
            "longevity listener lifetime",
            longevity.Counter("listener.add") == longevity.Counter("listener.remove")
            && longevity.ActiveListeners == 0,
            "500 explicit bind/unbind cycles and all rebuild listeners are released");

        AddUiFixInvariants(report, kklt);
    }

    private static void AddUiFixInvariants(
        HarnessReport report,
        SyntheticDataset dataset)
    {
        var expectedMaterialRows = dataset.MaterialCount
                                   * (3
                                      + dataset.CategoryCount
                                      + dataset.PropertyCount
                                      + dataset.TextureCount);
        var expectedExpandedRows = expectedMaterialRows
                                   + dataset.RendererCount
                                   * (SyntheticUiFixEngine.RendererChildRows + 1);
        var expectedCollapsedRendererRows = expectedMaterialRows
                                            + dataset.RendererCount;
        AddInvariant(
            report,
            "UI-fix stress dataset shape",
            dataset.PropertyCount == 250
            && dataset.CategoryCount == 30
            && dataset.ConditionCount == 40
            && dataset.EnumCount == 20
            && dataset.VectorCount == 10
            && dataset.BooleanCount == 15
            && dataset.RendererCount == 20
            && dataset.MaterialCount == 50
            && dataset.DropdownCount == 20
            && dataset.TextureCount == 15
            && SyntheticUiFixEngine.MaterialRowCount(dataset)
                == expectedMaterialRows
            && SyntheticUiFixEngine.ExpandedRowCount(dataset)
                == expectedExpandedRows
            && SyntheticUiFixEngine.CollapsedRendererRowCount(dataset)
                == expectedCollapsedRendererRows,
            "50 material blocks each model the 250-property descriptor (including conditions, 20 enums, vectors, and booleans), 30 categories, and 15 texture companion rows; 20 renderers are separate");

        var uiResults = report.Results
            .Where(item => item.Name.StartsWith("ui-fixes.", StringComparison.Ordinal))
            .ToList();
        var baselineScenarioNames = new[]
        {
            "simple.parse",
            "simple.rebuild",
            "kklt.parse",
            "kklt.rebuild.full.first",
            "kklt.rebuild.full.repeat",
            "kklt.search",
            "multi.1.equal",
            "multi.1.mixed",
            "multi.1.missing",
            "multi.1.variants",
            "multi.5.equal",
            "multi.5.mixed",
            "multi.5.missing",
            "multi.5.variants",
            "multi.20.equal",
            "multi.20.mixed",
            "multi.20.missing",
            "multi.20.variants",
            "multi.100.equal",
            "multi.100.mixed",
            "multi.100.missing",
            "multi.100.variants",
            "longevity"
        };
        var baselineResults = report.Results
            .Where(item => !item.Name.StartsWith(
                "ui-fixes.",
                StringComparison.Ordinal))
            .ToList();
        AddInvariant(
            report,
            "UI-fix scenarios are additive to the frozen baseline",
            uiResults.Count == 22
            && uiResults.All(item => item.BaselineIndependent)
            && baselineResults.Count == baselineScenarioNames.Length
            && baselineScenarioNames.All(name => baselineResults.Any(
                item => StringComparer.Ordinal.Equals(item.Name, name)))
            && baselineResults.All(item => !item.BaselineIndependent)
            && report.Results.Select(item => item.Name)
                .Distinct(StringComparer.Ordinal).Count() == report.Results.Count,
            "eleven legacy/optimized pairs are additive; all 23 unique frozen scenarios remain mandatory");

        var scenarioNames = new[]
        {
            "all-expanded",
            "renderers-collapsed",
            "sidebar-collapsed",
            "renderer-toggle-100",
            "sidebar-toggle-100",
            "rapid-scroll",
            "category-click-100",
            "dropdown-open-close-100",
            "dropdown-rebind-100",
            "shader-change-100",
            "window-open-close-100"
        };
        foreach (var scenarioName in scenarioNames)
        {
            var legacy = Find(report, "ui-fixes." + scenarioName + ".legacy");
            var optimized = Find(report, "ui-fixes." + scenarioName + ".optimized");
            AddInvariant(
                report,
                "UI-fix semantic differential " + scenarioName,
                legacy.OutcomeFingerprint == optimized.OutcomeFingerprint
                && legacy.ActiveListeners == 0
                && optimized.ActiveListeners == 0,
                "legacy and targeted paths finish in the same deterministic UI state with no retained listeners");
        }

        var allExpanded = Find(report, "ui-fixes.all-expanded.optimized");
        AddInvariant(
            report,
            "all-expanded models every material block and reuses RowViews",
            allExpanded.VisibleRows == expectedExpandedRows
            && allExpanded.Counter("row-model.create")
                == allExpanded.Iterations * (long)expectedExpandedRows
            && allExpanded.Counter("row-view.create") == 0L
            && allExpanded.Counter("row-view.reuse")
                == allExpanded.Iterations
                   * (long)SyntheticUiFixEngine.ViewportRows,
            "the warm 32-view pool is reused while all 50 material descriptor blocks contribute logical rows");

        var renderersCollapsed = Find(
            report,
            "ui-fixes.renderers-collapsed.optimized");
        AddInvariant(
            report,
            "closed renderers are idle and have no active child work",
            renderersCollapsed.VisibleRows == expectedCollapsedRendererRows
            && renderersCollapsed.Counter("row-model.create") == 0L
            && renderersCollapsed.Counter("row-view.create") == 0L
            && renderersCollapsed.Counter("row.bind") == 0L
            && renderersCollapsed.Counter("listener.add") == 0L
            && renderersCollapsed.Counter("layout.invalidate") == 0L
            && renderersCollapsed.Counter("visible-rows.invalidate") == 0L,
            "the steady closed state excludes 100 renderer child rows and performs no per-frame work");

        var sidebarCollapsed = Find(
            report,
            "ui-fixes.sidebar-collapsed.optimized");
        AddInvariant(
            report,
            "closed sidebar steady state is idle",
            sidebarCollapsed.Counter("layout.invalidate") == 0L
            && sidebarCollapsed.Counter("presentation.rebuild") == 0L
            && sidebarCollapsed.Counter("row.bind") == 0L
            && sidebarCollapsed.Counter("listener.add") == 0L,
            "the collapsed rail retains state without repeating its transition work");

        var rendererLegacy = Find(report, "ui-fixes.renderer-toggle-100.legacy");
        var rendererOptimized = Find(report, "ui-fixes.renderer-toggle-100.optimized");
        AddInvariant(
            report,
            "renderer collapse uses targeted visible-row replacement",
            rendererLegacy.Counter("presentation.rebuild")
                == rendererLegacy.Iterations * 100L
            && rendererLegacy.Counter("source.enumerate")
                == rendererLegacy.Iterations * 100L
            && rendererOptimized.Counter("presentation.rebuild") == 0L
            && rendererOptimized.Counter("source.enumerate") == 0L
            && rendererOptimized.Counter("visible-rows.invalidate")
                == rendererOptimized.Iterations * 100L
            && rendererLegacy.Counter("cache.invalidate")
                == rendererLegacy.Iterations * 100L
            && rendererOptimized.Counter("cache.invalidate")
                == rendererOptimized.Iterations * 100L
            && rendererOptimized.Counter("layout.invalidate")
                == rendererOptimized.Iterations * 100L
            && rendererOptimized.Counter("row-model.create") == 0L
            && rendererOptimized.Counter("row-view.create") == 0L
            && rendererOptimized.Counter("row-view.reuse")
                == rendererOptimized.Iterations * 100L
                   * (SyntheticUiFixEngine.ViewportRows - 1L)
            && rendererOptimized.Counter("row.bind")
                == rendererOptimized.Iterations * 100L
                   * SyntheticUiFixEngine.ViewportRows
            && rendererOptimized.Counter("listener.add")
                == rendererOptimized.Counter("listener.remove"),
            "100 transitions reuse cached child models and the warmed viewport; source.enumerate counts operations, not renderer items");

        var sidebarLegacy = Find(report, "ui-fixes.sidebar-toggle-100.legacy");
        var sidebarOptimized = Find(report, "ui-fixes.sidebar-toggle-100.optimized");
        AddInvariant(
            report,
            "sidebar collapse performs one layout invalidation per toggle",
            sidebarLegacy.Counter("layout.invalidate")
                == sidebarLegacy.Iterations * 100L
            && sidebarOptimized.Counter("layout.invalidate")
                == sidebarOptimized.Iterations * 100L
            && sidebarLegacy.Counter("presentation.rebuild") == 0L
            && sidebarOptimized.Counter("presentation.rebuild") == 0L,
            "both implementations toggle through one layout pass; the new state bridge avoids double initialization without inventing a toggle-path gain");

        var categoryLegacy = Find(report, "ui-fixes.category-click-100.legacy");
        var categoryOptimized = Find(report, "ui-fixes.category-click-100.optimized");
        AddInvariant(
            report,
            "category navigation uses targeted viewport work",
            categoryLegacy.Counter("viewport.programmatic-selection")
                == categoryLegacy.Iterations * 100L
            && categoryOptimized.Counter("viewport.programmatic-selection")
                == categoryOptimized.Iterations * 100L
            && categoryLegacy.Counter("presentation.rebuild")
                == categoryLegacy.Iterations * 100L
            && categoryOptimized.Counter("presentation.rebuild") == 0L
            && categoryOptimized.Counter("source.enumerate") == 0L
            && categoryOptimized.Counter("visible-rows.invalidate") == 0L
            && categoryOptimized.Counter("cache.invalidate") == 0L
            && categoryOptimized.Counter("layout.invalidate")
                == categoryOptimized.Iterations * 100L
            && categoryLegacy.Counter("layout.invalidate")
                == categoryLegacy.Iterations * 200L
            && categoryOptimized.Counter("row-view.reuse")
                == categoryOptimized.Iterations * 100L
                   * SyntheticUiFixEngine.ViewportRows
            && categoryOptimized.Counter("row.bind")
                == categoryOptimized.Iterations * 100L
                   * SyntheticUiFixEngine.ViewportRows
            && categoryOptimized.Counter("listener.add")
                == categoryOptimized.Counter("listener.remove")
            && categoryOptimized.Counter("refresh.request") == 0L
            && categoryOptimized.Counter("viewport.manual-selection") == 0L,
            "100 already-expanded category clicks publish their requested anchors and rebind only the warm viewport; no presentation, source, row-cache, visible-row, or refresh work is hidden");

        var dropdownOpenLegacy = Find(
            report,
            "ui-fixes.dropdown-open-close-100.legacy");
        var dropdownOpenOptimized = Find(
            report,
            "ui-fixes.dropdown-open-close-100.optimized");
        AddInvariant(
            report,
            "native dropdown open-close keeps projection stable and listeners balanced",
            dropdownOpenLegacy.Counter("dropdown-options.rebuild") == 0L
            && dropdownOpenOptimized.Counter("dropdown-options.rebuild") == 0L
            && dropdownOpenLegacy.Counter("listener.add")
                == dropdownOpenLegacy.Iterations * 100L * 3L
            && dropdownOpenOptimized.Counter("listener.add")
                == dropdownOpenOptimized.Iterations * 100L * 3L
            && dropdownOpenOptimized.Counter("listener.remove")
                == dropdownOpenOptimized.Iterations * 100L * 3L,
            "uGUI clones one popup per open; each clone owns three listeners which are removed on close, while OptionData projection is not rerun");

        var dropdownRebindLegacy = Find(
            report,
            "ui-fixes.dropdown-rebind-100.legacy");
        var dropdownRebindOptimized = Find(
            report,
            "ui-fixes.dropdown-rebind-100.optimized");
        AddInvariant(
            report,
            "stable dropdown rebinds reuse twenty cached projections",
            dropdownRebindLegacy.Counter("dropdown-options.rebuild")
                == dropdownRebindLegacy.Iterations
                   * 100L * dataset.DropdownCount
            && dropdownRebindOptimized.Counter("dropdown-options.rebuild")
                == dropdownRebindOptimized.Iterations
                   * (long)dataset.DropdownCount
            && dropdownRebindOptimized.Counter("listener.add") == 0L,
            "option projection is measured on rebind, separately from native popup open-close lifecycle");

        var scroll = Find(report, "ui-fixes.rapid-scroll.optimized");
        AddInvariant(
            report,
            "rapid scroll remains pooled and manual",
            scroll.Counter("viewport.manual-selection")
                == scroll.Iterations * 100L
            && scroll.Counter("presentation.rebuild") == 0L
            && scroll.Counter("dropdown-options.rebuild") == 0L
            && scroll.Counter("layout.invalidate")
                == scroll.Iterations * 100L
            && scroll.Counter("row-view.create") == 0L
            && scroll.Counter("row-view.reuse")
                == scroll.Iterations * 100L
                   * SyntheticUiFixEngine.ViewportRows
            && scroll.Counter("listener.add") == scroll.Counter("listener.remove"),
            "100 viewport moves bind a bounded pool without rebuilding presentation or dropdown options");

        var shader = Find(report, "ui-fixes.shader-change-100.optimized");
        AddInvariant(
            report,
            "shader changes invalidate all modeled dropdown contexts",
            shader.Counter("presentation.rebuild")
                == shader.Iterations * 100L
            && shader.Counter("source.enumerate")
                == shader.Iterations * 100L
            && shader.Counter("dropdown-options.rebuild")
                == shader.Iterations * 100L * dataset.DropdownCount,
            "a real shader-context change rebuilds each of the 20 dropdown projections; stable-context caching is not misapplied");

        var window = Find(report, "ui-fixes.window-open-close-100.optimized");
        AddInvariant(
            report,
            "window lifecycle listener balance",
            window.Counter("listener.add") == window.Counter("listener.remove")
            && window.ActiveListeners == 0
            && window.Counter("presentation.rebuild")
                == window.Iterations * 100L
            && window.Counter("source.enumerate")
                == window.Iterations * 100L
            && window.Counter("row-model.create")
                == window.Iterations * 100L * expectedExpandedRows
            && window.Counter("row-view.create") == 0L
            && window.Counter("row-view.reuse")
                == window.Iterations * 100L
                   * SyntheticUiFixEngine.ViewportRows,
            "100 reopen cycles rebuild logical rows, reuse the warmed 32-view pool, and leave no active listeners");
    }

    private static BenchmarkResult Find(HarnessReport report, string name)
    {
        return report.Results.Single(item => StringComparer.Ordinal.Equals(item.Name, name));
    }

    private static void AddInvariant(
        HarnessReport report,
        string name,
        bool passed,
        string detail)
    {
        report.Invariants.Add(new InvariantResult
        {
            Name = name,
            Passed = passed,
            Detail = detail
        });
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        index++;
        if (index >= args.Length)
            throw new ArgumentException(option + " requires a path.");
        return args[index];
    }
}
