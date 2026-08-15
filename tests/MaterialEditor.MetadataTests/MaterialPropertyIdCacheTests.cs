using MaterialEditorAPI;
using System.Reflection;
using UnityEngine;

internal static class MaterialPropertyIdCacheTests
{
    private static int _sink;

    internal static long LegacyAllocatedBytes { get; private set; }
    internal static long CachedAllocatedBytes { get; private set; }

    internal static void Run()
    {
        PrefixAndKeySemanticsRemainExact();
        RepeatedHitsResolveOnlyOnceAndIncrementOneMetric();
        FifoCapacityIsBoundedAndHitsDoNotReorder();
        ResolverFailureDoesNotCorruptFifoState();
        ProductionSourcesKeepIdsRuntimeOnlyAndPlayHomeSafe();
        MeasureLegacyAgainstCachedHits();
        Console.WriteLine(
            "Material PropertyToID cache tests passed; legacy allocations="
            + LegacyAllocatedBytes
            + " B/10000, cached allocations="
            + CachedAllocatedBytes
            + " B/10000.");
    }

    private static void PrefixAndKeySemanticsRemainExact()
    {
        Reset();

        var regular = MaterialPropertyIdCache.Get("MainTex");
        Equal("_MainTex", regular.FullName, "single prefix");

        var nullName = MaterialPropertyIdCache.Get(null);
        var emptyName = MaterialPropertyIdCache.Get(string.Empty);
        Equal("_", nullName.FullName, "null interpolation semantics");
        Equal("_", emptyName.FullName, "empty interpolation semantics");
        Equal(nullName.Id, emptyName.Id, "null and empty share the same Unity property");

        var alreadyPrefixed = MaterialPropertyIdCache.Get("_MainTex");
        Equal("__MainTex", alreadyPrefixed.FullName, "existing underscore remains doubled");

        var upper = MaterialPropertyIdCache.Get("CaseSensitive");
        var lower = MaterialPropertyIdCache.Get("casesensitive");
        NotEqual(upper.Id, lower.Id, "ordinal case distinction");
        Equal(5, MaterialPropertyIdCache.Count, "semantic cache entries");
    }

    private static void RepeatedHitsResolveOnlyOnceAndIncrementOneMetric()
    {
        Reset();
        MaterialEditorPerformance.Reset();
        MaterialEditorPerformance.Configure(false, true, 0d, null, null);
        try
        {
            var expected = MaterialPropertyIdCache.Get("Repeated");
            for (var index = 0; index < 10000; index++)
            {
                var actual = MaterialPropertyIdCache.Get("Repeated");
                Equal(expected.Id, actual.Id, "cached ID");
                Equal(expected.FullName, actual.FullName, "cached full name");
            }

            Equal(1, Shader.PropertyToIdCallCount, "Unity resolver miss count");
            var snapshot = MaterialEditorPerformance.CaptureSnapshot();
            Equal(
                1L,
                snapshot.GetCount(MaterialEditorPerformanceMetric.PropertyToIdCalls),
                "PropertyToID metric counts misses only");
        }
        finally
        {
            MaterialEditorPerformance.Configure(false, false, 0d, null, null);
            MaterialEditorPerformance.Reset();
        }
    }

    private static void FifoCapacityIsBoundedAndHitsDoNotReorder()
    {
        Reset();
        for (var index = 0; index < MaterialPropertyIdCache.Capacity; index++)
            MaterialPropertyIdCache.Get("Property" + index);

        Equal(MaterialPropertyIdCache.Capacity, MaterialPropertyIdCache.Count, "full capacity");
        var callsAtCapacity = Shader.PropertyToIdCallCount;

        // FIFO, not LRU: a hit on the oldest entry must not protect it.
        MaterialPropertyIdCache.Get("Property0");
        Equal(callsAtCapacity, Shader.PropertyToIdCallCount, "oldest entry hit");
        MaterialPropertyIdCache.Get("Property512");
        Equal(MaterialPropertyIdCache.Capacity, MaterialPropertyIdCache.Count, "bounded after 513th key");
        Equal(callsAtCapacity + 1, Shader.PropertyToIdCallCount, "513th key miss");

        MaterialPropertyIdCache.Get("Property0");
        Equal(callsAtCapacity + 2, Shader.PropertyToIdCallCount, "oldest FIFO entry evicted");
        Equal(MaterialPropertyIdCache.Capacity, MaterialPropertyIdCache.Count, "bounded after reinsert");
    }

