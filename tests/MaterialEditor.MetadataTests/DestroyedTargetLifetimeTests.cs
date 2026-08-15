using MaterialEditorAPI;
using UnityEngine;

internal static class DestroyedTargetLifetimeTests
{
    internal static void Run()
    {
        ClearingTargetDropsOnlyTargetOwnedState();
        ClearingTargetIsIdempotentAcrossFiveHundredCycles();
        CharacterOwnershipDistinguishesItemsAndSubtargets();
        ProductionCloseAndInvalidationPathsStaySeparate();
        ProductionLifecycleEventsUseDirectedInvalidation();
        ProductionCallbackRootsAreReleased();
        MakerStaticSubscriptionsAreSymmetric();
        Console.WriteLine("Destroyed-target lifetime regression tests passed.");
    }

    private static void ClearingTargetDropsOnlyTargetOwnedState()
    {
        var session = new MaterialEditorSessionState
        {
            CurrentGameObject = new GameObject(),
            CurrentData = new object(),
            Filter = "retained filter",
            ListsVisible = true,
            RenameListVisible = true
        };
        session.CollapsedMaterialSections["material"] = true;
        session.CollapsedShaderSections["shader"] = true;
        session.CollapsedPropertyCategories["category"] = true;

        var renderer = new GameObject().AddComponent<Renderer>();
        session.SelectedRenderers.Add(renderer);
        session.SelectedMaterials.Add(new Material());
        session.SelectedMaterialRenderers.Add(renderer);
        session.RequestObjExport(renderer);

        session.ClearTargetReferences();

        Equal(null, session.CurrentGameObject, "current GameObject released");
        Equal(null, session.CurrentData, "current data released");
        Equal(0, session.SelectedRenderers.Count, "renderer selection released");
        Equal(0, session.SelectedMaterials.Count, "material selection released");
        Equal(0, session.SelectedMaterialRenderers.Count,
            "rename renderer selection released");
        Equal(false, session.RenameListVisible, "rename state closed");
        Renderer exportedRenderer;
        Equal(false, session.TryTakeObjExport(out exportedRenderer),
            "pending OBJ export cancelled");
        Equal(null, exportedRenderer, "cancelled OBJ renderer released");

        Equal("retained filter", session.Filter, "filter preserved");
        Equal(true, session.ListsVisible, "side-panel preference preserved");
        Equal(true, session.CollapsedMaterialSections["material"],
            "material collapse state preserved");
        Equal(true, session.CollapsedShaderSections["shader"],
            "shader collapse state preserved");
        Equal(true, session.CollapsedPropertyCategories["category"],
            "category collapse state preserved");
    }

    private static void ClearingTargetIsIdempotentAcrossFiveHundredCycles()
    {
        var session = new MaterialEditorSessionState();
        for (var cycle = 0; cycle < 500; cycle++)
        {
            var renderer = new GameObject().AddComponent<Renderer>();
            session.CurrentGameObject = new GameObject();
            session.CurrentData = new object();
            session.SelectedRenderers.Add(renderer);
            session.SelectedMaterials.Add(new Material());
            session.SelectedMaterialRenderers.Add(renderer);
            session.RequestObjExport(renderer);
            session.RenameListVisible = true;

            session.ClearTargetReferences();
            session.ClearTargetReferences();

            Equal(null, session.CurrentGameObject,
                "idempotent target release cycle " + cycle);
            Equal(null, session.CurrentData,
                "idempotent data release cycle " + cycle);
            Equal(0, session.SelectedRenderers.Count,
                "idempotent renderer release cycle " + cycle);
            Equal(0, session.SelectedMaterials.Count,
                "idempotent material release cycle " + cycle);
            Equal(0, session.SelectedMaterialRenderers.Count,
                "idempotent rename release cycle " + cycle);
        }
    }

