using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MaterialEditorAPI
{
    // Cross-window notification for copy operations routed through the stable
    // edit facade. Direct mutations of the public CopyData lists are detected by
    // MaterialEditorClipboardSnapshot without allocations while a window is active.
    internal static class MaterialEditorClipboardState
    {
        internal static event Action Changed;

        internal static void NotifyChanged()
        {
            var changed = Changed;
            if (changed != null)
                changed();
        }

        internal static CopyContainer EnsureClipboard()
        {
            var clipboard = MaterialEditorPluginBase.CopyData;
            if (clipboard != null)
                return clipboard;
            clipboard = new CopyContainer();
            MaterialEditorPluginBase.CopyData = clipboard;
            return clipboard;
        }
    }

    internal sealed class MaterialEditorClipboardSnapshot
    {
        private CopyContainer _container;
        private object _floats;
        private object _keywords;
        private object _colors;
        private object _vectors;
        private object _textures;
        private object _cubemaps;
        private object _shaders;
        private object _projectors;
        private int _floatCount;
        private int _keywordCount;
        private int _colorCount;
        private int _vectorCount;
        private int _textureCount;
        private int _cubemapCount;
        private int _shaderCount;
        private int _projectorCount;
        private ulong _floatFingerprint;
        private ulong _keywordFingerprint;
        private ulong _colorFingerprint;
        private ulong _vectorFingerprint;
        private ulong _textureFingerprint;
        private ulong _cubemapFingerprint;
        private ulong _shaderFingerprint;
        private ulong _projectorFingerprint;

        internal bool Capture(CopyContainer container)
        {
            var floats = container == null ? null : container.MaterialFloatPropertyList;
            var keywords = container == null ? null : container.MaterialKeywordPropertyList;
            var colors = container == null ? null : container.MaterialColorPropertyList;
            var vectors = container == null ? null : container.MaterialVectorPropertyList;
            var textures = container == null ? null : container.MaterialTexturePropertyList;
            var cubemaps = container == null ? null : container.MaterialCubemapPropertyList;
            var shaders = container == null ? null : container.MaterialShaderList;
            var projectors = container == null ? null : container.ProjectorPropertyList;
            var floatCount = Count(floats);
            var keywordCount = Count(keywords);
            var colorCount = Count(colors);
            var vectorCount = Count(vectors);
            var textureCount = Count(textures);
            var cubemapCount = Count(cubemaps);
            var shaderCount = Count(shaders);
            var projectorCount = Count(projectors);
            var floatFingerprint = FingerprintFloats(floats);
            var keywordFingerprint = FingerprintKeywords(keywords);
            var colorFingerprint = FingerprintColors(colors);
            var vectorFingerprint = FingerprintVectors(vectors);
            var textureFingerprint = FingerprintTextures(textures);
            var cubemapFingerprint = FingerprintCubemaps(cubemaps);
            var shaderFingerprint = FingerprintShaders(shaders);
            var projectorFingerprint = FingerprintProjectors(projectors);

            var changed = !ReferenceEquals(_container, container)
                          || !ReferenceEquals(_floats, floats)
                          || !ReferenceEquals(_keywords, keywords)
                          || !ReferenceEquals(_colors, colors)
                          || !ReferenceEquals(_vectors, vectors)
                          || !ReferenceEquals(_textures, textures)
                          || !ReferenceEquals(_cubemaps, cubemaps)
                          || !ReferenceEquals(_shaders, shaders)
                          || !ReferenceEquals(_projectors, projectors)
                          || _floatCount != floatCount
                          || _keywordCount != keywordCount
                          || _colorCount != colorCount
                          || _vectorCount != vectorCount
                          || _textureCount != textureCount
                          || _cubemapCount != cubemapCount
                          || _shaderCount != shaderCount
                          || _projectorCount != projectorCount
                          || _floatFingerprint != floatFingerprint
                          || _keywordFingerprint != keywordFingerprint
                          || _colorFingerprint != colorFingerprint
                          || _vectorFingerprint != vectorFingerprint
                          || _textureFingerprint != textureFingerprint
                          || _cubemapFingerprint != cubemapFingerprint
                          || _shaderFingerprint != shaderFingerprint
                          || _projectorFingerprint != projectorFingerprint;

            _container = container;
            _floats = floats;
            _keywords = keywords;
            _colors = colors;
            _vectors = vectors;
            _textures = textures;
            _cubemaps = cubemaps;
            _shaders = shaders;
            _projectors = projectors;
            _floatCount = floatCount;
            _keywordCount = keywordCount;
            _colorCount = colorCount;
            _vectorCount = vectorCount;
            _textureCount = textureCount;
            _cubemapCount = cubemapCount;
            _shaderCount = shaderCount;
            _projectorCount = projectorCount;
            _floatFingerprint = floatFingerprint;
            _keywordFingerprint = keywordFingerprint;
            _colorFingerprint = colorFingerprint;
            _vectorFingerprint = vectorFingerprint;
            _textureFingerprint = textureFingerprint;
            _cubemapFingerprint = cubemapFingerprint;
            _shaderFingerprint = shaderFingerprint;
            _projectorFingerprint = projectorFingerprint;
            return changed;
        }

        private static int Count<T>(ICollection<T> values)
        {
            return values == null ? 0 : values.Count;
        }

        private static ulong FingerprintFloats(
            IList<CopyContainer.MaterialFloatProperty> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = MixString(hash, value.Property);
                hash = Mix(hash, value.Value.GetHashCode());
            }
            return hash;
        }

        private static ulong FingerprintKeywords(
            IList<CopyContainer.MaterialKeywordProperty> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = MixString(hash, value.Property);
                hash = Mix(hash, value.Value ? 1 : 0);
            }
            return hash;
        }

        private static ulong FingerprintColors(
            IList<CopyContainer.MaterialColorProperty> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = MixString(hash, value.Property);
                hash = Mix(hash, value.Value.GetHashCode());
            }
            return hash;
        }

        private static ulong FingerprintVectors(
            IList<CopyContainer.MaterialVectorProperty> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = MixString(hash, value.Property);
                hash = Mix(hash, value.Value.GetHashCode());
            }
            return hash;
        }

        private static ulong FingerprintTextures(
            IList<CopyContainer.MaterialTextureProperty> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = MixString(hash, value.Property);
                hash = MixIdentity(hash, value.Data);
                hash = Mix(hash, value.Data == null ? -1 : value.Data.Length);
                hash = MixNullable(hash, value.Offset);
                hash = MixNullable(hash, value.Scale);
            }
            return hash;
        }

        private static ulong FingerprintCubemaps(
            IList<CopyContainer.MaterialCubemapProperty> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = MixString(hash, value.Property);
                hash = MixIdentity(hash, value.Data);
                hash = Mix(hash, value.Data == null ? -1 : value.Data.Length);
            }
            return hash;
        }

        private static ulong FingerprintShaders(
            IList<CopyContainer.MaterialShader> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = MixString(hash, value.ShaderName);
                hash = MixNullable(hash, value.RenderQueue);
            }
            return hash;
        }

        private static ulong FingerprintProjectors(
            IList<CopyContainer.ProjectorProperty> values)
        {
            var hash = BeginFingerprint(values == null ? 0 : values.Count);
            if (values == null)
                return hash;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                hash = MixIdentity(hash, value);
                if (value == null)
                    continue;
                hash = Mix(hash, (int)value.Property);
                hash = Mix(hash, value.Value.GetHashCode());
            }
            return hash;
        }

        private static ulong BeginFingerprint(int count)
        {
            return Mix(14695981039346656037UL, count);
        }

        private static ulong Mix(ulong hash, int value)
        {
            unchecked
            {
                return (hash ^ (uint)value) * 1099511628211UL;
            }
        }

        private static ulong MixIdentity(ulong hash, object value)
        {
            hash = Mix(hash, value == null ? 0 : 1);
            return value == null
                ? hash
                : Mix(hash, RuntimeHelpers.GetHashCode(value));
        }

        private static ulong MixString(ulong hash, string value)
        {
            hash = Mix(hash, value == null ? -1 : value.Length);
            if (value == null)
                return hash;
            for (var index = 0; index < value.Length; index++)
                hash = Mix(hash, value[index]);
            return hash;
        }

        private static ulong MixNullable(ulong hash, int? value)
        {
            hash = Mix(hash, value.HasValue ? 1 : 0);
            return value.HasValue ? Mix(hash, value.Value) : hash;
        }

        private static ulong MixNullable(ulong hash, UnityEngine.Vector2? value)
        {
            hash = Mix(hash, value.HasValue ? 1 : 0);
            return value.HasValue
                ? Mix(hash, value.Value.GetHashCode())
                : hash;
        }
    }

    /// <summary>
    /// Temporarily presents paste repositories with non-null, null-free
    /// lists and restores the public clipboard references on every exit path.
    /// </summary>
    internal sealed class MaterialEditorClipboardPasteLease : IDisposable
    {
        private CopyContainer _clipboard;
        private readonly List<CopyContainer.MaterialFloatProperty> _floats;
        private readonly List<CopyContainer.MaterialKeywordProperty> _keywords;
        private readonly List<CopyContainer.MaterialColorProperty> _colors;
        private readonly List<CopyContainer.MaterialVectorProperty> _vectors;
        private readonly List<CopyContainer.MaterialTextureProperty> _textures;
        private readonly List<CopyContainer.MaterialCubemapProperty> _cubemaps;
        private readonly List<CopyContainer.MaterialShader> _shaders;
        private readonly List<CopyContainer.ProjectorProperty> _projectors;

        internal MaterialEditorClipboardPasteLease(
            CopyContainer clipboard,
            bool isolateProjectorEdits)
        {
            if (clipboard == null)
                throw new ArgumentNullException(nameof(clipboard));

            _clipboard = clipboard;
            _floats = clipboard.MaterialFloatPropertyList;
            _keywords = clipboard.MaterialKeywordPropertyList;
            _colors = clipboard.MaterialColorPropertyList;
            _vectors = clipboard.MaterialVectorPropertyList;
            _textures = clipboard.MaterialTexturePropertyList;
            _cubemaps = clipboard.MaterialCubemapPropertyList;
            _shaders = clipboard.MaterialShaderList;
            _projectors = clipboard.ProjectorPropertyList;

            clipboard.MaterialFloatPropertyList = Sanitize(_floats);
            clipboard.MaterialKeywordPropertyList = Sanitize(_keywords);
            clipboard.MaterialColorPropertyList = Sanitize(_colors);
            clipboard.MaterialVectorPropertyList = Sanitize(_vectors);
            clipboard.MaterialTexturePropertyList = Sanitize(_textures);
            clipboard.MaterialCubemapPropertyList = Sanitize(_cubemaps);
            clipboard.MaterialShaderList = Sanitize(_shaders);
            ProjectorEdits = Sanitize(_projectors);
            clipboard.ProjectorPropertyList = isolateProjectorEdits
                ? new List<CopyContainer.ProjectorProperty>()
                : ProjectorEdits;
        }

        internal List<CopyContainer.ProjectorProperty> ProjectorEdits { get; }

        public void Dispose()
        {
            var clipboard = _clipboard;
            if (clipboard == null)
                return;
            _clipboard = null;
            clipboard.MaterialFloatPropertyList = _floats;
            clipboard.MaterialKeywordPropertyList = _keywords;
            clipboard.MaterialColorPropertyList = _colors;
            clipboard.MaterialVectorPropertyList = _vectors;
            clipboard.MaterialTexturePropertyList = _textures;
            clipboard.MaterialCubemapPropertyList = _cubemaps;
            clipboard.MaterialShaderList = _shaders;
            clipboard.ProjectorPropertyList = _projectors;
        }

        private static List<T> Sanitize<T>(List<T> source) where T : class
        {
            if (source == null)
                return new List<T>();
            for (var index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                    continue;
                var filtered = new List<T>(source.Count - 1);
                for (var copyIndex = 0; copyIndex < source.Count; copyIndex++)
                {
                    var value = source[copyIndex];
                    if (value != null)
                        filtered.Add(value);
                }
                return filtered;
            }
            return source;
        }
    }
}
