using MaterialEditorAPI;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

internal static class RendererCollapsePresentationTests
{
    private const float RowHeight = MaterialEditorTheme.Metrics.RowHeight;

    internal static void Run()
    {
        StableKeysUseContextPathComponentAndRendererIdentity();
        ChildRowsAreLazyCachedAndOwnerScoped();
        GlobalSectionToggleUsesExistingRendererAndMaterialState();
        EveryCategoryAnchorShiftsWithRendererRows();
        ReplaceRangeFallsBackToTheRendererHeaderWithoutCoroutine();
        HundredCollapseCyclesReuseRowsViewsAndOneInvalidationEach();
        ProductionWiringKeepsCollapseVisualAndMenuIndependent();
        Console.WriteLine("Renderer collapse regression guards passed.");
    }

    private static void GlobalSectionToggleUsesExistingRendererAndMaterialState()
    {
        var rendererCollapsed = false;
        var materialCollapsed = false;
        var presentation = new MaterialEditorPresentation(150);
        var renderer = new RendererSectionPresentation(
            presentation.OwnerToken,
            "renderer",
            0,
            rendererCollapsed,
            () => new RowModel[0],
            value => rendererCollapsed = value);
        presentation.RendererSections.Add(renderer);
        presentation.MaterialSections.Add(
            new MaterialSectionPresentation(
                "material",
                "Body",
                "Shader",
                1,
                null,
                () => materialCollapsed,
                value => materialCollapsed = value));

        Equal(true, presentation.HasCollapsibleSections,
            "renderer/material sections are available to the global action");
        Equal(true, presentation.CanToggleSections,
            "global section action is available without property search");
        Equal(false, presentation.AllSectionsCollapsed,
            "expanded renderer and material sections are detected");

        presentation.SetAllSectionsCollapsed(true);
        Equal(true, rendererCollapsed,
            "global collapse uses the renderer section callback");
        Equal(true, materialCollapsed,
            "global collapse uses the material section callback");
        Equal(true, presentation.AllSectionsCollapsed,
            "fully collapsed renderer/material state is detected");

        presentation.SetAllSectionsCollapsed(false);
        Equal(false, rendererCollapsed,
            "global expand uses the renderer section callback");
        Equal(false, materialCollapsed,
            "global expand uses the material section callback");

        presentation.HasPropertyFilter = true;
        Equal(false, presentation.CanToggleSections,
            "property search disables the global section action");
        Equal(false, rendererCollapsed,
            "disabling the action preserves renderer collapse state");
        Equal(false, materialCollapsed,
            "disabling the action preserves material collapse state");
        presentation.HasPropertyFilter = false;
        Equal(true, presentation.CanToggleSections,
            "clearing property search restores the global section action");

        var categoryOnlyPresentation = new MaterialEditorPresentation(151);
        categoryOnlyPresentation.MaterialSections.Add(
            new MaterialSectionPresentation(
                "category-only",
                "Body",
                "Shader",
                0,
                null));
        Equal(false, categoryOnlyPresentation.HasCollapsibleSections,
            "legacy presentation without section callbacks stays unaffected");
        Equal(false, categoryOnlyPresentation.AllSectionsCollapsed,
            "empty section state is never reported as collapsed");
    }

