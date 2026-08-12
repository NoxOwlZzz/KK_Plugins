using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static class MaterialTextureOriginalSnapshot
    {
        private sealed class MaterialReferenceComparer : IEqualityComparer<Material>
        {
            internal static readonly MaterialReferenceComparer Instance = new MaterialReferenceComparer();

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

        internal static Dictionary<Material, Texture> SynchronizeByMaterialReference(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IDictionary<Material, Texture> originals)
        {
            var synchronized = NewReferenceDictionary();
            var materials = GetMatchingMaterials(gameObject, materialName, propertyName);
            var fullPropertyName = "_" + propertyName;
            for (var index = 0; index < materials.Count; index++)
            {
                var material = materials[index];
                Texture original;
                synchronized.Add(
                    material,
                    TryGetByReference(originals, material, out original)
                        ? original
                        : material.GetTexture(fullPropertyName));
            }
            return synchronized;
        }

        internal static List<Texture> GetOrderedValues(
            GameObject gameObject,
            string materialName,
            string propertyName,
            IDictionary<Material, Texture> originals)
        {
            if (originals == null)
                return null;

            var values = new List<Texture>();
            var materials = GetMatchingMaterials(gameObject, materialName, propertyName);
            for (var index = 0; index < materials.Count; index++)
            {
                Texture original;
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
            IList<Texture> orderedValues,
            out Dictionary<Material, Texture> remapped)
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
            IDictionary<Material, Texture> originals)
        {
            if (originals == null)
                return;

            var materials = GetMatchingMaterials(gameObject, materialName, propertyName);
            var fullPropertyName = "_" + propertyName;
            for (var index = 0; index < materials.Count; index++)
            {
                Texture original;
                if (TryGetByReference(originals, materials[index], out original))
                    materials[index].SetTexture(fullPropertyName, original);
            }
        }

        internal static Dictionary<Material, Texture> CloneByMaterialReference(
            IDictionary<Material, Texture> originals)
        {
            var clone = NewReferenceDictionary();
            if (originals != null)
                foreach (var original in originals)
                    clone.Add(original.Key, original.Value);
            return clone;
        }

        private static Dictionary<Material, Texture> NewReferenceDictionary()
        {
            return new Dictionary<Material, Texture>(MaterialReferenceComparer.Instance);
        }

        private static bool TryGetByReference(
            IDictionary<Material, Texture> originals,
            Material material,
            out Texture original)
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

    /// <summary>
    /// Class containing material data, used to for copy and paste of material edits
    /// </summary>
    public class CopyContainer
    {
        /// <summary>
        /// List of float property edits
        /// </summary>
        public List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        /// <summary>
        /// List of keyword property edits
        /// </summary>
        public List<MaterialKeywordProperty> MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
        /// <summary>
        /// List of color property edits
        /// </summary>
        public List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        /// <summary>
        /// List of texture property edits
        /// </summary>
        public List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        /// <summary>
        /// List of shader edits
        /// </summary>
        public List<MaterialShader> MaterialShaderList = new List<MaterialShader>();
        /// <summary>
        /// List of projector edits
        /// </summary>
        public List<ProjectorProperty> ProjectorPropertyList = new List<ProjectorProperty>();

        /// <summary>
        /// Whether there are any copied edits
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (MaterialFloatPropertyList.Count == 0 && MaterialKeywordPropertyList.Count == 0 && MaterialColorPropertyList.Count == 0 && MaterialTexturePropertyList.Count == 0 && MaterialShaderList.Count == 0 && ProjectorPropertyList.Count == 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Clear any copied edits
        /// </summary>
        public void ClearAll()
        {
            MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
            MaterialColorPropertyList = new List<MaterialColorProperty>();
            MaterialTexturePropertyList = new List<MaterialTextureProperty>();
            MaterialShaderList = new List<MaterialShader>();
            ProjectorPropertyList = new List<ProjectorProperty>();
        }

        /// <summary>
        /// Data storage class for float properties
        /// </summary>
        public class MaterialFloatProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public float Value;

            /// <summary>
            /// Data storage class for float properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialFloatProperty(string property, float value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for keyword properties
        /// </summary>
        public class MaterialKeywordProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public bool Value;

            /// <summary>
            /// Data storage class for keyword properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialKeywordProperty(string property, bool value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for color properties
        /// </summary>
        public class MaterialColorProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public Color Value;

            /// <summary>
            /// Data storage class for color properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialColorProperty(string property, Color value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for texture properties
        /// </summary>
        public class MaterialTextureProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// ID of the texture as stored in the texture dictionary
            /// </summary>
            public byte[] Data;
            /// <summary>
            /// Kind of texture represented by Data.
            /// </summary>
            internal MaterialAPI.ShaderPropertyType TextureKind;
            /// <summary>
            /// Texture offset value
            /// </summary>
            public Vector2? Offset;
            /// <summary>
            /// Texture scale value
            /// </summary>
            public Vector2? Scale;

            /// <summary>
            /// Data storage class for texture properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="data">Byte array containing the texture</param>
            /// <param name="offset">Texture offset value</param>
            /// <param name="scale">Texture scale value</param>
            public MaterialTextureProperty(string property, byte[] data = null, Vector2? offset = null, Vector2? scale = null)
                : this(property, data, offset, scale, MaterialAPI.ShaderPropertyType.Texture)
            {
            }

            internal MaterialTextureProperty(string property, byte[] data, Vector2? offset, Vector2? scale, MaterialAPI.ShaderPropertyType textureKind)
            {
                Property = property;
                Data = data;
                TextureKind = textureKind;
                Offset = textureKind == MaterialAPI.ShaderPropertyType.Cubemap ? null : offset;
                Scale = textureKind == MaterialAPI.ShaderPropertyType.Cubemap ? null : scale;
            }
        }

        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        public class MaterialShader
        {
            /// <summary>
            /// Name of the shader
            /// </summary>
            public string ShaderName;
            /// <summary>
            /// Render queue
            /// </summary>
            public int? RenderQueue;

            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="renderQueue">Render queue</param>
            public MaterialShader(string shaderName, int? renderQueue)
            {
                ShaderName = shaderName;
                RenderQueue = renderQueue;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="shaderName">Name of the shader</param>
            public MaterialShader(string shaderName)
            {
                ShaderName = shaderName;
            }
            /// <summary>
            /// Data storage class for shader data
            /// </summary>
            /// <param name="renderQueue">Render queue</param>
            public MaterialShader(int? renderQueue)
            {
                RenderQueue = renderQueue;
            }

            /// <summary>
            /// Check if the shader name and render queue are both null. Safe to delete this data if true.
            /// </summary>
            /// <returns></returns>
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }

        /// <summary>
        /// Data storage class for projector properties
        /// </summary>
        public class ProjectorProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public MaterialAPI.ProjectorProperties Property;
            /// <summary>
            /// Value
            /// </summary>
            public float Value;

            /// <summary>
            /// Data storage class for projector properties
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public ProjectorProperty(MaterialAPI.ProjectorProperties property, float value)
            {
                Property = property;
                Value = value;
            }
        }
    }
}
