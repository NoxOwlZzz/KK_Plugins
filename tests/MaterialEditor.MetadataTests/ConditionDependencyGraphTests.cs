using MaterialEditorAPI;

internal static class ConditionDependencyGraphTests
{
    internal static void Run()
    {
        GroupedVisibilityUsesAnyAndFailOpenSemantics();
        GroupedGraphCachesOneSnapshotPerSource();
        InitialEvaluationIsReusedAndRuntimeVisitsDirectEdges();
        FailOpenMatchesExistingPolicyAndRetriesErrors();
        FailedVisibilityRebuildLeavesOldGraphRetryable();
        HandlesDeduplicateOnlyWithinTheirOwnerAndGraph();
        EvaluationRejectsValidOrdinalsFromWrongGraphOrOwner();
        SingleSourceEvaluationDoesNotScaleScratchWithGraphSize();
        SourceCatalogPreservesManifestAndProviderPrecedence();
        CoordinatorCoalescesFiveHundredHandlesAndSearchDominates();
        DiagnosticsAreBoundedAndWarningOnce();
        LongDiagnosticCycleDoesNotUseRecursiveTraversal();
    }

    private static void GroupedVisibilityUsesAnyAndFailOpenSemantics()
    {
        var condition = Equal("Gate", 1f);

        var allTrue = MaterialConditionGroupPolicy.Evaluate(
            condition,
            Values(1f, 1f));
        Equal(MaterialConditionAggregateState.AllSatisfied, allTrue.State,
            "all materials satisfying ShowIf are all-satisfied");
        Equal(true, allTrue.Visible,
            "all-true material group is visible");

        var allFalse = MaterialConditionGroupPolicy.Evaluate(
            condition,
            Values(0f, 0f));
        Equal(MaterialConditionAggregateState.NoneSatisfied, allFalse.State,
            "all materials failing ShowIf are none-satisfied");
        Equal(false, allFalse.Visible,
            "all-false material group is hidden");

        var mixed = MaterialConditionGroupPolicy.Evaluate(
            condition,
            Values(0f, 1f));
        Equal(MaterialConditionAggregateState.Mixed, mixed.State,
            "different same-name material values are mixed");
        Equal(true, mixed.Visible,
            "ANY semantics preserve a mixed property");
        Equal(1, mixed.SatisfiedCount,
            "mixed result exposes satisfied member count");
        Equal(1, mixed.UnsatisfiedCount,
            "mixed result exposes unsatisfied member count");

        var missing = MaterialConditionGroupPolicy.Evaluate(
            condition,
            Values(null, null));
        Equal(MaterialConditionAggregateState.Missing, missing.State,
            "unavailable source is explicitly missing");
        Equal(true, missing.Visible,
            "missing material group remains fail-open");

        var partiallyMissing = MaterialConditionGroupPolicy.Evaluate(
            condition,
            Values(0f, null));
        Equal(MaterialConditionAggregateState.Mixed, partiallyMissing.State,
            "partial source availability is explicitly mixed");
        Equal(true, partiallyMissing.Visible,
            "partial source availability remains fail-open");
    }

    private static void GroupedGraphCachesOneSnapshotPerSource()
    {
        var reads = 0;
        var current = Values(0f, 1f);
        Func<string, MaterialConditionValueSnapshot> raw = name =>
        {
            reads++;
            return current;
        };
        var initial = new MaterialEditorConditionValueSnapshotCache(raw, 1);
        var graph = new MaterialConditionDependencyGraph(
            2,
            "Shader/Grouped",
            raw,
            null);
        var first = graph.RegisterProperty(
            "First",
            Equal("Gate", 1f),
            initial.Resolve);
        var second = graph.RegisterProperty(
            "Second",
            Equal("Gate", 1f),
            initial.Resolve);
        graph.CompleteBuild(
            Supported("Gate"),
            Known("Gate", "First", "Second"));

        Equal(MaterialConditionAggregateState.Mixed, first.AggregateState,
            "first grouped property exposes initial Mixed state");
        Equal(MaterialConditionAggregateState.Mixed, second.AggregateState,
            "second grouped property reuses initial Mixed snapshot");
        Equal(1, reads,
            "initial dependents scan one same-name group snapshot");

        MaterialConditionInvalidationHandle handle;
        graph.TryCreateHandle("Gate", out handle);
        current = Values(0f, 0f);
        var result = graph.EvaluateHandles(new[] { handle });
        Equal(2, result.DependentsEvaluated,
            "grouped source evaluates both direct dependents");
        Equal(2, result.VisibilityChangeCount,
            "mixed-to-all-false changes both visible dependents");
        Equal(2, reads,
            "runtime batch scans the group once for one source");
    }

