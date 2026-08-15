using MaterialEditorAPI;
using UnityEngine;

internal static class UiDarkThemeContractTests
{
    internal static void Run()
    {
        PaletteMatchesTheDarkStructuralSpecification();
        CommonControlsConsumeSemanticChrome();
        StructuralConsumersUseSemanticRoles();
        ColorPickerSwatchRetainsBinderOwnership();
        Console.WriteLine("Material Editor dark structural-theme contract guards passed.");
    }

    private static void StructuralConsumersUseSemanticRoles()
    {
        var navigator = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.CategoryNavigator.cs");
        Contains(navigator, "MaterialEditorPanelRole.LeftPanel", "left navigator role");
        Contains(navigator, "MaterialEditorTheme.Colors.SelectedText", "selected navigator text");

        var rightPanel = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.SelectListPanel.cs");
        Contains(rightPanel, "MaterialEditorPanelRole.RightPanel", "right selection-list role");
        Contains(rightPanel, "MaterialEditorTheme.Colors.SelectedText", "selected list text");

        var window = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.WindowView.cs");
        Contains(window, "MaterialEditorPanelRole.CenterPanel", "center workspace role");

        var shaderFactory = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowViewFactory.MaterialShader.cs");
        Contains(shaderFactory, "MaterialEditorStyles.ShaderColor", "shader structural row");

        var ui = ReadSource("src", "MaterialEditor.Base", "UI", "UI.cs");
        Contains(ui, "ItemColor = MaterialEditorStyles.PropertyColor", "property row surface");
    }

    private static void PaletteMatchesTheDarkStructuralSpecification()
    {
        AssertRgb(MaterialEditorTheme.Colors.Window, 0x1E, 0x23, 0x2B, "window");
        AssertRgb(MaterialEditorTheme.Colors.LeftPanel, 0x20, 0x26, 0x2F, "left panel");
        AssertRgb(MaterialEditorTheme.Colors.CenterPanel, 0x24, 0x2B, 0x34, "center panel");
        AssertRgb(MaterialEditorTheme.Colors.RightPanel, 0x22, 0x28, 0x32, "right panel");
        AssertRgb(MaterialEditorTheme.Colors.NeutralHeader, 0x2A, 0x31, 0x3B, "neutral header");
        AssertRgb(MaterialEditorTheme.Colors.PropertyRow, 0x2A, 0x31, 0x3B, "property row");
        AssertRgb(MaterialEditorTheme.Colors.PropertyRowAlternate, 0x2D, 0x35, 0x40, "alternate property row");
        AssertRgb(MaterialEditorTheme.Colors.InputSurface, 0x16, 0x1B, 0x22, "input");
        AssertRgb(MaterialEditorTheme.Colors.DropdownSurface, 0x16, 0x1B, 0x22, "dropdown");
        AssertRgb(MaterialEditorTheme.Colors.PopupSurface, 0x16, 0x1B, 0x22, "popup");
        AssertRgb(MaterialEditorTheme.Colors.RendererHeader, 0x2F, 0x4F, 0x74, "renderer");
        AssertRgb(MaterialEditorTheme.Colors.MaterialHeader, 0x5A, 0x4A, 0x73, "material");
        AssertRgb(MaterialEditorTheme.Colors.ShaderHeader, 0x36, 0x5C, 0x73, "shader");
        AssertRgb(MaterialEditorTheme.Colors.CategoryHeader, 0x6F, 0x5A, 0x8E, "category");
        AssertRgb(MaterialEditorTheme.Colors.CategoryHeaderHover, 0x76, 0x5F, 0x97, "category hover");
        AssertRgb(MaterialEditorTheme.Colors.CategoryHeaderExpanded, 0x5F, 0x4C, 0x7D, "category expanded");
        AssertRgb(MaterialEditorTheme.Colors.Hover, 0x33, 0x46, 0x5C, "hover");
        AssertRgb(MaterialEditorTheme.Colors.Pressed, 0x39, 0x45, 0x53, "pressed");
        AssertRgb(MaterialEditorTheme.Colors.Selected, 0x3A, 0x74, 0xA8, "selected");
        AssertRgb(MaterialEditorTheme.Colors.SelectedText, 0xFF, 0xFF, 0xFF, "selected text");
        AssertRgb(MaterialEditorTheme.Colors.ToggleMark, 0xFF, 0xFF, 0xFF, "toggle mark");
        AssertRgb(MaterialEditorTheme.Colors.DisabledSurface, 0x20, 0x26, 0x2F, "disabled surface");
        AssertRgb(MaterialEditorTheme.Colors.ModifiedIndicator, 0xD2, 0x94, 0x28, "modified indicator");
        AssertRgb(MaterialEditorTheme.Colors.ModifiedSurface, 0x4A, 0x39, 0x20, "modified surface");
        AssertRgb(MaterialEditorTheme.Colors.Primary, 0xE6, 0xEC, 0xF2, "primary text");
        AssertRgb(MaterialEditorTheme.Colors.Secondary, 0xAA, 0xB6, 0xC3, "secondary text");
        AssertRgb(MaterialEditorTheme.Colors.Disabled, 0x8A, 0x92, 0x9D, "disabled text");
        AssertRgb(MaterialEditorTheme.Colors.Accent, 0x4A, 0xA3, 0xFF, "accent");
        AssertRgb(MaterialEditorTheme.Colors.InputBorder, 0x8D, 0x9B, 0xAA, "input border");
        AssertRgb(MaterialEditorTheme.Colors.StrongBorder, 0x73, 0x82, 0x91, "strong border");
        AssertRgb(MaterialEditorTheme.Colors.Divider, 0x3E, 0x4A, 0x58, "divider");
        AssertRgb(MaterialEditorTheme.Colors.HandlePressed, 0x8D, 0xC6, 0xFF, "pressed handle");
        AssertRgb(MaterialEditorTheme.Colors.Warning, 0xD7, 0xA4, 0x4A, "warning");
        AssertRgb(MaterialEditorTheme.Colors.Error, 0xDD, 0x66, 0x70, "error");
        AssertRgb(MaterialEditorTheme.Colors.Success, 0x62, 0xB9, 0x85, "success");
        AssertRgb(MaterialEditorTheme.Colors.PlaceholderText, 0xAA, 0xB6, 0xC3, "placeholder text");
    }

