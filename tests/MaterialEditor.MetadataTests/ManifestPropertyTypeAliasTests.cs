using MaterialEditorAPI;
using System.Xml;
using static MaterialEditorAPI.MaterialAPI;

internal static class ManifestPropertyTypeAliasTests
{
    internal static void Run()
    {
        BooleanIsCanonicalAndToggleIsRejected();
        CubemapIsARealPropertyTypeInBothSchemas();
        TextureAndCubemapEditorsRemainTypeSpecific();
        DropdownAliasesUseFloatPersistence();
        ExplicitEditorTakesPrecedence();
        InvalidExplicitEditorUsesFloatFallback();
        DropdownWithoutOptionsFallsBackSafely();
        SchemaOneDoesNotEnableAliases();
        RemovedToggleEditorSyntaxFallsBackSafely();
        ExistingEnumEditorSyntaxRemainsValid();
        Console.WriteLine("Schema 2 Boolean/Enum/Dropdown type-alias regression tests passed.");
    }

    private static void BooleanIsCanonicalAndToggleIsRejected()
    {
        var warnings = new List<string>();
        var property = Parse(
            "<Property Name=\"UseDetail\" Type=\"Boolean\" OffValue=\"-2\" OnValue=\"3.5\" />",
            warnings);

        Equal(ShaderPropertyType.Float, property.Type, "Boolean storage type");
        Equal(MaterialEditorPropertyEditorIds.Toggle, property.EditorId, "Boolean editor");
        Equal(0f, property.OffValue, "Boolean fixed off value");
        Equal(1f, property.OnValue, "Boolean fixed on value");
        Equal(
            true,
            warnings.Any(message => message.Contains("fixed Boolean values", StringComparison.Ordinal)),
            "Boolean legacy values warning");

        var inverted = Parse(
            "<Property Name=\"Inverted\" Type=\"Boolean\" Invert=\"true\" />");
        Equal(1f, inverted.OffValue, "inverted Boolean off value");
        Equal(0f, inverted.OnValue, "inverted Boolean on value");

        var normalized = Parse(
            "<Property Name=\"Normalized\" Type=\"  boolean  \" />");
        Equal(ShaderPropertyType.Float, normalized.Type, "normalized Boolean storage type");
        Equal(MaterialEditorPropertyEditorIds.Toggle, normalized.EditorId, "normalized Boolean editor");

        foreach (var removedType in new[] { "Toggle", "toggle", " Toggle " })
        {
            MaterialEditorPluginBase.ShaderPropertyData removedProperty;
            warnings.Clear();
            var parsed = MaterialEditorPluginBase.ShaderPropertyData.TryParse(
                Element("<Property Name=\"Removed\" Type=\"" + removedType + "\" />"),
                warnings.Add,
                out removedProperty,
                2);
            Equal(false, parsed, removedType + " type is rejected");
            Equal(null, removedProperty, removedType + " rejected result");
            Equal(
                true,
                warnings.Any(message => message.Contains("unknown Type", StringComparison.Ordinal)),
                removedType + " rejection warning");
        }
    }

    private static void CubemapIsARealPropertyTypeInBothSchemas()
    {
        foreach (var testCase in new[]
                 {
                     new { Type = "Cubemap", SchemaVersion = 1 },
                     new { Type = "cubemap", SchemaVersion = 2 }
                 })
        {
            MaterialEditorPluginBase.ShaderPropertyData property;
            var warnings = new List<string>();
            var parsed = MaterialEditorPluginBase.ShaderPropertyData.TryParse(
                Element("<Property Name=\"Environment\" Type=\"" + testCase.Type + "\" />"),
                warnings.Add,
                out property,
                testCase.SchemaVersion);

            Equal(true, parsed, testCase.Type + " real property type parse");
            Equal(ShaderPropertyType.Cubemap, property.Type, testCase.Type + " property type");
            Equal(0, warnings.Count, testCase.Type + " warning count");
        }
    }

    private static void TextureAndCubemapEditorsRemainTypeSpecific()
    {
        var cubemap = Parse(
            "<Property Name=\"Environment\" Type=\"Cubemap\" Editor=\"Cubemap\" />");
        Equal(
            MaterialEditorPropertyEditorIds.Cubemap,
            cubemap.EditorId,
            "Cubemap uses its dedicated editor");

        var warnings = new List<string>();
        cubemap = Parse(
            "<Property Name=\"Environment\" Type=\"Cubemap\" Editor=\"Texture\" />",
            warnings);
        Equal(null, cubemap.EditorId, "Texture editor is rejected for Cubemap");
        Equal(1, warnings.Count, "Cubemap/Texture mismatch warning count");

        warnings.Clear();
        var texture = Parse(
            "<Property Name=\"MainTex\" Type=\"Texture\" Editor=\"Cubemap\" />",
            warnings);
        Equal(null, texture.EditorId, "Cubemap editor is rejected for Texture");
        Equal(1, warnings.Count, "Texture/Cubemap mismatch warning count");
    }

