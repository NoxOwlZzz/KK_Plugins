using MaterialEditorAPI;
using UnityEngine;

internal static class UiThemeTokenContractTests
{
    internal static void Run()
    {
        PaletteComponentsAreFiniteAndNormalized();
        MetricsTypographyAndSpacingAreCoherent();
        SemanticTextHasBasicContrast();
        VisualStatesAreOrderedAndComplete();
        VirtualizedRowsShareOneAuthoritativeHeight();
        ProductionViewsConsumeTheThemeSeam();
        Console.WriteLine("Material Editor UI theme-token contract guards passed.");
    }

    private static void PaletteComponentsAreFiniteAndNormalized()
    {
        var colors = new[]
        {
            MaterialEditorTheme.Colors.Window,
            MaterialEditorTheme.Colors.LeftPanel,
            MaterialEditorTheme.Colors.CenterPanel,
            MaterialEditorTheme.Colors.RightPanel,
            MaterialEditorTheme.Colors.NeutralHeader,
            MaterialEditorTheme.Colors.PropertyRow,
            MaterialEditorTheme.Colors.PropertyRowAlternate,
            MaterialEditorTheme.Colors.InputSurface,
            MaterialEditorTheme.Colors.DropdownSurface,
            MaterialEditorTheme.Colors.PopupSurface,
            MaterialEditorTheme.Colors.RendererHeader,
            MaterialEditorTheme.Colors.MaterialHeader,
            MaterialEditorTheme.Colors.ShaderHeader,
            MaterialEditorTheme.Colors.CategoryHeader,
            MaterialEditorTheme.Colors.CategoryHeaderHover,
            MaterialEditorTheme.Colors.CategoryHeaderExpanded,
            MaterialEditorTheme.Colors.Panel,
            MaterialEditorTheme.Colors.Raised,
            MaterialEditorTheme.Colors.Header,
            MaterialEditorTheme.Colors.Row,
            MaterialEditorTheme.Colors.Hover,
            MaterialEditorTheme.Colors.Pressed,
            MaterialEditorTheme.Colors.Selected,
            MaterialEditorTheme.Colors.SelectedText,
            MaterialEditorTheme.Colors.DisabledSurface,
            MaterialEditorTheme.Colors.ModifiedIndicator,
            MaterialEditorTheme.Colors.ModifiedSurface,
            MaterialEditorTheme.Colors.Border,
            MaterialEditorTheme.Colors.InputBorder,
            MaterialEditorTheme.Colors.StrongBorder,
            MaterialEditorTheme.Colors.Divider,
            MaterialEditorTheme.Colors.HandlePressed,
            MaterialEditorTheme.Colors.Primary,
            MaterialEditorTheme.Colors.Secondary,
            MaterialEditorTheme.Colors.Disabled,
            MaterialEditorTheme.Colors.Accent,
            MaterialEditorTheme.Colors.Warning,
            MaterialEditorTheme.Colors.Error,
            MaterialEditorTheme.Colors.Success,
            MaterialEditorTheme.Colors.TooltipSurface,
            MaterialEditorTheme.Colors.ChangedRow
        };

        for (var index = 0; index < colors.Length; index++)
            AssertNormalized(colors[index], "theme color " + index);
    }

