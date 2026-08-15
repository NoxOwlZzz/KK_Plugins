using MaterialEditorAPI;
using System.Xml;

internal static class Program
{
    private static int Main()
    {
        try
        {
            SharedDefaultsAndShaderOverridesMerge();
            PropertyDisplayNamesAreParsedClonedAndMerged();
            ReferencesAndWhitespaceAreResolved();
            InvalidCatalogsAreRejected();
            ShaderHintDisplayPolicyIsIndependentOfStandardTooltips();
            CubemapProjectionTests.Run();
            RadianceHdrDecoderTests.Run();
            CubemapContentKeyTests.Run();
            CubemapBackgroundReadTests.Run();
            CubemapIdentityTests.Run();
            CubemapMemoryBudgetTests.Run();
            CubemapPersistenceContractTests.Run();
            TextureImportCompletionContractTests.Run();
            SchemaVersionsAreBackwardCompatible();
            SchemaTwoPropertyMetadataIsParsed();
            MetadataFallbacksAreSafe();
            ConditionsAndVisibilityPoliciesArePure();
            ManifestArchitectureTests.Run();
            ManifestCanonicalSyntaxTests.Run();
            SubcategoryHierarchyContractTests.Run();
            PhaseOneRegressionTests.Run();
            ManifestPropertyTypeAliasTests.Run();
            UiThemeTokenContractTests.Run();
            UiDarkThemeContractTests.Run();
            UiTopBarPhaseThreeContractTests.Run();
            UiLeftNavigatorPhaseFourContractTests.Run();
            UiRightPanelsPhaseFiveContractTests.Run();
            UiRowActionsPhaseSixContractTests.Run();
            UiCopyPasteVisibilityTests.Run();
            UiPropertyCategoryPhaseSevenContractTests.Run();
            UiPropertyRowsPhaseEightContractTests.Run();
            UiFeedbackPhaseNineContractTests.Run();
            UiMaintenanceRegressionTests.Run();
            UiResponsivePhaseTenContractTests.Run();
            UiControlReadabilityRegressionTests.Run();
            CategoryNavigationPresentationTests.Run();
            RendererCollapsePresentationTests.Run();
            VirtualListScrollContextTests.Run();
            CRC64RegressionTests.Run();
            MaterialPropertyIdCacheTests.Run();
            TextureOwnershipTests.Run();
            KkRuntimeCompatibilityTests.Run();
            SiruSyncPolicyTests.Run();
            OptimizationRegressionTests.Run();
            PerformanceInstrumentationTests.Run();
            HotPathOptimizationTests.Run();
            UiLifetimeOptimizationTests.Run();
            PresentationInvalidationCoordinatorTests.Run();
            ConditionDependencyGraphTests.Run();
            ConditionInvalidationIntegrationTests.Run();
            SearchInvalidationIntegrationTests.Run();
            DestroyedTargetLifetimeTests.Run();
            EnumDropdownPriorityNineTests.Run();
            Console.WriteLine("Material Editor metadata regression tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void SharedDefaultsAndShaderOverridesMerge()
    {
        var shared = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <TooltipSet Id=""common"">
                  <Category Name=""Lighting"">Common lighting</Category>
                  <Property Name=""MainColor"">Common color</Property>
                </TooltipSet>
                <UseTooltipSet Ref=""common"" />
              </MaterialEditorTooltips>");
        var shaderSpecific = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <Shader Name=""xukmi/HairPlus"">
                  <Tooltip>Hair shader</Tooltip>
                  <Property Name=""MainColor"">Hair color</Property>
                </Shader>
              </MaterialEditorTooltips>");

        shared.Merge(shaderSpecific);
        var resolved = shared.ResolveShader("xukmi/HairPlus");

        Equal("Hair shader", resolved.TooltipText, "shader tooltip");
        Equal(
            "Common lighting",
            resolved.CategoryTooltips["Lighting"],
            "shared category tooltip");
        Equal(
            "Hair color",
            resolved.PropertyTooltips["MainColor"],
            "shader property override");
    }

    private static void PropertyDisplayNamesAreParsedClonedAndMerged()
    {
        var shared = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <TooltipSet Id=""common"">
                  <Property Name=""MainColor"" DisplayName=""Shared Main Color"">Common color</Property>
                  <Property Name=""DetailMask"" DisplayName=""Detail Mask"" />
                </TooltipSet>
                <UseTooltipSet Ref=""common"" />
              </MaterialEditorTooltips>");
        var shaderSpecific = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <Shader Name=""Test/DisplayNames"">
                  <Property Name=""MainColor"" DisplayName=""Hair Main Color"">Hair color</Property>
                </Shader>
              </MaterialEditorTooltips>");

        shared.Merge(shaderSpecific);
        var resolved = shared.ResolveShader("Test/DisplayNames");
        Equal(
            "Hair Main Color",
            resolved.PropertyDisplayNames["MainColor"],
            "shader display-name override");
        Equal(
            "Detail Mask",
            resolved.PropertyDisplayNames["DetailMask"],
            "display-name-only shared property");
        Equal(
            "Hair color",
            resolved.PropertyTooltips["MainColor"],
            "display name does not replace tooltip text");

        ShaderUiMetadataRegistry.SetShader("Test/DisplayNames", resolved);
        try
        {
            resolved.PropertyDisplayNames["MainColor"] = "Mutated after registration";
            Equal(
                "Hair Main Color",
                ShaderUiMetadataRegistry.GetPropertyDisplayName(
                    "Test/DisplayNames",
                    "MainColor"),
                "registry clone preserves display name");
            Equal(
                null,
                ShaderUiMetadataRegistry.GetPropertyDisplayName(
                    "Test/DisplayNames",
                    "Unknown"),
                "missing display name");
        }
        finally
        {
            ShaderUiMetadataRegistry.SetShader("Test/DisplayNames", null);
        }
    }

    private static void ReferencesAndWhitespaceAreResolved()
    {
        var warnings = new List<string>();
        var catalog = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <TooltipSet Id=""common"">
                  <Property Name=""RimStrength"">
                    Controls
                    rim strength
                  </Property>
                </TooltipSet>
                <Shader Name=""Test/Shader"">
                  <UseTooltipSet Ref=""common"" />
                  <Property Name=""RimPower"" Ref=""RimStrength"" />
                </Shader>
              </MaterialEditorTooltips>",
            warnings.Add);

        var resolved = catalog.ResolveShader("Test/Shader");
        Equal(
            "Controls\nrim strength",
            resolved.PropertyTooltips["RimPower"],
            "property reference");
        Equal(0, warnings.Count, "warning count");
    }

    private static void InvalidCatalogsAreRejected()
    {
        Throws<FormatException>(
            () => ShaderTooltipCatalogParser.Parse(
                "<WrongRoot SchemaVersion=\"1\" />"),
            "invalid root");
        Throws<FormatException>(
            () => ShaderTooltipCatalogParser.Parse(
                "<MaterialEditorTooltips SchemaVersion=\"2\" />"),
            "invalid schema version");
        Throws<ArgumentException>(
            () => ShaderTooltipCatalogParser.Parse(" "),
            "empty catalog");
    }

    private static void ShaderHintDisplayPolicyIsIndependentOfStandardTooltips()
    {
        Equal(
            TooltipDisplayKind.ShaderHint,
            ResolveTooltip(
                standardTooltipsEnabled: false,
                shaderHintsEnabled: true,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "Shift hint with standard tooltips disabled");
        Equal(
            TooltipDisplayKind.Standard,
            ResolveTooltip(
                standardTooltipsEnabled: true,
                shaderHintsEnabled: true,
                shiftPressed: false,
                hasStandardText: true,
                hasShaderHintText: true),
            "standard tooltip without Shift");
        Equal(
            TooltipDisplayKind.None,
            ResolveTooltip(
                standardTooltipsEnabled: false,
                shaderHintsEnabled: true,
                shiftPressed: false,
                hasStandardText: true,
                hasShaderHintText: true),
            "disabled standard tooltip without Shift");
        Equal(
            TooltipDisplayKind.Standard,
            ResolveTooltip(
                standardTooltipsEnabled: true,
                shaderHintsEnabled: false,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "disabled shader hints fall back to standard tooltip");
        Equal(
            TooltipDisplayKind.None,
            TooltipDisplayPolicy.Resolve(
                hovered: true,
                interactionSuppressed: true,
                standardTooltipsEnabled: true,
                shaderHintsEnabled: true,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "dragging suppresses all tooltips");
        Equal(
            TooltipDisplayKind.None,
            TooltipDisplayPolicy.Resolve(
                hovered: false,
                interactionSuppressed: false,
                standardTooltipsEnabled: true,
                shaderHintsEnabled: true,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "non-hovered target");
    }

    private static TooltipDisplayKind ResolveTooltip(
        bool standardTooltipsEnabled,
        bool shaderHintsEnabled,
        bool shiftPressed,
        bool hasStandardText,
        bool hasShaderHintText)
    {
        return TooltipDisplayPolicy.Resolve(
            hovered: true,
            interactionSuppressed: false,
            standardTooltipsEnabled,
            shaderHintsEnabled,
            shiftPressed,
            hasStandardText,
            hasShaderHintText);
    }

    private static void SchemaVersionsAreBackwardCompatible()
    {
        Equal(
            1,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                ParseElement("<MaterialEditor />")),
            "missing schema defaults to one");
        Equal(
            2,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                ParseElement("<MaterialEditor SchemaVersion=\"2\" />")),
            "schema two accepted");

        var warnings = new List<string>();
        Equal(
            2,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                ParseElement("<MaterialEditor SchemaVersion=\"99\" />"),
                warnings.Add),
            "future schema preserves the latest known subset");
        Equal(1, warnings.Count, "future schema compatibility warning count");

        warnings.Clear();
        Equal(
            1,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                ParseElement("<MaterialEditor SchemaVersion=\"invalid\" />"),
                warnings.Add),
            "malformed schema safely falls back to schema one");
        Equal(1, warnings.Count, "malformed schema warning count");
    }

    private static void SchemaTwoPropertyMetadataIsParsed()
    {
        var metadata = ShaderPropertyMetadataParser.Parse(
            ParseElement(
                @"<Property Name=""Cull""
                            DisplayName=""Cull Mode""
                            Order=""-10""
                            CategoryOrder=""30""
                            Editor=""Enum""
                            Tooltip=""Controls face culling.""
                            Group=""Render State""
                            ShowIf=""_UseRendering == true""
                            OffValue=""-1""
                            OnValue=""2"">
                      <Option Value=""0"" DisplayName=""Off"" />
                      <Option Value=""1"" Label=""Front"" />
                      <Option Value=""2"">Back</Option>
                  </Property>"));

        Equal("Cull Mode", metadata.DisplayName, "display name");
        Equal(-10, metadata.Order.Value, "order");
        Equal(30, metadata.CategoryOrder.Value, "category order");
        Equal("materialeditor.enum", metadata.EditorId, "enum editor id");
        Equal("Controls face culling.", metadata.TooltipText, "inline tooltip");
        Equal("Render State", metadata.Group, "group");
        Equal("UseRendering", metadata.ShowIf.PropertyName, "ShowIf property");
        Equal(MaterialEditorConditionComparison.Equal, metadata.ShowIf.Comparison, "ShowIf comparison");
        Equal(1f, metadata.ShowIf.Value, "ShowIf bool value");
        Equal(3, metadata.EnumOptions.Count, "enum option count");
        Equal(2f, metadata.EnumOptions[2].Value, "enum option value");
        Equal("Back", metadata.EnumOptions[2].DisplayName, "enum inner-text label");
        Equal(-1f, metadata.OffValue, "toggle off value");
        Equal(2f, metadata.OnValue, "toggle on value");

        var vector = ShaderPropertyMetadataParser.Parse(
            ParseElement(
                "<Property Name=\"Direction\" Editor=\"Vector\" VectorComponentCount=\"3\" />"));
        Equal("materialeditor.vector3", vector.EditorId, "generic vector editor id");
        Equal(3, vector.VectorComponentCount.Value, "vector component count");
    }

    private static void MetadataFallbacksAreSafe()
    {
        var defaults = ShaderPropertyMetadataParser.Parse(
            ParseElement("<Property Name=\"Legacy\" />"));
        Equal(null, defaults.EditorId, "legacy type-editor fallback");
        Equal(0, defaults.EnumOptions.Count, "legacy enum options");
        Equal(0f, defaults.OffValue, "default off value");
        Equal(1f, defaults.OnValue, "default on value");

        var warnings = new List<string>();
        var invalid = ShaderPropertyMetadataParser.Parse(
            ParseElement(
                @"<Property Name=""Broken""
                            Editor=""Mystery""
                            Order=""first""
                            CategoryOrder=""last""
                            ShowIf=""Value == nope""
                            VectorComponentCount=""8""
                            OffValue=""NaN""
                            OnValue=""Infinity"">
                      <Option Value=""nope"" />
                  </Property>"),
            warnings.Add);

        Equal(null, invalid.EditorId, "unknown editor fallback");
        Equal(null, invalid.Order, "invalid order fallback");
        Equal(null, invalid.CategoryOrder, "invalid category order fallback");
        Equal(null, invalid.ShowIf, "invalid ShowIf fallback");
        Equal(null, invalid.VectorComponentCount, "invalid vector fallback");
        Equal(0f, invalid.OffValue, "invalid off fallback");
        Equal(1f, invalid.OnValue, "invalid on fallback");
        Equal(0, invalid.EnumOptions.Count, "invalid option ignored");
        Equal(true, warnings.Count >= 7, "invalid metadata warnings");

        warnings.Clear();
        var emptyEnum = ShaderPropertyMetadataParser.Parse(
            ParseElement("<Property Name=\"EmptyEnum\" Editor=\"Enum\" />"),
            warnings.Add);
        Equal(null, emptyEnum.EditorId, "enum without options uses type editor");
        Equal(1, warnings.Count, "empty enum warning count");
    }

    private static void ConditionsAndVisibilityPoliciesArePure()
    {
        MaterialEditorPropertyCondition condition;
        Equal(
            true,
            ShaderPropertyMetadataParser.TryParseCondition(
                "_UseBacklight != 0",
                out condition),
            "condition parse");
        Equal("UseBacklight", condition.PropertyName, "normalized condition property");
        Equal(true, condition.Evaluate(1f), "condition true");
        Equal(false, condition.Evaluate(0f), "condition false");
        Equal(
            true,
            MaterialEditorConditionPolicy.Evaluate(
                condition,
                name => name == "UseBacklight" ? 1f : (float?)null),
            "resolved condition");
        Equal(
            true,
            MaterialEditorConditionPolicy.Evaluate(
                condition,
                name => null,
                fallback: true),
            "missing dependency safe fallback");

        Equal(
            false,
            MaterialEditorPropertyVisibilityPolicy.IsVisible(
                hidden: true,
                visibilityConditionSatisfied: true),
            "Hidden always hidden");
        Equal(
            false,
            MaterialEditorPropertyVisibilityPolicy.IsVisible(
                hidden: false,
                visibilityConditionSatisfied: false),
            "ShowIf false hidden");
        var warnings = new List<string>();
        Equal(
            false,
            ShaderPropertyMetadataParser.TryParseCondition(
                "Value approximately 1",
                out condition,
                warnings.Add),
            "malformed condition rejected");
        Equal(1, warnings.Count, "malformed condition warning");
    }

    private static XmlElement ParseElement(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document.DocumentElement;
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
    }

    private static void Throws<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}.");
    }
}
