using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class PropertyCategoryRowModel : RowModel
    {
        internal PropertyCategoryRowModel(string labelText)
            : base(RowItemType.PropertyCategory, labelText)
        {
        }

        internal bool Collapsed { get; set; }
        internal Action<bool> CollapsedOnChange { get; set; }
    }

    // Presentation-only contract for the second and final grouping level.
    // It organizes rows without changing their shader-property semantics.
    internal sealed class PropertySubcategoryRowModel : RowModel
    {
        internal PropertySubcategoryRowModel(string labelText)
            : base(RowItemType.PropertySubcategory, labelText)
        {
        }

        internal bool Collapsed { get; set; }
        internal Action<bool> CollapsedOnChange { get; set; }
    }

    internal sealed class TexturePropertyRowModel : RowModel
    {
        internal TexturePropertyRowModel(string labelText)
            : base(RowItemType.TextureProperty, labelText)
        {
        }

        internal bool Changed { get; set; }
        internal bool Exists { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action Export { get; set; }
        internal Action Import { get; set; }
        internal Action Reset { get; set; }
        internal Action RefreshState { get; set; }
    }

    internal sealed class TextureOffsetScaleRowModel : RowModel
    {
        internal TextureOffsetScaleRowModel()
            : base(RowItemType.TextureOffsetScale, string.Empty)
        {
        }

        internal Vector2 Offset { get; set; }
        internal Vector2 OriginalOffset { get; set; }
        internal Action<Vector2> OffsetOnChange { get; set; }
        internal Action OffsetOnReset { get; set; }
        internal Vector2 Scale { get; set; }
        internal Vector2 OriginalScale { get; set; }
        internal Action<Vector2> ScaleOnChange { get; set; }
        internal Action ScaleOnReset { get; set; }
    }

    internal sealed class CubemapPropertyRowModel : RowModel
    {
        internal CubemapPropertyRowModel(string labelText)
            : base(RowItemType.CubemapProperty, labelText)
        {
        }

        internal bool Changed { get; set; }
        internal bool Exists { get; set; }
        internal Action Export { get; set; }
        internal Action Import { get; set; }
        internal Action Reset { get; set; }
        internal Action RefreshState { get; set; }
    }
}
