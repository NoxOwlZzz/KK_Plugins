using System.Collections.Generic;
using System.Linq;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class OrganizedPropertyCategory
    {
        internal OrganizedPropertyCategory(
            string name,
            IList<ShaderPropertyData> properties)
        {
            Name = name;
            Properties = properties;
        }

        internal string Name { get; }
        internal IList<ShaderPropertyData> Properties { get; }
    }

    internal class PropertyOrganizer
    {
        private sealed class DeclaredProperty
        {
            internal ShaderPropertyData Definition;
            internal int FallbackOrder;
        }

        private sealed class DeclaredCategory
        {
            internal string Name;
            internal int? Order;
            internal int DeclarationOrder;
            internal List<DeclaredProperty> Properties;
        }

        // Shader -> ordered categories -> ordered properties.
        internal static readonly Dictionary<string, List<OrganizedPropertyCategory>>
            PropertyOrganization =
                new Dictionary<string, List<OrganizedPropertyCategory>>();

        internal static string UncategorizedName = "Uncategorized";

        internal static void Refresh()
        {
            PropertyOrganization.Clear();
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

                var categories = declared
                    .GroupBy(item => GetCategoryName(item.Definition))
                    .Select(group => new DeclaredCategory
                    {
                        Name = group.Key,
                        // A manifest should use one CategoryOrder per category.
                        // If it does not, the first explicitly declared value wins.
                        Order = group
                            .Where(item => item.Definition.CategoryOrder.HasValue)
                            .OrderBy(item => item.Definition.DeclarationOrder)
                            .ThenBy(item => item.FallbackOrder)
                            .Select(item => item.Definition.CategoryOrder)
                            .FirstOrDefault(),
                        DeclarationOrder = group.Min(
                            item => item.Definition.DeclarationOrder),
                        Properties = group.ToList()
                    })
                    .OrderBy(category =>
                        category.Name == UncategorizedName ? 1 : 0)
                    .ThenBy(category => category.Order.HasValue ? 0 : 1)
                    .ThenBy(category => category.Order ?? 0)
                    .ThenBy(category => category.DeclarationOrder)
                    .ThenBy(category => category.Name)
                    .Select(category => new OrganizedPropertyCategory(
                        category.Name,
                        SortProperties(category.Properties)))
                    .ToList();

                PropertyOrganization[shader.Key] = categories;
            }
        }

        private static string GetCategoryName(ShaderPropertyData definition)
        {
            if (string.IsNullOrEmpty(definition.Category)
                || !SortPropertiesByCategory.Value)
                return UncategorizedName;

            return char.ToUpper(definition.Category[0])
                   + definition.Category.Substring(1);
        }

        private static IList<ShaderPropertyData> SortProperties(
            IEnumerable<DeclaredProperty> source)
        {
            var items = source.ToList();
            var explicitlyOrdered = items
                .Where(item => item.Definition.Order.HasValue)
                .OrderBy(item => item.Definition.Order.Value)
                .ThenBy(item => item.Definition.DeclarationOrder)
                .ThenBy(item => item.FallbackOrder);

            IOrderedEnumerable<DeclaredProperty> legacyOrdered = items
                .Where(item => !item.Definition.Order.HasValue)
                .OrderBy(item => 0);

            // Properties without schema-v2 Order continue to use the existing
            // sort settings. Explicit Order ties retain declaration order.
            if (SortPropertiesByType.Value)
                legacyOrdered = legacyOrdered.ThenBy(item => item.Definition.Type);
            if (SortPropertiesByName.Value)
                legacyOrdered = legacyOrdered.ThenBy(item => item.Definition.Name);

            return explicitlyOrdered
                .Concat(legacyOrdered
                    .ThenBy(item => item.Definition.DeclarationOrder)
                    .ThenBy(item => item.FallbackOrder))
                .Select(item => item.Definition)
                .ToList();
        }
    }
}
