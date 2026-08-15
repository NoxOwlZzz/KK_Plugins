using UnityEngine;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorVisualState
    {
        Default,
        Hovered,
        Pressed,
        Selected,
        Focused,
        Disabled,
        Mixed,
        Modified
    }

    // Internal, immutable visual tokens for the programmatic uGUI surface.
    // This is one fixed dark presentation. Colors describe structural roles
    // only; they are never selected from a renderer, material, shader or
    // category name.
    internal static class MaterialEditorTheme
    {
        internal static class Colors
        {
            internal static readonly Color Window = Rgb(0x1E, 0x23, 0x2B);
            internal static readonly Color LeftPanel = Rgb(0x20, 0x26, 0x2F);
            internal static readonly Color CenterPanel = Rgb(0x24, 0x2B, 0x34);
            internal static readonly Color RightPanel = Rgb(0x22, 0x28, 0x32);
            internal static readonly Color NeutralHeader = Rgb(0x2A, 0x31, 0x3B);
            internal static readonly Color PropertyRow = Rgb(0x2A, 0x31, 0x3B);
            internal static readonly Color PropertyRowAlternate = Rgb(0x2D, 0x35, 0x40);
            internal static readonly Color InputSurface = Rgb(0x16, 0x1B, 0x22);
            internal static readonly Color DropdownSurface = Rgb(0x16, 0x1B, 0x22);
            internal static readonly Color PopupSurface = Rgb(0x16, 0x1B, 0x22);

            internal static readonly Color RendererHeader = Rgb(0x2F, 0x4F, 0x74);
            internal static readonly Color MaterialHeader = Rgb(0x5A, 0x4A, 0x73);
            internal static readonly Color ShaderHeader = Rgb(0x36, 0x5C, 0x73);
            internal static readonly Color CategoryHeader = Rgb(0x6F, 0x5A, 0x8E);
            internal static readonly Color CategoryHeaderHover = Rgb(0x76, 0x5F, 0x97);
            internal static readonly Color CategoryHeaderExpanded = Rgb(0x5F, 0x4C, 0x7D);
            // A neutral, lower-contrast child header keeps Category as the
            // dominant landmark in any manifest that opts into hierarchy.
            internal static readonly Color SubcategoryHeader = Rgb(0x3A, 0x42, 0x4C);
            internal static readonly Color SubcategoryHeaderHover = Rgb(0x44, 0x4D, 0x58);
            internal static readonly Color SubcategoryHeaderExpanded = Rgb(0x34, 0x3B, 0x44);

            internal static readonly Color Hover = Rgb(0x33, 0x46, 0x5C);
            internal static readonly Color Pressed = Rgb(0x39, 0x45, 0x53);
            internal static readonly Color Selected = Rgb(0x3A, 0x74, 0xA8);
            internal static readonly Color SelectedText = Rgb(0xFF, 0xFF, 0xFF);
            internal static readonly Color DisabledSurface = Rgb(0x20, 0x26, 0x2F);
            internal static readonly Color ModifiedIndicator = Rgb(0xD2, 0x94, 0x28);
            internal static readonly Color ModifiedSurface = Rgb(0x4A, 0x39, 0x20);

            internal static readonly Color Primary = Rgb(0xE6, 0xEC, 0xF2);
            internal static readonly Color Secondary = Rgb(0xAA, 0xB6, 0xC3);
            internal static readonly Color Disabled = Rgb(0x8A, 0x92, 0x9D);
            internal static readonly Color Accent = Rgb(0x4A, 0xA3, 0xFF);
            internal static readonly Color InputBorder = Rgb(0x8D, 0x9B, 0xAA);
            internal static readonly Color StrongBorder = Rgb(0x73, 0x82, 0x91);
            internal static readonly Color Divider = Rgb(0x3E, 0x4A, 0x58);
            internal static readonly Color HandlePressed = Rgb(0x8D, 0xC6, 0xFF);
            internal static readonly Color Warning = Rgb(0xD7, 0xA4, 0x4A);
            internal static readonly Color Error = Rgb(0xDD, 0x66, 0x70);
            internal static readonly Color Success = Rgb(0x62, 0xB9, 0x85);

            // Compatibility aliases for existing view factories. New code
            // should prefer the structural names above.
            internal static readonly Color Panel = CenterPanel;
            internal static readonly Color Raised = InputSurface;
            internal static readonly Color Header = NeutralHeader;
            internal static readonly Color Row = PropertyRow;
            internal static readonly Color Border = Divider;

            // Semantic aliases keep view code independent of concrete controls.
            // uGUI multiplies a Selectable ColorBlock by Graphic.color, so the
            // graphic base must remain the neutral multiplication identity.
            internal static readonly Color TintIdentity = new Color(1f, 1f, 1f, 1f);
            internal static readonly Color MainPanel = CenterPanel;
            internal static readonly Color SidePanel = RightPanel;
            internal static readonly Color NavigatorShaderHeader = ShaderHeader;
            internal static readonly Color RendererRow = RendererHeader;
            internal static readonly Color MaterialRow = MaterialHeader;
            internal static readonly Color ShaderRow = ShaderHeader;
            internal static readonly Color CategoryRow = CategoryHeader;
            internal static readonly Color SubcategoryRow = SubcategoryHeader;
            internal static readonly Color TransparentRow = WithAlpha(PropertyRow, 0f);
            internal static readonly Color ChangedRow = ModifiedSurface;
            internal static readonly Color ControlNormal = InputSurface;
            internal static readonly Color ControlHover = Hover;
            internal static readonly Color ControlPressed = Pressed;
            internal static readonly Color ControlDisabled = DisabledSurface;
            internal static readonly Color ToggleMark = SelectedText;
            internal static readonly Color SliderTrack = Divider;
            internal static readonly Color SliderFill = Accent;
            internal static readonly Color SliderHandle = Primary;
            internal static readonly Color SliderHandlePressed = HandlePressed;
            internal static readonly Color ScrollSurface = CenterPanel;
            internal static readonly Color ScrollbarTrack = RightPanel;
            internal static readonly Color Scrollbar = StrongBorder;
            internal static readonly Color ScrollbarHandle = Secondary;
            internal static readonly Color ScrollbarHandlePressed = HandlePressed;
            internal static readonly Color PlaceholderText = Secondary;
            internal static readonly Color ShaderHintUnderline = Accent;
            internal static readonly Color TooltipSurface = WithAlpha(PopupSurface, 0.98f);
            internal static readonly Color PrimaryText = Primary;
            internal static readonly Color SecondaryText = Secondary;
            internal static readonly Color DisabledText = Disabled;
            internal static readonly Color Outline = StrongBorder;

            private static Color Rgb(int red, int green, int blue)
            {
                return new Color(
                    red / 255f,
                    green / 255f,
                    blue / 255f,
                    1f);
            }

            private static Color WithAlpha(Color color, float alpha)
            {
                return new Color(color.r, color.g, color.b, alpha);
            }
        }

        internal static class Metrics
        {
            internal const float CanvasReferenceWidth = 1920f;
            internal const float CanvasReferenceHeight = 1080f;
            internal const float UiScaleMinimum = 1f;
            internal const float UiScaleDefault = 1.75f;
            internal const float UiScaleMaximum = 3f;
            internal const float WindowWidthMinimum = 0f;
            internal const float WindowWidthDefault = 0.33f;
            internal const float WindowWidthMaximum = 1f;
            internal const float WindowHeightMinimum = 0f;
            internal const float WindowHeightDefault = 0.3f;
            internal const float WindowHeightMaximum = 1f;
            internal const float Margin = 5f;
            internal const float HeaderHeight = 20f;
            internal const float TopBarHeight = HeaderHeight * 2f;
            internal const float WindowHeaderRecoveryWidth = 96f;
            internal const float SectionHeaderHeight = HeaderHeight;
            internal const float ScrollbarOffset = -15f;
            internal const float RowHeight = 22f;
            internal const float CategoryNavigatorWidth = 150f;
            internal const float CategoryNavigatorCollapsedWidth = 24f;
            internal const float CategoryActiveMarkerWidth = 3f;
            internal const float SelectionPanelCollapsedWidth = 24f;
            internal const float SelectionPanelHeaderHeight = HeaderHeight;
            internal const float SelectionPanelFilterHeight = HeaderHeight;
            internal const float SidePanelMinimumWidth = 100f;
            internal const float SidePanelDefaultWidth = 180f;
            internal const float SidePanelMaximumWidth = 500f;
            // The mode-row search spans x=177..right-91; 500 keeps 232 px usable.
            internal const float ResponsiveMinimumMainWidth = 500f;
            // Topbar plus one full row in each half-height selection/Rename list.
            internal const float ResponsiveMinimumMainHeight = 138f;
            internal const float ResponsiveOuterMarginFraction = 0.05f;

            internal const float LabelWidth = 0f;
            internal const float ButtonWidth = 100f;
            internal const float SmallButtonWidth = 20f;
            internal const float ResetButtonWidth = SmallButtonWidth;
            internal const float InterpolableButtonWidth = SmallButtonWidth;
            internal const float ContentWidth = 316f;
            internal const float SubcategoryHeaderIndent = 12f;
            internal const float SubcategoryHeaderRightInset = 2f;
            internal const float SubcategoryHeaderVerticalInset = 1f;
            internal const float SubcategoryContentIndent = SubcategoryHeaderIndent;
            internal const float SubcategoryContentRightInset = SubcategoryHeaderRightInset;
            internal const float FoldIndicatorWidth = 16f;
            internal const float RendererButtonWidth = ButtonWidth;
            internal const float RendererToggleWidth = 20f;
            internal const float RendererDropdownWidth = 94f;
            internal const float MaterialButtonWidth = ButtonWidth * 0.75f;
            internal const float MaterialRenameButtonWidth = SmallButtonWidth;
            internal const float ShaderLabelMinimumWidth = 70f;
            internal const float ShaderDropdownMinimumWidth = 220f;
            internal const float ShaderDropdownWidth = ContentWidth;
            internal const float RenderQueueInputWidth = 94f;
            internal const float OffsetScaleLabelXWidth = 48f;
            internal const float OffsetScaleLabelYWidth = 10f;
            internal const float OffsetScaleInputWidth = 50f;
            internal const float OffsetScaleGroupSpacing = 4f;
            internal const float ColorLabelWidth = 10f;
            internal const float ColorInputWidth = 64f;
            internal const float ColorEditButtonWidth = 20f;
            internal const float FloatSliderWidth = ContentWidth - 94f;
            internal const float FloatInputWidth = 94f;
            internal const float VectorComponentLabelWidth = 14f;
            internal const float VectorComponentInputWidth = 58f;
            internal const float KeywordToggleWidth = ContentWidth;

            internal const float DropdownTemplateWidth = 100f;
            internal const float TooltipWidth = 280f;
            internal const float TooltipMaximumHeight = 360f;
            internal const float TooltipDelaySeconds = 0f;
            internal const float SelectionPanelTitleFraction = 0.4f;
            internal const float SelectionToggleSize = 18f;
            internal const float IconSize = 16f;
            internal const float CloseGlyphInset = 8f;
            internal const float CloseGlyphAngle = 45f;
            internal const float ShaderHintDashWidth = 3f;
            internal const float ShaderHintDashGap = 2f;
            internal const float ShaderHintLineThickness = 2f;
            internal const float ShaderHintUnderlineRise = 2f;
        }

        internal static class Typography
        {
            internal const int PrimaryFontSize = 16;
            internal const int SecondaryFontSize = 14;
            internal const int IconFontSize = 16;
            internal const int InputMinimumFontSize = 2;
            internal const int DropdownFontSize = 16;
            internal const int DropdownMinimumFontSize = 12;
            internal const int VectorComponentFontSize = 16;
            internal const int VectorComponentMinimumFontSize = 12;
            internal const int PropertyLabelMinimumFontSize = 12;
            internal const int PropertyCategoryMinimumFontSize = 12;
            internal const int SelectionNameMinimumFontSize = 12;
            internal const int TooltipFontSize = 11;
        }

        internal static class Spacing
        {
            internal const float Horizontal = 2f;
            internal const float Vertical = 1f;
            internal const float Control = 2f;
            internal const float TopBarHorizontalInset = 3f;
            internal const float Section = 5f;
            internal const int PropertyLabelInset = 3;
            internal const int RowPaddingLeft = 1;
            internal const int RowPaddingRight = 1;
            internal const int RowPaddingTop = 1;
            internal const int RowPaddingBottom = 1;
            internal const int NavigatorContentPadding = 0;
            internal const float NavigatorListSpacing = 1f;
            internal const int CategoryEntryPadding = 1;
            internal const float CategoryEntrySpacing = 2f;
            internal const float PropertyCategorySpacing = 2f;
            internal const float PropertySubcategorySpacing = 2f;
            internal const int TooltipHorizontalPadding = 4;
            internal const int TooltipVerticalPadding = 2;
            internal const float TooltipCursorOffset = 5f;
            internal const float DropdownTextVerticalInset = 1f;
            internal const float DropdownCaptionLeftInset = 5f;
            internal const float DropdownCaptionRightInset = 15f;
            internal const float DropdownCaptionVerticalInset = 2f;
            internal const float SelectionPanelContentInset = 2f;
            internal const float SelectionPanelTitleInset = 5f;
            internal const float SelectionToggleInset = 1f;
        }

        internal static class Glyphs
        {
            internal const string FoldCollapsed = "\u25B8";
            internal const string FoldExpanded = "\u25BE";
            internal const string AllFolded = FoldCollapsed + FoldCollapsed;
            internal const string AllExpanded = FoldExpanded + FoldExpanded;
            internal const string Reset = "R";
            internal const string MaterialCollapsed = "+";
            internal const string MaterialExpanded = "-";
            internal const string ChevronRight = ">";
            internal const string ChevronLeft = "<";
            internal const string Interpolable = "O";
        }

        internal static class States
        {
            internal const float VisibleAlpha = 1f;
            internal const float DisabledAlpha = 0.55f;
            internal const float HiddenAlpha = 0f;

            internal static readonly Color DefaultSurface = Colors.Row;
            internal static readonly Color HoveredSurface = Colors.Hover;
            internal static readonly Color PressedSurface = Colors.Pressed;
            internal static readonly Color SelectedSurface = Colors.Selected;
            internal static readonly Color FocusedIndicator = Colors.Accent;
            internal static readonly Color DisabledText = Colors.Disabled;
            internal static readonly Color MixedIndicator = Colors.Secondary;
            internal static readonly Color ModifiedSurface = Colors.ChangedRow;
        }
    }
}
