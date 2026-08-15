using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    // Routes a pooled row model to a plain C# family binder and owns its listener lifetime.
    internal sealed class RowBinder : MonoBehaviour
    {
        private RowModel _currentModel;
        private RowControlSet _controls;
        private RowHandlerRegistry _registry;
        private MaterialEditorRowActionMenu _rowActionMenu;
        private MaterialEditorClipboardViewState _clipboardViewState;
        private readonly ListenerScope _listeners = new ListenerScope();
        private bool _bindingActive;
        private int _bindingGeneration;

        internal RowModel CurrentModel
        {
            get => _currentModel;
            set => Bind(value, false);
        }

        internal void InitializeControls()
        {
            if (_controls != null)
                return;

            _controls = RowControlSet.Create(this);
            _registry = new RowHandlerRegistry();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _rowActionMenu = canvas.GetComponent<MaterialEditorRowActionMenu>();
                _clipboardViewState =
                    canvas.GetComponent<MaterialEditorClipboardViewState>()
                    ?? canvas.gameObject.AddComponent<MaterialEditorClipboardViewState>();
            }

            var renderer = new RendererRowTypeBinder(_controls);
            _registry.Register(
                renderer,
                RowModel.RowItemType.Renderer,
                RowModel.RowItemType.RendererEnabled,
                RowModel.RowItemType.RendererShadowCastingMode,
                RowModel.RowItemType.RendererReceiveShadows,
                RowModel.RowItemType.RendererUpdateWhenOffscreen,
                RowModel.RowItemType.RendererRecalculateNormals);

            var materialShader = new MaterialShaderRowTypeBinder(_controls);
            _registry.Register(
                materialShader,
                RowModel.RowItemType.Material,
                RowModel.RowItemType.Shader,
                RowModel.RowItemType.ShaderRenderQueue);

            var texture = new TextureRowTypeBinder(_controls);
            _registry.Register(
                texture,
                RowModel.RowItemType.PropertyCategory,
                RowModel.RowItemType.PropertySubcategory,
                RowModel.RowItemType.TextureProperty,
                RowModel.RowItemType.TextureOffsetScale);

            _registry.Register(
                new CubemapRowTypeBinder(_controls),
                RowModel.RowItemType.CubemapProperty);

            _registry.Register(
                new ColorRowTypeBinder(_controls),
                RowModel.RowItemType.ColorProperty);

            var floatKeyword = new FloatKeywordRowTypeBinder(_controls);
            _registry.Register(
                floatKeyword,
                RowModel.RowItemType.FloatProperty,
                RowModel.RowItemType.KeywordProperty);

            _registry.Register(
                new EnumVectorToggleRowTypeBinder(_controls),
                RowModel.RowItemType.EnumProperty,
                RowModel.RowItemType.VectorProperty,
                RowModel.RowItemType.FloatToggleProperty);
        }

        internal void Bind(RowModel item, bool force)
        {
            if (!force
                && ReferenceEquals(item, _currentModel)
                && _bindingActive)
                return;
            InvalidateActionMenu();
            var bindSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.Bind);
            try
            {
                InitializeControls();
                var previousModel = _currentModel;
                if (previousModel != null
                    && item != null
                    && !ReferenceEquals(previousModel, item))
                {
                    MaterialEditorPerformance.Increment(
                        MaterialEditorPerformanceMetric.RowViewReuse);
                }
                _currentModel = item;

                ClearListeners();
                _bindingActive = true;
                if (item == null
                    || item.ItemType != RowModel.RowItemType.EnumProperty)
                {
                    _controls.Enum.OptionCache.ReleaseContext();
                }
                if (item == null
                    || item.ItemType != RowModel.RowItemType.Shader)
                {
                    _controls.Shader.OptionCache.ReleaseContext();
                }
                _controls.HideAll();

                if (item == null)
                    return;

                IRowTypeBinder handler;
                if (_registry.TryGet(item.ItemType, out handler))
                    handler.Bind(item, _listeners);
                _controls.SetHierarchyDepth(
                    item.ItemType,
                    item.HierarchyDepth);
                _controls.SetEnabled(item.ItemType, item.Enabled);
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.Bind,
                    bindSample);
            }
        }

        internal void SuspendListeners()
        {
            InvalidateActionMenu();
            ClearListeners();
            _bindingActive = false;
        }

        internal void Release()
        {
            InvalidateActionMenu();
            ClearListeners();
            _bindingActive = false;
            _currentModel = null;
            if (_controls != null)
            {
                _controls.Enum.OptionCache.ReleaseContext();
                _controls.Shader.OptionCache.ReleaseContext();
                _controls.HideAll();
            }
        }

        internal void OpenRendererActionMenu(
            RectTransform anchor,
            Action exportUv,
            Action exportObj)
        {
            if (_rowActionMenu == null
                || !IsActionMenuBindingValid(_bindingGeneration))
                return;
            _rowActionMenu.OpenRenderer(
                this,
                _bindingGeneration,
                anchor,
                exportUv,
                exportObj);
        }

        internal void OpenMaterialActionMenu(
            RectTransform anchor,
            Action copyOrRemove,
            string copyOrRemoveLabel,
            Action rename)
        {
            if (_rowActionMenu == null
                || !IsActionMenuBindingValid(_bindingGeneration))
                return;
            _rowActionMenu.OpenMaterial(
                this,
                _bindingGeneration,
                anchor,
                copyOrRemove,
                copyOrRemoveLabel,
                rename);
        }

        internal void ListenForClipboardChanges(
            ListenerScope listeners,
            Action listener)
        {
            if (_clipboardViewState == null || listeners == null || listener == null)
                return;

            _clipboardViewState.Changed += listener;
            listeners.OnDispose(() => _clipboardViewState.Changed -= listener);
        }

        internal bool IsActionMenuBindingValid(int generation)
        {
            return generation == _bindingGeneration
                   && _bindingActive
                   && _currentModel != null
                   && _currentModel.Enabled
                   && gameObject.activeInHierarchy;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public T GetUIComponent<T>(string gameObjectName) where T : Component
        {
            var uiTransform = transform.FindLoop(gameObjectName);
            if (uiTransform == null)
                throw new ArgumentException($"Couldn't find {gameObjectName}");

            var component = uiTransform.GetComponent<T>();
            if (component == null)
                throw new ArgumentException($"Couldn't find {gameObjectName}");
            return component;
        }

        private void OnDestroy()
        {
            Release();
        }

        private void ClearListeners()
        {
            if (_rowActionMenu != null)
                _rowActionMenu.CloseIfOwner(this);
            if (!_bindingActive)
                return;

            var unbindSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.Unbind);
            try
            {
                _listeners.Clear();
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.Unbind,
                    unbindSample);
            }
        }

        private void InvalidateActionMenu()
        {
            if (_rowActionMenu != null)
                _rowActionMenu.CloseIfOwner(this);
            unchecked
            {
                _bindingGeneration++;
            }
        }
    }

    // View-local interaction-state invalidation. It never retains a row after
    // ListenerScope releases that row's binding.
    internal sealed class MaterialEditorClipboardViewState : MonoBehaviour
    {
        private readonly MaterialEditorClipboardSnapshot _snapshot =
            new MaterialEditorClipboardSnapshot();
        internal event Action Changed;

        private void OnEnable()
        {
            MaterialEditorClipboardState.Changed += HandleGlobalChange;
            _snapshot.Capture(MaterialEditorPluginBase.CopyData);
            RaiseChanged();
        }

        private void OnDisable()
        {
            MaterialEditorClipboardState.Changed -= HandleGlobalChange;
        }

        // An allocation-free semantic scan is active only while the window
        // Canvas is active. It covers same-count item replacement and field
        // mutation in the legacy public CopyData lists used by extensions.
        private void Update()
        {
            if (_snapshot.Capture(MaterialEditorPluginBase.CopyData))
                RaiseChanged();
        }

        private void HandleGlobalChange()
        {
            _snapshot.Capture(MaterialEditorPluginBase.CopyData);
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            var changed = Changed;
            if (changed != null)
                changed();
        }

        private void OnDestroy()
        {
            MaterialEditorClipboardState.Changed -= HandleGlobalChange;
            Changed = null;
        }
    }
}