    private static void InitialEvaluationIsReusedAndRuntimeVisitsDirectEdges()
    {
        var gate = 0f;
        var reads = 0;
        Func<string, float?> raw = name =>
        {
            reads++;
            return name == "Gate" ? gate : (float?)null;
        };
        var initial = new MaterialEditorConditionValueCache(raw, 1);
        var graph = CreateGraph(1, raw);
        var state = graph.RegisterProperty(
            "Dependent",
            Equal("Gate", 1f),
            initial.Resolve);
        graph.CompleteBuild(
            Supported("Gate"),
            Known("Gate", "Dependent"));

        Equal(false, state.Visible, "initial ShowIf state");
        Equal(1, reads, "initial visibility evaluates one source read");

        MaterialConditionInvalidationHandle handle;
        Equal(true, graph.TryCreateHandle("Gate", out handle),
            "condition source handle exists");
        var unchanged = graph.EvaluateHandles(
            new[] { handle, handle });
        Equal(1, unchanged.DependentsEvaluated,
            "deduplicated source evaluates its direct edge once");
        Equal(false, unchanged.VisibilityChanged,
            "unchanged visibility does not request rebuild");
        Equal(2, reads, "one source read is shared by the runtime batch");

        gate = 1f;
        var changed = graph.EvaluateHandles(new[] { handle });
        Equal(true, changed.VisibilityChanged,
            "changed ShowIf is reported once");
        Equal(1, changed.VisibilityChangeCount,
            "one visibility edge changed");
    }

    private static void FailOpenMatchesExistingPolicyAndRetriesErrors()
    {
        var throws = true;
        var value = 0f;
        var reads = 0;
        Func<string, float?> raw = name =>
        {
            reads++;
            if (throws)
                throw new InvalidOperationException("transient");
            return value;
        };
        var graph = CreateGraph(3, raw);
        var state = graph.RegisterProperty(
            "Dependent",
            Equal("Gate", 1f),
            raw);
        graph.CompleteBuild(Supported("Gate"), Known("Gate", "Dependent"));
        Equal(true, state.Visible, "build-time resolver error fails open");

        MaterialConditionInvalidationHandle handle;
        graph.TryCreateHandle("Gate", out handle);
        var failed = graph.EvaluateHandles(new[] { handle });
        Equal(false, failed.VisibilityChanged,
            "runtime resolver error remains fail-open true");

        throws = false;
        value = 0f;
        var retried = graph.EvaluateHandles(new[] { handle });
        Equal(true, retried.VisibilityChanged,
            "a later batch retries the transient failure");
        Equal(true, state.Visible,
            "a proposed visibility transition does not mutate the old graph");
        var retryAfterFailedRebuild = graph.EvaluateHandles(
            new[] { handle });
        Equal(true, retryAfterFailedRebuild.VisibilityChanged,
            "the uncommitted visibility transition remains retryable");
        Equal(4, reads, "errors and proposals are never cached across batches");
    }

