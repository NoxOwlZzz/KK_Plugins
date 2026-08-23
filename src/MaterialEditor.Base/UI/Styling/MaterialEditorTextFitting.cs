using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorTextFitting
    {
        private const float WidthSafetyMargin = 0.5f;
        private static readonly TextGenerator WidthGenerator =
            new TextGenerator();

        internal static void ApplyAdaptiveSingleLine(
            Text text,
            int maximumFontSize)
        {
            if (text == null)
                return;

            var fitter =
                text.GetComponent<MaterialEditorAdaptiveTextFitter>()
                ?? text.gameObject.AddComponent<MaterialEditorAdaptiveTextFitter>();
            fitter.Configure(text, maximumFontSize);
        }

        internal static float MeasurePreferredWidth(
            Text text,
            string value,
            int fontSize)
        {
            if (text == null || text.font == null)
                return 0f;

            var settings = text.GetGenerationSettings(Vector2.zero);
            settings.resizeTextForBestFit = false;
            settings.fontSize = Mathf.Max(1, fontSize);
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            return WidthGenerator.GetPreferredWidth(
                       value ?? string.Empty,
                       settings)
                   / text.pixelsPerUnit;
        }

        internal static int ResolveFontSize(
            Text text,
            string value,
            float availableWidth,
            int minimumFontSize,
            int maximumFontSize)
        {
            var safeMaximum = Mathf.Max(1, maximumFontSize);
            var safeMinimum = Mathf.Clamp(
                minimumFontSize,
                1,
                safeMaximum);
            var usableWidth = Mathf.Max(
                0f,
                availableWidth - WidthSafetyMargin);
            if (text == null || text.font == null || usableWidth <= 0f)
                return safeMinimum;

            var low = safeMinimum;
            var high = safeMaximum;
            var best = safeMinimum;
            while (low <= high)
            {
                var candidate = (low + high) / 2;
                if (MeasurePreferredWidth(text, value, candidate)
                    <= usableWidth)
                {
                    best = candidate;
                    low = candidate + 1;
                }
                else
                {
                    high = candidate - 1;
                }
            }

            return best;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class MaterialEditorAdaptiveTextFitter : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private int _maximumFontSize;
        private string _lastText;
        private float _lastWidth = -1f;
        private Font _lastFont;
        private FontStyle _lastFontStyle;
        private int _lastAppliedFontSize = -1;
        private bool _dirty = true;

        internal void Configure(Text text, int maximumFontSize)
        {
            _text = text;
            _maximumFontSize = Mathf.Max(1, maximumFontSize);
            _dirty = true;
            Refresh(true);
        }

        internal void RefreshNow()
        {
            _dirty = true;
            Refresh(true);
        }

        private void OnEnable()
        {
            _dirty = true;
            Refresh(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            _dirty = true;
            if (isActiveAndEnabled)
                Refresh(true);
        }

        private void LateUpdate()
        {
            Refresh(false);
        }

        private void Refresh(bool force)
        {
            if (_text == null)
                return;

            var value = _text.text ?? string.Empty;
            var width = Mathf.Max(0f, _text.rectTransform.rect.width);
            if (!force
                && !_dirty
                && string.Equals(value, _lastText, StringComparison.Ordinal)
                && Mathf.Approximately(width, _lastWidth)
                && _text.font == _lastFont
                && _text.fontStyle == _lastFontStyle
                && _text.fontSize == _lastAppliedFontSize
                && !_text.resizeTextForBestFit
                && _text.horizontalOverflow == HorizontalWrapMode.Wrap
                && _text.verticalOverflow == VerticalWrapMode.Overflow)
                return;

            var safeMaximum = Mathf.Max(1, _maximumFontSize);
            var safeMinimum = Mathf.Min(
                MaterialEditorTheme.Typography.AdaptiveMinimumFontSize,
                safeMaximum);
            var fontSize = width > 0f
                ? MaterialEditorTextFitting.ResolveFontSize(
                    _text,
                    value,
                    width,
                    safeMinimum,
                    safeMaximum)
                : safeMaximum;

            _text.resizeTextForBestFit = false;
            _text.resizeTextMinSize = safeMinimum;
            _text.resizeTextMaxSize = safeMaximum;
            _text.fontSize = fontSize;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            // The size is calculated for one line. Overflow avoids the Unity
            // 5.6 zero-glyph bug caused by Best Fit with vertical truncation.
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.SetVerticesDirty();

            _lastText = value;
            _lastWidth = width;
            _lastFont = _text.font;
            _lastFontStyle = _text.fontStyle;
            _lastAppliedFontSize = fontSize;
            _dirty = false;
        }
    }
}
