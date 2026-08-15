using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorTargetOwnershipPolicy
    {
        internal static bool IsOwnedByRoot(
            bool exactRoot,
            bool descendant,
            bool hasScopedTargetData) =>
            exactRoot || (descendant && hasScopedTargetData);
    }

    internal sealed class MaterialEditorSessionState
    {
        internal const int CollapsedRendererStateLimit = 512;

        internal GameObject CurrentGameObject;
        internal object CurrentData;
        internal string Filter = "";
        internal readonly List<Renderer> SelectedRenderers = new List<Renderer>();
        internal readonly List<Material> SelectedMaterials = new List<Material>();
        internal readonly List<Renderer> SelectedMaterialRenderers = new List<Renderer>();
        internal readonly Dictionary<string, bool> CollapsedRendererSections = new Dictionary<string, bool>();
        internal readonly Dictionary<string, bool> CollapsedPropertyCategories = new Dictionary<string, bool>();
        internal readonly Dictionary<string, bool> CollapsedPropertySubcategories = new Dictionary<string, bool>();
        internal readonly Dictionary<string, bool> CollapsedMaterialSections = new Dictionary<string, bool>();
        internal readonly Dictionary<string, bool> CollapsedShaderSections = new Dictionary<string, bool>();
        private readonly LinkedList<string> _collapsedRendererOrder =
            new LinkedList<string>();
        private readonly Dictionary<string, LinkedListNode<string>>
            _collapsedRendererNodes =
                new Dictionary<string, LinkedListNode<string>>();

        internal bool ListsVisible;
        internal bool RenameListVisible;

        private bool _objExportPending;
        private Renderer _objRenderer;

        internal void RequestObjExport(Renderer renderer)
        {
            _objRenderer = renderer;
            _objExportPending = renderer != null;
        }

        internal bool TryTakeObjExport(out Renderer renderer)
        {
            renderer = _objRenderer;
            if (!_objExportPending)
                return false;

            _objExportPending = false;
            _objRenderer = null;
            return renderer != null;
        }

        internal void CancelObjExport()
        {
            _objExportPending = false;
            _objRenderer = null;
        }

        internal void ClearSelections()
        {
            SelectedRenderers.Clear();
            SelectedMaterials.Clear();
            SelectedMaterialRenderers.Clear();
        }

        internal void ClearTargetReferences()
        {
            CurrentGameObject = null;
            CurrentData = null;
            ClearSelections();
            CollapsedRendererSections.Clear();
            CollapsedPropertySubcategories.Clear();
            _collapsedRendererOrder.Clear();
            _collapsedRendererNodes.Clear();
            RenameListVisible = false;
            CancelObjExport();
        }

        internal static bool IsCollapsed(
            IDictionary<string, bool> states,
            string key)
        {
            bool collapsed;
            return states.TryGetValue(key, out collapsed) && collapsed;
        }

        internal static void SetCollapsed(
            IDictionary<string, bool> states,
            string key,
            bool collapsed)
        {
            if (collapsed)
                states[key] = true;
            else
                states.Remove(key);
        }

        internal void SetRendererCollapsed(string key, bool collapsed)
        {
            if (string.IsNullOrEmpty(key))
                return;

            LinkedListNode<string> node;
            if (!collapsed)
            {
                CollapsedRendererSections.Remove(key);
                if (_collapsedRendererNodes.TryGetValue(key, out node))
                {
                    _collapsedRendererNodes.Remove(key);
                    _collapsedRendererOrder.Remove(node);
                }
                return;
            }

            if (CollapsedRendererSections.ContainsKey(key))
                return;
            while (CollapsedRendererSections.Count
                   >= CollapsedRendererStateLimit)
            {
                node = _collapsedRendererOrder.First;
                if (node == null)
                    break;
                _collapsedRendererOrder.RemoveFirst();
                _collapsedRendererNodes.Remove(node.Value);
                CollapsedRendererSections.Remove(node.Value);
            }

            CollapsedRendererSections[key] = true;
            node = _collapsedRendererOrder.AddLast(key);
            _collapsedRendererNodes[key] = node;
        }

        internal static void PruneUnavailableUnityObjects<T>(
            IList<T> selected,
            IList<T> available)
            where T : UnityEngine.Object
        {
            for (var selectedIndex = selected.Count - 1;
                 selectedIndex >= 0;
                 selectedIndex--)
            {
                UnityEngine.Object selectedObject = selected[selectedIndex];
                var present = false;
                for (var availableIndex = 0;
                     selectedObject != null && availableIndex < available.Count;
                     availableIndex++)
                {
                    UnityEngine.Object availableObject = available[availableIndex];
                    if (availableObject != selectedObject)
                        continue;
                    present = true;
                    break;
                }
                if (!present)
                    selected.RemoveAt(selectedIndex);
            }
        }
    }
}
