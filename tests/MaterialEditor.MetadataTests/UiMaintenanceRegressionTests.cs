using MaterialEditorAPI;

internal static class UiMaintenanceRegressionTests
{
    internal static void Run()
    {
        WindowDragModesMapAndClampIndependently();
        WindowDragConfigKeepsTheLegacyApiLive();
        NumericDisplayAndEditPrecisionRemainDistinct();
        DenseRowsUseSharedSpacingTokens();
        ScrollableLabelsKeepFixedTypography();
        Console.WriteLine("UI maintenance regression guards passed.");
    }

    private static void WindowDragModesMapAndClampIndependently()
    {
        Equal(
            MaterialEditorWindowDragMode.NoLimits,
            MaterialEditorWindowBoundsPolicy.FromLegacy(false),
            "legacy false mapping");
        Equal(
            MaterialEditorWindowDragMode.KeepHeaderInside,
            MaterialEditorWindowBoundsPolicy.FromLegacy(true),
            "legacy true mapping");
        Equal(false,
            MaterialEditorWindowBoundsPolicy.ToLegacyBoolean(
                MaterialEditorWindowDragMode.NoLimits),
            "NoLimits legacy mirror");
        Equal(true,
            MaterialEditorWindowBoundsPolicy.ToLegacyBoolean(
                MaterialEditorWindowDragMode.KeepHeaderInside),
            "header legacy mirror");
        Equal(true,
            MaterialEditorWindowBoundsPolicy.ToLegacyBoolean(
                MaterialEditorWindowDragMode.KeepWholeWindowInside),
            "whole-window legacy mirror");

        var headerBounds = new MaterialEditorWindowDragBounds(
            -10f,
            10f,
            -20f,
            20f);
        var wholeBounds = new MaterialEditorWindowDragBounds(
            -2f,
            2f,
            -3f,
            3f);

        var x = 100f;
        var y = -100f;
        MaterialEditorWindowBoundsPolicy.ClampDragOffset(
            MaterialEditorWindowDragMode.NoLimits,
            headerBounds,
            wholeBounds,
            ref x,
            ref y);
        Equal(100f, x, "NoLimits x");
        Equal(-100f, y, "NoLimits y");

        x = 100f;
        y = -100f;
        MaterialEditorWindowBoundsPolicy.ClampDragOffset(
            MaterialEditorWindowDragMode.KeepHeaderInside,
            headerBounds,
            wholeBounds,
            ref x,
            ref y);
        Equal(10f, x, "header x");
        Equal(-20f, y, "header y");

        x = 100f;
        y = -100f;
        MaterialEditorWindowBoundsPolicy.ClampDragOffset(
            MaterialEditorWindowDragMode.KeepWholeWindowInside,
            headerBounds,
            wholeBounds,
            ref x,
            ref y);
        Equal(2f, x, "whole-window x");
        Equal(-3f, y, "whole-window y");
    }

    private static void WindowDragConfigKeepsTheLegacyApiLive()
    {
        var plugin = ReadSource(
            "src", "MaterialEditor.Base", "PluginBase.cs");
        var projectItems = ReadSource(
            "src", "MaterialEditor.Base", "MaterialEditor.Base.projitems");

        Contains(plugin,
            "public static ConfigEntry<bool> PreventDragout { get; set; }",
            "shipped legacy API");
        Contains(plugin,
            "internal static ConfigEntry<MaterialEditorWindowDragMode> WindowDragMode",
            "internal enum config");
        Contains(plugin,
            "new ConfigurationManagerAttributes { Browsable = false }",
            "legacy config hidden from new UI");
        Contains(plugin,
            "\"Window Drag Limits\"",
            "new config key");
        Contains(plugin,
            "PreventDragout.SettingChanged += HandleLegacyWindowDragSettingChanged;",
            "legacy-to-enum live synchronization");
        Contains(plugin,
            "WindowDragMode.SettingChanged += HandleWindowDragModeChanged;",
            "enum-to-legacy live synchronization");
        Contains(plugin,
            "MaterialEditorUI.UISettingChanged(sender, eventArgs);",
            "changing to a protected mode immediately recovers an active window");
        Contains(plugin,
            "_synchronizingWindowDragSettings",
            "bidirectional synchronization guard");
        Contains(projectItems,
            "UI\\UI.WindowBounds.cs",
            "window-bounds production include");
    }

    private static void NumericDisplayAndEditPrecisionRemainDistinct()
    {
        var numeric = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.NumericInputView.cs");
        Contains(numeric,
            "new NumericInputSpec(\"0.####\", \"R\")",
            "four-decimal display and round-trip edit formats");
        Contains(numeric,
            "? NumericText.FormatEdit(_value, _editFormat)",
            "editing uses round-trip precision");
        Contains(numeric,
            ": NumericText.FormatDisplay(_value, _displayFormat)",
            "inactive inputs use compact display precision");
    }

    private static void DenseRowsUseSharedSpacingTokens()
    {
        var support = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Support.cs");
        var texture = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");
        var layout = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowLayout.cs");

        Contains(support,
            "layout.spacing = MaterialEditorTheme.Spacing.Control;",
            "shared small row spacing");
        Contains(texture,
            "\"OffsetScaleGroupSpacer\"",
            "offset/scale group separator");
        Contains(layout,
            "MaterialEditorTheme.Metrics.OffsetScaleGroupSpacing",
            "tokenized scale-group spacing");
    }

    private static void ScrollableLabelsKeepFixedTypography()
    {
        var support = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Support.cs");
        var texture = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");
        var selection = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SelectListPanel.cs");

        Contains(support,
            "label.resizeTextForBestFit = false;",
            "pooled property labels disable best fit");
        Contains(texture,
            "label.resizeTextForBestFit = false;",
            "hierarchy labels disable best fit");
        var selectionLabel = Slice(
            selection,
            "private static void ConfigureEntryLabel(Text label)",
            "private static void ApplySelectedState(");
        Contains(selectionLabel,
            "label.resizeTextForBestFit = false;",
            "scrolling selection labels disable best fit");
        Contains(selectionLabel,
            "label.fontSize = MaterialEditorTheme.Typography.PrimaryFontSize;",
            "scrolling selection labels use a fixed font token");
        Contains(selection,
            "TooltipBinding.Bind(rowButton.gameObject, null, name);",
            "full-name tooltip remains available");
    }

    private static string Slice(
        string source,
        string start,
        string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0)
            throw new InvalidOperationException("source slice start missing");
        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            throw new InvalidOperationException("source slice end missing");
        return source.Substring(startIndex, endIndex - startIndex);
    }

    private static string ReadSource(params string[] segments) =>
        File.ReadAllText(Path.Combine(
            new[] { FindRepositoryRoot() }.Concat(segments).ToArray()));

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[]
                 {
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory
                 })
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
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static void Contains(string source, string value, string name)
    {
        if (!source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " missing.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
