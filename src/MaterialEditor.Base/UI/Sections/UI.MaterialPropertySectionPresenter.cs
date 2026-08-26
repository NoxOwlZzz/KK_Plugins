using System;
using System.Collections.Generic;
using System.Linq;
using static MaterialEditorAPI.MaterialEditorPluginBase;
using System.Globalization;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialPropertySectionPresenter
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;
        private readonly PropertyRowModelFactory _propertyRows;
        private readonly PropertyCategorySectionBuilder _categories;
        private readonly MaterialPropertyCandidatePlanner _candidatePlanner;

        internal MaterialPropertySectionPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
            _propertyRows = new PropertyRowModelFactory(editService, actions);
            _categories = new PropertyCategorySectionBuilder(session, actions);
            _candidatePlanner = new MaterialPropertyCandidatePlanner(
                editService,
                actions);
        }

        internal void AddRows(
            MaterialSectionContext context,
            MaterialSectionPresentation section,
            bool includeRows)
        {
            List<OrganizedPropertyCategory> organizedCategories;
            var shaderKey = PropertyOrganizer.ResolveShaderKey(
                context.ShaderName);
            if (!PropertyOrganizer.PropertyOrganization.TryGetValue(
                    shaderKey,
                    out organizedCategories))
                return;

            var target = MaterialEditorExtensionRegistry.CreateTargetContext(
                _editService,
                context.GameObject,
                context.Data,
                null,
                context.Material,
                context.Projector);
            var propertyContext = new MaterialEditorPropertyContext(
                target,
                context.MaterialName,
                context.ShaderName);
            var extensionDescriptors = MaterialEditorExtensionRegistry
                .GetPropertyDescriptors(propertyContext);
            Dictionary<string, ShaderPropertyData> manifestPropertyMap;
            if (!XMLShaderProperties.TryGetValue(
                    shaderKey,
                    out manifestPropertyMap))
            {
                manifestPropertyMap =
                    new Dictionary<string, ShaderPropertyData>(
                        StringComparer.Ordinal);
            }
            IEnumerable<ShaderPropertyData> manifestDefinitions =
                shaderKey == PropertyOrganizer.DefaultShaderKey
                    ? organizedCategories.SelectMany(
                        category => category.Properties)
                    : manifestPropertyMap.Values;
            var conditionContext = MaterialSectionConditionContext.Build(
                context,
                manifestDefinitions,
                extensionDescriptors);
            var conditionGraph = conditionContext.Graph;
            var manifestConditionStates =
                conditionContext.ManifestConditionStates;
            var extensionConditionStates =
                conditionContext.ExtensionConditionStates;
            if (conditionGraph != null)
            {
                section.ConditionGraph = conditionGraph;
                context.Presentation.RegisterConditionGraph(conditionGraph);
            }

            var hasSearch = context.PropertyFilter.Count > 0;
            var viableCategories = _candidatePlanner.BuildManifestCandidates(
                context,
                organizedCategories,
                manifestConditionStates,
                hasSearch);
            foreach (var viableCategory in viableCategories)
            {
                var category = viableCategory.Category;
                var candidates = viableCategory.Candidates;
                var explicitCategory = SortPropertiesByCategory.Value
                                       && category.Properties.Any(definition =>
                                            !string.IsNullOrEmpty(
                                                definition.CategoryId));
                var namedCategory = PropertyOrganizer.ShouldUseNamedCategory(
                    shaderKey,
                    explicitCategory,
                    category.Name,
                    viableCategories.Count);
                var categorySection = _categories.Add(
                    context,
                    section,
                    "manifest",
                    category.Name,
                    namedCategory,
                    includeRows,
                    category.Id);
                if (!includeRows || categorySection.RowsCollapsed)
                    continue;

                categorySection.RecordFirstRow(context.Rows.Count);
                var candidateMap = candidates.ToDictionary(
                    candidate => candidate.Definition);
                AddManifestCandidateRows(
                    context,
                    category.DirectProperties
                        .Where(candidateMap.ContainsKey)
                        .Select(definition => candidateMap[definition]),
                    category.Name,
                    null,
                    conditionGraph,
                    0);

                foreach (var subcategory in category.Subcategories)
                {
                    var subcategoryCandidates = subcategory.Properties
                        .Where(candidateMap.ContainsKey)
                        .Select(definition => candidateMap[definition])
                        .ToList();
                    if (subcategoryCandidates.Count == 0)
                        continue;

                    var collapsed = _categories.AddSubcategory(
                        context,
                        "manifest",
                        category.Id,
                        subcategory,
                        includeRows);
                    if (collapsed)
                        continue;

                    AddManifestCandidateRows(
                        context,
                        subcategoryCandidates,
                        category.Name,
                        subcategory.Name,
                        conditionGraph,
                        hasSearch ? 0 : 1);
                }
            }

            AddExtensionPropertyRows(
                context,
                section,
                includeRows,
                propertyContext,
                extensionDescriptors,
                extensionConditionStates,
                conditionGraph);
        }

        private void AddManifestCandidateRows(
            MaterialSectionContext context,
            IEnumerable<ManifestPropertyCandidate> candidates,
            string categoryName,
            string subcategoryName,
            MaterialConditionDependencyGraph conditionGraph,
            int hierarchyDepth)
        {
            var hasSearch = context.PropertyFilter.Count > 0;
            foreach (var candidate in candidates)
            {
                var definition = candidate.Definition;
                var descriptor = new PropertyDescriptor(
                    context.GameObject,
                    context.Data,
                    context.Material,
                    context.Projector,
                    context.MaterialName,
                    definition,
                    categoryName);
                var refresh = MaterialSectionConditionContext.CreateRefresh(
                    conditionGraph,
                    _actions,
                    definition.Name);
                foreach (var row in _propertyRows.Create(descriptor))
                {
                    row.HierarchyDepth = hierarchyDepth;
                    if (candidate.SearchOnlyHidden)
                        MaterialPropertyCandidatePlanner.MarkSearchOnlyShowIfResult(row, definition.ShowIf);
                    if (hasSearch)
                        MaterialPropertyCandidatePlanner.PrefixSearchPath(row, categoryName, subcategoryName);
                    row.PresentationRefresh = refresh;
                    context.Rows.Add(row);
                }
            }
        }

        private void AddExtensionPropertyRows(
            MaterialSectionContext sectionContext,
            MaterialSectionPresentation section,
            bool includeRows,
            MaterialEditorPropertyContext propertyContext,
            IList<MaterialEditorPropertyDescriptor> extensionDescriptors,
            IDictionary<int, MaterialConditionPropertyState> conditionStates,
            MaterialConditionDependencyGraph conditionGraph)
        {
            var candidates = _candidatePlanner.BuildExtensionCandidates(
                sectionContext,
                extensionDescriptors,
                conditionStates);
            foreach (var category in candidates.GroupBy(
                         candidate => candidate.Descriptor.Category ?? string.Empty))
            {
                var categoryName = category.Key;
                var namedCategory = !string.IsNullOrEmpty(categoryName);
                var categorySection = _categories.Add(
                    sectionContext,
                    section,
                    "extension",
                    categoryName,
                    namedCategory,
                    includeRows);
                if (!includeRows || categorySection.RowsCollapsed)
                    continue;

                categorySection.RecordFirstRow(sectionContext.Rows.Count);
                foreach (var candidate in category
                             .OrderBy(item => item.Descriptor.Order)
                             .ThenBy(item => item.Descriptor.DisplayName))
                {
                    var descriptor = candidate.Descriptor;
                    var propertyName = MaterialPropertyCandidatePlanner.GetPropertyName(descriptor);
                    var refresh = MaterialSectionConditionContext.CreateRefresh(
                        conditionGraph,
                        _actions,
                        propertyName);
                    if (candidate.BuiltInType.HasValue)
                    {
                        var internalDescriptor = new PropertyDescriptor(
                            sectionContext.GameObject,
                            sectionContext.Data,
                            sectionContext.Material,
                            sectionContext.Projector,
                            sectionContext.MaterialName,
                            descriptor,
                            candidate.BuiltInType.Value);
                        foreach (var row in _propertyRows.Create(internalDescriptor))
                        {
                            if (candidate.SearchOnlyHidden)
                                MaterialPropertyCandidatePlanner.MarkSearchOnlyShowIfResult(
                                    row,
                                    descriptor.VisibilityCondition);
                            if (sectionContext.PropertyFilter.Count > 0)
                                MaterialPropertyCandidatePlanner.PrefixSearchPath(row, categoryName, null);
                            row.PresentationRefresh = refresh;
                            sectionContext.Rows.Add(row);
                        }
                        continue;
                    }

                    foreach (var row in _propertyRows.CreateExtension(
                                 propertyContext,
                                 descriptor))
                    {
                        if (candidate.SearchOnlyHidden)
                            MaterialPropertyCandidatePlanner.MarkSearchOnlyShowIfResult(
                                row,
                                descriptor.VisibilityCondition);
                        if (sectionContext.PropertyFilter.Count > 0)
                            MaterialPropertyCandidatePlanner.PrefixSearchPath(row, categoryName, null);
                        row.PresentationRefresh = refresh;
                        sectionContext.Rows.Add(row);
                    }
                }
            }
        }
    }

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