    private static void CharacterOwnershipDistinguishesItemsAndSubtargets()
    {
        Equal(true,
            MaterialEditorTargetOwnershipPolicy.IsOwnedByRoot(
                exactRoot: true,
                descendant: true,
                hasScopedTargetData: false),
            "exact character root accepts non-ObjectData context");
        Equal(true,
            MaterialEditorTargetOwnershipPolicy.IsOwnedByRoot(
                exactRoot: false,
                descendant: true,
                hasScopedTargetData: true),
            "ObjectData character subtarget belongs to its root");
        Equal(false,
            MaterialEditorTargetOwnershipPolicy.IsOwnedByRoot(
                exactRoot: false,
                descendant: true,
                hasScopedTargetData: false),
            "Studio item int parented under a bone stays independent");
        Equal(false,
            MaterialEditorTargetOwnershipPolicy.IsOwnedByRoot(
                exactRoot: false,
                descendant: false,
                hasScopedTargetData: true),
            "another character subtarget stays independent");
    }

    private static void ProductionCloseAndInvalidationPathsStaySeparate()
    {
        var uiSource = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.cs"));
        var close = ExtractMethod(
            uiSource,
            "private void ReleaseTransientUiContent(");
        DoesNotContain(close, "ClearTargetReferences",
            "normal close keeps a live target for direct reopen");
        DoesNotContain(close, "CloseTargetColorPalette",
            "normal close preserves the live palette workflow");

        var restore = ExtractMethod(
            uiSource,
            "private void RestoreTransientUiContent(");
        Contains(restore, "if (gameObject == null)",
            "reopen detects a destroyed Unity target");
        Contains(restore, "Session.ClearTargetReferences();",
            "destroyed reopen drops retained data");
        Contains(restore,
            "PopulateListCore(gameObject, data, filter, null, true);",
            "live reopen preserves Rename context");

        var invalidation = ExtractMethod(
            uiSource,
            "internal static void InvalidateCurrentTarget(");
        Contains(invalidation, "Visible = false;",
            "directed destruction closes the window");
        Contains(invalidation, "ReleaseRetainedTargetContext();",
            "directed destruction releases callback roots");
        Contains(invalidation, "Session.ClearTargetReferences();",
            "directed destruction clears strong session roots");
        var releaseSelections = ExtractMethod(
            uiSource,
            "internal static void ReleaseCurrentTargetSelections(");
        Contains(releaseSelections, "Session.CancelObjExport();",
            "content replacement cancels an OBJ export of an old renderer");
        var populate = ExtractMethod(
            uiSource,
            "private void PopulateListCore(");
        DoesNotContain(populate,
            "CloseTargetColorPalette();\n"
            + "                ClearInterpolablesForTarget(previousTarget);",
            "switching live targets preserves Timeline activation");
        Contains(populate, "if (previousTargetWasDestroyed)",
            "switching away from a destroyed target is distinguished");
        Contains(populate, "ClearInterpolablesForTarget(previousTarget);",
            "destroyed previous target releases its Timeline activation");

        var selectionSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Base", "UI", "UI.SelectionController.cs"));
        var closeRename = ExtractMethod(
            selectionSource,
            "internal void CloseRenamePanel(");
        Contains(closeRename, "ReleaseRenameContext();",
            "logical Rename close releases its old context");
        var releaseRename = ExtractMethod(
            selectionSource,
            "private void ReleaseRenameContext(");
        Contains(releaseRename,
            "_view.RenameButton.onClick.RemoveAllListeners();",
            "Rename button callback released");
        Contains(releaseRename, "_view.RenameList.ReleaseEntries();",
            "Rename renderer callbacks released");
        Contains(releaseRename,
            "_session.SelectedMaterialRenderers.Clear();",
            "Rename renderer selection released");
    }

