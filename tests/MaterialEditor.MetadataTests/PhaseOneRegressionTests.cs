using BepInEx.Configuration;
using MaterialEditorAPI;
using System.Xml;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

internal static class PhaseOneRegressionTests
{
    private static int _executed;

    internal static void Run()
    {
        _executed = 0;
        Case(1, "Schema 1 and legacy property types", SchemaOneAndLegacyPropertyTypes);
        Case(5, "Hidden is invisible", HiddenIsInvisible);
        Case(12, "DisplayName does not change PropertyName", DisplayNameDoesNotChangePropertyName);
        Case(13, "Property and category order", PropertyAndCategoryOrder);
        Case(14, "Enum parsing", EnumParsing);
        Case(15, "Unknown enum values", UnknownEnumValues);
        Case(16, "Mixed enum state", MixedEnumState);
        Case(17, "Vector2", Vector2Editor);
        Case(18, "Vector3", Vector3Editor);
        Case(19, "Vector4", Vector4Editor);
        Case(20, "Vector component preservation", VectorComponentPreservation);
        Case(21, "Float-backed toggle custom values", FloatBackedToggleCustomValues);
        Case(22, "Keyword remains distinct from Toggle Float", KeywordRemainsDistinctFromToggleFloat);
        Case(23, "ShowIf true", ShowIfTrue);
        Case(24, "ShowIf false", ShowIfFalse);
        Case(27, "Invalid conditions and metadata use safe fallbacks", InvalidMetadataUsesSafeFallbacks);
        Case(29, "Extension API capabilities", ExtensionApiCapabilities);
        // #30 is intentionally not claimed here: PublicApiAnalyzers is a build-time check.
        Case(31, "Schema 2 XML roundtrip", SchemaTwoXmlRoundtrip);
        Case(32, "Category collapse toggle", CategoryCollapseToggle);
        Case(34, "Card persistence excludes UI classification and wires Vector data", UiClassificationIsAbsentFromCardModels);
        Case(35, "Scene persistence excludes UI classification and wires Vector data", UiClassificationIsAbsentFromSceneModels);

        Equal(21, _executed, "phase-one automated case count");
        Console.WriteLine(
            "21/22 phase-one automated cases passed; #30 PublicApiAnalyzers remains a build-time check.");
    }

