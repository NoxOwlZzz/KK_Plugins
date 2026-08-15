using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal class DropdownFilter : MonoBehaviour
    {
        internal const int PersistentFilterLimit = 16;

        private struct FilterItem
        {
            internal GameObject Root;
            internal string Label;
            internal bool AlwaysVisible;
            internal int OptionIndex;
        }

        [SerializeField] private Dropdown _parent;
        [SerializeField] private RectTransform _filterRect;
        [SerializeField] private InputField _filterField;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _clearButtonRect;
        [SerializeField] private Button _clearButton;
        [SerializeField] private string _persistentKeyword;

        private const float ClearButtonAnchorX = 0.16f;
        private static readonly Dictionary<string, string> PersistentFilterMap =
            new Dictionary<string, string>();
        private static readonly Queue<string> PersistentFilterOrder =
            new Queue<string>();

        private readonly List<FilterItem> _filterItems = new List<FilterItem>();
        private Scrollbar _scrollbar;
        private float _itemStride = 20f;
        private string _lastFilter;
        private bool _started;
        private bool _isOpen;
        private bool _listenersBound;

        /// <summary>Add filter UI to a dropdown template exactly once.</summary>
        public static void AddFilterUI(
            Dropdown target,
            string persistentKeyword = null)
        {
            if (target == null)
                return;

            var menuTemplate = target.transform.Find("Template");
            var content = target.transform.Find("Template/Viewport/Content")
                          as RectTransform;
            if (menuTemplate == null || content == null)
                return;

            var filter = menuTemplate.GetComponent<DropdownFilter>();
            if (filter != null)
            {
                filter._parent = target;
                filter._persistentKeyword = persistentKeyword;
                return;
            }

            filter = menuTemplate.gameObject.AddComponent<DropdownFilter>();
            filter._parent = target;
            filter._persistentKeyword = persistentKeyword;
            filter._content = content;

            var filterField = MaterialEditorControlFactory.CreateInputField(
                "Filter",
                menuTemplate.transform,
                "Filter");
            filter._filterField = filterField;

            const float uiMargin = 2f;
            var filterUiHeight = Mathf.Max(1f, GetItemSize(target)) + uiMargin;
            var templateRect = menuTemplate as RectTransform;
            if (templateRect != null)
            {
                templateRect.anchoredPosition = new Vector2(
                    0f,
                    -filterUiHeight + uiMargin + uiMargin);
            }

            var filterRect = (RectTransform)filterField.transform;
            filter._filterRect = filterRect;
            filterRect.anchorMin = new Vector2(ClearButtonAnchorX, 0f);
            filterRect.anchorMax = new Vector2(1f, 0f);
            filterRect.offsetMin = new Vector2(0f, -filterUiHeight);
            filterRect.offsetMax = Vector2.zero;
            filterRect.sizeDelta = new Vector2(0f, filterUiHeight);

            var clearButton = MaterialEditorControlFactory.CreateButton(
                "ClearFilter",
                menuTemplate.transform,
                "Clear");
            filter._clearButton = clearButton;
            var clearButtonRect = (RectTransform)clearButton.transform;
            filter._clearButtonRect = clearButtonRect;
            clearButtonRect.anchorMin = Vector2.zero;
            clearButtonRect.anchorMax = new Vector2(ClearButtonAnchorX, 0f);
            clearButtonRect.offsetMin = new Vector2(0f, -filterUiHeight);
            clearButtonRect.offsetMax = Vector2.zero;
            clearButtonRect.sizeDelta = new Vector2(0f, filterUiHeight);

            var contentLayoutGroup = content.GetComponent<VerticalLayoutGroup>()
                                     ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayoutGroup.childForceExpandWidth = true;
            contentLayoutGroup.childForceExpandHeight = false;
            contentLayoutGroup.childControlWidth = true;
            contentLayoutGroup.childControlHeight = false;

            var itemTemplate = content.Find("Item");
            if (itemTemplate != null
                && itemTemplate.GetComponent<LayoutElement>() == null)
            {
                itemTemplate.gameObject.AddComponent<LayoutElement>();
            }
        }

        private void OnEnable()
        {
            _isOpen = true;
            if (_started)
                ActivateRuntimePopup();
        }

        private void Start()
        {
            if (_filterRect == null || _filterField == null || _content == null)
                return; // Original disabled template, not an instantiated popup.

            _started = true;
            _isOpen = true;
            ActivateRuntimePopup();
        }

        private void OnDisable()
        {
            _isOpen = false;
            UnbindListeners();
            _lastFilter = null;
        }

        private void OnDestroy()
        {
            _isOpen = false;
            UnbindListeners();
        }

        private void ActivateRuntimePopup()
        {
            if (!_isOpen)
                return;

            SetUIPosition();
            if (_clearButton == null && _clearButtonRect != null)
                _clearButton = _clearButtonRect.GetComponent<Button>();
            if (_scrollbar == null)
                _scrollbar = GetComponentInChildren<Scrollbar>(true);
            if (_filterItems.Count == 0)
                BuildFilterItemCache();
            CalculateItemStride();
            MaterialEditorStyles.ApplyDropdownPopup(
                _parent,
                transform,
                _filterField,
                _clearButton);
            BindListeners();
            RestoreFilter();
            ApplyFilter(_filterField.text);
        }

        private void BindListeners()
        {
            if (_listenersBound || !_isOpen || _filterField == null)
                return;

            _filterField.onValueChanged.AddListener(OnChangeFilter);
            _filterField.onEndEdit.AddListener(OnEndEditFilter);
            if (_clearButton != null)
                _clearButton.onClick.AddListener(ClearFilter);
            _listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!_listenersBound)
                return;

            if (_filterField != null)
            {
                _filterField.onValueChanged.RemoveListener(OnChangeFilter);
                _filterField.onEndEdit.RemoveListener(OnEndEditFilter);
            }
            if (_clearButton != null)
                _clearButton.onClick.RemoveListener(ClearFilter);
            _listenersBound = false;
        }

        private void SaveFilter()
        {
            if (!_isOpen
                || string.IsNullOrEmpty(_persistentKeyword)
                || _filterField == null)
                return;

            if (!PersistentFilterMap.ContainsKey(_persistentKeyword))
            {
                while (PersistentFilterMap.Count >= PersistentFilterLimit
                       && PersistentFilterOrder.Count > 0)
                {
                    PersistentFilterMap.Remove(PersistentFilterOrder.Dequeue());
                }
                PersistentFilterOrder.Enqueue(_persistentKeyword);
            }
            PersistentFilterMap[_persistentKeyword] = _filterField.text;
        }

        private void RestoreFilter()
        {
            if (!_isOpen
                || string.IsNullOrEmpty(_persistentKeyword)
                || _filterField == null)
                return;

            string filter;
            if (PersistentFilterMap.TryGetValue(_persistentKeyword, out filter)
                && !string.Equals(_filterField.text, filter, StringComparison.Ordinal))
            {
                _filterField.text = filter;
            }
        }

        private void ClearFilter()
        {
            if (!_isOpen || _filterField == null)
                return;

            if (_filterField.text.Length == 0)
                ApplyFilter(string.Empty);
            else
                _filterField.text = string.Empty;
            SaveFilter();
        }

        private void OnEndEditFilter(string filter)
        {
            if (_isOpen)
                SaveFilter();
        }

        private void OnChangeFilter(string filter)
        {
            if (_isOpen)
                ApplyFilter(filter);
        }

        private void BuildFilterItemCache()
        {
            _filterItems.Clear();
            for (var index = 0; index < _content.childCount; index++)
            {
                var item = _content.GetChild(index);
                var itemName = item.name ?? string.Empty;
                var colon = itemName.IndexOf(':');
                if (colon < 0)
                    continue; // Skip the disabled item template itself.

                var label = itemName.Substring(colon + 1).Trim();
                var itemText = item.GetComponentInChildren<Text>(true);
                if (itemText != null && !string.IsNullOrEmpty(itemText.text))
                    label = itemText.text;

                _filterItems.Add(new FilterItem
                {
                    Root = item.gameObject,
                    Label = label,
                    OptionIndex = _filterItems.Count,
                    AlwaysVisible = string.Equals(
                        label,
                        "Reset",
                        StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        /// <summary>
        /// Maps a dropdown option index into the currently visible filtered
        /// sequence so one-shot autoscroll uses the same layout as the popup.
        /// </summary>
        internal bool TryGetVisibleOptionPosition(
            int optionIndex,
            out int visibleIndex,
            out int visibleCount)
        {
            visibleIndex = -1;
            visibleCount = 0;
            if (!_isOpen)
                return false;
            if (_filterItems.Count == 0 && _content != null)
                BuildFilterItemCache();

            for (var index = 0; index < _filterItems.Count; index++)
            {
                var item = _filterItems[index];
                if (item.Root == null || !item.Root.activeSelf)
                    continue;
                if (item.OptionIndex == optionIndex)
                    visibleIndex = visibleCount;
                visibleCount++;
            }
            return visibleIndex >= 0;
        }

        private void CalculateItemStride()
        {
            if (_filterItems.Count >= 2)
            {
                var first = _filterItems[0].Root.transform as RectTransform;
                var second = _filterItems[1].Root.transform as RectTransform;
                if (first != null && second != null)
                {
                    _itemStride = Mathf.Abs(first.offsetMin.y - second.offsetMin.y);
                }
            }
            if (_itemStride <= 0f)
                _itemStride = Mathf.Max(1f, GetItemSize(_parent));
        }

        private void ApplyFilter(string filter)
        {
            if (!_isOpen || _content == null)
                return;

            var pattern = string.IsNullOrEmpty(filter) ? "*" : filter;
            if (string.Equals(_lastFilter, pattern, StringComparison.Ordinal))
                return;
            _lastFilter = pattern;

            var activeItems = 0;
            var layoutChanged = false;
            for (var index = 0; index < _filterItems.Count; index++)
            {
                var item = _filterItems[index];
                var shown = item.AlwaysVisible
                            || WildcardMatch(item.Label, pattern);
                if (item.Root.activeSelf != shown)
                {
                    item.Root.SetActive(shown);
                    layoutChanged = true;
                }
                if (shown)
                    activeItems++;
            }

            var sizeDelta = _content.sizeDelta;
            var desiredHeight = Mathf.Max(1f, _itemStride) * activeItems + 8f;
            if (!Mathf.Approximately(sizeDelta.y, desiredHeight))
            {
                sizeDelta.y = desiredHeight;
                _content.sizeDelta = sizeDelta;
                layoutChanged = true;
            }

            if (layoutChanged && _scrollbar != null)
            {
                _scrollbar.value = 1f;
                _scrollbar.Rebuild(CanvasUpdate.Prelayout);
            }
        }

        private static bool WildcardMatch(string value, string pattern)
        {
            value = value ?? string.Empty;
            pattern = pattern ?? string.Empty;
            var valueIndex = 0;
            var patternIndex = 0;
            var starIndex = -1;
            var retryValueIndex = 0;
            var searchStart = 0;

            while (valueIndex < value.Length)
            {
                // Preserve Regex.IsMatch-style substring behavior without
                // constructing an implicit "*pattern*" string.
                if (patternIndex == pattern.Length)
                    return true;
                if (patternIndex < pattern.Length
                    && (pattern[patternIndex] == '?'
                        || char.ToUpperInvariant(pattern[patternIndex])
                        == char.ToUpperInvariant(value[valueIndex])))
                {
                    valueIndex++;
                    patternIndex++;
                }
                else if (patternIndex < pattern.Length
                         && pattern[patternIndex] == '*')
                {
                    starIndex = patternIndex++;
                    retryValueIndex = valueIndex;
                }
                else if (starIndex >= 0)
                {
                    patternIndex = starIndex + 1;
                    valueIndex = ++retryValueIndex;
                }
                else
                {
                    patternIndex = 0;
                    valueIndex = ++searchStart;
                }
            }

            while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
                patternIndex++;
            return patternIndex == pattern.Length;
        }

        /// <summary>
        /// Place the filter above or below the runtime popup to match the
        /// direction selected by uGUI.
        /// </summary>
        private void SetUIPosition()
        {
            if (_filterRect == null || _clearButtonRect == null)
                return;

            var height = _filterRect.sizeDelta.y;
            const float offset = 2f;
            var dropdownRect = transform as RectTransform;
            if (dropdownRect != null && dropdownRect.offsetMax.y > 0f)
            {
                _filterRect.anchorMin = new Vector2(ClearButtonAnchorX, 0f);
                _filterRect.anchorMax = new Vector2(1f, 0f);
                _filterRect.offsetMin = new Vector2(0f, -height + offset);
                _filterRect.offsetMax = new Vector2(0f, offset);

                _clearButtonRect.anchorMin = Vector2.zero;
                _clearButtonRect.anchorMax = new Vector2(ClearButtonAnchorX, 0f);
                _clearButtonRect.offsetMin = new Vector2(0f, -height + offset);
                _clearButtonRect.offsetMax = new Vector2(0f, offset);
            }
            else
            {
                _filterRect.anchorMin = new Vector2(ClearButtonAnchorX, 1f);
                _filterRect.anchorMax = Vector2.one;
                _filterRect.offsetMin = new Vector2(0f, -offset);
                _filterRect.offsetMax = new Vector2(0f, height - offset);

                _clearButtonRect.anchorMin = new Vector2(0f, 1f);
                _clearButtonRect.anchorMax = new Vector2(ClearButtonAnchorX, 1f);
                _clearButtonRect.offsetMin = new Vector2(0f, -offset);
                _clearButtonRect.offsetMax = new Vector2(0f, height - offset);
            }
        }

        private static float GetItemSize(Dropdown dropdown)
        {
            if (dropdown == null)
                return 20f;
            if (dropdown.itemText != null)
                return dropdown.itemText.rectTransform.rect.height;
            if (dropdown.itemImage != null)
                return dropdown.itemImage.rectTransform.rect.height;
            return 20f;
        }
    }
}
