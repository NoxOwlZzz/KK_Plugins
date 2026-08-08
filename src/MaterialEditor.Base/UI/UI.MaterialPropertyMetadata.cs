using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorPropertyUiLevel
    {
        Basic,
        Advanced
    }

    internal enum MaterialEditorConditionComparison
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    internal sealed class MaterialEditorPropertyCondition
    {
        internal MaterialEditorPropertyCondition(
            string propertyName,
            MaterialEditorConditionComparison comparison,
            float value)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("A condition property name is required.", nameof(propertyName));

            PropertyName = NormalizePropertyName(propertyName);
            Comparison = comparison;
            Value = value;
        }

        internal string PropertyName { get; }
        internal MaterialEditorConditionComparison Comparison { get; }
        internal float Value { get; }

        internal bool Evaluate(float currentValue)
        {
            switch (Comparison)
            {
                case MaterialEditorConditionComparison.Equal:
                    return currentValue == Value;
                case MaterialEditorConditionComparison.NotEqual:
                    return currentValue != Value;
                case MaterialEditorConditionComparison.GreaterThan:
                    return currentValue > Value;
                case MaterialEditorConditionComparison.GreaterThanOrEqual:
                    return currentValue >= Value;
                case MaterialEditorConditionComparison.LessThan:
                    return currentValue < Value;
                case MaterialEditorConditionComparison.LessThanOrEqual:
                    return currentValue <= Value;
                default:
                    return true;
            }
        }

        private static string NormalizePropertyName(string propertyName)
        {
            var normalized = propertyName.Trim();
            while (normalized.StartsWith("_", StringComparison.Ordinal))
                normalized = normalized.Substring(1);
            if (normalized.Length == 0)
                throw new ArgumentException("A condition property name is required.", nameof(propertyName));

            foreach (var character in normalized)
            {
                if (!char.IsLetterOrDigit(character)
                    && character != '_'
                    && character != '.')
                {
                    throw new ArgumentException(
                        "A condition property name contains unsupported characters.",
                        nameof(propertyName));
                }
            }

            return normalized;
        }
    }

    internal sealed class MaterialEditorEnumOption
    {
        internal MaterialEditorEnumOption(float value, string displayName)
        {
            Value = value;
            DisplayName = string.IsNullOrEmpty(displayName)
                ? value.ToString(CultureInfo.InvariantCulture)
                : displayName;
        }

        internal float Value { get; }
        internal string DisplayName { get; }
    }

    internal static class ShaderPropertyEditorIds
    {
        internal const string Enum = "materialeditor.enum";
        internal const string Toggle = "materialeditor.toggle";
    }

    internal sealed class ShaderPropertyUiMetadata
    {
        internal string DisplayName;
        internal int? Order;
        internal int? CategoryOrder;
        internal string EditorId;
        internal MaterialEditorPropertyUiLevel UiLevel = MaterialEditorPropertyUiLevel.Basic;
        internal MaterialEditorPropertyCondition ShowIf;
        internal readonly List<MaterialEditorEnumOption> EnumOptions =
            new List<MaterialEditorEnumOption>();
        internal float OffValue;
        internal float OnValue = 1f;
    }

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
                || version < 1
                || version > CurrentSchemaVersion)
            {
                Warn(
                    warning,
                    "Unknown MaterialEditor SchemaVersion '" + raw
                    + "'; using schema 1 compatibility mode.");
                return 1;
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
                DisplayName = ReadAttribute(propertyElement, "DisplayName")
            };
            var context = GetPropertyContext(propertyElement);

            ParseOrder(propertyElement, metadata, context, warning);
            ParseCategoryOrder(propertyElement, metadata, context, warning);
            ParseUiLevel(propertyElement, metadata, context, warning);
            metadata.EditorId = ParseEditor(propertyElement, context, warning);
            metadata.ShowIf = ParseConditionAttribute(
                propertyElement,
                "ShowIf",
                context,
                warning);
            metadata.OffValue = ParseFloatAttribute(
                propertyElement,
                "OffValue",
                0f,
                context,
                warning);
            metadata.OnValue = ParseFloatAttribute(
                propertyElement,
                "OnValue",
                1f,
                context,
                warning);
            ParseEnumOptions(propertyElement, metadata, context, warning);

            if (metadata.EditorId == ShaderPropertyEditorIds.Enum
                && metadata.EnumOptions.Count == 0)
            {
                Warn(
                    warning,
                    context
                    + " declares the Enum editor without any valid Option elements; "
                    + "using its type editor instead.");
                metadata.EditorId = null;
            }

            return metadata;
        }

        internal static bool TryResolvePropertyTypeAlias(
            string declaredType,
            int schemaVersion,
            out string normalizedType,
            out string editorId)
        {
            normalizedType = null;
            editorId = null;
            if (schemaVersion != CurrentSchemaVersion
                || string.IsNullOrEmpty(declaredType)
                || declaredType.Trim().Length == 0)
            {
                return false;
            }

            var value = declaredType.Trim();
            if (string.Equals(value, "Toggle", StringComparison.OrdinalIgnoreCase))
            {
                normalizedType = "Float";
                editorId = ShaderPropertyEditorIds.Toggle;
                return true;
            }

            if (string.Equals(value, "Dropdown", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Enum", StringComparison.OrdinalIgnoreCase))
            {
                normalizedType = "Float";
                editorId = ShaderPropertyEditorIds.Enum;
                return true;
            }

            return false;
        }

        internal static bool TryParseCondition(
            string expression,
            out MaterialEditorPropertyCondition condition,
            Action<string> warning = null)
        {
            condition = null;
            if (string.IsNullOrEmpty(expression)
                || expression.Trim().Length == 0)
            {
                Warn(warning, "Condition is empty.");
                return false;
            }

            var text = expression.Trim();
            if (text[0] == '!')
            {
                return TryCreateCondition(
                    text.Substring(1).Trim(),
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
                Warn(
                    warning,
                    "Condition '" + expression + "' has an invalid expected value.");
                return false;
            }

            MaterialEditorConditionComparison comparison;
            if (!TryMapComparison(operatorText, out comparison))
            {
                Warn(
                    warning,
                    "Condition '" + expression + "' has an unsupported comparison.");
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
                Warn(
                    warning,
                    context + " has invalid CategoryOrder '" + raw
                    + "'; category declaration order will be used.");
        }

        private static void ParseUiLevel(
            XmlElement element,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, "UiLevel");
            if (string.IsNullOrEmpty(raw)
                || string.Equals(raw, "Basic", StringComparison.OrdinalIgnoreCase))
            {
                metadata.UiLevel = MaterialEditorPropertyUiLevel.Basic;
                return;
            }

            if (string.Equals(raw, "Advanced", StringComparison.OrdinalIgnoreCase))
            {
                metadata.UiLevel = MaterialEditorPropertyUiLevel.Advanced;
                return;
            }

            Warn(
                warning,
                context + " has unknown UiLevel '" + raw + "'; Basic will be used.");
            metadata.UiLevel = MaterialEditorPropertyUiLevel.Basic;
        }

        private static string ParseEditor(
            XmlElement element,
            string context,
            Action<string> warning)
        {
            var raw = ReadAttribute(element, "Editor");
            if (string.IsNullOrEmpty(raw))
                return null;

            if (EqualsAny(raw, "Enum", ShaderPropertyEditorIds.Enum))
                return ShaderPropertyEditorIds.Enum;
            if (EqualsAny(
                    raw,
                    "Toggle",
                    "ToggleFloat",
                    ShaderPropertyEditorIds.Toggle))
            {
                return ShaderPropertyEditorIds.Toggle;
            }

            Warn(
                warning,
                context + " has unknown Editor '" + raw
                + "'; its type editor will be used.");
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
                    message => Warn(
                        warning,
                        context + " has invalid " + attributeName + ": " + message)))
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

            Warn(
                warning,
                context + " has invalid " + attributeName + " '" + raw + "'; "
                + fallback.ToString(CultureInfo.InvariantCulture) + " will be used.");
            return fallback;
        }

        private static void ParseEnumOptions(
            XmlElement element,
            ShaderPropertyUiMetadata metadata,
            string context,
            Action<string> warning)
        {
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
                    Warn(
                        warning,
                        context + " contains an Option with invalid Value '"
                        + rawValue + "'; it was ignored.");
                    continue;
                }

                if (!values.Add(value))
                {
                    Warn(
                        warning,
                        context + " contains duplicate enum option value '"
                        + rawValue + "'; the duplicate was ignored.");
                    continue;
                }

                var displayName = ReadAttribute(optionElement, "DisplayName");
                if (string.IsNullOrEmpty(displayName))
                    displayName = ReadAttribute(optionElement, "Label");
                if (string.IsNullOrEmpty(displayName))
                    displayName = ReadAttribute(optionElement, "Name");
                if (string.IsNullOrEmpty(displayName))
                    displayName = (optionElement.InnerText ?? string.Empty).Trim();

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
                condition = new MaterialEditorPropertyCondition(
                    propertyName,
                    comparison,
                    expected);
                return true;
            }
            catch (ArgumentException exception)
            {
                Warn(warning, exception.Message);
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

        private static bool EqualsAny(string value, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

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

    internal static class MaterialEditorConditionPolicy
    {
        internal static bool Evaluate(
            MaterialEditorPropertyCondition condition,
            Func<string, float?> resolveValue,
            bool fallback = true)
        {
            if (condition == null)
                return true;
            if (resolveValue == null)
                return fallback;

            try
            {
                var value = resolveValue(condition.PropertyName);
                return value.HasValue ? condition.Evaluate(value.Value) : fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
