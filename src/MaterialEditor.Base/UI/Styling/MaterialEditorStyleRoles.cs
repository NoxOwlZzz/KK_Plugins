using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorTextRole
    {
        PreserveHorizontal,
        Title,
        Chrome,
        SecondaryChrome,
        Label,
        CenteredLabel,
        Button,
        Input,
        Placeholder,
        Tooltip
    }

    internal enum MaterialEditorPanelRole
    {
        Default,
        Window,
        Main,
        CenterPanel,
        Header,
        SidePanel,
        LeftPanel,
        RightPanel,
        Row,
        PropertyRow,
        AlternatePropertyRow,
        RendererRow,
        MaterialRow,
        ShaderRow,
        CategoryRow,
        SubcategoryRow,
        SelectedRow,
        HoverRow,
        DisabledRow,
        ModifiedRow,
        TransparentRow,
        RowStencilMask,
        StencilMask,
        RowBackdrop
    }
}
