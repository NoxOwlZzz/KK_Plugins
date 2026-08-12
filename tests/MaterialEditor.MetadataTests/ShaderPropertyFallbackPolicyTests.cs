using MaterialEditorAPI;

internal static class ShaderPropertyFallbackPolicyTests
{
    internal static void Run()
    {
        var condition = new MaterialEditorPropertyCondition(
            "Enabled",
            MaterialEditorConditionComparison.NotEqual,
            0f);
        var shaderSpecific = new MaterialEditorPluginBase.ShaderPropertyData(
            "Mode",
            7)
        {
            DefaultValue = "2",
            DefaultValueAssetBundle = "bundle",
            AnisoLevel = 16,
            FilterMode = "Trilinear",
            WrapMode = "Clamp",
            MinValue = -2.5f,
            MaxValue = 8.5f,
            Hidden = true,
            Category = "Surface",
            DeclarationOrder = 4,
            DisplayName = "Rendering mode",
            EditorId = ShaderPropertyEditorIds.Enum,
            UiLevel = MaterialEditorPropertyUiLevel.Advanced,
            ShowIf = condition,
            Invert = true
        };
        shaderSpecific.EnumOptions.Add(
            new MaterialEditorEnumOption(2f, "Opaque"));

        var existingFallback =
            new MaterialEditorPluginBase.ShaderPropertyData("Existing", 3);
        var fallbackProperties =
            new Dictionary<string, MaterialEditorPluginBase.ShaderPropertyData>
            {
                [existingFallback.Name] = existingFallback
            };
        var fallback = ShaderPropertyFallbackPolicy.MergeInto(
            fallbackProperties,
            shaderSpecific);

        False(ReferenceEquals(shaderSpecific, fallback), "fallback is isolated");
        Equal(2, fallbackProperties.Count, "merge preserves existing fallback entries");
        Equal(
            existingFallback,
            fallbackProperties[existingFallback.Name],
            "merge does not replace unrelated fallback entries");
        Equal(
            fallback,
            fallbackProperties[shaderSpecific.Name],
            "merge stores the neutral fallback clone");
        Equal(shaderSpecific.Name, fallback.Name, "legacy Name");
        Equal(shaderSpecific.Type, fallback.Type, "legacy Type");
        Equal(shaderSpecific.DefaultValue, fallback.DefaultValue, "legacy DefaultValue");
        Equal(
            shaderSpecific.DefaultValueAssetBundle,
            fallback.DefaultValueAssetBundle,
            "legacy DefaultValueAssetBundle");
        Equal(shaderSpecific.AnisoLevel, fallback.AnisoLevel, "legacy AnisoLevel");
        Equal(shaderSpecific.FilterMode, fallback.FilterMode, "legacy FilterMode");
        Equal(shaderSpecific.WrapMode, fallback.WrapMode, "legacy WrapMode");
        Equal(shaderSpecific.MinValue, fallback.MinValue, "legacy MinValue");
        Equal(shaderSpecific.MaxValue, fallback.MaxValue, "legacy MaxValue");
        Equal(shaderSpecific.Hidden, fallback.Hidden, "legacy Hidden");
        Equal(shaderSpecific.Category, fallback.Category, "legacy Category");

        Equal(fallback.Name, fallback.DisplayName, "neutral DisplayName");
        Equal(0, fallback.DeclarationOrder, "neutral DeclarationOrder");
        Equal(
            MaterialEditorPropertyUiLevel.Basic,
            fallback.UiLevel,
            "neutral UiLevel");
        Equal(null, fallback.EditorId, "neutral EditorId");
        Equal(null, fallback.ShowIf, "neutral ShowIf");
        Equal(0, fallback.EnumOptions.Count, "neutral EnumOptions");
        False(
            ReferenceEquals(shaderSpecific.EnumOptions, fallback.EnumOptions),
            "fallback EnumOptions are isolated");
        Equal(false, fallback.Invert, "neutral Invert");

        Equal(
            "Rendering mode",
            shaderSpecific.DisplayName,
            "shader metadata remains intact");
        Equal(
            ShaderPropertyEditorIds.Enum,
            shaderSpecific.EditorId,
            "shader Editor remains intact");
        Equal(condition, shaderSpecific.ShowIf, "shader ShowIf remains intact");
        Equal(1, shaderSpecific.EnumOptions.Count, "shader options remain intact");
        Equal(true, shaderSpecific.Invert, "shader Invert remains intact");

        True(
            ShaderPropertyFallbackPolicy.IsReservedShaderName("default"),
            "exact fallback key is reserved");
        False(
            ShaderPropertyFallbackPolicy.IsReservedShaderName("Default"),
            "ordinary differently-cased shader name remains unaffected");
    }

    private static void True(bool value, string name)
    {
        Equal(true, value, name);
    }

    private static void False(bool value, string name)
    {
        Equal(false, value, name);
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
        }
    }
}

namespace MaterialEditorAPI
{
    public class MaterialEditorPluginBase
    {
        public class ShaderPropertyData
        {
            public ShaderPropertyData(string name, int type)
            {
                Name = name;
                Type = type;
                DisplayName = name;
                UiLevel = MaterialEditorPropertyUiLevel.Basic;
                EnumOptions = new List<MaterialEditorEnumOption>();
            }

            public string Name;
            public int Type;
            public string DefaultValue;
            public string DefaultValueAssetBundle;
            public int? AnisoLevel;
            public string FilterMode;
            public string WrapMode;
            public float? MinValue;
            public float? MaxValue;
            public bool Hidden;
            public string Category;
            internal int DeclarationOrder;
            internal string DisplayName;
            internal string EditorId;
            internal MaterialEditorPropertyUiLevel UiLevel;
            internal MaterialEditorPropertyCondition ShowIf;
            internal List<MaterialEditorEnumOption> EnumOptions;
            internal bool Invert;
        }
    }
}
