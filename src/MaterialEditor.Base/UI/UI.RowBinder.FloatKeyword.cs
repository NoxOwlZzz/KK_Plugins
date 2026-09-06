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
            }
        }

        private void BindFloat(FloatPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Float;
            controls.SetVisible(true);
            TooltipBinding.Bind(
                controls.Label.gameObject,
                item.TooltipText,
                item.PropertyName,
                controls.Label);
            TimelineColumnBinding.Bind(
                controls.SelectInterpolableButton,
                listeners,
                item.SelectInterpolable);
            if (controls.Slider.gameObject.activeSelf != item.HasRange)
                controls.Slider.gameObject.SetActive(item.HasRange);
            controls.InputLayout.SetFixedWidth(
                item.HasRange
                    ? MaterialEditorLayout.FloatInputWidth
                    : MaterialEditorLayout.ContentWidth
                      + MaterialEditorTheme.Spacing.Control);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);
            System.Action<float> applyValue = value =>
            {
                item.Value = value;
                if (item.HasRange)
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
            if (item.HasRange)
            {
                SliderBinding.Bind(
                    listeners,
                    controls.Slider,
                    item.SliderMinimum,
                    item.SliderMaximum,
                    item.Value,
                    applyValue);
            }
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                if (item.HasRange)
                    controls.Slider.Set(item.Value, false);
                controls.Input.SetValue(item.Value);
                item.ValueOnReset();
                refresh();
                item.PresentationRefresh?.Invoke();
            });
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
            TooltipBinding.Bind(
                controls.Label.gameObject,
                item.TooltipText,
                item.PropertyName,
                controls.Label);
            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);
            controls.Toggle.Set(item.Value, false);
            refresh();
            listeners.Listen(controls.Toggle, value =>
            {
                BooleanPropertyRowModelBinding.ApplyUserValue(item, value);
                refresh();
            });
            listeners.Listen(controls.ResetButton, () =>
            {
                BooleanPropertyRowModelBinding.Reset(item);
                controls.Toggle.Set(item.Value, false);
                refresh();
            });
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.KeywordProperty,
                () => item.PropertyName);
        }
    }
}
