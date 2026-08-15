using MaterialEditorAPI;
using System;
using System.Collections.Generic;
using static MaterialEditorAPI.MaterialAPI;

internal static class ManifestArchitectureTests
{
    internal static void Run()
    {
        EditorPolicyOwnsBuiltInTypeMappings();
        EquivalentFallbacksDoNotWarnAndKeepLastDeclarationOrder();
        SemanticFallbackConflictsWarnOnceAndKeepLastWinner();
        FallbackHierarchyIsStrippedWithoutLosingDeclarationOrder();
    }

    private static void EditorPolicyOwnsBuiltInTypeMappings()
    {
        Equal(
            MaterialEditorPropertyEditorIds.Float,
            ShaderPropertyEditorPolicy.GetDefaultEditorId(ShaderPropertyType.Float),
            "Float default editor");
        Equal(
            MaterialEditorPropertyEditorIds.Boolean,
            ShaderPropertyEditorPolicy.GetDefaultEditorId(ShaderPropertyType.Keyword),
            "Keyword default editor");
        Equal(
            MaterialEditorPropertyEditorIds.Vector4,
            ShaderPropertyEditorPolicy.GetDefaultEditorId(ShaderPropertyType.Vector),
            "Vector default editor");

        ShaderPropertyType type;
        Equal(
            true,
            ShaderPropertyEditorPolicy.TryGetBuiltInPropertyType(
                MaterialEditorPropertyEditorIds.Enum,
                out type),
            "Enum editor is built in");
        Equal(ShaderPropertyType.Float, type, "Enum backing type");
        Equal(
            true,
            ShaderPropertyEditorPolicy.IsCompatible(
                MaterialEditorPropertyEditorIds.Vector3,
                ShaderPropertyType.Color),
            "legacy Color vector compatibility");
        Equal(
            false,
            ShaderPropertyEditorPolicy.IsCompatible(
                MaterialEditorPropertyEditorIds.Boolean,
                ShaderPropertyType.Float),
            "Keyword editor rejected for Float storage");

        string editorId;
        int? components;
        Equal(
            true,
            ShaderPropertyEditorPolicy.TryNormalizeManifestEditor(
                "Vector",
                3,
                out editorId,
                out components),
            "generic Vector editor normalization");
        Equal(MaterialEditorPropertyEditorIds.Vector3, editorId, "Vector3 normalized ID");
        Equal((int?)3, components, "Vector3 normalized component count");
        Equal(
            true,
            ShaderPropertyEditorPolicy.TryNormalizeManifestEditor(
                "Boolean",
                null,
                out editorId,
                out components),
            "Boolean editor normalization");
        Equal(MaterialEditorPropertyEditorIds.Boolean, editorId, "Boolean normalized ID");
    }

    private static void EquivalentFallbacksDoNotWarnAndKeepLastDeclarationOrder()
    {
        var state = new ShaderPropertyFallbackMergeState();
        var catalog = new Dictionary<string, MaterialEditorPluginBase.ShaderPropertyData>(
            StringComparer.Ordinal);
        var warnings = new List<string>();
        var first = CreateFloat("Shared", 0f, 1f, 2);
        var second = CreateFloat("Shared", 0f, 1f, 17);

        state.MergeInto(catalog, first, "mod.first", warnings.Add);
        var winner = state.MergeInto(catalog, second, "mod.second", warnings.Add);

        Equal(0, warnings.Count, "declaration order alone is not a semantic conflict");
        Equal(17, winner.DeclarationOrder, "returned fallback declaration order");
        Equal(17, catalog["Shared"].DeclarationOrder, "catalog winner declaration order");
        Equal(
            true,
            ReferenceEquals(winner, catalog["Shared"]),
            "last fallback remains the catalog winner");
    }

    private static void SemanticFallbackConflictsWarnOnceAndKeepLastWinner()
    {
        var state = new ShaderPropertyFallbackMergeState();
        var catalog = new Dictionary<string, MaterialEditorPluginBase.ShaderPropertyData>(
            StringComparer.Ordinal);
        var warnings = new List<string>();
        var first = CreateFloat("Shared", 0f, 1f, 3);
        var second = CreateFloat("Shared", 0f, 2f, 9);
        second.Hidden = true;
        second.EditorId = MaterialEditorPropertyEditorIds.Enum;
        second.EnumOptions.Add(new MaterialEditorEnumOption(0f, "Off"));
        second.EnumOptions.Add(new MaterialEditorEnumOption(2f, "On"));

        state.MergeInto(catalog, first, "mod.first", warnings.Add);
        state.MergeInto(catalog, second, "mod.second", warnings.Add);

        Equal(1, warnings.Count, "first semantic conflict warning count");
        Contains(warnings[0], "mod.first", "previous fallback source");
        Contains(warnings[0], "mod.second", "incoming fallback source");
        Contains(warnings[0], "MaxValue", "range conflict detail");
        Contains(warnings[0], "Hidden", "visibility conflict detail");
        Contains(warnings[0], "EditorId", "editor conflict detail");
        Equal(2f, catalog["Shared"].MaxValue, "last range wins");
        Equal(9, catalog["Shared"].DeclarationOrder, "conflicting winner declaration order");

        var third = new MaterialEditorPluginBase.ShaderPropertyData(
            "Shared",
            ShaderPropertyType.Color)
        {
            DeclarationOrder = 21
        };
        state.MergeInto(catalog, third, "mod.third", warnings.Add);

        Equal(1, warnings.Count, "conflict is reported once per property");
        Equal(ShaderPropertyType.Color, catalog["Shared"].Type, "later conflict still wins");
        Equal(21, catalog["Shared"].DeclarationOrder, "latest declaration order survives");
    }

    private static void FallbackHierarchyIsStrippedWithoutLosingDeclarationOrder()
    {
        var state = new ShaderPropertyFallbackMergeState();
        var catalog = new Dictionary<string, MaterialEditorPluginBase.ShaderPropertyData>(
            StringComparer.Ordinal);
        var source = CreateFloat("Nested", 0f, 1f, 42);
        source.Category = "Surface";
        source.CategoryBeforeHierarchy = "legacy";
        source.CategoryId = "surface";
        source.CategoryDisplayName = "Surface";
        source.SubcategoryId = "details";
        source.SubcategoryDisplayName = "Details";

        var fallback = state.MergeInto(catalog, source, "mod.nested");

        Equal(42, fallback.DeclarationOrder, "hierarchy fallback declaration order");
        Equal(null, fallback.CategoryId, "fallback Category Id");
        Equal(null, fallback.SubcategoryId, "fallback Subcategory Id");
        Equal("legacy", fallback.Category, "legacy flat category restored");
        Equal("surface", source.CategoryId, "shader-specific hierarchy is unchanged");
    }

    private static MaterialEditorPluginBase.ShaderPropertyData CreateFloat(
        string name,
        float minimum,
        float maximum,
        int declarationOrder)
    {
        return new MaterialEditorPluginBase.ShaderPropertyData(
            name,
            ShaderPropertyType.Float)
        {
            MinValue = minimum,
            MaxValue = maximum,
            DeclarationOrder = declarationOrder
        };
    }

    private static void Contains(string value, string expected, string name)
    {
        if (value == null || !value.Contains(expected, StringComparison.Ordinal))
            throw new InvalidOperationException(name + ": missing '" + expected + "'.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + $": expected '{expected}', actual '{actual}'.");
        }
    }
}
