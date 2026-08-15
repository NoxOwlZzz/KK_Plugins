using MaterialEditorAPI;

internal static class CubemapIdentityTests
{
    internal static void Run()
    {
        ReorderedBindingsMatchByStableIdentity();
        BindingCountMismatchIsRejectedWithoutMapping();
        SameCountWithDifferentSlotIsRejected();
        DuplicateSavedIdentityIsRejected();
        DuplicateCurrentIdentityIsRejected();
    }

    private static void BindingCountMismatchIsRejectedWithoutMapping()
    {
        int[] mapping;
        string error;
        True(
            !MaterialCubemapIdentityMatcher.TryMatch(
                new[] { Identity("$", 0, 0, "Body") },
                new[]
                {
                    Identity("$", 0, 0, "Body"),
                    Identity("$", 0, 1, "Body")
                },
                out mapping,
                out error),
            "binding count mismatch rejected");
        Equal<int[]>(null, mapping, "count mismatch publishes no mapping");
        True(error != null && error.Contains("count changed"),
            "count mismatch reason");
    }

    private static void ReorderedBindingsMatchByStableIdentity()
    {
        var first = Identity("$/4:Body", 0, 0, "Body");
        var second = Identity("$/4:Body", 0, 1, "Body");
        int[] mapping;
        string error;
        True(
            MaterialCubemapIdentityMatcher.TryMatch(
                new[] { first, second },
                new[] { second, first },
                out mapping,
                out error),
            "reordered stable identities match");
        Equal(null, error, "reordered identity error");
        Equal(1, mapping[0], "reordered first mapping");
        Equal(0, mapping[1], "reordered second mapping");
    }

    private static void SameCountWithDifferentSlotIsRejected()
    {
        int[] mapping;
        string error;
        True(
            !MaterialCubemapIdentityMatcher.TryMatch(
                new[] { Identity("$", 0, 0, "Body") },
                new[] { Identity("$", 0, 1, "Body") },
                out mapping,
                out error),
            "same-count slot change rejected");
        True(error != null && error.Contains("No saved Cubemap binding"),
            "slot mismatch reason");
    }

    private static void DuplicateSavedIdentityIsRejected()
    {
        var duplicate = Identity("$", 0, 0, "Body");
        int[] mapping;
        string error;
        True(
            !MaterialCubemapIdentityMatcher.TryMatch(
                new[] { duplicate, duplicate },
                new[]
                {
                    duplicate,
                    Identity("$", 0, 1, "Body")
                },
                out mapping,
                out error),
            "duplicate saved identity rejected");
        True(error != null && error.Contains("saved"), "duplicate saved reason");
    }

    private static void DuplicateCurrentIdentityIsRejected()
    {
        var duplicate = Identity("$", 0, 0, "Body");
        int[] mapping;
        string error;
        True(
            !MaterialCubemapIdentityMatcher.TryMatch(
                new[]
                {
                    duplicate,
                    Identity("$", 0, 1, "Body")
                },
                new[] { duplicate, duplicate },
                out mapping,
                out error),
            "duplicate current identity rejected");
        True(error != null && error.Contains("current"), "duplicate current reason");
    }

    private static MaterialCubemapBindingIdentity Identity(
        string path,
        int componentIndex,
        int slot,
        string materialName)
    {
        return new MaterialCubemapBindingIdentity(
            MaterialCubemapBindingKind.Renderer,
            path,
            componentIndex,
            slot,
            materialName,
            "Cube");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException(name);
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected " + expected + ", got " + actual);
    }
}
