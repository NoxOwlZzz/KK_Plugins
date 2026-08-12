using MaterialEditorAPI;

internal static class ShaderUiModeTests
{
    internal static void Run()
    {
        ModesAreIndependentPerShader();
        BasicIsTheDefaultAndRemovesStoredState();
    }

    private static void ModesAreIndependentPerShader()
    {
        var state = new MaterialEditorShaderUiModeState();

        True(
            state.SetMode("Shader/A", MaterialEditorUiMode.Advanced),
            "first Shader/A change");
        Equal(
            MaterialEditorUiMode.Advanced,
            state.GetMode("Shader/A"),
            "Shader/A mode");
        Equal(
            MaterialEditorUiMode.Basic,
            state.GetMode("Shader/B"),
            "Shader/B remains independent");
        False(
            state.SetMode("Shader/A", MaterialEditorUiMode.Advanced),
            "same Shader/A mode is a no-op");
    }

    private static void BasicIsTheDefaultAndRemovesStoredState()
    {
        var state = new MaterialEditorShaderUiModeState();

        Equal(MaterialEditorUiMode.Basic, state.GetMode(null), "null shader");
        Equal(MaterialEditorUiMode.Basic, state.GetMode(string.Empty), "empty shader");
        Equal(MaterialEditorUiMode.Basic, state.GetMode("Shader/A"), "fresh shader");
        False(
            state.SetMode(string.Empty, MaterialEditorUiMode.Advanced),
            "empty shader is not stored");

        state.SetMode("Shader/A", MaterialEditorUiMode.Advanced);
        True(
            state.SetMode("Shader/A", MaterialEditorUiMode.Basic),
            "returning to Basic removes state");
        Equal(MaterialEditorUiMode.Basic, state.GetMode("Shader/A"), "restored Basic");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException(name + ": expected true.");
    }

    private static void False(bool value, string name)
    {
        if (value)
            throw new InvalidOperationException(name + ": expected false.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
