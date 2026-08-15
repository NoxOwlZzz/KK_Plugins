using BepInEx.Configuration;
using MaterialEditorAPI;
using System.Xml;
using static MaterialEditorAPI.MaterialEditorPluginBase;

internal static class SubcategoryHierarchyContractTests
{
    internal static void Run()
    {
        FutureSchemasPreserveTheLatestKnownContract();
        LegacyManifestSemanticsDoNotRequireHierarchy();
        HierarchyFeaturesAreIndependentlyOptIn();
        CategorySortingDisabledFlattensOptionalHierarchy();
        OrganizerGenerationAdvancesOncePerRefresh();
        ExactShaderCatalogsRemainIsolatedAcrossABARoundTrip();
        UnknownAndExactCatalogsRemainIsolatedAcrossSwitches();
        UnknownShaderFallbackIsFlatAndKeywordFree();
        RepeatedRefreshDoesNotAccumulateRemovedShaders();
        ViableCategoryCountControlsUncategorizedPresentation();
        NestedHierarchyPreservesPropertySemantics();
        NestedHierarchyRequiresSchemaTwo();
        DefaultFallbackDropsShaderSpecificHierarchy();
        SubcategoryIdsAreLocalToTheirCategory();
        ConflictingHierarchyDeclarationsWarnAndUseTheFirstDeclaration();
        LegacyFlatCategoriesRemainCompatible();
        SearchFlattensCollapseAndDiagnosesFalseShowIf();
        HierarchyHeadersRemainCollapsible();
        HierarchyHasExactlyTwoVisuallyDistinctLevels();
        SubcategoryControlsRemainPooled();
        DocumentationUsesThePresentationOnlyContract();
        Console.WriteLine("Subcategory presentation-only contract guards passed.");
    }

    private static void FutureSchemasPreserveTheLatestKnownContract()
    {
        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""99"">
                <Shader Name=""Future/KnownSubset"">
                  <Category Id=""surface"" DisplayName=""Surface"">
                    <Subcategory Id=""details"" DisplayName=""Details"">
                      <Property Name=""Enabled"" Type=""Boolean"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");
        var warnings = new List<string>();
        var effectiveVersion = ShaderPropertyMetadataParser.ReadSchemaVersion(
            document.DocumentElement,
            warnings.Add);

        Equal(2, effectiveVersion, "future schema uses the latest known contract");
        Equal(1, warnings.Count, "future schema warning count");

