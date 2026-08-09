internal static class UiLevelPerShaderContractTests
{
    internal static void Run()
    {
        var root = FindRepositoryRoot();
        var window = ReadSource(root, "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var session = ReadSource(root, "src", "MaterialEditor.Base", "UI", "UI.SessionState.cs");
        var presenter = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.MaterialSectionPresenter.cs");
        var model = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowModel.MaterialShader.cs");
        var factory = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowViewFactory.MaterialShader.cs");
        var controls = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowControls.cs");
        var binder = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowBinder.MaterialShader.cs");
        var descriptor = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.PropertyDescriptor.cs");

        DoesNotContain(
            window,
            "MaterialEditorModePanel",
            "the global Basic/Advanced panel is removed");
        DoesNotContain(
            window,
            "BasicModeButton",
            "the global Basic button is removed");
        DoesNotContain(
            window,
            "AdvancedModeButton",
            "the global Advanced button is removed");
        Contains(
            session,
            "MaterialEditorShaderUiModeState ShaderUiModes",
            "session owns per-shader modes");
        DoesNotContain(
            session,
            "MaterialEditorUiMode UiMode =",
            "session has no scalar global mode");

        Contains(model, "HasAdvancedProperties", "shader model advertises Advanced metadata");
        Contains(model, "UiModeOnChange", "shader model owns the mode action");
        Contains(factory, "ShaderUiModeButton", "shader row creates the mode control");
        Contains(controls, "ShaderUiModeButton", "pooled controls cache the mode control");
        Contains(
            binder,
            "controls.UiModeButton.gameObject.SetActive(item.HasAdvancedProperties);",
            "mode control is absent for shaders without Advanced properties");
        Contains(
            binder,
            "item.UiMode == MaterialEditorUiMode.Advanced",
            "mode control reflects its shader snapshot");

        Contains(
            presenter,
            "_session.ShaderUiModes.GetMode(context.ShaderName)",
            "property filtering resolves the current shader mode");
        Contains(
            presenter,
            "if (_session.ShaderUiModes.SetMode(shaderItem.ShaderName, value))",
            "changing a shader mode is guarded against no-op rebuilds");
        Equal(
            1,
            CountOccurrences(
                presenter,
                "if (_session.ShaderUiModes.SetMode(shaderItem.ShaderName, value))"),
            "one mode mutation site");
        DoesNotContain(
            presenter,
            "_session.UiMode",
            "presenter no longer reads global mode");

        Contains(
            descriptor,
            "MaterialEditorAdvancedPropertyPresentation.FormatLabel",
            "Advanced rows receive an explicit visible marker before binding");
    }

    private static string ReadSource(string root, params string[] parts)
    {
        var path = parts.Aggregate(root, Path.Combine);
        return File.ReadAllText(path).Replace("\r\n", "\n");
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
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Contains(string source, string value, string name) =>
        Equal(true, source.Contains(value, StringComparison.Ordinal), name);

    private static void DoesNotContain(string source, string value, string name) =>
        Equal(false, source.Contains(value, StringComparison.Ordinal), name);

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
