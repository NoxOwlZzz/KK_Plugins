using KKAPI.Studio.SaveLoad;
using MaterialEditorAPI;
using Studio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;
namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<SceneController, SceneController.MaterialTextureProperty>;

    public partial class SceneController
    {
        /// <summary>
        /// Add a float property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialFloatProperty(int id, Material material, string propertyName, float value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var materialProperty = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(id, material.NameFormatted(), propertyName, value.ToString(CultureInfo.InvariantCulture), valueOriginal.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == materialProperty.ValueOriginal)
                    RemoveMaterialFloatProperty(id, material, propertyName, false);
                else
                    materialProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }

            if (setProperty)
                SetFloat(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValue(int id, Material material, string propertyName)
        {
            var value = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
            if (value.IsNullOrEmpty())
                return null;
            float parsedValue;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : (float?)null;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValueOriginal(int id, Material material, string propertyName)
        {
            var valueOriginal = MaterialFloatPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            float parsedValue;
            return float.TryParse(valueOriginal, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : (float?)null;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialFloatProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetFloat(go, material.NameFormatted(), propertyName, (float)original);
            }

            MaterialFloatPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }
        /// <summary>
        /// Add a keyword property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialKeywordProperty(int id, Material material, string propertyName, bool value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var materialProperty = MaterialKeywordPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (materialProperty == null)
            {
                bool valueOriginal = material.IsKeywordEnabled($"_{propertyName}");
                MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(id, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == materialProperty.ValueOriginal)
                    RemoveMaterialKeywordProperty(id, material, propertyName, false);
                else
                    materialProperty.Value = value;
            }

            if (setProperty)
                SetKeyword(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved renderer property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="renderer">Renderer being modified</param>
        /// <param name="property">Property of the renderer</param>
        /// <returns>Saved renderer property's original value</returns>
        public bool? GetMaterialKeywordPropertyValue(int id, Material material, string propertyName)
        {
            return MaterialKeywordPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public bool? GetMaterialKeywordPropertyValueOriginal(int id, Material material, string propertyName)
        {
            return MaterialKeywordPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialKeywordProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialKeywordPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetKeyword(go, material.NameFormatted(), propertyName, (bool)original);
            }

            MaterialKeywordPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }
        /// <summary>
        /// Add a color property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialColorProperty(int id, Material material, string propertyName, Color value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var colorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(id, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    RemoveMaterialColorProperty(id, material, propertyName, false);
                else
                    colorProperty.Value = value;
            }

            if (setProperty)
                SetColor(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValue(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.Value;
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValueOriginal(int id, Material material, string propertyName) =>
            MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.ValueOriginal;
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="id">Item ID as found in studio's dicObjectCtrl</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialColorProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetColor(go, material.NameFormatted(), propertyName, (Color)original);
            }

            MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
        }

        /// <summary>
        /// Add a vector property to be saved and loaded with the scene and optionally also update the materials.
        /// </summary>
        public void SetMaterialVectorProperty(int id, Material material, string propertyName, Vector4 value, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            var vectorProperty = MigrateLegacyMaterialVectorProperty(id, material.NameFormatted(), propertyName, go);
            if (vectorProperty == null)
            {
                Vector4 valueOriginal = material.GetVector($"_{propertyName}");
                if (value != valueOriginal)
                    MaterialVectorPropertyList.Add(new MaterialVectorProperty(id, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == vectorProperty.ValueOriginal)
                    RemoveMaterialVectorProperty(id, material, propertyName, false);
                else
                    vectorProperty.Value = value;
            }

            if (setProperty)
                SetVector(go, material.NameFormatted(), propertyName, value);
        }

        /// <summary>
        /// Get the saved material vector value or null if none is saved.
        /// </summary>
        public Vector4? GetMaterialVectorPropertyValue(int id, Material material, string propertyName) =>
            MigrateLegacyMaterialVectorProperty(id, material.NameFormatted(), propertyName, GetObjectByID(id))?.Value;

        /// <summary>
        /// Get the saved material vector property's original value or null if none is saved.
        /// </summary>
        public Vector4? GetMaterialVectorPropertyValueOriginal(int id, Material material, string propertyName) =>
            MigrateLegacyMaterialVectorProperty(id, material.NameFormatted(), propertyName, GetObjectByID(id))?.ValueOriginal;

        /// <summary>
        /// Remove the saved material vector value and optionally restore the original value.
        /// </summary>
        public void RemoveMaterialVectorProperty(int id, Material material, string propertyName, bool setProperty = true)
        {
            GameObject go = GetObjectByID(id);
            if (setProperty)
            {
                var original = GetMaterialVectorPropertyValueOriginal(id, material, propertyName);
                if (original != null)
                    SetVector(go, material.NameFormatted(), propertyName, original.Value);
            }

            MaterialVectorPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            MaterialColorPropertyList.RemoveAll(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted() && IsVectorProperty(go, x.MaterialName, x.Property));
        }

        /// <summary>
        /// Convert a legacy color-backed vector override to the native vector list in memory.
        /// Native vector data wins if a transitional save contains both keys.
        /// </summary>
        private MaterialVectorProperty MigrateLegacyMaterialVectorProperty(int id, string materialName, string propertyName, GameObject go)
        {
            var vectorProperty = MaterialVectorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName);
            if (!IsVectorProperty(go, materialName, propertyName))
                return vectorProperty;

            var legacyColorProperty = MaterialColorPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == materialName);
            if (vectorProperty != null)
            {
                if (legacyColorProperty != null)
                    MaterialColorPropertyList.Remove(legacyColorProperty);
                if (vectorProperty.Value == vectorProperty.ValueOriginal)
                {
                    MaterialVectorPropertyList.Remove(vectorProperty);
                    return null;
                }
                return vectorProperty;
            }
            if (legacyColorProperty == null)
                return null;

            var legacyValue = new Vector4(legacyColorProperty.Value.r, legacyColorProperty.Value.g, legacyColorProperty.Value.b, legacyColorProperty.Value.a);
            var legacyValueOriginal = new Vector4(legacyColorProperty.ValueOriginal.r, legacyColorProperty.ValueOriginal.g, legacyColorProperty.ValueOriginal.b, legacyColorProperty.ValueOriginal.a);
            if (legacyValue != legacyValueOriginal)
            {
                vectorProperty = new MaterialVectorProperty(
                    legacyColorProperty.ID,
                    legacyColorProperty.MaterialName,
                    legacyColorProperty.Property,
                    legacyValue,
                    legacyValueOriginal);
                MaterialVectorPropertyList.Add(vectorProperty);
            }

            MaterialColorPropertyList.Remove(legacyColorProperty);
            return vectorProperty;
        }

        private void RemoveLegacyMaterialVectorDuplicates()
        {
            MaterialVectorPropertyList.RemoveAll(vector => vector.Value == vector.ValueOriginal);
            // Legacy Color entries are removed by MigrateLegacyMaterialVectorProperty
            // only after the active shader manifest confirms that the property is a
            // Vector. Keeping Color entries here prevents a stale Vector entry from
            // discarding a newer Color edit when a manifest is downgraded or changed.
        }

    }
}
