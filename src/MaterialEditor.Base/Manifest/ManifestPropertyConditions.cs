using System;

namespace MaterialEditorAPI
{
    /// <summary>Comparison performed by a property visibility condition.</summary>
    public enum MaterialEditorConditionComparison
    {
        /// <summary>The current value must equal the expected value.</summary>
        Equal,
        /// <summary>The current value must not equal the expected value.</summary>
        NotEqual,
        /// <summary>The current value must be greater than the expected value.</summary>
        GreaterThan,
        /// <summary>The current value must be greater than or equal to the expected value.</summary>
        GreaterThanOrEqual,
        /// <summary>The current value must be less than the expected value.</summary>
        LessThan,
        /// <summary>The current value must be less than or equal to the expected value.</summary>
        LessThanOrEqual
    }

    /// <summary>A simple numeric condition referencing another shader property.</summary>
    public sealed class MaterialEditorPropertyCondition
    {
        /// <summary>Create a property condition.</summary>
        public MaterialEditorPropertyCondition(
            string propertyName,
            MaterialEditorConditionComparison comparison,
            float value)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException(
                    "A condition property name is required.",
                    nameof(propertyName));

            PropertyName = NormalizePropertyName(propertyName);
            Comparison = comparison;
            Value = value;
        }

        /// <summary>Referenced shader property name, without a leading underscore.</summary>
        public string PropertyName { get; }
        /// <summary>Comparison to perform.</summary>
        public MaterialEditorConditionComparison Comparison { get; }
        /// <summary>Expected numeric value.</summary>
        public float Value { get; }

        /// <summary>Evaluate the condition against a current numeric value.</summary>
        public bool Evaluate(float currentValue)
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
            {
                throw new ArgumentException(
                    "A condition property name is required.",
                    nameof(propertyName));
            }
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
}
