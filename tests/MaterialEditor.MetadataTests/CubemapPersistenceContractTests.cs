internal static class CubemapPersistenceContractTests
{
    internal static void Run()
    {
        var root = FindRepositoryRoot();
        var contentKey = Read(root, "src", "MaterialEditor.Base", "CubemapContentKey.cs");
        var backgroundRead = Read(root, "src", "MaterialEditor.Base", "CubemapBackgroundRead.cs");
        var cache = Read(root, "src", "MaterialEditor.Base", "CubemapCache.cs");
        var conversionFacade = Read(root, "src", "MaterialEditor.Base", "CubemapConversion.cs");
        var import = Read(root, "src", "MaterialEditor.Base", "CubemapImport.cs");
        var export = Read(root, "src", "MaterialEditor.Base", "CubemapExport.cs");
        var gpuReadback = Read(root, "src", "MaterialEditor.Base", "CubemapGpuReadback.cs");
        var memoryBudget = Read(root, "src", "MaterialEditor.Base", "CubemapMemoryBudget.cs");
        var identity = Read(root, "src", "MaterialEditor.Base", "CubemapMaterialIdentity.cs");
        var conversion = string.Join(
            "\n",
            contentKey,
            cache,
            conversionFacade,
            import,
            export,
            gpuReadback,
            memoryBudget);
        var snapshot = Read(root, "src", "MaterialEditor.Base", "CubemapOriginalSnapshot.cs");
        var copyContainer = Read(root, "src", "MaterialEditor.Base", "CopyContainer.cs");
        var materialApi = Read(root, "src", "MaterialEditor.Base", "MaterialAPI.cs");
        var repositoryCapabilities = Read(
            root,
            "src",
            "MaterialEditor.Base",
            "IMaterialEditRepository.cs");
        var editService = Read(root, "src", "MaterialEditor.Base", "MaterialEditService.cs");
        var pluginBase = Read(root, "src", "MaterialEditor.Base", "PluginBase.cs");
        var unshippedApi = Read(root, "src", "MaterialEditor.API", "PublicAPI.Unshipped.txt");
        var shippedApi = Read(root, "src", "MaterialEditor.API", "PublicAPI.Shipped.txt");
        var baseProject = Read(root, "src", "MaterialEditor.Base", "MaterialEditor.Base.projitems");
        var propertyDescriptor = Read(root, "src", "MaterialEditor.Base", "UI", "UI.PropertyDescriptor.cs");
        var ui = Read(root, "src", "MaterialEditor.Base", "UI", "UI.cs");
        var importCoordinator = Read(
            root,
            "src",
            "MaterialEditor.Base",
            "UI",
            "UI.CubemapImportCoordinator.cs");
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
        OriginalSnapshotsAreCubemapSpecific(
            snapshot,
            identity,
            charaModel,
            sceneModel,
            charaCubemap,
            sceneCubemap);
        SharedMechanicsStayTypeAgnostic(conversion, charaController, sceneController);
        IncrementalUiImportUsesBoundedMainThreadWork(
            backgroundRead,
            importCoordinator,
            ui,
            repositoryCapabilities,
            editService,
            charaRepository,
            sceneRepository,
            charaCubemap,
            sceneCubemap);
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
        Contains(materialApi, "Cubemap = 5", "public Cubemap enum value is appended after Vector");
        Contains(materialApi, "public static bool SetCubemap(", "native Cubemap setter is type-specific");
        Contains(pluginBase, "Enum.Parse(", "manifest property types use the real enum");
        Contains(pluginBase, "Enum.IsDefined(typeof(ShaderPropertyType), parsed)", "undefined future types are rejected");
        DoesNotContain(shippedApi, "Cubemap", "shipped API baseline remains untouched");
        Contains(unshippedApi, "MaterialAPI.ShaderPropertyType.Cubemap = 5", "Cubemap enum API is additive");
        Contains(unshippedApi, "MaterialEditorPropertyEditorIds.Cubemap", "Cubemap editor has a distinct ID");
        Contains(unshippedApi, "CopyContainer.MaterialCubemapProperty", "Cubemap copy API is distinct");
        Contains(unshippedApi, "MaterialAPI.SetCubemap", "Cubemap material API is distinct");
    }

    private static void IndependentCopyModels(string source)
    {
        Contains(source, "List<MaterialCubemapProperty> MaterialCubemapPropertyList", "Cubemap copy list");
        Contains(source, "public class MaterialCubemapProperty", "Cubemap copy payload");
        Contains(source, "!HasAny(MaterialCubemapPropertyList)", "Cubemap participates in IsEmpty");
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
        Contains(cubemap, "List<MaterialCubemapOriginalBinding> CubemapOriginalBindings", name + " Cubemap duplicate snapshot has stable binding identities");
        Contains(cubemap, "CubemapOriginalBindingsNeedRemap", name + " Cubemap duplicate snapshot tracks remap state");
        Contains(cubemap, "return TexID == null || Property == null || MaterialName == null;", name + " Cubemap validity semantics match both environments");
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
        Contains(
            descriptor,
            "ShaderPropertyEditorPolicy.GetDefaultEditorId",
            "built-in property types use the canonical editor mapping");
        Contains(descriptor, "CreateCubemapRow(descriptor)", "Cubemap has a dedicated row factory path");
        Contains(descriptor, "new CubemapPropertyRowModel", "Cubemap has a dedicated row model");
        Contains(rowModel, "CubemapProperty", "Cubemap has a dedicated row item type");
        Contains(cubemapModel, "class CubemapPropertyRowModel", "Cubemap row model exists");
        DoesNotContain(textureModel, "Cubemap", "Texture row model stays Texture2D-only");
        DoesNotContain(textureBinder, "Cubemap", "Texture binder stays Texture2D-only");
        Contains(cubemapBinder, "class CubemapRowTypeBinder", "Cubemap binder is distinct");
        Contains(ui, "*.png;*.hdr", "Cubemap picker exposes PNG and HDR inputs");
        Contains(cubemapBinder, "Radiance RGBE (.hdr)", "Cubemap import tooltip identifies Radiance HDR");
        Contains(
            cubemapBinder,
            "HDR values above 1 are clipped in this SDR export.",
            "Cubemap PNG export explicitly describes its SDR clipping");
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
        string identity,
        string charaModel,
        string sceneModel,
        string charaCubemap,
        string sceneCubemap)
    {
        Contains(snapshot, "Dictionary<Material, Cubemap>", "Cubemap snapshots are strongly typed");
        Contains(snapshot, "ReferenceEquals(left, right)", "snapshots compare real material references");
        Contains(snapshot, "RuntimeHelpers.GetHashCode(material)", "snapshots do not use reusable instance IDs");
        Contains(snapshot, "TryRemapToCurrentMaterials", "duplicate snapshots remap deterministically");
        Contains(snapshot, "MaterialCubemapIdentityMatcher.TryMatch", "duplicate snapshots require exact stable identity matches");
        Contains(snapshot, "One material reference maps to conflicting Cubemap originals", "conflicting duplicate remaps fail safely");
        Contains(snapshot, "material.SetTexture(fullPropertyName, original)", "reset restores exact Cubemap including null");
        DoesNotContain(snapshot, "GetInstanceID()", "snapshots never depend on reusable instance IDs");
        DoesNotContain(snapshot, "GetSiblingIndex()", "binding paths are stable when unrelated siblings move");
        DoesNotContain(snapshot, "orderedValues[index]", "duplicate snapshots never remap by traversal index");
        Contains(identity, "RelativePath", "binding identity includes renderer-relative path");
        Contains(identity, "ComponentIndex", "binding identity includes component index");
        Contains(identity, "MaterialSlot", "binding identity includes material slot");
        Contains(identity, "MaterialName", "binding identity includes formatted material name");
        Contains(identity, "PropertyName", "binding identity includes property name");
        Contains(identity, "saved.Count != current.Count", "binding count changes fail safely");
        Contains(identity, "identity is ambiguous", "ambiguous identities fail safely");
        Contains(charaModel, "InheritCubemapOriginalSnapshot", "character duplicate snapshot inheritance");
        Contains(sceneModel, "InheritCubemapOriginalSnapshot", "scene duplicate snapshot inheritance");
        DoesNotContain(charaCubemap, "OriginalsMatchCurrentMaterials", "character does not recapture overridden originals");
        DoesNotContain(sceneCubemap, "OriginalsMatchCurrentMaterials", "scene does not recapture overridden originals");
        AssertFailClosedSnapshotRemap(charaModel, charaCubemap, "character");
        AssertFailClosedSnapshotRemap(sceneModel, sceneCubemap, "scene");
        AssertAtomicCubemapSetter(charaCubemap, "character");
        AssertAtomicCubemapSetter(sceneCubemap, "scene");
    }

    private static void AssertFailClosedSnapshotRemap(
        string modelSource,
        string controllerSource,
        string name)
    {
        var cubemapModel = Slice(
            modelSource,
            "public class MaterialCubemapProperty",
            "public class MaterialShader");
        var remap = Slice(
            cubemapModel,
            "if (CubemapOriginalBindingsNeedRemap)",
            "var synchronizedMaterials");
        Contains(remap, "return false;", name + " remap mismatch rejects the override");
        DoesNotContain(
            remap,
            "CubemapOriginalMaterials = null;",
            name + " remap mismatch retains the known snapshot");
        DoesNotContain(
            remap,
            "capturing the destination materials instead",
            name + " remap mismatch never recaptures an active override as original");
        Contains(
            cubemapModel,
            "CubemapOriginalSnapshotWarningLogged",
            name + " remap warning is deduplicated");
        AtLeast(
            2,
            Count(
                controllerSource,
                "if (!cubemapProperty.SynchronizeCubemapOriginalSnapshot("),
            name + " setter and Reset both fail closed without a safe original mapping");
    }

    private static void AssertAtomicCubemapSetter(string source, string name)
    {
        Contains(source, "previousAppliedValues =", name + " captures the active Cubemap before apply");
        Contains(source, "previousTexID = cubemapProperty.TexID;", name + " snapshots the previous TexID");
        Contains(source, "previousOriginalMaterials = cubemapProperty.CubemapOriginalMaterials;", name + " snapshots Reset materials");
        Contains(source, "previousOriginalBindings = cubemapProperty.CubemapOriginalBindings;", name + " snapshots stable Reset bindings");
        Contains(source, "catch (Exception exception)", name + " treats setter exceptions as failed transactions");
        Contains(source, "RollbackMaterialCubemapSet(", name + " has a shared rollback path for false and exceptions");
        Contains(source, "cubemapProperty.TexID = previousTexID;", name + " rollback restores the prior TexID");
        Contains(source, "|| cubemapProperty.CubemapOriginalSnapshotWarningLogged;", name + " rollback preserves warning deduplication");
        Contains(source, "MaterialCubemapPropertyList.Remove(cubemapProperty);", name + " rollback removes a newly-created edit");
        Contains(source, "CubemapLeases.Release(texID);", name + " rollback releases a newly-created lease");
        Contains(source, "TextureDictionary.Remove(texID);", name + " rollback removes newly-created source bytes");
        Contains(source, "the previous override was preserved", name + " failure reports transactional semantics");
        Contains(source, "PurgeUnusedTextures();", name + " Reset purges the released Cubemap bytes");
    }

    private static void SharedMechanicsStayTypeAgnostic(
        string conversion,
        string charaController,
        string sceneController)
    {
        Contains(conversion, "SHA256.Create()", "content-addressed Cubemap cache");
        Contains(conversion, "existing.References++", "shared Cubemap refcount acquire");
        Contains(conversion, "entry.References--", "shared Cubemap refcount release");
        Contains(conversion, "Apply(true, true)", "import discards CPU face copies");
        Contains(conversion, "CameraClearFlags.Skybox", "non-readable GPU readback path");
        Contains(conversion, "Unity returned no PNG data", "empty encoder output reports an error");
        Contains(conversion, "TryBeginAcquire", "cache exposes an incremental miss seam");
        Contains(conversion, "ProcessRows(int maxRows", "panorama projection can be budgeted per frame");
        Contains(conversion, "PublishConverted", "converted candidates use a double-check publication step");
        Contains(conversion, "duplicate = converted", "concurrent cache misses select a single cached Cubemap");
        Contains(conversion, "Object.Destroy(duplicate)", "the losing cache candidate is destroyed after publication");
        Contains(conversion, "MaterialEditorCubemapMemoryBudget.TryValidateImport", "imports enforce an explicit peak-memory budget");
        Contains(conversion, "MaterialEditorCubemapMemoryBudget.TryValidateExport", "exports enforce an explicit peak-memory budget");
        Contains(conversion, "TryReserveConversion", "imports and exports share one aggregate temporary-memory admission gate");
        Contains(conversion, "ReleaseMemoryReservation", "completed and cancelled imports release aggregate budget ownership");
        DoesNotContain(conversion, "void Update(", "no Cubemap conversion in Update");
        DoesNotContain(conversion, "void OnGUI(", "no Cubemap conversion in OnGUI");
        Contains(charaController, "MaterialEditorCubemapLeaseStore", "character uses the shared Cubemap lease lifecycle helper");
        Contains(sceneController, "MaterialEditorCubemapLeaseStore", "scene uses the shared Cubemap lease lifecycle helper");
        Contains(charaController, "TextureDictionary.Keys", "character keeps one neutral byte store");
        Contains(sceneController, "TextureDictionary.Keys", "scene keeps one neutral byte store");
    }

    private static void IncrementalUiImportUsesBoundedMainThreadWork(
        string backgroundRead,
        string coordinator,
        string ui,
        string repositoryCapabilities,
        string editService,
        string charaRepository,
        string sceneRepository,
        string charaCubemap,
        string sceneCubemap)
    {
        Contains(
            backgroundRead,
            "ThreadPool.QueueUserWorkItem",
            "Cubemap file IO is queued off the main thread");
        Contains(
            backgroundRead,
            "File.ReadAllBytes(_filePath)",
            "Cubemap source bytes are read by the worker");
        Contains(
            backgroundRead,
            "MaterialEditorCubemapContentKey.TryCompute",
            "Cubemap SHA-256 is computed by the worker");
        Contains(
            coordinator,
            "internal const int RowsPerFrame = 16;",
            "Cubemap projection has a documented fixed frame budget");
        Contains(
            coordinator,
            "_acquire.ProcessRows(RowsPerFrame",
            "Cubemap projection advances by bounded row batches");
        Contains(
            coordinator,
            "MaterialEditorCubemapCache.TryBeginAcquire(",
            "main-thread coordinator uses incremental cache acquisition");
        Contains(
            coordinator,
            "_contentKey,",
            "main-thread acquisition reuses the worker-computed key");
        Contains(
            coordinator,
            "private void OnDestroy()",
            "Cubemap runner owns destruction cleanup");
        Contains(
            coordinator,
            "Unity's LoadImage/GetPixels32",
            "coordinator does not misrepresent Unity thread affinity");

        var importAction = Slice(
            ui,
            "private void ImportCubemap(",
            "private void ScheduleTextureWatcherImport(");
        Contains(
            importAction,
            "MaterialEditorCubemapImportRunner",
            "Cubemap UI delegates orchestration to the lifecycle runner");
        Contains(
            importAction,
            "encodedData,",
            "Cubemap UI sends preloaded bytes to the edit service");
        Contains(
            importAction,
            "contentKey,",
            "Cubemap UI sends the worker key to the edit service");
        Contains(
            repositoryCapabilities,
            "MaterialEditorCubemapContentKey contentKey",
            "optional Cubemap byte repository preserves the worker key");
        Contains(
            repositoryCapabilities,
            "bool SetMaterialCubemap(",
            "optional Cubemap byte repository reports real application success");
        Contains(
            editService,
            "MaterialEditorCubemapContentKey contentKey",
            "Cubemap edit service preserves the worker key");
        Contains(
            editService,
            "return dataRepository.SetMaterialCubemap(",
            "Cubemap edit service forwards real repository success");
        Contains(
            editService,
            "SupportsMaterialCubemapDataImport",
            "Cubemap edit service separates capability detection from application failure");
        Contains(
            charaRepository,
            "MaterialEditorCubemapContentKey contentKey",
            "character Cubemap repository preserves the worker key");
        Contains(
            sceneRepository,
            "MaterialEditorCubemapContentKey contentKey",
            "Studio Cubemap repository preserves the worker key");
        Contains(
            charaCubemap,
            "contentKey == null",
            "character controller selects keyed cache acquisition when available");
        Contains(
            charaCubemap,
            "internal bool SetMaterialCubemap(",
            "character keyed controller path reports actual application success");
        Contains(
            charaCubemap,
            "if (SetCubemapWithProperty(go, cubemapProperty))",
            "character keyed controller success comes from real material assignment");
        Contains(
            sceneCubemap,
            "contentKey == null",
            "Studio controller selects keyed cache acquisition when available");
        Contains(
            sceneCubemap,
            "internal bool SetMaterialCubemap(",
            "Studio keyed controller path reports actual application success");
        Contains(
            sceneCubemap,
            "if (SetCubemapWithProperty(gameObject, cubemapProperty))",
            "Studio keyed controller success comes from real material assignment");
        Contains(charaCubemap, "lease = null;", "character transfers lease ownership explicitly");
        Contains(sceneCubemap, "lease = null;", "Studio transfers lease ownership explicitly");
        Contains(
            importAction,
            "legacy file import path",
            "legacy repositories retain an explicit compatibility fallback");
        Contains(
            importAction,
            "SupportsMaterialCubemapDataImport",
            "legacy fallback is gated by repository capability");
        Contains(
            importAction,
            "return EditService.SetMaterialCubemap(",
            "supported repositories propagate their real apply result");
        DoesNotContain(
            importAction,
            "IEnumerator ApplyFileSelectionOnMainThread",
            "Cubemap UI no longer owns the conversion coroutine");
    }

    private static void ProjectFilesIncludeDedicatedSources(
        string baseProject,
        string charaProject,
        string sceneProject)
    {
        Contains(baseProject, "CubemapOriginalSnapshot.cs", "Base includes Cubemap snapshot source");
        Contains(baseProject, "CubemapContentKey.cs", "Base includes the worker-safe content-key source");
        Contains(baseProject, "CubemapBackgroundRead.cs", "Base includes the background Cubemap reader");
        Contains(baseProject, "CubemapMaterialIdentity.cs", "Base includes stable Cubemap binding identities");
        Contains(baseProject, "CubemapMemoryBudget.cs", "Base includes Cubemap peak-memory estimates");
        Contains(baseProject, "CubemapCache.cs", "Base includes Cubemap cache ownership");
        Contains(baseProject, "CubemapImport.cs", "Base includes incremental Cubemap import");
        Contains(baseProject, "CubemapExport.cs", "Base includes Cubemap export");
        Contains(baseProject, "CubemapGpuReadback.cs", "Base includes GPU fallback readback");
        Contains(baseProject, "CubemapSource.cs", "Base includes Cubemap source inspection");
        Contains(baseProject, "RadianceHdrDecoder.cs", "Base includes the Radiance HDR decoder");
        Contains(baseProject, "UI.CubemapImportCoordinator.cs", "Base includes the incremental UI import coordinator");
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