    private static void FailedVisibilityRebuildLeavesOldGraphRetryable()
    {
        var gate = 0f;
        Func<string, float?> raw = name => gate;
        var graph = CreateGraph(4, raw);
        var state = graph.RegisterProperty(
            "Dependent",
            Equal("Gate", 1f),
            raw);
        graph.CompleteBuild(Supported("Gate"), Known("Gate", "Dependent"));
        MaterialConditionInvalidationHandle handle;
        graph.TryCreateHandle("Gate", out handle);

        gate = 1f;
        var firstAttempt = graph.EvaluateHandles(new[] { handle });
        Equal(true, firstAttempt.VisibilityChanged,
            "Show false-to-true requests a rebuild");
        try
        {
            Action simulatedPopulateListCore = () =>
            {
                throw new InvalidOperationException("transient rebuild");
            };
            simulatedPopulateListCore();
        }
        catch (InvalidOperationException)
        {
        }

        Equal(false, state.Visible,
            "a failed rebuild leaves the old graph state untouched");
        var secondAttempt = graph.EvaluateHandles(new[] { handle });
        Equal(true, secondAttempt.VisibilityChanged,
            "the next source request asks for the rebuild again");
    }

    private static void HandlesDeduplicateOnlyWithinTheirOwnerAndGraph()
    {
        Func<string, float?> raw = name => 1f;
        var graphA = CreateGraph(10, raw);
        graphA.RegisterProperty("A", Equal("Gate", 1f), raw);
        graphA.CompleteBuild(Supported("Gate"), Known("Gate", "A"));
        var graphB = CreateGraph(10, raw);
        graphB.RegisterProperty("B", Equal("Gate", 1f), raw);
        graphB.CompleteBuild(Supported("Gate"), Known("Gate", "B"));
        var graphC = CreateGraph(11, raw);
        graphC.RegisterProperty("C", Equal("Gate", 1f), raw);
        graphC.CompleteBuild(Supported("Gate"), Known("Gate", "C"));

        MaterialConditionInvalidationHandle a1;
        MaterialConditionInvalidationHandle a2;
        MaterialConditionInvalidationHandle b;
        MaterialConditionInvalidationHandle c;
        graphA.TryCreateHandle("Gate", out a1);
        graphA.TryCreateHandle("Gate", out a2);
        graphB.TryCreateHandle("Gate", out b);
        graphC.TryCreateHandle("Gate", out c);

        Equal(true, a1.Equals(a2),
            "same owner, graph, and source ordinal deduplicate");
        Equal(false, a1.Equals(b),
            "two materials never share a graph handle");
        Equal(false, a1.Equals(c),
            "a new presentation owner never shares a stale handle");
    }

    private static void EvaluationRejectsValidOrdinalsFromWrongGraphOrOwner()
    {
        Func<string, float?> raw = name => 1f;
        var graphA = CreateGraph(12, raw);
        var stateA = graphA.RegisterProperty(
            "A",
            Equal("Gate", 0f),
            raw);
        graphA.CompleteBuild(Supported("Gate"), Known("Gate", "A"));
        var graphB = CreateGraph(12, raw);
        graphB.RegisterProperty("B", Equal("Gate", 1f), raw);
        graphB.CompleteBuild(Supported("Gate"), Known("Gate", "B"));
        MaterialConditionInvalidationHandle handleB;
        graphB.TryCreateHandle("Gate", out handleB);
        var staleOwner = new MaterialConditionInvalidationHandle(
            99,
            graphA,
            handleB.SourceOrdinal);

        var result = graphA.EvaluateHandles(new[] { handleB, staleOwner });
        Equal(0, result.DependentsEvaluated,
            "valid ordinal cannot cross graph or owner boundary");
        Equal(false, stateA.Visible,
            "rejected handles cannot mutate graph state");
    }

    private static void SingleSourceEvaluationDoesNotScaleScratchWithGraphSize()
    {
        const int sourceCount = 10000;
        var reads = 0;
        Func<string, float?> raw = name =>
        {
            reads++;
            return 1f;
        };
        var graph = CreateGraph(13, raw);
        var kinds = new Dictionary<string, MaterialConditionSourceKind>(
            StringComparer.Ordinal);
        var known = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < sourceCount; index++)
        {
            var source = "Source" + index;
            var dependent = "Dependent" + index;
            kinds.Add(source, MaterialConditionSourceKind.Float);
            known.Add(source);
            known.Add(dependent);
            graph.RegisterProperty(
                dependent,
                Equal(source, 1f),
                raw);
        }
        graph.CompleteBuild(kinds, known);
        reads = 0;
        MaterialConditionInvalidationHandle handle;
        graph.TryCreateHandle("Source9999", out handle);

