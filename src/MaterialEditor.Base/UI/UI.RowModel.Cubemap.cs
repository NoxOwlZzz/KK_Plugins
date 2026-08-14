using System;

namespace MaterialEditorAPI
{
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
