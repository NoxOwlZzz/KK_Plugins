using System;
using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class MaterialSectionConditionContext
    {
        private MaterialSectionConditionContext(
            MaterialConditionDependencyGraph graph,
            Dictionary<string, MaterialConditionPropertyState>
                manifestConditionStates,
            Dictionary<int, MaterialConditionPropertyState>
                extensionConditionStates)
        {
            Graph = graph;
            ManifestConditionStates = manifestConditionStates;
            ExtensionConditionStates = extensionConditionStates;
        }

        internal MaterialConditionDependencyGraph Graph { get; private set; }
        internal Dictionary<string, MaterialConditionPropertyState>
            ManifestConditionStates { get; private set; }
        internal Dictionary<int, MaterialConditionPropertyState>
            ExtensionConditionStates { get; private set; }

        internal static MaterialSectionConditionContext Build(
            MaterialSectionContext context,
            IEnumerable<ShaderPropertyData> manifestDefinitions,
            IList<MaterialEditorPropertyDescriptor> extensionDescriptors)
        {
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
            }
            return new MaterialSectionConditionContext(
                conditionGraph,
                manifestConditionStates,
                extensionConditionStates);
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

        internal static Action CreateRefresh(
            MaterialConditionDependencyGraph graph,
            MaterialEditorPresentationActions actions,
            string propertyName)
        {
            if (graph == null || actions.RequestCondition == null)
                return null;
            MaterialConditionInvalidationHandle handle;
            if (!graph.TryCreateHandle(propertyName, out handle))
                return null;
            return () => actions.RequestCondition(handle);
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
    }
}
