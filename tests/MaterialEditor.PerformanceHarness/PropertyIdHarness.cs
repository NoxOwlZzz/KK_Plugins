using MaterialEditorAPI;

namespace UnityEngine
{
    internal static class Shader
    {
        private static readonly Dictionary<string, int> Ids =
            new Dictionary<string, int>(StringComparer.Ordinal);

        internal static int CallCount { get; private set; }

        public static int PropertyToID(string propertyName)
        {
            CallCount++;
            int id;
            if (!Ids.TryGetValue(propertyName, out id))
            {
                id = Ids.Count + 1;
                Ids.Add(propertyName, id);
            }
            return id;
        }

        internal static void Reset()
        {
            Ids.Clear();
            CallCount = 0;
        }
    }
}

internal sealed class PropertyIdHarnessResult
{
    internal long LegacyAllocatedBytes;
    internal long CachedAllocatedBytes;
    internal int LegacyResolverCalls;
    internal int CachedResolverCalls;
    internal bool SameIds;
}

internal static class PropertyIdHarness
{
    private const int PropertyCount = 250;
    private const int Iterations = 100000;
    private static int _sink;

    internal static PropertyIdHarnessResult Measure()
    {
        var names = new string[PropertyCount];
        var expectedIds = new int[PropertyCount];
        for (var index = 0; index < names.Length; index++)
            names[index] = "KkltProperty" + index;

        UnityEngine.Shader.Reset();
        for (var index = 0; index < names.Length; index++)
            expectedIds[index] = UnityEngine.Shader.PropertyToID(
                string.Concat("_", names[index]));

        Collect();
        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < Iterations; index++)
            _sink ^= UnityEngine.Shader.PropertyToID(
                string.Concat("_", names[index % names.Length]));
        var legacyAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
        var legacyCalls = UnityEngine.Shader.CallCount;

        UnityEngine.Shader.Reset();
        MaterialPropertyIdCache.ResetForTests();
        var sameIds = true;
        for (var index = 0; index < names.Length; index++)
        {
            var handle = MaterialPropertyIdCache.Get(names[index]);
            sameIds &= handle.Id == expectedIds[index]
                       && handle.FullName == "_" + names[index];
        }

        Collect();
        allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < Iterations; index++)
            _sink ^= MaterialPropertyIdCache.Get(names[index % names.Length]).Id;
        var cachedAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
        var cachedCalls = UnityEngine.Shader.CallCount;
        GC.KeepAlive(_sink);

        return new PropertyIdHarnessResult
        {
            LegacyAllocatedBytes = legacyAllocated,
            CachedAllocatedBytes = cachedAllocated,
            LegacyResolverCalls = legacyCalls,
            CachedResolverCalls = cachedCalls,
            SameIds = sameIds
        };
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
