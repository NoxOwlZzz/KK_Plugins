using System;
using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class CategoryNavigatorView
    {
        private readonly Action<CategoryNavigationTarget> _navigate;
        private readonly Action<CategoryNavigationTarget> _toggle;
        private readonly Action<bool> _expandedChanged;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Text _materialText;
        private readonly Text _shaderText;
        private readonly ScrollRect _scrollRect;
        private readonly RectTransform _centralScrollContent;
        private readonly Button _expandButton;
        private MaterialEditorPresentation _presentation;
        private Entry _activeEntry;
        private string _sectionId;
        private int _viewportAnchor = -1;
        private bool _viewportAnchorProgrammatic;
        private string _pendingClickedStableKey;
        private string _pendingNavigationKey;
        private bool _deferredPresentationRebuild;
        private bool _expanded;

        internal CategoryNavigatorView(
            Transform parent,
            RectTransform centralScrollContent,
            Action<CategoryNavigationTarget> navigate,
            Action<CategoryNavigationTarget> toggle,
            Action<bool> expandedChanged)
        {
            _navigate = navigate;
            _toggle = toggle;
            _expandedChanged = expandedChanged;
            _centralScrollContent = centralScrollContent;

            Panel = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigatorPanel",
                parent,
                MaterialEditorPanelRole.LeftPanel);
            UIUtility.AddOutlineToObject(
                Panel.transform,
                MaterialEditorTheme.Colors.Outline);

            var header = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigatorHeader",
                Panel.transform,
                MaterialEditorPanelRole.Header);
            header.transform.SetRect(
                0f, 1f, 1f, 1f,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight,
                -MaterialEditorLayout.Margin,
                0f);

            var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(
                MaterialEditorTheme.Spacing.PropertyLabelInset,
                0,
                0,
                0);
            headerLayout.spacing = MaterialEditorTheme.Spacing.Control;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandHeight = true;

            var title = MaterialEditorControlFactory.CreateText(
                "CategoryNavigatorTitle",
                header.transform,
                "Categories",
                MaterialEditorTextRole.Label);
            title.color = MaterialEditorTheme.Colors.PrimaryText;
            title.fontStyle = FontStyle.Bold;
            var titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.minWidth = 0f;
            titleLayout.preferredWidth = 0f;
            titleLayout.flexibleWidth = 1f;

            var panelCollapse = MaterialEditorControlFactory.CreateButton(
                "CategoryNavigatorPanelCollapse",
                header.transform,
                MaterialEditorTheme.Glyphs.ChevronLeft);
            var panelCollapseLayout = panelCollapse.gameObject.AddComponent<LayoutElement>();
            panelCollapseLayout.minWidth = MaterialEditorLayout.SmallButtonWidth;
            panelCollapseLayout.preferredWidth = MaterialEditorLayout.SmallButtonWidth;
            panelCollapseLayout.flexibleWidth = 0f;
            TooltipManager.AddTooltip(
                panelCollapse.gameObject,
                "Collapse categories panel");
            panelCollapse.onClick.AddListener(() => SetExpanded(false));

            _materialText = MaterialEditorControlFactory.CreateText(
                "CategoryNavigatorMaterial",
                Panel.transform,
                string.Empty,
                MaterialEditorTextRole.Label);
            ConfigureSingleLineText(_materialText);
            _materialText.transform.SetRect(
                0f, 1f, 1f, 1f,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight * 2f,
                -MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight);

            var shaderHeader = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigatorShaderHeader",
                Panel.transform);
            shaderHeader.color = MaterialEditorStyles.NavigatorShaderHeaderColor;
            shaderHeader.transform.SetRect(
                0f, 1f, 1f, 1f,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight * 3f,
                -MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight * 2f);

            _shaderText = MaterialEditorControlFactory.CreateText(
                "CategoryNavigatorShader",
                shaderHeader.transform,
                string.Empty,
                MaterialEditorTextRole.Label);
            _shaderText.color = MaterialEditorTheme.Colors.SecondaryText;
            ConfigureSingleLineText(_shaderText);
            _shaderText.transform.SetRect();

            _scrollRect = MaterialEditorControlFactory.CreateScrollView(
                "CategoryNavigatorScrollView",
                Panel.transform);
            _scrollRect.transform.SetRect(
                0f, 0f, 1f, 1f,
                MaterialEditorLayout.Margin,
                MaterialEditorLayout.Margin,
                -MaterialEditorLayout.Margin,
                -MaterialEditorLayout.HeaderHeight * 3f);
            _scrollRect.gameObject.AddComponent<Mask>();
            _scrollRect.verticalScrollbar.GetComponent<RectTransform>().offsetMin =
                new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            _scrollRect.viewport.offsetMax =
                new Vector2(MaterialEditorLayout.ScrollbarOffset, 0f);
            var layout = _scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                MaterialEditorTheme.Spacing.NavigatorContentPadding,
                MaterialEditorTheme.Spacing.NavigatorContentPadding,
                MaterialEditorTheme.Spacing.NavigatorContentPadding,
                MaterialEditorTheme.Spacing.NavigatorContentPadding);
            layout.spacing = MaterialEditorTheme.Spacing.NavigatorListSpacing;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            _scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            _expandButton = MaterialEditorControlFactory.CreateButton(
                "CategoryNavigatorExpand",
                parent,
                MaterialEditorTheme.Glyphs.ChevronRight);
            _expandButton.transform.SetRect(
                0f, 1f, 0f, 1f,
                -MaterialEditorTheme.Metrics.CategoryNavigatorCollapsedWidth,
                -MaterialEditorLayout.HeaderHeight,
                0f,
                0f);
            TooltipManager.AddTooltip(
                _expandButton.gameObject,
                "Expand categories panel");
            _expandButton.onClick.AddListener(() => SetExpanded(true));

            ApplySettings();
        }

        internal Image Panel { get; }
        internal bool Expanded => _expanded;

        internal void ApplySettings()
        {
            Panel.transform.SetRect(
                0f, 0f, 0f, 1f,
                -MaterialEditorLayout.CategoryNavigatorWidth - MaterialEditorLayout.Margin,
                0f,
                -MaterialEditorLayout.Margin,
                0f);
            UpdateVisibility();
        }

        internal void SetPresentation(
            MaterialEditorPresentation presentation,
            bool deferViewportAnchor = false)
        {
            _presentation = presentation;
            if (deferViewportAnchor)
            {
                _deferredPresentationRebuild = true;
                return;
            }
            _deferredPresentationRebuild = false;
            ApplyViewportAnchor(
                _viewportAnchor,
                true,
                _viewportAnchorProgrammatic);
        }

        internal void ReleasePresentation()
        {
            _presentation = null;
            _sectionId = null;
            _deferredPresentationRebuild = false;
            ClearPendingNavigationDiagnostic();
            _materialText.text = string.Empty;
            _shaderText.text = string.Empty;
            TooltipBinding.Bind(_materialText.gameObject, null, null);
            TooltipBinding.Bind(_shaderText.gameObject, null, null);
            ReleaseEntries();
            UpdateVisibility();
        }

        internal void SetViewportAnchor(int rowIndex)
        {
            SetViewportAnchor(rowIndex, false);
        }

        internal void SetViewportAnchor(int rowIndex, bool programmatic)
        {
            _viewportAnchorProgrammatic = programmatic;
            var forceRebuild = _deferredPresentationRebuild;
            _deferredPresentationRebuild = false;
            ApplyViewportAnchor(rowIndex, forceRebuild, programmatic);
        }

        internal bool ToggleExpanded()
        {
            SetExpanded(!_expanded);
            return _expanded;
        }

        private void SetExpanded(bool expanded)
        {
            if (_expanded == expanded)
                return;

            _expanded = expanded;
            UpdateVisibility();
            _expandedChanged?.Invoke(_expanded);
        }

        private void ApplyViewportAnchor(
            int rowIndex,
            bool forceRebuild,
            bool programmatic)
        {
            _viewportAnchor = rowIndex;
            var section = _presentation?.FindSectionAtRow(rowIndex);
            if (section == null || section.Categories.Count == 0)
            {
                var hadSection = _sectionId != null;
                _sectionId = null;
                if (hadSection)
                {
                    _materialText.text = string.Empty;
                    _shaderText.text = string.Empty;
                    TooltipBinding.Bind(_materialText.gameObject, null, null);
                    TooltipBinding.Bind(_shaderText.gameObject, null, null);
                    ReleaseEntries();
                }
                UpdateVisibility();
                return;
            }

            UpdateVisibility();
            if (forceRebuild || section.Id != _sectionId)
                Rebuild(section);
            UpdateHighlight(
                section.FindCategoryAtRow(rowIndex),
                programmatic);
        }

        private void UpdateVisibility()
        {
            Panel.gameObject.SetActive(_expanded);
            _expandButton.gameObject.SetActive(!_expanded);
        }

        private void Rebuild(MaterialSectionPresentation section)
        {
            _sectionId = section.Id;
            _materialText.text = section.MaterialName;
            _shaderText.text = section.ShaderName;
            TooltipBinding.Bind(
                _materialText.gameObject,
                null,
                section.MaterialName);
            TooltipBinding.Bind(
                _shaderText.gameObject,
                null,
                section.ShaderName);

            _activeEntry = null;
            while (_entries.Count < section.Categories.Count)
                _entries.Add(CreateEntry());

            var index = 0;
            foreach (var target in section.Categories)
                BindEntry(_entries[index++], target);
            while (index < _entries.Count)
                BindEntry(_entries[index++], null);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
        }

        private void ReleaseEntries()
        {
            _activeEntry = null;
            foreach (var entry in _entries)
                BindEntry(entry, null);
        }

        private Entry CreateEntry()
        {
            var root = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigationEntry",
                _scrollRect.content,
                MaterialEditorPanelRole.TransparentRow);
            root.raycastTarget = true;
            var rootButton = root.gameObject.AddComponent<Button>();
            rootButton.targetGraphic = root;
            rootButton.transition = Selectable.Transition.None;
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(
                MaterialEditorTheme.Spacing.CategoryEntryPadding,
                MaterialEditorTheme.Spacing.CategoryEntryPadding,
                MaterialEditorTheme.Spacing.CategoryEntryPadding,
                MaterialEditorTheme.Spacing.CategoryEntryPadding);
            layout.spacing = MaterialEditorTheme.Spacing.CategoryEntrySpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            var rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.minHeight = MaterialEditorLayout.RowHeight;
            rootLayout.preferredHeight = MaterialEditorLayout.RowHeight;

            var activeMarker = MaterialEditorControlFactory.CreatePanel(
                "CategoryNavigationActiveMarker",
                root.transform,
                MaterialEditorPanelRole.Default);
            activeMarker.color = MaterialEditorTheme.Colors.Accent;
            activeMarker.raycastTarget = false;
            var activeMarkerLayout = activeMarker.gameObject.AddComponent<LayoutElement>();
            activeMarkerLayout.minWidth = MaterialEditorTheme.Metrics.CategoryActiveMarkerWidth;
            activeMarkerLayout.preferredWidth = MaterialEditorTheme.Metrics.CategoryActiveMarkerWidth;
            activeMarkerLayout.flexibleWidth = 0f;
            activeMarker.enabled = false;

            var collapse = MaterialEditorControlFactory.CreateButton(
                "CategoryNavigationCollapse",
                root.transform,
                FoldGlyphs.Expanded);
            var collapseLayout = collapse.gameObject.AddComponent<LayoutElement>();
            collapseLayout.minWidth = MaterialEditorLayout.SmallButtonWidth;
            collapseLayout.preferredWidth = MaterialEditorLayout.SmallButtonWidth;
            collapseLayout.flexibleWidth = 0f;
            TooltipManager.AddTooltip(
                collapse.gameObject,
                "Expand or collapse this property category");
            var collapseText = collapse.GetComponentInChildren<Text>();
            collapseText.raycastTarget = false;

            var navigate = MaterialEditorControlFactory.CreateButton(
                "CategoryNavigationButton",
                root.transform,
                string.Empty);
            MaterialEditorStyles.ApplyCategoryNavigationButton(navigate);
            var navigateText = navigate.GetComponentInChildren<Text>();
            navigateText.raycastTarget = false;
            ConfigureSingleLineText(navigateText);
            var navigateLayout = navigate.gameObject.AddComponent<LayoutElement>();
            navigateLayout.minWidth = 0f;
            navigateLayout.preferredWidth = 0f;
            navigateLayout.flexibleWidth = 1f;
            var navigatorEntryIndex = _entries.Count;
            Entry entry = null;
            var binding = new CategoryNavigationEntryBinding(
                target => NavigateToEntry(entry, target),
                target => ToggleEntry(entry, target));
            entry = new Entry(
                root,
                activeMarker,
                rootButton,
                collapse,
                collapseText,
                navigate,
                navigateText,
                binding,
                navigatorEntryIndex,
                navigatorEntryIndex + 1);
            collapse.onClick.AddListener(binding.InvokeToggle);
            navigate.onClick.AddListener(binding.InvokeNavigate);
            rootButton.onClick.AddListener(binding.InvokeNavigate);
            root.gameObject.SetActive(false);
            return entry;
        }

        private void BindEntry(Entry entry, CategoryNavigationTarget target)
        {
            if (target == null)
            {
                if (entry.Target == null && !entry.Root.gameObject.activeSelf)
                    return;

                entry.Binding.Bind(null);
                entry.CollapseButton.gameObject.SetActive(false);
                entry.CollapseLabel.text = FoldGlyphs.Expanded;
                entry.Label.text = string.Empty;
                TooltipBinding.Bind(entry.NavigateButton.gameObject, null, null);
                SetEntryActive(entry, false);
                entry.Root.gameObject.SetActive(false);
                return;
            }

            entry.Binding.Bind(target);
            entry.CollapseButton.gameObject.SetActive(
                target.CanCollapse
                && (_presentation == null || !_presentation.HasPropertyFilter));
            entry.CollapseLabel.text = target.Collapsed
                ? FoldGlyphs.Collapsed
                : FoldGlyphs.Expanded;
            entry.Label.text = target.Name;
            TooltipBinding.Bind(
                entry.NavigateButton.gameObject,
                target.TooltipText,
                target.Name);
            SetEntryActive(entry, false);
            entry.Root.gameObject.SetActive(true);
        }

        private void ToggleEntry(
            Entry entry,
            CategoryNavigationTarget target)
        {
            SetPendingNavigationDiagnostic(target);
            LogEntryDiagnostic("toggle", entry, target, false);
            _toggle(target);
        }

        private void NavigateToEntry(
            Entry entry,
            CategoryNavigationTarget target)
        {
            SetPendingNavigationDiagnostic(target);
            LogEntryDiagnostic("navigate", entry, target, false);
            _navigate(target);
        }

        private void UpdateHighlight(
            CategoryNavigationTarget active,
            bool programmatic)
        {
            if (_activeEntry != null
                && active != null
                && _activeEntry.Target.Id == active.Id)
            {
                if (programmatic
                    && _pendingNavigationKey == active.Id)
                    LogHighlightDiagnostic(
                        _activeEntry,
                        active,
                        programmatic);
                return;
            }

            SetEntryActive(_activeEntry, false);
            _activeEntry = null;

            if (active == null)
                return;

            foreach (var entry in _entries)
            {
                if (entry.Target == null || entry.Target.Id != active.Id)
                    continue;

                _activeEntry = entry;
                SetEntryActive(_activeEntry, true);
                if (_pendingNavigationKey == null
                    || (programmatic
                        && _pendingNavigationKey == active.Id))
                {
                    LogHighlightDiagnostic(
                        _activeEntry,
                        active,
                        programmatic);
                }
                return;
            }
        }

        internal void CompleteNavigationDiagnostic(string categoryId)
        {
            if (_pendingNavigationKey == categoryId)
                ClearPendingNavigationDiagnostic();
        }

        private void SetPendingNavigationDiagnostic(
            CategoryNavigationTarget target)
        {
            if (!CategoryInteractionDiagnostics.Enabled)
                return;

            _pendingClickedStableKey = target.Id;
            _pendingNavigationKey = target.Id;
        }

        private void ClearPendingNavigationDiagnostic()
        {
            _pendingClickedStableKey = null;
            _pendingNavigationKey = null;
        }

        private void LogEntryDiagnostic(
            string phase,
            Entry entry,
            CategoryNavigationTarget target,
            bool programmatic)
        {
            if (!CategoryInteractionDiagnostics.Enabled)
                return;

            CategoryInteractionDiagnostics.Log(
                phase,
                target.RowIndex,
                entry.NavigatorEntryIndex,
                target.Id,
                entry.ListenerKey,
                target.Id,
                target.Id,
                _activeEntry?.Target?.Id,
                entry.Root.rectTransform,
                _scrollRect.content,
                _centralScrollContent,
                programmatic);
        }

        private void LogHighlightDiagnostic(
            Entry entry,
            CategoryNavigationTarget target,
            bool programmatic)
        {
            if (!CategoryInteractionDiagnostics.Enabled)
                return;

            CategoryInteractionDiagnostics.Log(
                "highlight",
                target.RowIndex,
                entry.NavigatorEntryIndex,
                target.Id,
                entry.ListenerKey,
                _pendingClickedStableKey,
                _pendingNavigationKey,
                target.Id,
                entry.Root.rectTransform,
                _scrollRect.content,
                _centralScrollContent,
                programmatic);
            ClearPendingNavigationDiagnostic();
        }

        private static void SetEntryActive(Entry entry, bool active)
        {
            if (entry == null)
                return;

            entry.ActiveMarker.enabled = active;
            MaterialEditorStyles.SetCategoryNavigationSelected(
                entry.NavigateButton,
                active);
            entry.Label.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            entry.Label.color = active
                ? MaterialEditorTheme.Colors.SelectedText
                : MaterialEditorTheme.Colors.SecondaryText;
        }

        private static void ConfigureSingleLineText(Text text)
        {
            if (text == null)
                return;

            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = TextAnchor.MiddleLeft;
        }

        private sealed class Entry
        {
            internal Entry(
                Image root,
                Image activeMarker,
                Button rootButton,
                Button collapseButton,
                Text collapseLabel,
                Button navigateButton,
                Text label,
                CategoryNavigationEntryBinding binding,
                int navigatorEntryIndex,
                int listenerKey)
            {
                Root = root;
                ActiveMarker = activeMarker;
                RootButton = rootButton;
                CollapseButton = collapseButton;
                CollapseLabel = collapseLabel;
                NavigateButton = navigateButton;
                Label = label;
                Binding = binding;
                NavigatorEntryIndex = navigatorEntryIndex;
                ListenerKey = listenerKey;
            }

            internal Image Root { get; }
            internal Image ActiveMarker { get; }
            internal Button RootButton { get; }
            internal Button CollapseButton { get; }
            internal Text CollapseLabel { get; }
            internal Button NavigateButton { get; }
            internal Text Label { get; }
            internal CategoryNavigationEntryBinding Binding { get; }
            internal int NavigatorEntryIndex { get; }
            internal int ListenerKey { get; }
            internal CategoryNavigationTarget Target => Binding.Target;
        }
    }
}
