using MaterialEditorAPI;
using UnityEngine;
using UnityEngine.UI;

internal static class EnumDropdownPriorityNineTests
{
    internal static long LegacyAllocatedBytes { get; private set; }
    internal static long OptimizedAllocatedBytes { get; private set; }

    internal static void Run()
    {
        EnumProjectionPreservesMixedUnknownNullAndDuplicateSemantics();
        StableContextSkipsOptionProjection();
        SameCountContentMutationRebuildsProjection();
        ReleasedContextIsNeutralAndReusable();
        FiveHundredRebindsReuseOptionsAndInvokeNoCallbacks();
        LargeSmallLargeOscillationReusesHighWaterSlots();
        AboveCapShrinkDropsTailAndCompactsBuffers();
        StableUnknownReusesItsSingleCachedCaption();
        RebindScrubsThePreviousModelListener();
        SameIndexRebuildRefreshesCaptionText();
        ProductionSourceKeepsPriorityNineGuards();
        Console.WriteLine(
            "Enum dropdown Priority 9 allocations: legacy="
            + LegacyAllocatedBytes
            + " B/500, optimized="
            + OptimizedAllocatedBytes
            + " B/500.");
    }

    private static void StableContextSkipsOptionProjection()
    {
        var dropdown = new Dropdown();
        var cache = new EnumDropdownOptionCache(dropdown);
        var options = CreateOptions();

        cache.Rebuild(options, 0f, false);
        var firstProjectionCount = cache.ProjectionRebuildCount;
        var stableOptions = dropdown.options.ToArray();
        for (var iteration = 0; iteration < 100; iteration++)
            cache.Rebuild(options, 0f, false);

        Equal(firstProjectionCount, cache.ProjectionRebuildCount,
            "same enum context does not regenerate options");
        cache.Rebuild(options, 2f, false);
        Equal(firstProjectionCount, cache.ProjectionRebuildCount,
            "known value change only moves selection");
        Equal(3, dropdown.value, "known selection moves without reprojection");
        for (var index = 0; index < stableOptions.Length; index++)
        {
            Equal(true, ReferenceEquals(stableOptions[index], dropdown.options[index]),
                "known value change preserves OptionData " + index);
        }

        cache.Rebuild(options, 2f, true);
        Equal(firstProjectionCount + 1, cache.ProjectionRebuildCount,
            "Mixed shape change projects exactly once");
    }

