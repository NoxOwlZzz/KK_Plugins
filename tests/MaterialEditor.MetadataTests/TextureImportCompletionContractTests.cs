internal static class TextureImportCompletionContractTests
{
    internal static void Run()
    {
        var root = FindRepositoryRoot();
        var repositoryContract = Read(root, "src", "MaterialEditor.Base", "IMaterialEditRepository.cs");
        var editService = Read(root, "src", "MaterialEditor.Base", "MaterialEditService.cs");
        var ui = Read(root, "src", "MaterialEditor.Base", "UI", "UI.cs");
        var charaRepository = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaRepository.cs");
        var charaTexture = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Edits.TextureShader.cs");
        var charaEvents = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Events.cs");
        var charaController = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.cs");
        var sceneRepository = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneRepository.cs");
        var sceneTexture = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Edits.TextureShader.cs");
        var sceneController = Read(root, "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.cs");
        var animation = Read(root, "src", "MaterialEditor.Core", "Core.MaterialEditor.Animation.cs");

        Contains(
            repositoryContract,
            "interface IMaterialTextureImportCompletionRepository",
            "deferred Texture2D imports expose an internal completion capability");
        Contains(
            editService,
            "repository as IMaterialTextureImportCompletionRepository",
            "the edit service uses completion-aware repositories without changing the public repository contract");
        Contains(
            editService,
            "completed?.Invoke(false)",
            "the synchronous compatibility path reports failures");

        AssertDeferredBackend(
            charaRepository,
            charaTexture,
            charaController,
            "character");
        AssertDeferredBackend(
            sceneRepository,
            sceneTexture,
            sceneController,
            "Studio");
        AssertAtomicBackend(charaTexture, charaEvents, charaController, "character");
        AssertAtomicBackend(sceneTexture, sceneTexture, sceneController, "Studio");
        Contains(
            animation,
            "Action<Controller, GameObject, Property, int> UpdateTexture",
            "legacy animation callback remains binary compatible");
        Contains(
            animation,
            "Func<Controller, GameObject, Property, int, bool> TryUpdateTexture",
            "animation texture updates report their real result");
        Contains(
            animation,
            "public void UpdateAnimation(",
            "legacy animation method remains binary compatible");
        Contains(
            animation,
            "if (UpdateTexture != null)",
            "a legacy callback remains authoritative when supplied");
        Contains(
            animation,
            "public bool TryUpdateAnimation(",
            "animation frame application exposes success");
        Contains(
            animation,
            "|| !TryUpdateTexture(",
            "failed animation frame application stays retryable");

        var import = Slice(ui, "string filePath = files[0];", "DisposeTexChangeWatcher();");
        Contains(import, "succeeded =>", "Texture2D UI state waits for the backend result");
        Contains(
            import,
            "GetMaterialTextureValueOriginal",
            "Texture2D changed state is read back from persistence");
        Contains(
            import,
            "MaterialPropertyAccess.GetTexture",
            "Texture2D existence is read back from the material");
        DoesNotContain(import, "textureItem.Changed = true", "Texture2D import is not reported optimistically");
        DoesNotContain(import, "textureItem.Exists = true", "Texture2D existence is not reported optimistically");
    }

    private static void AssertDeferredBackend(
        string repository,
        string textureEdits,
        string controller,
        string name)
    {
        Contains(
            repository,
            "IMaterialTextureImportCompletionRepository",
            name + " repository advertises completion support");
        Contains(
            repository,
            "QueueMaterialTextureFromFile(",
            name + " repository queues the completion-aware import");
        Contains(
            textureEdits,
            "Action<bool> completed",
            name + " deferred import carries its completion callback");
        Contains(
            textureEdits,
            "TextureImportCompleted?.Invoke(false)",
            name + " reports a displaced pending request");
        Contains(
            controller,
            "completed?.Invoke(succeeded)",
            name + " reports the actual Update result");
        Contains(
            controller,
            "TrySetMaterialTextureFromFile(",
            name + " derives completion from the real import path");
    }

    private static void AssertAtomicBackend(
        string textureEdits,
        string applySource,
        string controller,
        string name)
    {
        var import = Slice(
            textureEdits,
            "private bool TrySetMaterialTexture(",
            "private void CommitTextureImport(");
        Contains(
            import,
            "var existingProperty = MaterialTexturePropertyList.FirstOrDefault",
            name + " captures the existing override before decoding");
        Contains(
            import,
            "candidateProperty = new MaterialTextureProperty(",
            name + " applies through a detached candidate property");
        Contains(
            import,
            "if (!SetTextureWithProperty(go, candidateProperty))",
            name + " validates application before commit");
        Contains(
            import,
            "CommitTextureImport(existingProperty, candidateProperty);",
            name + " commits only after application succeeds");
        Contains(
            import,
            "PurgeUnusedTextures();",
            name + " removes unreferenced data after a failed attempt");

        var commit = Slice(
            textureEdits,
            "private void CommitTextureImport(",
            "/// <summary>");
        Contains(
            commit,
            "MaterialTexturePropertyList.Add(candidateProperty);",
            name + " publishes a new override only on commit");
        Contains(
            commit,
            "existingProperty.TexID = previousTexID;",
            name + " restores an existing texture ID if commit throws");
        Contains(
            commit,
            "existingProperty.TexAnimationDef = previousAnimationDefinition;",
            name + " restores existing animation semantics if commit throws");
        Contains(
            commit,
            "AnimationControllerMap[existingProperty] = previousController;",
            name + " restores the previous animation controller if commit throws");
        Contains(
            applySource,
            "return controller.TryUpdateAnimation(textureProperty);",
            name + " animated import commits only after a frame is applied");
        Contains(
            controller,
            "bool SetTextureForAnimation(",
            name + " animation callback reports application success");
        Contains(
            controller,
            "MEAnimationController.TryUpdateTexture = SetTextureForAnimation;",
            name + " uses the result-aware animation seam");
        Contains(
            controller,
            "return SetTexture(go, property.MaterialName, property.Property, tex.Texture);",
            name + " animation callback propagates MaterialAPI success");
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

    private static void Contains(string source, string value, string label)
    {
        if (!source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException("Missing " + label + ": " + value);
    }

    private static void DoesNotContain(string source, string value, string label)
    {
        if (source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException("Unexpected " + label + ": " + value);
    }
}
