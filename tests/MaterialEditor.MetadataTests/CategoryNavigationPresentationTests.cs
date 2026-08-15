using MaterialEditorAPI;
using UnityEngine;

internal static class CategoryNavigationPresentationTests
{
    internal static void Run()
    {
        StableKeysSurviveVisibleRowChangesAndRebuilds();
        RepeatedLogicalCategoriesUseTheirLatestVisibleAnchor();
        ParentExpansionReportsWhetherRowsMustBeRebuilt();
        PooledEntryListenersAlwaysUseTheLatestBoundTarget();
        Console.WriteLine("Category-navigation presentation tests passed.");
    }

    private static void StableKeysSurviveVisibleRowChangesAndRebuilds()
    {
        var gameObject = new GameObject();
        var material = new Material();
        var sectionKey = MaterialEditorSectionKeys.Shader(
            gameObject,
            material,
            "Test/Shader");
        var first = new MaterialSectionPresentation(
            sectionKey,
            "Material",
            "Test/Shader",
            0,
            null);
        var firstSurface = Add(first, "Surface", 3, "manifest:surface");

        var rebuilt = new MaterialSectionPresentation(
            MaterialEditorSectionKeys.Shader(
                gameObject,
                material,
                "Test/Shader"),
            "Material",
            "Test/Shader",
            20,
            null);
        Add(rebuilt, "Earlier after filtering", 22, "manifest:earlier");
        var rebuiltSurface = Add(
            rebuilt,
            "Surface",
            41,
            "manifest:surface");

        Equal(sectionKey, rebuilt.Id, "section key survives rebuild");
        Equal(firstSurface.Id, rebuiltSurface.Id,
            "category key does not depend on visible row index");
        Equal(3, firstSurface.RowIndex, "original visible anchor");
        Equal(41, rebuiltSurface.RowIndex, "rebuilt visible anchor");
        Equal(
            false,
            MaterialEditorSectionKeys.Category(
                gameObject,
                material,
                "Test/Shader",
                "manifest",
                "Surface")
            == MaterialEditorSectionKeys.Category(
                gameObject,
                material,
                "Test/Shader",
                "extension",
                "Surface"),
            "collapse-state keys retain their source identity");
    }

    private static void RepeatedLogicalCategoriesUseTheirLatestVisibleAnchor()
    {
        var section = new MaterialSectionPresentation(
            "section",
            "Material",
            "Test/Shader",
            0,
            null);
        var surface = Add(section, "Surface", 2, "manifest:surface");
        var detail = Add(section, "Detail", 5, "manifest:detail");
        var repeatedSurface = Add(
            section,
            "Surface",
            9,
            "extension:surface");
        var final = Add(section, "Final", 12, "extension:final");

        Same(surface, repeatedSurface,
            "same logical category keeps one stable navigation target");
        Equal(2, surface.RowIndex,
            "navigation keeps the first visible anchor");
        Same(surface, section.FindCategoryAtRow(2), "first category anchor");
        Same(detail, section.FindCategoryAtRow(8),
            "intermediate category before repeated anchor");
        Same(surface, section.FindCategoryAtRow(9),
            "repeated category becomes active at its later anchor");
        Same(final, section.FindCategoryAtRow(12), "last category anchor");
        Same(final, section.FindCategoryAtRow(100),
            "last category remains active after its anchor");
    }

    private static void ParentExpansionReportsWhetherRowsMustBeRebuilt()
    {
        var parentCollapsed = false;
        var calls = 0;
        var section = new MaterialSectionPresentation(
            "section",
            "Material",
            "Shader",
            0,
            () =>
            {
                calls++;
                var changed = parentCollapsed;
                parentCollapsed = false;
                return changed;
            });
        var target = Add(section, "Surface", 2, "surface");

        Equal(false, target.EnsureParentsExpanded(),
            "expanded parent keeps navigation on the no-rebuild path");
        parentCollapsed = true;
        Equal(true, target.EnsureParentsExpanded(),
            "collapsed parent requests exactly one presentation rebuild");
        Equal(false, parentCollapsed,
            "parent expansion mutates only the local presentation state");
        Equal(2, calls, "one parent-state query per navigation action");
    }

    private static void PooledEntryListenersAlwaysUseTheLatestBoundTarget()
    {
        var section = new MaterialSectionPresentation(
            "section", "Material", "Shader", 0, null);
        var first = Add(section, "First", 2, "first");
        var second = Add(section, "Second", 7, "second");
        CategoryNavigationTarget navigated = null;
        CategoryNavigationTarget toggled = null;
        var binding = new CategoryNavigationEntryBinding(
            target => navigated = target,
            target => toggled = target);

        binding.Bind(first);
        binding.InvokeNavigate();
        Same(first, navigated, "first binding navigation target");

        binding.Bind(second);
        binding.InvokeNavigate();
        binding.InvokeToggle();
        Same(second, navigated,
            "rebound permanent navigation listener cannot retain first target");
        Same(second, toggled,
            "rebound permanent collapse listener cannot retain first target");

        binding.Bind(null);
        navigated = null;
        toggled = null;
        binding.InvokeNavigate();
        binding.InvokeToggle();
        Same(null, navigated, "released navigation listener is inert");
        Same(null, toggled, "released collapse listener is inert");
    }

    private static CategoryNavigationTarget Add(
        MaterialSectionPresentation section,
        string name,
        int rowIndex,
        string stateId)
    {
        var collapsed = false;
        return section.AddCategory(
            name,
            rowIndex,
            stateId,
            () => collapsed,
            value => collapsed = value,
            null);
    }

    private static void Same(object expected, object actual, string name)
    {
        if (!ReferenceEquals(expected, actual))
            throw new InvalidOperationException(name + ": references differ.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
