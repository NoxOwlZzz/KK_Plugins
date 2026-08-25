using System;
using System.Collections.Generic;
using UnityEngine;

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
    internal sealed class MaterialCubemapOriginalBinding
    {
        internal MaterialCubemapOriginalBinding(
            MaterialCubemapBindingIdentity identity,
            Cubemap value)
        {
            Identity = identity;
            Value = value;
        }

        internal MaterialCubemapBindingIdentity Identity { get; private set; }
        internal Cubemap Value { get; private set; }
    }

    internal static class MaterialCubemapOriginalSnapshot
    {
        private sealed class MaterialReferenceComparer : IEqualityComparer<Material>
        {
            internal static readonly MaterialReferenceComparer Instance =
                new MaterialReferenceComparer();

            public bool Equals(Material left, Material right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(Material material)
            {
                return ReferenceEquals(material, null)
                    ? 0
                    : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(material);
            }
        }

        private sealed class MaterialBinding
        {
            internal MaterialCubemapBindingIdentity Identity;
            internal Material Material;
        }

        internal static Dictionary<Material, Cubemap> SynchronizeByMaterialReference(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IDictionary<Material, Cubemap> originals)
        {
            var synchronized = NewReferenceDictionary();
            var bindings = GetMatchingBindings(gameObject, materialName, propertyName);
            var fullPropertyName = "_" + propertyName;
            for (var index = 0; index < bindings.Count; index++)
            {
                var material = bindings[index].Material;
                if (synchronized.ContainsKey(material))
                    continue;

                Cubemap original;
                synchronized.Add(
                    material,
                    TryGetByReference(originals, material, out original)
                        ? original
                        : material.GetTexture(fullPropertyName) as Cubemap);
            }
            return synchronized;
        }

        internal static List<MaterialCubemapOriginalBinding> GetStableValues(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IDictionary<Material, Cubemap> originals)
        {
            if (originals == null)
                return null;

            var values = new List<MaterialCubemapOriginalBinding>();
            var identities = new HashSet<MaterialCubemapBindingIdentity>();
            var bindings = GetMatchingBindings(gameObject, materialName, propertyName);
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                Cubemap original;
                if (!identities.Add(binding.Identity)
                    || !TryGetByReference(originals, binding.Material, out original))
                    return null;
                values.Add(new MaterialCubemapOriginalBinding(binding.Identity, original));
            }
            return values;
        }

        internal static List<MaterialCubemapOriginalBinding> CloneStableValues(
            IList<MaterialCubemapOriginalBinding> values)
        {
            if (values == null)
                return null;

            var clone = new List<MaterialCubemapOriginalBinding>(values.Count);
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (value == null)
                    return null;
                clone.Add(new MaterialCubemapOriginalBinding(value.Identity, value.Value));
            }
            return clone;
        }

        internal static bool TryRemapToCurrentMaterials(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IList<MaterialCubemapOriginalBinding> stableValues,
            out Dictionary<Material, Cubemap> remapped,
            out string failureReason)
        {
            remapped = NewReferenceDictionary();
            failureReason = null;
            if (stableValues == null || stableValues.Count == 0)
            {
                failureReason = "The inherited Cubemap original snapshot is empty.";
                return false;
            }

            var currentBindings = GetMatchingBindings(gameObject, materialName, propertyName);
            var savedIdentities = new List<MaterialCubemapBindingIdentity>(stableValues.Count);
            for (var index = 0; index < stableValues.Count; index++)
            {
                var value = stableValues[index];
                if (value == null)
                {
                    failureReason = "The inherited Cubemap original snapshot contains an invalid entry.";
                    return false;
                }
                savedIdentities.Add(value.Identity);
            }

            var currentIdentities = new List<MaterialCubemapBindingIdentity>(currentBindings.Count);
            for (var index = 0; index < currentBindings.Count; index++)
                currentIdentities.Add(currentBindings[index].Identity);

            int[] savedIndexByCurrentIndex;
            if (!MaterialCubemapIdentityMatcher.TryMatch(
                    savedIdentities,
                    currentIdentities,
                    out savedIndexByCurrentIndex,
                    out failureReason))
                return false;

            for (var index = 0; index < currentBindings.Count; index++)
            {
                var material = currentBindings[index].Material;
                var original = stableValues[savedIndexByCurrentIndex[index]].Value;
                Cubemap existing;
                if (TryGetByReference(remapped, material, out existing))
                {
                    if (!ReferenceEquals(existing, original))
                    {
                        remapped.Clear();
                        failureReason = "One material reference maps to conflicting Cubemap originals.";
                        return false;
                    }
                    continue;
                }
                remapped.Add(material, original);
            }
            return true;
        }

        internal static void RestoreByMaterialReference(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IDictionary<Material, Cubemap> originals)
        {
            if (originals == null)
                return;

            var restored = NewReferenceDictionary();
            var bindings = GetMatchingBindings(gameObject, materialName, propertyName);
            var fullPropertyName = "_" + propertyName;
            for (var index = 0; index < bindings.Count; index++)
            {
                var material = bindings[index].Material;
                if (restored.ContainsKey(material))
                    continue;

                Cubemap original;
                if (TryGetByReference(originals, material, out original))
                {
                    material.SetTexture(fullPropertyName, original);
                    restored.Add(material, original);
                }
            }
        }

        internal static Dictionary<Material, Cubemap> CloneByMaterialReference(
            IDictionary<Material, Cubemap> originals)
        {
            var clone = NewReferenceDictionary();
            if (originals != null)
                foreach (var original in originals)
                    clone.Add(original.Key, original.Value);
            return clone;
        }

        private static Dictionary<Material, Cubemap> NewReferenceDictionary()
        {
            return new Dictionary<Material, Cubemap>(MaterialReferenceComparer.Instance);
        }

        private static bool TryGetByReference(
            IDictionary<Material, Cubemap> originals,
            Material material,
            out Cubemap original)
        {
            if (originals != null)
                foreach (var entry in originals)
                    if (ReferenceEquals(entry.Key, material))
                    {
                        original = entry.Value;
                        return true;
                    }

            original = null;
            return false;
        }

        private static List<MaterialBinding> GetMatchingBindings(
            GameObject gameObject,
            string materialName,
            string propertyName)
        {
            var matches = new List<MaterialBinding>();
            if (gameObject == null
                || string.IsNullOrEmpty(materialName)
                || string.IsNullOrEmpty(propertyName))
                return matches;

            var root = gameObject.transform;
            var fullPropertyName = "_" + propertyName;
            foreach (var renderer in MaterialAPI.GetRendererList(gameObject))
            {
                if (renderer == null)
                    continue;

                var componentIndex = FindComponentIndex(
                    renderer.gameObject.GetComponents<Renderer>(),
                    renderer);
                var relativePath = GetRelativePath(root, renderer.transform);
                if (componentIndex < 0 || relativePath == null)
                    continue;

                var materials = renderer.materials;
                for (var slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (material == null
                        || material.NameFormatted() != materialName
                        || !material.HasProperty(fullPropertyName))
                        continue;

                    matches.Add(new MaterialBinding
                    {
                        Identity = new MaterialCubemapBindingIdentity(
                            MaterialCubemapBindingKind.Renderer,
                            relativePath,
                            componentIndex,
                            slot,
                            materialName,
                            propertyName),
                        Material = material
                    });
                }
            }

            foreach (var projector in MaterialAPI.GetProjectorList(gameObject))
            {
                if (projector == null || projector.material == null)
                    continue;

                var componentIndex = FindComponentIndex(
                    projector.gameObject.GetComponents<Projector>(),
                    projector);
                var relativePath = GetRelativePath(root, projector.transform);
                var material = projector.material;
                if (componentIndex < 0
                    || relativePath == null
                    || material.NameFormatted() != materialName
                    || !material.HasProperty(fullPropertyName))
                    continue;

                matches.Add(new MaterialBinding
                {
                    Identity = new MaterialCubemapBindingIdentity(
                        MaterialCubemapBindingKind.Projector,
                        relativePath,
                        componentIndex,
                        0,
                        materialName,
                        propertyName),
                    Material = material
                });
            }
            return matches;
        }

        private static int FindComponentIndex<T>(T[] components, T candidate)
            where T : UnityEngine.Object
        {
            for (var index = 0; index < components.Length; index++)
                if (ReferenceEquals(components[index], candidate))
                    return index;
            return -1;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null)
                return null;
            if (ReferenceEquals(root, target))
                return "$";

            var segments = new Stack<string>();
            var current = target;
            while (current != null && !ReferenceEquals(current, root))
            {
                var segmentName = current.name ?? string.Empty;
                segments.Push(
                    segmentName.Length + ":" + segmentName);
                current = current.parent;
            }
            if (!ReferenceEquals(current, root))
                return null;
            return "$/" + string.Join("/", segments.ToArray());
        }
    }
}
