using System.Text.Json;

namespace ExtensibleSaveFormat
{
    internal sealed class PluginData
    {
        internal int version;
        internal Dictionary<string, object> data = new Dictionary<string, object>();
    }
}

namespace MessagePack
{
    internal static class MessagePackSerializer
    {
        internal static byte[] Serialize<T>(T value) =>
            JsonSerializer.SerializeToUtf8Bytes(value);

        internal static T Deserialize<T>(byte[] data) =>
            JsonSerializer.Deserialize<T>(data);
    }
}

namespace KK_Plugins.MaterialEditor
{
    using ExtensibleSaveFormat;

    internal static class MEStudio
    {
        internal static readonly SceneControllerStub SceneController = new SceneControllerStub();

        internal static SceneControllerStub GetSceneController() => SceneController;
    }

    internal sealed class SceneControllerStub
    {
        internal PluginData ExtendedData { get; set; }

        internal PluginData GetExtendedData() => ExtendedData;
    }
}
