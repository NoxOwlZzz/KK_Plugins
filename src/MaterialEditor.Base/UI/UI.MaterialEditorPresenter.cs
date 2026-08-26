using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal delegate void ImportTextureAction(
        TexturePropertyRowModel row,
        GameObject gameObject,
        object data,
        Material material,
        string propertyName);

    internal delegate void ImportCubemapAction(
        CubemapPropertyRowModel row,
        GameObject gameObject,
        object data,
        Material material,
        string propertyName);

    internal delegate void SelectInterpolableAction(
        GameObject gameObject,
        RowModel.RowItemType itemType,
        string materialName,
        string propertyName,
        string rendererName);

    internal delegate void EditColorAction(
        object data,
        Material material,
        string title,
        Color value,
        Action<Color> onChanged);

    internal sealed class MaterialEditorPresentationActions
    {
        internal Action<GameObject, object, string> Refresh { get; set; }
        internal Action<GameObject, object, string> RefreshDeferred { get; set; }
        internal Action<MaterialConditionInvalidationHandle> RequestCondition { get; set; }
        internal Action<GameObject, object, IEnumerable<Renderer>> RefreshMaterialSelection { get; set; }
        internal Action<RendererSectionPresentation, bool> SetRendererCollapsed { get; set; }
        internal Action<GameObject, Material, object> ShowRename { get; set; }
        internal Action<Renderer> ExportUv { get; set; }
        internal Action<Renderer> RequestObjExport { get; set; }
        internal Action<Material, string> ExportTexture { get; set; }
        internal ImportTextureAction ImportTexture { get; set; }
        internal Action<Material, string> ExportCubemap { get; set; }
        internal ImportCubemapAction ImportCubemap { get; set; }
        internal SelectInterpolableAction SelectInterpolable { get; set; }
        internal Action<GameObject, ProjectorProperties, string> SelectProjectorInterpolable { get; set; }
        internal EditColorAction EditColor { get; set; }
        internal Action<object, Material, string, Color> SetColorToPalette { get; set; }
        internal Func<string, string, bool> IsPropertyBlacklisted { get; set; }
    }

    /// <summary>
    /// Coordinates filtering and renderer presentation, then delegates each material
    /// section to <see cref="MaterialSectionPresenter"/>.
    /// </summary>
    internal sealed class MaterialEditorPresenter
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorPresentationActions _actions;
        private readonly MaterialSectionPresenter _materialSections;
        private int _nextPresentationToken;

        internal MaterialEditorPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _session = session;
            _actions = actions;
            _materialSections = new MaterialSectionPresenter(editService, session, actions);
        }

        internal MaterialEditorPresentation BuildRows(
            GameObject gameObject,
            object data,
            string filter,
            IList<Renderer> rendererSource,
            IList<Projector> projectorSource)
        {
            var allRenderers = rendererSource;
            var allProjectors = projectorSource;
            var rendererFilter = new List<string>();
            var propertyFilter = new List<string>();
            IList<Renderer> renderers;
            IList<Projector> projectors;
            Dictionary<string, Material> materials;
            IList<MaterialEditorFilterPattern> preparedPropertyFilter;
            MaterialEditorFilter.Parse(
                filter,
                rendererFilter,
                propertyFilter);
            var rendererPatterns = MaterialEditorFilter.Prepare(
                rendererFilter);
            var propertyPatterns = MaterialEditorFilter.Prepare(
                propertyFilter);
            renderers = SelectRenderers(allRenderers, rendererPatterns);
            projectors = rendererPatterns.Count == 0
                ? null
                : SelectProjectors(allProjectors, rendererPatterns);
            materials = SelectMaterials(
                gameObject,
                allRenderers,
                renderers,
                rendererPatterns);

            preparedPropertyFilter = propertyPatterns;

            var presentation = new MaterialEditorPresentation(
                NextPresentationToken());
            presentation.HasActiveFilter =
                rendererFilter.Count != 0 || propertyFilter.Count != 0;
            presentation.HasPropertyFilter = propertyFilter.Count != 0;

            foreach (var renderer in renderers)
                AddRendererRows(presentation, gameObject, data, renderer);

            foreach (var material in materials.Values)
            {
                _materialSections.AddRows(new MaterialSectionContext(
                    _editService,
                    presentation,
                    gameObject,
                    data,
                    filter,
                    allRenderers,
                    preparedPropertyFilter,
                    material,
                    null));
            }

            foreach (var projector in rendererFilter.Count == 0 ? allProjectors : projectors)
            {
                _materialSections.AddRows(new MaterialSectionContext(
                    _editService,
                    presentation,
                    gameObject,
                    data,
                    filter,
                    allRenderers,
                    preparedPropertyFilter,
                    projector.material,
                    projector));
            }

            return presentation;
        }

        private int NextPresentationToken()
        {
            unchecked
            {
                _nextPresentationToken++;
                if (_nextPresentationToken == 0)
                    _nextPresentationToken++;
            }
            return _nextPresentationToken;
        }

        private IList<Renderer> SelectRenderers(
            IList<Renderer> allRenderers,
            IList<MaterialEditorFilterPattern> filter)
        {
            if (_session.SelectedRenderers.Count > 0)
                return _session.SelectedRenderers;
            if (filter.Count == 0)
                return allRenderers;

            var renderers = new List<Renderer>();
            foreach (var renderer in allRenderers)
                foreach (var filterPattern in filter)
                    if (filterPattern.Matches(renderer.NameFormatted())
                        && !renderers.Contains(renderer))
                        renderers.Add(renderer);

            return renderers;
        }

        private static IList<Projector> SelectProjectors(
            IList<Projector> allProjectors,
            IList<MaterialEditorFilterPattern> filter)
        {
            var projectors = new List<Projector>();
            if (filter.Count == 0)
                return projectors;

            foreach (var projector in allProjectors)
                foreach (var filterPattern in filter)
                    if (filterPattern.Matches(projector.NameFormatted()))
                        projectors.Add(projector);

            return projectors;
        }

        private Dictionary<string, Material> SelectMaterials(
            GameObject gameObject,
            IList<Renderer> allRenderers,
            IList<Renderer> selectedRenderers,
            IList<MaterialEditorFilterPattern> filter)
        {
            var materials = new Dictionary<string, Material>();
            if (filter.Count == 0)
            {
                foreach (var renderer in selectedRenderers)
                    foreach (var material in GetSelectedMaterials(gameObject, renderer))
                        materials[material.NameFormatted()] = material;
                return materials;
            }

            foreach (var renderer in allRenderers)
                foreach (var material in GetSelectedMaterials(gameObject, renderer))
                    foreach (var filterPattern in filter)
                        if (filterPattern.Matches(material.NameFormatted()))
                            materials[material.NameFormatted()] = material;

            return materials;
        }

        private IEnumerable<Material> GetSelectedMaterials(GameObject gameObject, Renderer renderer)
        {
            var materials = GetMaterials(gameObject, renderer);
            return _session.SelectedMaterials.Count == 0
                ? materials
                : materials.Where(material => _session.SelectedMaterials.Contains(material));
        }

        private void AddRendererRows(
            MaterialEditorPresentation presentation,
            GameObject gameObject,
            object data,
            Renderer renderer)
        {
            var rendererName = renderer.NameFormatted();
            var rendererKey = MaterialEditorSectionKeys.Renderer(
                gameObject,
                renderer,
                GetRelativeRendererPath(gameObject, renderer),
                GetRendererComponentIndex(renderer));
            var collapsed = MaterialEditorSessionState.IsCollapsed(
                _session.CollapsedRendererSections,
                rendererKey);
            RendererSectionPresentation section = null;
            var header = new RendererRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Renderer = renderer,
                RendererName = rendererName,
                Collapsed = collapsed,
                CollapsedOnChange = value =>
                    _actions.SetRendererCollapsed(section, value),
                ExportUv = () => _actions.ExportUv(renderer),
                ExportObj = () => _actions.RequestObjExport(renderer),
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.Renderer,
                        string.Empty,
                        string.Empty,
                        rendererName)
            };
            section = new RendererSectionPresentation(
                presentation.OwnerToken,
                rendererKey,
                presentation.Rows.Count,
                collapsed,
                () => BuildRendererChildRows(gameObject, data, renderer),
                value =>
                {
                    header.Collapsed = value;
                    _session.SetRendererCollapsed(rendererKey, value);
                });
            presentation.RendererSections.Add(section);
            presentation.Rows.Add(header);
            if (!collapsed)
                presentation.Rows.AddRange(section.GetRowsForState(false));
        }

        private IList<RowModel> BuildRendererChildRows(
            GameObject gameObject,
            object data,
            Renderer renderer)
        {
            var rows = new List<RowModel>(5);
            var edits = new MaterialEditorEditService(
                _editService,
                gameObject,
                data);

            var originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.Enabled);
            var originalEnabled = originalValue.IsNullOrEmpty()
                ? renderer.enabled
                : originalValue == "1";
            rows.Add(new RendererEnabledRowModel()
            {
                Value = renderer.enabled,
                OriginalValue = originalEnabled,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.Enabled,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.Enabled)
            });

            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.ShadowCastingMode);
            var originalShadowCastingMode = originalValue.IsNullOrEmpty()
                ? renderer.shadowCastingMode
                : (UnityEngine.Rendering.ShadowCastingMode)int.Parse(originalValue);
            rows.Add(new RendererShadowCastingModeRowModel()
            {
                Value = (int)renderer.shadowCastingMode,
                OriginalValue = (int)originalShadowCastingMode,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.ShadowCastingMode,
                        value.ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.ShadowCastingMode)
            });

            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.ReceiveShadows);
            var originalReceiveShadows = originalValue.IsNullOrEmpty()
                ? renderer.receiveShadows
                : originalValue == "1";
            rows.Add(new RendererReceiveShadowsRowModel()
            {
                Value = renderer.receiveShadows,
                OriginalValue = originalReceiveShadows,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.ReceiveShadows,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.ReceiveShadows)
            });

            var meshRenderer = renderer as SkinnedMeshRenderer;
            if (meshRenderer == null)
                return rows;