    private static void ProductionLifecycleEventsUseDirectedInvalidation()
    {
        var coreSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core", "Core.MaterialEditor.cs"));
        Contains(coreSource,
            "MakerAPI.MakerExiting += (s, e) =>\n                MaterialEditorUI.InvalidateAllTargetState();",
            "Maker exit clears all target state");

        var characterSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.cs"));
        var onDestroy = ExtractMethod(
            characterSource,
            "protected override void OnDestroy(");
        Contains(onDestroy, "MaterialEditorUI.NotifyTargetDestroyed(targetRoot);",
            "character destruction invalidates by ownership root");
        var notify = onDestroy.IndexOf(
            "MaterialEditorUI.NotifyTargetDestroyed(targetRoot);",
            StringComparison.Ordinal);
        var baseDestroy = onDestroy.IndexOf("base.OnDestroy();",
            StringComparison.Ordinal);
        Equal(true, notify >= 0 && baseDestroy > notify,
            "target is released before base character destruction");

        var sceneSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.SceneController.cs"));
        var sceneLoad = ExtractMethod(
            sceneSource,
            "protected override void OnSceneLoad(");
        Contains(sceneLoad,
            "operation == SceneOperationKind.Clear\n                || operation == SceneOperationKind.Load",
            "scene Clear and Load are destructive lifecycle gates");
        Contains(sceneLoad, "MaterialEditorUI.InvalidateAllTargetState();",
            "scene Clear and Load release all targets");
        var sceneDelete = ExtractMethod(
            sceneSource,
            "protected override void OnObjectDeleted(");
        Contains(sceneDelete, "currentId == id",
            "Studio item deletion matches persisted item identity");
        Contains(sceneDelete, "NotifyTargetDestroyed(targetRoot);",
            "Studio character deletion matches ownership root");
        Contains(sceneDelete, "targetControl = character.GetChaControl();",
            "Studio character deletion retains exact control identity");
        Contains(sceneDelete,
            "ReleaseItemTypeDropdownTarget(targetControl);",
            "Studio character deletion releases only its own dropdown");
        DoesNotContain(sceneDelete,
            "ReleaseItemTypeDropdownTarget(targetRoot);",
            "unknown character root never means clear every dropdown");

        var eventSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Events.cs"));
        Contains(eventSource,
            "data.ObjectType == objectType\n                                       && data.Slot == slot",
            "slot replacement matches exact ObjectData kind and slot");
        Contains(eventSource, "CurrentUiTargetBelongsToCharacter()",
            "Studio characters with equal slot data cannot cross-invalidate");
        Contains(eventSource,
            "MaterialEditorTargetOwnershipPolicy.IsOwnedByRoot(",
            "character ownership uses the production-tested policy");
        Contains(eventSource, "CurrentUiTargetIsCharacterRoot()",
            "non-character data is allowed only on the exact character root");
        Contains(eventSource, "PruneDestroyedUiTargetsNextFrame()",
            "post-destroy Unity fake-null cleanup is event-driven");
        Contains(eventSource,
            "if (StartCoroutine(PruneDestroyedUiTargetsNextFrame()) != null)",
            "failed coroutine scheduling is detected");
        Contains(eventSource, "_uiLifetimePruneScheduled = false;",
            "failed coroutine scheduling releases its coalescing gate");

        var accessoryKind = ExtractMethod(
            eventSource,
            "internal void AccessoryKindChangeEvent(");
        var releaseSlot = accessoryKind.IndexOf(
            "ReleaseUiForObjectReplacement(",
            StringComparison.Ordinal);
        var refreshAccessory = accessoryKind.IndexOf(
            "MEMaker.Instance.UpdateUIAccessory();",
            StringComparison.Ordinal);
        Equal(true, releaseSlot >= 0 && refreshAccessory > releaseSlot,
            "accessory replacement releases the old slot before refresh");
    }

