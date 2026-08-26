using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Runtime-only material property lookup. Property names remain strings at every
    /// persistence and extension boundary; this handle is only used for Unity calls.
    /// </summary>
    internal struct MaterialPropertyHandle
    {
        internal MaterialPropertyHandle(string fullName, int id)
        {
            FullName = fullName;
            Id = id;
        }

        internal readonly string FullName;
        internal readonly int Id;
    }

    /// <summary>
    /// Bounded FIFO cache for the prefixed names accepted by Unity's Material API.
    /// Hits neither allocate the prefixed string nor call Shader.PropertyToID.
    /// </summary>
    internal static class MaterialPropertyIdCache
    {
        internal const int Capacity = 512;

        private static readonly Dictionary<string, MaterialPropertyHandle> Handles =
            new Dictionary<string, MaterialPropertyHandle>(StringComparer.Ordinal);
        private static readonly Queue<string> InsertionOrder = new Queue<string>();

        internal static MaterialPropertyHandle Get(string propertyName)
        {
            // Interpolation in the legacy call sites converted both null and empty to
            // "_". Use the unprefixed value as the key so a hit creates no new string.
            // Unity Material/Shader access is main-thread-only, so this cache follows
            // the same ownership instead of placing a Monitor on every hot-path hit.
            var cacheKey = propertyName ?? string.Empty;
            MaterialPropertyHandle handle;
            if (Handles.TryGetValue(cacheKey, out handle))
                return handle;

            var fullName = string.Concat("_", cacheKey);
            handle = new MaterialPropertyHandle(
                fullName,
                Shader.PropertyToID(fullName));

            // Resolve before eviction so a failing Unity call cannot corrupt FIFO
            // state or evict a valid entry.
            if (Handles.Count >= Capacity)
                Handles.Remove(InsertionOrder.Dequeue());
            Handles.Add(cacheKey, handle);
            InsertionOrder.Enqueue(cacheKey);
            return handle;
        }

    }
}
