using MaterialEditorAPI;
using System.Globalization;
using System.Xml;

internal static class SyntheticEngine
{
    private static int _providerSnapshotGeneration = -1;
    private static int[] _providerSnapshot;

    internal static void ResetOptimizationState()
    {
        _providerSnapshotGeneration = -1;
        _providerSnapshot = null;
    }

    internal static List<SyntheticProperty> ParseManifest(SyntheticDataset dataset)
    {
        var performanceSample = MaterialEditorPerformance.Start(
            MaterialEditorPerformanceMetric.ManifestParsing);
        try
        {
            var document = new XmlDocument();
            document.LoadXml(dataset.ManifestXml);
            var result = new List<SyntheticProperty>(dataset.PropertyCount);
            foreach (XmlElement element in document.GetElementsByTagName("Property"))
            {
                var metadata = ShaderPropertyMetadataParser.Parse(element);
                bool hidden;
                bool.TryParse(element.GetAttribute("Hidden"), out hidden);
                result.Add(new SyntheticProperty
                {
                    Name = element.GetAttribute("Name"),
                    Category = element.GetAttribute("Category"),
                    DisplayName = string.IsNullOrEmpty(metadata.DisplayName)
                        ? element.GetAttribute("Name")
                        : metadata.DisplayName,
                    Tooltip = metadata.TooltipText,
                    EditorId = metadata.EditorId,
                    Hidden = hidden,
                    ShowIf = metadata.ShowIf,
                    EnumOptionCount = metadata.EnumOptions.Count
                });
            }
            return result;
        }
        finally
        {
            MaterialEditorPerformance.Stop(
                MaterialEditorPerformanceMetric.ManifestParsing,
                performanceSample);
        }
    }

    internal static WorkloadOutcome DescribeParse(
        SyntheticDataset dataset,
        IList<SyntheticProperty> properties)
    {
        var categories = new HashSet<string>(StringComparer.Ordinal);
        var conditions = 0;
        var enums = 0;
        var vectors = 0;
        var booleans = 0;
        var tooltips = 0;
        var hidden = 0;
        var hash = StableHash.Start;
        foreach (var property in properties)
        {
            categories.Add(property.Category);
            if (property.ShowIf != null)
                conditions++;
            if (property.EditorId == "materialeditor.enum")
                enums++;
            else if (property.EditorId == "materialeditor.vector4")
                vectors++;
            else if (property.EditorId == "materialeditor.boolean")
                booleans++;
            if (!string.IsNullOrEmpty(property.Tooltip))
                tooltips++;
            if (property.Hidden)
                hidden++;
            hash = StableHash.Add(hash, property.Name);
            hash = StableHash.Add(hash, property.Category);
            hash = StableHash.Add(hash, property.EditorId);
            hash = StableHash.Add(hash, property.Tooltip);
            hash = StableHash.Add(hash, property.Hidden ? "hidden" : "visible");
        }

        var expectedHidden = Enumerable.Range(0, dataset.PropertyCount)
            .Count(index => index >= dataset.ConditionCount
                            && index > 0
                            && index % 29 == 0);

        return new WorkloadOutcome
        {
            Fingerprint = StableHash.Format(hash),
            VisibleRows = properties.Count,
            SemanticChecksPassed =
                properties.Count == dataset.PropertyCount
                && categories.Count == dataset.CategoryCount
                && conditions == dataset.ConditionCount
                && enums == dataset.EnumCount
                && vectors == dataset.VectorCount
                && booleans == dataset.BooleanCount
                && tooltips == dataset.PropertyCount
                && hidden == expectedHidden
        };
    }

    internal static WorkloadOutcome Rebuild(
        SyntheticDataset dataset,
        IList<SyntheticProperty> properties,
        string search,
        int targetCount,
        MultiTargetShape targetShape,
        SyntheticRowPool pool = null,
        bool closePool = true)
    {
        var performanceSample = MaterialEditorPerformance.Start(
            MaterialEditorPerformanceMetric.PresentationRebuild);
        try
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.SourceEnumerations,
                1L);

