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
            var mixedIndex = -1;
            var isMixed = false;
            Action rebuild = () =>
            {
                controls.Dropdown.options.Clear();
                optionValues = new List<float>();
                var selectedIndex = -1;
                mixedIndex = -1;
                var selection =
                    MaterialEditorFloatBackedValuePolicy.ResolveEnumSelection(
                        item.Options,
                        item.CurrentValues ?? new[] { item.Value });
                isMixed = selection.State == MaterialEditorEnumValueState.Mixed;

                if (isMixed)
                {
                    mixedIndex = optionValues.Count;
                    selectedIndex = mixedIndex;
                    optionValues.Add(item.Value);
                    controls.Dropdown.options.Add(new Dropdown.OptionData("Mixed"));
                }

                if (item.Options != null)
                {
                    for (var optionIndex = 0;
                         optionIndex < item.Options.Count;
                         optionIndex++)
                    {
                        var option = item.Options[optionIndex];
                        if (selection.State == MaterialEditorEnumValueState.Matched
                            && selection.OptionIndex == optionIndex)
                        {
                            selectedIndex = optionValues.Count;
                        }
                        optionValues.Add(option.Value);
                        controls.Dropdown.options.Add(
                            new Dropdown.OptionData(option.DisplayName));
                    }
                }

                // Preserve values authored outside the declared option set.
                if (selection.State == MaterialEditorEnumValueState.Unmatched)
                {
                    selectedIndex = optionValues.Count;
                    optionValues.Add(selection.CurrentValue);
                    controls.Dropdown.options.Add(
                        new Dropdown.OptionData(
                            "Current ("
                            + selection.CurrentValue.ToString(
                                CultureInfo.InvariantCulture)
                            + ")"));
                }

                controls.Dropdown.Set(selectedIndex);
            };
            Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    isMixed ? item.LabelText + " (Mixed)" : item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);

            rebuild();
            refresh();
            listeners.Listen(controls.Dropdown, index =>
            {
                if (optionValues == null
                    || index < 0
                    || index >= optionValues.Count
                    || index == mixedIndex)
                    return;

                var value = optionValues[index];
                if (!isMixed
                    && MaterialEditorFloatBackedValuePolicy.Approximately(
                        value,
                        item.Value))
                    return;

                var wasMixed = isMixed;
                item.Value = value;
                item.CurrentValues = new[] { value };
                if (wasMixed)
                {
                    // An explicit choice must be applied to every same-named
                    // material and remain persisted even when it equals the
                    // representative material's original value.
                    MaterialEditorFloatBackedValuePolicy.PersistExplicitEnumSelection(
                        item.ValueOnReset,
                        item.ValueOnChange,
                        item.Value);
                }
                else if (MaterialEditorFloatBackedValuePolicy.ShouldRemoveEnumOverride(
                             wasMixed,
                             item.Value,
                             item.OriginalValue))
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
                item.CurrentValues = new[] { item.OriginalValue };
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
                    MaterialEditorFloatBackedValuePolicy.GetBooleanDisplayValue(
                        item.Value,
                        item.Invert),
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
                var value =
                    MaterialEditorFloatBackedValuePolicy.GetBooleanStoredValue(
                        enabled,
                        item.Invert);
                if (MaterialEditorFloatBackedValuePolicy.Approximately(
                        value,
                        item.Value))
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
