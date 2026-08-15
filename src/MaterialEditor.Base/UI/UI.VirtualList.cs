using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal class VirtualList : MonoBehaviour
    {
        private static readonly bool instantiateOverloadExists = typeof(UnityEngine.Object).GetMethod("Instantiate", new[] { typeof(GameObject), typeof(Transform) }) != null;

        private readonly List<RowView> _cachedViews = new List<RowView>();
        private readonly List<RowModel> _models = new List<RowModel>();

        internal event Action<int> ViewportAnchorIndexChanged;

        public GameObject EntryTemplate;
        public ScrollRect ScrollRect;

        private bool _dirty;
        private int _lastItemsAboveViewRect;
        private float _lastScrollPosition = float.NaN;
        private float _lastViewportHeight = float.NaN;
        private int _viewportAnchorIndex = -1;
        private int _viewportRestoreVersion;
        private bool _viewportRestorePending;
        private bool _programmaticViewportAnchorPinned;
        private float _programmaticScrollPosition = float.NaN;
        private bool _rangeMutationAnchorPublished;
        private float _rangeMutationScrollPosition = float.NaN;
        private int _activeViewCapacity;

        private int _paddingBot;
        private int _paddingTop;

        private VerticalLayoutGroup _verticalLayoutGroup;

        public void Initialize()
        {
            if (ScrollRect == null) throw new ArgumentNullException(nameof(ScrollRect));

            _verticalLayoutGroup = ScrollRect.content.GetComponent<VerticalLayoutGroup>();
            if (_verticalLayoutGroup == null) throw new ArgumentNullException(nameof(_verticalLayoutGroup));

            _paddingTop = _verticalLayoutGroup.padding.top;
            _paddingBot = _verticalLayoutGroup.padding.bottom;

            SetupEntryTemplate();
            Clear();
        }

        private void SetupEntryTemplate()
        {
            if (EntryTemplate == null) throw new ArgumentNullException(nameof(EntryTemplate));

            EntryTemplate.SetActive(false);

            var rowView = EntryTemplate.AddComponent<RowView>();
            var listEntry = EntryTemplate.AddComponent<RowBinder>();
            rowView.Initialize(listEntry);
            rowView.Bind(null, true);
#if DEBUG
            RowLayoutRuntimeAssertions.Validate(rowView);
#endif
        }

        internal void EnsureViewportCapacity(float viewportHeight)
        {
            if (EntryTemplate == null)
                return;

            var required = VirtualListCachePolicy.RequiredViewCount(
                viewportHeight,
                PanelHeight,
                _models.Count);
            while (_cachedViews.Count < required)
                _cachedViews.Add(CreatePooledView());

            if (_activeViewCapacity == required)
                return;

            if (required < _activeViewCapacity)
            {
                for (var index = required;
                     index < _activeViewCapacity;
                     index++)
                    _cachedViews[index].Release();
            }

            _activeViewCapacity = required;
            _dirty = true;
#if DEBUG
            if (_cachedViews.Count > 0)
                RowLayoutRuntimeAssertions.Validate(_cachedViews[0]);
            if (_cachedViews.Count > 1)
                RowLayoutRuntimeAssertions.ValidateClones(
                    _cachedViews[0],
                    _cachedViews[1]);
#endif
        }

        private RowView CreatePooledView()
        {
            GameObject copy;
            if (instantiateOverloadExists)
            {
                copy = Instantiate(
                    EntryTemplate,
                    EntryTemplate.transform.parent);
            }
            else
            {
                copy = Instantiate(EntryTemplate);
                copy.transform.parent = EntryTemplate.transform.parent;
            }
            var entry = copy.GetComponent<RowView>();
            entry.Initialize(copy.GetComponent<RowBinder>());
            entry.Release();
            return entry;
        }

        public void Clear()
        {
            SetList(null);
        }

        public void SetList(IEnumerable<RowModel> items)
        {
            SetList(items, true);
        }

        internal void SetList(
            IEnumerable<RowModel> items,
            bool publishViewportAnchor)
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.CacheInvalidations);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.VisibleRowsInvalidations);
            SuspendRowListeners();
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            _models.Clear();
            if (items != null)
                _models.AddRange(items);

            EnsureViewportCapacity(GetViewportHeight());

            for (var index = _models.Count; index < _cachedViews.Count; index++)
                _cachedViews[index].Release();

            _dirty = true;
            UpdateViewportAnchor(true, publishViewportAnchor);
        }

        internal void ReplaceRange(
            int startIndex,
            int removeCount,
            IList<RowModel> replacementRows,
            int anchorFallbackIndex)
        {
            if (startIndex < 0 || startIndex > _models.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (removeCount < 0
                || removeCount > _models.Count - startIndex)
                throw new ArgumentOutOfRangeException(nameof(removeCount));

            var replacementCount = replacementRows == null
                ? 0
                : replacementRows.Count;
            if (removeCount == 0 && replacementCount == 0)
                return;

            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.CacheInvalidations);
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.VisibleRowsInvalidations);
            SuspendRowListeners();
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();

            var scrollPosition = Mathf.Max(
                0f,
                ScrollRect.content.localPosition.y);
            var topRowIndex = _models.Count == 0
                ? -1
                : Mathf.Clamp(
                    Mathf.FloorToInt(scrollPosition / PanelHeight),
                    0,
                    _models.Count - 1);
            var offsetWithinRow = topRowIndex < 0
                ? 0f
                : scrollPosition - topRowIndex * PanelHeight;
            var nextTopRowIndex = ShiftIndexForRange(
                topRowIndex,
                startIndex,
                removeCount,
                replacementCount,
                anchorFallbackIndex);
            var nextViewportAnchor = ShiftIndexForRange(
                _viewportAnchorIndex,
                startIndex,
                removeCount,
                replacementCount,
                anchorFallbackIndex);

            if (removeCount != 0)
                _models.RemoveRange(startIndex, removeCount);
            if (replacementCount != 0)
                _models.InsertRange(startIndex, replacementRows);

            EnsureViewportCapacity(GetViewportHeight());
            var position = ScrollRect.content.localPosition;
            if (_models.Count == 0)
            {
                position.y = 0f;
                nextViewportAnchor = -1;
            }
            else
            {
                nextTopRowIndex = Mathf.Clamp(
                    nextTopRowIndex,
                    0,
                    _models.Count - 1);
                nextViewportAnchor = Mathf.Clamp(
                    nextViewportAnchor,
                    0,
                    _models.Count - 1);
                var viewport = ScrollRect.viewport != null
                    ? ScrollRect.viewport
                    : ScrollRect.GetComponent<RectTransform>();
                var maximum = Mathf.Max(
                    0f,
                    _models.Count * PanelHeight - viewport.rect.height);
                position.y = Mathf.Clamp(
                    nextTopRowIndex * PanelHeight + offsetWithinRow,
                    0f,
                    maximum);
            }

            ScrollRect.StopMovement();
            ScrollRect.content.localPosition = position;
            _dirty = true;
            _rangeMutationAnchorPublished = true;
            _rangeMutationScrollPosition = position.y;
            SetViewportAnchorIndex(nextViewportAnchor, true, true);
            // A targeted collapse is complete when this method returns: stale
            // child RowViews and their listener graphs must not survive until
            // the next Unity frame. Update consumes the one dirty/layout pass;
            // the normal frame callback then remains on its idle fast path.
            Update();
        }

        private static int ShiftIndexForRange(
            int index,
            int startIndex,
            int removeCount,
            int replacementCount,
            int fallbackIndex)
        {
            if (index < 0 || index < startIndex)
                return index;
            if (removeCount != 0
                && index < startIndex + removeCount)
                return fallbackIndex;
            return index + replacementCount - removeCount;
        }

        internal void ReleaseContent()
        {
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            _models.Clear();
            for (var index = 0; index < _cachedViews.Count; index++)
                _cachedViews[index].Release();

            // Keep the exact scroll, published anchor, and padding for reopening.
            // Recording the current geometry also makes an accidental Update while
            // hidden a no-op instead of publishing an empty-list anchor.
            _lastScrollPosition = ScrollRect.content.localPosition.y;
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            _lastViewportHeight = viewport.rect.height;
            _dirty = false;
        }

        internal void SuspendRowListeners()
        {
            for (var index = 0; index < _cachedViews.Count; index++)
                _cachedViews[index].SuspendListeners();
        }

        private void Update()
        {
            var scrollPosition = ScrollRect.content.localPosition.y;
            var hadLastScrollPosition = !float.IsNaN(_lastScrollPosition);
            var scrollPositionChanged = hadLastScrollPosition
                                        && scrollPosition != _lastScrollPosition;
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            var viewportHeight = viewport.rect.height;
            if (viewportHeight != _lastViewportHeight)
                EnsureViewportCapacity(viewportHeight);
            // Explicit navigation owns the published anchor until the content
            // actually moves again. Virtualization still runs while it is pinned.
            if (_programmaticViewportAnchorPinned
                && !Mathf.Approximately(
                    scrollPosition,
                    _programmaticScrollPosition))
                ClearProgrammaticViewportAnchor();
            var rangeMutationAnchorPublished =
                _rangeMutationAnchorPublished
                && Mathf.Approximately(
                    scrollPosition,
                    _rangeMutationScrollPosition);
            ClearRangeMutationAnchor();
            if (!_dirty
                && scrollPosition == _lastScrollPosition
                && viewportHeight == _lastViewportHeight)
                return;

            _lastScrollPosition = scrollPosition;
            _lastViewportHeight = viewportHeight;
            if (!_programmaticViewportAnchorPinned
                && !rangeMutationAnchorPublished)
            {
                var previousViewportAnchor = _viewportAnchorIndex;
                UpdateViewportAnchor(false, !_viewportRestorePending);
                if (scrollPositionChanged
                    && !_viewportRestorePending
                    && previousViewportAnchor != _viewportAnchorIndex)
                {
                    MaterialEditorPerformance.Increment(
                        MaterialEditorPerformanceMetric.ViewportManualSelections);
                }
            }
            // How many items are not visible in current view
            var offscreenItemCount = Mathf.Max(
                0,
                _models.Count - _activeViewCapacity);
            // How many items are above current view rect and not visible
            var itemsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / PanelHeight, 0, offscreenItemCount));

            if (_lastItemsAboveViewRect == itemsAboveViewRect && !_dirty)
                return;

            _lastItemsAboveViewRect = itemsAboveViewRect;
            _dirty = false;

            // Store selected item to preserve selection when moving the list with mouse
            RowModel selectedItem = null;
            if (EventSystem.current != null)
            {
                var selectedGameObject = EventSystem.current.currentSelectedGameObject;
                for (var index = 0; index < _cachedViews.Count; index++)
                {
                    var cachedEntry = _cachedViews[index];
                    if (cachedEntry.gameObject != selectedGameObject)
                        continue;
                    selectedItem = cachedEntry.CurrentModel;
                    break;
                }
            }

            var visibleCount = Mathf.Min(
                _activeViewCapacity,
                _models.Count - itemsAboveViewRect);
            var hasEventSystem = EventSystem.current != null;
            for (var index = 0; index < visibleCount; index++)
            {
                var item = _models[itemsAboveViewRect + index];
                var cachedEntry = _cachedViews[index];

                cachedEntry.Bind(item, false);
                cachedEntry.SetVisible(true);

                if (hasEventSystem && ReferenceEquals(selectedItem, item))
                    EventSystem.current.SetSelectedGameObject(cachedEntry.gameObject);
            }

            // Keep the GameObjects pooled, but release every stale model/listener graph.
            for (var index = visibleCount;
                 index < _activeViewCapacity;
                 index++)
                _cachedViews[index].Release();

            RecalculateOffsets(
                itemsAboveViewRect,
                _activeViewCapacity);

            // Needed after changing _verticalLayoutGroup.padding since it doesn't make the object dirty
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.LayoutInvalidations);
            LayoutRebuilder.MarkLayoutForRebuild(_verticalLayoutGroup.GetComponent<RectTransform>());
        }

        private void RecalculateOffsets(
            int itemsAboveViewRect,
            int activeViewCapacity)
        {
            var topOffset = Mathf.RoundToInt(itemsAboveViewRect * PanelHeight);
            _verticalLayoutGroup.padding.top = _paddingTop + topOffset;

            var totalHeight = _models.Count * PanelHeight;
            var cacheEntriesHeight = activeViewCapacity * PanelHeight;
            var trailingHeight = totalHeight - cacheEntriesHeight - topOffset;
            _verticalLayoutGroup.padding.bottom = Mathf.FloorToInt(Mathf.Max(0, trailingHeight) + _paddingBot);
        }

        internal int ViewportAnchorIndex => _viewportAnchorIndex;
        internal bool ViewportAnchorIsProgrammatic =>
            _programmaticViewportAnchorPinned;
        internal int ActiveViewCapacity => _activeViewCapacity;
        internal int CachedViewCount => _cachedViews.Count;

        internal bool TryGetVisibleRowRange(
            out int firstVisibleRowIndex,
            out int lastVisibleRowIndex)
        {
            firstVisibleRowIndex = -1;
            lastVisibleRowIndex = -1;
            if (_models.Count == 0
                || ScrollRect == null
                || ScrollRect.content == null)
                return false;

            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            if (viewport == null)
                return false;

            var viewportHeight = viewport.rect.height;
            if (viewportHeight <= 0f
                || float.IsNaN(viewportHeight)
                || float.IsInfinity(viewportHeight))
                return false;

            var scrollPosition = Mathf.Max(
                0f,
                ScrollRect.content.localPosition.y);
            firstVisibleRowIndex = Mathf.Clamp(
                Mathf.FloorToInt(scrollPosition / PanelHeight),
                0,
                _models.Count - 1);
            const float boundaryEpsilon = 0.001f;
            var visibleBottom = Mathf.Max(
                scrollPosition,
                scrollPosition + viewportHeight - boundaryEpsilon);
            lastVisibleRowIndex = Mathf.Clamp(
                Mathf.FloorToInt(visibleBottom / PanelHeight),
                firstVisibleRowIndex,
                _models.Count - 1);
            return true;
        }

        internal sealed class TopRowAnchor
        {
            internal readonly List<RowIdentity> Rows;
            internal readonly int TopRowIndex;
            internal readonly float OffsetWithinRow;

            internal TopRowAnchor(
                List<RowIdentity> rows,
                int topRowIndex,
                float offsetWithinRow)
            {
                Rows = rows;
                TopRowIndex = topRowIndex;
                OffsetWithinRow = offsetWithinRow;
            }
        }

        internal TopRowAnchor CaptureTopRowAnchor()
        {
            if (_models.Count == 0)
                return null;

            var scrollPosition = Mathf.Max(0f, ScrollRect.content.localPosition.y);
            var topRowIndex = Mathf.Clamp(
                Mathf.FloorToInt(scrollPosition / PanelHeight),
                0,
                _models.Count - 1);
            return new TopRowAnchor(
                CreateRowIdentities(_models),
                topRowIndex,
                scrollPosition - topRowIndex * PanelHeight);
        }

        internal void RestoreTopRowAnchor(
            TopRowAnchor anchor,
            bool publishViewportAnchor = true)
        {
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            var restoreVersion = ++_viewportRestoreVersion;
            _viewportRestorePending = true;
            ApplyTopRowAnchor(anchor, publishViewportAnchor);
            StartCoroutine(RestoreTopRowAnchorAfterLayout(anchor, restoreVersion));
        }

        internal void PublishViewportAnchor()
        {
            ViewportAnchorIndexChanged?.Invoke(_viewportAnchorIndex);
        }

        internal void ScrollToIndex(int index)
        {
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            if (_models.Count == 0)
                return;

            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.ViewportProgrammaticSelections);
            index = Mathf.Clamp(index, 0, _models.Count - 1);
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            var maximum = Mathf.Max(
                0f,
                _models.Count * PanelHeight - viewport.rect.height);
            var position = ScrollRect.content.localPosition;
            position.y = Mathf.Clamp(index * PanelHeight, 0f, maximum);
            ScrollRect.StopMovement();
            ScrollRect.content.localPosition = position;
            _dirty = true;
            // The requested row is the semantic selection even when the last rows
            // cannot be aligned with the top because the scroll position clamps.
            _programmaticViewportAnchorPinned = true;
            _programmaticScrollPosition = position.y;
            SetViewportAnchorIndex(index, true, true);
        }

        private void ClearProgrammaticViewportAnchor()
        {
            _programmaticViewportAnchorPinned = false;
            _programmaticScrollPosition = float.NaN;
        }

        private void ClearRangeMutationAnchor()
        {
            _rangeMutationAnchorPublished = false;
            _rangeMutationScrollPosition = float.NaN;
        }

        private IEnumerator RestoreTopRowAnchorAfterLayout(
            TopRowAnchor anchor,
            int restoreVersion)
        {
            yield return null;
            if (restoreVersion != _viewportRestoreVersion)
                yield break;

            _viewportRestorePending = false;
            ApplyTopRowAnchor(anchor, false);
        }

        private void ApplyTopRowAnchor(TopRowAnchor anchor, bool publishViewportAnchor)
        {
            if (_models.Count == 0)
            {
                var emptyPosition = ScrollRect.content.localPosition;
                emptyPosition.y = 0f;
                ScrollRect.StopMovement();
                ScrollRect.content.localPosition = emptyPosition;
                _dirty = true;
                UpdateViewportAnchor(true, publishViewportAnchor);
                return;
            }

            if (anchor == null)
            {
                if (publishViewportAnchor)
                    PublishViewportAnchor();
                return;
            }

            var identities = CreateRowIdentities(_models);
            var currentRows = new Dictionary<RowIdentity, int>();
            for (var index = 0; index < identities.Count; index++)
                if (!currentRows.ContainsKey(identities[index]))
                    currentRows.Add(identities[index], index);

            var targetIndex = FindRestoreIndex(anchor, currentRows);
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            var maximum = Mathf.Max(
                0f,
                _models.Count * PanelHeight - viewport.rect.height);
            var position = ScrollRect.content.localPosition;
            position.y = Mathf.Clamp(
                targetIndex * PanelHeight + anchor.OffsetWithinRow,
                0f,
                maximum);
            ScrollRect.StopMovement();
            ScrollRect.content.localPosition = position;
            _dirty = true;
            UpdateViewportAnchor(true, publishViewportAnchor);
        }

        private static int FindRestoreIndex(
            TopRowAnchor anchor,
            IDictionary<RowIdentity, int> currentRows)
        {
            int targetIndex;
            var topIdentity = anchor.Rows[anchor.TopRowIndex];
            if (currentRows.TryGetValue(topIdentity, out targetIndex))
                return targetIndex;

            if (TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => candidate.HasSameCategory(topIdentity),
                    out targetIndex)
                || TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => candidate.HasSameShader(topIdentity),
                    out targetIndex)
                || TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => candidate.HasSameMaterial(topIdentity),
                    out targetIndex)
                || TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => true,
                    out targetIndex))
                return targetIndex;

            return Mathf.Clamp(anchor.TopRowIndex, 0, currentRows.Count - 1);
        }

        private static bool TryFindNearest(
            TopRowAnchor anchor,
            IDictionary<RowIdentity, int> currentRows,
            Func<RowIdentity, bool> scopeMatches,
            out int targetIndex)
        {
            for (var distance = 1; distance < anchor.Rows.Count; distance++)
            {
                var previous = anchor.TopRowIndex - distance;
                if (previous >= 0
                    && scopeMatches(anchor.Rows[previous])
                    && currentRows.TryGetValue(anchor.Rows[previous], out targetIndex))
                    return true;

                var next = anchor.TopRowIndex + distance;
                if (next < anchor.Rows.Count
                    && scopeMatches(anchor.Rows[next])
                    && currentRows.TryGetValue(anchor.Rows[next], out targetIndex))
                    return true;
            }

            targetIndex = -1;
            return false;
        }

        private static List<RowIdentity> CreateRowIdentities(IList<RowModel> rows)
        {
            var result = new List<RowIdentity>(rows.Count);
            var occurrences = new Dictionary<RowIdentityKey, int>();
            GameObject currentGameObject = null;
            Renderer currentRenderer = null;
            Material currentMaterial = null;
            Projector currentProjector = null;
            string currentShader = null;
            string currentCategory = null;

            foreach (var row in rows)
            {
                var rendererRow = row as RendererRowModel;
                if (rendererRow != null)
                {
                    currentGameObject = row.GameObject;
                    currentRenderer = row.Renderer;
                    currentMaterial = null;
                    currentProjector = null;
                    currentShader = null;
                    currentCategory = null;
                }

                var materialRow = row as MaterialRowModel;
                if (materialRow != null)
                {
                    currentGameObject = row.GameObject;
                    currentRenderer = null;
                    currentMaterial = row.Material;
                    currentProjector = row.Projector;
                    currentShader = null;
                    currentCategory = null;
                }

                var shaderRow = row as ShaderRowModel;
                if (shaderRow != null)
                {
                    currentShader = shaderRow.ShaderName;
                    currentCategory = null;
                }

                if (row is PropertyCategoryRowModel)
                    currentCategory = row.LabelText;

                var key = new RowIdentityKey(
                    row.ItemType,
                    ReferenceEquals(row.GameObject, null)
                        ? currentGameObject
                        : row.GameObject,
                    ReferenceEquals(row.Renderer, null)
                        ? currentRenderer
                        : row.Renderer,
                    ReferenceEquals(row.Material, null)
                        ? currentMaterial
                        : row.Material,
                    ReferenceEquals(row.Projector, null)
                        ? currentProjector
                        : row.Projector,
                    currentShader,
                    currentCategory,
                    row.PropertyName,
                    row.PublicDescriptor == null ? null : row.PublicDescriptor.Id,
                    row.LabelText);
                int occurrence;
                occurrences.TryGetValue(key, out occurrence);
                occurrences[key] = occurrence + 1;
                result.Add(new RowIdentity(key, occurrence));
            }

            return result;
        }

        internal sealed class RowIdentity : IEquatable<RowIdentity>
        {
            private readonly RowIdentityKey _key;
            private readonly int _occurrence;

            internal RowIdentity(RowIdentityKey key, int occurrence)
            {
                _key = key;
                _occurrence = occurrence;
            }

            internal bool HasSameCategory(RowIdentity other)
            {
                return other != null
                       && !string.IsNullOrEmpty(_key.Category)
                       && HasSameShader(other)
                       && StringComparer.Ordinal.Equals(
                           _key.Category,
                           other._key.Category);
            }

            internal bool HasSameShader(RowIdentity other)
            {
                return other != null
                       && !string.IsNullOrEmpty(_key.Shader)
                       && HasSameMaterial(other)
                       && StringComparer.Ordinal.Equals(
                           _key.Shader,
                           other._key.Shader);
            }

            internal bool HasSameMaterial(RowIdentity other)
            {
                return other != null
                       && !ReferenceEquals(_key.Material, null)
                       && ReferenceEquals(_key.Material, other._key.Material)
                       && ReferenceEquals(_key.Projector, other._key.Projector);
            }

            public bool Equals(RowIdentity other)
            {
                return other != null
                       && _occurrence == other._occurrence
                       && _key.Equals(other._key);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as RowIdentity);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return _key.GetHashCode() * 397 ^ _occurrence;
                }
            }
        }

        internal sealed class RowIdentityKey : IEquatable<RowIdentityKey>
        {
            internal RowIdentityKey(
                RowModel.RowItemType itemType,
                GameObject gameObject,
                Renderer renderer,
                Material material,
                Projector projector,
                string shader,
                string category,
                string propertyName,
                string descriptorId,
                string labelText)
            {
                ItemType = itemType;
                GameObject = gameObject;
                Renderer = renderer;
                Material = material;
                Projector = projector;
                Shader = shader;
                Category = category;
                PropertyName = propertyName;
                DescriptorId = descriptorId;
                LabelText = labelText;
            }

            private RowModel.RowItemType ItemType { get; }
            private GameObject GameObject { get; }
            private Renderer Renderer { get; }
            internal Material Material { get; }
            internal Projector Projector { get; }
            internal string Shader { get; }
            internal string Category { get; }
            private string PropertyName { get; }
            private string DescriptorId { get; }
            private string LabelText { get; }

            public bool Equals(RowIdentityKey other)
            {
                return other != null
                       && ItemType == other.ItemType
                       && ReferenceEquals(GameObject, other.GameObject)
                       && ReferenceEquals(Renderer, other.Renderer)
                       && ReferenceEquals(Material, other.Material)
                       && ReferenceEquals(Projector, other.Projector)
                       && StringComparer.Ordinal.Equals(Shader, other.Shader)
                       && StringComparer.Ordinal.Equals(Category, other.Category)
                       && StringComparer.Ordinal.Equals(PropertyName, other.PropertyName)
                       && StringComparer.Ordinal.Equals(DescriptorId, other.DescriptorId)
                       && StringComparer.Ordinal.Equals(LabelText, other.LabelText);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as RowIdentityKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (int)ItemType;
                    hash = hash * 397 ^ ReferenceHash(GameObject);
                    hash = hash * 397 ^ ReferenceHash(Renderer);
                    hash = hash * 397 ^ ReferenceHash(Material);
                    hash = hash * 397 ^ ReferenceHash(Projector);
                    hash = hash * 397 ^ StringHash(Shader);
                    hash = hash * 397 ^ StringHash(Category);
                    hash = hash * 397 ^ StringHash(PropertyName);
                    hash = hash * 397 ^ StringHash(DescriptorId);
                    hash = hash * 397 ^ StringHash(LabelText);
                    return hash;
                }
            }

            private static int ReferenceHash(object value)
            {
                return ReferenceEquals(value, null)
                    ? 0
                    : RuntimeHelpers.GetHashCode(value);
            }

            private static int StringHash(string value)
            {
                return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
            }
        }

        private void UpdateViewportAnchor(bool force, bool publish = true)
        {
            int next;
            if (_models.Count == 0)
            {
                next = -1;
            }
            else
            {
                var viewport = ScrollRect.viewport != null
                    ? ScrollRect.viewport
                    : ScrollRect.GetComponent<RectTransform>();
                var scrollPosition = Mathf.Max(0f, ScrollRect.content.localPosition.y);
                var anchorPosition = scrollPosition + viewport.rect.height * 0.3f;
                next = Mathf.Clamp(
                    Mathf.FloorToInt(anchorPosition / PanelHeight),
                    0,
                    _models.Count - 1);
            }

            SetViewportAnchorIndex(next, force, publish);
        }

        private void SetViewportAnchorIndex(
            int next,
            bool force,
            bool publish)
        {
            if (!force && next == _viewportAnchorIndex)
                return;

            _viewportAnchorIndex = next;
            if (publish)
                ViewportAnchorIndexChanged?.Invoke(next);
        }

        public void SelectFirstItem()
        {
            if (_activeViewCapacity > 0)
                _cachedViews[0].GetComponent<Button>().Select();
        }

        private float GetViewportHeight()
        {
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            return viewport.rect.height;
        }
    }
}
