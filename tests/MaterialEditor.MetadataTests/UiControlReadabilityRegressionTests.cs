using System.Text.RegularExpressions;

internal static class UiControlReadabilityRegressionTests
{
    internal static void Run()
    {
        VectorInputsHaveNonzeroAuthoritativeWidths();
        DropdownTypographyHasReadableBounds();
        DropdownRuntimeAvoidsDuplicateAndClosedWork();
        DebugDiagnosticsCoverBothControls();
        Console.WriteLine("Vector-input and dropdown-readability regression guards passed.");
    }

    private static void VectorInputsHaveNonzeroAuthoritativeWidths()
    {
        var source = ReadSource("src", "MaterialEditor.Base", "UI", "UI.RowLayout.cs");
        Contains(source, "\"VectorPanel\"", "Vector panel layout spec");

        var themeSource = ReadSource("src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        Contains(
            themeSource,
            "internal const float VectorComponentLabelWidth = 14f",
            "readable Vector component label width");
        Contains(
            themeSource,
            "internal const int VectorComponentMinimumFontSize = 12",
            "Vector component label font floor");

        foreach (var component in new[] { "X", "Y", "Z", "W" })
        {
            Matches(
                source,
                "\\\"Vector" + component
                + "Input\\\"[\\s\\S]{0,180}MaterialEditorLayout\\.VectorComponentInputWidth",
                "Vector " + component + " input authoritative width");
        }

        var numericSource = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.NumericInputView.cs");
        Contains(
            numericSource,
            "gameObject.AddComponent<RowColumnLayoutOverride>()",
            "numeric input high-priority layout override");
    }

    private static void DropdownTypographyHasReadableBounds()
    {
        var source = ReadSource("src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");
        var themeSource = ReadSource("src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        Contains(
            source,
            "text.resizeTextForBestFit = false",
            "dropdown typography remains stable across values and pooled rows");
        Contains(
            source,
            "text.resizeTextMinSize = MaterialEditorLayout.DropdownFontSize",
            "dropdown minimum size matches its fixed type size");
        Contains(
            source,
            "text.resizeTextMaxSize = MaterialEditorLayout.DropdownFontSize",
            "dropdown maximum size matches its fixed type size");
        Contains(
            source,
            "MaterialEditorLayout.DropdownTextVerticalInset",
            "compact-row dropdown vertical inset");
        Contains(
            source,
            "ApplyDropdownText(dropdown.captionText, null)",
            "dropdown caption style");
        Contains(
            source,
            "ApplyDropdownText(dropdown.itemText, dropdown.captionText)",
            "dropdown item style");
        Contains(source, "MaterialEditorTheme.Colors.PopupSurface", "dropdown popup surface");
        Contains(source, "MaterialEditorTheme.Colors.PrimaryText", "readable dropdown text");
        Contains(source, "MaterialEditorTheme.Colors.SelectedText", "selected dropdown text");
        Contains(source, "MaterialEditorTheme.Colors.DisabledText", "disabled dropdown text");
        Contains(source, "MaterialEditorTheme.Colors.SliderHandlePressed", "pressed slider handle state");
        Contains(source, "MaterialEditorTheme.Colors.ScrollbarHandlePressed", "pressed scrollbar handle state");
        Contains(source, "text.material = renderingSource.material", "dropdown material propagation");
        Contains(source, "text.maskable = true", "dropdown mask participation");
    }

    private static void DropdownRuntimeAvoidsDuplicateAndClosedWork()
    {
        var filter = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.DropdownFilter.cs");
        Contains(filter, "internal const int PersistentFilterLimit = 16", "bounded filter cache");
        Contains(filter, "if (_listenersBound", "listener idempotence guard");
        Contains(filter, "RemoveListener(OnChangeFilter)", "filter listener cleanup");
        Contains(filter, "RemoveListener(ClearFilter)", "clear listener cleanup");
        Contains(filter, "if (!_isOpen", "closed popup work guard");
        Contains(filter, "string.Equals(_lastFilter, pattern", "unchanged-filter fast path");
        Contains(filter, "WildcardMatch(item.Label, pattern)", "allocation-free wildcard filtering");
        Contains(filter, "TryGetVisibleOptionPosition(",
            "filtered dropdown exposes visible option position");
        if (filter.Contains("System.Text.RegularExpressions", StringComparison.Ordinal)
            || filter.Contains("new Regex(", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Dropdown filtering must not allocate a Regex per edit.");
        }

        var autoScroll = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.AutoScrollToCenter.cs");
        Contains(autoScroll, "enabled = false;", "one-shot dropdown autoscroll");
        Contains(autoScroll, "filter.TryGetVisibleOptionPosition(",
            "autoscroll uses the filtered option sequence");

        var shaderControls = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowControls.cs");
        var shaderBinder = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowBinder.MaterialShader.cs");
        Contains(shaderControls, "internal sealed class ShaderDropdownOptionCache",
            "shader dropdown owns unavailable-option lifetime");
        Contains(shaderControls, "for (var index = 1;",
            "shader selection skips the Reset sentinel");
        Contains(shaderBinder, "controls.OptionCache.PrepareSelection(item.ShaderName)",
            "unavailable current shader remains selected explicitly");
    }

    private static void DebugDiagnosticsCoverBothControls()
    {
        var source = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.LayoutDiagnostics.cs");
        Contains(source, "Vector X declared width", "Vector runtime assertion");
        Contains(source, "Enum caption fixed font size", "Dropdown runtime assertion");
        Contains(
            source,
            "cachedTextGenerator.fontSizeUsedForBestFit",
            "Dropdown rendered-size measurement");
        Contains(source, "ActivatePanels(", "active-panel runtime validation");
        Contains(source, "Enum caption text height", "Dropdown text-height assertion");
        Contains(source, "ValidateInputVisual(vectorXInput)", "Vector input background assertion");
        Contains(source, "input.targetGraphic != image", "Vector target-graphic assertion");
        Contains(source, "Vector W rendered label font size", "Vector axis font assertion");

        var virtualListSource = ReadSource(
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.VirtualList.cs");
        Matches(
            virtualListSource,
            "SetupEntryTemplate\\(\\)[\\s\\S]{0,700}RowLayoutRuntimeAssertions\\.Validate\\(rowView\\)",
            "template-level Debug validation");
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

    private static void Matches(string source, string pattern, string name)
    {
        if (!Regex.IsMatch(source, pattern, RegexOptions.CultureInvariant))
            throw new InvalidOperationException(name + " is missing.");
    }
}
