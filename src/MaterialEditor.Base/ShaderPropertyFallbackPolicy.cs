using System;
using System.Collections.Generic;
using ShaderPropertyData = MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Tracks source provenance and reports semantic conflicts in the cross-shader
    /// fallback catalog. Entries use deterministic last-loaded-wins precedence.
    /// </summary>
    internal sealed class ShaderPropertyFallbackMergeState
    {
        private const string UnknownSource = "unknown source";

        private readonly Dictionary<string, string> _sourceByProperty =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _reportedProperties =
            new HashSet<string>(StringComparer.Ordinal);

        internal void Reset()
        {
            _sourceByProperty.Clear();
            _reportedProperties.Clear();
        }

        internal ShaderPropertyData MergeInto(
            IDictionary<string, ShaderPropertyData> fallbackProperties,
            ShaderPropertyData shaderSpecific,
            string sourceId,
            Action<string> warning = null)
        {
            if (fallbackProperties == null)
                throw new ArgumentNullException(nameof(fallbackProperties));
            if (shaderSpecific == null)
                throw new ArgumentNullException(nameof(shaderSpecific));

            var incomingSource = NormalizeSource(sourceId);
            var fallback = shaderSpecific.WithoutHierarchyForDefaultFallback();
            ShaderPropertyData previous;
            if (fallbackProperties.TryGetValue(fallback.Name, out previous))
            {
                var differences = GetSemanticDifferences(previous, fallback);
                if (differences.Count > 0
                    && _reportedProperties.Add(fallback.Name))
                {
                    string previousSource;
                    if (!_sourceByProperty.TryGetValue(
                            fallback.Name,
                            out previousSource))
                    {
                        previousSource = UnknownSource;
                    }

                    warning?.Invoke(
                        "Shared default property '" + fallback.Name
                        + "' has conflicting semantic metadata between '"
                        + previousSource + "' and '" + incomingSource
                        + "'. Historical last-loaded precedence is preserved; '"
                        + incomingSource + "' wins. Changed fields: "
                        + string.Join(", ", differences.ToArray()) + ".");
                }
            }

            // The most recently loaded definition is the active fallback.
            fallbackProperties[fallback.Name] = fallback;
            _sourceByProperty[fallback.Name] = incomingSource;
            return fallback;
        }

        private static List<string> GetSemanticDifferences(
            ShaderPropertyData previous,
            ShaderPropertyData incoming)
        {
            var differences = new List<string>();
            AddIfDifferent(differences, "Type", previous.Type, incoming.Type);
            AddIfDifferent(
                differences,
                "DefaultValue",
                previous.DefaultValue,
                incoming.DefaultValue);
            AddIfDifferent(
                differences,
                "DefaultValueAssetBundle",
                previous.DefaultValueAssetBundle,
                incoming.DefaultValueAssetBundle);
            AddIfDifferent(
                differences,
                "AnisoLevel",
                previous.AnisoLevel,
                incoming.AnisoLevel);
            AddIfDifferent(
                differences,
                "FilterMode",
                previous.FilterMode,
                incoming.FilterMode);
            AddIfDifferent(
                differences,
                "WrapMode",
                previous.WrapMode,
                incoming.WrapMode);
            AddIfDifferent(
                differences,
                "MinValue",
                previous.MinValue,
                incoming.MinValue);
            AddIfDifferent(
                differences,
                "MaxValue",
                previous.MaxValue,
                incoming.MaxValue);
            AddIfDifferent(
                differences,
                "Hidden",
                previous.Hidden,
                incoming.Hidden);
            AddIfDifferent(
                differences,
                "Category",
                previous.Category,
                incoming.Category);
            AddIfDifferent(
                differences,
                "DisplayName",
                previous.DisplayName,
                incoming.DisplayName);
            AddIfDifferent(
                differences,
                "HasExplicitDisplayName",
                previous.HasExplicitDisplayName,
                incoming.HasExplicitDisplayName);
            AddIfDifferent(
                differences,
                "Order",
                previous.Order,
                incoming.Order);
            AddIfDifferent(
                differences,
                "CategoryOrder",
                previous.CategoryOrder,
                incoming.CategoryOrder);
            AddIfDifferent(
                differences,
                "EditorId",
                previous.EditorId,
                incoming.EditorId);
            AddIfDifferent(
                differences,
                "TooltipText",
                previous.TooltipText,
                incoming.TooltipText);
            AddIfDifferent(
                differences,
                "Group",
                previous.Group,
                incoming.Group);
            if (!ConditionsEqual(previous.ShowIf, incoming.ShowIf))
                differences.Add("ShowIf");
            if (!EnumOptionsEqual(previous.EnumOptions, incoming.EnumOptions))
                differences.Add("EnumOptions");
            AddIfDifferent(
                differences,
                "VectorComponentCount",
                previous.VectorComponentCount,
                incoming.VectorComponentCount);
            AddIfDifferent(
                differences,
                "Invert",
                previous.Invert,
                incoming.Invert);
            AddIfDifferent(
                differences,
                "OffValue",
                previous.OffValue,
                incoming.OffValue);
            AddIfDifferent(
                differences,
                "OnValue",
                previous.OnValue,
                incoming.OnValue);
            return differences;
        }

        private static bool ConditionsEqual(
            MaterialEditorPropertyCondition left,
            MaterialEditorPropertyCondition right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            return string.Equals(
                       left.PropertyName,
                       right.PropertyName,
                       StringComparison.Ordinal)
                   && left.Comparison == right.Comparison
                   && left.Value == right.Value;
        }

        private static bool EnumOptionsEqual(
            IList<MaterialEditorEnumOption> left,
            IList<MaterialEditorEnumOption> right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Count != right.Count)
                return false;
            for (var index = 0; index < left.Count; index++)
            {
                var leftOption = left[index];
                var rightOption = right[index];
                if (ReferenceEquals(leftOption, rightOption))
                    continue;
                if (leftOption == null
                    || rightOption == null
                    || leftOption.Value != rightOption.Value
                    || !string.Equals(
                        leftOption.DisplayName,
                        rightOption.DisplayName,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        private static void AddIfDifferent<T>(
            ICollection<string> differences,
            string fieldName,
            T previous,
            T incoming)
        {
            if (!EqualityComparer<T>.Default.Equals(previous, incoming))
                differences.Add(fieldName);
        }

        private static string NormalizeSource(string sourceId)
        {
            return string.IsNullOrEmpty(sourceId) || sourceId.Trim().Length == 0
                ? UnknownSource
                : sourceId.Trim();
        }
    }
}
