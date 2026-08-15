using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal static class ChangedStateBinding
    {
        internal static void Apply(
            Text label,
            string text,
            bool changed,
            Button resetButton,
            CanvasGroup panel)
        {
            label.text = text ?? string.Empty;
            panel.gameObject.GetComponent<Image>().color =
                changed ? MaterialEditorUI.ItemColorChanged : MaterialEditorUI.ItemColor;
            if (resetButton)
                resetButton.interactable = changed;
        }

        internal static void SetLabel(Text label, string text)
        {
            label.text = text ?? string.Empty;
        }
    }

    internal static class LabelClickBinding
    {
        internal static void Bind(
            ListenerScope listeners,
            LabelClickTrigger trigger,
            RowModel item,
            MaterialEditorLabelType labelType,
            Func<string> getName,
            Action<UnityEngine.EventSystems.PointerEventData> onClicked = null)
        {
            Action<UnityEngine.EventSystems.PointerEventData> handler = pointerEventData =>
            {
                var name = getName();
                MaterialEditorUI.RaiseLabelClicked(
                    new MaterialEditorLabelClickEventArgs(
                        labelType,
                        name,
                        item.GameObject,
                        item.Data,
                        item.Renderer,
                        item.Material,
                        item.Projector,
                        pointerEventData));
                MaterialEditorExtensionRegistry.RaiseLabelSelection(item, labelType, name);
                onClicked?.Invoke(pointerEventData);
            };
            trigger.Clicked = handler;
            listeners.OnDispose(() =>
            {
                if (trigger.Clicked == handler)
                    trigger.Clicked = null;
            });
        }
    }

    internal static class TooltipBinding
    {
        internal static void Bind(
            GameObject target,
            string tooltipText,
            string fallbackText = null,
            Text hintLabel = null)
        {
            if (target == null)
                return;

            var text = tooltipText ?? string.Empty;
            var tooltip = target.GetComponent<Tooltip>();
            if (tooltip == null
                && (!string.IsNullOrEmpty(text)
                    || !string.IsNullOrEmpty(fallbackText)))
            {
                tooltip = target.AddComponent<Tooltip>();
            }
            if (tooltip == null)
                return;

            var label = hintLabel
                        ?? target.GetComponent<Text>()
                        ?? target.GetComponentInChildren<Text>(true);
            tooltip.Configure(fallbackText, text, label);
        }
    }

    internal static class ToggleBinding
    {
        internal static void Bind(
            ListenerScope listeners,
            ToggleRowControls controls,
            RowModel item,
            Func<bool> getValue,
            Func<bool> getOriginal,
            Action<bool> setValue,
            Action<bool> changeValue,
            Action resetValue)
        {
            Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    getValue() != getOriginal(),
                    controls.ResetButton,
                    controls.Panel);

            controls.Toggle.Set(getValue(), false);
            refresh();

            listeners.Listen(controls.Toggle, value =>
            {
                setValue(value);
                if (getValue() == getOriginal())
                    resetValue();
                else
                    changeValue(getValue());
                refresh();
                item.PresentationRefresh?.Invoke();
            });

            listeners.Listen(controls.ResetButton, () =>
            {
                setValue(getOriginal());
                controls.Toggle.Set(getValue(), false);
                resetValue();
                refresh();
                item.PresentationRefresh?.Invoke();
            });
        }
    }

    // One semantic path for edits and resets from the ordinary Boolean and
    // Keyword rows. Keeping it shared prevents those two binders from drifting.
    internal static class BooleanPropertyRowModelBinding
    {
        internal static bool ApplyUserValue(RowModel item, bool value)
        {
            var keyword = item as KeywordPropertyRowModel;
            if (keyword != null)
            {
                if (keyword.Value == value)
                    return false;
                keyword.Value = value;
                if (keyword.Value == keyword.OriginalValue)
                    keyword.ValueOnReset();
                else
                    keyword.ValueOnChange(keyword.Value);
                keyword.PresentationRefresh?.Invoke();
                return true;
            }

            var toggle = item as FloatTogglePropertyRowModel;
            if (toggle == null)
                return false;
            var wasMixed = toggle.IsMixed;
            var selectedValue =
                MaterialEditorSemanticValuePolicy.SelectToggleValue(
                    value,
                    toggle.OffValue,
                    toggle.OnValue);
            if (!wasMixed && selectedValue == toggle.Value)
                return false;
            toggle.IsMixed = false;
            toggle.Value = selectedValue;
            if (wasMixed || toggle.Value != toggle.OriginalValue)
                toggle.ValueOnChange(toggle.Value);
            else
                toggle.ValueOnReset();
            toggle.PresentationRefresh?.Invoke();
            return true;
        }

        internal static void Reset(RowModel item)
        {
            var keyword = item as KeywordPropertyRowModel;
            if (keyword != null)
            {
                keyword.Value = keyword.OriginalValue;
                keyword.ValueOnReset();
                keyword.PresentationRefresh?.Invoke();
                return;
            }

            var toggle = item as FloatTogglePropertyRowModel;
            if (toggle == null)
                return;
            toggle.IsMixed = false;
            toggle.Value = toggle.OriginalValue;
            toggle.ValueOnReset();
            toggle.PresentationRefresh?.Invoke();
        }
    }

    internal static class InputFieldBinding
    {
        internal static void BindFloat(
            ListenerScope listeners,
            NumericInputView input,
            Func<float> getValue,
            Action<float> setValue)
        {
            input.SetValue(getValue());
            listeners.Listen(input.InputField, value =>
            {
                float parsed;
                if (!input.TryParse(value, out parsed))
                {
                    input.CommitValue(getValue());
                    return;
                }

                if (parsed == getValue())
                {
                    input.CommitValue(getValue());
                    return;
                }

                setValue(parsed);
                input.CommitValue(getValue());
            });
        }

        internal static void BindInt(
            ListenerScope listeners,
            InputField input,
            Func<int> getValue,
            Action<int> setValue)
        {
            input.Set(getValue().ToString(), false);
            listeners.Listen(input, value =>
            {
                int parsed;
                if (!int.TryParse(value, out parsed))
                {
                    input.Set(getValue().ToString(), false);
                    return;
                }

                if (parsed == getValue())
                {
                    input.Set(getValue().ToString(), false);
                    return;
                }

                setValue(parsed);
                input.Set(getValue().ToString(), false);
            });
        }
    }

    internal static class SliderBinding
    {
        internal static void Bind(
            ListenerScope listeners,
            Slider slider,
            float minimum,
            float maximum,
            float value,
            Action<float> changeValue)
        {
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.Set(value, false);
            listeners.Listen(slider, currentValue => changeValue(currentValue));
        }
    }

    internal interface IRowTypeBinder
    {
        void Bind(RowModel item, ListenerScope listeners);
    }

    internal sealed class RowHandlerRegistry
    {
        private readonly Dictionary<RowModel.RowItemType, IRowTypeBinder> _handlers =
            new Dictionary<RowModel.RowItemType, IRowTypeBinder>();

        internal void Register(IRowTypeBinder handler, params RowModel.RowItemType[] itemTypes)
        {
            foreach (var itemType in itemTypes)
                _handlers[itemType] = handler;
        }

        internal bool TryGet(RowModel.RowItemType itemType, out IRowTypeBinder handler)
        {
            return _handlers.TryGetValue(itemType, out handler);
        }
    }
}