    private static void SchemaOneAndLegacyPropertyTypes()
    {
        Equal(1, ShaderPropertyMetadataParser.ReadSchemaVersion(null), "shader without manifest");

        var document = LoadXml(
            @"<MaterialEditor SchemaVersion=""1"">
                <Shader Name=""Legacy/Shader"">
                  <Property Name=""RangeValue"" Type=""Float"" Range=""-2.5, 4.5"" Category=""lighting"" />
                  <Property Name=""HiddenValue"" Type=""Float"" Hidden=""true"" />
                  <Property Name=""Tint"" Type=""Color"" />
                  <Property Name=""MainTex"" Type=""Texture"" />
                  <Property Name=""UseFeature"" Type=""Keyword"" />
                </Shader>
              </MaterialEditor>");
        Equal(1, ShaderPropertyMetadataParser.ReadSchemaVersion(document.DocumentElement), "schema 1");

        var elements = document.GetElementsByTagName("Property").Cast<XmlElement>().ToArray();
        var range = ParseLegacyProperty(elements[0]);
        Equal(ShaderPropertyType.Float, range.Type, "legacy Range type");
        Equal(-2.5f, range.MinValue.Value, "legacy Range minimum");
        Equal(4.5f, range.MaxValue.Value, "legacy Range maximum");
        Equal("lighting", range.Category, "legacy Category");

        var hidden = ParseLegacyProperty(elements[1]);
        Equal(true, hidden.Hidden, "legacy Hidden");
        Equal(ShaderPropertyType.Color, ParseLegacyProperty(elements[2]).Type, "legacy Color");
        Equal(ShaderPropertyType.Texture, ParseLegacyProperty(elements[3]).Type, "legacy Texture");
        Equal(ShaderPropertyType.Keyword, ParseLegacyProperty(elements[4]).Type, "legacy Keyword");

        var schemaOneMetadata = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"LegacyMetadata\" Type=\"Float\" Editor=\"Toggle\" DisplayName=\"Ignored\" />"),
            schemaVersion: 1);
        Equal(null, schemaOneMetadata.EditorId, "schema-1 editor metadata is ignored");
        Equal("LegacyMetadata", schemaOneMetadata.DisplayName, "schema-1 display name remains legacy");
    }

    private static void HiddenIsInvisible()
    {
        Equal(
            false,
            MaterialEditorPropertyVisibilityPolicy.IsVisible(
                hidden: true,
                visibilityConditionSatisfied: true),
            "Hidden property");
    }

    private static void DisplayNameDoesNotChangePropertyName()
    {
        var property = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"_Cull\" Type=\"Float\" DisplayName=\"Cull Mode\" />"));
        Equal("_Cull", property.Name, "manifest property name");
        Equal("Cull Mode", property.DisplayName, "manifest display name");
        Equal(true, property.HasExplicitDisplayName, "manifest display name provenance");

        var implicitLabel = ParseLegacyProperty(
            ParseElement("<Property Name=\"MainTex\" Type=\"Texture\" />"));
        Equal(false, implicitLabel.HasExplicitDisplayName, "implicit display name provenance");

        var descriptor = new MaterialEditorPropertyDescriptor(
            "extension.cull",
            "Extension Cull",
            MaterialEditorPropertyEditorIds.Enum);
        Equal("extension.cull", descriptor.PropertyName, "descriptor property name");
        descriptor.DisplayName = "Readable Label";
        Equal("extension.cull", descriptor.PropertyName, "property name after relabeling");
    }

    private static void PropertyAndCategoryOrder()
    {
        ConfigureOrganizer();
        var earlyCategoryLateProperty = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"B\" Type=\"Float\" Category=\"first\" CategoryOrder=\"-20\" Order=\"20\" />"));
        var earlyCategoryEarlyProperty = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"A\" Type=\"Float\" Category=\"first\" CategoryOrder=\"-20\" Order=\"-10\" />"));
        var laterCategory = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"C\" Type=\"Float\" Category=\"second\" CategoryOrder=\"10\" />"));
        earlyCategoryLateProperty.DeclarationOrder = 0;
        earlyCategoryEarlyProperty.DeclarationOrder = 1;
        laterCategory.DeclarationOrder = 2;

        MaterialEditorPluginBase.XMLShaderProperties.Clear();
        MaterialEditorPluginBase.XMLShaderProperties["Order/Test"] =
            new Dictionary<string, MaterialEditorPluginBase.ShaderPropertyData>
            {
                [earlyCategoryLateProperty.Name] = earlyCategoryLateProperty,
                [earlyCategoryEarlyProperty.Name] = earlyCategoryEarlyProperty,
                [laterCategory.Name] = laterCategory
            };
        try
        {
            PropertyOrganizer.Refresh();
            var categories = PropertyOrganizer.PropertyOrganization["Order/Test"];
            Equal("First", categories[0].Name, "first category");
            Equal("Second", categories[1].Name, "second category");
            Equal("A", categories[0].Properties[0].Name, "first ordered property");
            Equal("B", categories[0].Properties[1].Name, "second ordered property");
        }
        finally
        {
            MaterialEditorPluginBase.XMLShaderProperties.Clear();
        }
    }

    private static void EnumParsing()
    {
        var metadata = ShaderPropertyMetadataParser.Parse(
            ParseElement(
                @"<Property Name=""Blend"" Editor=""Enum"">
                    <Option Value=""0"" DisplayName=""Off"" />
                    <Option Value=""1"" Label=""Front"" />
                    <Option Value=""2"">Back</Option>
                  </Property>"));
        Equal(MaterialEditorPropertyEditorIds.Enum, metadata.EditorId, "enum editor ID");
        Equal(3, metadata.EnumOptions.Count, "enum option count");
        Equal("Front", metadata.EnumOptions[1].DisplayName, "Label alias");
        Equal("Back", metadata.EnumOptions[2].DisplayName, "inner-text label");
    }

    private static void UnknownEnumValues()
    {
        var options = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "Off"),
            new MaterialEditorEnumOption(0.25f, "Quarter"),
            new MaterialEditorEnumOption(-3f, "Negative"),
            new MaterialEditorEnumOption(7.5f, "High")
        };
        Equal(1, MaterialEditorSemanticValuePolicy.FindEnumOptionIndex(options, 0.25f), "exact declared value");
        Equal(2, MaterialEditorSemanticValuePolicy.FindEnumOptionIndex(options, -3f), "declared negative value");
        Equal(-1, MaterialEditorSemanticValuePolicy.FindEnumOptionIndex(options, 0.25000003f), "near value is not an exact match");
        Equal(-1, MaterialEditorSemanticValuePolicy.FindEnumOptionIndex(options, -99f), "unknown negative value");
        Equal(-1, MaterialEditorSemanticValuePolicy.FindEnumOptionIndex(options, 99f), "unknown out-of-range value");
    }

    private static void MixedEnumState()
    {
        var writes = 0;
        var selected = float.NaN;
        var editor = new MaterialEditorEnumPropertyEditor(
            0f,
            0f,
            new[]
            {
                new MaterialEditorEnumOption(0f, "Off"),
                new MaterialEditorEnumOption(1f, "On")
            },
            value =>
            {
                writes++;
                selected = value;
            },
            () => { });
        editor.IsMixed = true;
        Equal(true, editor.IsMixed, "mixed state");
        Equal(0, writes, "representing Mixed does not write");
        editor.ValueChanged(1f);
        Equal(1, writes, "explicit Mixed enum selection writes once");
        Equal(1f, selected, "explicit Mixed enum selection keeps the selected value");

        var operations = new List<string>();
        var persisted = float.NaN;
        MaterialEditorSemanticValuePolicy.PersistExplicitEnumSelection(
            () => operations.Add("remove"),
            value =>
            {
                operations.Add("set");
                persisted = value;
            },
            0.25000003f);
        Equal(2, operations.Count, "explicit Mixed persistence operation count");
        Equal("remove", operations[0], "explicit Mixed removes the legacy override first");
        Equal("set", operations[1], "explicit Mixed stores the selected value second");
        Equal(0.25000003f, persisted, "explicit Mixed stores the exact selected value");

        AssertMixedSelectionRouting();
    }

    private static void Vector2Editor() => VerifyVectorEditor("Vector2", 2);

    private static void Vector3Editor() => VerifyVectorEditor("Vector3", 3);

    private static void Vector4Editor() => VerifyVectorEditor("Vector4", 4);

    private static void VectorComponentPreservation()
    {
        var x = 1f;
        var y = 2f;
        var z = 3f;
        var w = 4f;
        MaterialEditorSemanticValuePolicy.SetVectorComponent(ref x, ref y, ref z, ref w, 1, 20f);
        Equal(1f, x, "preserved x");
        Equal(20f, y, "updated y");
        Equal(3f, z, "preserved z");
        Equal(4f, w, "preserved w");
        Throws<ArgumentOutOfRangeException>(
            () => MaterialEditorSemanticValuePolicy.SetVectorComponent(
                ref x,
                ref y,
                ref z,
                ref w,
                4,
                0f),
            "invalid vector component");

        var changedComponent = -1;
        var changedValue = float.NaN;
        var editor = new MaterialEditorVectorPropertyEditor(
            new Vector4(1f, 2f, 3f, 4f),
            new Vector4(1f, 2f, 3f, 4f),
            4,
            _ => throw new InvalidOperationException("full-vector callback must not be needed for a mixed component edit"),
            () => { })
        {
            ComponentChanged = (component, value) =>
            {
                changedComponent = component;
                changedValue = value;
            }
        };
        editor.MixedComponents[2] = true;
        editor.ComponentChanged(2, 30f);
        Equal(2, changedComponent, "mixed vector component callback index");
        Equal(30f, changedValue, "mixed vector component callback value");

        AssertVectorComponentCallbackRouting();
    }

    private static void FloatBackedToggleCustomValues()
    {
        const float offValue = -1.5f;
        const float onValue = 3.25f;
        Equal("materialeditor.toggle", MaterialEditorPropertyEditorIds.Toggle, "extension toggle editor ID");
        Equal(offValue, MaterialEditorSemanticValuePolicy.SelectToggleValue(false, offValue, onValue), "off value");
        Equal(onValue, MaterialEditorSemanticValuePolicy.SelectToggleValue(true, offValue, onValue), "on value");

        var writtenValue = float.NaN;
        var editor = new MaterialEditorTogglePropertyEditor(
            offValue,
            offValue,
            offValue,
            onValue,
            value => writtenValue = value,
            () => { });
        Equal(-1.5f, editor.OffValue, "toggle contract off value");
        Equal(3.25f, editor.OnValue, "toggle contract on value");
        editor.IsMixed = true;
        editor.ValueChanged(editor.OnValue);
        Equal(3.25f, writtenValue, "explicit Mixed toggle selection keeps the selected value");
    }

    private static void KeywordRemainsDistinctFromToggleFloat()
    {
        var keywordEditorId = MaterialEditorPropertyEditorIds.Boolean;
        var toggleEditorId = MaterialEditorPropertyEditorIds.Toggle;
        Equal("materialeditor.boolean", keywordEditorId, "keyword editor ID");
        Equal("materialeditor.toggle", toggleEditorId, "extension toggle editor ID");
        Equal(false, keywordEditorId == toggleEditorId, "editor IDs are distinct");

        MaterialEditorPropertyEditor keywordContract = new MaterialEditorBooleanPropertyEditor(
            false,
            false,
            _ => { },
            () => { });
        MaterialEditorPropertyEditor toggleContract = new MaterialEditorTogglePropertyEditor(
            0f,
            0f,
            0f,
            1f,
            _ => { },
            () => { });
        Equal(false, keywordContract.GetType() == toggleContract.GetType(), "contract types are distinct");
    }

    private static void ShowIfTrue()
    {
        var property = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"Dependent\" Type=\"Float\" ShowIf=\"_Source &gt;= 2\" />"));
        Equal(
            true,
            MaterialEditorConditionPolicy.Evaluate(property.ShowIf, name => name == "Source" ? 2f : null),
            "ShowIf result");
    }

    private static void ShowIfFalse()
    {
        var property = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"Dependent\" Type=\"Float\" ShowIf=\"_Source &gt;= 2\" />"));
        Equal(
            false,
            MaterialEditorConditionPolicy.Evaluate(property.ShowIf, name => name == "Source" ? 1f : null),
            "ShowIf result");
    }

    private static void InvalidMetadataUsesSafeFallbacks()
    {
        var warnings = new List<string>();
        var property = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"Safe\" Type=\"Float\" Editor=\"Mystery\" ShowIf=\"Source == nope\" />"),
            warnings);
        Equal(null, property.EditorId, "unknown editor fallback");
        Equal(null, property.ShowIf, "invalid condition fallback");
        Equal(2, warnings.Count, "one warning per invalid declaration");
        Equal(
            true,
            MaterialEditorConditionPolicy.Evaluate(
                property.ShowIf,
                _ => throw new InvalidOperationException(),
                fallback: true),
            "safe fallback");

        warnings.Clear();
        var enumWithoutOptions = ShaderPropertyMetadataParser.Parse(
            ParseElement("<Property Name=\"Empty\" Editor=\"Enum\" />"),
            warnings.Add);
        Equal(null, enumWithoutOptions.EditorId, "enum fallback editor");
        Equal(1, warnings.Count, "enum warning once");

        warnings.Clear();
        var incompleteVector = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"Direction\" Type=\"Vector\" Editor=\"Vector\" VectorComponentCount=\"1\" />"),
            warnings);
        Equal(ShaderPropertyType.Vector, incompleteVector.Type, "incomplete vector remains accessible");
        Equal(MaterialEditorPropertyEditorIds.Vector4, incompleteVector.EditorId, "incomplete vector safe fallback");
        Equal(1, warnings.Count, "vector warning once");

        warnings.Clear();
        var vectorEditorOnFloat = ParseLegacyProperty(
            ParseElement(
                "<Property Name=\"Direction\" Type=\"Float\" Editor=\"Vector3\" />"),
            warnings);
        Equal(ShaderPropertyType.Float, vectorEditorOnFloat.Type, "invalid Vector editor preserves Float type");
        Equal(null, vectorEditorOnFloat.EditorId, "Vector editor rejected for Float property");
        Equal(1, warnings.Count, "Vector-on-Float warning once");

        var presenter = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.MaterialSectionPresenter.cs"));
        Contains(
            presenter,
            "shaderKey == PropertyOrganizer.DefaultShaderKey\n"
            + "                    ? organizedCategories.SelectMany(",
            "unknown-shader conditions use the conservative UI fallback");
        Contains(
            presenter,
            ": manifestPropertyMap.Values;",
            "exact shaders retain their raw manifest condition definitions");
        var conditionKindsCall = presenter.IndexOf(
            "var conditionKinds = BuildConditionSourceKinds(",
            StringComparison.Ordinal);
        var manifestDefinitionsArgument = presenter.IndexOf(
            "manifestDefinitions,",
            conditionKindsCall,
            StringComparison.Ordinal);
        var extensionDescriptorsArgument = presenter.IndexOf(
            "extensionDescriptors,",
            manifestDefinitionsArgument,
            StringComparison.Ordinal);
        var requestedSourcesArgument = presenter.IndexOf(
            "conditionDependencies);",
            extensionDescriptorsArgument,
            StringComparison.Ordinal);
        Equal(
            true,
            conditionKindsCall >= 0
            && manifestDefinitionsArgument > conditionKindsCall
            && extensionDescriptorsArgument > manifestDefinitionsArgument
            && requestedSourcesArgument > extensionDescriptorsArgument,
            "ShowIf source kinds receive raw manifest definitions, including Hidden sources");
    }

    private static void ExtensionApiCapabilities()
    {
        Equal(new Version(1, 2, 0), MaterialEditorExtensionApi.ApiVersion, "API version");
        var semanticCapabilities =
            MaterialEditorApiCapability.EnumPropertyEditors
            | MaterialEditorApiCapability.VectorPropertyEditors
            | MaterialEditorApiCapability.ConditionalPropertyVisibility
            | MaterialEditorApiCapability.ToggleFloatPropertyEditors;
        Equal(true, MaterialEditorExtensionApi.Supports(semanticCapabilities), "semantic capabilities");
        Equal(true, MaterialEditorExtensionApi.Supports(MaterialEditorApiCapability.None), "None capability");
        Equal(
            false,
            MaterialEditorExtensionApi.Supports((MaterialEditorApiCapability)(1 << 20)),
            "unknown capability");

        var labelTypes = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.LabelClick.cs"));
        Contains(labelTypes, "CubemapProperty = 10", "Cubemap label type enum value");
        Contains(labelTypes, "VectorProperty = 9", "Vector label type enum value");
        var publicApi = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.API", "PublicAPI.Unshipped.txt"));
        Contains(
            publicApi,
            "MaterialEditorAPI.MaterialEditorLabelType.CubemapProperty = 10",
            "Cubemap label type PublicAPI declaration");
        Contains(
            publicApi,
            "MaterialEditorAPI.MaterialEditorLabelType.VectorProperty = 9",
            "Vector label type PublicAPI declaration");
    }

    private static void SchemaTwoXmlRoundtrip()
    {
        var firstDocument = LoadXml(
            @"<MaterialEditor SchemaVersion=""2"">
                <Shader Name=""Roundtrip/Test"">
                  <Property Name=""Mode"" Type=""Float"" DisplayName=""Mode Label"" Order=""4"" Editor=""Enum"">
                    <Option Value=""0"" DisplayName=""Off"" />
                    <Option Value=""1"" DisplayName=""On"" />
                  </Property>
                </Shader>
              </MaterialEditor>");
        var first = ParseLegacyProperty(
            (XmlElement)firstDocument.GetElementsByTagName("Property")[0]);

        var secondDocument = LoadXml(firstDocument.OuterXml);
        Equal(2, ShaderPropertyMetadataParser.ReadSchemaVersion(secondDocument.DocumentElement), "roundtrip schema");
        var second = ParseLegacyProperty(
            (XmlElement)secondDocument.GetElementsByTagName("Property")[0]);
        Equal(first.Name, second.Name, "roundtrip property name");
        Equal(first.DisplayName, second.DisplayName, "roundtrip display name");
        Equal(first.Order, second.Order, "roundtrip order");
        Equal(first.EditorId, second.EditorId, "roundtrip editor");
        Equal(first.EnumOptions.Count, second.EnumOptions.Count, "roundtrip enum options");
    }

    private static void CategoryCollapseToggle()
    {
        var states = new Dictionary<string, bool>();
        Equal(false, MaterialEditorSessionState.IsCollapsed(states, "category"), "initial state");
        MaterialEditorSessionState.SetCollapsed(states, "category", true);
        Equal(true, MaterialEditorSessionState.IsCollapsed(states, "category"), "collapsed state");
        MaterialEditorSessionState.SetCollapsed(states, "category", false);
        Equal(false, MaterialEditorSessionState.IsCollapsed(states, "category"), "expanded state");
        Equal(0, states.Count, "expanded state is not retained");
    }

    private static void UiClassificationIsAbsentFromCardModels()
    {
        AssertUiClassificationIsAbsent(
            Path.Combine("src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Models.cs"),
            "card models");

        var models = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Models.cs"));
        Contains(models, "public class MaterialVectorProperty", "card Vector model");
        Contains(models, "public Vector4 Value;", "card Vector current value");
        Contains(models, "public Vector4 ValueOriginal;", "card Vector original value");

        var cardSave = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.cs"));
        Contains(
            cardSave,
            "data.data.Add(nameof(MaterialVectorPropertyList), MessagePackSerializer.Serialize(MaterialVectorPropertyList))",
            "card Vector save key");

        var persistence = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Persistence.cs"));
        Equal(
            true,
            CountOccurrences(persistence, "TryGetValue(nameof(MaterialVectorPropertyList)") >= 2,
            "card and coordinate Vector load keys");
        Contains(
            persistence,
            "MessagePackSerializer.Serialize(coordinateMaterialVectorPropertyList)",
            "coordinate Vector save payload");
        Contains(
            persistence,
            "MessagePackSerializer.Deserialize<List<MaterialVectorProperty>>",
            "card/coordinate Vector deserialize path");
        Contains(
            persistence,
            "MigrateLegacyMaterialVectorProperties(clothes, accessories, hair, body)",
            "card/coordinate legacy Color migration hook");
    }

    private static void UiClassificationIsAbsentFromSceneModels()
    {
        AssertUiClassificationIsAbsent(
            Path.Combine("src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Models.cs"),
            "scene models");

        var models = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Models.cs"));
        Contains(models, "public class MaterialVectorProperty", "scene Vector model");
        Contains(models, "public Vector4 Value;", "scene Vector current value");
        Contains(models, "public Vector4 ValueOriginal;", "scene Vector original value");

        var scene = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.cs"));
        Contains(
            scene,
            "data.data.Add(nameof(MaterialVectorPropertyList), MessagePackSerializer.Serialize(MaterialVectorPropertyList))",
            "scene Vector save key");
        Contains(
            scene,
            "MessagePackSerializer.Deserialize<List<MaterialVectorProperty>>",
            "scene Vector deserialize path");
        Contains(
            scene,
            "Native vector data is authoritative if a transitional save contains both keys.",
            "scene native Vector precedence");
        Contains(
            scene,
            "MigrateLegacyMaterialVectorProperty",
            "scene legacy Color migration hook");
    }

    private static void AssertUiClassificationIsAbsent(string relativePath, string name)
    {
        var source = ReadRepositorySource(relativePath);
        Equal(false, source.Contains("UiLevel", StringComparison.Ordinal), name + " level metadata");
        Equal(false, source.Contains("UiMode", StringComparison.Ordinal), name + " mode state");
    }

    private static void AssertMixedSelectionRouting()
    {
        var source = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.RowBinder.EnumVectorToggle.cs"));
        var sharedBooleanBinding = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.RowBinding.Common.cs"));
        Equal(
            true,
            CountOccurrences(source, "var wasMixed = item.IsMixed;") >= 1,
            "Enum captures Mixed before clearing it");
        Contains(
            sharedBooleanBinding,
            "var wasMixed = toggle.IsMixed;",
            "shared Boolean/header binding captures Mixed before clearing it");
        Contains(
            source,
            "BooleanPropertyRowModelBinding.ApplyUserValue(item, value);",
            "ordinary Boolean row uses the shared header-safe semantic path");
        Contains(
            source,
            "MaterialEditorSemanticValuePolicy.PersistExplicitEnumSelection(",
            "Enum persists an explicit Mixed selection");

        var enumStart = source.IndexOf("private void BindEnum", StringComparison.Ordinal);
        var enumListener = source.IndexOf(
            "listeners.Listen(controls.Dropdown",
            enumStart,
            StringComparison.Ordinal);
        Equal(true, enumStart >= 0 && enumListener > enumStart, "Enum listener block");
        var enumInitialization = source.Substring(enumStart, enumListener - enumStart);
        Contains(enumInitialization, "controls.OptionCache.Rebuild", "Enum draws before listening");
        Equal(false, enumInitialization.Contains("ValueOnChange", StringComparison.Ordinal), "Enum open does not write");
        Equal(false, enumInitialization.Contains("ValueOnReset", StringComparison.Ordinal), "Enum refresh does not reset");

        var toggleStart = source.IndexOf("private void BindFloatToggle", StringComparison.Ordinal);
        var toggleListener = source.IndexOf(
            "listeners.Listen(controls.Toggle",
            toggleStart,
            StringComparison.Ordinal);
        Equal(true, toggleStart >= 0 && toggleListener > toggleStart, "Boolean listener block");
        var toggleInitialization = source.Substring(toggleStart, toggleListener - toggleStart);
        Contains(toggleInitialization, "controls.Toggle.Set(desired, false)", "Boolean draws without notifying");
        Equal(false, toggleInitialization.Contains("ValueOnChange", StringComparison.Ordinal), "Boolean open does not write");
        Equal(false, toggleInitialization.Contains("ValueOnReset", StringComparison.Ordinal), "Boolean refresh does not reset");
    }

    private static void AssertVectorComponentCallbackRouting()
    {
        var source = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.RowBinder.EnumVectorToggle.cs"));
        var start = source.IndexOf("Action<int, float, bool> apply", StringComparison.Ordinal);
        var end = source.IndexOf("refreshState();", start, StringComparison.Ordinal);
        Equal(true, start >= 0 && end > start, "Vector apply callback block");
        var block = source.Substring(start, end - start);
        Contains(block, "if (!wasMixed", "Mixed vector edit bypasses whole-vector reset");
        Contains(block, "item.ComponentOnChange != null", "Vector component callback");

        var capture = source.IndexOf(
            "var wasMixed = HasMixed(item.MixedComponents, count);",
            end,
            StringComparison.Ordinal);
        var clear = source.IndexOf(
            "item.MixedComponents[index] = false;",
            capture,
            StringComparison.Ordinal);
        var dispatch = source.IndexOf(
            "apply(index, parsed, wasMixed);",
            clear,
            StringComparison.Ordinal);
        Equal(
            true,
            capture >= 0 && clear > capture && dispatch > clear,
            "Vector captures Mixed state before clearing and dispatching the component");
    }

    private static string ReadRepositorySource(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath))
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

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

    private static void VerifyVectorEditor(string editorName, int expectedComponents)
    {
        var metadata = ShaderPropertyMetadataParser.Parse(
            ParseElement($"<Property Name=\"Direction\" Editor=\"{editorName}\" />"));
        Equal(
            "materialeditor.vector" + expectedComponents,
            metadata.EditorId,
            editorName + " editor ID");
        Equal(expectedComponents, metadata.VectorComponentCount.Value, editorName + " component count");

        var contract = new MaterialEditorVectorPropertyEditor(
            new Vector4(1f, 2f, 3f, 4f),
            new Vector4(0f, 0f, 0f, 0f),
            expectedComponents,
            _ => { },
            () => { });
        Equal(expectedComponents, contract.ComponentCount, editorName + " contract count");
    }

    private static MaterialEditorPluginBase.ShaderPropertyData ParseLegacyProperty(
        XmlElement element,
        ICollection<string> warnings = null,
        int schemaVersion = 2)
    {
        MaterialEditorPluginBase.ShaderPropertyData property;
        var parsed = MaterialEditorPluginBase.ShaderPropertyData.TryParse(
            element,
            warnings == null ? null : warnings.Add,
            out property,
            schemaVersion);
        Equal(true, parsed, "legacy property parse");
        return property;
    }

    private static void ConfigureOrganizer()
    {
        MaterialEditorPluginBase.SortPropertiesByCategory = new ConfigEntry<bool>(true);
        MaterialEditorPluginBase.SortPropertiesByName = new ConfigEntry<bool>(false);
        MaterialEditorPluginBase.SortPropertiesByType = new ConfigEntry<bool>(false);
    }

    private static XmlDocument LoadXml(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document;
    }

    private static XmlElement ParseElement(string xml) => LoadXml(xml).DocumentElement;

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "tests", "MaterialEditor.MetadataTests", "MaterialEditor.MetadataTests.csproj")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the KK_Plugins repository root.");
    }

    private static void Case(int number, string name, Action action)
    {
        try
        {
            action();
            _executed++;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Phase-one case #{number} ({name}) failed.", exception);
        }
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