#if !KK
            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.UpdateWhenOffscreen);
            var originalUpdateWhenOffscreen = originalValue.IsNullOrEmpty()
                ? meshRenderer.updateWhenOffscreen
                : originalValue == "1";
            rows.Add(new RendererUpdateWhenOffscreenRowModel()
            {
                Value = meshRenderer.updateWhenOffscreen,
                OriginalValue = originalUpdateWhenOffscreen,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.UpdateWhenOffscreen,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.UpdateWhenOffscreen)
            });
#endif

            originalValue = edits.GetOriginalRendererProperty(
                renderer,
                RendererProperties.RecalculateNormals);
            var originalRecalculateNormals = !originalValue.IsNullOrEmpty() && originalValue == "1";
            var currentValue = edits.GetRendererProperty(
                renderer,
                RendererProperties.RecalculateNormals);
            var recalculateNormals = !currentValue.IsNullOrEmpty() && currentValue == "1";
            rows.Add(new RendererRecalculateNormalsRowModel()
            {
                Value = recalculateNormals,
                OriginalValue = originalRecalculateNormals,
                ValueOnChange = value =>
                    edits.SetRendererProperty(
                        renderer,
                        RendererProperties.RecalculateNormals,
                        (value ? 1 : 0).ToString()),
                ValueOnReset = () =>
                    edits.ResetRendererProperty(
                        renderer,
                        RendererProperties.RecalculateNormals)
            });
            return rows;
        }

        private static string GetRelativeRendererPath(
            GameObject gameObject,
            Renderer renderer)
        {
            if (renderer == null || renderer.transform == null)
                return string.Empty;

            var root = gameObject == null ? null : gameObject.transform;
            var current = renderer.transform;
            var segments = new Stack<string>();
            while (current != null && current != root)
            {
                segments.Push(
                    (current.name ?? string.Empty)
                    + "["
                    + current.GetSiblingIndex()
                    + "]");
                current = current.parent;
            }

            var path = segments.Count == 0
                ? "."
                : string.Join("/", segments.ToArray());
            return current == root ? path : "external/" + path;
        }

        private static int GetRendererComponentIndex(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null)
                return -1;
            var renderers = renderer.gameObject.GetComponents<Renderer>();
            for (var index = 0; index < renderers.Length; index++)
                if (ReferenceEquals(renderers[index], renderer))
                    return index;
            return -1;
        }
    }
}
