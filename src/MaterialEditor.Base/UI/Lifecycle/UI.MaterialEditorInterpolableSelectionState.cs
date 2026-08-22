using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns the current Material Editor selections exposed to Timeline.
    /// </summary>
    internal sealed class MaterialEditorInterpolableSelectionState
    {
        internal SelectedInterpolable SelectedMaterial { get; private set; }

        internal SelectedProjectorInterpolable SelectedProjector
        {
            get;
            private set;
        }

        internal void SelectMaterial(
            GameObject gameObject,
            RowModel.RowItemType rowType,
            string materialName,
            string propertyName,
            string rendererName)
        {
            SelectedMaterial = new SelectedInterpolable(
                gameObject,
                rowType,
                materialName,
                propertyName,
                rendererName);
            MaterialEditorPluginBase.Logger.LogMessage(
                $"Activated interpolable(s), {SelectedMaterial}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        internal void SelectProjector(
            GameObject gameObject,
            ProjectorProperties property,
            string projectorName)
        {
            SelectedProjector = new SelectedProjectorInterpolable(
                gameObject,
                property,
                projectorName);
            MaterialEditorPluginBase.Logger.LogMessage(
                $"Activated interpolable(s), {SelectedProjector}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        internal void ClearAll()
        {
            SelectedMaterial = null;
            SelectedProjector = null;
        }

        internal void ClearForTarget(GameObject root)
        {
            if (ReferenceEquals(root, null))
                return;
            if (SelectedMaterial != null
                && MaterialEditorTargetLifecycle.IsGameObjectWithin(
                    SelectedMaterial.GameObject,
                    root))
                SelectedMaterial = null;
            if (SelectedProjector != null
                && MaterialEditorTargetLifecycle.IsGameObjectWithin(
                    SelectedProjector.GameObject,
                    root))
                SelectedProjector = null;
        }

        internal void PruneDestroyed()
        {
            if (SelectedMaterial != null
                && SelectedMaterial.GameObject == null)
                SelectedMaterial = null;
            if (SelectedProjector != null
                && SelectedProjector.GameObject == null)
                SelectedProjector = null;
        }
    }

    internal sealed class SelectedInterpolable
    {
        public string MaterialName;
        public string PropertyName;
        public string RendererName;
        public GameObject GameObject;
        public RowModel.RowItemType RowType;

        internal SelectedInterpolable(
            GameObject gameObject,
            RowModel.RowItemType rowType,
            string materialName,
            string propertyName,
            string rendererName)
        {
            GameObject = gameObject;
            RowType = rowType;
            MaterialName = materialName;
            PropertyName = propertyName;
            RendererName = rendererName;
        }

        public override string ToString()
        {
            var details = string.Join(" - ", new[] { PropertyName, MaterialName, RendererName }.Where(x => !x.IsNullOrEmpty()).ToArray());
            return $"{RowType}: {details}";
        }
    }

    internal sealed class SelectedProjectorInterpolable
    {
        public string ProjectorName;
        public ProjectorProperties Property;
        public GameObject GameObject;

        internal SelectedProjectorInterpolable(
            GameObject gameObject,
            ProjectorProperties property,
            string projectorName)
        {
            GameObject = gameObject;
            Property = property;
            ProjectorName = projectorName;
        }

        public override string ToString()
        {
            var details = string.Join(" - ", new[] { Property.ToString(), ProjectorName }.Where(x => !x.IsNullOrEmpty()).ToArray());
            return $"Projector: {details}";
        }
    }
}
