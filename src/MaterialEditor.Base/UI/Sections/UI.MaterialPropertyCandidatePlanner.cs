using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class ExtensionPropertyCandidate
    {
        internal MaterialEditorPropertyDescriptor Descriptor;
        internal ShaderPropertyType? BuiltInType;
        internal bool SearchOnlyHidden;
    }

    internal sealed class ManifestPropertyCandidate
    {
        internal ShaderPropertyData Definition;
        internal bool SearchOnlyHidden;
    }

    internal sealed class ManifestCategoryCandidateGroup
    {
        internal OrganizedPropertyCategory Category;
        internal List<ManifestPropertyCandidate> Candidates;
    }

    internal sealed class MaterialPropertyCandidatePlanner
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;

        internal MaterialPropertyCandidatePlanner(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
        }

        internal List<ManifestCategoryCandidateGroup> BuildManifestCandidates(
            MaterialSectionContext context,
            IEnumerable<OrganizedPropertyCategory> organizedCategories,
            IDictionary<string, MaterialConditionPropertyState>
                manifestConditionStates,
            bool hasSearch)
        {
            var viableCategories =
                new List<ManifestCategoryCandidateGroup>();
            foreach (var category in organizedCategories)
            {
                var candidates = new List<ManifestPropertyCandidate>();
                foreach (var definition in category.Properties)
                {
                    if (!IsManifestPropertyCompatible(context, definition)
                        || definition.Hidden)
                        continue;

                    var matchesSearch = MatchesPropertySearch(
                        context.PropertyFilter,
                        definition.DisplayName,
                        definition.Name);
                    if (!matchesSearch)
                        continue;

                    MaterialConditionPropertyState conditionState = null;
                    if (definition.ShowIf != null
                        && manifestConditionStates != null)
                    {
                        manifestConditionStates.TryGetValue(
                            definition.Name,
                            out conditionState);
                    }

                    var hiddenByShowIf = definition.ShowIf != null
                                         && conditionState != null
                                         && !conditionState.Visible;
                    if (hiddenByShowIf && !hasSearch)
                        continue;

                    candidates.Add(new ManifestPropertyCandidate
                    {
                        Definition = definition,
                        SearchOnlyHidden = hiddenByShowIf
                    });
                }

                if (candidates.Count == 0)
                    continue;

                viableCategories.Add(new ManifestCategoryCandidateGroup
                {
                    Category = category,
                    Candidates = candidates
                });
            }
            return viableCategories;
        }

        internal List<ExtensionPropertyCandidate> BuildExtensionCandidates(
            MaterialSectionContext sectionContext,
            IList<MaterialEditorPropertyDescriptor> extensionDescriptors,
            IDictionary<int, MaterialConditionPropertyState> conditionStates)
        {
            var candidates = new List<ExtensionPropertyCandidate>();
            for (var descriptorIndex = 0;
                 descriptorIndex < extensionDescriptors.Count;
                 descriptorIndex++)
            {
                var descriptor = extensionDescriptors[descriptorIndex];
                MaterialConditionPropertyState conditionState = null;
                if (descriptor.VisibilityCondition != null
                    && conditionStates != null)
                    conditionStates.TryGetValue(
                        descriptorIndex,
                        out conditionState);
                var propertyName = GetPropertyName(descriptor);
                if (!MaterialEditorExtensionRegistry.HasPropertyEditor(
                        descriptor.EditorId)
                    || _actions.IsPropertyBlacklisted(
                        sectionContext.MaterialName,
                        propertyName))
                    continue;

                ShaderPropertyType builtInType;
                var hasBuiltInType = ShaderPropertyEditorPolicy.TryGetBuiltInPropertyType(
                    descriptor.EditorId,
                    out builtInType);
                if (hasBuiltInType
                    && builtInType != ShaderPropertyType.Keyword
                    && !MaterialPropertyAccess.HasProperty(
                        sectionContext.Material,
                        MaterialPropertyIdCache.Get(propertyName)))
                    continue;
                var matchesSearch = MatchesPropertySearch(
                    sectionContext.PropertyFilter,
                    descriptor.DisplayName,
                    propertyName);
                var hiddenByShowIf = descriptor.VisibilityCondition != null
                                     && conditionState != null
                                     && !conditionState.Visible;
                if (hiddenByShowIf
                    && sectionContext.PropertyFilter.Count == 0)
                    continue;
                if (!matchesSearch)
                    continue;

                candidates.Add(new ExtensionPropertyCandidate
                {
                    Descriptor = descriptor,
                    BuiltInType = hasBuiltInType
                        ? (ShaderPropertyType?)builtInType
                        : null,
                    SearchOnlyHidden = hiddenByShowIf
                });
            }
            return candidates;
        }

        internal static void PrefixSearchPath(
            RowModel row,
            string categoryName,
            string subcategoryName)
        {
            var path = categoryName ?? string.Empty;
            if (!string.IsNullOrEmpty(subcategoryName))
                path = string.IsNullOrEmpty(path)
                    ? subcategoryName
                    : path + " › " + subcategoryName;
            if (string.IsNullOrEmpty(path)
                || path == PropertyOrganizer.UncategorizedName)
                return;
            row.LabelText = path + " › " + row.LabelText;
        }

        internal static void MarkSearchOnlyShowIfResult(
            RowModel row,
            MaterialEditorPropertyCondition condition)
        {
            var expression = FormatCondition(condition);
            var reason = "Inactive because ShowIf=\"" + expression
                         + "\" is not currently satisfied.";
            row.LabelText += "  [ShowIf: " + expression + "]";
            row.TooltipText = string.IsNullOrEmpty(row.TooltipText)
                ? reason
                : row.TooltipText + "\n\n" + reason;
            row.Enabled = false;
        }

        private static string FormatCondition(
            MaterialEditorPropertyCondition condition)
        {
            if (condition == null)
                return string.Empty;
            string operation;
            switch (condition.Comparison)
            {
                case MaterialEditorConditionComparison.Equal:
                    operation = "==";
                    break;
                case MaterialEditorConditionComparison.NotEqual:
                    operation = "!=";
                    break;
                case MaterialEditorConditionComparison.GreaterThan:
                    operation = ">";
                    break;
                case MaterialEditorConditionComparison.GreaterThanOrEqual:
                    operation = ">=";
                    break;
                case MaterialEditorConditionComparison.LessThan:
                    operation = "<";
                    break;
                case MaterialEditorConditionComparison.LessThanOrEqual:
                    operation = "<=";
                    break;
                default:
                    operation = "?";
                    break;
            }
            return condition.PropertyName + " " + operation + " "
                   + condition.Value.ToString(CultureInfo.InvariantCulture);
        }
        private bool IsManifestPropertyCompatible(
            MaterialSectionContext context,
            ShaderPropertyData definition)
        {
            return (definition.Type == ShaderPropertyType.Keyword
                    || MaterialPropertyAccess.HasProperty(
                        context.Material,
                        MaterialPropertyIdCache.Get(definition.Name)))
                   && !_actions.IsPropertyBlacklisted(
                       context.MaterialName,
                       definition.Name);
        }

        private bool IsReliablyModified(
            MaterialSectionContext context,
            string propertyName,
            ShaderPropertyType type)
        {
            var material = context.Material;
            var property = type == ShaderPropertyType.Keyword
                ? default(MaterialPropertyHandle)
                : MaterialPropertyIdCache.Get(propertyName);
            switch (type)
            {
                case ShaderPropertyType.Float:
                    var originalFloat = _editService.GetMaterialFloatPropertyValueOriginal(
                        context.Data, material, propertyName, context.GameObject);
                    return originalFloat.HasValue
                           && MaterialPropertyAccess.GetFloat(material, property)
                           != originalFloat.Value;
                case ShaderPropertyType.Keyword:
                    var originalKeyword = _editService.GetMaterialKeywordPropertyValueOriginal(
                        context.Data, material, propertyName, context.GameObject);
                    return originalKeyword.HasValue
                           && material.IsKeywordEnabled($"_{propertyName}")
                           != originalKeyword.Value;
                case ShaderPropertyType.Color:
                    var originalColor = _editService.GetMaterialColorPropertyValueOriginal(
                        context.Data, material, propertyName, context.GameObject);
                    return originalColor.HasValue
                           && MaterialPropertyAccess.GetColor(material, property)
                           != originalColor.Value;
                case ShaderPropertyType.Vector:
                    var originalVector = _editService.GetMaterialVectorPropertyValueOriginal(
                        context.Data, material, propertyName, context.GameObject);
                    return originalVector.HasValue
                           && MaterialPropertyAccess.GetVector(material, property)
                           != originalVector.Value;
                case ShaderPropertyType.Cubemap:
                    return !_editService.GetMaterialCubemapValueOriginal(
                        context.Data,
                        material,
                        propertyName,
                        context.GameObject);
                case ShaderPropertyType.Texture:
                    if (!_editService.GetMaterialTextureValueOriginal(
                            context.Data,
                            material,
                            propertyName,
                            context.GameObject))
                        return true;
                    var originalOffset = _editService.GetMaterialTextureOffsetOriginal(
                        context.Data, material, propertyName, context.GameObject);
                    if (originalOffset.HasValue
                        && MaterialPropertyAccess.GetTextureOffset(material, property)
                        != originalOffset.Value)
                        return true;
                    var originalScale = _editService.GetMaterialTextureScaleOriginal(
                        context.Data, material, propertyName, context.GameObject);
                    return originalScale.HasValue
                           && MaterialPropertyAccess.GetTextureScale(material, property)
                           != originalScale.Value;
                default:
                    return false;
            }
        }

        private static bool MatchesPropertySearch(
            ICollection<MaterialEditorFilterPattern> searchTerms,
            string displayName,
            string propertyName)
        {
            if (searchTerms.Count == 0)
                return true;

            var display = displayName ?? string.Empty;
            var property = propertyName ?? string.Empty;
            foreach (var pattern in searchTerms)
                if (pattern.Matches(display) || pattern.Matches(property))
                    return true;
            return false;
        }
        internal static string GetPropertyName(
            MaterialEditorPropertyDescriptor descriptor)
        {
            return string.IsNullOrEmpty(descriptor.PropertyName)
                ? descriptor.Id
                : descriptor.PropertyName;
        }
    }
}
