using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class RowControlSet
    {
        private readonly List<RowControls> _rows;
        private readonly RowPanelInset _rowBackdropInset;

        private RowControlSet(RowBinder owner)
        {
            Owner = owner;
            _rowBackdropInset = owner.GetUIComponent<RowPanelInset>("RowBackdrop");
            Renderer = new RendererRowControls(owner);
            RendererEnabled = CreateToggle(owner, "RendererEnabled");
            RendererShadowCastingMode = new DropdownRowControls(
                owner.GetUIComponent<CanvasGroup>("RendererShadowCastingModePanel"),
                owner.GetUIComponent<Text>("RendererShadowCastingModeLabel"),
                owner.GetUIComponent<Dropdown>("RendererShadowCastingModeDropdown"),
                owner.GetUIComponent<Button>("RendererShadowCastingModeResetButton"));
            RendererReceiveShadows = CreateToggle(owner, "RendererReceiveShadows");
            RendererUpdateWhenOffscreen = CreateToggle(owner, "RendererUpdateWhenOffscreen");
            RendererRecalculateNormals = CreateToggle(owner, "RendererRecalculateNormals");
            Material = new MaterialRowControls(owner);
            Shader = new ShaderRowControls(owner);
            ShaderRenderQueue = new InputRowControls(
                owner.GetUIComponent<CanvasGroup>("ShaderRenderQueuePanel"),
                owner.GetUIComponent<Text>("ShaderRenderQueueLabel"),
                owner.GetUIComponent<LabelClickTrigger>("ShaderRenderQueueLabel"),
                owner.GetUIComponent<InputField>("ShaderRenderQueueInput"),
                owner.GetUIComponent<Button>("ShaderRenderQueueResetButton"));
            PropertyCategory = new PropertyCategoryRowControls(owner);
            PropertySubcategory = new PropertySubcategoryRowControls(owner);
            Texture = new TextureRowControls(owner);
            OffsetScale = new OffsetScaleRowControls(owner);
            Color = new ColorRowControls(owner);
            Float = new FloatRowControls(owner);
            Keyword = CreateToggle(owner, "Keyword", "KeywordLabel");
            FloatToggle = CreateToggle(
                owner,
                "FloatToggle",
                "FloatToggleLabel",
                "SelectInterpolableFloatToggleButton");
            Enum = new EnumRowControls(owner);
            Vector = new VectorRowControls(owner);

            _rows = new List<RowControls>
            {
                Renderer,
                RendererEnabled,
                RendererShadowCastingMode,
                RendererReceiveShadows,
                RendererUpdateWhenOffscreen,
                RendererRecalculateNormals,
                Material,
                Shader,
                ShaderRenderQueue,
                PropertyCategory,
                PropertySubcategory,
                Texture,
                OffsetScale,
                Color,
                Float,
                Keyword,
                FloatToggle,
                Enum,
                Vector
            };
        }

        internal RendererRowControls Renderer { get; }
        internal RowBinder Owner { get; }
        internal ToggleRowControls RendererEnabled { get; }
        internal DropdownRowControls RendererShadowCastingMode { get; }
        internal ToggleRowControls RendererReceiveShadows { get; }
        internal ToggleRowControls RendererUpdateWhenOffscreen { get; }
        internal ToggleRowControls RendererRecalculateNormals { get; }
        internal MaterialRowControls Material { get; }
        internal ShaderRowControls Shader { get; }
        internal InputRowControls ShaderRenderQueue { get; }
        internal PropertyCategoryRowControls PropertyCategory { get; }
        internal PropertySubcategoryRowControls PropertySubcategory { get; }
        internal TextureRowControls Texture { get; }
        internal OffsetScaleRowControls OffsetScale { get; }
        internal ColorRowControls Color { get; }
        internal FloatRowControls Float { get; }
        internal ToggleRowControls Keyword { get; }
        internal ToggleRowControls FloatToggle { get; }
        internal EnumRowControls Enum { get; }
        internal VectorRowControls Vector { get; }

        internal static RowControlSet Create(RowBinder owner)
        {
            return new RowControlSet(owner);
        }

        internal void HideAll()
        {
            foreach (var row in _rows)
                row.SetVisible(false);
        }

        // Apply a model's enabled state only to its active row family in the
        // pooled view.
        internal void SetEnabled(
            RowModel.RowItemType itemType,
            bool enabled)
        {
            // Category and Subcategory are structural controls and must stay
            // usable independently of a property row's enabled state.
            if (itemType == RowModel.RowItemType.PropertyCategory
                || itemType == RowModel.RowItemType.PropertySubcategory)
                return;

            RowControls row;
            if (!TryGetRow(itemType, out row))
                return;
            row.SetEnabled(enabled);
        }

        internal void SetHierarchyDepth(
            RowModel.RowItemType itemType,
            int depth)
        {
            var normalizedDepth = depth > 0 ? depth : 0;
            var left = MaterialEditorTheme.Metrics.SubcategoryContentIndent
                       * normalizedDepth;
            RowControls row;
            if (!TryGetRow(itemType, out row))
                return;
            var right = normalizedDepth > 0
                ? MaterialEditorTheme.Metrics.SubcategoryContentRightInset
                : 0f;
            var vertical = normalizedDepth > 0
                           && itemType == RowModel.RowItemType.PropertySubcategory
                ? MaterialEditorTheme.Metrics.SubcategoryHeaderVerticalInset
                : 0f;
            row.SetHierarchyInset(
                left,
                right,
                vertical);
            _rowBackdropInset.Configure(left, right, vertical);
        }

        private bool TryGetRow(
            RowModel.RowItemType itemType,
            out RowControls row)
        {
            switch (itemType)
            {
                case RowModel.RowItemType.Renderer:
                    row = Renderer;
                    break;
                case RowModel.RowItemType.RendererEnabled:
                    row = RendererEnabled;
                    break;
                case RowModel.RowItemType.RendererShadowCastingMode:
                    row = RendererShadowCastingMode;
                    break;
                case RowModel.RowItemType.RendererReceiveShadows:
                    row = RendererReceiveShadows;
                    break;
                case RowModel.RowItemType.RendererUpdateWhenOffscreen:
                    row = RendererUpdateWhenOffscreen;
                    break;
                case RowModel.RowItemType.RendererRecalculateNormals:
                    row = RendererRecalculateNormals;
                    break;
                case RowModel.RowItemType.Material:
                    row = Material;
                    break;
                case RowModel.RowItemType.Shader:
                    row = Shader;
                    break;
                case RowModel.RowItemType.ShaderRenderQueue:
                    row = ShaderRenderQueue;
                    break;
                case RowModel.RowItemType.PropertyCategory:
                    row = PropertyCategory;
                    break;
                case RowModel.RowItemType.PropertySubcategory:
                    row = PropertySubcategory;
                    break;
                case RowModel.RowItemType.TextureProperty:
                case RowModel.RowItemType.CubemapProperty:
                    row = Texture;
                    break;
                case RowModel.RowItemType.TextureOffsetScale:
                    row = OffsetScale;
                    break;
                case RowModel.RowItemType.ColorProperty:
                    row = Color;
                    break;
                case RowModel.RowItemType.FloatProperty:
                    row = Float;
                    break;
                case RowModel.RowItemType.KeywordProperty:
                    row = Keyword;
                    break;
                case RowModel.RowItemType.EnumProperty:
                    row = Enum;
                    break;
                case RowModel.RowItemType.VectorProperty:
                    row = Vector;
                    break;
                case RowModel.RowItemType.FloatToggleProperty:
                    row = FloatToggle;
                    break;
                default:
                    row = null;
                    return false;
            }
            return true;
        }

        private static ToggleRowControls CreateToggle(
            RowBinder owner,
            string prefix,
            string labelClickObjectName = null,
            string selectInterpolableObjectName = null)
        {
            return new ToggleRowControls(
                owner.GetUIComponent<CanvasGroup>($"{prefix}Panel"),
                owner.GetUIComponent<Text>($"{prefix}Label"),
                owner.GetUIComponent<Toggle>($"{prefix}Toggle"),
                owner.GetUIComponent<Button>($"{prefix}ResetButton"),
                labelClickObjectName == null
                    ? null
                    : owner.GetUIComponent<LabelClickTrigger>(labelClickObjectName),
                selectInterpolableObjectName == null
                    ? null
                    : owner.GetUIComponent<Button>(selectInterpolableObjectName));
        }

    }
}
