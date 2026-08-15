using System;
using System.Collections.Generic;
using System.Linq;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class OrganizedPropertyCategory
    {
        internal OrganizedPropertyCategory(
            string id,
            string name,
            int declarationOrder,
            IList<ShaderPropertyData> directProperties,
            IList<OrganizedPropertySubcategory> subcategories,
            IList<ShaderPropertyData> properties)
        {
            Id = id;
            Name = name;
            DeclarationOrder = declarationOrder;
            DirectProperties = directProperties;
            Subcategories = subcategories;
            Properties = properties;
        }

        internal string Id { get; }
        internal string Name { get; }
        internal int DeclarationOrder { get; }
        internal IList<ShaderPropertyData> DirectProperties { get; }
        internal IList<OrganizedPropertySubcategory> Subcategories { get; }
        // Flat compatibility view for consumers that do not render the
        // optional second grouping level.
        internal IList<ShaderPropertyData> Properties { get; }
    }

    internal sealed class OrganizedPropertySubcategory
    {
        internal OrganizedPropertySubcategory(
            string id,
            string name,
            int declarationOrder,
            IList<ShaderPropertyData> properties)
        {
            Id = id;
            Name = name;
            DeclarationOrder = declarationOrder;
            Properties = properties;
        }

        internal string Id { get; }
        internal string Name { get; }
        internal int DeclarationOrder { get; }
        internal IList<ShaderPropertyData> Properties { get; }
    }

    internal class PropertyOrganizer
    {
        internal const string DefaultShaderKey = "default";

        private sealed class DeclaredProperty
        {
            internal ShaderPropertyData Definition;
            internal int FallbackOrder;
        }

        private sealed class DeclaredCategory
        {
            internal string Id;
            internal string Name;
            internal int? Order;
            internal int DeclarationOrder;
            internal List<DeclaredProperty> Properties;
        }

        private sealed class DeclaredSubcategory
        {
            internal string Id;
            internal string Name;
            internal int DeclarationOrder;
            internal List<DeclaredProperty> Properties;
        }

        // Shader -> ordered category -> ordered property. Dynamic presentation
        // state (mode, conditions, compatibility and search) is deliberately not
        // cached here.
        internal static readonly Dictionary<string, List<OrganizedPropertyCategory>>
            PropertyOrganization =
                new Dictionary<string, List<OrganizedPropertyCategory>>();

        internal static string UncategorizedName = "Uncategorized";

        // Changes exactly once for each rebuilt catalog. Consumers can use the
        // generation to scope diagnostics and other catalog-derived caches.
        internal static int Generation { get; private set; }

        internal static void Refresh(Action<string> warning = null)
        {
            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.PropertyOrganization);
            try
            {
                unchecked
                {
                    Generation++;
                }
                PropertyOrganization.Clear();
                var emitWarning = warning ?? LogOrganizationWarning;
                foreach (var shader in XMLShaderProperties)
                {
                    var declared = shader.Value
                        .Select((item, index) => new DeclaredProperty
                        {
                            Definition = item.Value,
                            FallbackOrder = index
                        })
                        .Where(item => !item.Definition.Hidden)
                        .ToList();

                    // The legacy default dictionary is a cross-shader union used
                    // by non-UI persistence and API paths. Its shader-specific
                    // grouping metadata and Keyword declarations are not safe to
                    // present for an unknown shader, so build a conservative UI
                    // view without changing the underlying dictionary.
                    if (string.Equals(
                            shader.Key,
                            DefaultShaderKey,
                            StringComparison.Ordinal))
                    {
                        PropertyOrganization[shader.Key] =
                            CreateDefaultFallbackCategories(declared);
                        continue;
                    }

                    var categories = declared
                        .GroupBy(item => GetCategoryId(item.Definition))
                        .Select(group => CreateCategory(
                            shader.Key,
                            group.Key,
                            group,
                            emitWarning))
                        .OrderBy(category => category.Name == UncategorizedName ? 1 : 0)
                        .ThenBy(category => category.Order.HasValue ? 0 : 1)
                        .ThenBy(category => category.Order ?? 0)
                        .ThenBy(category => category.DeclarationOrder)
                        .ThenBy(category => category.Name)
                        .Select(category => new OrganizedPropertyCategory(
                            category.Id,
                            category.Name,
                            category.DeclarationOrder,
                            SortProperties(
                                category.Properties.Where(item =>
                                    !SortPropertiesByCategory.Value
                                    || string.IsNullOrEmpty(
                                        item.Definition.SubcategoryId))),
                            OrganizeSubcategories(
                                shader.Key,
                                category.Id,
                                category.Properties,
                                emitWarning),
                            SortProperties(category.Properties)))
                        .ToList();

                    PropertyOrganization[shader.Key] = categories;
                }
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.PropertyOrganization,
                    performanceSample);
            }
        }

        private static List<OrganizedPropertyCategory>
            CreateDefaultFallbackCategories(
                IEnumerable<DeclaredProperty> source)
        {
            var properties = source
                .Where(item =>
                    item.Definition.Type
                    != MaterialAPI.ShaderPropertyType.Keyword)
                .Select(item => new DeclaredProperty
                {
                    Definition = item.Definition
                        .WithoutConditionsForUiFallback(),
                    FallbackOrder = item.FallbackOrder
                })
                .ToList();
            if (properties.Count == 0)
                return new List<OrganizedPropertyCategory>();

            var ordered = SortProperties(properties);
            return new List<OrganizedPropertyCategory>
            {
                new OrganizedPropertyCategory(
                    UncategorizedName,
                    UncategorizedName,
                    properties.Min(item =>
                        item.Definition.DeclarationOrder),
                    ordered,
                    new List<OrganizedPropertySubcategory>(),
                    ordered)
            };
        }

        internal static bool ShouldUseNamedCategory(
            string shaderKey,
            bool hasExplicitCategory,
            string categoryName,
            int viableCategoryCount)
        {
            if (string.Equals(
                    shaderKey,
                    DefaultShaderKey,
                    StringComparison.Ordinal))
                return false;

            return hasExplicitCategory
                   || viableCategoryCount > 1
                   || categoryName != UncategorizedName;
        }

        internal static string ResolveShaderKey(string shaderName)
        {
            return XMLShaderProperties.ContainsKey(shaderName)
                ? shaderName
                : DefaultShaderKey;
        }

        private static DeclaredCategory CreateCategory(
            string shaderName,
            string categoryId,
            IEnumerable<DeclaredProperty> source,
            Action<string> warning)
        {
            var properties = InDeclarationOrder(source).ToList();
            var hierarchyEnabled = SortPropertiesByCategory.Value;
            var hasExplicitHierarchy = hierarchyEnabled
                                       && properties.Any(item =>
                                           !string.IsNullOrEmpty(
                                               item.Definition.CategoryId));

            if (!hasExplicitHierarchy)
            {
                return new DeclaredCategory
                {
                    Id = categoryId,
                    Name = hierarchyEnabled
                        ? GetCategoryName(
                            properties.Select(item => item.Definition))
                        : UncategorizedName,
                    Order = hierarchyEnabled
                        ? properties
                            .Where(item => item.Definition.CategoryOrder.HasValue)
                            .Select(item => item.Definition.CategoryOrder)
                            .FirstOrDefault()
                        : null,
                    DeclarationOrder = properties.Min(item =>
                        item.Definition.DeclarationOrder),
                    Properties = properties
                };
            }

            return new DeclaredCategory
            {
                Id = categoryId,
                Name = ResolveStringMetadata(
                           shaderName,
                           "Category",
                           categoryId,
                           null,
                           "DisplayName",
                           properties,
                           item => item.Definition.HasExplicitCategoryDisplayName
                               ? item.Definition.CategoryDisplayName
                               : null,
                           warning)
                       ?? categoryId,
                Order = ResolveOrderMetadata(
                    shaderName,
                    "Category",
                    categoryId,
                    null,
                    properties,
                    warning),
                DeclarationOrder = properties.Min(item =>
                    item.Definition.DeclarationOrder),
                Properties = properties
            };
        }

        private static string GetCategoryId(ShaderPropertyData definition)
        {
            if (!SortPropertiesByCategory.Value)
                return UncategorizedName;
            if (!string.IsNullOrEmpty(definition.CategoryId))
                return definition.CategoryId;
            if (string.IsNullOrEmpty(definition.Category))
                return UncategorizedName;
            return char.ToUpper(definition.Category[0])
                   + definition.Category.Substring(1);
        }

        private static string GetCategoryName(
            IEnumerable<ShaderPropertyData> definitions)
        {
            var definition = definitions.First();
            if (!string.IsNullOrEmpty(definition.CategoryId))
            {
                return FirstNonEmpty(
                           definitions.Select(item =>
                               item.CategoryDisplayName))
                       ?? definition.CategoryId;
            }

            return GetCategoryId(definition);
        }

        private static IList<OrganizedPropertySubcategory>
            OrganizeSubcategories(
                string shaderName,
                string categoryId,
                IEnumerable<DeclaredProperty> source,
                Action<string> warning)
        {
            if (!SortPropertiesByCategory.Value)
                return new List<OrganizedPropertySubcategory>();
            return source
                .Where(item =>
                    !string.IsNullOrEmpty(item.Definition.SubcategoryId))
                .GroupBy(item => item.Definition.SubcategoryId)
                .Select(group => CreateSubcategory(
                    shaderName,
                    categoryId,
                    group.Key,
                    group,
                    warning))
                .OrderBy(item => item.DeclarationOrder)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .Select(item => new OrganizedPropertySubcategory(
                    item.Id,
                    item.Name,
                    item.DeclarationOrder,
                    SortProperties(item.Properties)))
                .ToList();
        }

        private static DeclaredSubcategory CreateSubcategory(
            string shaderName,
            string categoryId,
            string subcategoryId,
            IEnumerable<DeclaredProperty> source,
            Action<string> warning)
        {
            var properties = InDeclarationOrder(source).ToList();
            return new DeclaredSubcategory
            {
                Id = subcategoryId,
                Name = ResolveStringMetadata(
                           shaderName,
                           "Subcategory",
                           subcategoryId,
                           categoryId,
                           "DisplayName",
                           properties,
                           item => item.Definition.HasExplicitSubcategoryDisplayName
                               ? item.Definition.SubcategoryDisplayName
                               : null,
                           warning)
                       ?? subcategoryId,
                DeclarationOrder = properties.Min(item =>
                    item.Definition.DeclarationOrder),
                Properties = properties
            };
        }

        private static string ResolveStringMetadata(
            string shaderName,
            string kind,
            string id,
            string parentCategoryId,
            string fieldName,
            IList<DeclaredProperty> properties,
            Func<DeclaredProperty, string> selector,
            Action<string> warning)
        {
            var values = properties
                .Select(selector)
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (values.Count > 1)
            {
                WarnConflict(
                    shaderName,
                    kind,
                    id,
                    parentCategoryId,
                    fieldName,
                    values[0],
                    warning);
            }
            return values.FirstOrDefault();
        }

        private static int? ResolveOrderMetadata(
            string shaderName,
            string kind,
            string id,
            string parentCategoryId,
            IList<DeclaredProperty> properties,
            Action<string> warning)
        {
            var values = properties
                .Where(item => item.Definition.CategoryOrder.HasValue)
                .Select(item => item.Definition.CategoryOrder.Value)
                .Distinct()
                .ToList();
            if (values.Count > 1)
            {
                WarnConflict(
                    shaderName,
                    kind,
                    id,
                    parentCategoryId,
                    "Order",
                    values[0].ToString(),
                    warning);
            }
            return values.Count == 0 ? (int?)null : values[0];
        }

        private static void WarnConflict(
            string shaderName,
            string kind,
            string id,
            string parentCategoryId,
            string fieldName,
            string selectedValue,
            Action<string> warning)
        {
            var location = kind + " Id '" + id + "'";
            if (!string.IsNullOrEmpty(parentCategoryId))
                location += " in Category Id '" + parentCategoryId + "'";
            warning?.Invoke(
                "Shader '" + shaderName + "' declares conflicting "
                + fieldName + " values for " + location + "; the first value '"
                + selectedValue + "' in manifest declaration order was used.");
        }

        private static IOrderedEnumerable<DeclaredProperty> InDeclarationOrder(
            IEnumerable<DeclaredProperty> source)
        {
            return source
                .OrderBy(item => item.Definition.DeclarationOrder)
                .ThenBy(item => item.FallbackOrder);
        }

        private static void LogOrganizationWarning(string message)
        {
            MaterialEditorPluginBase.Logger?.LogWarning(
                "Material Editor property organization: " + message);
        }

        private static string FirstNonEmpty(IEnumerable<string> values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrEmpty(value));
        }

        private static IList<ShaderPropertyData> SortProperties(
            IEnumerable<DeclaredProperty> source)
        {
            IOrderedEnumerable<DeclaredProperty> ordered = source
                .OrderBy(item => item.Definition.Order.HasValue ? 0 : 1)
                .ThenBy(item => item.Definition.Order ?? 0);

            if (SortPropertiesByType.Value)
                ordered = ordered.ThenBy(item => item.Definition.Type);
            if (SortPropertiesByName.Value)
                ordered = ordered.ThenBy(item => item.Definition.Name);

            return ordered
                .ThenBy(item => item.Definition.DeclarationOrder)
                .ThenBy(item => item.FallbackOrder)
                .Select(item => item.Definition)
                .ToList();
        }
    }
}