    private static void MetricsTypographyAndSpacingAreCoherent()
    {
        var positiveMetrics = new[]
        {
            MaterialEditorTheme.Metrics.CanvasReferenceWidth,
            MaterialEditorTheme.Metrics.CanvasReferenceHeight,
            MaterialEditorTheme.Metrics.Margin,
            MaterialEditorTheme.Metrics.HeaderHeight,
            MaterialEditorTheme.Metrics.TopBarHeight,
            MaterialEditorTheme.Metrics.WindowHeaderRecoveryWidth,
            MaterialEditorTheme.Metrics.SectionHeaderHeight,
            MaterialEditorTheme.Metrics.RowHeight,
            MaterialEditorTheme.Metrics.CategoryNavigatorWidth,
            MaterialEditorTheme.Metrics.SidePanelMinimumWidth,
            MaterialEditorTheme.Metrics.SidePanelDefaultWidth,
            MaterialEditorTheme.Metrics.SidePanelMaximumWidth,
            MaterialEditorTheme.Metrics.ButtonWidth,
            MaterialEditorTheme.Metrics.SmallButtonWidth,
            MaterialEditorTheme.Metrics.ResetButtonWidth,
            MaterialEditorTheme.Metrics.InterpolableButtonWidth,
            MaterialEditorTheme.Metrics.ContentWidth,
            MaterialEditorTheme.Metrics.RendererButtonWidth,
            MaterialEditorTheme.Metrics.RendererToggleWidth,
            MaterialEditorTheme.Metrics.RendererDropdownWidth,
            MaterialEditorTheme.Metrics.MaterialButtonWidth,
            MaterialEditorTheme.Metrics.MaterialRenameButtonWidth,
            MaterialEditorTheme.Metrics.ShaderLabelMinimumWidth,
            MaterialEditorTheme.Metrics.ShaderDropdownMinimumWidth,
            MaterialEditorTheme.Metrics.ShaderDropdownWidth,
            MaterialEditorTheme.Metrics.RenderQueueInputWidth,
            MaterialEditorTheme.Metrics.OffsetScaleLabelXWidth,
            MaterialEditorTheme.Metrics.OffsetScaleLabelYWidth,
            MaterialEditorTheme.Metrics.OffsetScaleInputWidth,
            MaterialEditorTheme.Metrics.OffsetScaleGroupSpacing,
            MaterialEditorTheme.Metrics.ColorLabelWidth,
            MaterialEditorTheme.Metrics.ColorInputWidth,
            MaterialEditorTheme.Metrics.ColorEditButtonWidth,
            MaterialEditorTheme.Metrics.FloatSliderWidth,
            MaterialEditorTheme.Metrics.FloatInputWidth,
            MaterialEditorTheme.Metrics.VectorComponentLabelWidth,
            MaterialEditorTheme.Metrics.VectorComponentInputWidth,
            MaterialEditorTheme.Metrics.KeywordToggleWidth,
            MaterialEditorTheme.Metrics.DropdownTemplateWidth,
            MaterialEditorTheme.Metrics.TooltipWidth,
            MaterialEditorTheme.Metrics.TooltipMaximumHeight,
            MaterialEditorTheme.Metrics.SelectionToggleSize,
            MaterialEditorTheme.Metrics.IconSize,
            MaterialEditorTheme.Metrics.CloseGlyphInset,
            MaterialEditorTheme.Metrics.ShaderHintDashWidth,
            MaterialEditorTheme.Metrics.ShaderHintDashGap,
            MaterialEditorTheme.Metrics.ShaderHintLineThickness,
            MaterialEditorTheme.Metrics.ShaderHintUnderlineRise
        };
        for (var index = 0; index < positiveMetrics.Length; index++)
            Positive(positiveMetrics[index], "positive metric " + index);

        NonNegative(MaterialEditorTheme.Metrics.LabelWidth, "flexible label width");
        Equal(70f, MaterialEditorTheme.Metrics.ShaderLabelMinimumWidth, "shader label minimum width");
        Equal(220f, MaterialEditorTheme.Metrics.ShaderDropdownMinimumWidth, "shader dropdown minimum width");
        if (MaterialEditorTheme.Metrics.ScrollbarOffset >= 0f)
            throw new InvalidOperationException("Scrollbar offset must reserve trailing space.");
        if (MaterialEditorTheme.Metrics.SelectionPanelTitleFraction <= 0f
            || MaterialEditorTheme.Metrics.SelectionPanelTitleFraction >= 1f)
            throw new InvalidOperationException("Selection title fraction must be inside (0, 1).");
        if (MaterialEditorTheme.Metrics.CloseGlyphAngle <= 0f
            || MaterialEditorTheme.Metrics.CloseGlyphAngle >= 90f)
            throw new InvalidOperationException("Close glyph angle must remain acute.");

        Ordered(
            MaterialEditorTheme.Metrics.UiScaleMinimum,
            MaterialEditorTheme.Metrics.UiScaleDefault,
            MaterialEditorTheme.Metrics.UiScaleMaximum,
            "UI scale");
        Ordered(
            MaterialEditorTheme.Metrics.WindowWidthMinimum,
            MaterialEditorTheme.Metrics.WindowWidthDefault,
            MaterialEditorTheme.Metrics.WindowWidthMaximum,
            "window width");
        Ordered(
            MaterialEditorTheme.Metrics.WindowHeightMinimum,
            MaterialEditorTheme.Metrics.WindowHeightDefault,
            MaterialEditorTheme.Metrics.WindowHeightMaximum,
            "window height");

        if (!(MaterialEditorTheme.Metrics.SidePanelMinimumWidth
              <= MaterialEditorTheme.Metrics.SidePanelDefaultWidth
              && MaterialEditorTheme.Metrics.SidePanelDefaultWidth
              <= MaterialEditorTheme.Metrics.SidePanelMaximumWidth))
        {
            throw new InvalidOperationException(
                "Side-panel widths must be ordered minimum <= default <= maximum.");
        }

        if (MaterialEditorTheme.Metrics.TooltipWidth
            <= MaterialEditorTheme.Spacing.TooltipHorizontalPadding * 2f)
        {
            throw new InvalidOperationException(
                "Tooltip width must exceed its horizontal padding.");
        }

        if (MaterialEditorTheme.Typography.PrimaryFontSize <= 0
            || MaterialEditorTheme.Typography.SecondaryFontSize <= 0
            || MaterialEditorTheme.Typography.IconFontSize <= 0
            || MaterialEditorTheme.Typography.TooltipFontSize <= 0
            || MaterialEditorTheme.Typography.InputMinimumFontSize <= 0
            || MaterialEditorTheme.Typography.DropdownFontSize <= 0
            || MaterialEditorTheme.Typography.DropdownMinimumFontSize <= 0
            || MaterialEditorTheme.Typography.VectorComponentFontSize <= 0
            || MaterialEditorTheme.Typography.VectorComponentMinimumFontSize <= 0
            || MaterialEditorTheme.Typography.DropdownMinimumFontSize
               > MaterialEditorTheme.Typography.DropdownFontSize
            || MaterialEditorTheme.Typography.VectorComponentMinimumFontSize
               > MaterialEditorTheme.Typography.VectorComponentFontSize
            || MaterialEditorTheme.Typography.SecondaryFontSize
               > MaterialEditorTheme.Typography.PrimaryFontSize)
        {
            throw new InvalidOperationException("Theme typography ranges are incoherent.");
        }

        NonNegative(MaterialEditorTheme.Spacing.Horizontal, "horizontal spacing");
        NonNegative(MaterialEditorTheme.Spacing.Vertical, "vertical spacing");
        NonNegative(MaterialEditorTheme.Spacing.Control, "control spacing");
        NonNegative(MaterialEditorTheme.Spacing.TopBarHorizontalInset, "topbar outer gutter");
        NonNegative(MaterialEditorTheme.Spacing.Section, "section spacing");
        NonNegative(MaterialEditorTheme.Metrics.TooltipDelaySeconds, "tooltip delay");
        NonNegative(MaterialEditorTheme.Spacing.PropertyLabelInset, "property label inset");
        NonNegative(MaterialEditorTheme.Spacing.RowPaddingLeft, "row left padding");
        NonNegative(MaterialEditorTheme.Spacing.RowPaddingRight, "row right padding");
        NonNegative(MaterialEditorTheme.Spacing.RowPaddingTop, "row top padding");
        NonNegative(MaterialEditorTheme.Spacing.RowPaddingBottom, "row bottom padding");
        NonNegative(MaterialEditorTheme.Spacing.NavigatorContentPadding, "navigator content padding");
        NonNegative(MaterialEditorTheme.Spacing.NavigatorListSpacing, "navigator list spacing");
        NonNegative(MaterialEditorTheme.Spacing.CategoryEntryPadding, "category entry padding");
        NonNegative(MaterialEditorTheme.Spacing.CategoryEntrySpacing, "category entry spacing");
        NonNegative(MaterialEditorTheme.Spacing.PropertyCategorySpacing, "property category spacing");
        NonNegative(MaterialEditorTheme.Spacing.TooltipHorizontalPadding, "tooltip horizontal padding");
        NonNegative(MaterialEditorTheme.Spacing.TooltipVerticalPadding, "tooltip vertical padding");
        NonNegative(MaterialEditorTheme.Spacing.TooltipCursorOffset, "tooltip cursor offset");
        NonNegative(MaterialEditorTheme.Spacing.DropdownTextVerticalInset, "dropdown text vertical inset");
        NonNegative(MaterialEditorTheme.Spacing.DropdownCaptionLeftInset, "dropdown left inset");
        NonNegative(MaterialEditorTheme.Spacing.DropdownCaptionRightInset, "dropdown right inset");
        NonNegative(MaterialEditorTheme.Spacing.DropdownCaptionVerticalInset, "dropdown vertical inset");
        NonNegative(MaterialEditorTheme.Spacing.SelectionPanelContentInset, "selection content inset");
        NonNegative(MaterialEditorTheme.Spacing.SelectionPanelTitleInset, "selection title inset");
        NonNegative(MaterialEditorTheme.Spacing.SelectionToggleInset, "selection toggle inset");
    }

