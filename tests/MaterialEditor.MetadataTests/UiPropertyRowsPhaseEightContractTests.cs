using MaterialEditorAPI;

internal static class UiPropertyRowsPhaseEightContractTests
{
    internal static void Run()
    {
        FloatRangePolicyRequiresRealBounds();
        FloatRangeOriginsAreExplicit();
        FloatBindingRestoresPooledLayoutWithoutExtraListeners();
        ResetButtonsUseOneCompactGlyphWithoutChangingShaderOptions();
        PropertyLabelsKeepFullNamesHintsAndClickTargets();
        ExistingStateAndFixedRowContractsRemainIntact();
        Console.WriteLine("Phase 8 property-row regression guards passed.");
    }

    private static void FloatRangePolicyRequiresRealBounds()
    {
        Equal(false,
            FloatPropertyRangePolicy.HasUsableRange(null, null),
            "missing float range");
        Equal(false,
            FloatPropertyRangePolicy.HasUsableRange(0f, null),
            "missing maximum");
        Equal(false,
            FloatPropertyRangePolicy.HasUsableRange(null, 1f),
            "missing minimum");
        Equal(false,
            FloatPropertyRangePolicy.HasUsableRange(float.NaN, 1f),
            "NaN minimum");
        Equal(false,
            FloatPropertyRangePolicy.HasUsableRange(0f, float.PositiveInfinity),
            "infinite maximum");
        Equal(false,
            FloatPropertyRangePolicy.HasUsableRange(1f, 1f),
            "empty range");
        Equal(false,
            FloatPropertyRangePolicy.HasUsableRange(2f, 1f),
            "reversed range");
        Equal(true,
            FloatPropertyRangePolicy.HasUsableRange(-2f, 3f),
            "finite ordered range");
    }

    private static void FloatRangeOriginsAreExplicit()
    {
        var model = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowModel.Properties.cs");
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.PropertyDescriptor.cs");
        var presenter = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialSectionPresenter.cs");
        var builtIn = Slice(
            factory,
            "internal static FloatPropertyRowModel CreateFloatRow(",
            "private KeywordPropertyRowModel CreateKeywordRow(");
        var extension = Slice(
            factory,
            "private static FloatPropertyRowModel CreateExtensionFloatRow(",
            "private ColorPropertyRowModel CreateExtensionColorRow(");
        var projector = Slice(
            presenter,
            "private static FloatPropertyRowModel CreateFloatRow(",
            "private static void GetProjectorPresentation(");

        Contains(model,
            "internal bool HasRange { get; set; }",
            "internal float-range signal");
        Contains(builtIn,
            "FloatPropertyRangePolicy.HasUsableRange(",
            "built-in range validation");
        Contains(builtIn,
            "HasRange = hasRange",
            "built-in range assignment");
        Contains(builtIn,
            "if (hasRange)",
            "bounds copied only for a usable range");
        Contains(extension,
            "HasRange = true",
            "extension float range contract");
        Contains(projector,
            "HasRange = true",
            "projector range contract");
        Contains(presenter,
            "0f,\n                    maxValue,",
            "projector supplies its real zero-to-maximum range");
    }

