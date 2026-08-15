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
            var contentList = MaterialEditorControlFactory.CreatePanel("ListEntry", parent);
            contentList.gameObject.AddComponent<LayoutElement>().preferredHeight = PanelHeight;
            var mask = contentList.gameObject.AddComponent<Mask>();
            // The root clips the active child panel but must not paint a
            // full-width band behind an indented Subcategory child.
            mask.showMaskGraphic = false;
            contentList.color = RowColor;

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
