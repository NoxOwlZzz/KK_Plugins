using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    internal enum MaterialCubemapBindingKind
    {
        Renderer = 0,
        Projector = 1
    }

    /// <summary>
    /// Stable identity for one material binding inside an object hierarchy.
    /// The root object's own name is deliberately excluded so duplicated or
    /// imported objects can be matched without relying on traversal order.
    /// </summary>
    internal sealed class MaterialCubemapBindingIdentity
        : IEquatable<MaterialCubemapBindingIdentity>
    {
        internal MaterialCubemapBindingIdentity(
            MaterialCubemapBindingKind kind,
            string relativePath,
            int componentIndex,
            int materialSlot,
            string materialName,
            string propertyName)
        {
            Kind = kind;
            RelativePath = relativePath ?? string.Empty;
            ComponentIndex = componentIndex;
            MaterialSlot = materialSlot;
            MaterialName = materialName ?? string.Empty;
            PropertyName = propertyName ?? string.Empty;
        }

        internal MaterialCubemapBindingKind Kind { get; private set; }
        internal string RelativePath { get; private set; }
        internal int ComponentIndex { get; private set; }
        internal int MaterialSlot { get; private set; }
        internal string MaterialName { get; private set; }
        internal string PropertyName { get; private set; }

        public bool Equals(MaterialCubemapBindingIdentity other)
        {
            return !ReferenceEquals(other, null)
                   && Kind == other.Kind
                   && ComponentIndex == other.ComponentIndex
                   && MaterialSlot == other.MaterialSlot
                   && string.Equals(RelativePath, other.RelativePath, StringComparison.Ordinal)
                   && string.Equals(MaterialName, other.MaterialName, StringComparison.Ordinal)
                   && string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MaterialCubemapBindingIdentity);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = hash * 397 ^ ComponentIndex;
                hash = hash * 397 ^ MaterialSlot;
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(RelativePath);
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(MaterialName);
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(PropertyName);
                return hash;
            }
        }

        public override string ToString()
        {
            return Kind + ":" + RelativePath
                   + "#" + ComponentIndex
                   + "[" + MaterialSlot + "]"
                   + "/" + MaterialName
                   + "/" + PropertyName;
        }
    }

    /// <summary>
    /// Pure identity matcher used by the Unity snapshot adapter. Exact unique
    /// identity is required; ambiguous or incomplete mappings are rejected.
    /// </summary>
    internal static class MaterialCubemapIdentityMatcher
    {
        internal static bool TryMatch(
            IList<MaterialCubemapBindingIdentity> saved,
            IList<MaterialCubemapBindingIdentity> current,
            out int[] savedIndexByCurrentIndex,
            out string failureReason)
        {
            savedIndexByCurrentIndex = null;
            failureReason = null;
            if (saved == null || current == null)
            {
                failureReason = "The Cubemap binding snapshot is missing.";
                return false;
            }
            if (saved.Count != current.Count)
            {
                failureReason = "The Cubemap binding count changed from "
                                + saved.Count + " to " + current.Count + ".";
                return false;
            }
            if (saved.Count == 0)
            {
                failureReason = "The Cubemap binding snapshot is empty.";
                return false;
            }

            var savedIndexes = new Dictionary<MaterialCubemapBindingIdentity, int>();
            for (var index = 0; index < saved.Count; index++)
            {
                var identity = saved[index];
                if (identity == null || savedIndexes.ContainsKey(identity))
                {
                    failureReason = "The saved Cubemap binding identity is ambiguous.";
                    return false;
                }
                savedIndexes.Add(identity, index);
            }

            var seenCurrent = new HashSet<MaterialCubemapBindingIdentity>();
            var mapping = new int[current.Count];
            for (var index = 0; index < current.Count; index++)
            {
                var identity = current[index];
                if (identity == null || !seenCurrent.Add(identity))
                {
                    failureReason = "The current Cubemap binding identity is ambiguous.";
                    return false;
                }

                int savedIndex;
                if (!savedIndexes.TryGetValue(identity, out savedIndex))
                {
                    failureReason = "No saved Cubemap binding matches " + identity + ".";
                    return false;
                }
                mapping[index] = savedIndex;
            }

            savedIndexByCurrentIndex = mapping;
            return true;
        }
    }
}
