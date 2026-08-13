internal static class CubemapPersistenceContractTests
{
    internal static void Run()
    {
        var root = FindRepositoryRoot();
        var conversion = Read(root, "src", "MaterialEditor.Base", "CubemapConversion.cs");
        var copyContainer = Read(root, "src", "MaterialEditor.Base", "CopyContainer.cs");
        var materialApi = Read(root, "src", "MaterialEditor.Base", "MaterialAPI.cs");
        var pluginBase = Read(root, "src", "MaterialEditor.Base", "PluginBase.cs");
        var propertyDescriptor = Read(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.PropertyDescriptor.cs");
        var ui = Read(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.cs");
        var textureBinder = Read(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.RowBinder.Texture.cs");
        var charaModel = Read(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Models.cs");
        var charaTexture = Read(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Edits.TextureShader.cs");
        var charaEvents = Read(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Events.cs");
        var charaController = Read(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.cs");
        var charaPersistence = Read(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Persistence.cs");
        var charaCopy = Read(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Edits.Core.cs");
        var sceneModel = Read(
            root,
            "src",
            "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.SceneController.Models.cs");
        var sceneTexture = Read(
            root,
            "src",
            "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.SceneController.Edits.TextureShader.cs");
        var sceneController = Read(
            root,
            "src",
            "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.SceneController.cs");
        var sceneCopy = Read(
            root,
            "src",
            "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.SceneController.Edits.Core.cs");

        Contains(materialApi, "Cubemap = 4", "public Cubemap enum value is additive");
        Contains(pluginBase, "Enum.Parse(", "real manifest property types use enum parsing");
        Contains(pluginBase, "Enum.IsDefined(typeof(ShaderPropertyType), parsed)",
            "manifest parser rejects undefined property-type values");
        Contains(charaModel, "[Key(\"TextureKind\")]", "card/coordinate kind key");
        Contains(sceneModel, "[Key(\"TextureKind\")]", "scene kind key");
        Contains(charaModel, "[IgnoreMember]", "card runtime original snapshot ignored");
        Contains(charaModel, "Dictionary<Material, Texture> TextureOriginalMaterials",
            "card original snapshot uses material references");
        Contains(sceneModel, "Dictionary<Material, Texture> TextureOriginalMaterials",
            "scene original snapshot uses material references");

        Contains(charaPersistence, "loadedProperty.TextureKind", "card and coordinate load kind");
        AtLeast(2, Count(charaPersistence, "loadedProperty.TextureKind"),
            "both card and coordinate load paths preserve kind");
        Contains(charaPersistence, "property.Value.Type == ShaderPropertyType.Cubemap",
            "tongue material refresh recognizes Cubemap properties");
        Contains(charaPersistence, "SetCubemap(ChaControl.gameObject, materialName, property.Key, cubemap)",
            "tongue material refresh copies native Cubemap values");
        Contains(sceneController, "loadedProperty.TextureKind", "scene load kind");
        Contains(charaController, "return property.TexID;", "Cubemap bytes included in card saves");
        Contains(sceneController, "return property.TexID;", "Cubemap bytes included in scene saves");
        Contains(charaController, "DisposeTextureDictionary();", "character texture containers released on destroy");
        Contains(charaController, "TextureDictionary.Clear();", "character texture dictionary cleared on destroy");
        Contains(charaController, "if (property.TextureKind == ShaderPropertyType.Cubemap)",
            "character Cubemap animation remains disabled");
        Contains(sceneController, "if (property.TextureKind == ShaderPropertyType.Cubemap)",
            "scene Cubemap animation remains disabled");
        DoesNotContain(charaController,
            "property.TextureKind == ShaderPropertyType.Cubemap ? null : property.TexID",
            "character persistence must not omit Cubemap bytes");
        DoesNotContain(sceneController,
            "property.TextureKind == ShaderPropertyType.Cubemap ? null : property.TexID",
            "scene persistence must not omit Cubemap bytes");
        Contains(charaCopy, "materialTextureProperty.TextureKind", "character copy/paste kind");
        Contains(sceneCopy, "materialTextureProperty.TextureKind", "scene copy/paste kind");

        Contains(copyContainer, "object.ReferenceEquals(left, right)",
            "original snapshots compare real material references");
        Contains(copyContainer, "RuntimeHelpers.GetHashCode(material)",
            "reference snapshots do not depend on reusable instance IDs");
        Contains(copyContainer, "SynchronizeByMaterialReference",
            "incremental target replacement guard");
        Contains(copyContainer, "TryRemapToCurrentMaterials",
            "duplicate snapshots remap in stable material order");
        Contains(copyContainer, "materials.Count != orderedValues.Count",
            "duplicate remap rejects ambiguous cardinality");
        DoesNotContain(copyContainer, "GetInstanceID()",
            "reset snapshots must not depend on reusable instance IDs");
        Contains(charaModel, "InheritTextureOriginalSnapshot", "character duplicate snapshot inheritance");
        Contains(sceneModel, "InheritTextureOriginalSnapshot", "scene duplicate snapshot inheritance");
        DoesNotContain(charaModel, "TextureOriginals.Values",
            "character portable snapshots must not use dictionary enumeration order");
        DoesNotContain(sceneModel, "TextureOriginals.Values",
            "scene portable snapshots must not use dictionary enumeration order");
        DoesNotContain(charaTexture, "TextureOriginalsMatchCurrentMaterials",
            "character must not recapture surviving overridden materials");
        DoesNotContain(sceneTexture, "TextureOriginalsMatchCurrentMaterials",
            "scene must not recapture surviving overridden materials");
        Contains(charaTexture, "setTexInUpdate && textureKind != ShaderPropertyType.Cubemap",
            "character Cubemap conversion stays out of Update");
        Contains(sceneTexture, "setTexInUpdate && textureKind != ShaderPropertyType.Cubemap",
            "scene Cubemap conversion stays out of Update");
        AtLeast(2, Count(charaTexture, "GetTextureKind(material, propertyName)"),
            "character file and byte APIs detect Cubemap manifests");
        AtLeast(2, Count(sceneTexture, "GetTextureKind(material, propertyName)"),
            "scene file and byte APIs detect Cubemap manifests");
        Contains(charaTexture, "XMLShaderProperties.TryGetValue(\"default\", out shaderProperties)",
            "character Cubemap detection honors the legacy fallback catalog");
        Contains(sceneTexture, "XMLShaderProperties.TryGetValue(\"default\", out shaderProperties)",
            "scene Cubemap detection honors the legacy fallback catalog");
        Contains(charaTexture, "RestoreByMaterialReference", "character exact reset");
        Contains(sceneTexture, "RestoreByMaterialReference", "scene exact reset");
        Contains(copyContainer, "materials[index].SetTexture(fullPropertyName, original)",
            "reset applies stored values including null");

        Contains(conversion, "SHA256.Create()", "content-addressed Cubemap cache");
        Contains(conversion, "entry.References++", "shared Cubemap refcount acquire");
        Contains(conversion, "entry.References--", "shared Cubemap refcount release");
        Contains(conversion, "result.Apply(true, true)", "imported Cubemap discards CPU face copies");
        Contains(conversion, "CameraClearFlags.Skybox", "non-readable GPU readback path");
        Contains(conversion, "Unity returned no PNG data",
            "empty PNG encoder output reports an explicit export error");
        DoesNotContain(conversion, "void Update(", "no Cubemap conversion in Update");
        DoesNotContain(conversion, "void OnGUI(", "no Cubemap conversion in OnGUI");

        Contains(charaEvents, "TextureKind == ShaderPropertyType.Cubemap", "character load applies native Cubemap");
        Contains(sceneTexture, "TextureKind == ShaderPropertyType.Cubemap", "scene load applies native Cubemap");
        Contains(propertyDescriptor, "CreateTextureRows(descriptor, false)", "Cubemap omits offset and scale rows");
        Contains(propertyDescriptor, "SelectInterpolable = includeOffsetAndScale", "Cubemap omits Timeline selection");
        var watcherCallback = Slice(
            ui,
            "TexChangeWatcher.Changed += (sender, args) =>",
            "TexChangeWatcher.Deleted +=");
        Contains(watcherCallback, "ScheduleTextureWatcherImport(data, material, propertyName, filePath, gameObject)",
            "texture watcher delegates before touching repository or Unity state");
        DoesNotContain(watcherCallback, "EditService.SetMaterialTexture",
            "texture watcher must not call the repository directly on its ThreadPool callback");
        Contains(ui, "private void ScheduleTextureWatcherImport(",
            "texture watcher has an explicit main-thread handoff seam");
        Contains(ui, "ThreadingHelper.Instance.StartSyncInvoke(() =>",
            "file dialog and texture watcher marshal work to Unity's main thread");
        var resetHandler = Slice(
            textureBinder,
            "listeners.Listen(controls.ResetButton",
            "if (!item.IsCubemap && item.SelectInterpolable != null)");
        OccursBefore(
            resetHandler,
            "item.Changed = false;",
            "item.Reset();",
            "Reset recomputes the final Changed state after the legacy fallback");
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(
                        directory.FullName,
                        "tests",
                        "MaterialEditor.MetadataTests",
                        "MaterialEditor.MetadataTests.csproj")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string Read(string root, params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidOperationException("Missing start marker '" + startMarker + "'.");
        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        if (end < 0)
            throw new InvalidOperationException("Missing end marker '" + endMarker + "'.");
        return source.Substring(start, end - start);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Contains(string source, string value, string name)
    {
        if (!source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + ": missing '" + value + "'.");
    }

    private static void DoesNotContain(string source, string value, string name)
    {
        if (source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + ": unexpected '" + value + "'.");
    }

    private static void OccursBefore(string source, string first, string second, string name)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        if (firstIndex < 0 || secondIndex < 0 || firstIndex >= secondIndex)
            throw new InvalidOperationException(
                name + ": expected '" + first + "' before '" + second + "'.");
    }

    private static void AtLeast(int minimum, int actual, string name)
    {
        if (actual < minimum)
            throw new InvalidOperationException(
                name + ": expected at least " + minimum + ", got " + actual + ".");
    }
}