        var property = ParseProperty(FirstProperty(document), effectiveVersion);
        Equal(MaterialAPI.ShaderPropertyType.Float, property.Type, "future schema known Boolean storage");
        Equal(MaterialEditorPropertyEditorIds.Toggle, property.EditorId, "future schema known Boolean editor");
        Equal("surface", property.CategoryId, "future schema known Category");
        Equal("details", property.SubcategoryId, "future schema known Subcategory");
    }

    private static void LegacyManifestSemanticsDoNotRequireHierarchy()
    {
        var element = ParseElement(
            @"<Property Name=""LegacyValue"" Type=""Float""
                        DefaultValue=""0.25"" Range=""-2,3""
                        Hidden=""false"" Category=""rendering"" />");
        var schemaOne = ParseProperty(element, schemaVersion: 1);
        var schemaTwo = ParseProperty(element, schemaVersion: 2);

        AssertEquivalentPropertySemantics(schemaOne, schemaTwo, "legacy parity");
        Equal(null, schemaOne.CategoryId, "schema-1 hierarchy is absent");
        Equal(null, schemaTwo.CategoryId, "schema-2 hierarchy remains opt-in");
        Equal(null, schemaTwo.SubcategoryId, "schema-2 Subcategory remains opt-in");
    }

    private static void HierarchyFeaturesAreIndependentlyOptIn()
    {
        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/OptionalMatrix"">
                  <Property Name=""Flat"" Type=""Float"" Category=""Legacy"" />
                  <Category Id=""category-only"" DisplayName=""Category Only"">
                    <Property Name=""CategoryOnly"" Type=""Float"" />
                  </Category>
                  <Category Id=""subcategory-only"" DisplayName=""Subcategory Only"">
                    <Subcategory Id=""visual-group"" DisplayName=""Visual Group"">
                      <Property Name=""SubcategoryOnly"" Type=""Float""
                                ShowIf=""Feature != 0"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");
        var properties = document.GetElementsByTagName("Property")
            .Cast<XmlElement>()
            .Select(element => ParseProperty(element))
            .ToDictionary(property => property.Name, StringComparer.Ordinal);

        Equal(null, properties["Flat"].CategoryId, "flat property has no explicit hierarchy");
        Equal("category-only", properties["CategoryOnly"].CategoryId, "Category can opt in alone");
        Equal(null, properties["CategoryOnly"].SubcategoryId, "Category does not imply Subcategory");
        Equal("visual-group", properties["SubcategoryOnly"].SubcategoryId, "Subcategory can opt in alone");
        Equal("Feature", properties["SubcategoryOnly"].ShowIf.PropertyName, "ShowIf remains independently explicit");
    }

    private static void CategorySortingDisabledFlattensOptionalHierarchy()
    {
        ConfigureOrganizer();
        MaterialEditorPluginBase.SortPropertiesByCategory.Value = false;
        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/HierarchyDisabled"">
                  <Category Id=""surface"" DisplayName=""Surface"">
                    <Property Name=""Enabled"" Type=""Boolean"" />
                    <Subcategory Id=""details"" DisplayName=""Details"">
                      <Property Name=""Detail"" Type=""Float"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");
        var declared = document.GetElementsByTagName("Property")
            .Cast<XmlElement>()
            .Select((element, declarationOrder) =>
            {
                var property = ParseProperty(element);
                property.DeclarationOrder = declarationOrder;
                return property;
            })
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
        XMLShaderProperties.Clear();
        XMLShaderProperties["Test/HierarchyDisabled"] = declared;
        var warnings = new List<string>();

        try
        {
            PropertyOrganizer.Refresh(warnings.Add);
            var category = PropertyOrganizer
                .PropertyOrganization["Test/HierarchyDisabled"]
                .Single();
            Equal(PropertyOrganizer.UncategorizedName, category.Id, "disabled category sorting Id");
            Equal(PropertyOrganizer.UncategorizedName, category.Name, "disabled category sorting label");
            Equal(0, category.Subcategories.Count, "disabled category sorting removes Subcategory presentation");
            Equal(2, category.Properties.Count, "disabled category sorting preserves every property");
            Equal(2, category.DirectProperties.Count, "disabled category sorting renders every property directly");
            Equal("Enabled", category.DirectProperties[0].Name, "disabled category sorting keeps first property");
            Equal("Detail", category.DirectProperties[1].Name, "disabled category sorting keeps nested property");
            Equal(0, warnings.Count, "disabled category sorting does not report hierarchy conflicts");
        }
        finally
        {
            MaterialEditorPluginBase.SortPropertiesByCategory.Value = true;
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void OrganizerGenerationAdvancesOncePerRefresh()
    {
        ConfigureOrganizer();
        XMLShaderProperties.Clear();
        var before = PropertyOrganizer.Generation;
        try
        {
            PropertyOrganizer.Refresh();
            Equal(before + 1, PropertyOrganizer.Generation, "one generation per Refresh");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void ExactShaderCatalogsRemainIsolatedAcrossABARoundTrip()
    {
        ConfigureOrganizer();
        XMLShaderProperties.Clear();
        var shaderA = ParseProperties(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/A"">
                  <Category Id=""alpha"" DisplayName=""Alpha"">
                    <Property Name=""AValue"" Type=""Float"" />
                    <Property Name=""AKeyword"" Type=""Keyword"" />
                  </Category>
                </Shader>
              </MaterialEditor>");
        var shaderB = ParseProperties(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/B"">
                  <Category Id=""beta"" DisplayName=""Beta"">
                    <Property Name=""BValue"" Type=""Float"" />
                    <Property Name=""BKeyword"" Type=""Keyword"" />
                  </Category>
                </Shader>
              </MaterialEditor>");
        XMLShaderProperties["Test/A"] = shaderA;
        XMLShaderProperties["Test/B"] = shaderB;

        try
        {
            PropertyOrganizer.Refresh();
            var firstA = SnapshotOrganization("Test/A");
            var betweenB = SnapshotOrganization("Test/B");
            var secondA = SnapshotOrganization("Test/A");

            Equal(firstA, secondA, "A/B/A returns to the exact A catalog");
            Equal("alpha:AValue/Float,AKeyword/Keyword", firstA, "A catalog contents");
            Equal("beta:BValue/Float,BKeyword/Keyword", betweenB, "B catalog contents");
            Equal(false, firstA.Contains("B", StringComparison.Ordinal), "A excludes B properties");
            Equal(false, betweenB.Contains("A", StringComparison.Ordinal), "B excludes A properties");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void UnknownShaderFallbackIsFlatAndKeywordFree()
    {
        ConfigureOrganizer();
        XMLShaderProperties.Clear();
        var fallback = ParseProperties(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/FallbackSource"">
                  <Category Id=""surface"" DisplayName=""Surface"">
                    <Subcategory Id=""details"" DisplayName=""Details"">
                      <Property Name=""SurfaceValue"" Type=""Float""
                                ShowIf=""SurfaceKeyword != 0"" />
                      <Property Name=""SurfaceKeyword"" Type=""Keyword"" />
                    </Subcategory>
                  </Category>
                  <Property Name=""LegacyValue"" Type=""Float""
                            Category=""legacy"" />
                </Shader>
              </MaterialEditor>");
        XMLShaderProperties[PropertyOrganizer.DefaultShaderKey] = fallback;

        try
        {
            PropertyOrganizer.Refresh();
            var categories = PropertyOrganizer.PropertyOrganization[
                PropertyOrganizer.DefaultShaderKey];
            var category = categories.Single();

            Equal(1, categories.Count, "fallback category count");
            Equal(PropertyOrganizer.UncategorizedName, category.Id, "fallback category Id");
            Equal(PropertyOrganizer.UncategorizedName, category.Name, "fallback category label");
            Equal(0, category.Subcategories.Count, "fallback has no Subcategories");
            Equal(2, category.DirectProperties.Count, "fallback direct property count");
            Equal(2, category.Properties.Count, "fallback flat property count");
            Equal(
                false,
                category.Properties.Any(property =>
                    property.Type == MaterialAPI.ShaderPropertyType.Keyword),
                "fallback UI excludes Keyword rows");
            Equal(null,
                category.Properties.Single(property =>
                    property.Name == "SurfaceValue").ShowIf,
                "fallback UI drops foreign visibility conditions");
            Equal(
                "SurfaceValue,LegacyValue",
                string.Join(",", category.Properties.Select(property => property.Name)),
                "fallback preserves non-Keyword declaration order");
            Equal(3, fallback.Count, "legacy fallback dictionary is not mutated");
            Equal(
                true,
                fallback.ContainsKey("SurfaceKeyword"),
                "legacy fallback dictionary retains Keyword metadata");
            Equal(
                "surface",
                fallback["SurfaceValue"].CategoryId,
                "legacy fallback dictionary retains source hierarchy");
            Equal(true,
                fallback["SurfaceValue"].ShowIf != null,
                "legacy fallback dictionary retains visibility metadata");
            Equal(
                false,
                PropertyOrganizer.ShouldUseNamedCategory(
                    PropertyOrganizer.DefaultShaderKey,
                    true,
                    "Surface",
                    2),
                "fallback never creates a category navigation header");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void UnknownAndExactCatalogsRemainIsolatedAcrossSwitches()
    {
        ConfigureOrganizer();
        XMLShaderProperties.Clear();
        XMLShaderProperties[PropertyOrganizer.DefaultShaderKey] =
            ParseProperties(
                @"<MaterialEditor SchemaVersion=""2"">
                    <Shader Name=""Foreign/Source"">
                      <Category Id=""foreign"" DisplayName=""Foreign"">
                        <Property Name=""SharedFloat"" Type=""Float"" />
                        <Property Name=""ForeignKeyword"" Type=""Keyword"" />
                      </Category>
                    </Shader>
                  </MaterialEditor>");
        XMLShaderProperties["Test/Categorized"] = ParseProperties(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/Categorized"">
                  <Category Id=""owned"" DisplayName=""Owned"">
                    <Property Name=""OwnedFloat"" Type=""Float"" />
                    <Property Name=""OwnedKeyword"" Type=""Keyword"" />
                  </Category>
                </Shader>
              </MaterialEditor>");

        try
        {
            PropertyOrganizer.Refresh();
            var firstUnknown = SnapshotResolvedOrganization("Test/UnknownA");
            var firstExact = SnapshotResolvedOrganization("Test/Categorized");
            var secondUnknown = SnapshotResolvedOrganization("Test/UnknownA");
            var otherUnknown = SnapshotResolvedOrganization("Test/UnknownB");
            var secondExact = SnapshotResolvedOrganization("Test/Categorized");

            Equal("Uncategorized:SharedFloat/Float", firstUnknown,
                "unknown shader receives only conservative flat fallback");
            Equal(firstUnknown, secondUnknown,
                "unknown/exact/unknown does not retain exact metadata");
            Equal(firstUnknown, otherUnknown,
                "different unknown shaders do not retain prior exact metadata");
            Equal("owned:OwnedFloat/Float,OwnedKeyword/Keyword", firstExact,
                "exact shader keeps its own category and Keyword");
            Equal(firstExact, secondExact,
                "exact catalog survives unknown switches unchanged");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void RepeatedRefreshDoesNotAccumulateRemovedShaders()
    {
        ConfigureOrganizer();
        XMLShaderProperties.Clear();
        XMLShaderProperties["Test/A"] = ParseProperties(
            @"<MaterialEditor><Shader Name=""Test/A"">
                <Property Name=""OldA"" Type=""Float"" />
              </Shader></MaterialEditor>");
        XMLShaderProperties["Test/B"] = ParseProperties(
            @"<MaterialEditor><Shader Name=""Test/B"">
                <Property Name=""OldB"" Type=""Float"" />
              </Shader></MaterialEditor>");

        try
        {
            PropertyOrganizer.Refresh();
            var firstGeneration = PropertyOrganizer.Generation;
            Equal(true, PropertyOrganizer.PropertyOrganization.ContainsKey("Test/B"), "first refresh contains B");

            XMLShaderProperties.Clear();
            XMLShaderProperties["Test/A"] = ParseProperties(
                @"<MaterialEditor><Shader Name=""Test/A"">
                    <Property Name=""NewA"" Type=""Float"" />
                  </Shader></MaterialEditor>");
            PropertyOrganizer.Refresh();

            Equal(firstGeneration + 1, PropertyOrganizer.Generation, "second refresh generation");
            Equal(false, PropertyOrganizer.PropertyOrganization.ContainsKey("Test/B"), "removed B is not retained");
            Equal("Uncategorized:NewA/Float", SnapshotOrganization("Test/A"), "A is replaced instead of accumulated");

            PropertyOrganizer.Refresh();
            Equal("Uncategorized:NewA/Float", SnapshotOrganization("Test/A"), "third refresh remains idempotent");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void ViableCategoryCountControlsUncategorizedPresentation()
    {
        ConfigureOrganizer();
        Equal(
            false,
            PropertyOrganizer.ShouldUseNamedCategory(
                "Test/Exact",
                false,
                PropertyOrganizer.UncategorizedName,
                1),
            "an incompatible or invisible named group does not force Uncategorized");
        Equal(
            true,
            PropertyOrganizer.ShouldUseNamedCategory(
                "Test/Exact",
                false,
                PropertyOrganizer.UncategorizedName,
                2),
            "a genuinely mixed catalog names Uncategorized");
        Equal(
            true,
            PropertyOrganizer.ShouldUseNamedCategory(
                "Test/Exact",
                false,
                "Lighting",
                1),
            "one viable legacy named category remains named");
        Equal(
            true,
            PropertyOrganizer.ShouldUseNamedCategory(
                "Test/Exact",
                true,
                PropertyOrganizer.UncategorizedName,
                1),
            "an explicit Category remains named even with the fallback label");

        var presenter = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialSectionPresenter.cs");
        Contains(
            presenter,
            "viableCategories.Count",
            "presenter uses post-filter viable category count");
        DoesNotContain(
            presenter,
            "organizedCategories.Count > 1",
            "presenter no longer uses the raw organizer category count");
    }

    private static void NestedHierarchyPreservesPropertySemantics()
    {
        var flatDocument = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/Flat"">
                  <Property Name=""BlendMode"" Type=""Enum""
                            Enums=""Off,0,On,2"" DefaultValue=""2""
                            DisplayName=""Blend Mode"" Category=""Main Color 2nd""
                            ShowIf=""UseMain2ndTex != 0"" />
                </Shader>
              </MaterialEditor>");
        var nestedDocument = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/Nested"">
                  <Category Id=""main-2nd"" DisplayName=""Main Color 2nd"">
                    <Subcategory Id=""blending"" DisplayName=""Blending"">
                      <Property Name=""BlendMode"" Type=""Enum""
                                Enums=""Off,0,On,2"" DefaultValue=""2""
                                DisplayName=""Blend Mode"" Category=""Main Color 2nd""
                                ShowIf=""UseMain2ndTex != 0"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");

        var flat = ParseProperty(FirstProperty(flatDocument));
        var nested = ParseProperty(FirstProperty(nestedDocument));

        Equal(flat.Name, nested.Name, "property persistence key");
        Equal(flat.Type, nested.Type, "backing type");
        Equal(flat.EditorId, nested.EditorId, "semantic editor");
        Equal(flat.DefaultValue, nested.DefaultValue, "default value");
        Equal(flat.DisplayName, nested.DisplayName, "display name");
        Equal(flat.Category, nested.Category, "legacy category view");
        Equal(flat.Hidden, nested.Hidden, "Hidden value");
        Equal(flat.EnumOptions.Count, nested.EnumOptions.Count, "enum option count");
        for (var index = 0; index < flat.EnumOptions.Count; index++)
        {
            Equal(flat.EnumOptions[index].Value, nested.EnumOptions[index].Value, "enum value " + index);
            Equal(flat.EnumOptions[index].DisplayName, nested.EnumOptions[index].DisplayName, "enum label " + index);
        }
        EqualCondition(flat.ShowIf, nested.ShowIf, "ShowIf");

        Equal("main-2nd", nested.CategoryId, "Category Id");
        Equal("Main Color 2nd", nested.CategoryDisplayName, "Category label");
        Equal("blending", nested.SubcategoryId, "Subcategory Id");
        Equal("Blending", nested.SubcategoryDisplayName, "Subcategory label");
    }

    private static void NestedHierarchyRequiresSchemaTwo()
    {
        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""1"">
                <Shader Name=""Legacy/Nested"">
                  <Category Id=""category"" DisplayName=""Category"">
                    <Subcategory Id=""subcategory"" DisplayName=""Subcategory"">
                      <Property Name=""LegacyValue"" Type=""Float""
                                Category=""Legacy"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");
        var property = ParseProperty(FirstProperty(document), schemaVersion: 1);

        Equal(null, property.CategoryId, "schema-1 Category Id");
        Equal(null, property.SubcategoryId, "schema-1 Subcategory Id");
        Equal("Legacy", property.Category, "schema-1 flat Category");
    }

    private static void SubcategoryIdsAreLocalToTheirCategory()
    {
        ConfigureOrganizer();
        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/LocalIds"">
                  <Category Id=""surface"" DisplayName=""Surface"">
                    <Subcategory Id=""options"" DisplayName=""Surface Options"">
                      <Property Name=""SurfaceValue"" Type=""Float"" />
                    </Subcategory>
                  </Category>
                  <Category Id=""lighting"" DisplayName=""Lighting"">
                    <Subcategory Id=""options"" DisplayName=""Lighting Options"">
                      <Property Name=""LightingValue"" Type=""Float"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");
        var properties = document.GetElementsByTagName("Property")
            .Cast<XmlElement>()
            .Select((element, index) =>
            {
                var property = ParseProperty(element);
                property.DeclarationOrder = index;
                return property;
            })
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
        XMLShaderProperties.Clear();
        XMLShaderProperties["Test/LocalIds"] = properties;
        var warnings = new List<string>();

        try
        {
            PropertyOrganizer.Refresh(warnings.Add);
            var categories = PropertyOrganizer.PropertyOrganization["Test/LocalIds"];
            Equal(2, categories.Count, "Category count");
            Equal("surface", categories[0].Id, "first Category Id");
            Equal("options", categories[0].Subcategories.Single().Id, "first local Subcategory Id");
            Equal("Surface Options", categories[0].Subcategories.Single().Name, "first Subcategory label");
            Equal("lighting", categories[1].Id, "second Category Id");
            Equal("options", categories[1].Subcategories.Single().Id, "second local Subcategory Id");
            Equal("Lighting Options", categories[1].Subcategories.Single().Name, "second Subcategory label");
            Equal(0, warnings.Count, "local Subcategory IDs do not conflict across Categories");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void ConflictingHierarchyDeclarationsWarnAndUseTheFirstDeclaration()
    {
        ConfigureOrganizer();
        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/Conflicts"">
                  <Category Id=""surface"" DisplayName=""Surface First"">
                    <Subcategory Id=""details"" DisplayName=""Details First"">
                      <Property Name=""FirstValue"" Type=""Float""
                                CategoryOrder=""10"" />
                    </Subcategory>
                  </Category>
                  <Category Id=""surface"" DisplayName=""Surface Later"">
                    <Subcategory Id=""details"" DisplayName=""Details Later"">
                      <Property Name=""LaterValue"" Type=""Float""
                                CategoryOrder=""20"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");
        var declared = document.GetElementsByTagName("Property")
            .Cast<XmlElement>()
            .Select((element, declarationOrder) =>
            {
                var property = ParseProperty(element);
                property.DeclarationOrder = declarationOrder;
                return property;
            })
            .ToArray();

        // Reverse insertion deliberately: manifest DeclarationOrder, not the
        // dictionary's enumeration order, owns conflicting hierarchy metadata.
        XMLShaderProperties.Clear();
        XMLShaderProperties["Test/Conflicts"] =
            new Dictionary<string, ShaderPropertyData>(StringComparer.Ordinal)
            {
                [declared[1].Name] = declared[1],
                [declared[0].Name] = declared[0]
            };
        var warnings = new List<string>();

        try
        {
            PropertyOrganizer.Refresh(warnings.Add);
            var category = PropertyOrganizer.PropertyOrganization["Test/Conflicts"].Single();
            var subcategory = category.Subcategories.Single();

            Equal("Surface First", category.Name, "first Category label wins");
            Equal("Details First", subcategory.Name, "first Subcategory label wins");
            Equal(2, subcategory.Properties.Count, "conflicting metadata never drops properties");
            Equal("FirstValue", subcategory.Properties[0].Name, "property declaration order remains stable");
            Equal("LaterValue", subcategory.Properties[1].Name, "later property remains accessible");
            Equal(3, warnings.Count, "each conflicting hierarchy field warns once");
            Equal(
                true,
                warnings.Any(message =>
                    message.Contains("conflicting DisplayName", StringComparison.Ordinal)
                    && message.Contains("Category Id 'surface'", StringComparison.Ordinal)
                    && !message.Contains("Subcategory", StringComparison.Ordinal)),
                "Category label conflict warning");
            Equal(
                true,
                warnings.Any(message =>
                    message.Contains("conflicting DisplayName", StringComparison.Ordinal)
                    && message.Contains("Subcategory Id 'details'", StringComparison.Ordinal)),
                "Subcategory label conflict warning");
            Equal(
                true,
                warnings.Any(message =>
                    message.Contains("conflicting Order", StringComparison.Ordinal)
                    && message.Contains("Category Id 'surface'", StringComparison.Ordinal)),
                "Category order conflict warning");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void DefaultFallbackDropsShaderSpecificHierarchy()
    {
        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Test/Fallback"">
                  <Category Id=""surface"" DisplayName=""Surface"">
                    <Subcategory Id=""options"" DisplayName=""Options"">
                      <Property Name=""SurfaceValue"" Type=""Float""
                                ShowIf=""Feature != 0"" />
                    </Subcategory>
                  </Category>
                </Shader>
              </MaterialEditor>");
        var nested = ParseProperty(FirstProperty(document));
        var fallback = nested.WithoutHierarchyForDefaultFallback();

        Equal("surface", nested.CategoryId, "source Category Id remains intact");
        Equal("options", nested.SubcategoryId, "source Subcategory Id remains intact");
        Equal(null, fallback.CategoryId, "default fallback Category Id");
        Equal(null, fallback.CategoryDisplayName, "default fallback Category label");
        Equal(null, fallback.SubcategoryId, "default fallback Subcategory Id");
        Equal(null, fallback.SubcategoryDisplayName, "default fallback Subcategory label");
        Equal(string.Empty, fallback.Category, "default fallback does not inherit the nested display category");
        Equal(nested.Name, fallback.Name, "fallback persistence key");
        Equal(nested.Type, fallback.Type, "fallback backing type");
        EqualCondition(nested.ShowIf, fallback.ShowIf, "fallback ShowIf");

        var plugin = ReadSource("src", "MaterialEditor.Base", "PluginBase.cs");
        var core = ReadSource(
            "src", "MaterialEditor.Core", "Core.MaterialEditor.Shaders.cs");
        Contains(
            plugin,
            "ShaderPropertyFallbacks.MergeInto(",
            "manifest parser routes the shared default through the hierarchy-safe fallback policy");
        Contains(
            core,
            "ShaderPropertyFallbacks.MergeInto(",
            "runtime shader loader routes the shared default through the hierarchy-safe fallback policy");
    }

    private static void LegacyFlatCategoriesRemainCompatible()
    {
        ConfigureOrganizer();
        var property = ParseProperty(
            ParseElement(
                "<Property Name=\"LegacyValue\" Type=\"Float\" Category=\"lighting\" />"));
        property.DeclarationOrder = 0;
        XMLShaderProperties.Clear();
        XMLShaderProperties["Test/Legacy"] =
            new Dictionary<string, ShaderPropertyData>(StringComparer.Ordinal)
            {
                [property.Name] = property
            };

        try
        {
            PropertyOrganizer.Refresh();
            var category = PropertyOrganizer.PropertyOrganization["Test/Legacy"].Single();
            Equal("Lighting", category.Id, "legacy normalized Category Id");
            Equal(1, category.DirectProperties.Count, "legacy direct property count");
            Equal(0, category.Subcategories.Count, "legacy Subcategory count");
            Equal(property, category.Properties.Single(), "legacy flat compatibility view");
        }
        finally
        {
            XMLShaderProperties.Clear();
            PropertyOrganizer.PropertyOrganization.Clear();
        }
    }

    private static void SearchFlattensCollapseAndDiagnosesFalseShowIf()
    {
        var presenter = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialSectionPresenter.cs");
        var context = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialSectionContext.cs");

        Contains(presenter, "if (hiddenByShowIf && !hasSearch)", "ShowIf hides only during normal browsing");
        Contains(presenter, "SearchOnlyHidden = hiddenByShowIf", "search retains false-ShowIf matches");
        Contains(presenter, "MarkSearchOnlyShowIfResult(row, definition.ShowIf);", "search decorates false-ShowIf matches");
        Contains(presenter, "row.Enabled = false;", "false-ShowIf search matches are disabled");
        Contains(presenter, "Inactive because ShowIf=\\\"", "search explains the blocking condition");
        Contains(presenter, "hasSearch ? 0 : 1", "search removes Subcategory indentation");
        Contains(context, "var showHeader = context.PropertyFilter.Count == 0;", "search suppresses hierarchy headers");
        Contains(context, "showHeader && storedCollapsed", "stored collapse applies only outside search");
        DoesNotContain(
            Slice(presenter, "private static void MarkSearchOnlyShowIfResult(", "private static string FormatCondition("),
            "ValueOnChange",
            "search diagnostics never write a value");
    }

    private static void HierarchyHeadersRemainCollapsible()
    {
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Texture.cs");
        var theme = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        var context = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialSectionContext.cs");
        var presentation = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Presentation.cs");
        var categoryBinding = Slice(
            binder,
            "private void BindCategory(",
            "private void BindSubcategory(");
        var subcategoryBinding = Slice(
            binder,
            "private void BindSubcategory(",
            "private void BindTexture(");

        foreach (var binding in new[] { categoryBinding, subcategoryBinding })
        {
            Contains(
                binding,
                "controls.CollapseIndicator.text = item.Collapsed",
                "each hierarchy level always binds its fold arrow");
            Contains(
                binding,
                "listeners.Listen(\n                controls.HeaderButton,",
                "each hierarchy level always binds its structural header");
        }
        Contains(context, "showHeader && storedCollapsed", "stored Category collapse affects only visible hierarchy");
        Contains(context, "return showHeader && storedCollapsed;", "stored Subcategory collapse affects only visible hierarchy");
        Contains(presentation, "if (!category.CanCollapse)", "collapse-all skips non-collapsible navigation targets");
        Contains(theme, "internal const string FoldCollapsed = \"\\u25B8\";", "collapsed arrow is a right triangle");
        Contains(theme, "internal const string FoldExpanded = \"\\u25BE\";", "expanded arrow is a down triangle");
    }

    private static void HierarchyHasExactlyTwoVisuallyDistinctLevels()
    {
        var rowModel = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowModel.Texture.cs");
        var context = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialSectionContext.cs");
        var theme = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.Theme.cs");
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");

        Contains(rowModel, "class PropertyCategoryRowModel", "Category row model");
        Contains(rowModel, "class PropertySubcategoryRowModel", "Subcategory row model");
        Contains(context, "HierarchyDepth = 1,", "Subcategory header indentation");
        Contains(theme, "SubcategoryHeaderIndent = 12f", "Subcategory header indent token");
        Contains(theme, "SubcategoryContentIndent = SubcategoryHeaderIndent", "Subcategory child indent token");
        Contains(factory, "\"PropertySubcategoryPanel\"", "distinct Subcategory panel");
        Contains(factory, "SubcategoryColor", "distinct Subcategory surface color");
        Contains(factory, "label.fontStyle = FontStyle.Normal;", "Subcategory typography differs from Category");
    }

    private static void SubcategoryControlsRemainPooled()
    {
        var factory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Texture.cs");
        var controls = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowControls.cs");
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Texture.cs");

        Equal(1, CountOccurrences(factory, "CreateSubcategoryRow(parent);"), "one pooled Subcategory template");
        Contains(
            controls,
            "PropertySubcategory = new PropertySubcategoryRowControls(owner);",
            "Subcategory controls are cached once");
        Contains(
            controls,
            "PropertyCategory,\n                PropertySubcategory,",
            "pooled HideAll covers both hierarchy headers");
        DoesNotContain(binder, "GetUIComponent", "Subcategory rebind performs no name lookup");
        DoesNotContain(binder, "AddComponent", "Subcategory rebind creates no components");
        DoesNotContain(binder, "new GameObject", "Subcategory rebind creates no GameObjects");
    }

    private static void DocumentationUsesThePresentationOnlyContract()
    {
        var schema = ReadSource("MANIFEST_SCHEMA_V2.md");
        var templateText = ReadSource(
            "Guides", "Material Editor Guide", "shader_manifest_template.xml");
        var template = LoadXml(templateText);
        var organizationalCategory = template.GetElementsByTagName("Category")
            .Cast<XmlElement>()
            .Single(element => element.GetAttribute("Id") == "lighting");
        var organizationalSubcategory = organizationalCategory
            .GetElementsByTagName("Subcategory")
            .Cast<XmlElement>()
            .Single(element => element.GetAttribute("Id") == "direct-light");
        var conditionalSubcategory = template.GetElementsByTagName("Subcategory")
            .Cast<XmlElement>()
            .Single(element => element.GetAttribute("Id") == "back-light");
        var conditionalProperties = conditionalSubcategory.GetElementsByTagName("Property")
            .Cast<XmlElement>()
            .ToDictionary(element => element.GetAttribute("Name"), StringComparer.Ordinal);

        Contains(schema, "This hierarchy changes presentation only", "schema hierarchy scope");
        Contains(schema, "Every hierarchy feature is opt-in", "schema hierarchy opt-in contract");
        Contains(schema, "`ShowIf` is the only dynamic", "schema ShowIf visibility rule");
        Contains(schema, "`UiLevel` is ignored", "development-only UiLevel exception");
        Contains(schema, "shared `default` property fallback", "schema shader-local fallback");
        Contains(schema, "shown disabled with the blocking condition", "schema search diagnostic");
        DoesNotContain(schema, "Texture3D", "Texture3D remains future work");

        Equal(2, organizationalSubcategory.GetElementsByTagName("Property").Count, "pure Subcategory has ordinary children");
        Equal("Boolean", conditionalProperties["UseBacklight"].GetAttribute("Type"), "template Boolean condition source type");
        Equal(
            "UseBacklight != 0",
            conditionalProperties["BacklightColor"].GetAttribute("ShowIf"),
            "template keeps child visibility explicit");
        Equal(
            string.Empty,
            conditionalProperties["BacklightColor"].GetAttribute("Category"),
            "nested template does not duplicate flat Category metadata");
        Equal(
            false,
            template.GetElementsByTagName("Property")
                .Cast<XmlElement>()
                .Any(element => string.Equals(
                    element.GetAttribute("Type"),
                    "Toggle",
                    StringComparison.OrdinalIgnoreCase)),
            "template declares no deprecated Toggle property type");
        DoesNotContain(templateText, "Texture3D", "template leaves Texture3D pending");
    }

    private static ShaderPropertyData ParseProperty(
        XmlElement element,
        int schemaVersion = 2)
    {
        ShaderPropertyData property;
        var warnings = new List<string>();
        var parsed = ShaderPropertyData.TryParse(
            element,
            warnings.Add,
            out property,
            schemaVersion);
        Equal(true, parsed, "property parse");
        return property;
    }

    private static void ConfigureOrganizer()
    {
        MaterialEditorPluginBase.SortPropertiesByCategory = new ConfigEntry<bool>(true);
        MaterialEditorPluginBase.SortPropertiesByName = new ConfigEntry<bool>(false);
        MaterialEditorPluginBase.SortPropertiesByType = new ConfigEntry<bool>(false);
    }

    private static void AssertEquivalentPropertySemantics(
        ShaderPropertyData expected,
        ShaderPropertyData actual,
        string name)
    {
        Equal(expected.Name, actual.Name, name + " Name");
        Equal(expected.Type, actual.Type, name + " Type");
        Equal(expected.DefaultValue, actual.DefaultValue, name + " DefaultValue");
        Equal(expected.MinValue, actual.MinValue, name + " MinValue");
        Equal(expected.MaxValue, actual.MaxValue, name + " MaxValue");
        Equal(expected.Hidden, actual.Hidden, name + " Hidden");
        Equal(expected.Category, actual.Category, name + " Category");
        Equal(expected.DisplayName, actual.DisplayName, name + " DisplayName");
        Equal(expected.EditorId, actual.EditorId, name + " EditorId");
        Equal(expected.OffValue, actual.OffValue, name + " OffValue");
        Equal(expected.OnValue, actual.OnValue, name + " OnValue");
        EqualCondition(expected.ShowIf, actual.ShowIf, name + " ShowIf");
    }

    private static void EqualCondition(
        MaterialEditorPropertyCondition expected,
        MaterialEditorPropertyCondition actual,
        string name)
    {
        Equal(expected == null, actual == null, name + " null state");
        if (expected == null)
            return;
        Equal(expected.PropertyName, actual.PropertyName, name + " property");
        Equal(expected.Comparison, actual.Comparison, name + " comparison");
        Equal(expected.Value, actual.Value, name + " value");
    }

    private static XmlElement FirstProperty(XmlDocument document) =>
        (XmlElement)document.GetElementsByTagName("Property")[0];

    private static Dictionary<string, ShaderPropertyData> ParseProperties(
        string xml)
    {
        var document = LoadXml(xml);
        return document.GetElementsByTagName("Property")
            .Cast<XmlElement>()
            .Select((element, declarationOrder) =>
            {
                var property = ParseProperty(element);
                property.DeclarationOrder = declarationOrder;
                return property;
            })
            .ToDictionary(
                property => property.Name,
                StringComparer.Ordinal);
    }

    private static string SnapshotOrganization(string shaderName) =>
        string.Join(
            "|",
            PropertyOrganizer.PropertyOrganization[shaderName]
                .Select(category =>
                    category.Id + ":" + string.Join(
                        ",",
                        category.Properties.Select(property =>
                            property.Name + "/" + property.Type))));

    private static string SnapshotResolvedOrganization(string shaderName) =>
        SnapshotOrganization(PropertyOrganizer.ResolveShaderKey(shaderName));

    private static XmlDocument LoadXml(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document;
    }

    private static XmlElement ParseElement(string xml)
    {
        var document = LoadXml(xml);
        return document.DocumentElement;
    }

    private static string ReadSource(params string[] segments) =>
        File.ReadAllText(Path.Combine(
            new[] { FindRepositoryRoot() }.Concat(segments).ToArray()))
            .Replace("\r\n", "\n");

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
        if (startIndex < 0)
            throw new InvalidOperationException("Could not locate source slice start: " + start);
        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            throw new InvalidOperationException("Could not locate source slice end: " + end);
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
