using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Code for the MaterialEditor UI
    /// </summary>
#pragma warning disable BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    public abstract partial class MaterialEditorUI : BaseUnityPlugin
#pragma warning restore BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    {
        /// <summary>
        /// Element containing the entire UI
        /// </summary>
        public static Canvas MaterialEditorWindow;
        /// <summary>
        /// Main panel
        /// </summary>
        public static Image MaterialEditorMainPanel;
        /// <summary>
        /// Draggable header
        /// </summary>
        public static Image DragPanel;
        private static readonly MaterialEditorSessionState Session = new MaterialEditorSessionState();
        private static MaterialEditorWindowView ActiveView;
        private static MaterialEditorUI ActiveUi;

        private MaterialEditorWindowView _windowView;
        private MaterialEditorSelectionController _selectionController;
        private MaterialEditorPresenter _presenter;
        private MaterialEditorPresentation _presentation;
        private readonly DeferredRefreshCoordinator _deferredRefresh =
            new DeferredRefreshCoordinator();
        private Coroutine _deferredRefreshCoroutine;
        private static readonly WaitForEndOfFrame PresentationEndOfFrame =
            new WaitForEndOfFrame();
        private readonly PresentationInvalidationCoordinator<
            MaterialConditionInvalidationHandle>
            _presentationInvalidation =
                new PresentationInvalidationCoordinator<
                    MaterialConditionInvalidationHandle>();
        private Coroutine _presentationInvalidationCoroutine;
        private long _presentationInvalidationCoroutineLeaseId;
        private bool _transientContentReleased;

        private static readonly List<Action<MaterialEditorLabelClickEventArgs>> LabelClickHandlers = new List<Action<MaterialEditorLabelClickEventArgs>>();

        internal static FileSystemWatcher TexChangeWatcher;
        private VirtualList VirtualList;

        internal const float MarginSize = MaterialEditorLayout.Margin;
        internal const float HeaderSize = MaterialEditorLayout.HeaderHeight;
        internal const float ScrollOffsetX = MaterialEditorLayout.ScrollbarOffset;
        internal const float PanelHeight = MaterialEditorLayout.RowHeight;

        #region Entry Item Width
        // General
        internal const float LabelWidth = MaterialEditorLayout.LabelWidth;
        internal const float ButtonWidth = MaterialEditorLayout.ButtonWidth;
        internal const float SmallButtonWidth = MaterialEditorLayout.SmallButtonWidth;
        internal const float ResetButtonWidth = MaterialEditorLayout.ResetButtonWidth;
        internal const float InterpolableButtonWidth = MaterialEditorLayout.InterpolableButtonWidth;
        internal const float ContentFullWidth = MaterialEditorLayout.ContentWidth;
        // Renderer (Enbale/ShadowCastingMode/ReceiveShadows/RendererUpdateWhenOffscreen/RecalulateNormals)
        internal const float RendererButtonWidth = MaterialEditorLayout.RendererButtonWidth;
        internal const float RendererToggleWidth = MaterialEditorLayout.RendererToggleWidth;
        internal const float RendererDropdownWidth = MaterialEditorLayout.RendererDropdownWidth;
        // Material
        internal const float MaterialButtonWidth = MaterialEditorLayout.MaterialButtonWidth;
        internal const float MaterialRenameButtonWidth = MaterialEditorLayout.MaterialRenameButtonWidth;
        // Shader
        internal const float ShaderLabelMinimumWidth = MaterialEditorLayout.ShaderLabelMinimumWidth;
        internal const float ShaderDropdownMinimumWidth = MaterialEditorLayout.ShaderDropdownMinimumWidth;
        internal const float ShaderDropdownWidth = MaterialEditorLayout.ShaderDropdownWidth;
        // RenderQueue
        internal const float RenderQueueInputFieldWidth = MaterialEditorLayout.RenderQueueInputWidth;
        // Texture
        internal const float TextureButtonWidth = ContentFullWidth / 2f;
        // Texture Offset and Scale
        internal const float OffsetScaleLabelXWidth = MaterialEditorLayout.OffsetScaleLabelXWidth;
        internal const float OffsetScaleLabelYWidth = MaterialEditorLayout.OffsetScaleLabelYWidth;
        internal const float OffsetScaleInputFieldWidth = MaterialEditorLayout.OffsetScaleInputWidth;
        // Color
        internal const float ColorLabelWidth = MaterialEditorLayout.ColorLabelWidth;
        internal const float ColorInputFieldWidth = MaterialEditorLayout.ColorInputWidth;
        internal const float ColorEditButtonWidth = MaterialEditorLayout.ColorEditButtonWidth;
        // Float
        internal const float FloatSliderWidth = MaterialEditorLayout.FloatSliderWidth;
        internal const float FloatInputFieldWidth = MaterialEditorLayout.FloatInputWidth;
        // Keyword
        internal const float KeywordToggleWidth = MaterialEditorLayout.KeywordToggleWidth;
        #endregion

        internal static RectOffset Padding => MaterialEditorLayout.RowPadding;

        #region Colors
        internal static readonly Color RowColor = MaterialEditorStyles.RowColor;
        internal static readonly Color RendererColor = MaterialEditorStyles.RendererColor;
        internal static readonly Color MaterialColor = MaterialEditorStyles.MaterialColor;
        internal static readonly Color CategoryColor = MaterialEditorStyles.CategoryColor;
        internal static readonly Color SubcategoryColor = MaterialEditorStyles.SubcategoryColor;
        internal static readonly Color ItemColor = MaterialEditorStyles.PropertyColor;
        internal static readonly Color ItemColorChanged = MaterialEditorStyles.ChangedRowColor;
        #endregion

        private protected IMaterialEditorColorPalette ColorPalette;

        internal GameObject CurrentGameObject
        {
            get => Session.CurrentGameObject;
            set => Session.CurrentGameObject = value;
        }

        internal object CurrentData
        {
            get => Session.CurrentData;
            set => Session.CurrentData = value;
        }

        internal static GameObject RetainedTargetGameObject =>
            Session.CurrentGameObject;

        internal static object RetainedTargetData => Session.CurrentData;

        private MaterialEditService _materialEditService;
        private static string CurrentFilter
        {
            get => Session.Filter;
            set => Session.Filter = value;
        }

        internal static SelectedInterpolable selectedInterpolable;
        internal static SelectedProjectorInterpolable selectedProjectorInterpolable;

        private protected MaterialEditService EditService =>
            _materialEditService ?? (_materialEditService = CreateMaterialEditService());

        private protected virtual MaterialEditService CreateMaterialEditService() =>
            new MaterialEditService(new LegacyMaterialEditRepository(this));

        /// <summary>
        /// Register a callback for clicks on renderer, material, shader, and property labels.
        /// Registering the same callback more than once has no effect.
        /// </summary>
        /// <param name="handler">Callback invoked with the current Material Editor context.</param>
        public static void RegisterLabelClickHandler(Action<MaterialEditorLabelClickEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (!LabelClickHandlers.Contains(handler))
                LabelClickHandlers.Add(handler);
        }

        /// <summary>
        /// Unregister a callback previously registered with <see cref="RegisterLabelClickHandler"/>.
        /// </summary>
        /// <param name="handler">Callback to remove.</param>
        public static void UnregisterLabelClickHandler(Action<MaterialEditorLabelClickEventArgs> handler)
        {
            if (handler == null)
                return;
            LabelClickHandlers.Remove(handler);
        }

        internal static void RaiseLabelClicked(MaterialEditorLabelClickEventArgs eventArgs)
        {
            foreach (var handler in LabelClickHandlers.ToArray())
            {
                try
                {
                    handler(eventArgs);
                }
                catch (Exception ex)
                {
                    MaterialEditorPluginBase.Logger?.LogError($"Exception in Material Editor label click handler: {ex}");
                }
            }
        }

        /// <summary>
        /// Initialize the MaterialEditor UI
        /// </summary>
        protected void InitUI()
        {
            ActiveUi = this;
            MaterialEditorExtensionRegistry.SetActiveEditService(EditService);
            _windowView = new MaterialEditorWindowView(
                transform,
                CurrentFilter,
                HandleFilterChanged,
                () => Visible = false,
                () => _selectionController.ToggleSidePanels(),
                () => _selectionController.HideSidePanels(),
                ToggleAllCategories,
                ToggleAllSections,
                NavigateToCategory,
                ToggleCategory);
            _selectionController = new MaterialEditorSelectionController(
                Session,
                _windowView,
                EditService,
                PopulateList);
            _selectionController.InitializeViewState();
            _presenter = new MaterialEditorPresenter(
                EditService,
                Session,
                new MaterialEditorPresentationActions
                {
                    Refresh = (go, data, filter) => PopulateList(
                        go,
                        data,
                        ResolveFilterForCurrentTarget(go, data, filter)),
                    RefreshDeferred = SchedulePopulateList,
                    RequestCondition = HandleConditionChanged,
                    RefreshMaterialSelection = PopulateMaterialList,
                    SetRendererCollapsed = SetRendererCollapsed,
                    ShowRename = PopulateRenameList,
                    ExportUv = Export.ExportUVMaps,
                    RequestObjExport = Session.RequestObjExport,
                    ExportTexture = ExportTexture,
                    ImportTexture = ImportTexture,
                    ExportCubemap = ExportCubemap,
                    ImportCubemap = ImportCubemap,
                    SelectInterpolable = SelectInterpolableButtonOnClick,
                    SelectProjectorInterpolable = SelectProjectorInterpolableButtonOnClick,
                    EditColor = (data, material, title, value, onChanged) =>
                        SetupColorPalette(data, material, title, value, onChanged, true),
                    SetColorToPalette = SetColorToPalette,
                    IsPropertyBlacklisted = (materialName, propertyName) =>
                        Instance.CheckBlacklist(materialName, propertyName)
                });

            ActiveView = _windowView;
            MaterialEditorWindow = _windowView.Window;
            MaterialEditorMainPanel = _windowView.MainPanel;
            DragPanel = _windowView.HeaderPanel;
            VirtualList = _windowView.VirtualList;
            Visible = false;
        }

        private protected void SetHeaderTitleHorizontalOffset(float offset)
        {
            _windowView?.SetHeaderTitleHorizontalOffset(offset);
        }

        private protected Transform HeaderContextSlot =>
            _windowView?.HeaderContextSlot;

        private protected void SetHeaderContextControlVisible(bool visible)
        {
            _windowView?.SetHeaderContextControlVisible(visible);
        }

        /// <summary>
        /// Refresh the MaterialEditor UI
        /// </summary>
        public void RefreshUI() => RefreshUI(CurrentFilter);
        /// <summary>
        /// Refresh the MaterialEditor UI using the specified filter text
        /// </summary>
        public void RefreshUI(string filterText) => PopulateList(CurrentGameObject, CurrentData, filterText);

        /// <summary>
        /// Get or set the MaterialEditor UI visibility
        /// </summary>
        public static bool Visible
        {
            get
            {
                if (MaterialEditorWindow != null && MaterialEditorWindow.gameObject != null)
                    return MaterialEditorWindow.gameObject.activeInHierarchy;
                return false;
            }
            set
            {
                var wasVisible = Visible;
                if (MaterialEditorWindow != null)
                    MaterialEditorWindow.gameObject.SetActive(value);
                if (!value)
                    ActiveUi?.ReleaseTransientUiContent();
                else if (!wasVisible)
                    ActiveUi?.RestoreTransientUiContent();
                if (wasVisible && !value)
                    LogPerformanceSummaryOnWindowClose();
            }
        }

        internal static void UISettingChanged(object sender, EventArgs e)
        {
            ActiveView?.ApplySettings();
        }

        /// <summary>
        /// Search text using wildcards.
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="filter">Filter with which to search the text</param>
        internal static bool WildCardSearch(string text, string filter)
        {
            return MaterialEditorFilter.Matches(text, filter);
        }

        /// <summary>
        /// Populate the renderer list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="rendListFull">List of all renderers to display</param>
        private void PopulateRendererList(GameObject go, object data, IEnumerable<Renderer> rendListFull)
        {
            _selectionController.PopulateRendererList(go, data, rendListFull);
        }


        /// <summary>
        /// Populate the materials list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="materials">List of all materials to display</param>
        private void PopulateMaterialList(GameObject go, object data, IEnumerable<Renderer> materials)
        {
            _selectionController.PopulateMaterialList(go, data, materials);
        }

        /// <summary>
        /// Populate the rename list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="material">Material to be renamed</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        private void PopulateRenameList(GameObject go, Material material, object data)
        {
            _selectionController.ShowRenamePanel(go, material, data);
        }

        /// <summary>
        /// Populate the MaterialEditor UI
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers and materials</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="filter">Comma separated list of text to filter the results</param>
        protected void PopulateList(GameObject go, object data, string filter = null)
        {
            CancelPendingRefreshes();
            PopulateListCore(go, data, filter, null, false);
        }

        private void PopulateList(
            GameObject go,
            object data,
            string filter,
            VirtualList.TopRowAnchor topRowAnchor)
        {
            CancelPendingRefreshes();
            PopulateListCore(go, data, filter, topRowAnchor, false);
        }

        private void PopulateListCore(
            GameObject go,
            object data,
            string filter,
            VirtualList.TopRowAnchor topRowAnchor,
            bool preserveRenamePanel,
            bool publishViewportAnchor = true)
        {
            var previousTarget = CurrentGameObject;
            var previousTargetWasDestroyed =
                !ReferenceEquals(previousTarget, null) && previousTarget == null;
            _transientContentReleased = false;
            if (!preserveRenamePanel)
                _selectionController.CloseRenamePanel();

            if (!ReferenceEquals(previousTarget, null)
                && !ReferenceEquals(previousTarget, go))
            {
                CloseTargetColorPalette();
                if (previousTargetWasDestroyed)
                {
                    ClearInterpolablesForTarget(previousTarget);
                    PruneDestroyedInterpolables();
                }
            }

            if (filter == null)
                filter = PersistFilter.Value ? CurrentFilter : string.Empty;

            _windowView.PrepareForDisplay(filter);
            if (go == null)
            {
                ReleaseRetainedTargetContext();
                Session.ClearTargetReferences();
                if (previousTargetWasDestroyed)
                    ClearInterpolablesForTarget(previousTarget);
                PruneDestroyedInterpolables();
                return;
            }

            var renderers = GetRendererList(go).ToList();
            var projectors = EditService.GetProjectorList(data, go).ToList();
            PopulateRendererList(go, data, renderers);

            CurrentGameObject = go;
            CurrentData = data;
            CurrentFilter = filter;

            _presentation = _presenter.BuildRows(go, data, filter, renderers, projectors);
            VirtualList.SetList(_presentation.Rows, false);
            if (topRowAnchor != null)
                VirtualList.RestoreTopRowAnchor(topRowAnchor, false);

            // Install the new presentation first, then publish its final anchor
            // once. Deferring the navigator update avoids a false no-categories
            // transition and two responsive layout passes during one rebuild.
            _windowView.SetPresentation(_presentation, true);
            if (publishViewportAnchor)
                VirtualList.PublishViewportAnchor();
        }

        private void NavigateToCategory(CategoryNavigationTarget target)
        {
            if (target == null)
                return;
            if (target.EnsureParentsExpanded())
            {
                RebuildAndScrollToCategory(target.SectionId, target.Id);
                return;
            }
            if (target.RowIndex >= 0)
                VirtualList.ScrollToIndex(target.RowIndex);
            _windowView?.CategoryNavigator?.CompleteNavigationDiagnostic(
                target.Id);
        }

        private void ToggleCategory(CategoryNavigationTarget target)
        {
            if (target == null)
                return;
            var sectionId = target.SectionId;
            var categoryId = target.Id;
            target.SetCollapsed(!target.Collapsed);
            RebuildAndScrollToCategory(sectionId, categoryId);
        }

        private void SetRendererCollapsed(
            RendererSectionPresentation section,
            bool collapsed)
        {
            var presentation = _presentation;
            if (presentation == null || VirtualList == null)
                return;

            var childRowsWereCreated = section != null
                                       && section.ChildRowsCreated;
            int replaceStartIndex;
            int removeCount;
            IList<RowModel> replacementRows;
            if (!presentation.TrySetRendererCollapsed(
                    section,
                    collapsed,
                    out replaceStartIndex,
                    out removeCount,
                    out replacementRows))
                return;

            if (!childRowsWereCreated && section.ChildRowsCreated)
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.RowModelCreation,
                    section.CachedChildRowCount);
            }
            VirtualList.ReplaceRange(
                replaceStartIndex,
                removeCount,
                replacementRows,
                section.HeaderRowIndex);
            _windowView?.RefreshSectionCollapseState(presentation);
        }

        private void ToggleAllCategories()
        {
            if (_presentation == null)
                return;

            _presentation.SetAllCategoriesCollapsed(
                !_presentation.AllCategoriesCollapsed);
            PopulateList(CurrentGameObject, CurrentData, CurrentFilter);
        }

        private void ToggleAllSections()
        {
            if (_presentation == null
                || !_presentation.CanToggleSections)
                return;

            _presentation.SetAllSectionsCollapsed(
                !_presentation.AllSectionsCollapsed);
            PopulateList(CurrentGameObject, CurrentData, CurrentFilter);
        }

        private void RebuildAndScrollToCategory(
            string sectionId,
            string categoryId)
        {
            CancelPendingRefreshes();
            PopulateListCore(
                CurrentGameObject,
                CurrentData,
                CurrentFilter,
                null,
                false,
                false);
            var target = _presentation?.FindCategory(sectionId, categoryId);
            if (target != null && target.RowIndex >= 0)
                VirtualList.ScrollToIndex(target.RowIndex);
            else
                VirtualList.PublishViewportAnchor();
            _windowView?.CategoryNavigator?.CompleteNavigationDiagnostic(
                categoryId);
        }

        /// <summary>
        /// Obj export should be done in OnGUI or something similarly late so that finger rotation is exported properly
        /// </summary>
        private void OnGUI()
        {
            if (Session.TryTakeObjExport(out var renderer))
                Export.ExportObj(renderer);
        }

        /// <summary>
        /// Hacky workaround to wait for the dropdown fade to complete before refreshing
        /// </summary>
        protected IEnumerator PopulateListCoroutine(GameObject go, object data, string filter = "")
        {
            var version = ScheduleDeferredPopulate(go, data, filter);
            yield return WaitForDeferredPopulate(version);
        }

        private void SchedulePopulateList(GameObject go, object data, string filter)
        {
            ScheduleDeferredPopulate(
                go,
                data,
                ResolveFilterForCurrentTarget(go, data, filter));
        }

        private int ScheduleDeferredPopulate(GameObject go, object data, string filter)
        {
            CancelPresentationInvalidation();
            var version = _deferredRefresh.Schedule(go, data, filter);
            if (_deferredRefresh.TryStartWorker())
            {
                try
                {
                    _deferredRefreshCoroutine = StartCoroutine(DeferredPopulateWorker());
                    if (_deferredRefreshCoroutine == null)
                        _deferredRefresh.WorkerStopped();
                }
                catch
                {
                    _deferredRefresh.WorkerStopped();
                    throw;
                }
            }
            return version;
        }

        private IEnumerator WaitForDeferredPopulate(int version)
        {
            while (_deferredRefresh.IsCurrent(version))
                yield return null;
        }

        private IEnumerator DeferredPopulateWorker()
        {
            while (_deferredRefresh.HasPending)
            {
                yield return null;

                object target;
                object data;
                string filter;
                if (!_deferredRefresh.AdvanceFrame(
                        10,
                        out target,
                        out data,
                        out filter))
                    continue;

                _deferredRefresh.WorkerStopped();
                _deferredRefreshCoroutine = null;
                PopulateListCore((GameObject)target, data, filter, null, false);
                yield break;
            }

            _deferredRefresh.WorkerStopped();
            _deferredRefreshCoroutine = null;
        }

        private void CancelDeferredPopulate()
        {
            _deferredRefresh.Cancel();
            if (_deferredRefreshCoroutine == null)
                return;
            StopCoroutine(_deferredRefreshCoroutine);
            _deferredRefreshCoroutine = null;
        }

        private string ResolveFilterForCurrentTarget(
            GameObject go,
            object data,
            string filter)
        {
            return ReferenceEquals(go, CurrentGameObject)
                   && ReferenceEquals(data, CurrentData)
                ? CurrentFilter
                : filter;
        }

        private void HandleFilterChanged(string filter)
        {
            CurrentFilter = filter;

            // Search is newer than any pending shader refresh and therefore wins.
            // Do not cancel this coordinator here: repeated keystrokes must share
            // the same worker lease and accumulate into one end-of-frame batch.
            CancelDeferredPopulate();
            if (!Visible || CurrentGameObject == null)
            {
                CancelPresentationInvalidation();
                return;
            }

            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RefreshRequests);
            if (!_presentationInvalidation.RequestSearch())
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.RefreshCoalesced);
            }
            TryStartPresentationInvalidationWorker();
        }

        private void HandleConditionChanged(
            MaterialConditionInvalidationHandle handle)
        {
            var presentation = _presentation;
            if (!Visible
                || CurrentGameObject == null
                || presentation == null
                || !presentation.Owns(handle))
                return;

            // A valid condition edit is newer than a pending shader rebuild.
            // Search remains dominant inside the shared presentation batch.
            CancelDeferredPopulate();
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RefreshRequests);
            if (!_presentationInvalidation.RequestCondition(handle))
            {
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.RefreshCoalesced);
            }
            TryStartPresentationInvalidationWorker();
        }

        private void TryStartPresentationInvalidationWorker()
        {
            PresentationInvalidationWorkerLease lease;
            if (!_presentationInvalidation.TryAcquireWorker(out lease))
                return;

            _presentationInvalidationCoroutineLeaseId = lease.LeaseId;
            Coroutine coroutine;
            try
            {
                coroutine = StartCoroutine(PresentationInvalidationWorker(lease));
            }
            catch (Exception ex)
            {
                RecoverPresentationInvalidationWorkerStart(lease, ex);
                return;
            }

            if (coroutine == null)
            {
                RecoverPresentationInvalidationWorkerStart(lease, null);
                return;
            }
            _presentationInvalidationCoroutine = coroutine;
        }

        private IEnumerator PresentationInvalidationWorker(
            PresentationInvalidationWorkerLease lease)
        {
            try
            {
                while (_presentationInvalidation.IsWorkerLeaseCurrent(lease))
                {
                    yield return PresentationEndOfFrame;

                    PresentationInvalidationBatch<
                        MaterialConditionInvalidationHandle> batch;
                    if (!_presentationInvalidation.TryBeginFlush(
                            lease,
                            Time.frameCount,
                            out batch))
                        continue;

                    var waitForNextFrame = false;
                    try
                    {
                        if (_presentationInvalidation.IsGenerationCurrent(
                                batch.Generation))
                            ApplyPresentationInvalidationBatch(batch);
                    }
                    catch (Exception ex)
                    {
                        MaterialEditorPluginBase.Logger?.LogError(
                            "Exception while applying a coalesced Material Editor "
                            + "presentation refresh: " + ex);
                    }
                    finally
                    {
                        waitForNextFrame =
                            _presentationInvalidation.CompleteFlush(lease);
                    }

                    if (!waitForNextFrame)
                        yield break;
                }
            }
            finally
            {
                _presentationInvalidation.AbandonWorker(lease);
                ClearPresentationInvalidationCoroutine(lease);
            }
        }

        private void RecoverPresentationInvalidationWorkerStart(
            PresentationInvalidationWorkerLease lease,
            Exception exception)
        {
            if (exception != null)
            {
                MaterialEditorPluginBase.Logger?.LogError(
                    "Could not start the Material Editor presentation refresh "
                    + "worker; draining its pending batch synchronously: "
                    + exception);
            }

            try
            {
                const int recoveryFlushLimit = 16;
                var flushCount = 0;
                while (_presentationInvalidation.IsWorkerLeaseCurrent(lease)
                       && flushCount < recoveryFlushLimit)
                {
                    PresentationInvalidationBatch<
                        MaterialConditionInvalidationHandle> batch;
                    if (!_presentationInvalidation.TryBeginRecoveryFlush(
                            lease,
                            out batch))
                        break;

                    var waitForNextBatch = false;
                    try
                    {
                        if (_presentationInvalidation.IsGenerationCurrent(
                                batch.Generation))
                            ApplyPresentationInvalidationBatch(batch);
                    }
                    catch (Exception ex)
                    {
                        MaterialEditorPluginBase.Logger?.LogError(
                            "Exception while applying the synchronous Material "
                            + "Editor presentation refresh fallback: " + ex);
                    }
                    finally
                    {
                        waitForNextBatch =
                            _presentationInvalidation.CompleteFlush(lease);
                    }

                    flushCount++;
                    if (!waitForNextBatch)
                        break;
                }

                if (_presentationInvalidation.IsWorkerLeaseCurrent(lease))
                {
                    _presentationInvalidation.AbandonWorker(lease);
                    if (_presentationInvalidation.HasPending)
                    {
                        _presentationInvalidation.Cancel();
                        MaterialEditorPluginBase.Logger?.LogError(
                            "Material Editor presentation refresh recovery "
                            + "exceeded its bounded synchronous flush limit.");
                    }
                }
            }
            finally
            {
                ClearPresentationInvalidationCoroutine(lease);
            }
        }

        private void ApplyPresentationInvalidationBatch(
            PresentationInvalidationBatch<
                MaterialConditionInvalidationHandle> batch)
        {
            if (!Visible || CurrentGameObject == null)
                return;
            if ((batch.Reason & PresentationInvalidationReason.Search) != 0)
            {
                ApplySearchRefresh();
                return;
            }
            if ((batch.Reason & PresentationInvalidationReason.Conditions) != 0)
                ApplyConditionRefresh(batch.ConditionSources);
        }

        private void ApplyConditionRefresh(
            IEnumerable<MaterialConditionInvalidationHandle> handles)
        {
            var presentation = _presentation;
            if (presentation == null)
                return;

            var grouped = new Dictionary<
                MaterialConditionDependencyGraph,
                List<MaterialConditionInvalidationHandle>>();
            foreach (var handle in handles)
            {
                if (!presentation.Owns(handle))
                    continue;
                List<MaterialConditionInvalidationHandle> graphHandles;
                if (!grouped.TryGetValue(handle.Graph, out graphHandles))
                {
                    graphHandles =
                        new List<MaterialConditionInvalidationHandle>();
                    grouped.Add(handle.Graph, graphHandles);
                }
                graphHandles.Add(handle);
            }
            if (grouped.Count == 0)
                return;

            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RefreshExecuted);
            var visibilityChanged = false;
            foreach (var entry in grouped)
            {
                var result = entry.Key.EvaluateHandles(entry.Value);
                visibilityChanged |= result.VisibilityChanged;
            }

            // Finish the complete batch before rebuilding so all coalesced
            // ShowIf sources are evaluated against the same presentation.
            if (visibilityChanged)
            {
                var topRowAnchor = VirtualList.CaptureTopRowAnchor();
                PopulateListCore(
                    CurrentGameObject,
                    CurrentData,
                    CurrentFilter,
                    topRowAnchor,
                    false);
            }
        }

        private void ApplySearchRefresh()
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RefreshExecuted);
            PopulateListCore(
                CurrentGameObject,
                CurrentData,
                CurrentFilter,
                null,
                false);
        }

        private void CancelPresentationInvalidation()
        {
            _presentationInvalidation.Cancel();
            var coroutine = _presentationInvalidationCoroutine;
            _presentationInvalidationCoroutine = null;
            _presentationInvalidationCoroutineLeaseId = 0;
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        private void ClearPresentationInvalidationCoroutine(
            PresentationInvalidationWorkerLease lease)
        {
            if (_presentationInvalidationCoroutineLeaseId != lease.LeaseId)
                return;
            _presentationInvalidationCoroutine = null;
            _presentationInvalidationCoroutineLeaseId = 0;
        }

        private void CancelPendingRefreshes()
        {
            CancelDeferredPopulate();
            CancelPresentationInvalidation();
        }

        private void ReleaseTransientUiContent()
        {
            CancelPendingRefreshes();
            DisposeTexChangeWatcher();
            VirtualList?.ReleaseContent();
            _selectionController?.ReleaseTransientContent();
            _windowView?.ReleasePresentation();
            _presentation = null;
            _transientContentReleased = true;
        }

        private void ReleaseRetainedTargetContext()
        {
            ReleaseTransientUiContent();
            _selectionController?.ReleaseTargetContent();
            CloseTargetColorPalette();
            _transientContentReleased = true;
        }

        private void RestoreTransientUiContent()
        {
            if (!_transientContentReleased)
                return;

            var gameObject = CurrentGameObject;
            var data = CurrentData;
            var filter = CurrentFilter;
            if (gameObject == null)
            {
                var destroyedTarget = !ReferenceEquals(gameObject, null);
                ReleaseRetainedTargetContext();
                Session.ClearTargetReferences();
                if (destroyedTarget)
                    ClearInterpolablesForTarget(gameObject);
                PruneDestroyedInterpolables();
                return;
            }

            _transientContentReleased = false;
            CancelPendingRefreshes();
            PopulateListCore(gameObject, data, filter, null, true);
        }

        internal static bool IsGameObjectWithin(
            GameObject candidate,
            GameObject root)
        {
            if (ReferenceEquals(candidate, root))
                return !ReferenceEquals(candidate, null);
            if (ReferenceEquals(candidate, null)
                || ReferenceEquals(root, null)
                || candidate == null
                || root == null)
                return false;

            try
            {
                return candidate.transform.IsChildOf(root.transform);
            }
            catch (MissingReferenceException)
            {
                return false;
            }
        }

        internal static bool IsCurrentTargetWithin(GameObject root) =>
            IsGameObjectWithin(Session.CurrentGameObject, root);

        internal static void ReleaseCurrentTargetSelections()
        {
            try
            {
                Visible = false;
                ActiveUi?._selectionController?.ReleaseTargetContent();
                ActiveUi?.CloseTargetColorPalette();
            }
            finally
            {
                Session.CancelObjExport();
                Session.ClearSelections();
                PruneDestroyedInterpolables();
            }
        }

        internal static void InvalidateCurrentTarget()
        {
            var target = Session.CurrentGameObject;
            try
            {
                Visible = false;
                ActiveUi?.ReleaseRetainedTargetContext();
            }
            finally
            {
                Session.ClearTargetReferences();
                if (ReferenceEquals(target, null))
                {
                    selectedInterpolable = null;
                    selectedProjectorInterpolable = null;
                }
                else
                {
                    ClearInterpolablesForTarget(target);
                    PruneDestroyedInterpolables();
                }
            }
        }

        internal static void InvalidateAllTargetState()
        {
            try
            {
                Visible = false;
                ActiveUi?.ReleaseRetainedTargetContext();
            }
            finally
            {
                Session.ClearTargetReferences();
                selectedInterpolable = null;
                selectedProjectorInterpolable = null;
            }
        }

        internal static bool NotifyTargetDestroyed(GameObject root)
        {
            ClearInterpolablesForTarget(root);
            PruneDestroyedInterpolables();
            if (!IsCurrentTargetWithin(root))
                return false;

            InvalidateCurrentTarget();
            return true;
        }

        internal static void PruneDestroyedInterpolables()
        {
            if (selectedInterpolable != null
                && selectedInterpolable.GameObject == null)
                selectedInterpolable = null;
            if (selectedProjectorInterpolable != null
                && selectedProjectorInterpolable.GameObject == null)
                selectedProjectorInterpolable = null;
        }

        private static void ClearInterpolablesForTarget(GameObject root)
        {
            if (ReferenceEquals(root, null))
                return;
            if (selectedInterpolable != null
                && IsGameObjectWithin(selectedInterpolable.GameObject, root))
                selectedInterpolable = null;
            if (selectedProjectorInterpolable != null
                && IsGameObjectWithin(
                    selectedProjectorInterpolable.GameObject,
                    root))
                selectedProjectorInterpolable = null;
        }

        private void CloseTargetColorPalette()
        {
            try
            {
                ColorPalette?.Close();
            }
            catch (Exception ex)
            {
                MaterialEditorPluginBase.Logger?.LogWarning(
                    "Could not close the Material Editor color palette while "
                    + "releasing a target: " + ex);
            }
        }

        internal void ShutdownMaterialEditorUi()
        {
            if (!ReferenceEquals(ActiveUi, this))
                return;

            try
            {
                InvalidateAllTargetState();
            }
            finally
            {
                MaterialEditorExtensionRegistry.SetActiveEditService(null);
                ActiveView = null;
                ActiveUi = null;
                MaterialEditorWindow = null;
                MaterialEditorMainPanel = null;
                DragPanel = null;
                VirtualList = null;
                _windowView = null;
                _selectionController = null;
                _presenter = null;
                ColorPalette = null;
            }
        }

        internal static void DisposeTexChangeWatcher()
        {
            var watcher = TexChangeWatcher;
            TexChangeWatcher = null;
            watcher?.Dispose();
        }

        private void ImportTexture(
            TexturePropertyRowModel textureItem,
            GameObject gameObject,
            object data,
            Material material,
            string propertyName)
        {
#if !API
            string fileFilter = KK_Plugins.ImageHelper.FileFilter;
#else
            string fileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
#endif
            var propertyHandle = MaterialPropertyIdCache.Get(propertyName);
            KKAPI.Utilities.OpenFileDialog.Show(
                OnFileAccept,
                "Open image",
                ExportPath,
                fileFilter,
                ".png");

            void OnFileAccept(string[] files)
            {
                ThreadingHelper.Instance.StartSyncInvoke(
                    () =>
                    {
                        if (this != null)
                            StartCoroutine(ApplyFileSelectionOnMainThread(files));
                    });
            }

            IEnumerator ApplyFileSelectionOnMainThread(string[] files)
            {
                // StartSyncInvoke is drained by BepInEx.Update. Yield once so
                // disk reads run outside that drain.
                yield return null;

                if (material == null || gameObject == null)
                    yield break;

                if (files == null || files.Length == 0 || files[0].IsNullOrEmpty())
                {
                    textureItem.Changed =
                        !EditService.GetMaterialTextureValueOriginal(
                            data,
                            material,
                            propertyName,
                            gameObject);
                    var currentTexture = MaterialPropertyAccess.GetTexture(
                        material,
                        propertyHandle);
                    textureItem.Exists = currentTexture != null;
                    textureItem.RefreshState?.Invoke();
                    yield break;
                }

                string filePath = files[0];
                EditService.SetMaterialTexture(
                    data,
                    material,
                    propertyName,
                    filePath,
                    gameObject,
                    succeeded =>
                    {
                        if (this == null || material == null || gameObject == null)
                            return;

                        // Character and Studio repositories apply Texture2D imports
                        // on their next Update. Refresh only after that work reports
                        // completion, and derive both flags from the real edit/material
                        // state instead of assuming that decoding succeeded.
                        textureItem.Changed =
                            !EditService.GetMaterialTextureValueOriginal(
                                data,
                                material,
                                propertyName,
                                gameObject);
                        textureItem.Exists = MaterialPropertyAccess.GetTexture(
                            material,
                            propertyHandle) != null;
                        textureItem.RefreshState?.Invoke();

                        if (!succeeded)
                        {
                            MaterialEditorPluginBase.Logger.LogWarning(
                                $"Could not import texture '{filePath}' for {propertyName}.");
                        }
                    });

                DisposeTexChangeWatcher();
                if (!WatchTexChanges.Value)
                    yield break;

                var directory = Path.GetDirectoryName(filePath);
                if (directory == null)
                    yield break;

                TexChangeWatcher = new FileSystemWatcher(directory, Path.GetFileName(filePath));
                TexChangeWatcher.Changed += (sender, args) =>
                {
                    if (WatchTexChanges.Value && File.Exists(filePath))
                        ScheduleTextureWatcherImport(data, material, propertyName, filePath, gameObject);
                };
                TexChangeWatcher.Deleted += (sender, args) => DisposeTexChangeWatcher();
                TexChangeWatcher.Error += (sender, args) => DisposeTexChangeWatcher();
                TexChangeWatcher.EnableRaisingEvents = true;
            }
        }

        private void ImportCubemap(
            CubemapPropertyRowModel cubemapItem,
            GameObject gameObject,
            object data,
            Material material,
            string propertyName)
        {
            const string fileFilter = "Cubemap panoramas (*.png;*.hdr)|*.png;*.hdr|PNG images (*.png)|*.png|Radiance HDR images (*.hdr)|*.hdr|All files|*.*";
            var propertyHandle = MaterialPropertyIdCache.Get(propertyName);
            KKAPI.Utilities.OpenFileDialog.Show(
                OnFileAccept,
                "Open Cubemap source",
                ExportPath,
                fileFilter,
                ".png");

            void OnFileAccept(string[] files)
            {
                ThreadingHelper.Instance.StartSyncInvoke(
                    () =>
                    {
                        if (this == null
                            || material == null
                            || gameObject == null
                            || files == null
                            || files.Length == 0
                            || files[0].IsNullOrEmpty())
                            return;

                        var filePath = files[0];
                        try
                        {
                            var runner = this.gameObject.AddComponent<
                                MaterialEditorCubemapImportRunner>();
                            runner.Begin(
                                filePath,
                                () => this != null
                                      && material != null
                                      && gameObject != null
                                      && MaterialPropertyAccess.HasProperty(
                                          material,
                                          propertyHandle),
                                (encodedData, contentKey, warmLease) =>
                                {
                                    // Keep the preheated cache entry alive until
                                    // the repository has acquired its own lease.
                                    if (warmLease == null)
                                        return false;
                                    if (EditService.SupportsMaterialCubemapDataImport(
                                            data))
                                    {
                                        // A supported repository reports the
                                        // real persistence/application result.
                                        // Do not reinterpret failure as a reason
                                        // to retry through the legacy file API.
                                        return EditService.SetMaterialCubemap(
                                            data,
                                            material,
                                            propertyName,
                                            encodedData,
                                            contentKey,
                                            gameObject);
                                    }

                                    // External/legacy repositories only expose
                                    // the original file-path API. Current Chara
                                    // and Studio repositories use the byte seam,
                                    // so they do not repeat disk IO here.
                                    MaterialEditorPluginBase.Logger?.LogWarning(
                                        "The active Material Editor repository does not support "
                                        + "preloaded Cubemap data; using its legacy file import path.");
                                    EditService.SetMaterialCubemap(
                                        data,
                                        material,
                                        propertyName,
                                        filePath,
                                        gameObject);
                                    return true;
                                },
                                message => MaterialEditorPluginBase.Logger?.LogInfo(
                                    message),
                                message => MaterialEditorPluginBase.Logger?.LogWarning(
                                    message),
                                message => MaterialEditorPluginBase.Logger?.LogError(
                                    "Could not import Cubemap '"
                                    + propertyName
                                    + "': "
                                    + message),
                                succeeded =>
                                {
                                    if (this == null
                                        || material == null
                                        || gameObject == null)
                                        return;

                                    cubemapItem.Changed =
                                        !EditService.GetMaterialCubemapValueOriginal(
                                            data,
                                            material,
                                            propertyName,
                                            gameObject);
                                    cubemapItem.Exists =
                                        MaterialPropertyAccess.GetTexture(
                                            material,
                                            propertyHandle) is Cubemap;
                                    cubemapItem.RefreshState?.Invoke();
                                });
                        }
                        catch (Exception exception)
                        {
                            MaterialEditorPluginBase.Logger?.LogError(
                                "Could not start Cubemap import '"
                                + propertyName
                                + "': "
                                + exception.Message);
                        }
                    });
            }
        }

        private void ScheduleTextureWatcherImport(
            object data,
            Material material,
            string propertyName,
            string filePath,
            GameObject gameObject)
        {
            // FileSystemWatcher raises Changed on a ThreadPool thread, so
            // marshal the Texture2D import before it touches Unity objects.
            ThreadingHelper.Instance.StartSyncInvoke(() =>
            {
                if (this != null
                    && material != null
                    && gameObject != null
                    && WatchTexChanges.Value
                    && File.Exists(filePath))
                    EditService.SetMaterialTexture(data, material, propertyName, filePath, gameObject);
            });
        }

        internal virtual void ExportTexture(Material mat, string property)
        {
            var tex = MaterialPropertyAccess.GetTexture(
                mat,
                MaterialPropertyIdCache.Get(property));
            if (tex == null) return;
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.png");
            Instance.ConvertNormalMap(ref tex, property, ConvertNormalmapsOnExport.Value);
            SaveTex(tex, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportCubemap(Material mat, string property)
        {
            var cubemap = MaterialPropertyAccess.GetTexture(
                mat,
                MaterialPropertyIdCache.Get(property)) as Cubemap;
            if (cubemap == null)
                return;

            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(
                ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.png");
            byte[] pngData;
            string error;
            if (!MaterialEditorCubemapConversion.TryExport(
                    cubemap,
                    out pngData,
                    out error))
            {
                MaterialEditorPluginBase.Logger.LogError(error);
                MaterialEditorPluginBase.Logger.LogMessage(error);
                return;
            }

            File.WriteAllBytes(filename, pngData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportTextureOriginal(Material mat, string property, string ext, byte[] texData)
        {
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.{ext}");
            System.IO.File.WriteAllBytes(filename, texData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        private void SetupColorPalette(object data, Material material, string title, Color value, Action<Color> onChanged, bool useAlpha)
        {
            var name = material.name;
            if (ColorPalette.IsShowing(title, data, name))
            {
                ColorPalette.Close();
                return;
            }

            try
            {
                ColorPalette.Setup(title, data, name, value, onChanged, useAlpha);
            }
            catch (ArgumentException)
            {
                MaterialEditorPluginBase.Logger.LogError($"Color value is out of range. ({value})");
                ColorPalette.Close();
            }
        }
        private void SetColorToPalette(object data, Material material, string title, Color value)
        {
            if (ColorPalette.IsShowing(title, data, material.name))
            {
                try
                {
                    ColorPalette.SetColor(value);
                }
                catch (ArgumentException)
                {
                    MaterialEditorPluginBase.Logger.LogError($"Color value is out of range. ({value})");
                    ColorPalette.Close();
                }
            }
        }

        private void SelectInterpolableButtonOnClick(GameObject go, RowModel.RowItemType rowType, string materialName = "", string propertyName = "", string rendererName = "")
        {
            selectedInterpolable = new SelectedInterpolable(go, rowType, materialName, propertyName, rendererName);
            MaterialEditorPluginBase.Logger.LogMessage($"Activated interpolable(s), {selectedInterpolable}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        private void SelectProjectorInterpolableButtonOnClick(GameObject go, ProjectorProperties property, string projectorName)
        {
            selectedProjectorInterpolable = new SelectedProjectorInterpolable(go, property, projectorName);
            MaterialEditorPluginBase.Logger.LogMessage($"Activated interpolable(s), {selectedProjectorInterpolable}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        internal class SelectedInterpolable
        {
            public string MaterialName;
            public string PropertyName;
            public string RendererName;
            public GameObject GameObject;
            public RowModel.RowItemType RowType;

            public SelectedInterpolable(GameObject go, RowModel.RowItemType rowType, string materialName, string propertyName, string rendererName)
            {
                GameObject = go;
                RowType = rowType;
                MaterialName = materialName;
                PropertyName = propertyName;
                RendererName = rendererName;
            }

            public override string ToString()
            {
                return $"{RowType}: {string.Join(" - ", new string[] { PropertyName, MaterialName, RendererName, }.Where(x => !x.IsNullOrEmpty()).ToArray())}";
            }
        }

        internal class SelectedProjectorInterpolable
        {
            public string ProjectorName;
            public ProjectorProperties Property;
            public GameObject GameObject;

            public SelectedProjectorInterpolable(GameObject go, ProjectorProperties property, string projectorName)
            {
                GameObject = go;
                Property = property;
                ProjectorName = projectorName;
            }

            public override string ToString()
            {
                return $"Projector: {string.Join(" - ", new string[] { Property.ToString(), ProjectorName, }.Where(x => !x.IsNullOrEmpty()).ToArray())}";
            }
        }
    }

}
