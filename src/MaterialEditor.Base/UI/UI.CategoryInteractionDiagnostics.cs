using System.Globalization;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Event-driven diagnostics for category click/navigation/highlight mapping.
    /// The existing performance diagnostics switch is disabled by default, so
    /// the normal path is one branch and performs no formatting or logging.
    /// </summary>
    internal static class CategoryInteractionDiagnostics
    {
        internal static bool Enabled
        {
            get
            {
                var setting = MaterialEditorPluginBase.PerformanceDiagnostics;
                return setting != null && setting.Value;
            }
        }

        internal static void Log(
            string phase,
            int visualRowIndex,
            int navigatorEntryIndex,
            string stableCategoryKey,
            int listenerKey,
            string clickedStableKey,
            string navigationKey,
            string highlightKey,
            RectTransform categoryRect,
            RectTransform navigatorScrollContent,
            RectTransform centralScrollContent,
            bool programmatic)
        {
            if (!Enabled)
                return;

            var pointer = Input.mousePosition;
            var rect = categoryRect == null
                ? new Rect()
                : categoryRect.rect;
            var worldMin = categoryRect == null
                ? Vector3.zero
                : categoryRect.TransformPoint(
                    new Vector3(rect.xMin, rect.yMin, 0f));
            var worldMax = categoryRect == null
                ? Vector3.zero
                : categoryRect.TransformPoint(
                    new Vector3(rect.xMax, rect.yMax, 0f));
            var navigatorScrollPosition = navigatorScrollContent == null
                ? 0f
                : navigatorScrollContent.localPosition.y;
            var centralScrollPosition = centralScrollContent == null
                ? 0f
                : centralScrollContent.localPosition.y;
            var invariant = CultureInfo.InvariantCulture;

            MaterialEditorPluginBase.Logger?.LogMessage(
                "[MaterialEditor category] phase=" + Safe(phase)
                + " pointer=(" + pointer.x.ToString("F1", invariant)
                + "," + pointer.y.ToString("F1", invariant) + ")"
                + " visualRowIndex=" + visualRowIndex.ToString(invariant)
                + " navigatorEntryIndex=" + navigatorEntryIndex.ToString(invariant)
                + " stableCategoryKey=" + Safe(stableCategoryKey)
                + " listenerKey=" + listenerKey.ToString(invariant)
                + " clickedStableKey=" + Safe(clickedStableKey)
                + " navigationKey=" + Safe(navigationKey)
                + " highlightKey=" + Safe(highlightKey)
                + " rect=(" + worldMin.x.ToString("F1", invariant)
                + "," + worldMin.y.ToString("F1", invariant)
                + ")-(" + worldMax.x.ToString("F1", invariant)
                + "," + worldMax.y.ToString("F1", invariant) + ")"
                + " navigatorScroll="
                + navigatorScrollPosition.ToString("F1", invariant)
                + " centralScroll="
                + centralScrollPosition.ToString("F1", invariant)
                + " scrollOrigin=" + (programmatic ? "programmatic" : "user"));
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<none>" : value;
        }
    }
}