    private static void CommonControlsConsumeSemanticChrome()
    {
        var style = ReadSource("src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");
        Contains(style, "text.color = GetTextColor(role)", "role-based text color");
        Contains(style, "colors.normalColor = normal", "normal selectable state");
        Contains(style, "colors.highlightedColor = highlighted", "hover selectable state");
        Contains(style, "colors.pressedColor = pressed", "pressed selectable state");
        Contains(style, "colors.disabledColor = disabled", "disabled selectable state");
        Contains(
            style,
            "selectable.targetGraphic.color = MaterialEditorTheme.Colors.TintIdentity",
            "non-multiplying selectable graphic base");
        Contains(style, "MaterialEditorTheme.Colors.PlaceholderText", "input placeholder color");
        Contains(
            style,
            "graphic.color = MaterialEditorTheme.Colors.ToggleMark",
            "toggle checkmark consumes the white semantic token");
        Contains(
            style,
            "image.sprite = GetToggleMarkSprite();",
            "toggle replaces the dark bundled checkmark bitmap");
        Contains(
            style,
            "new Color32(0xFF, 0xFF, 0xFF, 0xFF)",
            "toggle mark source pixels are opaque white");
        Contains(
            style,
            "private const int ToggleMarkStrokeWidth = 2;",
            "toggle mark uses a visually thin two-pixel stroke");
        Contains(
            style,
            "for (var offsetY = 0; offsetY < ToggleMarkStrokeWidth; offsetY++)",
            "toggle mark rasterizer applies the declared thin stroke");
        Contains(
            style,
            "if (_toggleMarkSprite != null)\n                return _toggleMarkSprite;",
            "toggle mark sprite is cached across control creation and rebinds");
        Contains(
            style,
            "graphic.canvasRenderer.SetColor(MaterialEditorTheme.Colors.TintIdentity)",
            "toggle renderer cannot multiply the white mark by a stale dark tint");
        Contains(
            style,
            "var toggle = UIUtility.CreateToggle(name, parent, value)",
            "toggle creation remains centralized");
        Contains(
            style,
            "MaterialEditorStyles.ApplyToggle(toggle);",
            "every factory toggle receives the shared checkmark style");
        Contains(style, "ApplyDropdownText(dropdown.captionText, null)", "dropdown caption typography");
        Contains(style, "ApplyDropdownText(dropdown.itemText, dropdown.captionText)", "dropdown item typography");
        Contains(style, "ApplyDropdownScrollView", "dropdown popup-specific surface");
        Contains(style, "MaterialEditorDropdownItemStyle", "dropdown selected-item state");
        Contains(style, "MaterialEditorDropdownPopupStyle", "runtime dropdown popup style");
        Contains(
            style,
            "dropdown.template.GetComponent<MaterialEditorDropdownPopupStyle>()",
            "runtime popup style reuse");
        Contains(
            style,
            "dropdown.template.gameObject.AddComponent<MaterialEditorDropdownPopupStyle>()",
            "runtime popup style installation");
        Contains(style, "popupStyle.Configure(dropdown)", "runtime popup owner binding");
        Contains(
            style,
            "popupRoot.GetComponentsInChildren<Toggle>(true)",
            "runtime popup item discovery");
        Contains(
            style,
            "ApplyDropdownItemState(items[i], itemText, items[i].isOn)",
            "runtime popup item restyle");
        Contains(style, "private void Start()", "post-clone popup lifecycle hook");
        Contains(style, "_started = true;", "post-clone popup activation");
        Contains(
            style,
            "MaterialEditorStyles.ApplyDropdownPopup(",
            "runtime popup shared style route");
        Contains(
            style,
            "toggle.targetGraphic.canvasRenderer.SetColor(surface)",
            "runtime popup background color application");
        Contains(style, "ApplyControlOutline", "input/dropdown contrast outline");
        Contains(style, "text.material = renderingSource.material", "dropdown material propagation");
        Contains(style, "text.canvasRenderer.SetAlpha", "explicit text opacity");
        Contains(style, "mask.showMaskGraphic = true", "dropdown viewport mask");
        Contains(style, "MaterialEditorTheme.Colors.SliderFill", "slider fill color");
        Contains(style, "MaterialEditorTheme.Colors.ScrollbarHandle", "scrollbar handle color");
        Contains(style, "MaterialEditorStyles.ApplySlider(slider)", "slider factory integration");
    }

