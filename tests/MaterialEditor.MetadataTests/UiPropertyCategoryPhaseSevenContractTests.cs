internal static class UiPropertyCategoryPhaseSevenContractTests
{
    internal static void Run()
    {
        CentralHeaderUsesOneFullSurfaceAction();
        CategoryNameAndTooltipRemainReadable();
        CollapseBindingUsesOnlyTheExistingModelContract();
        PropertySearchAloneSuppressesHierarchyCollapseControls();
        FixedVirtualRowAndLegacyNamesRemainCompatible();
        UnsupportedCategoryFeaturesStayAbsent();
        Console.WriteLine("Phase 7 property-category regression guards passed.");
    }

    private static void CentralHeaderUsesOneFullSurfaceAction()
    {
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");
        var style = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");
        var category = Slice(
            factory,
            "private static void CreateCategoryRow(Transform parent)",
            "private static void CreateSubcategoryRow(Transform parent)");
        var categoryStyle = Slice(
            style,
            "internal static void ApplyPropertyCategoryButton(Button button)",
            "internal static void ApplyCategoryNavigationButton(Button button)");

        Equal(1,
            CountOccurrences(category, "AddComponent<Button>()"),
            "one category-header Button");
        DoesNotContain(category,
            "MaterialEditorControlFactory.CreateButton(",
            "nested category Button");
        Contains(category,
            "headerButton.targetGraphic = panel;",
            "full panel is the selectable target");
        Contains(category,
            "panel.raycastTarget = true;",
            "full panel remains raycastable");
        Contains(category,
            "collapseIndicator.raycastTarget = false;",
            "collapse indicator is passive");
        Contains(category,
            "label.raycastTarget = false;",
            "label cannot steal the panel click");
        Contains(categoryStyle,
            "MaterialEditorTheme.Colors.CategoryRow",
            "neutral category normal surface");
        Contains(categoryStyle,
            "MaterialEditorTheme.Colors.CategoryHeaderHover",
            "shared structural category hover surface");
        Contains(categoryStyle,
            "MaterialEditorTheme.Colors.CategoryHeaderExpanded",
            "shared structural category expanded surface");
    }

    private static void CategoryNameAndTooltipRemainReadable()
    {
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Texture.cs");
        var common = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinding.Common.cs");
        var theme = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        var category = Slice(
            factory,
            "private static void CreateCategoryRow(Transform parent)",
            "private static void CreateSubcategoryRow(Transform parent)");
        var binding = Slice(
            binder,
            "private void BindCategory(PropertyCategoryRowModel item, ListenerScope listeners)",
            "private void BindSubcategory(");

        Contains(category, "label.fontStyle = FontStyle.Bold;", "bold category name");
        Contains(category, "label.resizeTextForBestFit = false;", "stable category font size");
        Contains(category,
            "label.fontSize = MaterialEditorTheme.Typography.PrimaryFontSize;",
            "category uses the fixed primary font token");
        Contains(category,
            "label.verticalOverflow = VerticalWrapMode.Truncate;",
            "single-row truncation");
        Contains(theme,
            "internal const int PrimaryFontSize = 16;",
            "fixed readable category size");
        Contains(binding,
            "controls.HeaderButton.gameObject,\n                item.TooltipText,\n                item.LabelText,\n                controls.Label",
            "full name is standard tooltip and metadata remains the hint");
        Contains(common,
            "Text hintLabel = null",
            "tooltip binding accepts the intended hint label");
        Contains(common,
            "var label = hintLabel",
            "metadata underline targets the category name");
    }

    private static void CollapseBindingUsesOnlyTheExistingModelContract()
    {
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Texture.cs");
        var style = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.StyleSystem.cs");
        var binding = Slice(
            binder,
            "private void BindCategory(PropertyCategoryRowModel item, ListenerScope listeners)",
            "private void BindSubcategory(");

        Contains(binding,
            "controls.CollapseIndicator.text = item.Collapsed\n                ? FoldGlyphs.Collapsed\n                : FoldGlyphs.Expanded;",
            "visual derives from current collapsed state");
        Contains(binding,
            "MaterialEditorStyles.SetPropertyCategoryExpanded(\n                controls.HeaderButton,\n                !item.Collapsed);",
            "expanded category styling follows only structural collapse state");
        Contains(style,
            "MaterialEditorTheme.Colors.CategoryHeaderExpanded",
            "expanded category state uses the shared theme role");
        Equal(1,
            CountOccurrences(binding, "listeners.Listen("),
            "one category action listener");
        Contains(binding,
            "() => item.CollapsedOnChange?.Invoke(!item.Collapsed)",
            "click invokes the existing collapse callback immediately");
        DoesNotContain(binding, "item.Collapsed =", "binder-owned collapsed state");
        DoesNotContain(binding, ".onClick.Invoke", "programmatic click invocation");
    }

    private static void FixedVirtualRowAndLegacyNamesRemainCompatible()
    {
        var ui = ReadSource("src", "MaterialEditor.Base", "UI", "UI.cs");
        var rowFactory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.cs");
        var textureFactory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");
        var controls = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowControls.cs");
        var theme = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Theme.cs");

        Contains(theme, "internal const float RowHeight = 22f;", "fixed 22-pixel row");
        Contains(ui,
            "internal const float PanelHeight = MaterialEditorLayout.RowHeight;",
            "legacy panel height aliases the canonical row height");
        Contains(rowFactory,
            "AddComponent<LayoutElement>().preferredHeight = PanelHeight;",
            "virtual-list template keeps fixed row height");
        foreach (var name in new[]
                 {
                     "PropertyCategoryPanel",
                     "PropertyCategoryCollapseButton",
                     "PropertyCategoryLabel"
                 })
        {
            Contains(textureFactory + controls, "\"" + name + "\"", name + " retained");
        }
        Contains(textureFactory,
            "CreateCategoryRow(parent);",
            "category remains in the pooled row template");
    }

    private static void PropertySearchAloneSuppressesHierarchyCollapseControls()
    {
        var presenter = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialEditorPresenter.cs");
        var navigator = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.CategoryNavigator.cs");
        var topBar = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.TopBarView.cs");
        var bindEntry = Slice(
            navigator,
            "private void BindEntry(Entry entry, CategoryNavigationTarget target)",
            "private static void SetEntryActive(Entry entry, bool active)");
        var hasCategories = Slice(
            topBar,
            "private static bool HasCategories(",
            "private static void SetNavigationNone(Selectable selectable)");

        Contains(
            presenter,
            "presentation.HasActiveFilter =\n                    rendererFilter.Count != 0 || propertyFilter.Count != 0;",
            "empty-state filtering still includes renderer/material filtering");
        Contains(
            presenter,
            "presentation.HasPropertyFilter = propertyFilter.Count != 0;",
            "property search has a distinct presentation flag");
        Contains(
            bindEntry,
            "!_presentation.HasPropertyFilter",
            "navigator hides collapse only during property search");
        DoesNotContain(
            bindEntry,
            "HasActiveFilter",
            "renderer/material filtering does not hide navigator disclosure");
        Contains(
            hasCategories,
            "if (presentation.HasPropertyFilter)",
            "global hierarchy actions hide during property search");
        DoesNotContain(
            hasCategories,
            "HasActiveFilter",
            "renderer/material filtering preserves global hierarchy actions");
    }

    private static void UnsupportedCategoryFeaturesStayAbsent()
    {
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Texture.cs");
        var model = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowModel.Texture.cs");
        var categoryFactory = Slice(
            factory,
            "private static void CreateCategoryRow(Transform parent)",
            "private static void CreateSubcategoryRow(Transform parent)");
        var categoryBinding = Slice(
            binder,
            "private void BindCategory(PropertyCategoryRowModel item, ListenerScope listeners)",
            "private void BindSubcategory(");
        var categoryModel = Slice(
            model,
            "internal sealed class PropertyCategoryRowModel",
            "internal sealed class PropertySubcategoryRowModel");
        var category = categoryFactory + categoryBinding + categoryModel;

        foreach (var forbidden in new[]
                 {
                     "ModifiedCount",
                     "MasterToggle",
                     "Warning",
                     "PopupMenu",
                     "ActionMenu",
                     "Animator",
                     "Animation",
                     "ContentSizeFitter",
                     "StartCoroutine",
                     "void Update()",
                     "void LateUpdate()",
                     "Rebuild"
                 })
        {
            DoesNotContain(category, forbidden, forbidden + " category feature");
        }
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
