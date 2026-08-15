using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
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
