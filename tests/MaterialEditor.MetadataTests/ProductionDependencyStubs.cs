using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum KeyCode
    {
        None,
        B,
        LeftControl,
        LeftShift
    }

    public class Object
    {
        private static int _nextInstanceId;
        internal static readonly List<Object> DestroyedObjects = new List<Object>();
        private readonly int _instanceId = ++_nextInstanceId;

        public static GameObject Instantiate(GameObject original) =>
            CloneGameObject(original, null);

        public static GameObject Instantiate(GameObject original, Transform parent)
        {
            return CloneGameObject(original, parent);
        }

        private static GameObject CloneGameObject(
            GameObject original,
            Transform parent)
        {
            var copy = new GameObject();
            copy.transform.parent = parent;
            if (original.GetComponent<MaterialEditorAPI.RowView>() != null)
                copy.AddComponent<MaterialEditorAPI.RowView>();
            if (original.GetComponent<MaterialEditorAPI.RowBinder>() != null)
                copy.AddComponent<MaterialEditorAPI.RowBinder>();
            return copy;
        }

        public static void Destroy(Object value)
        {
            if (value != null)
                DestroyedObjects.Add(value);
        }

        public int GetInstanceID() => _instanceId;

        internal static void ResetDestroyedObjects() => DestroyedObjects.Clear();
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public Transform transform => gameObject?.transform;
        public T GetComponent<T>() where T : class => gameObject?.GetComponent<T>();
    }

    public sealed class Coroutine { }

    public class MonoBehaviour : Component
    {
        private readonly List<IEnumerator> _pendingCoroutines = new List<IEnumerator>();

        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            _pendingCoroutines.Add(routine);
            return new Coroutine();
        }

        internal int PendingCoroutineCount => _pendingCoroutines.Count;

        internal void RunPendingCoroutines()
        {
            var pending = _pendingCoroutines.ToArray();
            _pendingCoroutines.Clear();
            foreach (var routine in pending)
                while (routine.MoveNext()) { }
        }
    }

    public class GameObject : Object
    {
        private readonly Dictionary<Type, object> _components = new Dictionary<Type, object>();

        public GameObject()
        {
            transform = new Transform { gameObject = this };
            _components[typeof(Transform)] = transform;
        }

        public Transform transform { get; private set; }

        public T AddComponent<T>() where T : class, new()
        {
            var component = new T();
            var unityComponent = component as Component;
            if (unityComponent != null)
                unityComponent.gameObject = this;
            _components[typeof(T)] = component;
            var rectTransform = component as RectTransform;
            if (rectTransform != null)
                transform = rectTransform;
            return component;
        }

        public T GetComponent<T>() where T : class
        {
            object component;
            if (_components.TryGetValue(typeof(T), out component))
                return (T)component;
            return transform as T;
        }

        public T[] GetComponentsInChildren<T>() => Array.Empty<T>();
        public void SetActive(bool value) { }
    }

    public class Transform : Component
    {
        public Transform parent { get; set; }
    }

    public class RectTransform : Transform
    {
        public Rect rect { get; set; }
        public Vector3 localPosition { get; set; }
    }

    public class Renderer : Object
    {
        public Material[] materials = Array.Empty<Material>();
    }

    public class Projector : Object { }

    public class Shader : Object
    {
        private static readonly Dictionary<string, int> PropertyIds =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public string name;

        public static int PropertyToIdCallCount { get; private set; }
        public static string ThrowOnPropertyName { get; set; }

        public static int PropertyToID(string propertyName)
        {
            PropertyToIdCallCount++;
            if (string.Equals(propertyName, ThrowOnPropertyName, StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic PropertyToID failure.");
            int id;
            if (!PropertyIds.TryGetValue(propertyName, out id))
            {
                id = PropertyIds.Count + 1;
                PropertyIds.Add(propertyName, id);
            }
            return id;
        }

        public static void ResetPropertyIds()
        {
            PropertyIds.Clear();
            PropertyToIdCallCount = 0;
            ThrowOnPropertyName = null;
        }
    }

    public class Texture : Object
    {
        public int width;
        public int height;
    }

    public class Sprite : Object { }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void ReadPixels(Rect source, int destinationX, int destinationY) { }
        public byte[] EncodeToPNG() => Array.Empty<byte>();
    }

    public class RenderTexture : Texture
    {
        public static RenderTexture active;
        public bool useMipMap;

        public RenderTexture() { }

        public RenderTexture(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
        }

        public static RenderTexture GetTemporary(
            int width,
            int height,
            int depth,
            RenderTextureFormat format,
            RenderTextureReadWrite readWrite) =>
            new RenderTexture { width = width, height = height };

        public static void ReleaseTemporary(RenderTexture texture) { }
    }

    public class Material : Object
    {
        private readonly HashSet<string> _properties = new HashSet<string>();
        private readonly Dictionary<string, Texture> _textures =
            new Dictionary<string, Texture>(StringComparer.Ordinal);
        public string name;
        public Shader shader = new Shader();
        public int renderQueue;

        public bool HasProperty(string propertyName) =>
            _properties.Contains(propertyName);

        internal void AddProperty(string propertyName) =>
            _properties.Add(propertyName);

        public Texture GetTexture(string propertyName)
        {
            Texture texture;
            return _textures.TryGetValue(propertyName, out texture)
                ? texture
                : null;
        }

        public void SetTexture(string propertyName, Texture texture)
        {
            _properties.Add(propertyName);
            _textures[propertyName] = texture;
        }
    }

    public readonly struct Rect
    {
        public Rect(float x, float y, float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public readonly float width;
        public readonly float height;
    }

    public struct Vector3
    {
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x;
        public float y;
        public float z;
    }

    public readonly struct Color
    {
        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public readonly float r;
        public readonly float g;
        public readonly float b;
        public readonly float a;
    }

    public readonly struct Vector2
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public readonly float x;
        public readonly float y;
    }

    public readonly struct Vector4
    {
        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public readonly float x;
        public readonly float y;
        public readonly float z;
        public readonly float w;
    }

    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum TextureFormat { ARGB32 }
    public enum RenderTextureFormat { Default }
    public enum RenderTextureReadWrite { Default }

    public static class Mathf
    {
        public static int Clamp(int value, int minimum, int maximum) =>
            Math.Min(Math.Max(value, minimum), maximum);
        public static float Clamp(float value, float minimum, float maximum) =>
            Math.Min(Math.Max(value, minimum), maximum);
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static int RoundToInt(float value) => (int)Math.Round(value);
        public static bool Approximately(float left, float right) =>
            Math.Abs(left - right) <= 0.00001f;
        public static int Max(int left, int right) => Math.Max(left, right);
        public static float Max(float left, float right) => Math.Max(left, right);
        public static int Min(int left, int right) => Math.Min(left, right);
    }

    public sealed class RectOffset
    {
        public int top;
        public int bottom;
    }

    public static class GL
    {
        public static void Clear(bool clearDepth, bool clearColor, Color color) { }
    }

    public static class Graphics
    {
        public static void Blit(Texture source, RenderTexture destination) { }
    }
}