    private static void StableKeysUseContextPathComponentAndRendererIdentity()
    {
        var root = new GameObject();
        var renderer = new Renderer();
        var stable = MaterialEditorSectionKeys.Renderer(
            root,
            renderer,
            "Body[0]/Top[2]",
            1);
        Equal(stable,
            MaterialEditorSectionKeys.Renderer(
                root,
                renderer,
                "Body[0]/Top[2]",
                1),
            "renderer key is stable for one presenter identity");
        NotEqual(stable,
            MaterialEditorSectionKeys.Renderer(
                root,
                renderer,
                "Body[0]/Top[3]",
                1),
            "relative transform path participates in renderer identity");
        NotEqual(stable,
            MaterialEditorSectionKeys.Renderer(
                root,
                renderer,
                "Body[0]/Top[2]",
                2),
            "component index participates in renderer identity");
        NotEqual(stable,
            MaterialEditorSectionKeys.Renderer(
                root,
                new Renderer(),
                "Body[0]/Top[2]",
                1),
            "renderer instance disambiguates a replaced component");

        var session = new MaterialEditorSessionState();
        session.SetRendererCollapsed(stable, true);
        session.ClearTargetReferences();
        Equal(false,
            MaterialEditorSessionState.IsCollapsed(
                session.CollapsedRendererSections,
                stable),
            "destroyed target release clears renderer-fold identities");
        Equal(false,
            MaterialEditorSessionState.IsCollapsed(
                session.CollapsedRendererSections,
                MaterialEditorSectionKeys.Renderer(
                    root,
                    renderer,
                    "Body[0]/Top[2]",
                    2)),
            "a different component does not inherit the renderer fold");
        for (var index = 0;
             index < MaterialEditorSessionState.CollapsedRendererStateLimit + 20;
             index++)
            session.SetRendererCollapsed("renderer:" + index, true);
        Equal(MaterialEditorSessionState.CollapsedRendererStateLimit,
            session.CollapsedRendererSections.Count,
            "renderer-fold state is bounded for long live-target sessions");
        Equal(false,
            MaterialEditorSessionState.IsCollapsed(
                session.CollapsedRendererSections,
                "renderer:0"),
            "oldest renderer-fold state is evicted at the cap");
        session.SetRendererCollapsed("renderer:531", false);
        Equal(MaterialEditorSessionState.CollapsedRendererStateLimit - 1,
            session.CollapsedRendererSections.Count,
            "expanding removes the local renderer preference entry");
    }

    private static void ChildRowsAreLazyCachedAndOwnerScoped()
    {
        var buildCount = 0;
        var storedCollapsed = true;
        var children = CreateRows(3, "Renderer child ");
        var presentation = CreateCollapsedPresentation(
            101,
            children,
            () => buildCount++,
            value => storedCollapsed = value);
        var section = presentation.RendererSections[0];

        Equal(false, section.ChildRowsCreated,
            "closed renderer creates no child row models");
        Equal(1, presentation.Rows.Count,
            "closed renderer exposes only its header");

        int start;
        int remove;
        IList<RowModel> replacement;
        Equal(true,
            presentation.TrySetRendererCollapsed(
                section,
                false,
                out start,
                out remove,
                out replacement),
            "first expand mutation");
        Equal(1, start, "children replace immediately after header");
        Equal(0, remove, "first expand removes no rows");
        Equal(3, replacement.Count, "three renderer children inserted");
        Equal(1, buildCount, "lazy child builder runs once");
        Equal(false, storedCollapsed, "local UI preference updated");

        Equal(true,
            presentation.TrySetRendererCollapsed(
                section,
                true,
                out start,
                out remove,
                out replacement),
            "collapse mutation");
        Equal(3, remove, "collapse removes all renderer children");
        Equal(0, replacement.Count, "collapse inserts no child rows");
        Equal(1, presentation.Rows.Count,
            "collapsed presentation retains only renderer header");
        Equal(1, buildCount, "collapse does not rebuild child models");

        Equal(true,
            presentation.TrySetRendererCollapsed(
                section,
                false,
                out start,
                out remove,
                out replacement),
            "second expand mutation");
        Equal(1, buildCount, "reopen reuses cached child models");
        for (var index = 0; index < children.Count; index++)
            Equal(true,
                ReferenceEquals(children[index], replacement[index]),
                "cached renderer child " + index);

        var replacementPresentation = new MaterialEditorPresentation(102);
        replacementPresentation.RendererSections.Add(section);
        Equal(false,
            replacementPresentation.TrySetRendererCollapsed(
                section,
                true,
                out start,
                out remove,
                out replacement),
            "stale owner-token callback is ignored");
    }

