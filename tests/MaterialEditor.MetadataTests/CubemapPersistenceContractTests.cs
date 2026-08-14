internal static class CubemapPersistenceContractTests
{
    internal static void Run()
    {
        var root = FindRepositoryRoot();
        var conversion = Read(root, "src", "MaterialEditor.Base", "CubemapConversion.cs");
        var snapshot = Read(root, "src", "MaterialEditor.Base", "CubemapOriginalSnapshot.cs");
        var copyContainer = Read(root, "src", "MaterialEditor.Base", "CopyContainer.cs");
        var materialApi = Read(root, "src", "MaterialEditor.Base", "MaterialAPI.cs");
        var pluginBase = Read(root, "src", "MaterialEditor.Base", "PluginBase.cs");
        var unshippedApi = Read(root, "src", "MaterialEditor.API", "PublicAPI.Unshipped.txt");
        var shippedApi = Read(root, "src", "MaterialEditor.API", "PublicAPI.Shipped.txt");
        var baseProject = Read(root, "src", "MaterialEditor.Base", "MaterialEditor.Base.projitems");
        var propertyDescriptor = Read(root, "src", "MaterialEditor.Base", "UI", "UI.PropertyDescriptor.cs");
        var ui = Read(root, "src", "MaterialEditor.Base", "UI", "UI.cs");
        var rowModel = Read(root, "src", "MaterialEditor.Base", "UI", "UI.RowModel.cs");
        var textureModel = Read(root, "src", "MaterialEditor.Base", "UI", "UI.RowModel.Texture.cs");
        var cubemapModel = Read(root, "src", "MaterialEditor.Base", "UI", "UI.RowModel.Cubemap.cs");
        var textureBinder = Read(root, "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Texture.cs");
        var cubemapBinder = Read(root, "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Cubemap.cs");
        var repositoryContract = Read(root, "src", "MaterialEditor.Base", "UI", "UI.RepositoryContract.cs");

        var charaModel = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Models.cs");
        var charaTexture = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Edits.TextureShader.cs");
        var charaCubemap = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Edits.Cubemap.cs");
        var charaEvents = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Events.cs");
        var charaController = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.cs");
        var charaPersistence = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Persistence.cs");
        var charaCopy = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Edits.Core.cs");
        var charaImport = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.Import.cs");
        var charaRepository = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaRepository.cs");
        var charaProject = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.projitems");
        var maker = Read(root, "src", "MaterialEditor.Core.Maker", "Core.MaterialEditor.Maker.cs");

        var sceneModel = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Models.cs");
        var sceneTexture = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Edits.TextureShader.cs");
        var sceneCubemap = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Edits.Cubemap.cs");
        var sceneController = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.cs");
        var sceneCopy = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Edits.Core.cs");
        var sceneRepository = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneRepository.cs");
        var sceneProject = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.Studio.projitems");

        IndependentPublicTypes(materialApi, pluginBase, shippedApi, unshippedApi);
        IndependentCopyModels(copyContainer);
        IndependentPersistenceModels(charaModel, sceneModel);
        IndependentControllerPaths(
            charaTexture,
            charaCubemap,
            charaController,
            charaPersistence,
            charaEvents,
            charaCopy,
            charaImport,
            charaRepository,
            maker,
            sceneTexture,
            sceneCubemap,
            sceneController,
            sceneCopy,
            sceneRepository);
        IndependentUiPaths(
            propertyDescriptor,
            ui,
            rowModel,
            textureModel,
            cubemapModel,
            textureBinder,
            cubemapBinder,
            repositoryContract);
        OriginalSnapshotsAreCubemapSpecific(snapshot, charaModel, sceneModel, charaCubemap, sceneCubemap);
        SharedMechanicsStayTypeAgnostic(conversion, charaController, sceneController);
        ProjectFilesIncludeDedicatedSources(baseProject, charaProject, sceneProject);

        var production = ReadProductionSources(root);
        DoesNotContain(production, "TextureKind", "no texture subtype discriminator remains");
        DoesNotContain(production, "GetTextureKind", "no manifest-based texture subtype detection remains");
        DoesNotContain(production, "item.IsCubemap", "texture rows do not branch on Cubemap");
        DoesNotContain(production, "textureItem.IsCubemap", "texture actions do not branch on Cubemap");
        DoesNotContain(production, "CreateTextureRows(descriptor, false)", "Cubemap does not reuse the Texture row path");
        DoesNotContain(materialApi, "Texture3D", "Texture3D remains a future type");
    }

    private static void IndependentPublicTypes(
        string materialApi,
        string pluginBase,
        string shippedApi,
        string unshippedApi)
    {
        Contains(materialApi, "Cubemap = 4", "public Cubemap enum value is additive");
        Contains(materialApi, "public static bool SetCubemap(", "native Cubemap setter is type-specific");
        Contains(pluginBase, "Enum.Parse(", "manifest property types use the real enum");
        Contains(pluginBase, "Enum.IsDefined(typeof(ShaderPropertyType), parsed)", "undefined future types are rejected");
        DoesNotContain(shippedApi, "Cubemap", "shipped API baseline remains untouched");
        Contains(unshippedApi, "MaterialAPI.ShaderPropertyType.Cubemap = 4", "Cubemap enum API is additive");
        Contains(unshippedApi, "MaterialEditorPropertyEditorIds.Cubemap", "Cubemap editor has a distinct ID");
        Contains(unshippedApi, "CopyContainer.MaterialCubemapProperty", "Cubemap copy API is distinct");
        Contains(unshippedApi, "MaterialAPI.SetCubemap", "Cubemap material API is distinct");
    }

    private static void IndependentCopyModels(string source)
    {
        Contains(source, "List<MaterialCubemapProperty> MaterialCubemapPropertyList", "Cubemap copy list");
        Contains(source, "public class MaterialCubemapProperty", "Cubemap copy payload");
        Contains(source, "MaterialCubemapPropertyList.Count == 0", "Cubemap participates in IsEmpty");
        Contains(source, "MaterialCubemapPropertyList = new List<MaterialCubemapProperty>();", "Cubemap participates in ClearAll");

        var texture = Slice(source, "public class MaterialTextureProperty", "public class MaterialCubemapProperty");
        DoesNotContain(texture, "TextureKind", "Texture copy payload has no subtype discriminator");
        DoesNotContain(texture, "CubemapOriginal", "Texture copy payload has no Cubemap state");
        Contains(texture, "Vector2? Offset", "Texture copy payload keeps offset");
        Contains(texture, "Vector2? Scale", "Texture copy payload keeps scale");

        var cubemap = Slice(source, "public class MaterialCubemapProperty", "public class MaterialShader");
        Contains(cubemap, "byte[] Data", "Cubemap copy payload stores source bytes");
        DoesNotContain(cubemap, "Offset", "Cubemap copy payload has no 2D offset");
        DoesNotContain(cubemap, "Scale", "Cubemap copy payload has no 2D scale");
    }

    private static void IndependentPersistenceModels(string chara, string scene)
    {
        AssertIndependentPersistenceModel(chara, "character");
        AssertIndependentPersistenceModel(scene, "scene");
    }

    private static void AssertIndependentPersistenceModel(string source, string name)
    {
        var texture = Slice(source, "public class MaterialTextureProperty", "public class MaterialCubemapProperty");
        DoesNotContain(texture, "TextureKind", name + " Texture model has no subtype discriminator");
        DoesNotContain(texture, "CubemapOriginal", name + " Texture model has no Cubemap snapshot");
        Contains(texture, "MEAnimationDefine TexAnimationDef", name + " Texture retains animation data");

        var cubemap = Slice(source, "public class MaterialCubemapProperty", "public class MaterialShader");
        Contains(cubemap, "[Key(\"TexID\")]", name + " Cubemap persists source ID");
        Contains(cubemap, "Dictionary<Material, Cubemap> CubemapOriginalMaterials", name + " Cubemap snapshot is strongly typed");
        Contains(cubemap, "List<Cubemap> CubemapOriginalValues", name + " Cubemap duplicate snapshot is strongly typed");
        DoesNotContain(cubemap, "Offset", name + " Cubemap model has no offset");
        DoesNotContain(cubemap, "Scale", name + " Cubemap model has no scale");
        DoesNotContain(cubemap, "MEAnimationDefine", name + " Cubemap model has no animation");
    }

    private static void IndependentControllerPaths(
        string charaTexture,
        string charaCubemap,
        string charaController,
        string charaPersistence,
        string charaEvents,
        string charaCopy,
        string charaImport,
        string charaRepository,
        string maker,
        string sceneTexture,
        string sceneCubemap,
        string sceneController,
        string sceneCopy,
        string sceneRepository)
    {
        DoesNotContain(charaTexture, "Cubemap", "character Texture operations stay Texture2D-only");
        DoesNotContain(sceneTexture, "Cubemap", "scene Texture operations stay Texture2D-only");

        AssertDedicatedCubemapEditor(charaCubemap, "character");
        AssertDedicatedCubemapEditor(sceneCubemap, "scene");
        Contains(charaRepository, "GetMaterialCubemapValueOriginal", "character Cubemap repository seam");
        Contains(charaRepository, "SetMaterialCubemap(", "character Cubemap set seam");
        Contains(charaRepository, "RemoveMaterialCubemap(", "character Cubemap reset seam");
        Contains(sceneRepository, "GetMaterialCubemapValueOriginal", "scene Cubemap repository seam");
        Contains(sceneRepository, "SetMaterialCubemap(", "scene Cubemap set seam");
        Contains(sceneRepository, "RemoveMaterialCubemap(", "scene Cubemap reset seam");
        Contains(maker, "override bool GetMaterialCubemapValueOriginal", "Maker forwards Cubemap original-state queries");
        Contains(maker, "override void SetMaterialCubemap", "Maker forwards Cubemap imports");
        Contains(maker, "override void RemoveMaterialCubemap", "Maker forwards Cubemap resets");

        Contains(charaController, "nameof(MaterialCubemapPropertyList)", "character Cubemap save key");
        Contains(charaController, "MessagePackSerializer.Serialize(MaterialCubemapPropertyList)", "character Cubemap serialization");
        Contains(sceneController, "nameof(MaterialCubemapPropertyList)", "scene Cubemap save key");
        Contains(sceneController, "Deserialize<List<MaterialCubemapProperty>>", "scene Cubemap deserialization");
        AtLeast(2, Count(charaPersistence, "Deserialize<List<MaterialCubemapProperty>>"), "card and coordinate Cubemap deserialization");

        Contains(charaCopy, "CopyData.MaterialCubemapPropertyList", "character Cubemap copy payload");
        Contains(charaCopy, "SetMaterialCubemap(", "character Cubemap paste path");
        Contains(sceneCopy, "CopyData.MaterialCubemapPropertyList", "scene Cubemap copy payload");
        Contains(sceneCopy, "SetMaterialCubemap(", "scene Cubemap paste path");
        Contains(charaEvents, "MaterialCubemapPropertyList", "coordinate/accessory Cubemap copy paths");
        AtLeast(3, Count(charaImport, "MaterialCubemapPropertyList"), "all character import preprocessors preserve Cubemap data");

        Contains(charaController, "MaterialCubemapPropertyList[i].TexID", "character byte-store purge includes Cubemap IDs");
        Contains(sceneController, "MaterialCubemapPropertyList[i].TexID", "scene byte-store purge includes Cubemap IDs");
        Contains(charaController, "PurgeUnusedAnimation(AnimationControllerMap, MaterialTexturePropertyList)", "character Timeline remains Texture2D-only");
        Contains(sceneController, "PurgeUnusedAnimation(AnimationControllerMap, MaterialTexturePropertyList)", "scene Timeline remains Texture2D-only");
        DoesNotContain(charaController, "PurgeUnusedAnimation(AnimationControllerMap, MaterialCubemapPropertyList)", "character Cubemap never enters Timeline");
        DoesNotContain(sceneController, "PurgeUnusedAnimation(AnimationControllerMap, MaterialCubemapPropertyList)", "scene Cubemap never enters Timeline");
    }

    private static void AssertDedicatedCubemapEditor(string source, string name)
    {
        Contains(source, "SetMaterialCubemapFromFile(", name + " dedicated file import");
        Contains(source, "SetMaterialCubemap(", name + " dedicated byte import");
        Contains(source, "GetMaterialCubemap(", name + " dedicated getter");
        Contains(source, "RemoveMaterialCubemap(", name + " dedicated reset");
        Contains(source, "MaterialEditorCubemapCache.TryAcquire", name + " Cubemap conversion cache");
        Contains(source, "MaterialCubemapOriginalSnapshot.RestoreByMaterialReference", name + " exact Cubemap reset");
        Contains(source, "PurgeUnusedCubemapLeases", name + " Cubemap lease cleanup");
    }

    private static void IndependentUiPaths(
        string descriptor,
        string ui,
        string rowModel,
        string textureModel,
        string cubemapModel,
        string textureBinder,
        string cubemapBinder,
        string repositoryContract)
    {
        Contains(descriptor, "return MaterialEditorPropertyEditorIds.Cubemap", "Cubemap has a dedicated editor ID");
        Contains(descriptor, "CreateCubemapRow(descriptor)", "Cubemap has a dedicated row factory path");
        Contains(descriptor, "new CubemapPropertyRowModel", "Cubemap has a dedicated row model");
        Contains(rowModel, "CubemapProperty", "Cubemap has a dedicated row item type");
        Contains(cubemapModel, "class CubemapPropertyRowModel", "Cubemap row model exists");
        DoesNotContain(textureModel, "Cubemap", "Texture row model stays Texture2D-only");
        DoesNotContain(textureBinder, "Cubemap", "Texture binder stays Texture2D-only");
        Contains(cubemapBinder, "class CubemapRowTypeBinder", "Cubemap binder is distinct");
        Contains(cubemapBinder, "SelectInterpolableButton.gameObject.SetActive(false)", "Cubemap exposes no Timeline control");
        Contains(ui, "private void ImportCubemap(", "Cubemap import action is distinct");
        Contains(ui, "internal void ExportCubemap(", "Cubemap export action is distinct");
        Contains(repositoryContract, "GetMaterialCubemapValueOriginal", "Cubemap UI repository contract");
        Contains(repositoryContract, "SetMaterialCubemap(", "Cubemap UI import contract");
        Contains(repositoryContract, "RemoveMaterialCubemap(", "Cubemap UI reset contract");

        var watcher = Slice(ui, "TexChangeWatcher.Changed += (sender, args) =>", "TexChangeWatcher.Deleted +=");
        Contains(watcher, "ScheduleTextureWatcherImport", "Texture2D watcher keeps its main-thread handoff");
        DoesNotContain(watcher, "Cubemap", "Cubemap never enters the Texture2D watcher");

        var reset = Slice(cubemapBinder, "listeners.Listen(_controls.ResetButton", "LabelClickBinding.Bind(");
        OccursBefore(reset, "item.Changed = false;", "item.Reset();", "Cubemap reset recalculates final state after fallback");
    }

    private static void OriginalSnapshotsAreCubemapSpecific(
        string snapshot,
        string charaModel,
        string sceneModel,
        string charaCubemap,
        string sceneCubemap)
    {
        Contains(snapshot, "Dictionary<Material, Cubemap>", "Cubemap snapshots are strongly typed");
        Contains(snapshot, "object.ReferenceEquals(left, right)", "snapshots compare real material references");
        Contains(snapshot, "RuntimeHelpers.GetHashCode(material)", "snapshots do not use reusable instance IDs");
        Contains(snapshot, "TryRemapToCurrentMaterials", "duplicate snapshots remap deterministically");
        Contains(snapshot, "materials.Count != orderedValues.Count", "ambiguous duplicate remaps are rejected");
        Contains(snapshot, "materials[index].SetTexture(fullPropertyName, original)", "reset restores exact Cubemap including null");
        DoesNotContain(snapshot, "GetInstanceID()", "snapshots never depend on reusable instance IDs");
        Contains(charaModel, "InheritCubemapOriginalSnapshot", "character duplicate snapshot inheritance");
        Contains(sceneModel, "InheritCubemapOriginalSnapshot", "scene duplicate snapshot inheritance");
        DoesNotContain(charaCubemap, "OriginalsMatchCurrentMaterials", "character does not recapture overridden originals");
        DoesNotContain(sceneCubemap, "OriginalsMatchCurrentMaterials", "scene does not recapture overridden originals");
    }

    private static void SharedMechanicsStayTypeAgnostic(
        string conversion,
        string charaController,
        string sceneController)
    {
        Contains(conversion, "SHA256.Create()", "content-addressed Cubemap cache");
        Contains(conversion, "entry.References++", "shared Cubemap refcount acquire");
        Contains(conversion, "entry.References--", "shared Cubemap refcount release");
        Contains(conversion, "result.Apply(true, true)", "import discards CPU face copies");
        Contains(conversion, "CameraClearFlags.Skybox", "non-readable GPU readback path");
        Contains(conversion, "Unity returned no PNG data", "empty encoder output reports an error");
        DoesNotContain(conversion, "void Update(", "no Cubemap conversion in Update");
        DoesNotContain(conversion, "void OnGUI(", "no Cubemap conversion in OnGUI");
        Contains(charaController, "TextureDictionary.Keys", "character keeps one neutral byte store");
        Contains(sceneController, "TextureDictionary.Keys", "scene keeps one neutral byte store");
    }

    private static void ProjectFilesIncludeDedicatedSources(
        string baseProject,
        string charaProject,
        string sceneProject)
    {
        Contains(baseProject, "CubemapOriginalSnapshot.cs", "Base includes Cubemap snapshot source");
        Contains(baseProject, "UI.RowModel.Cubemap.cs", "Base includes Cubemap row model");
        Contains(baseProject, "UI.RowBinder.Cubemap.cs", "Base includes Cubemap binder");
        Contains(charaProject, "CharaController.Edits.Cubemap.cs", "character includes Cubemap operations");
        Contains(sceneProject, "SceneController.Edits.Cubemap.cs", "Studio includes Cubemap operations");
    }

    private static string ReadProductionSources(string root)
    {
        var sourceRoot = Path.Combine(root, "src");
        return string.Join(
            "\n",
            Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("obj"))
                .Select(File.ReadAllText));
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
            throw new InvalidOperationException(name + ": expected '" + first + "' before '" + second + "'.");
    }

    private static void AtLeast(int minimum, int actual, string name)
    {
        if (actual < minimum)
            throw new InvalidOperationException(name + ": expected at least " + minimum + ", got " + actual + ".");
    }
}
