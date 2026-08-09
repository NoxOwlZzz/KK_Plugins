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
                        DeclarationOrder = group.Min(
                            item => item.Definition.DeclarationOrder),
                        Properties = group.ToList()
                    })
                    .OrderBy(category =>
                        category.Name == UncategorizedName ? 1 : 0)
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
            IOrderedEnumerable<DeclaredProperty> legacyOrdered =
                items.OrderBy(item => 0);

            // Preserve the legacy sort settings. Declaration order provides a
            // deterministic fallback when the configured sort keys tie or are
            // disabled.
            if (SortPropertiesByType.Value)
                legacyOrdered = legacyOrdered.ThenBy(item => item.Definition.Type);
            if (SortPropertiesByName.Value)
                legacyOrdered = legacyOrdered.ThenBy(item => item.Definition.Name);

            return legacyOrdered
                .ThenBy(item => item.Definition.DeclarationOrder)
                .ThenBy(item => item.FallbackOrder)
                .Select(item => item.Definition)
                .ToList();
        }
    }
}