    private static void EveryCategoryAnchorShiftsWithRendererRows()
    {
        var children = CreateRows(3, "Renderer child ");
        var presentation = CreateCollapsedPresentation(
            201,
            children,
            () => { },
            _ => { });
        presentation.Rows.AddRange(CreateRows(5, "Material row "));
        var material = new MaterialSectionPresentation(
            "material",
            "Body",
            "Shader",
            1,
            null)
        {
            EndRowIndex = 5
        };
        var category = material.AddCategory(
            "Shared",
            2,
            "manifest",
            () => false,
            _ => { },
            null);
        material.AddCategory(
            "Shared",
            4,
            "extension",
            () => false,
            _ => { },
            null);
        presentation.MaterialSections.Add(material);

        int start;
        int remove;
        IList<RowModel> replacement;
        presentation.TrySetRendererCollapsed(
            presentation.RendererSections[0],
            false,
            out start,
            out remove,
            out replacement);
        Equal(4, material.MaterialRowIndex,
            "material anchor shifts after renderer expansion");
        Equal(8, material.EndRowIndex,
            "material end shifts after renderer expansion");
        Equal(5, category.RowIndex,
            "first category anchor shifts after renderer expansion");
        Equal(7, category.FindRowAnchorAtOrBefore(7),
            "second non-contiguous category anchor also shifts");
        Equal(category,
            material.FindCategoryAtRow(7),
            "scroll-spy resolves the shifted repeated anchor");

        presentation.TrySetRendererCollapsed(
            presentation.RendererSections[0],
            true,
            out start,
            out remove,
            out replacement);
        Equal(1, material.MaterialRowIndex,
            "material anchor returns after renderer collapse");
        Equal(2, category.RowIndex,
            "first category anchor returns after renderer collapse");
        Equal(4, category.FindRowAnchorAtOrBefore(4),
            "every category anchor returns after renderer collapse");
    }

    private static void ReplaceRangeFallsBackToTheRendererHeaderWithoutCoroutine()
    {
        var children = CreateRows(3, "Renderer child ");
        var presentation = CreateCollapsedPresentation(
            301,
            children,
            () => { },
            _ => { });
        presentation.Rows.AddRange(CreateRows(4, "Trailing row "));
        var list = CreateVirtualList(RowHeight * 2f);
        list.SetList(presentation.Rows, false);

        int start;
        int remove;
        IList<RowModel> replacement;
        presentation.TrySetRendererCollapsed(
            presentation.RendererSections[0],
            false,
            out start,
            out remove,
            out replacement);
        list.ReplaceRange(start, remove, replacement, 0);
        list.ScrollToIndex(1);
        InvokeUpdate(list);
        Equal(1, list.ViewportAnchorIndex,
            "renderer child is the pre-collapse semantic anchor");
        var notifications = 0;
        list.ViewportAnchorIndexChanged += _ => notifications++;

        presentation.TrySetRendererCollapsed(
            presentation.RendererSections[0],
            true,
            out start,
            out remove,
            out replacement);
        list.ReplaceRange(start, remove, replacement, 0);
        foreach (var view in CachedViews(list))
        {
            Equal(false,
                view.CurrentModel != null && children.Contains(view.CurrentModel),
                "removed child RowViews are released before ReplaceRange returns");
        }
        Equal(0f, list.ScrollRect.content.localPosition.y,
            "removed child top row falls back to renderer header");
        Equal(0, list.ViewportAnchorIndex,
            "removed semantic anchor falls back to renderer header");
        Equal(false,
            GetField<bool>(list, "_programmaticViewportAnchorPinned"),
            "range replacement releases a prior programmatic pin");
        Equal(0, list.PendingCoroutineCount,
            "range replacement never schedules anchor restoration");
        InvokeUpdate(list);
        Equal(1, notifications,
            "range replacement publishes its fallback anchor exactly once");
        list.ScrollRect.content.localPosition =
            new Vector3(0f, RowHeight, 0f);
        InvokeUpdate(list);
        Equal(2, notifications,
            "manual scroll resumes anchor publication after replacement");
    }