    private static void SemanticTextHasBasicContrast()
    {
        AtLeast(0.8f, MaterialEditorTheme.Colors.Primary.a, "primary text alpha");
        AtLeast(0.8f, MaterialEditorTheme.Colors.Secondary.a, "secondary text alpha");
        AtLeast(0.8f, MaterialEditorTheme.Colors.Disabled.a, "disabled text alpha");
        AtLeast(0.8f, MaterialEditorTheme.Colors.Window.a, "window alpha");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.Primary, MaterialEditorTheme.Colors.Window),
            "primary text/window contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.Primary, MaterialEditorTheme.Colors.InputSurface),
            "primary text/input contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.Primary, MaterialEditorTheme.Colors.DropdownSurface),
            "primary text/dropdown contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.Primary, MaterialEditorTheme.Colors.PopupSurface),
            "primary text/popup contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.Secondary, MaterialEditorTheme.Colors.Window),
            "secondary text/window contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.SecondaryText, MaterialEditorTheme.Colors.Pressed),
            "secondary text/pressed control contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.PlaceholderText, MaterialEditorTheme.Colors.Hover),
            "placeholder text/hovered input contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.PlaceholderText, MaterialEditorTheme.Colors.Pressed),
            "placeholder text/pressed input contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.Disabled, MaterialEditorTheme.Colors.InputSurface),
            "disabled text/input contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.Disabled, MaterialEditorTheme.Colors.DisabledSurface),
            "disabled text/disabled button contrast");
        AtLeast(
            4.5f,
            Contrast(MaterialEditorTheme.Colors.SelectedText, MaterialEditorTheme.Colors.Selected),
            "selected text/selection contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.Selected, MaterialEditorTheme.Colors.PopupSurface),
            "selection/popup boundary contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.InputBorder, MaterialEditorTheme.Colors.InputSurface),
            "input boundary contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.InputBorder, MaterialEditorTheme.Colors.Hover),
            "hovered input boundary contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.InputBorder, MaterialEditorTheme.Colors.Pressed),
            "pressed input boundary contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.InputBorder, MaterialEditorTheme.Colors.DisabledSurface),
            "disabled input boundary contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.StrongBorder, MaterialEditorTheme.Colors.CenterPanel),
            "strong panel boundary contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.SliderHandlePressed, MaterialEditorTheme.Colors.SliderTrack),
            "pressed slider handle/track contrast");
        AtLeast(
            3f,
            Contrast(MaterialEditorTheme.Colors.ScrollbarHandlePressed, MaterialEditorTheme.Colors.ScrollbarTrack),
            "pressed scrollbar handle/track contrast");
        AssertDistinct(
            new[]
            {
                MaterialEditorTheme.Colors.Window,
                MaterialEditorTheme.Colors.LeftPanel,
                MaterialEditorTheme.Colors.CenterPanel,
                MaterialEditorTheme.Colors.RightPanel
            },
            "window/left/center/right structure");
        AssertDistinct(
            new[]
            {
                MaterialEditorTheme.Colors.RendererHeader,
                MaterialEditorTheme.Colors.MaterialHeader,
                MaterialEditorTheme.Colors.ShaderHeader,
                MaterialEditorTheme.Colors.CategoryHeader,
                MaterialEditorTheme.Colors.PropertyRow
            },
            "renderer/material/shader/category/property structure");
    }

    private static void VisualStatesAreOrderedAndComplete()
    {
        if (MaterialEditorTheme.States.HiddenAlpha != 0f
            || MaterialEditorTheme.States.VisibleAlpha != 1f
            || MaterialEditorTheme.States.DisabledAlpha
               <= MaterialEditorTheme.States.HiddenAlpha
            || MaterialEditorTheme.States.DisabledAlpha
               >= MaterialEditorTheme.States.VisibleAlpha)
        {
            throw new InvalidOperationException(
                "Hidden, disabled and visible alpha tokens must remain strictly ordered.");
        }

        var expectedStates = new[]
        {
            MaterialEditorVisualState.Default,
            MaterialEditorVisualState.Hovered,
            MaterialEditorVisualState.Pressed,
            MaterialEditorVisualState.Selected,
            MaterialEditorVisualState.Focused,
            MaterialEditorVisualState.Disabled,
            MaterialEditorVisualState.Mixed,
            MaterialEditorVisualState.Modified
        };
        var actualStates = (MaterialEditorVisualState[])Enum.GetValues(
            typeof(MaterialEditorVisualState));
        if (actualStates.Length != expectedStates.Length)
            throw new InvalidOperationException("The common visual-state vocabulary changed.");
        for (var index = 0; index < expectedStates.Length; index++)
        {
            if (actualStates[index] != expectedStates[index])
                throw new InvalidOperationException("The visual-state order changed at " + index + ".");
        }

        AssertNormalized(MaterialEditorTheme.States.DefaultSurface, "default state");
        AssertNormalized(MaterialEditorTheme.States.HoveredSurface, "hovered state");
        AssertNormalized(MaterialEditorTheme.States.PressedSurface, "pressed state");
        AssertNormalized(MaterialEditorTheme.States.SelectedSurface, "selected state");
        AssertNormalized(MaterialEditorTheme.States.FocusedIndicator, "focused state");
        AssertNormalized(MaterialEditorTheme.States.DisabledText, "disabled state");
        AssertNormalized(MaterialEditorTheme.States.MixedIndicator, "mixed state");
        AssertNormalized(MaterialEditorTheme.States.ModifiedSurface, "modified state");
        if (SameColor(
                MaterialEditorTheme.States.MixedIndicator,
                MaterialEditorTheme.States.ModifiedSurface))
            throw new InvalidOperationException("Mixed and Modified states must remain distinct.");
        AssertDistinct(
            new[]
            {
                MaterialEditorTheme.States.DefaultSurface,
                MaterialEditorTheme.States.HoveredSurface,
                MaterialEditorTheme.States.PressedSurface,
                MaterialEditorTheme.States.SelectedSurface
            },
            "surface state");
        AssertDistinct(
            new[]
            {
                MaterialEditorTheme.States.DisabledText,
                MaterialEditorTheme.States.MixedIndicator,
                MaterialEditorTheme.States.ModifiedSurface
            },
            "disabled/mixed/modified state");
    }

    private static void VirtualizedRowsShareOneAuthoritativeHeight()
    {
        Equal(
            MaterialEditorTheme.Metrics.RowHeight,
            MaterialEditorUI.PanelHeight,
            "test/runtime virtual row height");

        var style = ReadSource("src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");
        Contains(
            style,
            "RowHeight = MaterialEditorTheme.Metrics.RowHeight",
            "layout adapter row height");
        var ui = ReadSource("src", "MaterialEditor.Base", "UI", "UI.cs");
        Contains(
            ui,
            "PanelHeight = MaterialEditorLayout.RowHeight",
            "MaterialEditorUI row height adapter");
        var factory = ReadSource("src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.cs");
        Contains(factory, "preferredHeight = PanelHeight", "pooled template row height");
        var virtualList = ReadSource("src", "MaterialEditor.Base", "UI", "UI.VirtualList.cs");
        Contains(virtualList, "PanelHeight", "VirtualList uniform row stride");
    }

    private static void ProductionViewsConsumeTheThemeSeam()
    {
        var projectItems = ReadSource("src", "MaterialEditor.Base", "MaterialEditor.Base.projitems");
        Contains(projectItems, "UI\\UI.Theme.cs", "production shared-project theme include");

        var themeConsumers = new[]
        {
            "UI.StyleSystem.cs",
            "UI.RowStyle.cs",
            "UI.WindowView.cs",
            "UI.TopBarView.cs",
            "UI.PopupMenu.cs",
            "UI.CategoryNavigator.cs",
            "UI.SelectListPanel.cs",
            "UI.TooltipManager.cs",
            "UI.RowControls.cs",
            "UI.RowBinder.MaterialShader.cs",
            "UI.RowBinder.Texture.cs",
            "UI.RowViewFactory.Support.cs",
            "UI.RowViewFactory.Color.cs",
            "UI.RowViewFactory.MaterialShader.cs",
            "UI.RowViewFactory.Renderer.cs",
            "UI.RowViewFactory.Texture.cs",
            "UI.ShaderHintUnderline.cs"
        };
        foreach (var file in themeConsumers)
        {
            var source = ReadSource("src", "MaterialEditor.Base", "UI", file);
            Contains(source, "MaterialEditorTheme.", file + " token integration");
            if (source.Contains("Color.black", StringComparison.Ordinal)
                || source.Contains("Color.gray", StringComparison.Ordinal)
                || source.Contains("Color.white", StringComparison.Ordinal)
                || source.Contains("Color.clear", StringComparison.Ordinal)
                || source.Contains("new Color(", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    file + " owns a semantic color outside UI.Theme.cs.");
            }
        }

        var pluginBase = ReadSource("src", "MaterialEditor.Base", "PluginBase.cs");
        Contains(
            pluginBase,
            "MaterialEditorTheme.Metrics.SidePanelDefaultWidth",
            "side-panel configuration default uses theme");
        Contains(
            pluginBase,
            "MaterialEditorTheme.Metrics.UiScaleDefault",
            "UI-scale configuration default uses theme");

        var window = ReadSource("src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        Contains(
            window,
            "MaterialEditorTheme.Metrics.CanvasReferenceWidth",
            "window geometry uses themed canvas width");
        if (window.Contains("1920f", StringComparison.Ordinal))
            throw new InvalidOperationException("WindowView duplicates the canvas-width token.");

        var style = ReadSource("src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");
        Contains(
            style,
            "MaterialEditorTheme.Typography.PrimaryFontSize",
            "typography adapter uses primary font token");
        if (style.Contains("UIUtility.defaultFontSize", StringComparison.Ordinal))
            throw new InvalidOperationException("StyleSystem duplicates the primary font token.");

        var hintUnderline = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.ShaderHintUnderline.cs");
        Contains(
            hintUnderline,
            "var yMax = bodyBottom - UnderlineRise;",
            "shader-hint underline stays below the visible glyph body");
        if (hintUnderline.Contains(
                "bodyBottom + UnderlineRise",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Shader-hint underline must not rise through the label text.");
        }
    }

    private static void AssertNormalized(Color color, string name)
    {
        var components = new[] { color.r, color.g, color.b, color.a };
        foreach (var component in components)
        {
            if (float.IsNaN(component)
                || float.IsInfinity(component)
                || component < 0f
                || component > 1f)
            {
                throw new InvalidOperationException(name + " is not finite normalized RGBA.");
            }
        }
    }

    private static float Contrast(Color left, Color right)
    {
        var leftLuminance = Luminance(left);
        var rightLuminance = Luminance(right);
        return (Math.Max(leftLuminance, rightLuminance) + 0.05f)
               / (Math.Min(leftLuminance, rightLuminance) + 0.05f);
    }

    private static float Luminance(Color color) =>
        0.2126f * Linear(color.r)
        + 0.7152f * Linear(color.g)
        + 0.0722f * Linear(color.b);

    private static float Linear(float component) =>
        component <= 0.03928f
            ? component / 12.92f
            : (float)Math.Pow((component + 0.055f) / 1.055f, 2.4f);

    private static void Positive(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            throw new InvalidOperationException(name + " must be finite and positive.");
    }

    private static void NonNegative(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            throw new InvalidOperationException(name + " must be finite and non-negative.");
    }

    private static void Ordered(float minimum, float value, float maximum, string name)
    {
        if (float.IsNaN(minimum)
            || float.IsNaN(value)
            || float.IsNaN(maximum)
            || float.IsInfinity(minimum)
            || float.IsInfinity(value)
            || float.IsInfinity(maximum)
            || minimum < 0f
            || minimum > value
            || value > maximum)
        {
            throw new InvalidOperationException(
                name + " must be finite and ordered minimum <= default <= maximum.");
        }
    }

    private static bool SameColor(Color left, Color right) =>
        Math.Abs(left.r - right.r) <= 0.0001f
        && Math.Abs(left.g - right.g) <= 0.0001f
        && Math.Abs(left.b - right.b) <= 0.0001f
        && Math.Abs(left.a - right.a) <= 0.0001f;

    private static void AssertDistinct(Color[] colors, string name)
    {
        for (var left = 0; left < colors.Length; left++)
        {
            for (var right = left + 1; right < colors.Length; right++)
            {
                if (SameColor(colors[left], colors[right]))
                    throw new InvalidOperationException(
                        name + " tokens " + left + " and " + right + " must differ.");
            }
        }
    }

    private static void AtLeast(float expected, float actual, string name)
    {
        if (actual < expected)
            throw new InvalidOperationException(
                name + ": expected at least " + expected + ", got " + actual + ".");
    }

    private static void Equal(float expected, float actual, string name)
    {
        if (Math.Abs(expected - actual) > 0.0001f)
            throw new InvalidOperationException(
                name + ": expected " + expected + ", got " + actual + ".");
    }

    private static string ReadSource(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(segments).ToArray()));

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(
                        directory.FullName,
                        "tests",
                        "MaterialEditor.MetadataTests",
                        "MaterialEditor.MetadataTests.csproj")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the KK_Plugins repository root.");
    }

    private static void Contains(string source, string expected, string name)
    {
        if (!source.Contains(expected, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " is missing.");
    }
}
