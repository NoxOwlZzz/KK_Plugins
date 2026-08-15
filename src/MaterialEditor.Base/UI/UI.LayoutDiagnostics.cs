using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal static class RowLayoutDiagnostics
    {
        internal static string Describe(GameObject rowRoot, string context)
        {
            var message = new StringBuilder();
            message.AppendLine("[ME layout] " + context);

            foreach (var group in rowRoot.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                var panel = (RectTransform)group.transform;
                message.AppendLine(
                    string.Format(
                        "{0}: width={1:F3}, controlWidth={2}, expandWidth={3}, spacing={4:F3}",
                        panel.name,
                        panel.rect.width,
                        group.childControlWidth,
                        group.childForceExpandWidth,
                        group.spacing));

                foreach (Transform child in panel)
                {
                    var rect = child as RectTransform;
                    if (rect == null)
                        continue;

                    message.AppendLine(
                        string.Format(
                            "  {0}: x={1:F3}, width={2:F3}, active={3}",
                            child.name,
                            rect.anchoredPosition.x,
                            rect.rect.width,
                            child.gameObject.activeSelf));

                    foreach (var component in child.GetComponents<Component>())
                    {
                        var element = component as ILayoutElement;
                        if (element == null)
                            continue;

                        var column = component as RowColumnLayoutOverride;
                        message.AppendLine(
                            string.Format(
                                "    {0}: priority={1}, min={2:F3}, preferred={3:F3}, flexible={4:F3}{5}",
                                component.GetType().FullName,
                                element.layoutPriority,
                                element.minWidth,
                                element.preferredWidth,
                                element.flexibleWidth,
                                column != null ? ", role=" + column.Role : string.Empty));
                    }
                }
            }

            return message.ToString();
        }
    }

    internal static class RowLayoutRuntimeAssertions
    {
        private const float Tolerance = 0.5f;

        internal static void Validate(RowView row)
        {
            var wasActive = row.gameObject.activeSelf;
            Dictionary<GameObject, bool> panelStates = null;
            Dropdown enumDropdown = null;
            string originalEnumCaption = null;
            row.SetVisible(true);

            try
            {
                var colorPanel = FindRect(row.transform, "ColorPanel");
                var offsetPanel = FindRect(row.transform, "OffsetScalePanel");
                var floatPanel = FindRect(row.transform, "FloatPanel");
                var enumPanel = FindRect(row.transform, "EnumPanel");
                var vectorPanel = FindRect(row.transform, "VectorPanel");
                var rInput = FindInput(row.transform, "ColorRInput");
                var gInput = FindInput(row.transform, "ColorGInput");
                var bInput = FindInput(row.transform, "ColorBInput");
                var aInput = FindInput(row.transform, "ColorAInput");
                var vectorXInput = FindInput(row.transform, "VectorXInput");
                var vectorYInput = FindInput(row.transform, "VectorYInput");
                var vectorZInput = FindInput(row.transform, "VectorZInput");
                var vectorWInput = FindInput(row.transform, "VectorWInput");
                var vectorXLabel = FindText(row.transform, "VectorXText");
                var vectorYLabel = FindText(row.transform, "VectorYText");
                var vectorZLabel = FindText(row.transform, "VectorZText");
                var vectorWLabel = FindText(row.transform, "VectorWText");
                enumDropdown = FindDropdown(row.transform, "EnumDropdown");
                panelStates = ActivatePanels(
                    colorPanel,
                    offsetPanel,
                    floatPanel,
                    enumPanel,
                    vectorPanel);

                var originalR = rInput.text;
                var originalG = gInput.text;
                originalEnumCaption = enumDropdown.captionText.text;
                enumDropdown.captionText.text = "Logical Copy Inverted";
                enumDropdown.captionText.SetAllDirty();
                ValidateNumericEditing(rInput);
                ValidateNumericEditing(vectorXInput);
                ValidateInputVisual(vectorXInput);
                ValidateInputVisual(vectorYInput);
                ValidateInputVisual(vectorZInput);
                ValidateInputVisual(vectorWInput);
                rInput.text = "0";
                gInput.text = "0.123456789";

                ForceLayout(colorPanel);
                ForceLayout(offsetPanel);
                ForceLayout(floatPanel);
                ForceLayout(enumPanel);
                ForceLayout(vectorPanel);
                Canvas.ForceUpdateCanvases();

                AssertClose(
                    "RGBA declared width",
                    MaterialEditorLayout.ColorInputWidth,
                    ((RectTransform)rInput.transform).rect.width,
                    row);
                AssertClose(
                    "RGBA short/long widths",
                    ((RectTransform)rInput.transform).rect.width,
                    ((RectTransform)gInput.transform).rect.width,
                    row);
                AssertClose(
                    "RGBA R/B widths",
                    ((RectTransform)rInput.transform).rect.width,
                    ((RectTransform)bInput.transform).rect.width,
                    row);
                AssertClose(
                    "RGBA R/A widths",
                    ((RectTransform)rInput.transform).rect.width,
                    ((RectTransform)aInput.transform).rect.width,
                    row);

                AssertClose(
                    "Color/offset editor alignment",
                    WorldLeft(FindRect(row.transform, "ColorRText")),
                    WorldLeft(FindRect(row.transform, "OffsetXText")),
                    row);
                AssertClose(
                    "Color/float editor alignment",
                    WorldLeft(FindRect(row.transform, "ColorRText")),
                    WorldLeft(FindRect(row.transform, "FloatSlider")),
                    row);
                AssertClose(
                    "Vector X declared width",
                    MaterialEditorLayout.VectorComponentInputWidth,
                    ((RectTransform)vectorXInput.transform).rect.width,
                    row);
                AssertClose(
                    "Vector X/Y widths",
                    ((RectTransform)vectorXInput.transform).rect.width,
                    ((RectTransform)vectorYInput.transform).rect.width,
                    row);
                AssertClose(
                    "Vector X/Z widths",
                    ((RectTransform)vectorXInput.transform).rect.width,
                    ((RectTransform)vectorZInput.transform).rect.width,
                    row);
                AssertClose(
                    "Vector X/W widths",
                    ((RectTransform)vectorXInput.transform).rect.width,
                    ((RectTransform)vectorWInput.transform).rect.width,
                    row);
                AssertAtLeast(
                    "Vector X rendered label font size",
                    MaterialEditorLayout.VectorComponentMinimumFontSize,
                    vectorXLabel.cachedTextGenerator.fontSizeUsedForBestFit,
                    row);
                AssertAtLeast(
                    "Vector Y rendered label font size",
                    MaterialEditorLayout.VectorComponentMinimumFontSize,
                    vectorYLabel.cachedTextGenerator.fontSizeUsedForBestFit,
                    row);
                AssertAtLeast(
                    "Vector Z rendered label font size",
                    MaterialEditorLayout.VectorComponentMinimumFontSize,
                    vectorZLabel.cachedTextGenerator.fontSizeUsedForBestFit,
                    row);
                AssertAtLeast(
                    "Vector W rendered label font size",
                    MaterialEditorLayout.VectorComponentMinimumFontSize,
                    vectorWLabel.cachedTextGenerator.fontSizeUsedForBestFit,
                    row);
                AssertOrdered(
                    "Vector X/Y controls",
                    WorldRight((RectTransform)vectorXInput.transform),
                    WorldLeft(vectorYLabel.rectTransform),
                    row);
                AssertOrdered(
                    "Vector Y/Z controls",
                    WorldRight((RectTransform)vectorYInput.transform),
                    WorldLeft(vectorZLabel.rectTransform),
                    row);
                AssertOrdered(
                    "Vector Z/W controls",
                    WorldRight((RectTransform)vectorZInput.transform),
                    WorldLeft(vectorWLabel.rectTransform),
                    row);
                AssertOrdered(
                    "Vector W/reset controls",
                    WorldRight((RectTransform)vectorWInput.transform),
                    WorldLeft(FindRect(row.transform, "VectorResetButton")),
                    row);
                AssertClose(
                    "Enum caption fixed font size",
                    MaterialEditorLayout.DropdownFontSize,
                    enumDropdown.captionText.fontSize,
                    row);
                AssertClose(
                    "Enum caption configured minimum font size",
                    MaterialEditorLayout.DropdownFontSize,
                    enumDropdown.captionText.resizeTextMinSize,
                    row);
                AssertClose(
                    "Enum item configured minimum font size",
                    MaterialEditorLayout.DropdownFontSize,
                    enumDropdown.itemText.resizeTextMinSize,
                    row);
                AssertAtLeast(
                    "Enum caption text height",
                    MaterialEditorLayout.DropdownMinimumFontSize,
                    enumDropdown.captionText.rectTransform.rect.height,
                    row);

                rInput.text = originalR;
                gInput.text = originalG;
                ForceLayout(colorPanel);
                if (MaterialEditorPluginBase.Logger != null)
                {
                    MaterialEditorPluginBase.Logger.LogMessage(
                        string.Format(
                            "[ME layout validation] PASS: Vector inputs={0:F1} UI; "
                            + "Enum rendered font={1}; caption height={2:F1} UI.",
                            ((RectTransform)vectorXInput.transform).rect.width,
                            enumDropdown.captionText.cachedTextGenerator.fontSizeUsedForBestFit,
                            enumDropdown.captionText.rectTransform.rect.height));
                }
            }
            catch (Exception exception)
            {
                if (MaterialEditorPluginBase.Logger != null)
                    MaterialEditorPluginBase.Logger.LogError(
                        "[ME layout assertion] Validation failed: " + exception);
            }
            finally
            {
                if (enumDropdown?.captionText != null
                    && originalEnumCaption != null)
                {
                    enumDropdown.captionText.text = originalEnumCaption;
                    enumDropdown.captionText.SetAllDirty();
                }
                RestorePanels(panelStates);
                row.SetVisible(wasActive);
            }
        }

        internal static void ValidateClones(RowView first, RowView second)
        {
            var firstWasActive = first.gameObject.activeSelf;
            var secondWasActive = second.gameObject.activeSelf;
            Dictionary<GameObject, bool> panelStates = null;
            first.SetVisible(true);
            second.SetVisible(true);

            try
            {
                var firstPanel = FindRect(first.transform, "ColorPanel");
                var secondPanel = FindRect(second.transform, "ColorPanel");
                var firstVectorPanel = FindRect(first.transform, "VectorPanel");
                var secondVectorPanel = FindRect(second.transform, "VectorPanel");
                var firstEnumPanel = FindRect(first.transform, "EnumPanel");
                var secondEnumPanel = FindRect(second.transform, "EnumPanel");
                panelStates = ActivatePanels(
                    firstPanel,
                    secondPanel,
                    firstVectorPanel,
                    secondVectorPanel,
                    firstEnumPanel,
                    secondEnumPanel);
                ForceLayout(firstPanel);
                ForceLayout(secondPanel);
                ForceLayout(firstVectorPanel);
                ForceLayout(secondVectorPanel);
                ForceLayout(firstEnumPanel);
                ForceLayout(secondEnumPanel);
                Canvas.ForceUpdateCanvases();
                AssertClose(
                    "RGBA cloned row widths",
                    FindRect(first.transform, "ColorRInput").rect.width,
                    FindRect(second.transform, "ColorRInput").rect.width,
                    first);
                AssertClose(
                    "Vector cloned row widths",
                    FindRect(first.transform, "VectorXInput").rect.width,
                    FindRect(second.transform, "VectorXInput").rect.width,
                    first);
                AssertClose(
                    "Enum cloned caption font size",
                    FindDropdown(first.transform, "EnumDropdown").captionText.fontSize,
                    FindDropdown(second.transform, "EnumDropdown").captionText.fontSize,
                    first);
                if (MaterialEditorPluginBase.Logger != null)
                {
                    MaterialEditorPluginBase.Logger.LogMessage(
                        "[ME layout validation] PASS: pooled Vector/Enum clones are stable.");
                }
            }
            catch (Exception exception)
            {
                if (MaterialEditorPluginBase.Logger != null)
                    MaterialEditorPluginBase.Logger.LogError(
                        "[ME layout assertion] Clone validation failed: " + exception);
            }
            finally
            {
                RestorePanels(panelStates);
                first.SetVisible(firstWasActive);
                second.SetVisible(secondWasActive);
            }
        }

        private static void AssertClose(
            string name,
            float expected,
            float actual,
            RowView row)
        {
            if (Mathf.Abs(expected - actual) <= Tolerance)
                return;

            if (MaterialEditorPluginBase.Logger == null)
                return;

            MaterialEditorPluginBase.Logger.LogError(
                string.Format(
                    "[ME layout assertion] {0}: expected {1:F3}, actual {2:F3}\n{3}",
                    name,
                    expected,
                    actual,
                    RowLayoutDiagnostics.Describe(row.gameObject, name)));
        }

        private static void AssertAtLeast(
            string name,
            float minimum,
            float actual,
            RowView row)
        {
            if (actual >= minimum - Tolerance)
                return;

            if (MaterialEditorPluginBase.Logger == null)
                return;

            MaterialEditorPluginBase.Logger.LogError(
                string.Format(
                    "[ME layout assertion] {0}: expected at least {1:F3}, actual {2:F3}\n{3}",
                    name,
                    minimum,
                    actual,
                    RowLayoutDiagnostics.Describe(row.gameObject, name)));
        }

        private static void AssertOrdered(
            string name,
            float leftControlRight,
            float rightControlLeft,
            RowView row)
        {
            if (leftControlRight <= rightControlLeft + Tolerance)
                return;

            if (MaterialEditorPluginBase.Logger == null)
                return;

            MaterialEditorPluginBase.Logger.LogError(
                string.Format(
                    "[ME layout assertion] {0} overlap: left right={1:F3}, right left={2:F3}\n{3}",
                    name,
                    leftControlRight,
                    rightControlLeft,
                    RowLayoutDiagnostics.Describe(row.gameObject, name)));
        }

        private static void ForceLayout(RectTransform panel)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        private static void ValidateNumericEditing(InputField input)
        {
            var numeric = input.GetComponent<NumericInputView>();
            if (numeric == null)
                throw new InvalidOperationException("Missing NumericInputView on " + input.name);

            numeric.SetValue(0.9255123f);
            numeric.OnSelect(null);
            numeric.CommitValue(0.5f);
            if (input.text != "0.5")
                throw new InvalidOperationException(
                    "Numeric input did not restore compact text after editing: " + input.text);
        }

        private static void ValidateInputVisual(InputField input)
        {
            var image = input.GetComponent<Image>();
            if (image == null
                || !image.enabled
                || image.sprite == null
                || image.color.a <= 0.01f
                || input.targetGraphic != image)
            {
                throw new InvalidOperationException(
                    "Numeric input has no visible target background: " + input.name);
            }

            var viewport = input.transform.Find(input.gameObject.name + "Viewport")
                           as RectTransform;
            if (viewport == null
                || Mathf.Abs(viewport.anchorMin.x) > Tolerance
                || Mathf.Abs(viewport.anchorMin.y) > Tolerance
                || Mathf.Abs(viewport.anchorMax.x - 1f) > Tolerance
                || Mathf.Abs(viewport.anchorMax.y - 1f) > Tolerance)
            {
                throw new InvalidOperationException(
                    "Numeric input viewport does not fill its control: " + input.name);
            }
        }

        private static float WorldLeft(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[0].x;
        }

        private static float WorldRight(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[2].x;
        }

        private static Dictionary<GameObject, bool> ActivatePanels(
            params RectTransform[] panels)
        {
            var states = new Dictionary<GameObject, bool>();
            foreach (var panel in panels)
            {
                if (panel == null || states.ContainsKey(panel.gameObject))
                    continue;

                states.Add(panel.gameObject, panel.gameObject.activeSelf);
                panel.gameObject.SetActive(true);
            }

            return states;
        }

        private static void RestorePanels(Dictionary<GameObject, bool> states)
        {
            if (states == null)
                return;

            foreach (var state in states)
                state.Key.SetActive(state.Value);
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            var target = root.FindLoop(name);
            if (target == null)
                throw new InvalidOperationException("Missing " + name);
            return target.GetComponent<RectTransform>();
        }

        private static InputField FindInput(Transform root, string name)
        {
            var target = root.FindLoop(name);
            if (target == null)
                throw new InvalidOperationException("Missing " + name);
            return target.GetComponent<InputField>();
        }

        private static Text FindText(Transform root, string name)
        {
            var target = root.FindLoop(name);
            if (target == null)
                throw new InvalidOperationException("Missing " + name);
            return target.GetComponent<Text>();
        }

        private static Dropdown FindDropdown(Transform root, string name)
        {
            var target = root.FindLoop(name);
            if (target == null)
                throw new InvalidOperationException("Missing " + name);
            return target.GetComponent<Dropdown>();
        }
    }
}
