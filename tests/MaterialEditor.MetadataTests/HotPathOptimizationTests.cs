using KK_Plugins.MaterialEditor;

internal static class HotPathOptimizationTests
{
    internal static void Run()
    {
        AnimationValidationRejectsUnsafeDefinitions();
        AnimationValidationAcceptsValidDefinitions();
        RefreshRequestsAreCoalescedUntilCompletion();
        RuntimeHotPathsUseBoundedWork();
        Console.WriteLine("Idle hot-path regression tests passed.");
    }

    private static void AnimationValidationRejectsUnsafeDefinitions()
    {
        False(MEAnimationValidation.IsUsable(0f, 1), "zero total time");
        False(MEAnimationValidation.IsUsable(-1f, 1), "negative total time");
        False(MEAnimationValidation.IsUsable(float.NaN, 1), "NaN total time");
        False(MEAnimationValidation.IsUsable(float.PositiveInfinity, 1), "infinite total time");
        False(MEAnimationValidation.IsUsable(1f, 0), "empty frame array");
    }

    private static void AnimationValidationAcceptsValidDefinitions()
    {
        True(MEAnimationValidation.IsUsable(1f / 24f, 1), "single frame");
        True(MEAnimationValidation.IsUsable(120f, 7200), "long animation");
        Equal(0.5f, MEAnimationValidation.WrapTime(2.5f, 1f), "ordinary time wrap");
        Equal(-0.5f, MEAnimationValidation.WrapTime(-0.5f, 1f), "negative time stays unchanged");
        Equal(0f, MEAnimationValidation.WrapTime(float.NaN, 1f), "NaN time reset");
        Equal(0f, MEAnimationValidation.WrapTime(float.PositiveInfinity, 1f), "infinite time reset");
        Equal(0f, MEAnimationValidation.WrapTime(1f, 0f), "invalid duration reset");
        var bounded = MEAnimationValidation.WrapTime(float.MaxValue, float.Epsilon);
        True(!float.IsNaN(bounded) && !float.IsInfinity(bounded), "extreme time wrap remains finite");
        True(bounded >= 0f && bounded < float.Epsilon, "extreme time wrap is bounded");
    }

    private static void RefreshRequestsAreCoalescedUntilCompletion()
    {
        var gate = new EndOfFrameRefreshGate();
        var accepted = 0;
        for (var index = 0; index < 100; index++)
            if (gate.TryRequest())
                accepted++;
        Equal(1, accepted, "coalesced refresh requests");
        True(gate.IsPending, "refresh remains pending before completion");
        gate.Complete();
        True(!gate.IsPending, "refresh completion clears pending state");
        True(gate.TryRequest(), "next-frame refresh is accepted");
        gate.Complete();
    }

    private static void RuntimeHotPathsUseBoundedWork()
    {
        var root = FindRepositoryRoot();
        var animation = ReadNormalized(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.Animation.cs");
        Contains(
            animation,
            "if (controllerMap == null || controllerMap.Count == 0)\n                return;",
            "empty animation-map early return");
        Contains(
            animation,
            "List<Property> removed = null;",
            "lazy animation removal list");

        var hooks = ReadNormalized(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.Hooks.cs");
        var eyeHook = Slice(
            hooks,
            "private static void EyeLookMaterialControll_Update_Postfix",
            "[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType)");
        Equal(1, Count(eyeHook, ".sharedMaterials"), "eye shared-material snapshots");
        Equal(1, Count(eyeHook, "GetTextureOffset"), "eye offset reads per texture state");
        Equal(1, Count(eyeHook, "GetTextureScale"), "eye scale reads per texture state");

        var siruHook = Slice(
            hooks,
            "private static void ChaControl_UpdateSiru_Postfix",
            "#if KK || KKS");
        Equal(2, Count(siruHook, ".sharedMaterials"), "siru shared-material snapshots");
        Contains(siruHook, "faceMaterials.Length > 1", "siru face copy guard");
        Contains(siruHook, "bodyMaterials.Length > 1", "siru body copy guard");

        var normalMaps = ReadNormalized(root, "src", "MaterialEditor.Base", "NormalMapManager.cs");
        Contains(normalMaps, "if (_convertedNormalMap.Count == 0)", "empty normal-map cache early return");
        Contains(normalMaps, "NormalMapSweepIntervalFrames = 60", "normal-map sweep cadence");

        var chara = ReadNormalized(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.cs");
        var pendingCharacterTexture = Slice(
            chara,
            "private void SetMaterialTextureFromFileByUpdate()",
            "internal int GetCoordinateIndex");
        Contains(pendingCharacterTexture, "if (FileToSet == null)\n                return;", "character pending-texture idle return");
        Contains(pendingCharacterTexture, "finally", "character pending-texture cleanup");

        var scene = ReadNormalized(
            root,
            "src",
            "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.SceneController.cs");
        var pendingSceneTexture = Slice(
            scene,
            "private void Update()",
            "private void SetRendererPropertyRecursive");
        Contains(pendingSceneTexture, "if (FileToSet != null)\n            {", "scene pending-texture idle guard");
        Contains(pendingSceneTexture, "finally", "scene pending-texture cleanup");
        Contains(
            pendingSceneTexture,
            "            }\n\n            MEAnimationController.UpdateAnimations(AnimationControllerMap);",
            "scene animation update remains outside pending-texture guard");

        var charaEvents = ReadNormalized(
            root,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Events.cs");
        var clothesRefresh = Slice(
            charaEvents,
            "public void RefreshClothesMainTex()",
            "private bool SetTextureWithProperty");
        Contains(
            clothesRefresh,
            "if (StartCoroutine(RefreshClothesMainTexCoroutine()) != null)",
            "clothes refresh accepts only a scheduled coroutine");
        Equal(3, Count(clothesRefresh, "_clothesMainTexRefreshGate.Complete();"),
            "clothes refresh gate null/exception/finally release paths");

        var bodyRefresh = Slice(
            charaEvents,
            "public void RefreshBodyMainTex()",
            "public void RefreshBodyEdits()");
        Contains(
            bodyRefresh,
            "if (StartCoroutine(RefreshBodyMainTexCoroutine()) != null)",
            "body refresh accepts only a scheduled coroutine");
        Equal(3, Count(bodyRefresh, "_bodyMainTexRefreshGate.Complete();"),
            "body refresh gate null/exception/finally release paths");
    }

    private static string ReadNormalized(string root, params string[] parts)
    {
        var path = root;
        foreach (var part in parts)
            path = Path.Combine(path, part);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0)
            throw new InvalidOperationException(
                "Could not locate guarded hot-path source start block.");
        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            throw new InvalidOperationException(
                "Could not locate guarded hot-path source end block.");
        return source.Substring(startIndex, endIndex - startIndex);
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static void Contains(string source, string value, string name)
    {
        if (source.IndexOf(value, StringComparison.Ordinal) < 0)
            throw new InvalidOperationException(name + " guard was not found.");
    }

    private static void Equal(int expected, int actual, string name)
    {
        if (expected != actual)
            throw new InvalidOperationException(
                name + " expected " + expected + " but got " + actual + ".");
    }

    private static void Equal(float expected, float actual, string name)
    {
        if (expected != actual)
            throw new InvalidOperationException(
                name + " expected " + expected + " but got " + actual + ".");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException(name + " should be accepted.");
    }

    private static void False(bool value, string name)
    {
        if (value)
            throw new InvalidOperationException(name + " should be rejected.");
    }
}