    private static void HundredCollapseCyclesReuseRowsViewsAndOneInvalidationEach()
    {
        var buildCount = 0;
        var children = CreateRows(5, "Renderer child ");
        var presentation = CreateCollapsedPresentation(
            401,
            children,
            () => buildCount++,
            _ => { });
        presentation.Rows.AddRange(CreateRows(3, "Tail "));
        var material = new MaterialSectionPresentation(
            "stress-material",
            "Body",
            "Shader",
            1,
            null)
        {
            EndRowIndex = 3
        };
        var category = material.AddCategory(
            "Shared",
            2,
            "stress-manifest",
            () => false,
            _ => { },
            null);
        material.AddCategory(
            "Shared",
            3,
            "stress-extension",
            () => false,
            _ => { },
            null);
        presentation.MaterialSections.Add(material);
        var list = CreateVirtualList(RowHeight * 4f);
        list.SetList(presentation.Rows, false);
        InvokeUpdate(list);

        MaterialEditorPerformance.Configure(false, true, 0d, null, null);
        MaterialEditorPerformance.Reset();
        LayoutRebuilder.Reset();
        try
        {
            var expandedPoolSize = -1;
            var anchorPublications = 0;
            list.ViewportAnchorIndexChanged += _ => anchorPublications++;
            for (var cycle = 0; cycle < 100; cycle++)
            {
                var collapsed = cycle % 2 != 0;
                int start;
                int remove;
                IList<RowModel> replacement;
                Equal(true,
                    presentation.TrySetRendererCollapsed(
                        presentation.RendererSections[0],
                        collapsed,
                        out start,
                        out remove,
                        out replacement),
                    "renderer mutation cycle " + cycle);
                list.ReplaceRange(start, remove, replacement, 0);
                InvokeUpdate(list);

                Equal(presentation.Rows.Count,
                    Models(list).Count,
                    "presentation/list count cycle " + cycle);
                for (var rowIndex = 0;
                     rowIndex < presentation.Rows.Count;
                     rowIndex++)
                {
                    Equal(true,
                        ReferenceEquals(
                            presentation.Rows[rowIndex],
                            Models(list)[rowIndex]),
                        "presentation/list row " + cycle + "/" + rowIndex);
                }

                if (!collapsed && expandedPoolSize < 0)
                    expandedPoolSize = list.CachedViewCount;
                if (expandedPoolSize >= 0)
                    Equal(expandedPoolSize,
                        list.CachedViewCount,
                        "pooled view high-water cycle " + cycle);
                if (collapsed)
                {
                    Equal(1, material.MaterialRowIndex,
                        "material anchor restored cycle " + cycle);
                    Equal(3, material.EndRowIndex,
                        "material end restored cycle " + cycle);
                    Equal(2, category.RowIndex,
                        "first category anchor restored cycle " + cycle);
                    Equal(3, category.FindRowAnchorAtOrBefore(3),
                        "last category anchor restored cycle " + cycle);
                    foreach (var view in CachedViews(list))
                    {
                        Equal(false,
                            view.CurrentModel != null
                            && children.Contains(view.CurrentModel),
                            "closed renderer has no active child RowView " + cycle);
                    }
                }
                else
                {
                    Equal(6, material.MaterialRowIndex,
                        "material anchor shifted cycle " + cycle);
                    Equal(8, material.EndRowIndex,
                        "material end shifted cycle " + cycle);
                    Equal(7, category.RowIndex,
                        "first category anchor shifted cycle " + cycle);
                    Equal(8, category.FindRowAnchorAtOrBefore(8),
                        "last category anchor shifted cycle " + cycle);
                }
                Equal(0, list.PendingCoroutineCount,
                    "no replacement coroutine cycle " + cycle);
            }

            var metrics = MaterialEditorPerformance.CaptureSnapshot();
            Equal(100L,
                metrics.GetCount(
                    MaterialEditorPerformanceMetric.CacheInvalidations),
                "one cache invalidation per collapse transition");
            Equal(100, anchorPublications,
                "one anchor publication per collapse transition");
            Equal(100, LayoutRebuilder.MarkCount,
                "one virtualized layout invalidation per collapse transition");
            Equal(0L,
                metrics.GetCount(
                    MaterialEditorPerformanceMetric.RefreshRequests),
                "renderer collapse requests no full refresh");
            Equal(0L,
                metrics.GetCount(
                    MaterialEditorPerformanceMetric.RefreshExecuted),
                "renderer collapse executes no full refresh");
            Equal(1, buildCount,
                "one lazy child build across one hundred transitions");
        }
        finally
        {
            MaterialEditorPerformance.Configure(false, false, 0d, null, null);
            MaterialEditorPerformance.Reset();
        }
    }

