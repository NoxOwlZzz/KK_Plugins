using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class RowViewFactory
    {
        internal static GameObject CreateTemplate(Transform parent)
        {
            // ListEntry is only the clipping source. A separate backdrop owns
            // the visible Legacy row edge so hierarchy depth can inset the
            // surface without changing the mask or the Dark layout.
            var contentList = MaterialEditorControlFactory.CreateRowStencilMaskPanel(
                "ListEntry",
                parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;

            var backdrop = MaterialEditorControlFactory.CreatePanel(
                "RowBackdrop",
                contentList.transform,
                MaterialEditorPanelRole.RowBackdrop);
            backdrop.raycastTarget = false;
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<RowPanelInset>();

            RendererRowViewFactory.CreateRows(contentList.transform);
            MaterialShaderRowViewFactory.CreateRows(contentList.transform);
            TextureRowViewFactory.CreateRows(contentList.transform);
            ColorRowViewFactory.CreateRows(contentList.transform);
            FloatKeywordRowViewFactory.CreateRows(contentList.transform);
            EnumVectorToggleRowViewFactory.CreateRows(contentList.transform);

            RowStyle.Apply(contentList.gameObject);
            RowLayoutCatalog.Apply(contentList.gameObject);
            return contentList.gameObject;
        }
    }
}
