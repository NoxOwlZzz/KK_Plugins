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
            contentList.gameObject.AddComponent<Mask>();
            contentList.color = RowColor;

            RendererRowViewFactory.CreateRows(contentList.transform);
            MaterialShaderRowViewFactory.CreateRows(contentList.transform);
            TextureRowViewFactory.CreateRows(contentList.transform);
            ColorRowViewFactory.CreateRows(contentList.transform);
            FloatKeywordRowViewFactory.CreateRows(contentList.transform);

            RowStyle.Apply(contentList.gameObject);
            RowLayoutCatalog.Apply(contentList.gameObject);
            CreateAdvancedPropertyAccent(contentList.transform);
            return contentList.gameObject;
        }

        private static void CreateAdvancedPropertyAccent(Transform parent)
        {
            var accent = MaterialEditorControlFactory.CreatePanel(
                "AdvancedPropertyAccent",
                parent);
            accent.color = MaterialEditorStyles.AdvancedPropertyAccentColor;
            accent.raycastTarget = false;
            accent.transform.SetRect(
                0f,
                0f,
                0f,
                1f,
                0f,
                MaterialEditorLayout.AdvancedPropertyAccentVerticalInset,
                MaterialEditorLayout.AdvancedPropertyAccentWidth,
                -MaterialEditorLayout.AdvancedPropertyAccentVerticalInset);

            var canvasGroup = accent.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            accent.transform.SetAsLastSibling();
            accent.gameObject.SetActive(false);
        }
    }
}