    private static void ProductionWiringKeepsCollapseVisualAndMenuIndependent()
    {
        var presenter = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialEditorPresenter.cs");
        var ui = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.cs");
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Renderer.cs");
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Renderer.cs");
        var controls = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowControls.cs");
        var virtualList = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.VirtualList.cs");
        var session = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.SessionState.cs");

        Contains(session,
            "CollapsedRendererSections",
            "renderer collapse has a session-only preference store");
        var addRenderer = Slice(
            presenter,
            "private void AddRendererRows(",
            "private IList<RowModel> BuildRendererChildRows(");
        Contains(addRenderer,
            "MaterialEditorSectionKeys.Renderer(",
            "renderer section uses its stable presenter key");
        Contains(addRenderer,
            "new RendererSectionPresentation(",
            "renderer section carries the presentation owner token");
        Contains(addRenderer,
            "() => BuildRendererChildRows(",
            "renderer children use a lazy builder");
        DoesNotContain(addRenderer,
            "_actions.Refresh(",
            "renderer collapse never requests a full rebuild");

        var childBuilder = Slice(
            presenter,
            "private IList<RowModel> BuildRendererChildRows(",
            "private static string GetRelativeRendererPath(");
        Contains(childBuilder,
            "new List<RowModel>(5)",
            "renderer child builder is bounded to the known three-to-five rows");
        Equal(5,
            CountOccurrences(childBuilder, "rows.Add(new Renderer"),
            "five possible renderer child model types");

        var toggle = Slice(
            ui,
            "private void SetRendererCollapsed(",
            "private void ToggleAllCategories()");
        Contains(toggle,
            "presentation.TrySetRendererCollapsed(",
            "collapse validates the current owner presentation");
        Contains(toggle,
            "VirtualList.ReplaceRange(",
            "collapse mutates only the visible range");
        Contains(toggle,
            "_windowView?.RefreshSectionCollapseState(presentation);",
            "targeted renderer collapse refreshes the global menu state");
        foreach (var forbidden in new[]
                 {
                     "PopulateList(",
                     "StartCoroutine",
                     "_actions.Refresh",
                     "GetRendererList",
                     "ShaderUiMetadata",
                     "Provider",
                     "SetRendererProperty",
                     "ResetRendererProperty",
                     "Undo"
                 })
            DoesNotContain(toggle, forbidden,
                forbidden + " forbidden renderer-collapse side effect");

        var bindRenderer = Slice(
            binder,
            "private void BindRenderer(",
            "private static void BindToggle(");
        Contains(bindRenderer,
            "controls.CollapseButton",
            "renderer header has a dedicated collapse binding");
        Contains(bindRenderer,
            "controls.HeaderButton",
            "renderer header background toggles the same section");
        Contains(bindRenderer,
            "toggleCollapsed();",
            "renderer name click preserves label selection and toggles the section");
        Contains(bindRenderer,
            "controls.ActionMenuButton",
            "renderer actions retain a separate menu binding");
        Equal(1,
            CountOccurrences(bindRenderer, "CollapsedOnChange"),
            "all header hit surfaces share one renderer-collapse callback");
        Equal(true,
            bindRenderer.IndexOf(
                "refreshCollapsedGlyph();",
                bindRenderer.IndexOf(
                    "CollapsedOnChange",
                    StringComparison.Ordinal),
                StringComparison.Ordinal) >= 0,
            "visible renderer header refreshes its glyph after the model changes");
        Contains(factory,
            "\"RendererCollapseButton\"",
            "renderer template creates the dedicated header button");
        Contains(factory,
            "panel.gameObject.AddComponent<Button>()",
            "renderer template makes the header background clickable");
        Contains(controls,
            "CollapseButton = owner.GetUIComponent<Button>(\"RendererCollapseButton\");",
            "renderer controls resolve the dedicated header button");
        Contains(controls,
            "HeaderButton = owner.GetUIComponent<Button>(\"RendererPanel\");",
            "renderer controls resolve the background hit surface");

        var replaceRange = Slice(
            virtualList,
            "internal void ReplaceRange(",
            "internal void ReleaseContent()");
        Equal(1,
            CountOccurrences(replaceRange,
                "MaterialEditorPerformanceMetric.CacheInvalidations"),
            "range replacement records one logical invalidation");
        Equal(1,
            CountOccurrences(replaceRange,
                "MaterialEditorPerformanceMetric.VisibleRowsInvalidations"),
            "range replacement records one visible-row invalidation");
        Equal(1,
            CountOccurrences(replaceRange, "_dirty = true;"),
            "range replacement dirties virtualization once");
        Equal(1,
            CountOccurrences(replaceRange, "SetViewportAnchorIndex("),
            "range replacement publishes one anchor update");
        Equal(1,
            CountOccurrences(replaceRange,
                "ClearProgrammaticViewportAnchor();"),
            "range replacement cancels one stale programmatic pin");
        DoesNotContain(replaceRange,
            "StartCoroutine",
            "range replacement restores no anchor coroutine");
        DoesNotContain(replaceRange,
            "LayoutRebuilder",
            "range replacement synchronizes one layout pass through virtualization");
        DoesNotContain(replaceRange,
            "CaptureTopRowAnchor",
            "range replacement uses constant-time anchor arithmetic");
    }