        var result = graph.EvaluateHandles(new[] { handle });
        Equal(1, result.DependentsEvaluated,
            "one requested source visits one direct edge in a 10k graph");
        Equal(1, reads,
            "one requested source performs one material read in a 10k graph");
    }

    private static void SourceCatalogPreservesManifestAndProviderPrecedence()
    {
        var definitions = new[]
        {
            new MaterialEditorPluginBase.ShaderPropertyData(
                "ManifestWins",
                MaterialAPI.ShaderPropertyType.Float),
            new MaterialEditorPluginBase.ShaderPropertyData(
                "UnsupportedManifest",
                MaterialAPI.ShaderPropertyType.Color)
        };
        var descriptors = new[]
        {
            Descriptor(
                "manifest-shadow",
                "ManifestWins",
                MaterialEditorPropertyEditorIds.Boolean),
            Descriptor(
                "unsupported-fill",
                "UnsupportedManifest",
                MaterialEditorPropertyEditorIds.Boolean),
            Descriptor("custom", "CustomOnly", "owner.custom-editor"),
            Descriptor("custom-first", "ExtensionOrder", "owner.custom-editor"),
            Descriptor(
                "supported-second",
                "ExtensionOrder",
                MaterialEditorPropertyEditorIds.Boolean),
            Descriptor(
                "float-first",
                "FirstSupportedWins",
                MaterialEditorPropertyEditorIds.Float),
            Descriptor(
                "keyword-second",
                "FirstSupportedWins",
                MaterialEditorPropertyEditorIds.Boolean)
        };

        var kinds = MaterialConditionSourceCatalog.Build(
            definitions,
            descriptors);
        Equal(MaterialConditionSourceKind.Float, kinds["ManifestWins"],
            "supported manifest source kind wins over extensions");
        Equal(MaterialConditionSourceKind.Keyword, kinds["UnsupportedManifest"],
            "unsupported manifest kind leaves room for extension source kind");
        Equal(false, kinds.ContainsKey("CustomOnly"),
            "custom editor remains unsupported even if its factory returns Float");
        Equal(MaterialConditionSourceKind.Keyword, kinds["ExtensionOrder"],
            "unsupported earlier descriptor does not claim source kind");
        Equal(MaterialConditionSourceKind.Float, kinds["FirstSupportedWins"],
            "first supported descriptor in provider order wins");

        var manyDefinitions = new List<
            MaterialEditorPluginBase.ShaderPropertyData>();
        for (var index = 0; index < 10000; index++)
        {
            manyDefinitions.Add(
                new MaterialEditorPluginBase.ShaderPropertyData(
                    "Unused" + index,
                    MaterialAPI.ShaderPropertyType.Float));
        }
        manyDefinitions.Add(
            new MaterialEditorPluginBase.ShaderPropertyData(
                "Requested",
                MaterialAPI.ShaderPropertyType.Keyword));
        var selective = MaterialConditionSourceCatalog.Build(
            manyDefinitions,
            new MaterialEditorPropertyDescriptor[0],
            Known("Requested"));
        Equal(1, selective.Count,
            "one condition source does not allocate a 10k source-kind catalog");
        Equal(MaterialConditionSourceKind.Keyword, selective["Requested"],
            "selective catalog preserves requested source kind");
    }

    private static void CoordinatorCoalescesFiveHundredHandlesAndSearchDominates()
    {
        Func<string, float?> raw = name => 1f;
        var graph = CreateGraph(20, raw);
        graph.RegisterProperty("A", Equal("Gate", 1f), raw);
        graph.CompleteBuild(Supported("Gate"), Known("Gate", "A"));
        MaterialConditionInvalidationHandle handle;
        graph.TryCreateHandle("Gate", out handle);

        var coordinator =
            new PresentationInvalidationCoordinator<
                MaterialConditionInvalidationHandle>();
        var changed = 0;
        for (var index = 0; index < 500; index++)
            if (coordinator.RequestCondition(handle))
                changed++;
        Equal(1, changed, "500 same-source requests create one pending source");
        Equal(1, coordinator.PendingConditionCount,
            "coordinator stores one handle");

        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "one worker lease is acquired");
        Equal(false, coordinator.TryAcquireWorker(out _),
            "no second worker lease is created");

        Equal(true, coordinator.RequestSearch(),
            "Search supersedes pending Conditions");
        PresentationInvalidationBatch<MaterialConditionInvalidationHandle> batch;
        Equal(true, coordinator.TryBeginFlush(lease, 1, out batch),
            "dominant Search batch begins");
        Equal(PresentationInvalidationReason.Search, batch.Reason,
            "Search is the only reason in the batch");
        Equal(0, batch.ConditionSources.Length,
            "Search clears individual condition handles");
        coordinator.CompleteFlush(lease);
    }

    private static void DiagnosticsAreBoundedAndWarningOnce()
    {
        MaterialConditionWarningCache.ResetForTests();
        var warnings = new List<string>();
        Func<string, float?> missing = name => null;
        for (var build = 0; build < 2; build++)
        {
            var graph = new MaterialConditionDependencyGraph(
                30 + build,
                "Shader/Diagnostics",
                missing,
                warnings.Add);
            graph.RegisterProperty(
                "Self",
                Equal("Self", 1f),
                missing);
            graph.RegisterProperty(
                "CycleA",
                Equal("CycleB", 1f),
                missing);
            graph.RegisterProperty(
                "CycleB",
                Equal("CycleA", 1f),
                missing);
            graph.RegisterProperty(
                "MissingDependent",
                Equal("Missing", 1f),
                missing);
            graph.RegisterProperty(
                "IncompatibleDependent",
                Equal("ColorSource", 1f),
                missing);
            graph.CompleteBuild(
                Supported(),
                Known(
                    "Self",
                    "CycleA",
                    "CycleB",
                    "ColorSource",
                    "IncompatibleDependent"));
        }

        Equal(7, warnings.Count,
            "self, cycle, missing, and four incompatible sources warn once");
        for (var index = 0; index < 300; index++)
            MaterialConditionWarningCache.TryRecord("bounded-" + index);
        Equal(256, MaterialConditionWarningCache.Count,
            "warning cache retains only its fixed string capacity");
    }

    private static void LongDiagnosticCycleDoesNotUseRecursiveTraversal()
    {
        const int propertyCount = 4096;
        MaterialConditionWarningCache.ResetForTests();
        var warnings = new List<string>();
        Func<string, float?> raw = name => 1f;
        var graph = new MaterialConditionDependencyGraph(
            40,
            "Shader/LongCycle",
            raw,
            warnings.Add);
        var kinds = new Dictionary<string, MaterialConditionSourceKind>(
            StringComparer.Ordinal);
        var known = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < propertyCount; index++)
        {
            var source = "P" + index;
            var dependent = "P" + ((index + 1) % propertyCount);
            kinds.Add(source, MaterialConditionSourceKind.Float);
            known.Add(source);
            graph.RegisterProperty(
                dependent,
                Equal(source, 1f),
                raw);
        }

        graph.CompleteBuild(kinds, known);
        Equal(1, warnings.Count,
            "one long cycle completes with one bounded diagnostic");
        Equal(true, warnings[0].Contains("count=4096"),
            "long diagnostic retains bounded cycle cardinality");
    }

    private static MaterialConditionDependencyGraph CreateGraph(
        int owner,
        Func<string, float?> resolver)
    {
        return new MaterialConditionDependencyGraph(
            owner,
            "Shader/Test",
            resolver,
            null);
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

    private static MaterialConditionValueSnapshot Values(
        params float?[] values)
    {
        return new MaterialConditionValueSnapshot(values);
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

    private static MaterialEditorPropertyDescriptor Descriptor(
        string id,
        string propertyName,
        string editorId)
    {
        return new MaterialEditorPropertyDescriptor(id, id, editorId)
        {
            PropertyName = propertyName
        };
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
