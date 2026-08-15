using System;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    // One canvas-level action surface shared by every pooled renderer/material row.
    internal sealed class MaterialEditorRowActionMenu : MonoBehaviour
    {
        private readonly Button[] _buttons =
            new Button[RowActionMenuLease.MaximumActions];
        private readonly Text[] _labels =
            new Text[RowActionMenuLease.MaximumActions];
        private readonly Tooltip[] _tooltips =
            new Tooltip[RowActionMenuLease.MaximumActions];
        private readonly Vector3[] _anchorCorners = new Vector3[4];
        private readonly Vector3[] _rootCorners = new Vector3[4];
        private readonly Vector3[] _panelCorners = new Vector3[4];
        private readonly RowActionMenuLease _lease = new RowActionMenuLease();

        private Image _root;
        private Image _menuPanel;
        private RectTransform _anchor;
        private ScrollRect _scrollRect;
        private Action _beforeOpen;

        internal bool IsOpen => _lease.IsOpen;

        internal void Initialize(Transform popupParent, Action beforeOpen)
        {
            if (_root != null)
                return;

            _beforeOpen = beforeOpen;
            _root = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorRowActionMenu",
                popupParent);
            _root.transform.SetRect();
            _root.color = MaterialEditorTheme.Colors.TransparentRow;
            _root.raycastTarget = false;

            var dismissLayer = MaterialEditorControlFactory.CreateButton(
                "MaterialEditorRowActionMenuDismissLayer",
                _root.transform,
                string.Empty);
            dismissLayer.transform.SetRect();
            dismissLayer.image.color = MaterialEditorTheme.Colors.TransparentRow;
            dismissLayer.navigation = NoNavigation();
            dismissLayer.onClick.AddListener(Close);

            _menuPanel = MaterialEditorControlFactory.CreatePanel(
                "MaterialEditorRowActionMenuPanel",
                _root.transform,
                MaterialEditorPanelRole.Header);
            _menuPanel.raycastTarget = false;
            _menuPanel.rectTransform.anchorMin = Vector2.zero;
            _menuPanel.rectTransform.anchorMax = Vector2.zero;
            _menuPanel.rectTransform.pivot = new Vector2(1f, 1f);
            _menuPanel.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                MaterialEditorTheme.Metrics.ButtonWidth * 2f);
            _menuPanel.gameObject
                .AddComponent<RowActionMenuCancelForwarder>()
                .Initialize(this);

            for (var index = 0; index < _buttons.Length; index++)
                _buttons[index] = CreateActionButton(index);
            _buttons[0].onClick.AddListener(Invoke0);
            _buttons[1].onClick.AddListener(Invoke1);
            _buttons[2].onClick.AddListener(Invoke2);
            _buttons[3].onClick.AddListener(Invoke3);

            MaterialEditorStyles.ApplyTypography(_menuPanel.gameObject);
            _root.gameObject.SetActive(false);
            enabled = false;
        }

        internal void BindScrollRect(ScrollRect scrollRect)
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
            _scrollRect = scrollRect;
            if (_scrollRect != null)
                _scrollRect.onValueChanged.AddListener(HandleScrollChanged);
        }

        internal void OpenRenderer(
            RowBinder owner,
            int generation,
            RectTransform anchor,
            Action exportUv,
            Action exportObj)
        {
            Begin(owner, generation, anchor);
            AddAction(
                "Export UV Map",
                exportUv,
                true,
                "Export the UV map of this renderer.\n\nThe UV map is the 2d projection of the renderer with which to map textures to the 3d model. You can use this UV map as a guide to drawing on textures");
            AddAction(
                "Export .obj",
                exportObj,
                true,
                "Export the renderer as a .obj.\n\nYou can use the <i>ExportBakedMesh</i> and <i>ExportBakedWorldPosition</i> config options to change the exporting behaviour");
            Show();
        }

        internal void OpenMaterial(
            RowBinder owner,
            int generation,
            RectTransform anchor,
            Action copyOrRemove,
            string copyOrRemoveLabel,
            Action rename)
        {
            Begin(owner, generation, anchor);
            AddAction(
                copyOrRemoveLabel,
                copyOrRemove,
                true,
                "Make a copy of this material.\n\nUseful for overlaying different effects onto an object with different material shaders/properties");
            AddAction(
                "Rename on...",
                rename,
                true,
                "Rename material instances");
            Show();
        }

        internal void CloseIfOwner(RowBinder owner)
        {
            if (!_lease.IsOpen || !ReferenceEquals(_lease.Owner, owner))
                return;
            Close(false);
        }

        internal void Close()
        {
            Close(true);
        }

        private void Close(bool restoreFocus)
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null
                ? eventSystem.currentSelectedGameObject
                : null;
            var selectedFromMenu = selected != null
                                   && _root != null
                                   && (selected.transform == _root.transform
                                       || selected.transform.IsChildOf(
                                           _root.transform));
            var focusTarget = restoreFocus
                              && _anchor != null
                              && _anchor.gameObject.activeInHierarchy
                ? _anchor.gameObject
                : null;

            _lease.Close();
            _anchor = null;
            for (var index = 0; index < _buttons.Length; index++)
            {
                if (_buttons[index] == null)
                    continue;
                _buttons[index].interactable = false;
                _buttons[index].gameObject.SetActive(false);
                if (_labels[index] != null)
                {
                    _labels[index].text = string.Empty;
                    _labels[index].color =
                        MaterialEditorTheme.Colors.PrimaryText;
                }
                if (_tooltips[index] != null)
                    _tooltips[index].SetStandardTooltipText(null);
            }
            if (_root != null)
                _root.gameObject.SetActive(false);
            enabled = false;
            if (selectedFromMenu && eventSystem != null)
                eventSystem.SetSelectedGameObject(focusTarget);
        }

        private void Begin(
            RowBinder owner,
            int generation,
            RectTransform anchor)
        {
            Close(false);
            if (owner == null || anchor == null)
                return;

            _beforeOpen?.Invoke();
            _anchor = anchor;
            _lease.Begin(owner, generation);
        }

        private void AddAction(
            string label,
            Action action,
            bool interactable,
            string tooltip)
        {
            var index = _lease.Add(action);
            if (index < 0)
                return;

            var button = _buttons[index];
            _labels[index].text = label;
            _labels[index].color = interactable
                ? MaterialEditorTheme.Colors.PrimaryText
                : MaterialEditorTheme.Colors.DisabledText;
            button.interactable = interactable;
            button.gameObject.SetActive(true);
            _tooltips[index].SetStandardTooltipText(tooltip);
        }

        private void Show()
        {
            if (!_lease.IsOpen || _lease.Count == 0 || _anchor == null)
            {
                Close();
                return;
            }

            _menuPanel.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                MaterialEditorTheme.Metrics.HeaderHeight * _lease.Count
                + MaterialEditorTheme.Spacing.Control * 2f);
            _root.transform.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            PositionAtAnchor();
            enabled = true;
            SelectMenuSurface();
        }

        private Button CreateActionButton(int row)
        {
            var button = MaterialEditorControlFactory.CreateButton(
                "MaterialEditorRowAction" + row,
                _menuPanel.transform,
                string.Empty);
            button.navigation = NoNavigation();
            var top = -MaterialEditorTheme.Spacing.Control
                      - row * MaterialEditorTheme.Metrics.HeaderHeight;
            button.transform.SetRect(
                0f,
                1f,
                1f,
                1f,
                MaterialEditorTheme.Spacing.Control,
                top - MaterialEditorTheme.Metrics.HeaderHeight,
                -MaterialEditorTheme.Spacing.Control,
                top);
            _labels[row] = button.GetComponentInChildren<Text>(true);
            button.gameObject
                .AddComponent<RowActionMenuCancelForwarder>()
                .Initialize(this);
            button.interactable = false;
            button.gameObject.SetActive(false);
            _tooltips[row] = TooltipManager.AddTooltip(
                button.gameObject,
                string.Empty);
            return button;
        }

        private void SelectMenuSurface()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    _menuPanel.gameObject);
            }
        }

        internal void SelectFirstInteractableButton()
        {
            if (EventSystem.current == null)
                return;
            for (var index = 0; index < _lease.Count; index++)
            {
                if (!_buttons[index].interactable)
                    continue;
                EventSystem.current.SetSelectedGameObject(
                    _buttons[index].gameObject);
                return;
            }
            EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);
        }

        internal void MoveSelection(
            GameObject current,
            MoveDirection direction)
        {
            if (EventSystem.current == null || _lease.Count == 0)
                return;

            var step = direction == MoveDirection.Left
                       || direction == MoveDirection.Up
                ? -1
                : 1;
            var start = step > 0 ? -1 : _lease.Count;
            for (var index = 0; index < _lease.Count; index++)
            {
                if (_buttons[index].gameObject == current)
                {
                    start = index;
                    break;
                }
            }

            for (var offset = 1; offset <= _lease.Count; offset++)
            {
                var candidate = (start + step * offset) % _lease.Count;
                if (candidate < 0)
                    candidate += _lease.Count;
                if (!_buttons[candidate].gameObject.activeSelf
                    || !_buttons[candidate].interactable)
                    continue;
                EventSystem.current.SetSelectedGameObject(
                    _buttons[candidate].gameObject);
                return;
            }
        }

        internal bool IsMenuSurface(GameObject target)
        {
            return _menuPanel != null && _menuPanel.gameObject == target;
        }

        private void PositionAtAnchor()
        {
            _anchor.GetWorldCorners(_anchorCorners);
            _menuPanel.rectTransform.position = _anchorCorners[3];

            _root.rectTransform.GetWorldCorners(_rootCorners);
            _menuPanel.rectTransform.GetWorldCorners(_panelCorners);
            var position = _menuPanel.rectTransform.position;
            if (_panelCorners[0].x < _rootCorners[0].x)
                position.x += _rootCorners[0].x - _panelCorners[0].x;
            if (_panelCorners[2].x > _rootCorners[2].x)
                position.x -= _panelCorners[2].x - _rootCorners[2].x;
            if (_panelCorners[0].y < _rootCorners[0].y)
                position.y += _rootCorners[0].y - _panelCorners[0].y;
            if (_panelCorners[2].y > _rootCorners[2].y)
                position.y -= _panelCorners[2].y - _rootCorners[2].y;
            _menuPanel.rectTransform.position = position;
        }

        private void Invoke(int index)
        {
            var owner = _lease.Owner as RowBinder;
            var generation = _lease.Generation;
            var ownerActive = owner != null
                              && owner.IsActionMenuBindingValid(generation);
            var actionEnabled = index >= 0
                                && index < _buttons.Length
                                && _buttons[index].gameObject.activeSelf
                                && _buttons[index].interactable;
            var action = _lease.Take(
                index,
                owner,
                generation,
                ownerActive,
                actionEnabled);

            // Close the visual surface after Take has already nulled the lease.
            Close(false);
            if (action != null)
                action();
        }

        private void Invoke0() => Invoke(0);
        private void Invoke1() => Invoke(1);
        private void Invoke2() => Invoke(2);
        private void Invoke3() => Invoke(3);

        private void HandleScrollChanged(Vector2 position)
        {
            if (_lease.IsOpen)
                Close(false);
        }

        private static Navigation NoNavigation()
        {
            return new Navigation
            {
                mode = Navigation.Mode.None
            };
        }

        private void OnDisable()
        {
            if (_lease.IsOpen)
                Close(false);
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
            _scrollRect = null;
            _beforeOpen = null;
            Close(false);
        }
    }

    // The neutral menu surface receives initial focus so no action looks chosen
    // before the user navigates. The same component on each permanent button
    // preserves Escape and directional keyboard navigation without per-open
    // wiring.
    internal sealed class RowActionMenuCancelForwarder : MonoBehaviour,
        ICancelHandler,
        IPointerDownHandler,
        IMoveHandler,
        ISubmitHandler
    {
        private MaterialEditorRowActionMenu _owner;

        internal void Initialize(MaterialEditorRowActionMenu owner)
        {
            _owner = owner;
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (_owner == null)
                return;
            _owner.Close();
            if (eventData != null)
                eventData.Use();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_owner != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnMove(AxisEventData eventData)
        {
            if (_owner == null || eventData == null)
                return;
            _owner.MoveSelection(gameObject, eventData.moveDir);
            eventData.Use();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_owner == null || !_owner.IsMenuSurface(gameObject))
                return;
            _owner.SelectFirstInteractableButton();
            if (eventData != null)
                eventData.Use();
        }
    }
}
