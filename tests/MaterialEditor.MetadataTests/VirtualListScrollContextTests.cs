using MaterialEditorAPI;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

internal static class VirtualListScrollContextTests
{
    private const float RowHeight = MaterialEditorUI.PanelHeight;

    internal static void Run()
    {
        ExactIdentitySurvivesInsertedRowsAndDecoys();
        MissingAnchorFallsBackWithinItsCategory();
        SilentRebuildPublishesOneAtomicViewportNotification();
        ExplicitScrollCancelsDeferredRestore();
        ProgrammaticNavigationPinsTheExactRequestedAnchor();
        ExactVisibleRangeExcludesTheOverscanRow();
        ClickedNavigationAndHighlightKeysStayEqual();
        CategoryChevronUsesTheSameStableNavigationTarget();
        ProgrammaticPinIsClearedByListLifecycle();
        Console.WriteLine("Virtual-list viewport-context regression tests passed.");
    }

    private static void ExactIdentitySurvivesInsertedRowsAndDecoys()
    {
        var gameObject = new GameObject();
        var renderer = new Renderer();
        var materialA = new Material { name = "Material A" };
        var materialB = new Material { name = "Material B" };
        var list = CreateVirtualList(viewportHeight: RowHeight);

        var original = new List<RowModel>();
        AddContext(original, gameObject, renderer, materialA, "Surface");
        var originalTarget = Property(
            RowModel.RowItemType.FloatProperty,
            "Shared label",
            "TargetProperty");
        original.Add(originalTarget);

        list.SetList(original, false);
        SetScrollPosition(list, original.IndexOf(originalTarget) * RowHeight + 6.25f);
        var anchor = list.CaptureTopRowAnchor();

        var rebuilt = new List<RowModel>();
        AddContext(rebuilt, gameObject, renderer, materialA, "Surface");
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Inserted row",
            "InsertedOnly"));
        rebuilt.Add(Property(
            RowModel.RowItemType.ColorProperty,
            "Shared label",
            "TargetProperty")); // wrong ItemType
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Shared label",
            "DifferentProperty")); // wrong PropertyName
        rebuilt.Add(new PropertyCategoryRowModel("Other category"));
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Shared label",
            "TargetProperty")); // wrong category
        AddMaterialContext(rebuilt, gameObject, materialB, "Surface");
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Shared label",
            "TargetProperty")); // wrong material
        AddMaterialContext(rebuilt, gameObject, materialA, "Surface");
        var rebuiltTarget = Property(
            RowModel.RowItemType.FloatProperty,
            "Shared label",
            "TargetProperty");
        rebuilt.Add(rebuiltTarget);
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Trailing one",
            "TrailingOne"));
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Trailing two",
            "TrailingTwo"));

        list.SetList(rebuilt, false);
        list.RestoreTopRowAnchor(anchor, false);
        Equal(
            rebuilt.IndexOf(rebuiltTarget) * RowHeight + 6.25f,
            ScrollPosition(list),
            "identity uses ItemType/material/property/category");

        list.RunPendingCoroutines();
        Equal(
            rebuilt.IndexOf(rebuiltTarget) * RowHeight + 6.25f,
            ScrollPosition(list),
            "post-layout restore keeps the exact identity");
    }

    private static void MissingAnchorFallsBackWithinItsCategory()
    {
        var gameObject = new GameObject();
        var renderer = new Renderer();
        var material = new Material { name = "Material" };
        var list = CreateVirtualList(viewportHeight: RowHeight);

        var original = new List<RowModel>();
        AddContext(original, gameObject, renderer, material, "Surface");
        var before = Property(
            RowModel.RowItemType.FloatProperty,
            "Before",
            "Before");
        var removed = Property(
            RowModel.RowItemType.FloatProperty,
            "Removed target",
            "RemovedTarget");
        var after = Property(
            RowModel.RowItemType.FloatProperty,
            "After",
            "After");
        original.Add(before);
        original.Add(removed);
        original.Add(after);

        list.SetList(original, false);
        SetScrollPosition(list, original.IndexOf(removed) * RowHeight + 3.5f);
        var anchor = list.CaptureTopRowAnchor();

        var rebuilt = new List<RowModel>();
        AddContext(rebuilt, gameObject, renderer, material, "Surface");
        var rebuiltBefore = Property(
            RowModel.RowItemType.FloatProperty,
            "Before",
            "Before");
        rebuilt.Add(rebuiltBefore);
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "After",
            "After"));

        list.SetList(rebuilt, false);
        list.RestoreTopRowAnchor(anchor, false);
        Equal(
            rebuilt.IndexOf(rebuiltBefore) * RowHeight + 3.5f,
            ScrollPosition(list),
            "removed row falls back to nearest prior row in category");
    }

    private static void SilentRebuildPublishesOneAtomicViewportNotification()
    {
        var gameObject = new GameObject();
        var renderer = new Renderer();
        var material = new Material();
        var list = CreateVirtualList(viewportHeight: RowHeight);
        var original = new List<RowModel>();
        AddContext(original, gameObject, renderer, material, "Surface");
        var target = Property(
            RowModel.RowItemType.FloatProperty,
            "Target",
            "Target");
        original.Add(target);
        list.SetList(original, false);
        SetScrollPosition(list, original.IndexOf(target) * RowHeight + 2f);
        var anchor = list.CaptureTopRowAnchor();

        var rebuilt = new List<RowModel>();
        AddContext(rebuilt, gameObject, renderer, material, "Surface");
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Inserted",
            "Inserted"));
        rebuilt.Add(Property(
            RowModel.RowItemType.FloatProperty,
            "Target",
            "Target"));

        var notifications = 0;
        list.ViewportAnchorIndexChanged += _ => notifications++;
        list.SetList(rebuilt, false);
        Equal(0, notifications, "SetList(..., false) is silent");
        list.RestoreTopRowAnchor(anchor, false);
        Equal(0, notifications, "silent immediate restore");
        list.PublishViewportAnchor();
        Equal(1, notifications, "one atomic publication");
        Equal(1, list.PendingCoroutineCount, "one deferred layout correction");
        list.RunPendingCoroutines();
        Equal(1, notifications, "deferred correction does not republish");
    }

    private static void ExplicitScrollCancelsDeferredRestore()
    {
        var list = CreateVirtualList(viewportHeight: RowHeight);
        var rows = Enumerable.Range(0, 12)
            .Select(index => Property(
                RowModel.RowItemType.FloatProperty,
                "Property " + index,
                "Property" + index))
            .Cast<RowModel>()
            .ToList();

        list.SetList(rows, false);
        SetScrollPosition(list, 8 * RowHeight + 4f);
        var anchor = list.CaptureTopRowAnchor();
        list.SetList(rows, false);
        list.RestoreTopRowAnchor(anchor, false);
        Equal(1, list.PendingCoroutineCount, "deferred restore is pending");

        list.ScrollToIndex(2);
        Equal(2 * RowHeight, ScrollPosition(list), "explicit navigation position");
        list.RunPendingCoroutines();
        Equal(
            2 * RowHeight,
            ScrollPosition(list),
            "ScrollToIndex cancels the stale deferred restore");
    }

    private static void ProgrammaticNavigationPinsTheExactRequestedAnchor()
    {
        var list = CreateVirtualList(viewportHeight: 10 * RowHeight);
        var rows = Enumerable.Range(0, 30)
            .Select(index => Property(
                RowModel.RowItemType.FloatProperty,
                "Property " + index,
                "Property" + index))
            .Cast<RowModel>()
            .ToList();
        list.SetList(rows, false);
        InvokeUpdate(list);

        var notifications = new List<int>();
        list.ViewportAnchorIndexChanged += notifications.Add;

        list.ScrollToIndex(2);
        Equal(2 * RowHeight, ScrollPosition(list),
            "programmatic navigation moves to requested row");
        Equal(2, list.ViewportAnchorIndex,
            "programmatic navigation publishes the requested row, not the 30 percent spy point");
        Equal(1, notifications.Count,
            "programmatic navigation publishes one logical selection");
        Equal(2, notifications[0], "programmatic selection notification");

        InvokeUpdate(list);
        Equal(2, list.ViewportAnchorIndex,
            "virtualization update does not overwrite programmatic selection");
        Equal(1, notifications.Count,
            "virtualization update does not republish a shifted selection");

        SetScrollPosition(list, 4 * RowHeight);
        InvokeUpdate(list);
        Equal(7, list.ViewportAnchorIndex,
            "manual scroll resumes the 30 percent scroll spy");
        Equal(2, notifications.Count,
            "manual scroll publishes one new logical selection");
        Equal(7, notifications[1], "manual scroll selection notification");
        Equal(false, GetField<bool>(list, "_programmaticViewportAnchorPinned"),
            "manual scroll releases the programmatic pin");

        list.ScrollToIndex(rows.Count - 1);
        Equal(20 * RowHeight, ScrollPosition(list),
            "last row navigation clamps to maximum scroll position");
        Equal(rows.Count - 1, list.ViewportAnchorIndex,
            "last requested row remains the selected anchor despite clamp");
        InvokeUpdate(list);
        Equal(rows.Count - 1, list.ViewportAnchorIndex,
            "last requested row remains selected after virtualization update");
    }

    private static void ProgrammaticPinIsClearedByListLifecycle()
    {
        var list = CreateVirtualList(viewportHeight: 4 * RowHeight);
        var rows = Enumerable.Range(0, 12)
            .Select(index => Property(
                RowModel.RowItemType.FloatProperty,
                "Property " + index,
                "Property" + index))
            .Cast<RowModel>()
            .ToList();
        list.SetList(rows, false);

        list.ScrollToIndex(8);
        Equal(true, GetField<bool>(list, "_programmaticViewportAnchorPinned"),
            "explicit navigation establishes pin");
        list.SetList(rows, false);
        Equal(false, GetField<bool>(list, "_programmaticViewportAnchorPinned"),
            "SetList clears pin");

        list.ScrollToIndex(7);
        var anchor = list.CaptureTopRowAnchor();
        list.RestoreTopRowAnchor(anchor, false);
        Equal(false, GetField<bool>(list, "_programmaticViewportAnchorPinned"),
            "viewport restore clears pin");

        list.ScrollToIndex(6);
        list.ReleaseContent();
        Equal(false, GetField<bool>(list, "_programmaticViewportAnchorPinned"),
            "content release clears pin");
    }

    private static void ExactVisibleRangeExcludesTheOverscanRow()
    {
        var list = CreateVirtualList(viewportHeight: 2 * RowHeight);
        var rows = Enumerable.Range(0, 10)
            .Select(index => Property(
                RowModel.RowItemType.FloatProperty,
                "Property " + index,
                "Property" + index))
            .Cast<RowModel>()
            .ToList();
        list.SetList(rows, false);

        int first;
        int last;
        SetScrollPosition(list, 3 * RowHeight);
        Equal(true, list.TryGetVisibleRowRange(out first, out last),
            "exact visible range is available");
        Equal(3, first, "first fully aligned visible row");
        Equal(4, last, "row at the exact lower boundary is excluded");

        SetScrollPosition(list, 3 * RowHeight + 5f);
        Equal(true, list.TryGetVisibleRowRange(out first, out last),
            "partial visible range is available");
        Equal(3, first, "partially clipped top row remains visible");
        Equal(5, last, "partially visible lower row is included");
    }

    private static void ClickedNavigationAndHighlightKeysStayEqual()
    {
        var list = CreateVirtualList(viewportHeight: 10 * RowHeight);
        var rows = Enumerable.Range(0, 30)
            .Select(index => Property(
                RowModel.RowItemType.FloatProperty,
                "Property " + index,
                "Property" + index))
            .Cast<RowModel>()
            .ToList();
        list.SetList(rows, false);
        InvokeUpdate(list);

        var section = new MaterialSectionPresentation(
            "section",
            "Material",
            "Test/Shader",
            0,
            null);
        for (var index = 0; index < 10; index++)
        {
            section.AddCategory(
                "Category " + index,
                index + 2,
                "state:" + index,
                () => false,
                value => { },
                null);
        }

        var clicked = section.Categories[2];
        var clickedStableKey = clicked.Id;
        var navigationStableKey = clicked.Id;
        string highlightStableKey = null;
        list.ViewportAnchorIndexChanged += rowIndex =>
            highlightStableKey = section.FindCategoryAtRow(rowIndex)?.Id;

        list.ScrollToIndex(clicked.RowIndex);
        Equal(clickedStableKey, navigationStableKey,
            "clicked key equals navigation key");
        Equal(clickedStableKey, highlightStableKey,
            "clicked key equals immediate highlight key");
        InvokeUpdate(list);
        Equal(clickedStableKey, highlightStableKey,
            "virtualization cannot drift highlight to a nearby category");
    }

    private static void CategoryChevronUsesTheSameStableNavigationTarget()
    {
        var ui = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.cs"));
        var toggle = ExtractMethod(
            ui,
            "private void ToggleCategory(CategoryNavigationTarget target)");

        Contains(toggle,
            "var sectionId = target.SectionId;",
            "collapse captures the clicked section key before rebuild");
        Contains(toggle,
            "var categoryId = target.Id;",
            "collapse captures the clicked category key before rebuild");
        Contains(toggle,
            "RebuildAndScrollToCategory(sectionId, categoryId);",
            "chevron resolves and highlights the same stable target atomically");
        Equal(0, CountOccurrences(toggle, "PopulateList("),
            "collapse delegates instead of exposing an intermediate anchor");
        Equal(0, CountOccurrences(toggle, "StartCoroutine("),
            "collapse does not defer the requested highlight by one frame");

        var rebuild = ExtractMethod(
            ui,
            "private void RebuildAndScrollToCategory(")
            .Replace("\r\n", "\n");
        Equal(1, CountOccurrences(rebuild, "PopulateListCore("),
            "one logical visible-row invalidation");
        Contains(rebuild,
            "false,\n                false);",
            "rebuild suppresses publication of the restored category anchor");
        Equal(1, CountOccurrences(rebuild, "VirtualList.ScrollToIndex("),
            "requested stable target is the sole success publication");
        Equal(1, CountOccurrences(rebuild, "VirtualList.PublishViewportAnchor();"),
            "missing-target fallback publishes exactly once");
    }

    private static VirtualList CreateVirtualList(float viewportHeight)
    {
        var contentObject = new GameObject();
        var content = contentObject.AddComponent<RectTransform>();
        content.rect = new Rect(0f, 0f, 300f, 4000f);
        var layout = contentObject.AddComponent<VerticalLayoutGroup>();

        var viewportObject = new GameObject();
        var viewport = viewportObject.AddComponent<RectTransform>();
        viewport.rect = new Rect(0f, 0f, 300f, viewportHeight);

        var scrollObject = new GameObject();
        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;

        var listObject = new GameObject();
        var list = listObject.AddComponent<VirtualList>();
        list.ScrollRect = scrollRect;
        SetField(list, "_verticalLayoutGroup", layout);
        return list;
    }

    private static T GetField<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("Field not found: " + name);
        return (T)field.GetValue(instance);
    }

    private static void SetField(object instance, string name, object value)
    {
        var field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("Field not found: " + name);
        field.SetValue(instance, value);
    }

    private static void InvokeUpdate(VirtualList list)
    {
        var update = typeof(VirtualList).GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (update == null)
            throw new InvalidOperationException("VirtualList.Update not found.");
        update.Invoke(list, null);
    }

    private static void AddContext(
        ICollection<RowModel> rows,
        GameObject gameObject,
        Renderer renderer,
        Material material,
        string category)
    {
        rows.Add(new RendererRowModel
        {
            GameObject = gameObject,
            Renderer = renderer
        });
        AddMaterialContext(rows, gameObject, material, category);
    }

    private static void AddMaterialContext(
        ICollection<RowModel> rows,
        GameObject gameObject,
        Material material,
        string category)
    {
        rows.Add(new MaterialRowModel
        {
            GameObject = gameObject,
            Material = material
        });
        rows.Add(new ShaderRowModel { ShaderName = "Test/Shader" });
        rows.Add(new PropertyCategoryRowModel(category));
    }

    private static TestPropertyRowModel Property(
        RowModel.RowItemType itemType,
        string label,
        string propertyName) =>
        new TestPropertyRowModel(itemType, label, propertyName);

    private static float ScrollPosition(VirtualList list) =>
        list.ScrollRect.content.localPosition.y;

    private static void SetScrollPosition(VirtualList list, float value)
    {
        var position = list.ScrollRect.content.localPosition;
        position.y = value;
        list.ScrollRect.content.localPosition = position;
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
            throw new InvalidOperationException("Method not found: " + signature);
        var openingBrace = source.IndexOf('{', signatureIndex);
        if (openingBrace < 0)
            throw new InvalidOperationException("Method body not found: " + signature);

        var depth = 0;
        for (var index = openingBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(openingBrace, index - openingBrace + 1);
        }

        throw new InvalidOperationException("Unterminated method: " + signature);
    }

    private static string ReadRepositorySource(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));

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

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Contains(string source, string expected, string name) =>
        Equal(true, source.Contains(expected, StringComparison.Ordinal), name);

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
    }
}
