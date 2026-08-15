namespace MaterialEditorAPI
{
    internal static class MaterialEditorPropertyEditorIds
    {
        internal const string Float = "materialeditor.float";
        internal const string Color = "materialeditor.color";
        internal const string Boolean = "materialeditor.boolean";
        internal const string Texture = "materialeditor.texture";
        internal const string Cubemap = "materialeditor.cubemap";
        internal const string Enum = "materialeditor.enum";
        internal const string Vector2 = "materialeditor.vector2";
        internal const string Vector3 = "materialeditor.vector3";
        internal const string Vector4 = "materialeditor.vector4";
        internal const string Toggle = "materialeditor.toggle";
    }

    internal static class MaterialAPI
    {
        internal enum ShaderPropertyType
        {
            Texture,
            Color,
            Float,
            Keyword,
            Vector,
            Cubemap
        }
    }
}
