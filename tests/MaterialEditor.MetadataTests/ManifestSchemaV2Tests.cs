using System.Xml;
using MaterialEditorAPI;

internal static class ManifestSchemaV2Tests
{
    internal static void Run()
    {
        SchemaVersionTwoIsAnExplicitCompatibilityGate();
        SchemaTwoMetadataIsParsedWithSafeDefaults();
        FloatBackedAliasesAreLimitedToSchemaTwo();
        BooleanIsCanonicalAndToggleIsReadCompatible();
        EnumOptionsUseUnityStyleAttributeWithInvariantValues();
        EnumSelectionUsesDeclaredValuesAndPreservesSpecialStates();
        ExplicitMixedEnumSelectionRecreatesThePersistedOverride();
        BooleanValuesRoundTripAsFloatZeroOrOne();
        ShowIfSupportsTheDocumentedFormsAndFailsOpen();
    }

    private static void SchemaVersionTwoIsAnExplicitCompatibilityGate()
    {
        var warnings = new List<string>();

        Equal(
            1,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                Element("<MaterialEditor />"),
                warnings.Add),
            "missing schema version uses legacy mode");
        Equal(
            1,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                Element("<MaterialEditor SchemaVersion=\"1\" />"),
                warnings.Add),
            "explicit schema version 1");
        Equal(
            2,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                Element("<MaterialEditor SchemaVersion=\"2\" />"),
                warnings.Add),
            "explicit schema version 2");
        Equal(0, warnings.Count, "known schema versions do not warn");

        Equal(
            1,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                Element("<MaterialEditor SchemaVersion=\"3\" />"),
                warnings.Add),
            "future schema version falls back to legacy mode");
        Equal(
            1,
            ShaderPropertyMetadataParser.ReadSchemaVersion(
                Element("<MaterialEditor SchemaVersion=\"invalid\" />"),
                warnings.Add),
            "invalid schema version falls back to legacy mode");
        Equal(2, warnings.Count, "unsupported schema versions warn");
    }

    private static void SchemaTwoMetadataIsParsedWithSafeDefaults()
    {
        var warnings = new List<string>();
        var metadata = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Detail\" DisplayName=\"Detail strength\" "
                + "UiLevel=\"advanced\" Editor=\"Boolean\" Invert=\"true\" "
                + "ShowIf=\"_Enabled &gt;= 1\" />"),
            warnings.Add);

        Equal("Detail strength", metadata.DisplayName, "display name");
        Equal(
            MaterialEditorPropertyUiLevel.Advanced,
            metadata.UiLevel,
            "advanced UI level");
        Equal(ShaderPropertyEditorIds.Toggle, metadata.EditorId, "Boolean uses the Float toggle editor");
        Equal(true, metadata.Invert, "inverted boolean");
        NotNull(metadata.ShowIf, "parsed ShowIf");
        Equal("Enabled", metadata.ShowIf.PropertyName, "normalized condition source");
        Equal(
            MaterialEditorConditionComparison.GreaterThanOrEqual,
            metadata.ShowIf.Comparison,
            "condition comparison");
        Equal(1f, metadata.ShowIf.Value, "condition value");
        Equal(0, warnings.Count, "valid metadata warning count");

        var defaults = ShaderPropertyMetadataParser.Parse(
            Element("<Property Name=\"PlainFloat\" Type=\"Float\" />"));
        Equal(null, defaults.DisplayName, "display name remains optional");
        Equal(
            MaterialEditorPropertyUiLevel.Basic,
            defaults.UiLevel,
            "default UI level");
        Equal(false, defaults.Invert, "boolean is not inverted by default");
        Equal(0, defaults.EnumOptions.Count, "default enum option count");

        warnings.Clear();
        var invalid = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Invalid\" UiLevel=\"Expert\" "
                + "Invert=\"sometimes\" />"),
            warnings.Add);
        Equal(
            MaterialEditorPropertyUiLevel.Basic,
            invalid.UiLevel,
            "invalid UI level fallback");
        Equal(false, invalid.Invert, "invalid invert fallback");
        Equal(2, warnings.Count, "invalid metadata warning count");
    }

    private static void FloatBackedAliasesAreLimitedToSchemaTwo()
    {
        Alias("Boolean", "Float", ShaderPropertyEditorIds.Toggle);
        Alias("boolean", "Float", ShaderPropertyEditorIds.Toggle);
        Alias("Toggle", "Float", ShaderPropertyEditorIds.Toggle);
        Alias("toggle", "Float", ShaderPropertyEditorIds.Toggle);
        Alias("Enum", "Float", ShaderPropertyEditorIds.Enum);

        NoAlias("Float", 2, "ordinary type is not an alias");
        NoAlias("Dropdown", 2, "dropdown is not a data type alias");
        NoAlias("Boolean", 1, "boolean is not a legacy schema alias");
        NoAlias("Toggle", 1, "toggle is not a legacy schema alias");
        NoAlias("Enum", 1, "enum is not a legacy alias");
        NoAlias("Dropdown", 3, "unknown schema does not enable aliases");

        var unknownSchema = ShaderPropertyMetadataParser.ReadSchemaVersion(
            Element("<MaterialEditor SchemaVersion=\"99\" />"));
        NoAlias(
            "Toggle",
            unknownSchema,
            "unknown schema fallback remains legacy for aliases");
    }

    private static void BooleanIsCanonicalAndToggleIsReadCompatible()
    {
        var canonical = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Enabled\" Type=\"Float\" "
                + "Editor=\"Boolean\" />"));
        Equal(
            ShaderPropertyEditorIds.Toggle,
            canonical.EditorId,
            "Float property with canonical Boolean editor");

        var legacyName = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Enabled\" Type=\"Float\" "
                + "Editor=\"Toggle\" />"));
        Equal(
            ShaderPropertyEditorIds.Toggle,
            legacyName.EditorId,
            "legacy Toggle editor normalizes to Boolean");

        var legacyId = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Enabled\" Type=\"Float\" "
                + "Editor=\"materialeditor.toggle\" />"));
        Equal(
            ShaderPropertyEditorIds.Toggle,
            legacyId.EditorId,
            "legacy Toggle editor ID normalizes to Boolean");

        var plainFloat = ShaderPropertyMetadataParser.Parse(
            Element("<Property Name=\"Strength\" Type=\"Float\" />"));
        Equal(null, plainFloat.EditorId, "ordinary Float editor remains unchanged");
    }

    private static void EnumOptionsUseUnityStyleAttributeWithInvariantValues()
    {
        var warnings = new List<string>();
        ShaderPropertyUiMetadata metadata;
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                new System.Globalization.CultureInfo("fr-FR");
            metadata = ShaderPropertyMetadataParser.Parse(
                Element(
                    "<Property Name=\"Mode\" Editor=\"Enum\" "
                    + "Enums=\"Off,0, Soft,1.5, Sharp,2\">"
                    + "<Group><Option Value=\"99\" DisplayName=\"Ignored\" /></Group>"
                    + "</Property>"),
                warnings.Add);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }

        Equal(ShaderPropertyEditorIds.Enum, metadata.EditorId, "enum editor");
        Equal(3, metadata.EnumOptions.Count, "valid enum option count");
        Option(metadata.EnumOptions[0], 0f, "Off", "first enum option");
        Option(metadata.EnumOptions[1], 1.5f, "Soft", "fractional enum option");
        Option(metadata.EnumOptions[2], 2f, "Sharp", "third enum option");
        Equal(
            false,
            metadata.EnumOptions.Any(option => option.Value == 99f),
            "nested Option elements are ignored");
        Equal(0, warnings.Count, "valid enum warning count");

        warnings.Clear();
        var invalid = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"InvalidMode\" Editor=\"Enum\" "
                + "Enums=\"Off,0,,1,NotFinite,Infinity,"
                + "DuplicateValue,0,Off,3,Dangling\" />"),
            warnings.Add);
        Equal(ShaderPropertyEditorIds.Enum, invalid.EditorId, "partially valid enum editor");
        Equal(1, invalid.EnumOptions.Count, "only valid unique enum option remains");
        Option(invalid.EnumOptions[0], 0f, "Off", "surviving enum option");
        Equal(
            5,
            warnings.Count,
            "odd pair, empty label, non-finite value and duplicates warn");

        warnings.Clear();
        var empty = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"EmptyMode\" Editor=\"Enum\" Enums=\"\" />"),
            warnings.Add);
        Equal(null, empty.EditorId, "empty enum falls back to the type editor");
        Equal(1, warnings.Count, "empty enum warning count");
    }

    private static void EnumSelectionUsesDeclaredValuesAndPreservesSpecialStates()
    {
        var consecutive = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(0f, "Off"),
            new MaterialEditorEnumOption(1f, "On"),
            new MaterialEditorEnumOption(2f, "Extra")
        };
        var consecutiveSelection =
            MaterialEditorFloatBackedValuePolicy.ResolveEnumSelection(
                consecutive,
                new[] { 1f, 1f });
        Equal(
            MaterialEditorEnumValueState.Matched,
            consecutiveSelection.State,
            "consecutive enum selection state");
        Equal(1, consecutiveSelection.OptionIndex, "consecutive enum option index");
        Equal(1f, consecutive[consecutiveSelection.OptionIndex].Value,
            "consecutive enum declared value");

        var declared = new List<MaterialEditorEnumOption>
        {
            new MaterialEditorEnumOption(-3f, "Off"),
            new MaterialEditorEnumOption(0.25f, "Low"),
            new MaterialEditorEnumOption(7.5f, "High")
        };
        var decimalSelection =
            MaterialEditorFloatBackedValuePolicy.ResolveEnumSelection(
                declared,
                new[] { 0.25f, 0.25f });
        Equal(
            MaterialEditorEnumValueState.Matched,
            decimalSelection.State,
            "non-consecutive decimal enum selection state");
        Equal(1, decimalSelection.OptionIndex,
            "non-consecutive decimal enum option index");
        Equal(0.25f, declared[decimalSelection.OptionIndex].Value,
            "dropdown writes the declared numeric value, not its index");

        var unmatched =
            MaterialEditorFloatBackedValuePolicy.ResolveEnumSelection(
                declared,
                new[] { 0.75f, 0.75f });
        Equal(
            MaterialEditorEnumValueState.Unmatched,
            unmatched.State,
            "unmatched enum value state");
        Equal(-1, unmatched.OptionIndex, "unmatched enum has no option index");
        Equal(0.75f, unmatched.CurrentValue,
            "unmatched enum preserves the current float");

        var mixed = MaterialEditorFloatBackedValuePolicy.ResolveEnumSelection(
            declared,
            new[] { 0.25f, 7.5f });
        Equal(
            MaterialEditorEnumValueState.Mixed,
            mixed.State,
            "multiple materials preserve mixed enum state");
        Equal(-1, mixed.OptionIndex, "mixed enum has no option index");
        False(
            MaterialEditorFloatBackedValuePolicy.ShouldRemoveEnumOverride(
                true,
                0.25f,
                0.25f),
            "an explicit selection from Mixed remains persisted even when it matches the representative original");
        True(
            MaterialEditorFloatBackedValuePolicy.ShouldRemoveEnumOverride(
                false,
                0.25f,
                0.25f),
            "a non-mixed value matching the original removes a redundant override");
    }

    private static void ExplicitMixedEnumSelectionRecreatesThePersistedOverride()
    {
        const float original = 0.25f;
        var overrideExists = true;
        var storedValue = 7.5f;
        var calls = new List<string>();

        Action removeOverride = () =>
        {
            calls.Add("remove");
            overrideExists = false;
        };
        Action<float> legacySetOverride = value =>
        {
            calls.Add("set");
            if (overrideExists &&
                MaterialEditorFloatBackedValuePolicy.Approximately(value, original))
            {
                overrideExists = false;
                return;
            }

            overrideExists = true;
            storedValue = value;
        };

        // A direct legacy set demonstrates the cleanup behavior that used to
        // lose an explicit Mixed -> original selection on the next load.
        legacySetOverride(original);
        False(overrideExists, "legacy backend removes an existing original-valued override");

        overrideExists = true;
        calls.Clear();
        MaterialEditorFloatBackedValuePolicy.PersistExplicitEnumSelection(
            removeOverride,
            legacySetOverride,
            original);

        True(overrideExists, "explicit Mixed selection remains persisted");
        Equal(original, storedValue, "persisted enum keeps the selected declared value");
        Equal("remove", calls[0], "existing override is removed first");
        Equal("set", calls[1], "selected value is persisted after removal");
        Equal(2, calls.Count, "force-persist sequence performs exactly two operations");
    }

    private static void BooleanValuesRoundTripAsFloatZeroOrOne()
    {
        False(
            MaterialEditorFloatBackedValuePolicy.GetBooleanDisplayValue(0f, false),
            "zero reads as disabled");
        True(
            MaterialEditorFloatBackedValuePolicy.GetBooleanDisplayValue(1f, false),
            "one reads as enabled");
        True(
            MaterialEditorFloatBackedValuePolicy.GetBooleanDisplayValue(-2f, false),
            "legacy non-zero Float reads as enabled without being rewritten");
        True(
            MaterialEditorFloatBackedValuePolicy.GetBooleanDisplayValue(0f, true),
            "Invert reverses the displayed state");

        Equal(
            0f,
            MaterialEditorFloatBackedValuePolicy.GetBooleanStoredValue(false, false),
            "disabled Boolean stores Float zero");
        Equal(
            1f,
            MaterialEditorFloatBackedValuePolicy.GetBooleanStoredValue(true, false),
            "enabled Boolean stores Float one");
        Equal(
            0f,
            MaterialEditorFloatBackedValuePolicy.GetBooleanStoredValue(true, true),
            "inverted enabled Boolean stores Float zero");
        Equal(
            1f,
            MaterialEditorFloatBackedValuePolicy.GetBooleanStoredValue(false, true),
            "inverted disabled Boolean stores Float one");

        foreach (var displayValue in new[] { false, true })
        {
            var stored = MaterialEditorFloatBackedValuePolicy.GetBooleanStoredValue(
                displayValue,
                false);
            Equal(
                displayValue,
                MaterialEditorFloatBackedValuePolicy.GetBooleanDisplayValue(
                    stored,
                    false),
                "Boolean Float round-trip " + displayValue);
        }
    }

    private static void ShowIfSupportsTheDocumentedFormsAndFailsOpen()
    {
        Condition(
            "Enabled",
            "Enabled",
            MaterialEditorConditionComparison.NotEqual,
            0f,
            1f,
            true);
        Condition(
            "!Enabled",
            "Enabled",
            MaterialEditorConditionComparison.Equal,
            0f,
            0f,
            true);
        Condition(
            "_Mode == true",
            "Mode",
            MaterialEditorConditionComparison.Equal,
            1f,
            1f,
            true);
        Condition(
            "Mode != false",
            "Mode",
            MaterialEditorConditionComparison.NotEqual,
            0f,
            1f,
            true);
        Condition(
            "Strength > 0.5",
            "Strength",
            MaterialEditorConditionComparison.GreaterThan,
            0.5f,
            0.25f,
            false);
        Condition(
            "Strength >= 0.5",
            "Strength",
            MaterialEditorConditionComparison.GreaterThanOrEqual,
            0.5f,
            0.5f,
            true);
        Condition(
            "Strength < 0.5",
            "Strength",
            MaterialEditorConditionComparison.LessThan,
            0.5f,
            0.25f,
            true);
        Condition(
            "Strength <= 0.5",
            "Strength",
            MaterialEditorConditionComparison.LessThanOrEqual,
            0.5f,
            0.75f,
            false);

        MaterialEditorPropertyCondition parsed;
        True(
            ShaderPropertyMetadataParser.TryParseCondition(
                "Enabled == 1",
                out parsed),
            "condition used by fail-open policy");
        False(
            MaterialEditorConditionPolicy.Evaluate(parsed, _ => 0f),
            "resolved false condition");
        True(
            MaterialEditorConditionPolicy.Evaluate(parsed, _ => null),
            "missing source fails open");
        True(
            MaterialEditorConditionPolicy.Evaluate(
                parsed,
                _ => throw new InvalidOperationException("unavailable material")),
            "resolver failure fails open");

        var warnings = new List<string>();
        var invalid = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Dependent\" "
                + "ShowIf=\"Enabled == not-a-number\" />"),
            warnings.Add);
        Equal(null, invalid.ShowIf, "invalid ShowIf is discarded");
        Equal(1, warnings.Count, "invalid ShowIf warning count");
        True(
            MaterialEditorConditionPolicy.Evaluate(invalid.ShowIf, _ => 0f),
            "discarded condition remains visible");
    }

    private static XmlElement Element(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document.DocumentElement
            ?? throw new InvalidOperationException("XML has no root element.");
    }

    private static void Alias(
        string declaredType,
        string expectedType,
        string expectedEditor)
    {
        True(
            ShaderPropertyMetadataParser.TryResolvePropertyTypeAlias(
                declaredType,
                2,
                out var normalizedType,
                out var editorId),
            declaredType + " schema 2 alias");
        Equal(expectedType, normalizedType, declaredType + " backing type");
        Equal(expectedEditor, editorId, declaredType + " editor id");
    }

    private static void NoAlias(string declaredType, int schemaVersion, string name)
    {
        False(
            ShaderPropertyMetadataParser.TryResolvePropertyTypeAlias(
                declaredType,
                schemaVersion,
                out var normalizedType,
                out var editorId),
            name);
        Equal(null, normalizedType, name + " normalized type");
        Equal(null, editorId, name + " editor id");
    }

    private static void Option(
        MaterialEditorEnumOption option,
        float expectedValue,
        string expectedDisplayName,
        string name)
    {
        Equal(expectedValue, option.Value, name + " value");
        Equal(expectedDisplayName, option.DisplayName, name + " display name");
    }

    private static void Condition(
        string expression,
        string expectedProperty,
        MaterialEditorConditionComparison expectedComparison,
        float expectedValue,
        float currentValue,
        bool expectedResult)
    {
        True(
            ShaderPropertyMetadataParser.TryParseCondition(
                expression,
                out var condition),
            expression + " parses");
        Equal(expectedProperty, condition.PropertyName, expression + " source");
        Equal(expectedComparison, condition.Comparison, expression + " comparison");
        Equal(expectedValue, condition.Value, expression + " value");
        Equal(expectedResult, condition.Evaluate(currentValue), expression + " result");
    }

    private static void NotNull(object value, string name)
    {
        if (value == null)
            throw new InvalidOperationException(name + ": expected a value.");
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