    private static void ResolverFailureDoesNotCorruptFifoState()
    {
        Reset();
        for (var index = 0; index < MaterialPropertyIdCache.Capacity; index++)
            MaterialPropertyIdCache.Get("Stable" + index);
        var callsBeforeFailure = Shader.PropertyToIdCallCount;

        Shader.ThrowOnPropertyName = "_Throws";
        Throws<InvalidOperationException>(
            () => MaterialPropertyIdCache.Get("Throws"),
            "resolver failure");
        Shader.ThrowOnPropertyName = null;

        Equal(MaterialPropertyIdCache.Capacity, MaterialPropertyIdCache.Count, "failure retains count");
        MaterialPropertyIdCache.Get("Stable0");
        Equal(callsBeforeFailure + 1, Shader.PropertyToIdCallCount, "failure retains oldest entry");

        MaterialPropertyIdCache.Get("AfterFailure");
        MaterialPropertyIdCache.Get("Stable0");
        Equal(callsBeforeFailure + 3, Shader.PropertyToIdCallCount, "FIFO remains usable after failure");
    }

    private static void ProductionSourcesKeepIdsRuntimeOnlyAndPlayHomeSafe()
    {
        var root = FindRepositoryRoot();
        var cache = File.ReadAllText(Path.Combine(
            root, "src", "MaterialEditor.Base", "MaterialPropertyIdCache.cs"));
        var access = File.ReadAllText(Path.Combine(
            root, "src", "MaterialEditor.Base", "MaterialPropertyAccess.cs"));
        var materialApi = File.ReadAllText(Path.Combine(
            root, "src", "MaterialEditor.Base", "MaterialAPI.cs"));
        var descriptorFactory = File.ReadAllText(Path.Combine(
            root, "src", "MaterialEditor.Base", "UI", "UI.PropertyDescriptor.cs"));
        var sectionPresenter = File.ReadAllText(Path.Combine(
            root, "src", "MaterialEditor.Base", "UI", "UI.MaterialSectionPresenter.cs"));
        var timeline = File.ReadAllText(Path.Combine(
            root,
            "src",
            "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.TimelineCompatibilityHelper.cs"));

        DoesNotContain(cache, "MessagePack", "runtime cache is not serialized");
        DoesNotContain(cache, "PluginData", "runtime cache is not persisted");
        DoesNotContain(cache, "public ", "cache adds no public API");
        Contains(cache, "StringComparer.Ordinal", "case-sensitive cache key");
        Contains(cache, "internal const int Capacity = 512;", "bounded FIFO capacity");

        Equal(4, Count(access, "#if PH"), "four PH-only offset/scale branches");
        Equal(4, Count(access, "property.FullName"), "PH uses strings only where required");
        Contains(access, "material.HasProperty(property.Id)", "HasProperty integer overload");
        Contains(access, "material.GetFloat(property.Id)", "float integer overload");
        Contains(access, "material.GetTexture(property.Id)", "texture integer overload");

        foreach (var method in new[]
                 {
                     "SetFloat", "SetColor", "SetVector", "SetTexture",
                     "SetTextureOffset", "SetTextureScale"
                 })
        {
            var methodSource = ExtractMethod(
                materialApi,
                "public static bool " + method + "(");
            Contains(
                methodSource,
                "MaterialPropertyIdCache.Get(propertyName)",
                method + " uses cached handle");
            var emptyGuard = methodSource.IndexOf(".Count == 0) return false;", StringComparison.Ordinal);
            var resolve = methodSource.IndexOf("MaterialPropertyIdCache.Get(propertyName)", StringComparison.Ordinal);
            if (emptyGuard < 0 || emptyGuard > resolve)
                throw new InvalidOperationException(
                    method + " must skip PropertyToID resolution when no material exists.");
        }

        var keyword = ExtractMethod(materialApi, "public static bool SetKeyword(");
        DoesNotContain(keyword, "MaterialPropertyIdCache", "keywords remain strings");
        Contains(keyword, "EnableKeyword($\"_{propertyName}\")", "keyword name remains persisted spelling");

        Contains(
            descriptorFactory,
            "Type == ShaderPropertyType.Keyword",
            "property descriptors do not resolve keyword IDs");
        Contains(
            descriptorFactory,
            "MaterialPropertyAccess.GetTextureOffset",
            "descriptor texture offset uses PH-safe helper");
        DoesNotContain(
            descriptorFactory,
            "GetFloat($\"_{",
            "descriptor float getters avoid prefixed allocation");
        Contains(
            sectionPresenter,
            "MaterialPropertyAccess.HasProperty",
            "presenter compatibility uses integer property handles");
        Contains(
            sectionPresenter,
            "material.IsKeywordEnabled($\"_{propertyName}\")",
            "presenter keyword reads remain strings");
        DoesNotContain(
            timeline,
            "GetFloat($\"_{parameter.propertyName}",
            "Timeline float getter avoids prefixed allocation");
        Contains(
            timeline,
            "MaterialPropertyAccess.GetTextureScale",
            "Timeline texture scale uses target-safe helper");

        foreach (var relativePath in new[]
                 {
                     Path.Combine("src", "MaterialEditor.Core", "Core.MaterialEditor.TextureSaveHandler.cs"),
                     Path.Combine("src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Models.cs"),
                     Path.Combine("src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Models.cs"),
                     Path.Combine("src", "MaterialEditor.Base", "MaterialEditorExtension.Contracts.cs")
                 })
        {
            var source = File.ReadAllText(Path.Combine(root, relativePath));
            DoesNotContain(source, "MaterialPropertyHandle", relativePath + " stores no runtime handle");
            DoesNotContain(source, "PropertyToID", relativePath + " stores no runtime ID");
        }
    }

