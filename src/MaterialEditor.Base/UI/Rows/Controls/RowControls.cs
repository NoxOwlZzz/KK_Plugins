using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal abstract class RowControls
    {
        private bool _visible;
        private bool _enabled = true;
        private readonly RowPanelInset _hierarchyInset;

        protected RowControls(CanvasGroup panel)
        {
            Panel = panel;
            _hierarchyInset = panel.GetComponent<RowPanelInset>()
                              ?? panel.gameObject.AddComponent<RowPanelInset>();
        }

        internal CanvasGroup Panel { get; }

        internal void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyState();
        }

        internal void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            ApplyState();
        }

        internal void SetHierarchyInset(float left, float right, float vertical)
        {
            _hierarchyInset.Configure(left, right, vertical);
        }

        private void ApplyState()
        {
            Panel.alpha = _visible
                ? (_enabled
                    ? MaterialEditorTheme.States.VisibleAlpha
                    : MaterialEditorTheme.States.DisabledAlpha)
                : MaterialEditorTheme.States.HiddenAlpha;
            Panel.interactable = _enabled;
            Panel.blocksRaycasts = _visible && _enabled;
        }
    }

    internal sealed class ToggleRowControls : RowControls
    {
        internal ToggleRowControls(
            CanvasGroup panel,
            Text label,
            Toggle toggle,
            Button resetButton,
            LabelClickTrigger labelClickTrigger = null,
            Button selectInterpolableButton = null)
            : base(panel)
        {
            Label = label;
            Toggle = toggle;
            ResetButton = resetButton;
            LabelClickTrigger = labelClickTrigger;
            SelectInterpolableButton = selectInterpolableButton;
        }

        internal Text Label { get; }
        internal Toggle Toggle { get; }
        internal Button ResetButton { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
    }

    internal sealed class DropdownRowControls : RowControls
    {
        internal DropdownRowControls(
            CanvasGroup panel,
            Text label,
            Dropdown dropdown,
            Button resetButton)
            : base(panel)
        {
            Label = label;
            Dropdown = dropdown;
            ResetButton = resetButton;
        }

        internal Text Label { get; }
        internal Dropdown Dropdown { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class InputRowControls : RowControls
    {
        internal InputRowControls(
            CanvasGroup panel,
            Text label,
            LabelClickTrigger labelClickTrigger,
            InputField input,
            Button resetButton)
            : base(panel)
        {
            Label = label;
            LabelClickTrigger = labelClickTrigger;
            Input = input;
            ResetButton = resetButton;
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal InputField Input { get; }
        internal Button ResetButton { get; }
    }
}