    private static void FloatBindingRestoresPooledLayoutWithoutExtraListeners()
    {
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.FloatKeyword.cs");
        var controls = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowControls.cs");
        var layout = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowLayout.cs");
        var rowView = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowView.cs");
        var floatBinding = Slice(
            binder,
            "private void BindFloat(FloatPropertyRowModel item, ListenerScope listeners)",
            "private void BindKeyword(KeywordPropertyRowModel item, ListenerScope listeners)");
        var setWidth = Slice(
            layout,
            "internal void SetFixedWidth(float width)",
            "public void CalculateLayoutInputHorizontal()");

        Contains(floatBinding,
            "controls.Slider.gameObject.activeSelf != item.HasRange",
            "idempotent pooled slider visibility");
        Contains(floatBinding,
            "? MaterialEditorLayout.FloatInputWidth\n                    : MaterialEditorLayout.ContentWidth",
            "ranged and range-free input widths");
        Equal(1,
            CountOccurrences(floatBinding, "SliderBinding.Bind("),
            "single optional slider binding");
        Equal(true,
            floatBinding.IndexOf("if (item.HasRange)", StringComparison.Ordinal)
            < floatBinding.IndexOf("SliderBinding.Bind(", StringComparison.Ordinal),
            "range guard precedes slider binding");
        Contains(floatBinding,
            "InputFieldBinding.BindFloat(",
            "numeric input remains available without a slider");
        Contains(floatBinding,
            "controls.Slider.Set(item.Value, false);",
            "programmatic slider synchronization stays silent");
        DoesNotContain(floatBinding,
            "new RowColumnSpec",
            "row spec allocation per bind");
        DoesNotContain(floatBinding,
            "new LayoutElement",
            "parallel layout element per bind");
        Contains(controls,
            "InputLayout = Input.GetComponent<RowColumnLayoutOverride>();",
            "input layout override cached once");
        Contains(rowView,
            "RowLayoutCatalog.Restore(gameObject);",
            "layout overrides exist before controls initialize");
        Contains(setWidth,
            "&& _flexibleWidth == 0f)\n                return;",
            "identical width avoids redundant rebuild");
        DoesNotContain(setWidth, "new ", "width change allocation");
    }

    private static void ResetButtonsUseOneCompactGlyphWithoutChangingShaderOptions()
    {
        var theme = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        var factories = ReadSources(
            "UI.RowViewFactory.Renderer.cs",
            "UI.RowViewFactory.MaterialShader.cs",
            "UI.RowViewFactory.Texture.cs",
            "UI.RowViewFactory.Color.cs",
            "UI.RowViewFactory.FloatKeyword.cs",
            "UI.RowViewFactory.EnumVectorToggle.cs");

        Contains(theme,
            "internal const float SmallButtonWidth = 20f;",
            "compact control width");
        Contains(theme,
            "internal const float ResetButtonWidth = SmallButtonWidth;",
            "reset width aliases compact control");
        Contains(theme,
            "internal const string Reset = \"R\";",
            "reset letter token");
        Equal(12,
            CountOccurrences(factories, "MaterialEditorTheme.Glyphs.Reset"),
            "ordinary reset creation paths use the glyph");
        foreach (var resetName in new[]
                 {
                     "RendererEnabledResetButton",
                     "RendererReceiveShadowsResetButton",
                     "RendererUpdateWhenOffscreenResetButton",
                     "RendererRecalculateNormalsResetButton",
                     "RendererShadowCastingModeResetButton",
                     "ShaderResetButton",
                     "ShaderRenderQueueResetButton",
                     "TextureResetButton",
                     "OffsetScaleResetButton",
                     "ColorResetButton",
                     "FloatResetButton",
                     "KeywordResetButton",
                     "EnumResetButton",
                     "VectorResetButton",
                     "FloatToggleResetButton"
                 })
        {
            Contains(factories, "\"" + resetName + "\"", resetName + " retained");
        }
        Equal(1,
            CountOccurrences(factories, "new Dropdown.OptionData(\"Reset\")"),
            "shader semantic Reset option remains textual");
        Contains(factories,
            "Reset the selected property to its original value",
            "existing reset help remains available");
        Contains(factories,
            "In order for the reset to take effect",
            "detailed reset help remains available");
    }

