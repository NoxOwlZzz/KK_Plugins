using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class TimelineColumnBinding
    {
        internal static bool Bind(
            Button button,
            ListenerScope listeners,
            Action selectInterpolable)
        {
            if (button == null)
                return false;

            var visible = selectInterpolable != null && IsTimelineAvailable();
            var buttonObject = button.gameObject;
            if (!buttonObject.activeSelf)
                buttonObject.SetActive(true);

            // Timeline owns a stable historical column. Incompatible rows keep
            // the 20 px slot in layout, but expose no glyph or hit target.
            var visibility = button.GetComponent<CanvasGroup>()
                             ?? buttonObject.AddComponent<CanvasGroup>();
            visibility.alpha = visible
                ? MaterialEditorTheme.States.VisibleAlpha
                : MaterialEditorTheme.States.HiddenAlpha;
            visibility.interactable = visible;
            visibility.blocksRaycasts = visible;
            MaterialEditorStyles.SetControlAvailability(
                button,
                visible,
                MaterialEditorControlAvailabilityMode.TimelineSlot);

            var layout = button.GetComponent<RowColumnLayoutOverride>()
                         ?? buttonObject.AddComponent<RowColumnLayoutOverride>();
            layout.Configure(RowColumnSpec.Fixed(
                buttonObject.name,
                RowColumnRole.Timeline,
                MaterialEditorLayout.InterpolableButtonWidth));

            var parent = button.transform.parent as RectTransform;
            if (parent != null)
                LayoutRebuilder.MarkLayoutForRebuild(parent);

            if (visible)
                listeners.Listen(button, () => selectInterpolable());
            return visible;
        }

        private static bool IsTimelineAvailable()
        {
#if API || EC
            return false;
#else
            return TimelineCompatibilityHelper.IsTimelineAvailable();
#endif
        }
    }

    internal enum RowColumnRole
    {
        Label,
        Timeline,
        Editor,
        Reset,
        Auxiliary
    }

    internal sealed class RowColumnLayoutOverride : MonoBehaviour, ILayoutElement
    {
        [SerializeField] private RowColumnRole _role;
        [SerializeField] private float _minWidth;
        [SerializeField] private float _preferredWidth;
        [SerializeField] private float _flexibleWidth;

        internal RowColumnRole Role => _role;

        internal void Configure(RowColumnSpec spec)
        {
            _role = spec.Role;
            _minWidth = spec.MinWidth;
            _preferredWidth = spec.PreferredWidth;
            _flexibleWidth = spec.FlexibleWidth;
            RestoreLayout();
        }

        internal void RestoreLayout()
        {
            var rect = transform as RectTransform;
            if (rect != null && _flexibleWidth <= 0f && _preferredWidth >= 0f)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _preferredWidth);

            var parentRect = transform.parent as RectTransform;
            if (parentRect != null)
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        internal void SetFixedWidth(float width)
        {
            if (_minWidth == width
                && _preferredWidth == width
                && _flexibleWidth == 0f)
                return;

            _minWidth = width;
            _preferredWidth = width;
            _flexibleWidth = 0f;
            RestoreLayout();
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public float minWidth => _minWidth;
        public float preferredWidth => _preferredWidth;
        public float flexibleWidth => _flexibleWidth;
        public float minHeight => -1f;
        public float preferredHeight => -1f;
        public float flexibleHeight => -1f;
        public int layoutPriority => 100;
    }

    internal sealed class RowColumnSpec
    {
        internal RowColumnSpec(
            string objectName,
            RowColumnRole role,
            float minWidth,
            float preferredWidth,
            float flexibleWidth)
        {
            ObjectName = objectName;
            Role = role;
            MinWidth = minWidth;
            PreferredWidth = preferredWidth;
            FlexibleWidth = flexibleWidth;
        }

        internal string ObjectName { get; }
        internal RowColumnRole Role { get; }
        internal float MinWidth { get; }
        internal float PreferredWidth { get; }
        internal float FlexibleWidth { get; }

        internal static RowColumnSpec Fixed(
            string objectName,
            RowColumnRole role,
            float width)
        {
            return new RowColumnSpec(objectName, role, width, width, 0f);
        }

        internal static RowColumnSpec Flexible(
            string objectName,
            RowColumnRole role)
        {
            return new RowColumnSpec(objectName, role, 0f, 0f, 1f);
        }
    }

    internal sealed class RowLayoutSpec
    {
        internal RowLayoutSpec(string panelName, params RowColumnSpec[] columns)
        {
            PanelName = panelName;
            Columns = columns ?? new RowColumnSpec[0];
        }

        internal string PanelName { get; }
        internal RowColumnSpec[] Columns { get; }

        internal void Apply(GameObject rowRoot)
        {
            var panel = rowRoot.transform.Find(PanelName);
            if (panel == null)
                throw new InvalidOperationException("Missing row panel " + PanelName);

            var group = panel.GetComponent<HorizontalLayoutGroup>();
            if (group == null)
                throw new InvalidOperationException("Missing HorizontalLayoutGroup on " + PanelName);

            group.childControlWidth = true;
            group.childForceExpandWidth = false;
            group.childAlignment = TextAnchor.MiddleLeft;

            foreach (var column in Columns)
            {
                var child = panel.Find(column.ObjectName);
                if (child == null)
                    throw new InvalidOperationException(
                        "Missing " + column.ObjectName + " in " + PanelName);

                var layout = child.GetComponent<RowColumnLayoutOverride>()
                             ?? child.gameObject.AddComponent<RowColumnLayoutOverride>();
                layout.Configure(column);
            }

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)panel);
        }
    }

    internal static class RowLayoutCatalog
    {
        private static readonly RowLayoutSpec[] Specs =
        {
            new RowLayoutSpec(
                "ShaderRenderQueuePanel",
                RowColumnSpec.Flexible("ShaderRenderQueueLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "ShaderRenderQueueInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.RenderQueueInputWidth),
                RowColumnSpec.Fixed(
                    "ShaderRenderQueueResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "OffsetScalePanel",
                RowColumnSpec.Flexible("OffsetScaleLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "EmptySpace",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "OffsetXText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelXWidth),
                RowColumnSpec.Fixed(
                    "OffsetXInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "OffsetYText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelYWidth),
                RowColumnSpec.Fixed(
                    "OffsetYInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "OffsetScaleGroupSpacer",
                    RowColumnRole.Auxiliary,
                    MaterialEditorTheme.Metrics.OffsetScaleGroupSpacing),
                RowColumnSpec.Fixed(
                    "ScaleXText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelXWidth),
                RowColumnSpec.Fixed(
                    "ScaleXInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "ScaleYText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleLabelYWidth),
                RowColumnSpec.Fixed(
                    "ScaleYInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.OffsetScaleInputWidth),
                RowColumnSpec.Fixed(
                    "OffsetScaleResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "ColorPanel",
                RowColumnSpec.Flexible("ColorLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "SelectInterpolableColorButton",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "ColorEditorGroup",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ContentWidth
                    + MaterialEditorTheme.Spacing.Control),
                RowColumnSpec.Fixed(
                    "ColorResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "FloatPanel",
                RowColumnSpec.Flexible("FloatLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "SelectInterpolableFloatButton",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "FloatSlider",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.FloatSliderWidth),
                RowColumnSpec.Fixed(
                    "FloatInputField",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.FloatInputWidth),
                RowColumnSpec.Fixed(
                    "FloatResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "EnumPanel",
                RowColumnSpec.Flexible("EnumLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "SelectInterpolableEnumButton",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "EnumDropdown",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.ContentWidth
                    + MaterialEditorTheme.Spacing.Control),
                RowColumnSpec.Fixed(
                    "EnumResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "VectorPanel",
                RowColumnSpec.Flexible("VectorLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "SelectInterpolableVectorButton",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "VectorXText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentLabelWidth),
                RowColumnSpec.Fixed(
                    "VectorXInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentInputWidth),
                RowColumnSpec.Fixed(
                    "VectorYText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentLabelWidth),
                RowColumnSpec.Fixed(
                    "VectorYInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentInputWidth),
                RowColumnSpec.Fixed(
                    "VectorZText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentLabelWidth),
                RowColumnSpec.Fixed(
                    "VectorZInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentInputWidth),
                RowColumnSpec.Fixed(
                    "VectorWText",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentLabelWidth),
                RowColumnSpec.Fixed(
                    "VectorWInput",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.VectorComponentInputWidth),
                RowColumnSpec.Fixed(
                    "VectorResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "FloatTogglePanel",
                RowColumnSpec.Flexible("FloatToggleLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "SelectInterpolableFloatToggleButton",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "FloatToggleToggle",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.KeywordToggleWidth
                    + MaterialEditorTheme.Spacing.Control),
                RowColumnSpec.Fixed(
                    "FloatToggleResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth)),
            new RowLayoutSpec(
                "KeywordPanel",
                RowColumnSpec.Flexible("KeywordLabel", RowColumnRole.Label),
                RowColumnSpec.Fixed(
                    "EmptySpace",
                    RowColumnRole.Timeline,
                    MaterialEditorLayout.InterpolableButtonWidth),
                RowColumnSpec.Fixed(
                    "KeywordToggle",
                    RowColumnRole.Editor,
                    MaterialEditorLayout.KeywordToggleWidth
                    + MaterialEditorTheme.Spacing.Control),
                RowColumnSpec.Fixed(
                    "KeywordResetButton",
                    RowColumnRole.Reset,
                    MaterialEditorLayout.ResetButtonWidth))
        };

        internal static void Apply(GameObject rowRoot)
        {
            foreach (var spec in Specs)
                spec.Apply(rowRoot);
        }

        internal static void Restore(GameObject rowRoot)
        {
            Apply(rowRoot);
            foreach (var layout in rowRoot.GetComponentsInChildren<RowColumnLayoutOverride>(true))
                layout.RestoreLayout();
        }
    }
}
