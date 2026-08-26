using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace MaterialEditorAPI
{
    /// <summary>Safe parser for optional Material Editor manifest metadata.</summary>
    internal static class ShaderPropertyMetadataParser
    {
        private const int CurrentSchemaVersion = 2;

        internal static int ReadSchemaVersion(
            XmlElement materialEditorElement,
            Action<string> warning = null)
        {
            if (materialEditorElement == null)
                return 1;

            var raw = ReadAttribute(materialEditorElement, "SchemaVersion");
            if (string.IsNullOrEmpty(raw))
                return 1;

            int version;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out version)
                || version < 1)
            {
                Warn(warning, "Unknown MaterialEditor SchemaVersion '" + raw + "'; using schema 1 compatibility mode.");
                return 1;
            }

            if (version > CurrentSchemaVersion)
            {
                Warn(
                    warning,
                    "MaterialEditor SchemaVersion '" + raw
                    + "' is newer than the supported schema "
                    + CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)
                    + "; using the known schema "
                    + CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)
                    + " subset.");
                return CurrentSchemaVersion;
            }

            return version;
        }

        internal static ShaderPropertyUiMetadata Parse(
            XmlElement propertyElement,
            Action<string> warning = null)
        {
            if (propertyElement == null)
                throw new ArgumentNullException(nameof(propertyElement));
            var metadata = new ShaderPropertyUiMetadata
            {
                DisplayName = ReadAttribute(propertyElement, "DisplayName"),
                TooltipText = ReadAttribute(propertyElement, "Tooltip"),
                Group = ReadAttribute(propertyElement, "Group")
            };
            var context = GetPropertyContext(propertyElement);

            ParseOrder(propertyElement, metadata, context, warning);
            ParseCategoryOrder(propertyElement, metadata, context, warning);
            ParseVectorComponentCount(propertyElement, metadata, context, warning);
            metadata.EditorId = ParseEditor(propertyElement, metadata, context, warning);
            metadata.ShowIf = ParseConditionAttribute(propertyElement, "ShowIf", context, warning);
            if (propertyElement.HasAttribute("Invert"))
            {
                metadata.Invert = ParseBooleanAttribute(
                    propertyElement,
                    "Invert",
                    false,
                    context,
                    warning);
                metadata.OffValue = metadata.Invert ? 1f : 0f;
                metadata.OnValue = metadata.Invert ? 0f : 1f;
            }
            else
            {
                // Compatibility for local schema-2 manifests authored before
                // PR #401. New manifests should use Invert with fixed 0/1 values.
                metadata.OffValue = ParseFloatAttribute(
                    propertyElement, "OffValue", 0f, context, warning);
                metadata.OnValue = ParseFloatAttribute(
                    propertyElement, "OnValue", 1f, context, warning);
            }
            ParseEnumOptions(propertyElement, metadata, context, warning);

            if (metadata.EditorId == MaterialEditorPropertyEditorIds.Enum
                && metadata.EnumOptions.Count == 0)
            {
                Warn(warning, context + " declares the Enum editor without a valid Enums attribute or legacy Option elements; using its type editor instead.");
                metadata.EditorId = null;
            }

            return metadata;
        }

        internal static bool TryParseCondition(
            string expression,
            out MaterialEditorPropertyCondition condition,
            Action<string> warning = null)
        {
            condition = null;
            if (string.IsNullOrEmpty(expression) || expression.Trim().Length == 0)
            {
                Warn(warning, "Condition is empty.");
                return false;
            }

            var text = expression.Trim();
            if (text[0] == '!')
            {
                var negatedProperty = text.Substring(1).Trim();
                return TryCreateCondition(
                    negatedProperty,
                    MaterialEditorConditionComparison.Equal,
                    0f,
                    out condition,
                    warning);
            }

            string operatorText;
            int operatorIndex;
            if (!TryFindOperator(text, out operatorText, out operatorIndex))
            {
                return TryCreateCondition(
                    text,
                    MaterialEditorConditionComparison.NotEqual,
                    0f,
                    out condition,
                    warning);
            }

            var propertyName = text.Substring(0, operatorIndex).Trim();
            var expectedText = text.Substring(operatorIndex + operatorText.Length).Trim();
            float expected;
            if (!TryParseConditionValue(expectedText, out expected))
            {
                Warn(warning, "Condition '" + expression + "' has an invalid expected value.");
                return false;
            }

            MaterialEditorConditionComparison comparison;
            if (!TryMapComparison(operatorText, out comparison))
            {
                Warn(warning, "Condition '" + expression + "' has an unsupported comparison.");
                return false;
            }

            return TryCreateCondition(
                propertyName,
                comparison,
                expected,
                out condition,
                warning);
        }

        private static void ParseOrder(
            XmlElement element,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, "Order");
            if (string.IsNullOrEmpty(raw))
                return;

            int value;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                metadata.Order = value;
            else
                Warn(warning, context + " has invalid Order '" + raw + "'; declaration order will be used.");
        }

        private static void ParseCategoryOrder(
            XmlElement element,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, "CategoryOrder");
            if (string.IsNullOrEmpty(raw))
                return;

            int value;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                metadata.CategoryOrder = value;
            else
                Warn(warning, context + " has invalid CategoryOrder '" + raw + "'; category declaration order will be used.");
        }

        private static void ParseVectorComponentCount(
            XmlElement element,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, "VectorComponentCount");
            if (string.IsNullOrEmpty(raw))
                return;

            int value;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                && value >= 2
                && value <= 4)
            {
                metadata.VectorComponentCount = value;
                return;
            }

            Warn(warning, context + " has invalid VectorComponentCount '" + raw + "'; the editor default will be used.");
        }

        private static string ParseEditor(
            XmlElement element,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, "Editor");
            if (string.IsNullOrEmpty(raw))
                return null;

            string editorId;
            int? vectorComponentCount;
            if (ShaderPropertyEditorPolicy.TryNormalizeManifestEditor(
                    raw,
                    metadata.VectorComponentCount,
                    out editorId,
                    out vectorComponentCount))
            {
                metadata.VectorComponentCount = vectorComponentCount;
                return editorId;
            }

            Warn(warning, context + " has unknown Editor '" + raw + "'; its type editor will be used.");
            return null;
        }

        private static MaterialEditorPropertyCondition ParseConditionAttribute(
            XmlElement element,
            string attributeName,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, attributeName);
            if (string.IsNullOrEmpty(raw))
                return null;

            MaterialEditorPropertyCondition condition;
            if (TryParseCondition(
                    raw,
                    out condition,
                    message => Warn(warning, context + " has invalid " + attributeName + ": " + message)))
            {
                return condition;
            }
            return null;
        }

        private static float ParseFloatAttribute(
            XmlElement element,
            string attributeName,
            float fallback,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, attributeName);
            if (string.IsNullOrEmpty(raw))
                return fallback;

            float value;
            if (TryParseFiniteFloat(raw, out value))
                return value;

            Warn(warning, context + " has invalid " + attributeName + " '" + raw + "'; "
                          + fallback.ToString(CultureInfo.InvariantCulture) + " will be used.");
            return fallback;
        }

        private static bool ParseBooleanAttribute(
            XmlElement element,
            string attributeName,
            bool fallback,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, attributeName);
            if (string.IsNullOrEmpty(raw))
                return fallback;

            bool value;
            if (bool.TryParse(raw, out value))
                return value;

            Warn(warning, context + " has invalid " + attributeName + " '" + raw
                          + "'; " + fallback.ToString() + " will be used.");
            return fallback;
        }

        private static void ParseEnumOptions(
            XmlElement element,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, "Enums");
            if (element.HasAttribute("Enums"))
            {
                ParseEnumPairs(raw ?? string.Empty, metadata, context, warning);
                return;
            }

            // Compatibility for local schema-2 manifests authored before
            // PR #401. Enums takes precedence whenever it is present.
            var values = new HashSet<float>();
            foreach (XmlNode child in element.ChildNodes)
            {
                var optionElement = child as XmlElement;
                if (optionElement == null || optionElement.Name != "Option")
                    continue;

                var rawValue = ReadAttribute(optionElement, "Value");
                float value;
                if (!TryParseFiniteFloat(rawValue, out value))
                {
                    Warn(warning, context + " contains an Option with invalid Value '" + rawValue + "'; it was ignored.");
                    continue;
                }
                if (!values.Add(value))
                {
                    Warn(warning, context + " contains duplicate enum option value '" + rawValue + "'; the duplicate was ignored.");
                    continue;
                }

                var displayName = ReadAttribute(optionElement, "DisplayName");
                if (string.IsNullOrEmpty(displayName))
                    displayName = ReadAttribute(optionElement, "Label");
                if (string.IsNullOrEmpty(displayName))
                    displayName = ReadAttribute(optionElement, "Name");
                if (string.IsNullOrEmpty(displayName))
                    displayName = (optionElement.InnerText ?? string.Empty).Trim();

                metadata.EnumOptions.Add(new MaterialEditorEnumOption(value, displayName));
            }
        }

        private static void ParseEnumPairs(
            string raw,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
            var tokens = raw.Split(',');
            if (tokens.Length % 2 != 0)
            {
                Warn(warning, context + " has an invalid Enums list; labels and values "
                              + "must be declared in pairs. The unmatched final token was ignored.");
            }

            var values = new HashSet<float>();
            var labels = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index + 1 < tokens.Length; index += 2)
            {
                var displayName = tokens[index].Trim();
                var rawValue = tokens[index + 1].Trim();
                if (displayName.Length == 0)
                {
                    Warn(warning, context + " contains an enum option with an empty label; the option was ignored.");
                    continue;
                }

                float value;
                if (!TryParseFiniteFloat(rawValue, out value))
                {
                    Warn(warning, context + " contains an enum option with invalid value '"
                                  + rawValue + "'; it was ignored.");
                    continue;
                }
                if (!values.Add(value))
                {
                    Warn(warning, context + " contains duplicate enum option value '"
                                  + rawValue + "'; the duplicate was ignored.");
                    continue;
                }
                if (!labels.Add(displayName))
                {
                    values.Remove(value);
                    Warn(warning, context + " contains duplicate enum option label '"
                                  + displayName + "'; the duplicate was ignored.");
                    continue;
                }

                metadata.EnumOptions.Add(
                    new MaterialEditorEnumOption(value, displayName));
            }
        }

        private static bool TryFindOperator(
            string expression,
            out string operatorText,
            out int operatorIndex)
        {
            var operators = new[] { ">=", "<=", "!=", "==", ">", "<" };
            foreach (var candidate in operators)
            {
                var index = expression.IndexOf(candidate, StringComparison.Ordinal);
                if (index > 0)
                {
                    operatorText = candidate;
                    operatorIndex = index;
                    return true;
                }
            }

            operatorText = null;
            operatorIndex = -1;
            return false;
        }

        private static bool TryMapComparison(
            string operatorText,
            out MaterialEditorConditionComparison comparison)
        {
            switch (operatorText)
            {
                case "==":
                    comparison = MaterialEditorConditionComparison.Equal;
                    return true;
                case "!=":
                    comparison = MaterialEditorConditionComparison.NotEqual;
                    return true;
                case ">":
                    comparison = MaterialEditorConditionComparison.GreaterThan;
                    return true;
                case ">=":
                    comparison = MaterialEditorConditionComparison.GreaterThanOrEqual;
                    return true;
                case "<":
                    comparison = MaterialEditorConditionComparison.LessThan;
                    return true;
                case "<=":
                    comparison = MaterialEditorConditionComparison.LessThanOrEqual;
                    return true;
                default:
                    comparison = MaterialEditorConditionComparison.Equal;
                    return false;
            }
        }

        private static bool TryCreateCondition(
            string propertyName,
            MaterialEditorConditionComparison comparison,
            float expected,
            out MaterialEditorPropertyCondition condition,
            Action<string> warning)
        {
            condition = null;
            try
            {
                condition = new MaterialEditorPropertyCondition(propertyName, comparison, expected);
                return true;
            }
            catch (ArgumentException)
            {
                Warn(warning, "Condition property name is missing.");
                return false;
            }
        }

        private static bool TryParseConditionValue(string value, out float parsed)
        {
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                parsed = 1f;
                return true;
            }
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                parsed = 0f;
                return true;
            }
            return TryParseFiniteFloat(value, out parsed);
        }

        private static bool TryParseFiniteFloat(string value, out float parsed)
        {
            if (float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out parsed)
                && !float.IsNaN(parsed)
                && !float.IsInfinity(parsed))
            {
                return true;
            }

            parsed = 0f;
            return false;
        }

        private static string ReadAttribute(XmlElement element, string attributeName)
        {
            if (element == null || !element.HasAttribute(attributeName))
                return null;
            var value = element.GetAttribute(attributeName);
            return string.IsNullOrEmpty(value) ? null : value.Trim();
        }

        private static string GetPropertyContext(XmlElement element)
        {
            var name = ReadAttribute(element, "Name");
            return string.IsNullOrEmpty(name)
                ? "Shader property"
                : "Shader property '" + name + "'";
        }

        private static void Warn(Action<string> warning, string message)
        {
            warning?.Invoke(message);
        }
    }

}
