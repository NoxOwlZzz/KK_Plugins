using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Class containing material data, used to for copy and paste of material edits
    /// </summary>
    public class CopyContainer
    {
        public List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        public List<MaterialKeywordProperty> MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
        public List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        public List<MaterialVectorProperty> MaterialVectorPropertyList = new List<MaterialVectorProperty>();
        public List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        public List<MaterialCubemapProperty> MaterialCubemapPropertyList = new List<MaterialCubemapProperty>();
        public List<MaterialShader> MaterialShaderList = new List<MaterialShader>();
        public List<ProjectorProperty> ProjectorPropertyList = new List<ProjectorProperty>();

        /// <summary>
        /// Whether there are any copied edits
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return !HasAny(MaterialFloatPropertyList)
                       && !HasAny(MaterialKeywordPropertyList)
                       && !HasAny(MaterialColorPropertyList)
                       && !HasAny(MaterialVectorPropertyList)
                       && !HasAny(MaterialTexturePropertyList)
                       && !HasAny(MaterialCubemapPropertyList)
                       && !HasAny(MaterialShaderList)
                       && !HasAny(ProjectorPropertyList);
            }
        }

        private static bool HasAny<T>(IList<T> values) where T : class
        {
            if (values == null)
                return false;
            for (var index = 0; index < values.Count; index++)
                if (values[index] != null)
                    return true;
            return false;
        }

        /// <summary>
        /// Clear any copied edits
        /// </summary>
        public void ClearAll()
        {
            MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
            MaterialColorPropertyList = new List<MaterialColorProperty>();
            MaterialVectorPropertyList = new List<MaterialVectorProperty>();
            MaterialTexturePropertyList = new List<MaterialTextureProperty>();
            MaterialCubemapPropertyList = new List<MaterialCubemapProperty>();
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

        public class MaterialVectorProperty
        {
            public string Property;
            public Vector4 Value;

            public MaterialVectorProperty(string property, Vector4 value)
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
            {
                Property = property;
                Data = data;
                Offset = offset;
                Scale = scale;
            }
        }

        public class MaterialCubemapProperty
        {
            public string Property;
            /// <summary>
            /// Encoded PNG or Radiance HDR Cubemap source data.
            /// </summary>
            public byte[] Data;

            public MaterialCubemapProperty(string property, byte[] data = null)
            {
                Property = property;
                Data = data;
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