    private static void ProductionCallbackRootsAreReleased()
    {
        var uiSource = ReadRepositorySource(
            Path.Combine("src", "MaterialEditor.Base", "UI", "UI.cs"));
        var allTargets = ExtractMethod(
            uiSource,
            "internal static void InvalidateAllTargetState(");
        Contains(allTargets, "selectedInterpolable = null;",
            "all-target invalidation releases material Timeline selection");
        Contains(allTargets, "selectedProjectorInterpolable = null;",
            "all-target invalidation releases projector Timeline selection");

        var timelineSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.TimelineCompatibilityHelper.cs"));
        Contains(timelineSource, "selectedInterpolable.GameObject == null",
            "Timeline material parameter rejects destroyed GameObjects");
        Contains(timelineSource,
            "selectedProjectorInterpolable.GameObject == null",
            "Timeline projector parameter rejects destroyed GameObjects");

        var paletteSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.Studio.ColorPalette.cs"));
        Contains(paletteSource, "Studio.colorMenu.updateColorFunc = null;",
            "PH Studio palette releases its engine callback");
        Contains(paletteSource, "var palette = Studio.colorPalette;",
            "Studio palette instance is captured for an atomic close");
        Contains(paletteSource, "palette.Close();",
            "other Studio targets use the canonical palette close path");
        Contains(paletteSource, "palette.visible = false;",
            "Studio palette close rearms its reactive visibility for reopen");
        Contains(paletteSource, "if (!palette.isOpen)",
            "Studio palette setup detects a native physical close");
        Equal(true,
            CountOccurrences(paletteSource, "palette.visible = false;") >= 2,
            "close and setup both rearm Studio palette visibility");
        Contains(paletteSource, "_onChanged = null;",
            "Studio palette wrapper releases callback state");
        Equal(true, CountOccurrences(paletteSource, "finally") >= 3,
            "palette callbacks and wrapper fields clear through finally");

        var studioSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.Studio.cs"));
        var dropdownRelease = ExtractMethod(
            studioSource,
            "internal void ReleaseItemTypeDropdownTarget(");
        Contains(dropdownRelease,
            "dropdown.onValueChanged.RemoveAllListeners();",
            "Studio item-type dropdown releases captured ChaControl");
        Contains(studioSource,
            "!ReferenceEquals(_itemTypeTarget, destroyedTarget)",
            "destroyed ChaControl releases only its own dropdown callback");
        Contains(studioSource,
            "if (ReferenceEquals(destroyedTarget, null)",
            "unknown ChaControl identity never clears every dropdown");
        Contains(studioSource,
            "SceneManager.sceneLoaded -= SceneManagerSceneLoaded;",
            "Studio scene-load subscription has an exact removal path");
    }

    private static void MakerStaticSubscriptionsAreSymmetric()
    {
        var makerSource = ReadRepositorySource(Path.Combine(
            "src", "MaterialEditor.Core.Maker",
            "Core.MaterialEditor.Maker.cs"));
        var start = ExtractMethod(makerSource, "private void Start(");
        var destroy = ExtractMethod(makerSource, "private void OnDestroy(");
        var subscriptions = new[]
        {
            "MakerAPI.MakerBaseLoaded",
            "MakerAPI.RegisterCustomSubCategories",
            "MakerAPI.MakerFinishedLoading",
            "MakerAPI.ReloadCustomInterface",
            "MakerAPI.MakerExiting",
            "AccessoriesApi.SelectedMakerAccSlotChanged",
            "AccessoriesApi.AccessoryKindChanged",
            "AccessoriesApi.AccessoryTransferred",
            "AccessoriesApi.AccessoriesCopied"
        };

        foreach (var subscription in subscriptions)
        {
            Contains(start, subscription + " +=",
                subscription + " is registered");
            Contains(destroy, subscription + " -=",
                subscription + " is unregistered");
        }
        DoesNotContain(start, "MakerAPI.MakerFinishedLoading += (",
            "Maker lifecycle subscription is not an anonymous callback");
        DoesNotContain(start, "AccessoriesApi.AccessoryTransferred += (",
            "accessory lifecycle subscription is not anonymous");

        var makerExit = ExtractMethod(
            makerSource,
            "private void MakerAPI_MakerExiting(");
        Contains(makerExit, "InvalidateAllTargetState();",
            "Maker exit releases target state before palette ownership ends");
        Contains(makerExit, "ColorPalette = null;",
            "Maker exit releases the palette wrapper");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
            throw new InvalidOperationException("Method not found: " + signature);
        var openingBrace = source.IndexOf('{', signatureIndex);
        var depth = 0;
        for (var index = openingBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(openingBrace, index - openingBrace + 1);
        }
        throw new InvalidOperationException("Unterminated method: " + signature);
    }

    private static string ReadRepositorySource(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath))
            .Replace("\r\n", "\n");

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[]
                 {
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory
                 })
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
        throw new DirectoryNotFoundException(
            "Could not locate the repository root.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(
                    value,
                    index,
                    StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Contains(string source, string expected, string name) =>
        Equal(true, source.Contains(expected, StringComparison.Ordinal), name);

    private static void DoesNotContain(
        string source,
        string expected,
        string name) =>
        Equal(false, source.Contains(expected, StringComparison.Ordinal), name);

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