namespace KK_Plugins
{
    using UnityEngine;

    internal static class ImageHelper
    {
        internal static Texture2D LoadTexture2DFromBytes(
            byte[] data,
            TextureFormat format,
            bool mipmaps) => new Texture2D(4, 4);
    }

    internal static class TextureTestExtensions
    {
        internal static bool SequenceEqualFast(this byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (var index = 0; index < left.Length; index++)
                if (left[index] != right[index])
                    return false;
            return true;
        }
    }
}

namespace UnityEngine.EventSystems
{
    using UnityEngine;

    public sealed class EventSystem
    {
        public static EventSystem current;
        public GameObject currentSelectedGameObject;

        public void SetSelectedGameObject(GameObject value) =>
            currentSelectedGameObject = value;
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T>(T value);

    public sealed class UnityEvent
    {
        private readonly List<UnityAction> _listeners = new List<UnityAction>();
        public int ListenerCount => _listeners.Count;
        public void AddListener(UnityAction listener) => _listeners.Add(listener);
        public void RemoveListener(UnityAction listener) => _listeners.Remove(listener);
        public void Invoke()
        {
            foreach (var listener in _listeners.ToArray())
                listener();
        }
    }

    public sealed class UnityEvent<T>
    {
        private readonly List<UnityAction<T>> _listeners = new List<UnityAction<T>>();
        public int ListenerCount => _listeners.Count;
        public void AddListener(UnityAction<T> listener) => _listeners.Add(listener);
        public void RemoveListener(UnityAction<T> listener) => _listeners.Remove(listener);
        public void Invoke(T value)
        {
            foreach (var listener in _listeners.ToArray())
                listener(value);
        }
    }
}

namespace UnityEngine.Profiling
{
    public static class Profiler
    {
        public static void BeginSample(string name) { }
        public static void EndSample() { }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine;
    using UnityEngine.Events;

    public sealed class ScrollRect : Component
    {
        public RectTransform content;
        public RectTransform viewport;
        public int StopMovementCount { get; private set; }

        public void StopMovement() => StopMovementCount++;
    }

