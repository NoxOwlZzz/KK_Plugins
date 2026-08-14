using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static class MaterialCubemapOriginalSnapshot
    {
        private sealed class MaterialReferenceComparer : IEqualityComparer<Material>
        {
            internal static readonly MaterialReferenceComparer Instance =
                new MaterialReferenceComparer();

            public bool Equals(Material left, Material right)
            {
                return object.ReferenceEquals(left, right);
            }

            public int GetHashCode(Material material)
            {
                return object.ReferenceEquals(material, null)
                    ? 0
                    : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(material);
            }
        }

        internal static Dictionary<Material, Cubemap> SynchronizeByMaterialReference(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IDictionary<Material, Cubemap> originals)
        {
            var synchronized = NewReferenceDictionary();
            var materials = GetMatchingMaterials(gameObject, materialName, propertyName);
            var fullPropertyName = "_" + propertyName;
            for (var index = 0; index < materials.Count; index++)
            {
                var material = materials[index];
                Cubemap original;
                synchronized.Add(
                    material,
                    TryGetByReference(originals, material, out original)
                        ? original
                        : material.GetTexture(fullPropertyName) as Cubemap);
            }
            return synchronized;
        }

        internal static List<Cubemap> GetOrderedValues(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IDictionary<Material, Cubemap> originals)
        {
            if (originals == null)
                return null;

            var values = new List<Cubemap>();
            var materials = GetMatchingMaterials(gameObject, materialName, propertyName);
            for (var index = 0; index < materials.Count; index++)
            {
                Cubemap original;
                if (!TryGetByReference(originals, materials[index], out original))
                    return null;
                values.Add(original);
            }
            return values;
        }

        internal static bool TryRemapToCurrentMaterials(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IList<Cubemap> orderedValues,
            out Dictionary<Material, Cubemap> remapped)
        {
            remapped = NewReferenceDictionary();
            if (orderedValues == null || orderedValues.Count == 0)
                return false;

            var materials = GetMatchingMaterials(gameObject, materialName, propertyName);
            if (materials.Count != orderedValues.Count)
                return false;

            for (var index = 0; index < materials.Count; index++)
                remapped.Add(materials[index], orderedValues[index]);
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

            var materials = GetMatchingMaterials(gameObject, materialName, propertyName);
            var fullPropertyName = "_" + propertyName;
            for (var index = 0; index < materials.Count; index++)
            {
                Cubemap original;
                if (TryGetByReference(originals, materials[index], out original))
                    materials[index].SetTexture(fullPropertyName, original);
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
                    if (object.ReferenceEquals(entry.Key, material))
                    {
                        original = entry.Value;
                        return true;
                    }

            original = null;
            return false;
        }

        private static List<Material> GetMatchingMaterials(
            GameObject gameObject,
            string materialName,
            string propertyName)
        {
            var matches = new List<Material>();
            if (gameObject == null)
                return matches;

            var materials = MaterialAPI.GetObjectMaterials(gameObject, materialName);
            var fullPropertyName = "_" + propertyName;
            for (var index = 0; index < materials.Count; index++)
            {
                var material = materials[index];
                if (material == null
                    || material.NameFormatted() != materialName
                    || !material.HasProperty(fullPropertyName)
                    || ContainsReference(matches, material))
                    continue;

                matches.Add(material);
            }
            return matches;
        }

        private static bool ContainsReference(IList<Material> materials, Material candidate)
        {
            for (var index = 0; index < materials.Count; index++)
                if (object.ReferenceEquals(materials[index], candidate))
                    return true;
            return false;
        }
    }
}
