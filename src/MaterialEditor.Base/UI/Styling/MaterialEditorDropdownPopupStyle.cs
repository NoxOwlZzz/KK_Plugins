using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    // Unity clones Dropdown.Template into a separate runtime hierarchy. The
    // clone can restore DefaultControls colors, so every dropdown reapplies the
    // shared popup style after Unity has finished creating its option items.
    internal sealed class MaterialEditorDropdownPopupStyle : MonoBehaviour
    {
        private static int _openPopupCount;

        [SerializeField] private Dropdown _dropdown;
        private bool _started;
        private bool _registeredOpen;

        internal static bool AnyPopupOpen => _openPopupCount > 0;

        internal void Configure(Dropdown dropdown)
        {
            _dropdown = dropdown;
            if (_started && isActiveAndEnabled)
                Apply();
        }

        private void OnEnable()
        {
            RegisterRuntimePopup();
            if (_started)
                Apply();
        }

        private void Start()
        {
            _started = true;
            RegisterRuntimePopup();
            Apply();
        }

        private void OnDisable()
        {
            UnregisterRuntimePopup();
        }

        private void OnDestroy()
        {
            UnregisterRuntimePopup();
        }

        private void RegisterRuntimePopup()
        {
            // uGUI renames the cloned template root before activating it. The
            // original disabled template remains named "Template" and is never
            // counted, while every Material Editor dropdown clone is tracked.
            if (_registeredOpen || gameObject.name != "Dropdown List")
                return;
            _registeredOpen = true;
            _openPopupCount++;
        }

        private void UnregisterRuntimePopup()
        {
            if (!_registeredOpen)
                return;
            _registeredOpen = false;
            if (_openPopupCount > 0)
                _openPopupCount--;
        }

        internal void ReapplyTheme()
        {
            Apply();
        }

        private void Apply()
        {
            MaterialEditorStyles.ApplyDropdownPopup(
                _dropdown,
                transform,
                null,
                null);
            FitRuntimePopupWidth();
        }

        private void FitRuntimePopupWidth()
        {
            if (gameObject.name != "Dropdown List" || _dropdown == null)
                return;

            var popupRect = transform as RectTransform;
            var dropdownRect = _dropdown.transform as RectTransform;
            var measurementText = _dropdown.itemText ?? _dropdown.captionText;
            if (popupRect == null || measurementText == null)
                return;

            Canvas.ForceUpdateCanvases();

            var preferredTextWidth = 0f;
            var options = _dropdown.options;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option == null)
                    continue;
                preferredTextWidth = Mathf.Max(
                    preferredTextWidth,
                    MaterialEditorTextFitting.MeasurePreferredWidth(
                        measurementText,
                        option.text,
                        MaterialEditorLayout.DropdownFontSize));
            }

            var currentWidth = dropdownRect != null
                               && dropdownRect.rect.width > 0f
                ? dropdownRect.rect.width
                : popupRect.rect.width;
            var desiredWidth = Mathf.Max(
                currentWidth,
                preferredTextWidth
                + MaterialEditorTheme.Metrics.DropdownPopupHorizontalPadding);
            desiredWidth = Mathf.Min(
                desiredWidth,
                MaterialEditorTheme.Metrics.DropdownPopupMaximumWidth);

            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (canvasRect != null)
            {
                var availableWidth = canvasRect.rect.width
                                     - MaterialEditorTheme.Metrics
                                         .DropdownPopupScreenMargin * 2f;
                if (availableWidth <= 0f)
                    return;
                desiredWidth = Mathf.Min(desiredWidth, availableWidth);
            }

            popupRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                desiredWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
            Canvas.ForceUpdateCanvases();

            if (canvasRect != null)
            {
                AlignRightEdgeToDropdown(
                    popupRect,
                    dropdownRect,
                    canvasRect);
                ClampHorizontallyToCanvas(popupRect, canvasRect);
            }

            RefreshAdaptiveItemText();
        }

        private static void ClampHorizontallyToCanvas(
            RectTransform popupRect,
            RectTransform canvasRect)
        {
            var bounds = GetHorizontalBoundsInCanvas(popupRect, canvasRect);
            var minimum = canvasRect.rect.xMin
                          + MaterialEditorTheme.Metrics.DropdownPopupScreenMargin;
            var maximum = canvasRect.rect.xMax
                          - MaterialEditorTheme.Metrics.DropdownPopupScreenMargin;
            var shift = bounds.x < minimum
                ? minimum - bounds.x
                : bounds.y > maximum
                    ? maximum - bounds.y
                    : 0f;
            if (Mathf.Approximately(shift, 0f) || popupRect.parent == null)
                return;

            MoveHorizontallyInCanvasUnits(popupRect, canvasRect, shift);
        }

        private static void AlignRightEdgeToDropdown(
            RectTransform popupRect,
            RectTransform dropdownRect,
            RectTransform canvasRect)
        {
            if (dropdownRect == null || popupRect.parent == null)
                return;

            var dropdownBounds =
                GetHorizontalBoundsInCanvas(dropdownRect, canvasRect);
            var popupBounds =
                GetHorizontalBoundsInCanvas(popupRect, canvasRect);
            var shift = dropdownBounds.y - popupBounds.y;
            if (Mathf.Approximately(shift, 0f))
                return;

            MoveHorizontallyInCanvasUnits(popupRect, canvasRect, shift);
            Canvas.ForceUpdateCanvases();
        }

        private void RefreshAdaptiveItemText()
        {
            var fitters =
                GetComponentsInChildren<MaterialEditorAdaptiveTextFitter>(true);
            for (var i = 0; i < fitters.Length; i++)
                fitters[i].RefreshNow();

            var popupRect = transform as RectTransform;
            if (popupRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
        }

        private static void MoveHorizontallyInCanvasUnits(
            RectTransform popupRect,
            RectTransform canvasRect,
            float shift)
        {
            if (popupRect == null
                || canvasRect == null
                || popupRect.parent == null)
                return;

            var worldShift = canvasRect.TransformVector(new Vector3(shift, 0f, 0f));
            var parentShift = popupRect.parent.InverseTransformVector(worldShift);
            popupRect.anchoredPosition +=
                new Vector2(parentShift.x, parentShift.y);
        }

        private static Vector2 GetHorizontalBoundsInCanvas(
            RectTransform rectTransform,
            RectTransform canvasRect)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            for (var i = 0; i < corners.Length; i++)
            {
                var canvasPoint = canvasRect.InverseTransformPoint(corners[i]);
                minimum = Mathf.Min(minimum, canvasPoint.x);
                maximum = Mathf.Max(maximum, canvasPoint.x);
            }

            return new Vector2(minimum, maximum);
        }
    }
}