    public sealed class VerticalLayoutGroup : Component
    {
        public RectOffset padding = new RectOffset();
    }

    public sealed class Button : Component
    {
        public UnityEvent onClick { get; } = new UnityEvent();
        public void Select() { }
    }

    public sealed class Toggle : Component
    {
        public bool isOn;
        public UnityEvent<bool> onValueChanged { get; } = new UnityEvent<bool>();
    }

    public sealed class Dropdown : Component
    {
        public sealed class OptionData
        {
            public OptionData() { }
            public OptionData(string text) => this.text = text;

            public string text;
            public Sprite image;
        }

        public List<OptionData> options { get; } = new List<OptionData>();
        public int value;
        public string captionText;
        public UnityEvent<int> onValueChanged { get; } = new UnityEvent<int>();
    }

    public sealed class InputField : Component
    {
        public UnityEvent<string> onEndEdit { get; } = new UnityEvent<string>();
    }

    public sealed class Slider : Component
    {
        public UnityEvent<float> onValueChanged { get; } = new UnityEvent<float>();
    }

    public static class LayoutRebuilder
    {
        public static int MarkCount { get; private set; }
        public static void MarkLayoutForRebuild(RectTransform target) => MarkCount++;
        public static void Reset() => MarkCount = 0;
    }
}

namespace UILib
{
    using UnityEngine.UI;

    public static class Extensions
    {
        public static void Set(this Dropdown dropdown, int value)
        {
            dropdown.value = value;
            dropdown.captionText = value >= 0 && value < dropdown.options.Count
                ? dropdown.options[value].text
                : null;
        }
    }
}

namespace BepInEx.Logging
{
    public sealed class ManualLogSource
    {
        public void LogError(object value) { }
        public void LogMessage(object value) { }
        public void LogWarning(object value) { }
    }
}

namespace BepInEx.Configuration
{
    using UnityEngine;

    public struct KeyboardShortcut
    {
        public KeyboardShortcut(KeyCode mainKey, params KeyCode[] modifiers) { }
    }

    public class ConfigEntryBase { }

    public sealed class ConfigEntry<T> : ConfigEntryBase
    {
        public ConfigEntry(T value) => Value = value;
        public T Value { get; set; }
        public event EventHandler SettingChanged;
        internal void RaiseSettingChanged() => SettingChanged?.Invoke(this, EventArgs.Empty);
    }

    public sealed class ConfigDescription
    {
        public ConfigDescription(string description, object acceptableValues = null, params object[] tags) { }
    }

    public sealed class AcceptableValueRange<T>
    {
        public AcceptableValueRange(T minimum, T maximum) { }
    }

    public sealed class ConfigFile
    {
        public ConfigEntry<T> Bind<T>(string section, string key, T value, object description) =>
            new ConfigEntry<T>(value);
    }
}

namespace BepInEx
{
    using BepInEx.Configuration;
    using BepInEx.Logging;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class BepInDependencyAttribute : Attribute
    {
        public BepInDependencyAttribute(string identifier, string version) { }
    }

    public static class Paths
    {
        public static string GameRootPath => string.Empty;
    }

    public class BaseUnityPlugin
    {
        protected ManualLogSource Logger { get; } = new ManualLogSource();
        protected ConfigFile Config { get; } = new ConfigFile();
        protected static void DestroyImmediate(object value) { }
    }
}

namespace BepInEx.Bootstrap
{
    internal static class Placeholder { }
}

namespace XUnity.ResourceRedirector
{
    public static class Constants
    {
        public static class PluginData
        {
            public const string Identifier = "test.resource.redirector";
            public const string Version = "0";
        }
    }

    public enum HookBehaviour { OneCallbackPerResourceLoaded }

    public sealed class AssetLoadedContext
    {
        public object Asset { get; set; }
    }

    public static class ResourceRedirection
    {
        public static void RegisterAssetLoadedHook(
            HookBehaviour behaviour,
            Action<AssetLoadedContext> callback) { }
    }
}

namespace MaterialEditorAPI
{
    using UnityEngine;

    internal static class RowLayoutRuntimeAssertions
    {
        internal static void Validate(RowView row) { }
        internal static void ValidateClones(RowView first, RowView second) { }
    }

    public static class MaterialAPI
    {
        public enum ShaderPropertyType
        {
            Texture = 0,
            Color = 1,
            Float = 2,
            Keyword = 3,
            Cubemap = 5,
            Vector = 4
        }

        internal static List<Material> GetObjectMaterials(
            GameObject gameObject,
            string materialName) => new List<Material>();

