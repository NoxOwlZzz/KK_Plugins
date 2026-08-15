using System;
using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class EnumVectorToggleRowTypeBinder : IRowTypeBinder
    {
        private readonly RowControlSet _controls;

        internal EnumVectorToggleRowTypeBinder(RowControlSet controls)
        {
            _controls = controls;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            switch (item.ItemType)
            {
                case RowModel.RowItemType.EnumProperty:
                    BindEnum((EnumPropertyRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.VectorProperty:
                    BindVector((VectorPropertyRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.FloatToggleProperty:
                    BindFloatToggle((FloatTogglePropertyRowModel)item, listeners);
                    break;
            }
        }

        private void BindEnum(EnumPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Enum;
            controls.SetVisible(true);
            TooltipBinding.Bind(
                controls.Label.gameObject,
                item.TooltipText,
                item.PropertyName,
                controls.Label);
            var canSelectInterpolable = item.SelectInterpolable != null;
            controls.SelectInterpolableButton.gameObject.SetActive(
                canSelectInterpolable);
            Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.IsMixed ? "Mixed · " + item.LabelText : item.LabelText,
                    item.IsMixed || item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);

            controls.OptionCache.Rebuild(item.Options, item.Value, item.IsMixed);
            refresh();
            listeners.Listen(controls.Dropdown, index =>
            {
                float selectedValue;
                if (!controls.OptionCache.TryGetValue(index, out selectedValue))
                    return;
                var wasMixed = item.IsMixed;
                if (!wasMixed && selectedValue == item.Value)
                    return;
                item.IsMixed = false;
                item.Value = selectedValue;
                if (wasMixed)
                    MaterialEditorSemanticValuePolicy.PersistExplicitEnumSelection(
                        item.ValueOnReset,
                        item.ValueOnChange,
                        item.Value);
                else if (item.Value != item.OriginalValue)
                    item.ValueOnChange(item.Value);
                else
                    item.ValueOnReset();
                controls.OptionCache.Rebuild(item.Options, item.Value, item.IsMixed);
                refresh();
                item.PresentationRefresh?.Invoke();
            });
            listeners.Listen(controls.ResetButton, () =>
            {
                item.IsMixed = false;
                item.Value = item.OriginalValue;
                item.ValueOnReset();
                controls.OptionCache.Rebuild(item.Options, item.Value, item.IsMixed);
                refresh();
                item.PresentationRefresh?.Invoke();
            });
            if (canSelectInterpolable)
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

        private void BindVector(VectorPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Vector;
            controls.SetVisible(true);
            TooltipBinding.Bind(
                controls.Label.gameObject,
                item.TooltipText,
                item.PropertyName,
                controls.Label);
            var count = Mathf.Clamp(item.ComponentCount, 2, 4);
            if (item.MixedComponents == null || item.MixedComponents.Length < 4)
                item.MixedComponents = new bool[4];
            var canSelectInterpolable = item.SelectInterpolable != null;
            controls.SelectInterpolableButton.gameObject.SetActive(
                canSelectInterpolable);

            Action refreshState = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    HasMixed(item.MixedComponents, count)
                    || item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);
            Action<int, float, bool> apply =
                (componentIndex, componentValue, wasMixed) =>
            {
                if (item.ComponentOnChange != null)
                    item.ComponentOnChange(componentIndex, componentValue);
                else if (!wasMixed
                    && item.Value == item.OriginalValue
                    && !HasMixed(item.MixedComponents, count))
                    item.ValueOnReset();
                else
                    item.ValueOnChange(item.Value);
                refreshState();
                item.PresentationRefresh?.Invoke();
            };

            for (var component = 0; component < 4; component++)
            {
                var index = component;
                var visible = index < count;
                controls.ComponentLabels[index].gameObject.SetActive(visible);
                controls.ComponentInputs[index].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                var input = controls.ComponentInputs[index];
                if (item.MixedComponents[index])
                    input.SetMixed();
                else
                    input.SetValue(GetComponent(item.Value, index));
                listeners.Listen(input.InputField, text =>
                {
                    float parsed;
                    if (!input.TryParse(text, out parsed))
                    {
                        if (item.MixedComponents[index])
                            input.SetMixed();
                        else
                            input.CommitValue(GetComponent(item.Value, index));
                        return;
                    }
                    if (item.Minimum.HasValue)
                        parsed = Mathf.Max(item.Minimum.Value, parsed);
                    if (item.Maximum.HasValue)
                        parsed = Mathf.Min(item.Maximum.Value, parsed);
                    var wasMixed = HasMixed(item.MixedComponents, count);
                    var componentWasMixed = item.MixedComponents[index];
                    var currentValue = GetComponent(item.Value, index);
                    if (!componentWasMixed && parsed == currentValue)
                    {
                        input.CommitValue(currentValue);
                        return;
                    }
                    item.MixedComponents[index] = false;
                    item.Value = WithComponent(item.Value, index, parsed);
                    input.CommitValue(parsed);
                    apply(index, parsed, wasMixed);
                });
            }
            refreshState();
            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                for (var index = 0; index < item.MixedComponents.Length; index++)
                    item.MixedComponents[index] = false;
                for (var index = 0; index < count; index++)
                    controls.ComponentInputs[index].SetValue(
                        GetComponent(item.Value, index));
                item.ValueOnReset();
                refreshState();
                item.PresentationRefresh?.Invoke();
            });
            if (canSelectInterpolable)
                listeners.Listen(
                    controls.SelectInterpolableButton,
                    () => item.SelectInterpolable());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.VectorProperty,
                () => item.PropertyName);
        }

        private void BindFloatToggle(
            FloatTogglePropertyRowModel item,
            ListenerScope listeners)
        {
            var controls = _controls.FloatToggle;
            controls.SetVisible(true);
            TooltipBinding.Bind(
                controls.Label.gameObject,
                item.TooltipText,
                item.PropertyName,
                controls.Label);
            var canSelectInterpolable = item.SelectInterpolable != null;
            controls.SelectInterpolableButton.gameObject.SetActive(
                canSelectInterpolable);
            Action refresh = () =>
            {
                var desired = !item.IsMixed && item.Value == item.OnValue;
                if (controls.Toggle.isOn != desired)
                    controls.Toggle.Set(desired, false);
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.IsMixed ? "Mixed · " + item.LabelText : item.LabelText,
                    item.IsMixed || item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);
            };
            refresh();
            listeners.Listen(controls.Toggle, value =>
            {
                BooleanPropertyRowModelBinding.ApplyUserValue(item, value);
                refresh();
            });
            listeners.Listen(controls.ResetButton, () =>
            {
                BooleanPropertyRowModelBinding.Reset(item);
                refresh();
            });
            if (canSelectInterpolable)
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

        private static bool HasMixed(bool[] values, int count)
        {
            return MaterialEditorMixedStatePerformance.HasMixed(values, count);
        }

        private static float GetComponent(Vector4 value, int index)
        {
            switch (index)
            {
                case 0: return value.x;
                case 1: return value.y;
                case 2: return value.z;
                default: return value.w;
            }
        }

        private static Vector4 WithComponent(Vector4 value, int index, float component)
        {
            MaterialEditorSemanticValuePolicy.SetVectorComponent(
                ref value.x,
                ref value.y,
                ref value.z,
                ref value.w,
                index,
                component);
            return value;
        }
    }
}
