using MaterialEditorAPI;

internal static class CubemapContentKeyTests
{
    internal static void Run()
    {
        SameContentHasSameShaButKeysRemainSourceBound();
        NullDataIsRejected();
    }

    private static void SameContentHasSameShaButKeysRemainSourceBound()
    {
        var firstBytes = new byte[] { 1, 2, 3, 4 };
        var secondBytes = new byte[] { 1, 2, 3, 4 };
        MaterialEditorCubemapContentKey first;
        MaterialEditorCubemapContentKey second;
        string error;
        True(
            MaterialEditorCubemapContentKey.TryCompute(firstBytes, out first, out error),
            "first content key");
        Equal(null, error, "first content key error");
        True(
            MaterialEditorCubemapContentKey.TryCompute(secondBytes, out second, out error),
            "second content key");
        Equal(first.Value, second.Value, "same content SHA-256");
        True(first.Matches(firstBytes), "key matches source instance");
        True(!first.Matches(secondBytes), "key rejects different source instance");
    }

    private static void NullDataIsRejected()
    {
        MaterialEditorCubemapContentKey key;
        string error;
        True(
            !MaterialEditorCubemapContentKey.TryCompute(null, out key, out error),
            "null content key rejected");
        Equal(null, key, "null content key result");
        True(error != null && error.Contains("no data"), "null content key reason");
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