        public enum ProjectorProperties
        {
            NearClipPlane,
            FarClipPlane,
            FieldOfView,
            AspectRatio,
            Orthographic,
            OrthographicSize
        }
    }

    internal static class MaterialPropertyAccess
    {
        internal static bool HasProperty(
            Material material,
            MaterialPropertyHandle property) =>
            material != null && material.HasProperty(property.FullName);

        internal static Texture GetTexture(
            Material material,
            MaterialPropertyHandle property) =>
            material == null ? null : material.GetTexture(property.FullName);

        internal static void SetTexture(
            Material material,
            MaterialPropertyHandle property,
            Texture value)
        {
            if (material != null)
                material.SetTexture(property.FullName, value);
        }
    }

    internal static class MaterialNameTestExtensions
    {
        internal static string NameFormatted(this Material material) =>
            material == null || material.name == null
                ? string.Empty
                : material.name.Replace("(Instance)", string.Empty)
                    .Replace(" Instance", string.Empty)
                    .Trim();
    }

    public sealed class MaterialEditService { }

    public sealed class MaterialEditorEditService
    {
        internal MaterialEditorEditService(
            MaterialEditService service,
            GameObject gameObject,
            object data) { }
    }

    internal enum MaterialEditorLabelType
    {
        Renderer,
        Material,
        Shader,
        Property
    }

    internal class RowModel
    {
        internal RowModel(RowItemType itemType, string labelText)
        {
            ItemType = itemType;
            LabelText = labelText;
        }

        internal RowItemType ItemType;
        internal string LabelText;
        internal GameObject GameObject;
        internal object Data;
        internal Renderer Renderer;
        internal Material Material;
        internal Projector Projector;
        internal string PropertyName;
        internal MaterialEditorPropertyDescriptor PublicDescriptor;
        internal bool Enabled = true;

        internal enum RowItemType
        {
            Renderer,
            RendererEnabled,
            RendererShadowCastingMode,
            RendererReceiveShadows,
            RendererUpdateWhenOffscreen,
            RendererRecalculateNormals,
            Material,
            Shader,
            ShaderRenderQueue,
            PropertyCategory,
            TextureProperty,
            TextureOffsetScale,
            ColorProperty,
            FloatProperty,
            KeywordProperty,
            EnumProperty,
            VectorProperty,
            FloatToggleProperty
        }
    }

    internal sealed class RendererRowModel : RowModel
    {
        internal RendererRowModel() : base(RowItemType.Renderer, "Renderer") { }
    }

    internal sealed class MaterialRowModel : RowModel
    {
        internal MaterialRowModel() : base(RowItemType.Material, "Material") { }
    }

    internal sealed class ShaderRowModel : RowModel
    {
        internal ShaderRowModel() : base(RowItemType.Shader, "Shader") { }
        internal string ShaderName;
    }

    internal sealed class PropertyCategoryRowModel : RowModel
    {
        internal PropertyCategoryRowModel(string labelText)
            : base(RowItemType.PropertyCategory, labelText) { }
    }

    internal sealed class TestPropertyRowModel : RowModel
    {
        internal TestPropertyRowModel(
            RowItemType itemType,
            string labelText,
            string propertyName)
            : base(itemType, labelText)
        {
            PropertyName = propertyName;
        }
    }

    internal sealed class RowBinder : UnityEngine.Component { }

    internal sealed class RowView : UnityEngine.Component
    {
        internal RowModel CurrentModel { get; private set; }
        internal bool Visible { get; private set; }
        internal bool ListenersActive { get; private set; }
        internal int BindCount { get; private set; }
        internal int ReleaseCount { get; private set; }

        internal void Initialize(RowBinder binder) { }
        internal void Bind(RowModel model, bool force)
        {
            CurrentModel = model;
            ListenersActive = model != null;
            BindCount++;
        }
        internal void SetVisible(bool value) => Visible = value;
        internal void SuspendListeners() => ListenersActive = false;
        internal void Release()
        {
            CurrentModel = null;
            ListenersActive = false;
            Visible = false;
            ReleaseCount++;
        }
    }

    internal static class MaterialEditorUI
    {
        internal const float PanelHeight = MaterialEditorTheme.Metrics.RowHeight;
        internal static IDisposable TexChangeWatcher;
        internal static void DisposeTexChangeWatcher()
        {
            var watcher = TexChangeWatcher;
            TexChangeWatcher = null;
            watcher?.Dispose();
        }
        internal static void UISettingChanged(object sender, EventArgs eventArgs) { }
    }

    internal static class StringCompatibilityExtensions
    {
        internal static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);
        internal static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);
    }
}