    private static void DropdownAliasesUseFloatPersistence()
    {
        foreach (var alias in new[] { "Dropdown", "Enum", "dropdown" })
        {
            var property = Parse(
                "<Property Name=\"BlendMode\" Type=\"" + alias + "\">"
                + "<Option Value=\"0\" DisplayName=\"Opaque\" />"
                + "<Option Value=\"1\" DisplayName=\"Cutout\" />"
                + "</Property>");

            Equal(ShaderPropertyType.Float, property.Type, alias + " storage type");
            Equal(MaterialEditorPropertyEditorIds.Enum, property.EditorId, alias + " editor");
            Equal(2, property.EnumOptions.Count, alias + " option count");
            Equal("Cutout", property.EnumOptions[1].DisplayName, alias + " option label");
        }
    }

    private static void ExplicitEditorTakesPrecedence()
    {
        var booleanAsEnum = Parse(
            "<Property Name=\"Mode\" Type=\"Boolean\" Editor=\"Enum\" "
            + "Enums=\"Off,0,On,1\" />");
        Equal(MaterialEditorPropertyEditorIds.Enum, booleanAsEnum.EditorId, "explicit Enum editor");

        var enumAsFloat = Parse(
            "<Property Name=\"Mode\" Type=\"Enum\" Editor=\"Float\" "
            + "Enums=\"Off,0,On,1\" />");
        Equal(MaterialEditorPropertyEditorIds.Float, enumAsFloat.EditorId, "explicit Float editor");
    }

    private static void InvalidExplicitEditorUsesFloatFallback()
    {
        var warnings = new List<string>();
        var property = Parse(
            "<Property Name=\"Invalid\" Type=\"Boolean\" Editor=\"Mystery\" />",
            warnings);

        Equal(ShaderPropertyType.Float, property.Type, "invalid explicit editor storage type");
        Equal(null, property.EditorId, "invalid explicit editor fallback");
        Equal(1, warnings.Count, "invalid explicit editor warning count");
        Equal(
            true,
            warnings[0].Contains("unknown Editor", StringComparison.Ordinal),
            "invalid explicit editor warning");
    }

    private static void DropdownWithoutOptionsFallsBackSafely()
    {
        var warnings = new List<string>();
        var property = Parse(
            "<Property Name=\"Empty\" Type=\"Dropdown\" />",
            warnings);

        Equal(ShaderPropertyType.Float, property.Type, "empty Dropdown storage type");
        Equal(null, property.EditorId, "empty Dropdown fallback editor");
        Equal(2, warnings.Count, "empty Dropdown warning count");
        Equal(
            true,
            warnings.Any(message =>
                message.Contains("legacy Type 'Dropdown'", StringComparison.Ordinal)),
            "empty Dropdown compatibility warning");
        Equal(
            true,
            warnings.Any(message =>
                message.Contains(
                    "without a valid Enums attribute or legacy Option elements",
                    StringComparison.Ordinal)),
            "empty Dropdown fallback warning");
    }

    private static void SchemaOneDoesNotEnableAliases()
    {
        foreach (var alias in new[] { "Boolean", "Toggle", "Dropdown", "Enum" })
        {
            MaterialEditorPluginBase.ShaderPropertyData property;
            var warnings = new List<string>();
            var parsed = MaterialEditorPluginBase.ShaderPropertyData.TryParse(
                Element("<Property Name=\"Legacy\" Type=\"" + alias + "\" />"),
                warnings.Add,
                out property,
                1);

            Equal(false, parsed, "schema 1 rejects " + alias);
            Equal(null, property, "schema 1 result for " + alias);
            Equal(
                true,
                warnings.Any(message => message.Contains("unknown Type", StringComparison.Ordinal)),
                "schema 1 warning for " + alias);
        }
    }

    private static void RemovedToggleEditorSyntaxFallsBackSafely()
    {
        foreach (var removedEditor in new[] { "Toggle", "ToggleFloat", "materialeditor.toggle" })
        {
            var warnings = new List<string>();
            var property = Parse(
                "<Property Name=\"Removed\" Type=\"Float\" Editor=\""
                + removedEditor
                + "\" />",
                warnings);
            Equal(ShaderPropertyType.Float, property.Type, removedEditor + " backing type");
            Equal(null, property.EditorId, removedEditor + " falls back to Float editor");
            Equal(1, warnings.Count, removedEditor + " warning count");
            Equal(
                true,
                warnings[0].Contains("unknown Editor", StringComparison.Ordinal),
                removedEditor + " warning");
        }
    }

    private static void ExistingEnumEditorSyntaxRemainsValid()
    {
        var dropdown = Parse(
            "<Property Name=\"Dropdown\" Type=\"Float\" Editor=\"Enum\">"
            + "<Option Value=\"0\" DisplayName=\"Zero\" />"
            + "</Property>");
        Equal(ShaderPropertyType.Float, dropdown.Type, "legacy Dropdown Float storage type");
        Equal(MaterialEditorPropertyEditorIds.Enum, dropdown.EditorId, "legacy Dropdown Float editor");
    }

    private static MaterialEditorPluginBase.ShaderPropertyData Parse(
        string xml,
        ICollection<string> warnings = null)
    {
        MaterialEditorPluginBase.ShaderPropertyData property;
        var parsed = MaterialEditorPluginBase.ShaderPropertyData.TryParse(
            Element(xml),
            warnings == null ? null : warnings.Add,
            out property,
            2);
        Equal(true, parsed, "schema 2 alias parse");
        return property;
    }

    private static XmlElement Element(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document.DocumentElement;
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected " + expected + ", actual " + actual + ".");
    }
}