    private static void SameCountContentMutationRebuildsProjection()
    {
        var dropdown = new Dropdown();
        var cache = new EnumDropdownOptionCache(dropdown);
        var options = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "Zero"),
            new MaterialEditorEnumOption(1f, "One")
        };

        cache.Rebuild(options, 1f, false);
        var projectionCount = cache.ProjectionRebuildCount;
        options[1] = new MaterialEditorEnumOption(1f, "One (updated)");
        cache.Rebuild(options, 1f, false);

        Equal(projectionCount + 1, cache.ProjectionRebuildCount,
            "same-list same-count content replacement rebuilds projection");
        Equal("One (updated)", dropdown.options[1].text,
            "same-count replacement refreshes displayed option");
        Equal("One (updated)", dropdown.captionText,
            "same-count replacement refreshes selected caption");

        var equivalentReplacement = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "Zero"),
            new MaterialEditorEnumOption(1f, "One (updated)")
        };
        cache.Rebuild(equivalentReplacement, 1f, false);
        Equal(projectionCount + 1, cache.ProjectionRebuildCount,
            "equivalent replacement list reuses its content projection");
    }

    private static void ReleasedContextIsNeutralAndReusable()
    {
        var dropdown = new Dropdown();
        var cache = new EnumDropdownOptionCache(dropdown);
        var options = CreateOptions();
        cache.Rebuild(options, 1f, false);
        var retained = dropdown.options.ToArray();
        var projectionCount = cache.ProjectionRebuildCount;

        cache.ReleaseContext();
        Equal(0, dropdown.options.Count, "release removes active dropdown options");
        Equal(retained.Length, cache.RetainedOptionDataCount,
            "release retains only reusable OptionData shells");
        for (var index = 0; index < retained.Length; index++)
            Equal(string.Empty, retained[index].text, "release neutralizes text " + index);
        AssertNoMappedValue(cache, 0, "release clears previous value mapping");

        cache.ReleaseContext();
        Equal(projectionCount, cache.ProjectionRebuildCount,
            "repeated neutral release performs no projection work");
        cache.Rebuild(options, 1f, false);
        Equal(projectionCount + 1, cache.ProjectionRebuildCount,
            "rebound context projects exactly once");
        for (var index = 0; index < retained.Length; index++)
        {
            Equal(true, ReferenceEquals(retained[index], dropdown.options[index]),
                "rebound context reuses neutral OptionData " + index);
        }
    }

    private static void EnumProjectionPreservesMixedUnknownNullAndDuplicateSemantics()
    {
        var dropdown = new Dropdown();
        var callbacks = 0;
        dropdown.onValueChanged.AddListener(_ => callbacks++);
        var cache = new EnumDropdownOptionCache(dropdown);
        var options = CreateOptions();

        cache.Rebuild(options, 1f, false);
        AssertLabels(dropdown, "Zero", "First", "Last", "Two");
        Equal(2, dropdown.value, "duplicate value selects its last displayed option");
        AssertMappedValue(cache, 0, 0f, "first known value");
        AssertMappedValue(cache, 1, 1f, "first duplicate snapshot");
        AssertMappedValue(cache, 2, 1f, "last duplicate snapshot");

        cache.Rebuild(options, 1f, true);
        AssertLabels(dropdown, "Mixed", "Zero", "First", "Last", "Two");
        Equal(0, dropdown.value, "Mixed placeholder selected");
        AssertNoMappedValue(cache, 0, "Mixed placeholder does not write");

        cache.Rebuild(options, 7f, false);
        Equal(true, dropdown.options[0].text.StartsWith(
            "Unknown (",
            StringComparison.Ordinal), "Unknown placeholder retained");
        Equal(0, dropdown.value, "Unknown placeholder selected");
        AssertNoMappedValue(cache, 0, "Unknown placeholder does not write");
        AssertLabelsFrom(
            dropdown,
            1,
            "Zero",
            "First",
            "Last",
            "Two");

        cache.Rebuild(null, 7f, false);
        Equal(1, dropdown.options.Count, "null option source remains safe");
        AssertNoMappedValue(cache, 0, "null-source Unknown remains read-only");
        Equal(0, callbacks, "programmatic projection invokes no callbacks");
    }

    private static void FiveHundredRebindsReuseOptionsAndInvokeNoCallbacks()
    {
        var dropdown = new Dropdown();
        var callbacks = 0;
        dropdown.onValueChanged.AddListener(_ => callbacks++);
        var cache = new EnumDropdownOptionCache(dropdown);
        var options = CreateOptions();

        for (var warmup = 0; warmup < 16; warmup++)
            cache.Rebuild(options, 1f, false);
        var stableOptions = dropdown.options.ToArray();

        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 500; iteration++)
            cache.Rebuild(options, 1f, false);
        OptimizedAllocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Equal(4, dropdown.options.Count, "500 rebinds preserve exact option count");
        Equal(0, callbacks, "500 programmatic rebinds invoke no callbacks");
        for (var index = 0; index < stableOptions.Length; index++)
            Equal(true, ReferenceEquals(stableOptions[index], dropdown.options[index]),
                "OptionData slot remains stable " + index);

        var legacyDropdown = new Dropdown();
        LegacyRebuild(legacyDropdown, options, 1f, false);
        allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 500; iteration++)
            LegacyRebuild(legacyDropdown, options, 1f, false);
        LegacyAllocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Equal(0L, OptimizedAllocatedBytes,
            "warm enum option projection allocates no managed memory");
        Equal(true, LegacyAllocatedBytes > OptimizedAllocatedBytes,
            "cached option projection improves on rebuilding OptionData");
    }

    private static void ProductionSourceKeepsPriorityNineGuards()
    {
        var binder = ReadRepositorySource(Path.Combine(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowBinder.EnumVectorToggle.cs"));
        Equal(0, CountOccurrences(binder, "new List<float?>"),
            "binder does not allocate an enum index list");
        Equal(0, CountOccurrences(binder, "new Dropdown.OptionData"),
            "binder does not rebuild enum OptionData");
        Equal(0, CountOccurrences(binder, "RefreshShownValue"),
            "binder does not duplicate Dropdown.Set refresh");
        Contains(binder, "controls.OptionCache.TryGetValue(index, out selectedValue)",
            "dropdown listener reads the displayed value snapshot");
        Contains(binder, "if (controls.Toggle.isOn != desired)",
            "float toggle skips an unchanged programmatic Set");

        var cache = ReadRepositorySource(Path.Combine(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.EnumDropdownOptionCache.cs"));
        Equal(0, CountOccurrences(cache, "foreach"),
            "option cache avoids interface enumerator allocations");
        Contains(cache, "option.text = text;", "OptionData text is reused in place");
        Contains(cache, "option.image = null;", "reused OptionData matches text-only legacy state");
        Contains(cache, "_optionData[slot]", "removed active options remain cached by slot");
        Contains(cache, "internal const int RetainedOptionDataLimit = 128;",
            "retained OptionData has an explicit per-RowView cap");
        Contains(cache, "internal void ReleaseContext()",
            "pooled enum context has an explicit neutralization seam");
        Contains(cache, "ProjectionRebuildCount",
            "tests observe option projection rather than dropdown opening");
        Contains(cache, "if (sameSource",
            "stable enum context has an option-projection fast path");
        Contains(cache, "ComputeFingerprint(options)",
            "mutable enum lists use a content fingerprint");
        Equal(0, CountOccurrences(cache, "ReferenceEquals(options"),
            "enum projection validity never relies on list identity alone");
        Contains(cache, "NeutralizeRetainedTail",
            "inactive cached slots release previous context strings");
        Contains(cache, "exactly one value/string pair per RowView",
            "bounded dynamic Unknown caption cache is documented");

        var descriptor = ReadRepositorySource(Path.Combine(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.PropertyDescriptor.cs"));
        var constructor = ExtractMethod(
            descriptor,
            "internal PropertyDescriptor(\n            GameObject gameObject");
        Equal(0, CountOccurrences(constructor, "EnumOptions = definition.EnumOptions"),
            "manifest descriptor does not replace its constructor-owned enum list");
        Contains(constructor, ".AddRange(definition.EnumOptions);",
            "manifest options fill the existing enum list");
    }

    private static void StableUnknownReusesItsSingleCachedCaption()
    {
        var dropdown = new Dropdown();
        var callbacks = 0;
        dropdown.onValueChanged.AddListener(_ => callbacks++);
        var cache = new EnumDropdownOptionCache(dropdown);
        var options = new List<MaterialEditorEnumOption>();

        for (var warmup = 0; warmup < 16; warmup++)
            cache.Rebuild(options, 7f, false);
        var stableText = dropdown.options[0].text;

        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 500; iteration++)
            cache.Rebuild(options, 7f, false);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Equal(0L, allocated, "stable Unknown caption allocates nothing after warm-up");
        Equal(0, callbacks, "stable Unknown rebuild invokes no callbacks");
        Equal(true, ReferenceEquals(stableText, dropdown.options[0].text),
            "stable Unknown reuses the same string instance");

        cache.Rebuild(options, 8f, false);
        Equal(false, ReferenceEquals(stableText, dropdown.options[0].text),
            "changed Unknown replaces its cached string");
        Equal(true, dropdown.options[0].text.Contains("8", StringComparison.Ordinal),
            "changed Unknown updates displayed text");
        Equal(dropdown.options[0].text, dropdown.captionText,
            "changed Unknown refreshes the caption");
    }

    private static void AboveCapShrinkDropsTailAndCompactsBuffers()
    {
        var dropdown = new Dropdown();
        var cache = new EnumDropdownOptionCache(dropdown);
        var aboveCap = CreateSequentialOptions(192);
        var small = CreateSequentialOptions(2);

        cache.Rebuild(aboveCap, 191f, false);
        Equal(192, dropdown.options.Count, "above-cap option set displays in full");
        Equal(EnumDropdownOptionCache.RetainedOptionDataLimit,
            cache.RetainedOptionDataCount,
            "only the first 128 OptionData slots are retained");

        cache.Rebuild(small, 1f, false);
        Equal(2, dropdown.options.Count, "above-cap tail removed after shrink");
        Equal(EnumDropdownOptionCache.RetainedOptionDataLimit,
            cache.RetainedOptionDataCount,
            "shrink retains no OptionData beyond the explicit cap");
        Equal(true,
            dropdown.options.Capacity <= EnumDropdownOptionCache.RetainedOptionDataLimit,
            "active option buffer compacts to the cap after shrink");
        Equal(true,
            cache.IndexValueCapacity <= EnumDropdownOptionCache.RetainedOptionDataLimit,
            "value snapshot buffer compacts to the cap after shrink");
    }

    private static void RebindScrubsThePreviousModelListener()
    {
        var dropdown = new Dropdown();
        var cache = new EnumDropdownOptionCache(dropdown);
        var listeners = new ListenerScope();
        var optionsA = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "A Zero"),
            new MaterialEditorEnumOption(1f, "A One")
        };
        var optionsB = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(10f, "B Ten"),
            new MaterialEditorEnumOption(11f, "B Eleven")
        };
        var writesA = 0;
        var writesB = 0;
        var valueA = -1f;
        var valueB = -1f;

        cache.Rebuild(optionsA, 0f, false);
        listeners.Listen(dropdown, index =>
        {
            float selected;
            if (!cache.TryGetValue(index, out selected))
                return;
            writesA++;
            valueA = selected;
        });
        dropdown.onValueChanged.Invoke(1);
        Equal(1, writesA, "model A receives its initial selection");
        Equal(1f, valueA, "model A receives its own mapped value");

        listeners.Clear();
        Equal(0, dropdown.onValueChanged.ListenerCount,
            "rebind scrub removes model A listener");
        cache.Rebuild(optionsB, 10f, false);
        listeners.Listen(dropdown, index =>
        {
            float selected;
            if (!cache.TryGetValue(index, out selected))
                return;
            writesB++;
            valueB = selected;
        });
        Equal(1, dropdown.onValueChanged.ListenerCount,
            "rebind installs only model B listener");

        dropdown.onValueChanged.Invoke(1);
        Equal(1, writesA, "post-rebind selection never writes model A");
        Equal(1, writesB, "post-rebind selection writes model B once");
        Equal(11f, valueB, "model B receives its own mapped value");
        listeners.Clear();
    }

    private static void SameIndexRebuildRefreshesCaptionText()
    {
        var dropdown = new Dropdown();
        var cache = new EnumDropdownOptionCache(dropdown);
        var optionsA = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "A Zero"),
            new MaterialEditorEnumOption(1f, "A One")
        };
        var optionsB = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "B Zero"),
            new MaterialEditorEnumOption(1f, "B One")
        };

        cache.Rebuild(optionsA, 1f, false);
        Equal(1, dropdown.value, "first selected index");
        Equal("A One", dropdown.captionText, "first caption text");

        cache.Rebuild(optionsB, 1f, false);
        Equal(1, dropdown.value, "selected index remains unchanged");
        Equal("B One", dropdown.options[dropdown.value].text,
            "same OptionData slot receives the new text");
        Equal("B One", dropdown.captionText,
            "Dropdown.Set refreshes caption even at the same index");
    }

    private static void LargeSmallLargeOscillationReusesHighWaterSlots()
    {
        var dropdown = new Dropdown();
        var callbacks = 0;
        dropdown.onValueChanged.AddListener(_ => callbacks++);
        var cache = new EnumDropdownOptionCache(dropdown);
        var large = CreateSequentialOptions(64);
        var small = CreateSequentialOptions(2);

        for (var warmup = 0; warmup < 16; warmup++)
        {
            cache.Rebuild(large, 63f, false);
            cache.Rebuild(small, 1f, false);
            cache.Rebuild(large, 63f, false);
        }
        var highWaterSlots = dropdown.options.ToArray();

        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 500; iteration++)
        {
            cache.Rebuild(small, 1f, false);
            cache.Rebuild(large, 63f, false);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Equal(0L, allocated, "64 to 2 to 64 rebinds allocate nothing after warm-up");
        Equal(0, callbacks, "64 to 2 to 64 programmatic rebinds invoke no callbacks");
        Equal(64, dropdown.options.Count, "large option count restored exactly");
        Equal(63, dropdown.value, "large selected index restored exactly");
        for (var index = 0; index < highWaterSlots.Length; index++)
            Equal(true, ReferenceEquals(highWaterSlots[index], dropdown.options[index]),
                "high-water OptionData slot remains stable " + index);
    }

    private static List<MaterialEditorEnumOption> CreateOptions()
    {
        return new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "Zero"),
            null,
            new MaterialEditorEnumOption(1f, "First"),
            new MaterialEditorEnumOption(1f, "Last"),
            new MaterialEditorEnumOption(2f, "Two")
        };
    }

    private static List<MaterialEditorEnumOption> CreateSequentialOptions(int count)
    {
        var options = new List<MaterialEditorEnumOption>(count);
        for (var index = 0; index < count; index++)
            options.Add(new MaterialEditorEnumOption(index, "Option " + index));
        return options;
    }

    private static void LegacyRebuild(
        Dropdown dropdown,
        IList<MaterialEditorEnumOption> options,
        float value,
        bool isMixed)
    {
        var indexValues = new List<float?>();
        dropdown.options.Clear();
        var selectedIndex = -1;
        if (isMixed)
        {
            dropdown.options.Add(new Dropdown.OptionData("Mixed"));
            indexValues.Add(null);
            selectedIndex = 0;
        }
        else if (MaterialEditorSemanticValuePolicy.FindEnumOptionIndex(options, value) < 0)
        {
            dropdown.options.Add(new Dropdown.OptionData("Unknown (" + value + ")"));
            indexValues.Add(null);
            selectedIndex = 0;
        }

        if (options != null)
        {
            foreach (var option in options)
            {
                if (option == null)
                    continue;
                if (!isMixed && option.Value == value)
                    selectedIndex = indexValues.Count;
                dropdown.options.Add(new Dropdown.OptionData(option.DisplayName));
                indexValues.Add(option.Value);
            }
        }
        dropdown.value = Mathf.Max(0, selectedIndex);
    }

    private static void AssertMappedValue(
        EnumDropdownOptionCache cache,
        int index,
        float expected,
        string name)
    {
        float actual;
        Equal(true, cache.TryGetValue(index, out actual), name + " exists");
        Equal(expected, actual, name);
    }

    private static void AssertNoMappedValue(
        EnumDropdownOptionCache cache,
        int index,
        string name)
    {
        float ignored;
        Equal(false, cache.TryGetValue(index, out ignored), name);
    }

    private static void AssertLabels(Dropdown dropdown, params string[] expected) =>
        AssertLabelsFrom(dropdown, 0, expected);

    private static void AssertLabelsFrom(
        Dropdown dropdown,
        int start,
        params string[] expected)
    {
        Equal(start + expected.Length, dropdown.options.Count, "displayed option count");
        for (var index = 0; index < expected.Length; index++)
            Equal(expected[index], dropdown.options[start + index].text,
                "displayed option " + (start + index));
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
