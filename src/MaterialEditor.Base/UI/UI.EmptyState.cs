namespace MaterialEditorAPI
{
    internal static class MaterialEditorEmptyState
    {
        internal static string ForPresentation(
            int rowCount,
            bool hasActiveFilter)
        {
            if (rowCount != 0)
                return null;

            return hasActiveFilter
                ? "No matches"
                : "No rows in this view";
        }

        internal static string ForSelectionList(
            int totalEntryCount,
            int visibleEntryCount,
            string noEntriesText)
        {
            if (visibleEntryCount != 0)
                return null;

            return totalEntryCount == 0
                ? noEntriesText
                : "No matches";
        }
    }
}
