using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class FloatKeywordRowTypeBinder : IRowTypeBinder
    {
        private readonly RowControlSet _controls;

        internal FloatKeywordRowTypeBinder(RowControlSet controls)
        {
            _controls = controls;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            switch (item.ItemType)
            {
                case RowModel.RowItemType.FloatProperty:
                    BindFloat((FloatPropertyRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.KeywordProperty:
                    BindKeyword((KeywordPropertyRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.EnumProperty:
                    BindEnum((EnumPropertyRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.FloatToggleProperty:
                    BindFloatToggle((FloatTogglePropertyRowModel)item, listeners);
                    break;
            }
        }

        private void BindFloat(FloatPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Float;
            controls.SetVisible(true);
            TooltipBinding.Bind(controls.Label.gameObject, item.TooltipText);

            Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);
            Action<float> applyValue = value =>
            {
                item.Value = value;
                controls.Slider.Set(item.Value, false);
                controls.Input.SetValue(item.Value);
                if (item.Value == item.OriginalValue)
                    item.ValueOnReset();
                else
                    item.ValueOnChange(item.Value);
                refresh();
                item.PresentationRefresh?.Invoke();
            };

            InputFieldBinding.BindFloat(
                listeners,
                controls.Input,
                () => item.Value,
                applyValue);
            SliderBinding.Bind(
                listeners,
                controls.Slider,
                item.SliderMinimum,
                item.SliderMaximum,
                item.Value,
                applyValue);
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                controls.Slider.Set(item.Value, false);
                controls.Input.SetValue(item.Value);
                item.ValueOnReset();
                refresh();
                item.PresentationRefresh?.Invoke();
            });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolable());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.FloatProperty,
                () => item.PropertyName);
        }

        private void BindKeyword(KeywordPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Keyword;
            controls.SetVisible(true);
            TooltipBinding.Bind(controls.Label.gameObject, item.TooltipText);
            ToggleBinding.Bind(
                listeners,
                controls,
                item,
                () => item.Value,
                () => item.OriginalValue,
                value => item.Value = value,
                item.ValueOnChange,
                item.ValueOnReset);
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.KeywordProperty,
                () => item.PropertyName);
        }

        private void BindEnum(EnumPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Enum;
            controls.SetVisible(true);
            TooltipBinding.Bind(controls.Label.gameObject, item.TooltipText);

            List<float> optionValues = null;
            Action rebuild = () =>
            {
                controls.Dropdown.options.Clear();
                optionValues = new List<float>();
                var selectedIndex = -1;

                if (item.Options != null)
                {
                    foreach (var option in item.Options)
                    {
                        if (Mathf.Approximately(option.Value, item.Value))
                            selectedIndex = optionValues.Count;
                        optionValues.Add(option.Value);
                        controls.Dropdown.options.Add(
                            new Dropdown.OptionData(option.DisplayName));
                    }
                }

                // Preserve values authored outside the declared option set.
                if (selectedIndex < 0)
                {
                    selectedIndex = optionValues.Count;
                    optionValues.Add(item.Value);
                    controls.Dropdown.options.Add(
                        new Dropdown.OptionData(
                            "Current ("
                            + item.Value.ToString(CultureInfo.InvariantCulture)
                            + ")"));
                }

                controls.Dropdown.Set(selectedIndex);
            };
            Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);

            rebuild();
            refresh();
            listeners.Listen(controls.Dropdown, index =>
            {
                if (optionValues == null
                    || index < 0
                    || index >= optionValues.Count)
                    return;

                var value = optionValues[index];
                if (Mathf.Approximately(value, item.Value))
                    return;

                item.Value = value;
                if (item.Value == item.OriginalValue)
                    item.ValueOnReset();
                else
                    item.ValueOnChange(item.Value);
                rebuild();
                refresh();
                item.PresentationRefresh?.Invoke();
            });
            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                item.ValueOnReset();
                rebuild();
                refresh();
                item.PresentationRefresh?.Invoke();
            });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolable());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.FloatProperty,
                () => item.PropertyName);
        }

        private void BindFloatToggle(
            FloatTogglePropertyRowModel item,
            ListenerScope listeners)
        {
            var controls = _controls.FloatToggle;
            controls.SetVisible(true);
            TooltipBinding.Bind(controls.Label.gameObject, item.TooltipText);

            Action refresh = () =>
            {
                controls.Toggle.Set(
                    Mathf.Approximately(item.Value, item.Invert ? 0f : 1f),
                    false);
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);
            };

            refresh();
            listeners.Listen(controls.Toggle, enabled =>
            {
                var value = enabled != item.Invert ? 1f : 0f;
                if (Mathf.Approximately(value, item.Value))
                    return;

                item.Value = value;
                if (item.Value == item.OriginalValue)
                    item.ValueOnReset();
                else
                    item.ValueOnChange(item.Value);
                refresh();
                item.PresentationRefresh?.Invoke();
            });
            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                item.ValueOnReset();
                refresh();
                item.PresentationRefresh?.Invoke();
            });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolable());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.FloatProperty,
                () => item.PropertyName);
        }
    }
}
