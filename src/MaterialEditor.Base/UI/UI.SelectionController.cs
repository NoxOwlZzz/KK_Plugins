using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorSelectionController
    {
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorWindowView _view;
        private readonly MaterialEditService _editService;
        private readonly Action<GameObject, object, string> _refresh;
        private bool _selectionEntriesReleased;

        internal MaterialEditorSelectionController(
            MaterialEditorSessionState session,
            MaterialEditorWindowView view,
            MaterialEditService editService,
            Action<GameObject, object, string> refresh)
        {
            _session = session;
            _view = view;
            _editService = editService;
            _refresh = refresh;
        }

        internal void InitializeViewState()
        {
            _session.CategoriesVisible =
                MaterialEditorPluginBase.CategoriesPanelOpen != null
                && MaterialEditorPluginBase.CategoriesPanelOpen.Value;
            _session.SelectionPanelsVisible =
                (MaterialEditorPluginBase.RenderersPanelOpen != null
                 && MaterialEditorPluginBase.RenderersPanelOpen.Value)
                || (MaterialEditorPluginBase.MaterialsPanelOpen != null
                    && MaterialEditorPluginBase.MaterialsPanelOpen.Value);
            PersistSelectionPanelsVisible(
                _session.SelectionPanelsVisible);
            ApplyPanelState();
        }

        internal void ToggleCategoriesPanel()
        {
            var visible = !_session.CategoriesVisible;
            _session.CategoriesVisible = visible;
            if (MaterialEditorPluginBase.CategoriesPanelOpen != null
                && MaterialEditorPluginBase.CategoriesPanelOpen.Value != visible)
                MaterialEditorPluginBase.CategoriesPanelOpen.Value = visible;
            ApplyPanelState();
        }

        internal void ToggleSelectionPanels()
        {
            if (_session.RenameListVisible)
            {
                _session.RenameListVisible = false;
                if (_session.SelectionPanelsVisible)
                {
                    _session.SelectionPanelsVisible = false;
                    PersistSelectionPanelsVisible(false);
                }
                ApplyPanelState();
                ReleaseRenameContext();
                return;
            }

            var visible = !_session.SelectionPanelsVisible;
            _session.SelectionPanelsVisible = visible;
            PersistSelectionPanelsVisible(visible);
            ApplyPanelState();
        }

        internal void CloseRenamePanel()
        {
            if (_session.RenameListVisible)
            {
                _session.RenameListVisible = false;
                ApplyPanelState();
            }
            ReleaseRenameContext();
        }

        internal void ShowRenamePanel(GameObject gameObject, Material material, object data)
        {
            _session.RenameListVisible = true;
            ApplyPanelState();
            PopulateRenameList(gameObject, material, data);
        }

        internal void PopulateRendererList(GameObject gameObject, object data, IEnumerable<Renderer> renderers)
        {
            var rendererList = renderers as IList<Renderer> ?? renderers.ToList();
            var sameTarget = gameObject == _session.CurrentGameObject;
            var restoreReleasedEntries = sameTarget && _selectionEntriesReleased;
            if (sameTarget && !restoreReleasedEntries)
                return;

            if (restoreReleasedEntries)
                MaterialEditorSessionState.PruneUnavailableUnityObjects(
                    _session.SelectedRenderers,
                    rendererList);

            if (!restoreReleasedEntries)
            {
                _session.SelectedRenderers.Clear();
                _view.RendererList.ClearList();
            }

            foreach (var renderer in rendererList)
            {
                var capturedRenderer = renderer;
                _view.RendererList.AddEntry(
                    capturedRenderer.NameFormatted(),
                    restoreReleasedEntries
                    && _session.SelectedRenderers.Contains(capturedRenderer),
                    selected =>
                    {
                        UpdateSelection(_session.SelectedRenderers, capturedRenderer, selected);
                        MaterialEditorExtensionRegistry.RaiseSelection(
                            _editService,
                            MaterialEditorSelectionType.Renderer,
                            selected
                                ? MaterialEditorSelectionAction.Selected
                                : MaterialEditorSelectionAction.Deselected,
                            capturedRenderer.NameFormatted(),
                            gameObject,
                            data,
                            capturedRenderer,
                            null,
                            null);
                        _refresh(gameObject, data, _session.Filter);
                        PopulateMaterialList(gameObject, data, rendererList);
                    });
            }

            PopulateMaterialList(
                gameObject,
                data,
                rendererList,
                restoreReleasedEntries);
            _selectionEntriesReleased = false;
        }

        internal void PopulateMaterialList(GameObject gameObject, object data, IEnumerable<Renderer> renderers)
        {
            PopulateMaterialList(gameObject, data, renderers, false);
        }

        private void PopulateMaterialList(
            GameObject gameObject,
            object data,
            IEnumerable<Renderer> renderers,
            bool restoreReleasedEntries)
        {
            if (!restoreReleasedEntries)
            {
                _session.SelectedMaterials.Clear();
                _view.MaterialList.ClearList();
            }

            List<Material> availableMaterials = null;
            if (restoreReleasedEntries && _session.SelectedMaterials.Count > 0)
                availableMaterials = new List<Material>();

            foreach (var renderer in renderers.Where(renderer =>
                         _session.SelectedRenderers.Count == 0
                         || _session.SelectedRenderers.Contains(renderer)))
            {
                foreach (var material in GetMaterials(gameObject, renderer))
                {
                    if (availableMaterials != null)
                        availableMaterials.Add(material);
                    var capturedMaterial = material;
                    _view.MaterialList.AddEntry(
                        capturedMaterial.NameFormatted(),
                        restoreReleasedEntries
                        && _session.SelectedMaterials.Contains(capturedMaterial),
                        selected =>
                        {
                            UpdateSelection(_session.SelectedMaterials, capturedMaterial, selected);
                            MaterialEditorExtensionRegistry.RaiseSelection(
                                _editService,
                                MaterialEditorSelectionType.Material,
                                selected
                                    ? MaterialEditorSelectionAction.Selected
                                    : MaterialEditorSelectionAction.Deselected,
                                capturedMaterial.NameFormatted(),
                                gameObject,
                                data,
                                renderer,
                                capturedMaterial,
                                null);
                            _refresh(gameObject, data, _session.Filter);
                        });
                }
            }

            if (availableMaterials != null)
                MaterialEditorSessionState.PruneUnavailableUnityObjects(
                    _session.SelectedMaterials,
                    availableMaterials);
        }

        internal void ReleaseTransientContent()
        {
            _view.RendererList.ReleaseEntries();
            _view.MaterialList.ReleaseEntries();
            _selectionEntriesReleased = true;

            // A visible rename panel is intentionally retained: rebuilding it would
            // require retaining or guessing its material context. Hidden rename
            // content is safe to discard because ShowRenamePanel always rebuilds it.
            if (_session.RenameListVisible)
                return;
            ReleaseRenameContext();
        }

        internal void ReleaseTargetContent()
        {
            _view.RendererList.ReleaseEntries();
            _view.MaterialList.ReleaseEntries();
            _selectionEntriesReleased = true;

            if (_session.RenameListVisible)
            {
                _session.RenameListVisible = false;
                ApplyPanelState();
            }
            ReleaseRenameContext();
            _session.ClearSelections();
        }

        private void ReleaseRenameContext()
        {
            _view.RenameButton.onClick.RemoveAllListeners();
            MaterialEditorStyles.SetControlAvailability(
                _view.RenameButton,
                false);
            _view.RenameList.ReleaseEntries();
            _session.SelectedMaterialRenderers.Clear();
        }

        private void ApplyPanelState()
        {
            _view.SetPanelState(
                _session.CategoriesVisible,
                _session.SelectionPanelsVisible,
                _session.RenameListVisible);
        }

        private static void PersistSelectionPanelsVisible(bool visible)
        {
            // Preserve both existing keys as compatibility aliases while the
            // UI exposes one joint Renderers/Materials visibility state.
            if (MaterialEditorPluginBase.RenderersPanelOpen != null
                && MaterialEditorPluginBase.RenderersPanelOpen.Value != visible)
            {
                MaterialEditorPluginBase.RenderersPanelOpen.Value = visible;
            }
            if (MaterialEditorPluginBase.MaterialsPanelOpen != null
                && MaterialEditorPluginBase.MaterialsPanelOpen.Value != visible)
            {
                MaterialEditorPluginBase.MaterialsPanelOpen.Value = visible;
            }
        }

        private void PopulateRenameList(GameObject gameObject, Material material, object data)
        {
            _session.SelectedMaterialRenderers.Clear();
            _view.RenameList.ClearList();
            _view.RenameMaterial.text = material.NameFormatted();

            var formattedName = material.NameFormatted()
                .Split(new[] { MaterialCopyPostfix }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(formattedName))
            {
                MaterialEditorPluginBase.Logger.LogWarning("Material name is empty or failed to be extracted from: " + material.name);
                formattedName = "";
            }
            _view.RenameField.text = formattedName;

            var suffix = material.NameFormatted().Replace(formattedName, "");
            MaterialEditorStyles.SetControlAvailability(
                _view.RenameButton,
                false);
            _view.RenameButton.onClick.RemoveAllListeners();
            _view.RenameButton.onClick.AddListener(() =>
            {
                var safeNewName = _view.RenameField.text.Replace(MaterialCopyPostfix, "").Trim() + suffix;
                foreach (var renderer in _session.SelectedMaterialRenderers)
                    _editService.SetMaterialName(data, renderer, material, safeNewName, gameObject);
                _refresh(gameObject, data, _session.Filter);
            });

            foreach (var renderer in GetRendererList(gameObject))
            {
                if (!renderer.materials.Any(candidate => candidate.NameFormatted() == material.NameFormatted()))
                    continue;

                var capturedRenderer = renderer;
                _view.RenameList.AddEntry(capturedRenderer.NameFormatted(), selected =>
                {
                    UpdateSelection(_session.SelectedMaterialRenderers, capturedRenderer, selected);
                    MaterialEditorStyles.SetControlAvailability(
                        _view.RenameButton,
                        _session.SelectedMaterialRenderers.Count > 0);
                });
            }
        }

        private static void UpdateSelection<T>(ICollection<T> selection, T item, bool selected)
        {
            if (selected)
            {
                if (!selection.Contains(item))
                    selection.Add(item);
            }
            else
            {
                selection.Remove(item);
            }
        }
    }
}