    private static void ColorPickerSwatchRetainsBinderOwnership()
    {
        var style = ReadSource("src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");
        Contains(style, "ApplySwatchButton", "explicit swatch style");
        Contains(style, "Selectable.Transition.None", "swatch tint suppression");
        if (style.Contains("\"ColorEditButton\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "StyleSystem must not identify swatches through a GameObject name.");
        }

        var factory = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowViewFactory.Color.cs");
        Contains(
            factory,
            "MaterialEditorControlFactory.CreateSwatchButton(",
            "color-row swatch factory route");
    }

    private static void AssertRgb(Color color, int red, int green, int blue, string name)
    {
        AssertComponent(color.r, red, name + " red");
        AssertComponent(color.g, green, name + " green");
        AssertComponent(color.b, blue, name + " blue");
        if (Math.Abs(color.a - 1f) > 0.0001f)
            throw new InvalidOperationException(name + " must be opaque.");
    }

    private static void AssertComponent(float actual, int expectedByte, string name)
    {
        var expected = expectedByte / 255f;
        if (Math.Abs(actual - expected) > 0.0001f)
        {
            throw new InvalidOperationException(
                name + ": expected " + expected + ", got " + actual + ".");
        }
    }

    private static string ReadSource(params string[] segments)
    {
        return File.ReadAllText(
            Path.Combine(new[] { FindRepositoryRoot() }.Concat(segments).ToArray()));
    }

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
                {
                    return directory.FullName;
                }
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
