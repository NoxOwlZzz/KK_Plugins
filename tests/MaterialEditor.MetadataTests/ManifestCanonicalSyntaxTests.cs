using MaterialEditorAPI;
using System.Xml;
using static MaterialEditorAPI.MaterialAPI;

internal static class ManifestCanonicalSyntaxTests
{
    internal static void Run()
    {
        EnumsUsesInvariantLabelValuePairs();
        InvertUsesFixedBooleanValues();
        EnumsTakesPrecedenceOverLegacyOptions();
        EmptyCanonicalAttributesStillSuppressLegacyValues();
        LegacyOptionsRemainACompatibilityFallback();
        DropdownRemainsCompatibleWithAnExplicitWarning();
    }

    private static void EnumsUsesInvariantLabelValuePairs()
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
                    "<Property Name=\"Mode\" Type=\"Float\" Editor=\"Enum\" "
                    + "Enums=\"Opaque,0,Cutout,1,Fade,1.5\" />"),
                warnings.Add);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }

        Equal(MaterialEditorPropertyEditorIds.Enum, metadata.EditorId, "canonical enum editor");
        Equal(3, metadata.EnumOptions.Count, "canonical enum option count");
        Option(metadata.EnumOptions[0], 0f, "Opaque", "first canonical option");
        Option(metadata.EnumOptions[1], 1f, "Cutout", "second canonical option");
        Option(metadata.EnumOptions[2], 1.5f, "Fade", "invariant fractional option");
        Equal(0, warnings.Count, "canonical enum warning count");
    }

    private static void InvertUsesFixedBooleanValues()
    {
        var inverted = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Enabled\" Type=\"Boolean\" "
                + "Invert=\"true\" OffValue=\"9\" OnValue=\"10\" />"));
        Equal(true, inverted.Invert, "Invert flag");
        Equal(1f, inverted.OffValue, "inverted off value is fixed");
        Equal(0f, inverted.OnValue, "inverted on value is fixed");

        var normal = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Enabled\" Type=\"Boolean\" "
                + "Invert=\"false\" OffValue=\"9\" OnValue=\"10\" />"));
        Equal(false, normal.Invert, "normal Invert flag");
        Equal(0f, normal.OffValue, "normal off value is fixed");
        Equal(1f, normal.OnValue, "normal on value is fixed");
    }

    private static void EnumsTakesPrecedenceOverLegacyOptions()
    {
        var metadata = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Mode\" Editor=\"Enum\" Enums=\"Canonical,7\">"
                + "<Option Value=\"99\" DisplayName=\"Legacy\" />"
                + "</Property>"));

        Equal(1, metadata.EnumOptions.Count, "Enums suppresses legacy Option projection");
        Option(metadata.EnumOptions[0], 7f, "Canonical", "canonical option wins");
    }

    private static void LegacyOptionsRemainACompatibilityFallback()
    {
        var metadata = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Mode\" Editor=\"Enum\">"
                + "<Option Value=\"0\" DisplayName=\"Off\" />"
                + "<Option Value=\"1\">On</Option>"
                + "</Property>"));

        Equal(MaterialEditorPropertyEditorIds.Enum, metadata.EditorId, "legacy fallback editor");
        Equal(2, metadata.EnumOptions.Count, "legacy fallback option count");
        Option(metadata.EnumOptions[0], 0f, "Off", "legacy attribute label");
        Option(metadata.EnumOptions[1], 1f, "On", "legacy inner-text label");
    }

    private static void EmptyCanonicalAttributesStillSuppressLegacyValues()
    {
        var warnings = new List<string>();
        var enumMetadata = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Mode\" Editor=\"Enum\" Enums=\"\">"
                + "<Option Value=\"99\" DisplayName=\"Legacy\" />"
                + "</Property>"),
            warnings.Add);
        Equal(null, enumMetadata.EditorId, "empty Enums does not revive Option");
        Equal(0, enumMetadata.EnumOptions.Count, "empty Enums option count");

        var booleanMetadata = ShaderPropertyMetadataParser.Parse(
            Element(
                "<Property Name=\"Enabled\" Type=\"Boolean\" Invert=\"\" "
                + "OffValue=\"9\" OnValue=\"10\" />"));
        Equal(0f, booleanMetadata.OffValue, "empty Invert suppresses legacy off");
        Equal(1f, booleanMetadata.OnValue, "empty Invert suppresses legacy on");
    }

    private static void DropdownRemainsCompatibleWithAnExplicitWarning()
    {
        var warnings = new List<string>();
        MaterialEditorPluginBase.ShaderPropertyData property;
        var parsed = MaterialEditorPluginBase.ShaderPropertyData.TryParse(
            Element(
                "<Property Name=\"BlendMode\" Type=\"Dropdown\" "
                + "Enums=\"Opaque,0,Cutout,1\" />"),
            warnings.Add,
            out property,
            2);

        Equal(true, parsed, "legacy Dropdown parses");
        Equal(ShaderPropertyType.Float, property.Type, "legacy Dropdown backing type");
        Equal(MaterialEditorPropertyEditorIds.Enum, property.EditorId, "legacy Dropdown editor");
        Equal(2, property.EnumOptions.Count, "legacy Dropdown canonical options");
        Equal(1, warnings.Count, "legacy Dropdown warning count");
        Equal(
            true,
            warnings[0].Contains("legacy Type 'Dropdown'", StringComparison.Ordinal)
            && warnings[0].Contains("Enums attribute", StringComparison.Ordinal),
            "legacy Dropdown warning recommends canonical syntax");
    }

    private static XmlElement Element(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document.DocumentElement
               ?? throw new InvalidOperationException("XML has no root element.");
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

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
