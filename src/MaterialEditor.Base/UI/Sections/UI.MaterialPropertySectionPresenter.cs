using System;
using System.Collections.Generic;
using System.Linq;
using static MaterialEditorAPI.MaterialEditorPluginBase;

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
}
