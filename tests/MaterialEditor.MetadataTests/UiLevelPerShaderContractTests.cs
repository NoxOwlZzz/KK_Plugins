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
        var shaderMode = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.ShaderUiMode.cs");
        var rowFactory = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowViewFactory.cs");
        var rowBinder = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowBinder.cs");
        var style = ReadSource(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.StyleSystem.cs");

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
            "row.IsAdvanced = isAdvanced;",
            "Advanced metadata reaches the row model");
        DoesNotContain(descriptor, "FormatLabel", "metadata does not decorate property labels");
        DoesNotContain(
            shaderMode,
            "MaterialEditorAdvancedPropertyPresentation",
            "obsolete textual marker helper is removed");
        var obsoleteAdvancedMarker = "[" + "A" + "]";
        DoesNotContain(
            descriptor + shaderMode,
            obsoleteAdvancedMarker,
            "Advanced labels contain no textual marker");

        Contains(
            rowFactory,
            "CreateAdvancedPropertyAccent(contentList.transform);",
            "pooled template precreates one Advanced accent overlay");
        Equal(
            1,
            CountOccurrences(rowFactory, "\"AdvancedPropertyAccent\","),
            "Advanced accent object is created once per pooled template");
        Contains(
            rowFactory,
            "accent.color = MaterialEditorStyles.AdvancedPropertyAccentColor;",
            "accent uses its semantic color");
        Contains(rowFactory, "accent.raycastTarget = false;", "accent consumes no pointer events");
        Contains(rowFactory, "canvasGroup.interactable = false;", "accent is non-interactive");
        Contains(rowFactory, "canvasGroup.blocksRaycasts = false;", "accent blocks no row controls");
        Contains(rowFactory, "accent.transform.SetAsLastSibling();", "accent renders over the row panel");
        Contains(rowFactory, "accent.gameObject.SetActive(false);", "template starts without a stale accent");
        DoesNotContain(rowFactory, "SetWidth(accent", "overlay consumes no horizontal layout width");
        Equal(
            true,
            rowFactory.IndexOf("RowLayoutCatalog.Apply(contentList.gameObject);", StringComparison.Ordinal)
            < rowFactory.IndexOf("CreateAdvancedPropertyAccent(contentList.transform);", StringComparison.Ordinal),
            "accent remains outside the family HorizontalLayoutGroups");
        Contains(
            style,
            "internal const float AdvancedPropertyAccentWidth = 5f;",
            "Advanced accent width is structural");
        Contains(
            style,
            "internal const float AdvancedPropertyAccentVerticalInset = 2f;",
            "Advanced accent vertical inset is structural");
        Contains(
            style,
            "new Color32(0x4A, 0xA3, 0xFF, 0xFF);",
            "Advanced accent keeps the final blue color without the wider redesign");

        Contains(
            controls,
            "owner.GetUIComponent<Image>(\"AdvancedPropertyAccent\")",
            "pooled controls cache the accent without a bind-time lookup");
        Contains(
            controls,
            "SetAdvancedPropertyAccent(false);",
            "every rebind first clears the previous row accent");
        Contains(
            rowBinder,
            "_controls.SetAdvancedPropertyAccent(item.IsAdvanced);",
            "binding applies the accent only from the row Advanced flag");
        var bindMethod = Slice(
            rowBinder,
            "internal void Bind(RowModel item, bool force)",
            "public void SetVisible(bool visible)");
        Equal(
            true,
            bindMethod.IndexOf("_controls.HideAll();", StringComparison.Ordinal)
            < bindMethod.IndexOf("_controls.SetAdvancedPropertyAccent(item.IsAdvanced);", StringComparison.Ordinal),
            "Basic/Advanced rebinds clear stale state before applying the model");
        Equal(
            1,
            CountOccurrences(bindMethod, "SetAdvancedPropertyAccent("),
            "each successful bind applies one accent state");
        DoesNotContain(bindMethod, "GetUIComponent", "rebind performs no name lookup");
        DoesNotContain(bindMethod, "AddComponent", "rebind creates no UI components");
        DoesNotContain(bindMethod, "new GameObject", "rebind creates no GameObjects");
        Contains(
            bindMethod,
            "if (!force && ReferenceEquals(item, _currentModel))",
            "same-model fast path preserves the already-correct accent state");
    }

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0)
            throw new InvalidOperationException("Could not locate source slice start.");
        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            throw new InvalidOperationException("Could not locate source slice end.");
        return source.Substring(startIndex, endIndex - startIndex);
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
