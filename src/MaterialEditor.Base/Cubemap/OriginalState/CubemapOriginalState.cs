using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns the non-persisted original-value state for one Cubemap edit.
    /// Character and Studio persistence records keep their separate serialized
    /// schemas and compose this runtime-only state.
    /// </summary>
    internal sealed class MaterialCubemapOriginalState
    {
        internal sealed class Checkpoint
        {
            internal Dictionary<Material, Cubemap> Materials;
            internal List<MaterialCubemapOriginalBinding> Bindings;
            internal bool BindingsNeedRemap;
            internal bool WarningLogged;
        }

        private Dictionary<Material, Cubemap> _materials;
        private List<MaterialCubemapOriginalBinding> _bindings;
        private bool _bindingsNeedRemap;
        private bool _warningLogged;

        internal Checkpoint CaptureCheckpoint()
        {
            return new Checkpoint
            {
                Materials = _materials,
                Bindings = _bindings,
                BindingsNeedRemap = _bindingsNeedRemap,
                WarningLogged = _warningLogged
            };
        }

        internal void RestoreCheckpoint(
            Checkpoint checkpoint,
            bool preserveCurrentWarning)
        {
            var currentWarning = _warningLogged;
            if (checkpoint == null)
            {
                Clear();
                _warningLogged = preserveCurrentWarning && currentWarning;
                return;
            }

            _materials = checkpoint.Materials;
            _bindings = checkpoint.Bindings;
            _bindingsNeedRemap = checkpoint.BindingsNeedRemap;
            _warningLogged = checkpoint.WarningLogged
                             || preserveCurrentWarning && currentWarning;
        }

        internal void InheritFrom(
            MaterialCubemapOriginalState source,
            GameObject sourceGameObject,
            string materialName,
            string propertyName)
        {
            Clear();
            if (source == null)
                return;

            if (sourceGameObject != null)
                source.Synchronize(sourceGameObject, materialName, propertyName);
            _bindings = MaterialCubemapOriginalSnapshot.CloneStableValues(
                source._bindings);
            _bindingsNeedRemap = _bindings != null && _bindings.Count > 0;
        }

        internal void InheritSameMaterialsFrom(
            MaterialCubemapOriginalState source,
            GameObject sourceGameObject,
            string materialName,
            string propertyName)
        {
            Clear();
            if (source == null)
                return;

            if (sourceGameObject != null)
                source.Synchronize(sourceGameObject, materialName, propertyName);
            _materials = MaterialCubemapOriginalSnapshot.CloneByMaterialReference(
                source._materials);
            _bindings = MaterialCubemapOriginalSnapshot.CloneStableValues(
                source._bindings);
        }

        internal bool Synchronize(
            GameObject gameObject,
            string materialName,
            string propertyName)
        {
            if (gameObject == null)
                return false;

            if (_bindingsNeedRemap)
            {
                Dictionary<Material, Cubemap> remapped;
                string remapFailure;
                if (MaterialCubemapOriginalSnapshot.TryRemapToCurrentMaterials(
                    gameObject,
                    materialName,
                    propertyName,
                    _bindings,
                    out remapped,
                    out remapFailure))
                {
                    _materials = remapped;
                    _bindingsNeedRemap = false;
                    _warningLogged = false;
                }
                else
                {
                    LogWarningOnce(
                        "Could not map the inherited Cubemap original snapshot for "
                        + materialName + "/" + propertyName
                        + "; the Cubemap override was skipped so Reset remains safe. "
                        + remapFailure);
                    return false;
                }
            }

            var synchronizedMaterials =
                MaterialCubemapOriginalSnapshot.SynchronizeByMaterialReference(
                    gameObject,
                    materialName,
                    propertyName,
                    _materials);
            var synchronizedBindings = MaterialCubemapOriginalSnapshot.GetStableValues(
                gameObject,
                materialName,
                propertyName,
                synchronizedMaterials);
            if (synchronizedBindings == null)
            {
                LogWarningOnce(
                    "Could not create an unambiguous Cubemap original snapshot for "
                    + materialName + "/" + propertyName
                    + "; the Cubemap override was skipped so Reset remains safe.");
                return false;
            }

            _materials = synchronizedMaterials;
            _bindings = synchronizedBindings;
            _warningLogged = false;
            return true;
        }

        internal bool RestoreOriginal(
            GameObject gameObject,
            string materialName,
            string propertyName)
        {
            if (!Synchronize(gameObject, materialName, propertyName))
                return false;
            MaterialCubemapOriginalSnapshot.RestoreByMaterialReference(
                gameObject,
                materialName,
                propertyName,
                _materials);
            return true;
        }

        internal void Clear()
        {
            _materials = null;
            _bindings = null;
            _bindingsNeedRemap = false;
            _warningLogged = false;
        }

        private void LogWarningOnce(string message)
        {
            if (_warningLogged)
                return;
            _warningLogged = true;
            MaterialEditorPluginBase.Logger.LogWarning(message);
        }
    }
}