            var visible = new List<SyntheticProperty>();
            Func<string, float?> resolveCondition = ResolveConditionValue;
            var visibleConditionCount = dataset.ConditionCount;
            if (visibleConditionCount > 0)
            {
                var conditionValues = new MaterialEditorConditionValueCache(
                    ResolveConditionValue,
                    Math.Min(5, visibleConditionCount));
                resolveCondition = conditionValues.Resolve;
            }
            var searchPattern = string.IsNullOrEmpty(search)
                ? null
                : MaterialEditorFilter.Prepare(search);
            var filteringSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.Filtering);
            try
            {
                foreach (var property in properties)
                {
                    if (property.Hidden)
                        continue;
                    var show = MaterialEditorConditionPolicy.Evaluate(
                        property.ShowIf,
                        resolveCondition,
                        true);
                    if (!show)
                        continue;
                    if (!MatchesSearch(property, searchPattern))
                        continue;
                    visible.Add(property);
                }
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.Filtering,
                    filteringSample);
            }

            var categories = new HashSet<string>(StringComparer.Ordinal);
            var categorySample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.CategoryRebuild);
            try
            {
                foreach (var property in visible)
                    categories.Add(property.Category);
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.CategoryRebuild,
                    categorySample);
            }

            var providerDescriptors = InvokeProviders(
                dataset.ProviderCount,
                Math.Max(1, targetCount));
            var rowCount = visible.Count + categories.Count + providerDescriptors;
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RowModelCreation,
                rowCount);

            var mixedRows = 0;
            var missingRows = 0;
            var variantRows = 0;
            var hash = StableHash.Start;
            for (var index = 0; index < visible.Count; index++)
            {
                var mixedState = CalculateMixed(
                    index,
                    targetCount,
                    targetShape,
                    out var missing,
                    out var variant);
                if (mixedState)
                    mixedRows++;
                if (missing)
                    missingRows++;
                if (variant)
                    variantRows++;
                hash = StableHash.Add(hash, visible[index].Name);
                hash = StableHash.Add(hash, mixedState ? 1 : 0);
                hash = StableHash.Add(hash, missing ? 1 : 0);
                hash = StableHash.Add(hash, variant ? 1 : 0);
            }
            hash = StableHash.Add(hash, categories.Count);
            hash = StableHash.Add(hash, providerDescriptors);

            var activePool = pool ?? new SyntheticRowPool();
            activePool.BindRows(rowCount);
            if (closePool)
                activePool.Close();

            return new WorkloadOutcome
            {
                Fingerprint = StableHash.Format(hash),
                VisibleRows = rowCount,
                MixedRows = mixedRows,
                MissingRows = missingRows,
                VariantRows = variantRows,
                PoolPeak = activePool.Peak,
                ActiveListeners = activePool.ActiveListenerCount,
                LogicalRebuilds = 1,
                SemanticChecksPassed =
                    activePool.Peak <= 32
                    && (!closePool || activePool.ActiveListenerCount == 0)
            };
        }
        finally
        {
            MaterialEditorPerformance.Stop(
                MaterialEditorPerformanceMetric.PresentationRebuild,
                performanceSample);
        }
    }

    internal static WorkloadOutcome RunLongevity(SyntheticDataset dataset)
    {
        var properties = ParseManifest(dataset);
        var pool = new SyntheticRowPool();
        WorkloadOutcome last = null;

        for (var index = 0; index < 100; index++)
        {
            last = Rebuild(
                dataset,
                properties,
                string.Empty,
                5,
                MultiTargetShape.Mixed,
                pool,
                false);
            pool.Close();
        }

        for (var index = 0; index < 200; index++)
            last = Rebuild(
                dataset,
                properties,
                string.Empty,
                5,
                index % 2 == 0 ? MultiTargetShape.Equal : MultiTargetShape.Mixed,
                pool,
                false);

        for (var index = 0; index < 100; index++)
            last = Rebuild(
                dataset,
                properties,
                string.Empty,
                5,
                MultiTargetShape.Mixed,
                pool,
                false);

        for (var index = 0; index < 100; index++)
            last = Rebuild(
                dataset,
                properties,
                "Property" + (index % 25).ToString(CultureInfo.InvariantCulture),
                5,
                MultiTargetShape.Mixed,
                pool,
                false);

        pool.Close();
        for (var index = 0; index < 500; index++)
        {
            pool.BindRows(12);
            pool.Close();
        }

        var condition = properties.First(property => property.ShowIf != null).ShowIf;
        for (var index = 0; index < 100; index++)
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.CacheInvalidations);
            MaterialEditorConditionPolicy.Evaluate(
                condition,
                ResolveConditionValue,
                true);
        }

        var registry = new SyntheticProviderRegistry();
        for (var index = 0; index < 100; index++)
        {
            using (registry.Register())
            {
            }
        }
        pool.Close();

        var hash = StableHash.Start;
        hash = StableHash.Add(hash, last == null ? string.Empty : last.Fingerprint);
        hash = StableHash.Add(hash, pool.Peak);
        hash = StableHash.Add(hash, registry.Count);
        return new WorkloadOutcome
        {
            Fingerprint = StableHash.Format(hash),
            VisibleRows = last == null ? 0 : last.VisibleRows,
            MixedRows = last == null ? 0 : last.MixedRows,
            PoolPeak = pool.Peak,
            ActiveListeners = pool.ActiveListenerCount,
            ProviderRegistrations = registry.Count,
            LogicalRebuilds = 500,
            SemanticChecksPassed =
                pool.Peak <= 32
                && pool.ActiveListenerCount == 0
                && registry.Count == 0
        };
    }

    private static float? ResolveConditionValue(string propertyName)
    {
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ConditionResolveCalls);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ConditionMaterialReads);
        return propertyName.StartsWith("Source", StringComparison.Ordinal)
            ? 1f
            : (float?)null;
    }

    private static bool MatchesSearch(
        SyntheticProperty property,
        MaterialEditorFilterPattern search)
    {
        if (search == null)
            return true;
        return search.Matches(property.DisplayName)
               || search.Matches(property.Name);
    }

    private static int InvokeProviders(int providerCount, int materialCount)
    {
        var descriptorCount = 0;
        if (_providerSnapshot == null
            || _providerSnapshotGeneration != providerCount)
        {
            var snapshotSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.ProviderSnapshotBuilds);
            try
            {
                _providerSnapshot = Enumerable.Range(0, providerCount)
                    .OrderByDescending(value => value)
                    .ToArray();
                _providerSnapshotGeneration = providerCount;
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.ProviderSnapshotBuilds,
                    snapshotSample);
            }
        }

        for (var materialIndex = 0; materialIndex < materialCount; materialIndex++)
        {
            foreach (var priority in _providerSnapshot)
            {
                var providerSample = MaterialEditorPerformance.Start(
                    MaterialEditorPerformanceMetric.ProviderCalls);
                try
                {
                    descriptorCount += priority % 3 + 1;
                }
                finally
                {
                    MaterialEditorPerformance.Stop(
                        MaterialEditorPerformanceMetric.ProviderCalls,
                        providerSample);
                }
            }
        }
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ProviderEnumeratedDescriptors,
            descriptorCount);
        return descriptorCount;
    }

    private static bool CalculateMixed(
        int propertyIndex,
        int targetCount,
        MultiTargetShape shape,
        out bool missing,
        out bool variant)
    {
        missing = false;
        variant = false;
        var performanceSample = MaterialEditorPerformance.Start(
            MaterialEditorPerformanceMetric.MixedCalculations);
        try
        {
            if (targetCount <= 1)
                return false;
            var first = ResolveSyntheticValue(propertyIndex, 0, shape, out var firstExists, out var firstVariant);
            for (var targetIndex = 1; targetIndex < targetCount; targetIndex++)
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.MixedComparisons);
                var value = ResolveSyntheticValue(
                    propertyIndex,
                    targetIndex,
                    shape,
                    out var exists,
                    out var targetVariant);
                if (exists != firstExists)
                    missing = true;
                if (targetVariant != firstVariant)
                    variant = true;
                if (exists != firstExists
                    || targetVariant != firstVariant
                    || value != first)
                    return true;
            }
            return false;
        }
        finally
        {
            MaterialEditorPerformance.Stop(
                MaterialEditorPerformanceMetric.MixedCalculations,
                performanceSample);
        }
    }

    private static float ResolveSyntheticValue(
        int propertyIndex,
        int targetIndex,
        MultiTargetShape shape,
        out bool exists,
        out int variant)
    {
        exists = shape != MultiTargetShape.Missing || targetIndex % 5 != 4;
        variant = shape == MultiTargetShape.Variants ? targetIndex % 3 : 0;
        if (!exists)
            return 0f;
        if (shape == MultiTargetShape.Mixed && targetIndex == 1)
            return propertyIndex + 0.5f;
        return propertyIndex;
    }
}

internal static class StableHash
{
    internal const ulong Start = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    internal static ulong Add(ulong hash, string value)
    {
        if (value == null)
            return Add(hash, -1);
        foreach (var character in value)
        {
            hash ^= character;
            hash *= Prime;
        }
        return hash;
    }

    internal static ulong Add(ulong hash, int value)
    {
        unchecked
        {
            hash ^= (uint)value;
            hash *= Prime;
            return hash;
        }
    }

    internal static string Format(ulong hash)
    {
        return hash.ToString("X16", CultureInfo.InvariantCulture);
    }
}