    private static void PropertyLabelsKeepFullNamesHintsAndClickTargets()
    {
        var support = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Support.cs");
        var theme = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        var factories = ReadSources(
            "UI.RowViewFactory.Renderer.cs",
            "UI.RowViewFactory.MaterialShader.cs",
            "UI.RowViewFactory.Texture.cs",
            "UI.RowViewFactory.Color.cs",
            "UI.RowViewFactory.FloatKeyword.cs",
            "UI.RowViewFactory.EnumVectorToggle.cs");
        var propertyBinders = ReadSources(
            "UI.RowBinder.Texture.cs",
            "UI.RowBinder.Color.cs",
            "UI.RowBinder.FloatKeyword.cs",
            "UI.RowBinder.EnumVectorToggle.cs");
        var rendererBinder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Renderer.cs");
        var materialShaderBinder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.MaterialShader.cs");
        var helper = Slice(
            support,
            "internal static void ConfigurePropertyLabel(Text label)",
            "internal static LayoutElement SetWidth(");

        Contains(helper,
            "label.resizeTextForBestFit = false;",
            "property labels keep stable typography while recycled");
        Contains(helper,
            "label.fontSize = MaterialEditorTheme.Typography.PrimaryFontSize;",
            "property labels use the fixed primary font token");
        Contains(helper,
            "label.verticalOverflow = VerticalWrapMode.Truncate;",
            "single-row property truncation");
        DoesNotContain(helper,
            "raycastTarget",
            "label click raycast mutation");
        Equal(14,
            CountOccurrences(factories, "ConfigurePropertyLabel("),
            "visible ordinary and named-header labels configured");
        DoesNotContain(factories,
            "\"RendererLabel\"",
            "zero-width renderer label cannot leak glyph fragments");
        DoesNotContain(factories,
            "\"MaterialLabel\"",
            "zero-width material label cannot leak glyph fragments");
        DoesNotContain(rendererBinder,
            "ChangedStateBinding.SetLabel(",
            "renderer header binds only its visible name");
        DoesNotContain(materialShaderBinder,
            "ChangedStateBinding.SetLabel(",
            "material header binds only its visible name");
        Equal(8,
            CountOccurrences(propertyBinders, ".PropertyName,"),
            "eight technical property families expose raw names");
        Contains(propertyBinders,
            "item.TooltipText",
            "shader metadata remains the hint source");
        Contains(rendererBinder,
            "item.RendererName,\n                controls.Name",
            "renderer full-name tooltip");
        Contains(materialShaderBinder,
            "item.MaterialName,\n                controls.Name",
            "material full-name tooltip");
        Contains(materialShaderBinder,
            "item.ShaderName,\n                controls.Label",
            "shader full-name tooltip");
        Contains(propertyBinders,
            "LabelClickBinding.Bind(",
            "existing label click binding retained");
    }

    private static void ExistingStateAndFixedRowContractsRemainIntact()
    {
        var theme = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        var rowFactory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.cs");
        var binders = ReadSources(
            "UI.RowBinder.Color.cs",
            "UI.RowBinder.FloatKeyword.cs",
            "UI.RowBinder.EnumVectorToggle.cs");
        var controls = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowControls.cs");
        var changedState = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinding.Common.cs");

        Contains(theme,
            "internal const float RowHeight = 22f;",
            "fixed row height");
        Contains(rowFactory,
            "AddComponent<LayoutElement>().preferredHeight = PanelHeight;",
            "virtual-list fixed row template");
        Contains(binders, "IsMixed", "mixed-state handling retained");
        Contains(binders,
            "ChangedStateBinding.Apply(",
            "modified-state handling retained");
        Contains(changedState,
            "resetButton.interactable = changed;",
            "reset interactability remains changed-state owned");
        Contains(controls,
            "Panel.blocksRaycasts = _visible && _enabled;",
            "disabled rows remain noninteractive");

        var changedFiles = theme + rowFactory + binders + controls;
        foreach (var forbidden in new[]
                 {
                     "Thumbnail",
                     "ContentSizeFitter",
                     "Animator",
                     "StartCoroutine",
                     "void Update()",
                     "void LateUpdate()"
                 })
        {
            DoesNotContain(changedFiles, forbidden, forbidden + " phase-eight feature");
        }
    }

    private static string ReadSources(params string[] fileNames)
    {
        var source = string.Empty;
        foreach (var fileName in fileNames)
            source += ReadSource("src", "MaterialEditor.Base", "UI", fileName);
        return source;
    }

    private static string ReadSource(params string[] segments)
    {
        return File.ReadAllText(
                Path.Combine(new[] { FindRepositoryRoot() }.Concat(segments).ToArray()))
            .Replace("\r\n", "\n");
    }

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

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        var endIndex = source.IndexOf(
            end,
            startIndex + start.Length,
            StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("Could not isolate source contract.");
        return source.Substring(startIndex, endIndex - startIndex);
    }

    private static void Contains(string source, string value, string name)
    {
        if (!source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " missing.");
    }

    private static void DoesNotContain(string source, string value, string name)
    {
        if (source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " present.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