    private static void MeasureLegacyAgainstCachedHits()
    {
        const int iterations = 10000;
        var propertyName = new string("BenchmarkProperty".ToCharArray());

        Reset();
        _sink ^= Shader.PropertyToID(string.Concat("_", propertyName));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
            _sink ^= Shader.PropertyToID(string.Concat("_", propertyName));
        LegacyAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Reset();
        _sink ^= MaterialPropertyIdCache.Get(propertyName).Id;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
            _sink ^= MaterialPropertyIdCache.Get(propertyName).Id;
        CachedAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        if (CachedAllocatedBytes >= LegacyAllocatedBytes)
            throw new InvalidOperationException(
                "Cached PropertyToID path did not reduce allocations: legacy="
                + LegacyAllocatedBytes
                + ", cached="
                + CachedAllocatedBytes
                + ".");
        Equal(1, Shader.PropertyToIdCallCount, "cached benchmark resolver calls");
        GC.KeepAlive(_sink);
    }

    private static void Reset()
    {
        MaterialPropertyIdCache.ResetForTests();
        Shader.ResetPropertyIds();
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidOperationException("Could not find method: " + signature);
        var bodyStart = source.IndexOf('{', start);
        var depth = 0;
        for (var index = bodyStart; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(start, index - start + 1);
        }
        throw new InvalidOperationException("Could not parse method: " + signature);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }

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
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static void Contains(string source, string value, string name)
    {
        if (!source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + ": missing '" + value + "'.");
    }

    private static void DoesNotContain(string source, string value, string name)
    {
        if (source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + ": found forbidden '" + value + "'.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }

    private static void NotEqual<T>(T left, T right, string name)
    {
        if (EqualityComparer<T>.Default.Equals(left, right))
            throw new InvalidOperationException(name + ": values unexpectedly match.");
    }

    private static void Throws<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException(name + ": expected " + typeof(TException).Name + ".");
    }
}