    private static MaterialEditorPresentation CreateCollapsedPresentation(
        int ownerToken,
        IList<RowModel> children,
        Action built,
        Action<bool> collapsedChanged)
    {
        var presentation = new MaterialEditorPresentation(ownerToken);
        presentation.Rows.Add(new RendererRowModel());
        var section = new RendererSectionPresentation(
            ownerToken,
            "renderer",
            0,
            true,
            () =>
            {
                built();
                return children;
            },
            collapsedChanged);
        presentation.RendererSections.Add(section);
        return presentation;
    }

    private static List<RowModel> CreateRows(int count, string prefix)
    {
        var rows = new List<RowModel>(count);
        for (var index = 0; index < count; index++)
        {
            rows.Add(new TestPropertyRowModel(
                RowModel.RowItemType.FloatProperty,
                prefix + index,
                prefix + index));
        }
        return rows;
    }

    private static VirtualList CreateVirtualList(float viewportHeight)
    {
        var contentObject = new GameObject();
        var content = contentObject.AddComponent<RectTransform>();
        content.rect = new Rect(0f, 0f, 300f, 4000f);
        contentObject.AddComponent<VerticalLayoutGroup>();

        var viewportObject = new GameObject();
        var viewport = viewportObject.AddComponent<RectTransform>();
        viewport.rect = new Rect(0f, 0f, 300f, viewportHeight);

        var scrollObject = new GameObject();
        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;

        var template = new GameObject();
        template.transform.parent = content;

        var listObject = new GameObject();
        var list = listObject.AddComponent<VirtualList>();
        list.ScrollRect = scrollRect;
        list.EntryTemplate = template;
        list.Initialize();
        return list;
    }

    private static List<RowModel> Models(VirtualList list) =>
        GetField<List<RowModel>>(list, "_models");

    private static List<RowView> CachedViews(VirtualList list) =>
        GetField<List<RowView>>(list, "_cachedViews");

    private static T GetField<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("Field not found: " + name);
        return (T)field.GetValue(instance);
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
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
        }
    }

    private static void NotEqual<T>(T first, T second, string name)
    {
        if (EqualityComparer<T>.Default.Equals(first, second))
            throw new InvalidOperationException(name + ": values unexpectedly match.");
    }
}
