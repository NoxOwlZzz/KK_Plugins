using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static class VirtualListRowAnchorResolver
    {
        internal sealed class TopRowAnchor
        {
            internal readonly List<RowIdentity> Rows;
            internal readonly int TopRowIndex;
            internal readonly float OffsetWithinRow;

            internal TopRowAnchor(
                List<RowIdentity> rows,
                int topRowIndex,
                float offsetWithinRow)
            {
                Rows = rows;
                TopRowIndex = topRowIndex;
                OffsetWithinRow = offsetWithinRow;
            }
        }

        internal static TopRowAnchor Capture(
            IList<RowModel> rows,
            float scrollPosition,
            float rowHeight)
        {
            if (rows.Count == 0)
                return null;

            scrollPosition = Mathf.Max(0f, scrollPosition);
            var topRowIndex = Mathf.Clamp(
                Mathf.FloorToInt(scrollPosition / rowHeight),
                0,
                rows.Count - 1);
            return new TopRowAnchor(
                CreateRowIdentities(rows),
                topRowIndex,
                scrollPosition - topRowIndex * rowHeight);
        }

        internal static int ResolveRestoreIndex(
            TopRowAnchor anchor,
            IList<RowModel> rows)
        {
            var identities = CreateRowIdentities(rows);
            var currentRows = new Dictionary<RowIdentity, int>();
            for (var index = 0; index < identities.Count; index++)
                if (!currentRows.ContainsKey(identities[index]))
                    currentRows.Add(identities[index], index);
            return FindRestoreIndex(anchor, currentRows);
        }

        private static int FindRestoreIndex(
            TopRowAnchor anchor,
            IDictionary<RowIdentity, int> currentRows)
        {
            int targetIndex;
            var topIdentity = anchor.Rows[anchor.TopRowIndex];
            if (currentRows.TryGetValue(topIdentity, out targetIndex))
                return targetIndex;

            if (TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => candidate.HasSameCategory(topIdentity),
                    out targetIndex)
                || TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => candidate.HasSameShader(topIdentity),
                    out targetIndex)
                || TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => candidate.HasSameMaterial(topIdentity),
                    out targetIndex)
                || TryFindNearest(
                    anchor,
                    currentRows,
                    candidate => true,
                    out targetIndex))
                return targetIndex;

            return Mathf.Clamp(anchor.TopRowIndex, 0, currentRows.Count - 1);
        }

        private static bool TryFindNearest(
            TopRowAnchor anchor,
            IDictionary<RowIdentity, int> currentRows,
            Func<RowIdentity, bool> scopeMatches,
            out int targetIndex)
        {
            for (var distance = 1; distance < anchor.Rows.Count; distance++)
            {
                var previous = anchor.TopRowIndex - distance;
                if (previous >= 0
                    && scopeMatches(anchor.Rows[previous])
                    && currentRows.TryGetValue(
                        anchor.Rows[previous],
                        out targetIndex))
                    return true;

                var next = anchor.TopRowIndex + distance;
                if (next < anchor.Rows.Count
                    && scopeMatches(anchor.Rows[next])
                    && currentRows.TryGetValue(
                        anchor.Rows[next],
                        out targetIndex))
                    return true;
            }

            targetIndex = -1;
            return false;
        }

        private static List<RowIdentity> CreateRowIdentities(
            IList<RowModel> rows)
        {
            var result = new List<RowIdentity>(rows.Count);
            var occurrences = new Dictionary<RowIdentityKey, int>();
            GameObject currentGameObject = null;
            Renderer currentRenderer = null;
            Material currentMaterial = null;
            Projector currentProjector = null;
            string currentShader = null;
            string currentCategory = null;

            foreach (var row in rows)
            {
                var rendererRow = row as RendererRowModel;
                if (rendererRow != null)
                {
                    currentGameObject = row.GameObject;
                    currentRenderer = row.Renderer;
                    currentMaterial = null;
                    currentProjector = null;
                    currentShader = null;
                    currentCategory = null;
                }

                var materialRow = row as MaterialRowModel;
                if (materialRow != null)
                {
                    currentGameObject = row.GameObject;
                    currentRenderer = null;
                    currentMaterial = row.Material;
                    currentProjector = row.Projector;
                    currentShader = null;
                    currentCategory = null;
                }

                var shaderRow = row as ShaderRowModel;
                if (shaderRow != null)
                {
                    currentShader = shaderRow.ShaderName;
                    currentCategory = null;
                }

                if (row is PropertyCategoryRowModel)
                    currentCategory = row.LabelText;

                var key = new RowIdentityKey(
                    row.ItemType,
                    ReferenceEquals(row.GameObject, null)
                        ? currentGameObject
                        : row.GameObject,
                    ReferenceEquals(row.Renderer, null)
                        ? currentRenderer
                        : row.Renderer,
                    ReferenceEquals(row.Material, null)
                        ? currentMaterial
                        : row.Material,
                    ReferenceEquals(row.Projector, null)
                        ? currentProjector
                        : row.Projector,
                    currentShader,
                    currentCategory,
                    row.PropertyName,
                    row.PublicDescriptor == null
                        ? null
                        : row.PublicDescriptor.Id,
                    row.LabelText);
                int occurrence;
                occurrences.TryGetValue(key, out occurrence);
                occurrences[key] = occurrence + 1;
                result.Add(new RowIdentity(key, occurrence));
            }

            return result;
        }

        internal sealed class RowIdentity : IEquatable<RowIdentity>
        {
            private readonly RowIdentityKey _key;
            private readonly int _occurrence;

            internal RowIdentity(RowIdentityKey key, int occurrence)
            {
                _key = key;
                _occurrence = occurrence;
            }

            internal bool HasSameCategory(RowIdentity other)
            {
                return other != null
                       && !string.IsNullOrEmpty(_key.Category)
                       && HasSameShader(other)
                       && StringComparer.Ordinal.Equals(
                           _key.Category,
                           other._key.Category);
            }

            internal bool HasSameShader(RowIdentity other)
            {
                return other != null
                       && !string.IsNullOrEmpty(_key.Shader)
                       && HasSameMaterial(other)
                       && StringComparer.Ordinal.Equals(
                           _key.Shader,
                           other._key.Shader);
            }

            internal bool HasSameMaterial(RowIdentity other)
            {
                return other != null
                       && !ReferenceEquals(_key.Material, null)
                       && ReferenceEquals(_key.Material, other._key.Material)
                       && ReferenceEquals(
                           _key.Projector,
                           other._key.Projector);
            }

            public bool Equals(RowIdentity other)
            {
                return other != null
                       && _occurrence == other._occurrence
                       && _key.Equals(other._key);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as RowIdentity);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return _key.GetHashCode() * 397 ^ _occurrence;
                }
            }
        }

        internal sealed class RowIdentityKey : IEquatable<RowIdentityKey>
        {
            internal RowIdentityKey(
                RowModel.RowItemType itemType,
                GameObject gameObject,
                Renderer renderer,
                Material material,
                Projector projector,
                string shader,
                string category,
                string propertyName,
                string descriptorId,
                string labelText)
            {
                ItemType = itemType;
                GameObject = gameObject;
                Renderer = renderer;
                Material = material;
                Projector = projector;
                Shader = shader;
                Category = category;
                PropertyName = propertyName;
                DescriptorId = descriptorId;
                LabelText = labelText;
            }

            private RowModel.RowItemType ItemType { get; }
            private GameObject GameObject { get; }
            private Renderer Renderer { get; }
            internal Material Material { get; }
            internal Projector Projector { get; }
            internal string Shader { get; }
            internal string Category { get; }
            private string PropertyName { get; }
            private string DescriptorId { get; }
            private string LabelText { get; }

            public bool Equals(RowIdentityKey other)
            {
                return other != null
                       && ItemType == other.ItemType
                       && ReferenceEquals(GameObject, other.GameObject)
                       && ReferenceEquals(Renderer, other.Renderer)
                       && ReferenceEquals(Material, other.Material)
                       && ReferenceEquals(Projector, other.Projector)
                       && StringComparer.Ordinal.Equals(
                           Shader,
                           other.Shader)
                       && StringComparer.Ordinal.Equals(
                           Category,
                           other.Category)
                       && StringComparer.Ordinal.Equals(
                           PropertyName,
                           other.PropertyName)
                       && StringComparer.Ordinal.Equals(
                           DescriptorId,
                           other.DescriptorId)
                       && StringComparer.Ordinal.Equals(
                           LabelText,
                           other.LabelText);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as RowIdentityKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (int)ItemType;
                    hash = hash * 397 ^ ReferenceHash(GameObject);
                    hash = hash * 397 ^ ReferenceHash(Renderer);
                    hash = hash * 397 ^ ReferenceHash(Material);
                    hash = hash * 397 ^ ReferenceHash(Projector);
                    hash = hash * 397 ^ StringHash(Shader);
                    hash = hash * 397 ^ StringHash(Category);
                    hash = hash * 397 ^ StringHash(PropertyName);
                    hash = hash * 397 ^ StringHash(DescriptorId);
                    hash = hash * 397 ^ StringHash(LabelText);
                    return hash;
                }
            }

            private static int ReferenceHash(object value)
            {
                return ReferenceEquals(value, null)
                    ? 0
                    : RuntimeHelpers.GetHashCode(value);
            }

            private static int StringHash(string value)
            {
                return value == null
                    ? 0
                    : StringComparer.Ordinal.GetHashCode(value);
            }
        }
    }
}
