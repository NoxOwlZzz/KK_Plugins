using System;
using System.Security.Cryptography;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Opaque content key computed from one immutable byte-array instance. This
    /// type is Unity-free; SHA-256 work may be performed on a worker thread.
    /// </summary>
    internal sealed class MaterialEditorCubemapContentKey
    {
        private readonly byte[] _source;

        private MaterialEditorCubemapContentKey(byte[] source, string value)
        {
            _source = source;
            Value = value;
        }

        internal string Value { get; private set; }

        internal bool Matches(byte[] source)
        {
            return ReferenceEquals(_source, source);
        }

        internal static bool TryCompute(
            byte[] pngData,
            out MaterialEditorCubemapContentKey key,
            out string error)
        {
            key = null;
            error = null;
            if (pngData == null)
            {
                error = "The selected PNG contains no data.";
                return false;
            }

            try
            {
                string value;
                using (var sha256 = SHA256.Create())
                    value = Convert.ToBase64String(sha256.ComputeHash(pngData));
                key = new MaterialEditorCubemapContentKey(pngData, value);
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not hash the Cubemap source: " + exception.Message;
                return false;
            }
        }
    }
}
