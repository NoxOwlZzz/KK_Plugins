using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class TextureRowControls : RowControls
    {
        internal TextureRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("TexturePanel"))
        {
            Label = owner.GetUIComponent<Text>("TextureLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("TextureLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableTextureButton");
            ExportButton = owner.GetUIComponent<Button>("TextureExportButton");
            ImportButton = owner.GetUIComponent<Button>("TextureImportButton");
            ResetButton = owner.GetUIComponent<Button>("TextureResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ExportButton { get; }
        internal Button ImportButton { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class OffsetScaleRowControls : RowControls
    {
        internal OffsetScaleRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("OffsetScalePanel"))
        {
            Label = owner.GetUIComponent<Text>("OffsetScaleLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("OffsetScaleLabel");
            OffsetXLabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("OffsetXText");
            OffsetXInput = owner.GetUIComponent<NumericInputView>("OffsetXInput");
            OffsetYInput = owner.GetUIComponent<NumericInputView>("OffsetYInput");
            ScaleXInput = owner.GetUIComponent<NumericInputView>("ScaleXInput");
            ScaleYInput = owner.GetUIComponent<NumericInputView>("ScaleYInput");
            ResetButton = owner.GetUIComponent<Button>("OffsetScaleResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal LabelClickTrigger OffsetXLabelClickTrigger { get; }
        internal NumericInputView OffsetXInput { get; }
        internal NumericInputView OffsetYInput { get; }
        internal NumericInputView ScaleXInput { get; }
        internal NumericInputView ScaleYInput { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class ColorRowControls : RowControls
    {
        internal ColorRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("ColorPanel"))
        {
            Label = owner.GetUIComponent<Text>("ColorLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("ColorLabel");
            RInput = owner.GetUIComponent<NumericInputView>("ColorRInput");
            GInput = owner.GetUIComponent<NumericInputView>("ColorGInput");
            BInput = owner.GetUIComponent<NumericInputView>("ColorBInput");
            AInput = owner.GetUIComponent<NumericInputView>("ColorAInput");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableColorButton");
            ResetButton = owner.GetUIComponent<Button>("ColorResetButton");
            EditButton = owner.GetUIComponent<Button>("ColorEditButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal NumericInputView RInput { get; }
        internal NumericInputView GInput { get; }
        internal NumericInputView BInput { get; }
        internal NumericInputView AInput { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ResetButton { get; }
        internal Button EditButton { get; }
    }

    internal sealed class FloatRowControls : RowControls
    {
        internal FloatRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("FloatPanel"))
        {
            Label = owner.GetUIComponent<Text>("FloatLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("FloatLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableFloatButton");
            Slider = owner.GetUIComponent<Slider>("FloatSlider");
            Input = owner.GetUIComponent<NumericInputView>("FloatInputField");
            InputLayout = Input.GetComponent<RowColumnLayoutOverride>();
            ResetButton = owner.GetUIComponent<Button>("FloatResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Slider Slider { get; }
        internal NumericInputView Input { get; }
        internal RowColumnLayoutOverride InputLayout { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class EnumRowControls : RowControls
    {
        internal EnumRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("EnumPanel"))
        {
            Label = owner.GetUIComponent<Text>("EnumLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("EnumLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableEnumButton");
            Dropdown = owner.GetUIComponent<Dropdown>("EnumDropdown");
            OptionCache = new EnumDropdownOptionCache(Dropdown);
            ResetButton = owner.GetUIComponent<Button>("EnumResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Dropdown Dropdown { get; }
        internal EnumDropdownOptionCache OptionCache { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class VectorRowControls : RowControls
    {
        internal VectorRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("VectorPanel"))
        {
            Label = owner.GetUIComponent<Text>("VectorLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("VectorLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableVectorButton");
            ComponentLabels = new[]
            {
                owner.GetUIComponent<Text>("VectorXText"),
                owner.GetUIComponent<Text>("VectorYText"),
                owner.GetUIComponent<Text>("VectorZText"),
                owner.GetUIComponent<Text>("VectorWText")
            };
            ComponentInputs = new[]
            {
                owner.GetUIComponent<NumericInputView>("VectorXInput"),
                owner.GetUIComponent<NumericInputView>("VectorYInput"),
                owner.GetUIComponent<NumericInputView>("VectorZInput"),
                owner.GetUIComponent<NumericInputView>("VectorWInput")
            };
            ResetButton = owner.GetUIComponent<Button>("VectorResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Text[] ComponentLabels { get; }
        internal NumericInputView[] ComponentInputs { get; }
        internal Button ResetButton { get; }
    }
}
