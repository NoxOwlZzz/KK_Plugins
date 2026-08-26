using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    // Process-wide warning-once cache. Only bounded ordinal strings are retained;
    // no material, row, target, graph, or other scene reference can escape here.
    internal static class MaterialConditionWarningCache
    {
        private const int Capacity = 256;
        private static readonly object Sync = new object();
        private static readonly HashSet<string> Keys =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly Queue<string> Order = new Queue<string>();

        internal static int Count
        {
            get
            {
                lock (Sync)
                    return Keys.Count;
            }
        }

        internal static bool TryRecord(string key)
        {
            lock (Sync)
            {
                if (!Keys.Add(key))
                    return false;
                Order.Enqueue(key);
                while (Order.Count > Capacity)
                    Keys.Remove(Order.Dequeue());
                return true;
            }
        }
    }
}
