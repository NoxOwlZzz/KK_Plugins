using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorUiMode
    {
        Basic,
        Advanced
    }

    /// <summary>
    /// Keeps the unpersisted Basic/Advanced preference independent for each
    /// shader. Basic is represented by absence so the state stays compact.
    /// </summary>
    internal sealed class MaterialEditorShaderUiModeState
    {
        private readonly HashSet<string> _advancedShaders =
            new HashSet<string>(StringComparer.Ordinal);

        internal MaterialEditorUiMode GetMode(string shaderName)
        {
            return !string.IsNullOrEmpty(shaderName)
                   && _advancedShaders.Contains(shaderName)
                ? MaterialEditorUiMode.Advanced
                : MaterialEditorUiMode.Basic;
        }

        internal bool SetMode(string shaderName, MaterialEditorUiMode mode)
        {
            if (string.IsNullOrEmpty(shaderName))
                return false;

            return mode == MaterialEditorUiMode.Advanced
                ? _advancedShaders.Add(shaderName)
                : _advancedShaders.Remove(shaderName);
        }
    }

    internal static class MaterialEditorAdvancedPropertyPresentation
    {
        internal const string Marker = "[A] ";

        internal static string FormatLabel(string label, bool advanced)
        {
            var text = label ?? string.Empty;
            return advanced ? Marker + text : text;
        }
    }
}
