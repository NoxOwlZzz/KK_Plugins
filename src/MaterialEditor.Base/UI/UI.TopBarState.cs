using System;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorShaderContextKind
    {
        None,
        Single,
        Multiple
    }

    internal struct MaterialEditorShaderContextAccumulator
    {
        private bool _hasSection;
        private bool _hasMultipleShaders;
        private string _shaderName;

        internal void Add(string shaderName)
        {
            shaderName = shaderName ?? string.Empty;
            if (!_hasSection)
            {
                _hasSection = true;
                _shaderName = shaderName;
                return;
            }

            if (!string.Equals(
                    _shaderName,
                    shaderName,
                    StringComparison.Ordinal))
                _hasMultipleShaders = true;
        }

        internal MaterialEditorShaderContextKind Resolve(
            out string shaderName)
        {
            if (!_hasSection
                || (!_hasMultipleShaders
                    && string.IsNullOrEmpty(_shaderName)))
            {
                shaderName = string.Empty;
                return MaterialEditorShaderContextKind.None;
            }

            if (_hasMultipleShaders)
            {
                shaderName = string.Empty;
                return MaterialEditorShaderContextKind.Multiple;
            }

            shaderName = _shaderName;
            return MaterialEditorShaderContextKind.Single;
        }
    }
}
