using MaterialEditorAPI;
using System.Globalization;
using System.Text;
using System.Xml;

internal enum MultiTargetShape
{
    Equal,
    Mixed,
    Missing,
    Variants
}

internal sealed class SyntheticProperty
{
    internal string Name;
    internal string Category;
    internal string DisplayName;
    internal string Tooltip;
    internal string EditorId;
    internal bool Hidden;
    internal MaterialEditorPropertyCondition ShowIf;
    internal int EnumOptionCount;
}

internal sealed class SyntheticDataset
{
    internal string Name;
    internal string ManifestXml;
    internal int PropertyCount;
    internal int CategoryCount;
    internal int ConditionCount;
    internal int EnumCount;
    internal int VectorCount;
    internal int BooleanCount;
    internal int ProviderCount;
    internal int RendererCount;
    internal int MaterialCount;
    internal int DropdownCount;
    internal int TextureCount;
}

internal sealed class WorkloadOutcome
{
    internal string Fingerprint;
    internal int VisibleRows;
    internal int MixedRows;
    internal int MissingRows;
    internal int VariantRows;
    internal int PoolPeak;
    internal int ActiveListeners;
    internal int ProviderRegistrations;
    internal int LogicalRebuilds;
    internal bool SemanticChecksPassed = true;
}

internal static class SyntheticDatasetFactory
{
    internal static SyntheticDataset Simple()
    {
        return Create(
            "simple-20x4",
            propertyCount: 20,
            categoryCount: 4,
            conditionCount: 0,
            enumCount: 0,
            vectorCount: 0,
            booleanCount: 0,
            providerCount: 1);
    }

    internal static SyntheticDataset KkltLike()
    {
        return Create(
            "kklt-like-250x30",
            propertyCount: 250,
            categoryCount: 30,
            conditionCount: 40,
            enumCount: 20,
            vectorCount: 10,
            booleanCount: 15,
            providerCount: 4);
    }

    private static SyntheticDataset Create(
        string name,
        int propertyCount,
        int categoryCount,
        int conditionCount,
        int enumCount,
        int vectorCount,
        int booleanCount,
        int providerCount)
    {
        var xml = new StringBuilder(propertyCount * 180);
        xml.Append("<MaterialEditor SchemaVersion=\"2\"><Shader Name=\"Synthetic/Stress\">");
        for (var index = 0; index < propertyCount; index++)
        {
            var editor = ResolveEditor(index, enumCount, vectorCount, booleanCount);
            xml.Append("<Property Name=\"Property")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append("\" DisplayName=\"Display Property ")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append("\" Category=\"Category")
                .Append((index % categoryCount).ToString(CultureInfo.InvariantCulture))
                .Append("\" Tooltip=\"Synthetic tooltip ")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append("\"");

            if (index < conditionCount)
            {
                xml.Append(" ShowIf=\"")
                    .Append("Source")
                    .Append((index % 5).ToString(CultureInfo.InvariantCulture))
                    .Append(" == 1\"");
            }
            if (index >= conditionCount && index > 0 && index % 29 == 0)
                xml.Append(" Hidden=\"true\"");
            if (editor != null)
                xml.Append(" Editor=\"").Append(editor).Append("\"");
            if (editor == "Vector")
                xml.Append(" VectorComponentCount=\"4\"");
            if (editor == "Boolean")
                xml.Append(" OffValue=\"0\" OnValue=\"1\"");

            if (editor == "Enum")
            {
                xml.Append("><Option Value=\"0\" DisplayName=\"Off\" />")
                    .Append("<Option Value=\"1\" DisplayName=\"Low\" />")
                    .Append("<Option Value=\"2\" DisplayName=\"High\" /></Property>");
            }
            else
            {
                xml.Append(" />");
            }
        }
        xml.Append("</Shader></MaterialEditor>");

        return new SyntheticDataset
        {
            Name = name,
            ManifestXml = xml.ToString(),
            PropertyCount = propertyCount,
            CategoryCount = categoryCount,
            ConditionCount = conditionCount,
            EnumCount = enumCount,
            VectorCount = vectorCount,
            BooleanCount = booleanCount,
            ProviderCount = providerCount,
            RendererCount = name == "kklt-like-250x30" ? 20 : 2,
            MaterialCount = name == "kklt-like-250x30" ? 50 : 4,
            DropdownCount = enumCount,
            TextureCount = name == "kklt-like-250x30" ? 15 : 2
        };
    }

    private static string ResolveEditor(
        int index,
        int enumCount,
        int vectorCount,
        int booleanCount)
    {
        if (index < enumCount)
            return "Enum";
        if (index < enumCount + vectorCount)
            return "Vector";
        if (index < enumCount + vectorCount + booleanCount)
            return "Boolean";
        return null;
    }
}

internal sealed class SyntheticRowPool
{
    private const int MaximumVisibleViews = 32;
    private int _created;
    private int _activeListenerCount;
    private int _boundViewCount;

    internal int Peak { get; private set; }
    internal int ActiveListenerCount { get { return _activeListenerCount; } }

    internal void BindRows(int rowCount)
    {
        UnbindCurrent();
        var visible = Math.Min(rowCount, MaximumVisibleViews);
        if (_created < visible)
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.RowViewCreation,
                visible - _created);
            _created = visible;
            Peak = Math.Max(Peak, _created);
        }

        _boundViewCount = visible;
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Bind,
            visible);
        _activeListenerCount = visible * 2;
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRegistrations,
            _activeListenerCount);

        var recycled = Math.Max(0, rowCount - visible);
        if (recycled == 0)
            return;
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.RowViewReuse,
            recycled);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Bind,
            recycled);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Unbind,
            recycled);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRegistrations,
            recycled * 2L);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRemovals,
            recycled * 2L);
    }

    internal void Close()
    {
        UnbindCurrent();
    }

    private void UnbindCurrent()
    {
        if (_boundViewCount == 0)
            return;
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.Unbind,
            _boundViewCount);
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ListenerRemovals,
            _activeListenerCount);
        _boundViewCount = 0;
        _activeListenerCount = 0;
    }
}

internal sealed class SyntheticProviderRegistry
{
    private int _registrations;

    internal int Count { get { return _registrations; } }

    internal IDisposable Register()
    {
        _registrations++;
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ProviderRegistrations);
        return new Registration(this);
    }

    private void Remove()
    {
        if (_registrations == 0)
            return;
        _registrations--;
        MaterialEditorPerformance.Increment(
            MaterialEditorPerformanceMetric.ProviderRemovals);
    }

    private sealed class Registration : IDisposable
    {
        private SyntheticProviderRegistry _owner;

        internal Registration(SyntheticProviderRegistry owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            var owner = _owner;
            if (owner == null)
                return;
            _owner = null;
            owner.Remove();
        }
    }
}
