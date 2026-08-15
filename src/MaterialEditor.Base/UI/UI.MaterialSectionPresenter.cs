using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Builds one material section, including its shader, projector, manifest,
    /// extension-property, and category-navigation rows.
    /// </summary>
    internal sealed class MaterialSectionPresenter
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorPresentationActions _actions;
        private readonly PropertyRowModelFactory _propertyRows;
        private readonly PropertyCategorySectionBuilder _categories;

        private sealed class ExtensionPropertyCandidate
        {
            internal MaterialEditorPropertyDescriptor Descriptor;
            internal ShaderPropertyType? BuiltInType;
            internal bool SearchOnlyHidden;
        }

        private sealed class ManifestPropertyCandidate
        {
            internal ShaderPropertyData Definition;
            internal bool SearchOnlyHidden;
        }

        private sealed class ManifestCategoryCandidateGroup
        {
            internal OrganizedPropertyCategory Category;
            internal List<ManifestPropertyCandidate> Candidates;
        }

        internal MaterialSectionPresenter(
            MaterialEditService editService,
            MaterialEditorSessionState session,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _session = session;
            _actions = actions;
            _propertyRows = new PropertyRowModelFactory(editService, actions);
            _categories = new PropertyCategorySectionBuilder(session, actions);
        }

        internal void AddRows(MaterialSectionContext context)
        {
            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.CategoryRebuild);
            try
            {
                var materialCollapsed = MaterialEditorSessionState.IsCollapsed(
                    _session.CollapsedMaterialSections,
                    context.MaterialKey);
                var shaderCollapsed = MaterialEditorSessionState.IsCollapsed(
                    _session.CollapsedShaderSections,
                    context.ShaderKey);
                var hasSearch = context.PropertyFilter.Count > 0;
                var materialRowsCollapsed = materialCollapsed && !hasSearch;
                var shaderRowsCollapsed = shaderCollapsed && !hasSearch;
                var section = new MaterialSectionPresentation(
                    context.ShaderKey,
                    context.MaterialName,
                    context.ShaderName,
                    context.Rows.Count,
                    () =>
                    {
                        var changed = MaterialEditorSessionState.IsCollapsed(
                                          _session.CollapsedMaterialSections,
                                          context.MaterialKey)
                                      || MaterialEditorSessionState.IsCollapsed(
                                          _session.CollapsedShaderSections,
                                          context.ShaderKey);
                        MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedMaterialSections, context.MaterialKey, false);
                        MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedShaderSections, context.ShaderKey, false);
                        return changed;
                    },
                    () => MaterialEditorSessionState.IsCollapsed(
                        _session.CollapsedMaterialSections,
                        context.MaterialKey),
                    value => MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedMaterialSections,
                        context.MaterialKey,
                        value));
                context.Presentation.MaterialSections.Add(section);

                var materialItem = new MaterialRowModel()
                {
                    GameObject = context.GameObject,
                    Data = context.Data,
                    Material = context.Material,
                    Projector = context.Projector,
                    MaterialName = context.MaterialName,
                    Collapsed = materialRowsCollapsed,
                    CollapsedOnChange = value =>
                    {
                        MaterialEditorSessionState.SetCollapsed(
                            _session.CollapsedMaterialSections, context.MaterialKey, value);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                    },
                    Copy = () => context.Edits.CopyMaterialEdits(
                        context.Material,
                        context.Projector),
                    Paste = () =>
                    {
                        context.Edits.PasteMaterialEdits(
                            context.Material,
                            context.Projector);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                    },
                    Rename = () => _actions.ShowRename(
                        context.GameObject,
                        context.Material,
                        context.Data)
                };
                if (context.Projector == null)
                {
                    materialItem.CopyOrRemove = () =>
                    {
                        context.Edits.CopyOrRemoveMaterial(context.Material);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                        _actions.RefreshMaterialSelection(
                            context.GameObject,
                            context.Data,
                            context.AllRenderers);
                    };
                }
                context.Rows.Add(materialItem);

                ShaderRowModel shaderItem = null;
                if (!materialRowsCollapsed && context.Projector != null)
                    AddProjectorRows(context);

                if (!materialRowsCollapsed)
                    shaderItem = AddShaderRows(context, shaderRowsCollapsed);

                AddPropertyRows(
                    context,
                    section,
                    !materialRowsCollapsed && !shaderRowsCollapsed);

                if (shaderItem != null)
                {
                    shaderItem.HasCategories = !hasSearch
                                               && section.Categories.Any(
                                                   category =>
                                                       category.CanCollapse);
                    shaderItem.AllCategoriesCollapsed = !hasSearch
                                                        && section.AllCategoriesCollapsed;
                    shaderItem.CategoriesCollapsedOnChange = value =>
                    {
                        section.SetAllCategoriesCollapsed(value);
                        _actions.Refresh(context.GameObject, context.Data, context.Filter);
                    };
                }
                section.EndRowIndex = Math.Max(
                    section.MaterialRowIndex,
                    context.Rows.Count - 1);
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.CategoryRebuild,
                    performanceSample);
            }
        }

        private ShaderRowModel AddShaderRows(
            MaterialSectionContext context,
            bool collapsed)
        {
            var originalShaderName = context.Edits.GetOriginalShader(
                context.Material);
            if (originalShaderName.IsNullOrEmpty())
                originalShaderName = context.ShaderName;
            var shaderItem = new ShaderRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                ShaderName = context.ShaderName,
                OriginalShaderName = originalShaderName,
                TooltipText = ShaderUiMetadataRegistry.GetShaderTooltip(
                    context.ShaderName),
                Collapsed = collapsed,
                CollapsedOnChange = value =>
                {
                    MaterialEditorSessionState.SetCollapsed(
                        _session.CollapsedShaderSections,
                        context.ShaderKey,
                        value);
                    _actions.Refresh(context.GameObject, context.Data, context.Filter);
                },
                ShaderNameOnChange = value =>
                {
                    context.Edits.SetShader(
                        context.Material,
                        value);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                ShaderNameOnReset = () =>
                {
                    context.Edits.ResetShader(context.Material);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        context.GameObject,
                        RowModel.RowItemType.Shader,
                        context.MaterialName,
                        string.Empty,
                        string.Empty)
            };
            context.Rows.Add(shaderItem);

            if (collapsed)
                return shaderItem;

            var originalRenderQueue =
                context.Edits.GetOriginalRenderQueue(context.Material)
                ?? context.Material.renderQueue;
            context.Rows.Add(new ShaderRenderQueueRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                Value = context.Material.renderQueue,
                OriginalValue = originalRenderQueue,
                ValueOnChange = value =>
                    context.Edits.SetRenderQueue(
                        context.Material,
                        value),
                ValueOnReset = () =>
                    context.Edits.ResetRenderQueue(context.Material)
            });
            return shaderItem;
        }

        private void AddPropertyRows(
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
            var conditionDependencies = BuildConditionDependencies(
                manifestDefinitions,
                extensionDescriptors);
            MaterialConditionDependencyGraph conditionGraph = null;
            Dictionary<string, MaterialConditionPropertyState>
                manifestConditionStates = null;
            Dictionary<int, MaterialConditionPropertyState>
                extensionConditionStates = null;
            if (conditionDependencies.Count > 0)
            {
                var conditionKinds = BuildConditionSourceKinds(
                    manifestDefinitions,
                    extensionDescriptors,
                    conditionDependencies);
                var conditionMaterials = CaptureConditionMaterialGroup(
                    context);
                Func<string, MaterialConditionValueSnapshot>
                    resolveConditionValues = propertyName =>
                    ResolveConditionValues(
                        conditionMaterials,
                        conditionKinds,
                        propertyName);
                var conditionValues =
                    new MaterialEditorConditionValueSnapshotCache(
                    resolveConditionValues,
                    conditionDependencies.Count);
                Func<string, MaterialConditionValueSnapshot>
                    resolveInitialCondition =
                    conditionValues.Resolve;
                conditionGraph = new MaterialConditionDependencyGraph(
                    context.Presentation.OwnerToken,
                    context.ShaderName,
                    resolveConditionValues,
                    LogConditionWarning);
                var knownPropertyNames = new HashSet<string>(
                    StringComparer.Ordinal);
                manifestConditionStates =
                    new Dictionary<string, MaterialConditionPropertyState>(
                        StringComparer.Ordinal);
                foreach (var definition in manifestDefinitions)
                {
                    if (conditionDependencies.Contains(definition.Name))
                        knownPropertyNames.Add(definition.Name);
                    if (definition.ShowIf == null)
                        continue;
                    manifestConditionStates[definition.Name] =
                        conditionGraph.RegisterProperty(
                            definition.Name,
                            definition.ShowIf,
                            resolveInitialCondition);
                }

                extensionConditionStates =
                    new Dictionary<int, MaterialConditionPropertyState>();
                for (var descriptorIndex = 0;
                     descriptorIndex < extensionDescriptors.Count;
                     descriptorIndex++)
                {
                    var descriptor = extensionDescriptors[descriptorIndex];
                    var propertyName = GetPropertyName(descriptor);
                    if (conditionDependencies.Contains(propertyName))
                        knownPropertyNames.Add(propertyName);
                    if (descriptor.VisibilityCondition == null)
                        continue;
                    extensionConditionStates.Add(
                        descriptorIndex,
                        conditionGraph.RegisterProperty(
                            propertyName,
                            descriptor.VisibilityCondition,
                            resolveInitialCondition));
                }
                conditionGraph.CompleteBuild(
                    conditionKinds,
                    knownPropertyNames);
                section.ConditionGraph = conditionGraph;
                context.Presentation.RegisterConditionGraph(conditionGraph);
            }

            var hasSearch = context.PropertyFilter.Count > 0;

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
                var refresh = CreateConditionRefresh(
                    conditionGraph,
                    definition.Name);
                foreach (var row in _propertyRows.Create(descriptor))
                {
                    row.HierarchyDepth = hierarchyDepth;
                    if (candidate.SearchOnlyHidden)
                        MarkSearchOnlyShowIfResult(row, definition.ShowIf);
                    if (hasSearch)
                        PrefixSearchPath(row, categoryName, subcategoryName);
                    row.PresentationRefresh = refresh;
                    context.Rows.Add(row);
                }
            }
        }

        private static void PrefixSearchPath(
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

        private static void MarkSearchOnlyShowIfResult(
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

        private void AddExtensionPropertyRows(
            MaterialSectionContext sectionContext,
            MaterialSectionPresentation section,
            bool includeRows,
            MaterialEditorPropertyContext propertyContext,
            IList<MaterialEditorPropertyDescriptor> extensionDescriptors,
            IDictionary<int, MaterialConditionPropertyState> conditionStates,
            MaterialConditionDependencyGraph conditionGraph)
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
                    var propertyName = GetPropertyName(descriptor);
                    var refresh = CreateConditionRefresh(
                        conditionGraph,
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
                                MarkSearchOnlyShowIfResult(
                                    row,
                                    descriptor.VisibilityCondition);
                            if (sectionContext.PropertyFilter.Count > 0)
                                PrefixSearchPath(row, categoryName, null);
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
                            MarkSearchOnlyShowIfResult(
                                row,
                                descriptor.VisibilityCondition);
                        if (sectionContext.PropertyFilter.Count > 0)
                            PrefixSearchPath(row, categoryName, null);
                        row.PresentationRefresh = refresh;
                        sectionContext.Rows.Add(row);
                    }
                }
            }
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

        private static Dictionary<string, MaterialConditionSourceKind>
            BuildConditionSourceKinds(
                IEnumerable<ShaderPropertyData> definitions,
                IEnumerable<MaterialEditorPropertyDescriptor> descriptors,
                ICollection<string> requestedSources)
        {
            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.ConditionSourceKindBuilds);
            try
            {
                return MaterialConditionSourceCatalog.Build(
                    definitions,
                    descriptors,
                    requestedSources);
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.ConditionSourceKindBuilds,
                    performanceSample);
            }
        }

        private static HashSet<string> BuildConditionDependencies(
            IEnumerable<ShaderPropertyData> definitions,
            IEnumerable<MaterialEditorPropertyDescriptor> descriptors)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in definitions)
                AddConditionDependency(result, definition.ShowIf);
            foreach (var descriptor in descriptors)
                AddConditionDependency(result, descriptor.VisibilityCondition);
            return result;
        }

        private static void AddConditionDependency(
            HashSet<string> destination,
            MaterialEditorPropertyCondition condition)
        {
            if (condition != null)
                destination.Add(condition.PropertyName);
        }

        private static IList<Material> CaptureConditionMaterialGroup(
            MaterialSectionContext context)
        {
            var materials = new List<Material>();
            AddConditionMaterial(materials, context.Material);
            if (context.Projector != null)
                return materials;

            foreach (var renderer in context.AllRenderers)
            {
                if (renderer == null)
                    continue;
                IEnumerable<Material> rendererMaterials;
                try
                {
                    rendererMaterials = GetMaterials(
                        context.GameObject,
                        renderer);
                }
                catch
                {
                    // The representative remains available, so a patched or
                    // transient renderer lookup cannot hide its properties.
                    continue;
                }
                if (rendererMaterials == null)
                    continue;
                foreach (var material in rendererMaterials)
                {
                    if (material == null
                        || material.NameFormatted() != context.MaterialName)
                        continue;
                    AddConditionMaterial(materials, material);
                }
            }
            return materials;
        }

        private static void AddConditionMaterial(
            IList<Material> materials,
            Material material)
        {
            if (material == null)
                return;
            for (var index = 0; index < materials.Count; index++)
                if (ReferenceEquals(materials[index], material))
                    return;
            materials.Add(material);
        }

        private static MaterialConditionValueSnapshot ResolveConditionValues(
            IList<Material> materials,
            IDictionary<string, MaterialConditionSourceKind> sourceKinds,
            string propertyName)
        {
            var values = new float?[materials.Count];
            MaterialConditionSourceKind kind;
            if (!sourceKinds.TryGetValue(propertyName, out kind))
                return new MaterialConditionValueSnapshot(values);

            MaterialPropertyHandle property = default(MaterialPropertyHandle);
            if (kind == MaterialConditionSourceKind.Float)
                property = MaterialPropertyIdCache.Get(propertyName);
            for (var index = 0; index < materials.Count; index++)
            {
                var material = materials[index];
                if (material == null)
                    continue;
                MaterialEditorPerformance.Increment(
                    MaterialEditorPerformanceMetric.ConditionMaterialReads);
                try
                {
                    if (kind == MaterialConditionSourceKind.Keyword)
                    {
                        values[index] = material.IsKeywordEnabled(
                            $"_{propertyName}")
                            ? 1f
                            : 0f;
                    }
                    else if (MaterialPropertyAccess.HasProperty(
                                 material,
                                 property))
                    {
                        values[index] = MaterialPropertyAccess.GetFloat(
                            material,
                            property);
                    }
                }
                catch
                {
                    // One unreadable member is a missing value. The aggregate
                    // remains fail-open instead of hiding a shared property.
                }
            }
            return new MaterialConditionValueSnapshot(values);
        }

        private Action CreateConditionRefresh(
            MaterialConditionDependencyGraph graph,
            string propertyName)
        {
            if (graph == null || _actions.RequestCondition == null)
                return null;
            MaterialConditionInvalidationHandle handle;
            if (!graph.TryCreateHandle(propertyName, out handle))
                return null;
            return () => _actions.RequestCondition(handle);
        }

        private static void LogConditionWarning(string message)
        {
            MaterialEditorPluginBase.Logger?.LogWarning(message);
        }

        private static string GetPropertyName(
            MaterialEditorPropertyDescriptor descriptor)
        {
            return string.IsNullOrEmpty(descriptor.PropertyName)
                ? descriptor.Id
                : descriptor.PropertyName;
        }

        private void AddProjectorRows(MaterialSectionContext context)
        {
            foreach (var property in Enum.GetValues(typeof(ProjectorProperties)).Cast<ProjectorProperties>())
            {
                string name;
                float value;
                float maxValue;
                GetProjectorPresentation(
                    context.Projector,
                    property,
                    out name,
                    out value,
                    out maxValue);

                if (context.PropertyFilter.Count > 0)
                {
                    var matches = false;
                    foreach (var filterPattern in context.PropertyFilter)
                    {
                        if (!filterPattern.Matches(name))
                            continue;
                        matches = true;
                        break;
                    }
                    if (!matches)
                        continue;
                }

                var original =
                    context.Edits.GetOriginalProjectorProperty(
                        context.Projector,
                        property)
                    ?? value;
                context.Rows.Add(CreateFloatRow(
                    context.GameObject,
                    context.Data,
                    null,
                    context.Projector,
                    name,
                    value,
                    original,
                    0f,
                    maxValue,
                    () => _actions.SelectProjectorInterpolable(
                        context.GameObject,
                        property,
                        context.Projector.NameFormatted()),
                    newValue =>
                        _editService.SetProjectorProperty(
                            context.Data,
                            context.Projector,
                            property,
                            newValue,
                            context.Projector.gameObject),
                    () =>
                        _editService.RemoveProjectorProperty(
                            context.Data,
                            context.Projector,
                            property,
                            context.Projector.gameObject)));
            }
        }

        private static FloatPropertyRowModel CreateFloatRow(
            GameObject gameObject,
            object data,
            Material material,
            Projector projector,
            string propertyName,
            float value,
            float original,
            float? minValue,
            float? maxValue,
            Action selectInterpolable,
            Action<float> changeValue,
            Action resetValue)
        {
            var item = new FloatPropertyRowModel(propertyName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                Value = value,
                OriginalValue = original,
                HasRange = true,
                SelectInterpolable = selectInterpolable,
                ValueOnChange = changeValue,
                ValueOnReset = resetValue
            };
            if (minValue != null)
                item.SliderMinimum = minValue.Value;
            if (maxValue != null)
                item.SliderMaximum = maxValue.Value;
            return item;
        }

        private static void GetProjectorPresentation(
            Projector projector,
            ProjectorProperties property,
            out string name,
            out float value,
            out float maxValue)
        {
            name = string.Empty;
            value = 0f;
            maxValue = 100f;
            switch (property)
            {
                case ProjectorProperties.Enabled:
                    name = "Enabled";
                    value = Convert.ToSingle(projector.enabled);
                    maxValue = 1f;
                    break;
                case ProjectorProperties.NearClipPlane:
                    name = "Near Clip Plane";
                    value = projector.nearClipPlane;
                    maxValue = ProjectorNearClipPlaneMax.Value;
                    break;
                case ProjectorProperties.FarClipPlane:
                    name = "Far Clip Plane";
                    value = projector.farClipPlane;
                    maxValue = ProjectorFarClipPlaneMax.Value;
                    break;
                case ProjectorProperties.FieldOfView:
                    name = "Field Of View";
                    value = projector.fieldOfView;
                    maxValue = ProjectorFieldOfViewMax.Value;
                    break;
                case ProjectorProperties.AspectRatio:
                    name = "Aspect Ratio";
                    value = projector.aspectRatio;
                    maxValue = ProjectorAspectRatioMax.Value;
                    break;
                case ProjectorProperties.Orthographic:
                    name = "Orthographic";
                    value = Convert.ToSingle(projector.orthographic);
                    maxValue = 1f;
                    break;
                case ProjectorProperties.OrthographicSize:
                    name = "Orthographic Size";
                    value = projector.orthographicSize;
                    maxValue = ProjectorOrthographicSizeMax.Value;
                    break;
                case ProjectorProperties.IgnoreMapLayer:
                    name = "Ignore Map layer";
                    value = Convert.ToSingle(
                        projector.ignoreLayers == (projector.ignoreLayers | (1 << 11)));
                    maxValue = 1f;
                    break;
                case ProjectorProperties.IgnoreCharaLayer:
                    name = "Ignore Chara Layer";
                    value = Convert.ToSingle(
                        projector.ignoreLayers == (projector.ignoreLayers | (1 << 10)));
                    maxValue = 1f;
                    break;
            }
        }
    }
}
