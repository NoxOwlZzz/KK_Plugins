internal static class UiCopyPasteVisibilityTests
{
    internal static void Run()
    {
        PrimaryActionsRemainVisibleAndUseExistingBackends();
        ClipboardStateRefreshIsTargetedAndLifetimeSafe();
        ClipboardStateCoversExternalMutationAndMultipleViews();
        ClipboardPasteLeaseSanitizesAndRestores();
        ClipboardCompatibilityUsesTheActualTarget();
        UnknownShaderDoesNotInferCubemapFromGlobalFallback();
        TextureAndCubemapPasteStayTypeSafe();
        ClipboardCompatibilityToleratesNullListsAndItems();
        ProjectorIdentityFlowsFromTheRowToBothBackends();
        CharacterProjectorPasteUsesTheProjectorClipboardCount();
        Console.WriteLine("Material Copy/Paste visibility guards passed.");
    }

    private static void PrimaryActionsRemainVisibleAndUseExistingBackends()
    {
        var factory = ReadSource("UI.RowViewFactory.MaterialShader.cs");
        var controls = ReadSource("UI.RowControls.cs");
        var binder = ReadSource("UI.RowBinder.MaterialShader.cs");
        var presenter = ReadSource("UI.MaterialSectionPresenter.cs");
        var menu = ReadSource("UI.RowActionMenu.cs");

        Equal(1, Count(factory, "\"MaterialCopyEditsButton\""),
            "one visible Copy Edits control per pooled template");
        Equal(1, Count(factory, "\"MaterialPasteEditsButton\""),
            "one visible Paste Edits control per pooled template");
        Contains(factory, "\"Copy Edits\"");
        Contains(factory, "\"Paste Edits\"");
        Contains(controls, "internal Button CopyEditsButton { get; }");
        Contains(controls, "internal Button PasteEditsButton { get; }");

        Contains(presenter,
            "Copy = () => context.Edits.CopyMaterialEdits(");
        Contains(presenter, "context.Projector");
        Contains(presenter,
            "context.Edits.PasteMaterialEdits(");
        Contains(binder, "listeners.Listen(controls.CopyEditsButton");
        Contains(binder, "listeners.Listen(controls.PasteEditsButton");
        Contains(binder, "item.Copy();");
        Contains(binder, "item.Paste();");

        DoesNotContain(menu, "\"Copy Edits\"");
        DoesNotContain(menu, "\"Paste Edits\"");
        DoesNotContain(factory, "MaterialPasteEditsButton.gameObject.SetActive");
        DoesNotContain(binder, "MaterialEditService");
        DoesNotContain(binder, "PopulateList");
    }

    private static void ClipboardStateRefreshIsTargetedAndLifetimeSafe()
    {
        var binder = ReadSource("UI.RowBinder.cs");
        var materialBinder = ReadSource("UI.RowBinder.MaterialShader.cs");

        Contains(materialBinder,
            "MaterialEditorClipboardPolicy.CanPaste(");
        Contains(materialBinder,
            "controls.PasteEditsButton.interactable = canPaste;");
        Contains(materialBinder,
            "? \"Paste all copied edits into this material\"");
        Contains(materialBinder,
            "\"Copy material edits before pasting\"");
        Contains(materialBinder,
            "\"Copied edits are not compatible with this material\"");
        DoesNotContain(materialBinder, "NotifyClipboardChanged();");
        Contains(materialBinder, "ListenForClipboardChanges(");
        Contains(binder, "listeners.OnDispose(() => _clipboardViewState.Changed -= listener);");
        Contains(binder, "internal sealed class MaterialEditorClipboardViewState");
        Contains(binder, "MaterialEditorClipboardState.Changed += HandleGlobalChange;");
        Contains(binder, "_snapshot.Capture(MaterialEditorPluginBase.CopyData)");
        Contains(binder,
            "_snapshot.Capture(MaterialEditorPluginBase.CopyData);\n            RaiseChanged();");
        Contains(binder, "Changed = null;");

        DoesNotContain(materialBinder, "SetList(");
        DoesNotContain(materialBinder, "LayoutRebuilder");
        DoesNotContain(materialBinder, "RefreshDeferred");
        DoesNotContain(materialBinder, "StartCoroutine");

        var policy = ReadSource("UI.MaterialClipboardPolicy.cs");
        Contains(policy, "internal static bool CanPaste(");
        Contains(policy, "projector != null && HasProjectorEdit(clipboard)");
        Contains(policy, "material.HasProperty(\"_\" + propertyName)");
        DoesNotContain(policy, "MaterialEditService");
        DoesNotContain(policy, "PopulateList");
    }

    private static void ClipboardStateCoversExternalMutationAndMultipleViews()
    {
        var clipboard = new MaterialEditorAPI.CopyContainer();
        var snapshotA = new MaterialEditorAPI.MaterialEditorClipboardSnapshot();
        var snapshotB = new MaterialEditorAPI.MaterialEditorClipboardSnapshot();
        Equal(true, snapshotA.Capture(clipboard), "first view captures clipboard");
        Equal(true, snapshotB.Capture(clipboard), "second view captures clipboard");
        Equal(false, snapshotA.Capture(clipboard), "stable clipboard is idle");

        clipboard.MaterialFloatPropertyList.Add(
            new MaterialEditorAPI.CopyContainer.MaterialFloatProperty("Gloss", 0.5f));
        Equal(true, snapshotA.Capture(clipboard), "direct public-list mutation is observed");
        Equal(true, snapshotB.Capture(clipboard), "all views observe the same mutation");
        Equal(false, snapshotA.Capture(clipboard), "warm snapshot performs no work twice");

        clipboard.MaterialFloatPropertyList[0] =
            new MaterialEditorAPI.CopyContainer.MaterialFloatProperty("Gloss", 0.5f);
        Equal(true, snapshotA.Capture(clipboard),
            "same-count equal-content item replacement is observed");
        Equal(true, snapshotB.Capture(clipboard),
            "all views observe same-count item replacement");
        Equal(false, snapshotA.Capture(clipboard),
            "replacement snapshot becomes stable");

        clipboard.MaterialFloatPropertyList[0].Value = 0.75f;
        Equal(true, snapshotA.Capture(clipboard),
            "same-count public item mutation is observed");
        Equal(true, snapshotB.Capture(clipboard),
            "all views observe same-count item mutation");
        Equal(false, snapshotA.Capture(clipboard),
            "mutated snapshot becomes stable");

        clipboard.MaterialCubemapPropertyList.Add(
            new MaterialEditorAPI.CopyContainer.MaterialCubemapProperty(
                "Environment",
                new byte[] { 1, 2 }));
        Equal(true, snapshotA.Capture(clipboard),
            "Cubemap-list mutation is observed");
        Equal(true, snapshotB.Capture(clipboard),
            "all views observe the Cubemap mutation");
        Equal(false, snapshotA.Capture(clipboard),
            "Cubemap snapshot becomes stable");
        clipboard.MaterialCubemapPropertyList[0].Data = new byte[] { 3, 4 };
        Equal(true, snapshotA.Capture(clipboard),
            "same-count Cubemap data replacement is observed");
        Equal(true, snapshotB.Capture(clipboard),
            "all views observe Cubemap data replacement");

        var firstView = 0;
        var secondView = 0;
        Action first = () => firstView++;
        Action second = () => secondView++;
        MaterialEditorAPI.MaterialEditorClipboardState.Changed += first;
        MaterialEditorAPI.MaterialEditorClipboardState.Changed += second;
        MaterialEditorAPI.MaterialEditorClipboardState.NotifyChanged();
        MaterialEditorAPI.MaterialEditorClipboardState.Changed -= first;
        MaterialEditorAPI.MaterialEditorClipboardState.Changed -= second;
        Equal(1, firstView, "facade change reaches first view");
        Equal(1, secondView, "facade change reaches second view");

        var facade = ReadRepositorySource(
            "src", "MaterialEditor.Base", "MaterialEditorEditServiceFacade.cs");
        Contains(facade, "MaterialEditorClipboardState.NotifyChanged();");
    }

    private static void ClipboardPasteLeaseSanitizesAndRestores()
    {
        var previousGlobalClipboard =
            MaterialEditorAPI.MaterialEditorPluginBase.CopyData;
        try
        {
            MaterialEditorAPI.MaterialEditorPluginBase.CopyData = null;
            var ensured =
                MaterialEditorAPI.MaterialEditorClipboardState.EnsureClipboard();
            Equal(true, ensured != null, "copy path recreates a null clipboard");
            Equal(true,
                ReferenceEquals(
                    ensured,
                    MaterialEditorAPI.MaterialEditorPluginBase.CopyData),
                "recreated clipboard becomes the shared backend container");
        }
        finally
        {
            MaterialEditorAPI.MaterialEditorPluginBase.CopyData = previousGlobalClipboard;
        }

        var keyword = new MaterialEditorAPI.CopyContainer.MaterialKeywordProperty(
            "DETAIL_ON",
            true);
        var projector = new MaterialEditorAPI.CopyContainer.ProjectorProperty(
            MaterialEditorAPI.MaterialAPI.ProjectorProperties.FieldOfView,
            45f);
        var cubemap = new MaterialEditorAPI.CopyContainer.MaterialCubemapProperty(
            "Environment",
            new byte[] { 1, 2, 3 });
        var clipboard = new MaterialEditorAPI.CopyContainer
        {
            MaterialFloatPropertyList = null,
            MaterialKeywordPropertyList =
                new List<MaterialEditorAPI.CopyContainer.MaterialKeywordProperty>
                {
                    null,
                    keyword
                },
            MaterialCubemapPropertyList =
                new List<MaterialEditorAPI.CopyContainer.MaterialCubemapProperty>
                {
                    null,
                    cubemap
                },
            ProjectorPropertyList =
                new List<MaterialEditorAPI.CopyContainer.ProjectorProperty>
                {
                    null,
                    projector
                }
        };
        var originalFloats = clipboard.MaterialFloatPropertyList;
        var originalKeywords = clipboard.MaterialKeywordPropertyList;
        var originalCubemaps = clipboard.MaterialCubemapPropertyList;
        var originalProjectors = clipboard.ProjectorPropertyList;
        var expectedException = false;

        try
        {
            using (var lease =
                   new MaterialEditorAPI.MaterialEditorClipboardPasteLease(
                       clipboard,
                       true))
            {
                Equal(0, clipboard.MaterialFloatPropertyList.Count,
                    "lease substitutes a non-null float list");
                Equal(1, clipboard.MaterialKeywordPropertyList.Count,
                    "lease removes null material items");
                Equal(true,
                    ReferenceEquals(keyword, clipboard.MaterialKeywordPropertyList[0]),
                    "lease retains valid material item identity");
                Equal(1, clipboard.MaterialCubemapPropertyList.Count,
                    "lease removes null Cubemap items");
                Equal(true,
                    ReferenceEquals(cubemap, clipboard.MaterialCubemapPropertyList[0]),
                    "lease retains valid Cubemap item identity");
                Equal(1, lease.ProjectorEdits.Count,
                    "lease exposes valid projector edits separately");
                Equal(0, clipboard.ProjectorPropertyList.Count,
                    "projector-aware paste isolates projector edits from material backend");
                throw new ApplicationException("backend failure");
            }
        }
        catch (ApplicationException)
        {
            expectedException = true;
        }

        Equal(true, expectedException, "simulated backend failure reached finally path");
        Equal(true, ReferenceEquals(originalFloats, clipboard.MaterialFloatPropertyList),
            "lease restores an original null float-list reference");
        Equal(true, ReferenceEquals(originalKeywords, clipboard.MaterialKeywordPropertyList),
            "lease restores original material-list identity");
        Equal(true, ReferenceEquals(originalCubemaps, clipboard.MaterialCubemapPropertyList),
            "lease restores original Cubemap-list identity");
        Equal(true, ReferenceEquals(originalProjectors, clipboard.ProjectorPropertyList),
            "lease restores original projector-list identity");

        using (var lease =
               new MaterialEditorAPI.MaterialEditorClipboardPasteLease(
                   clipboard,
                   false))
        {
            Equal(true,
                ReferenceEquals(lease.ProjectorEdits, clipboard.ProjectorPropertyList),
                "legacy paste receives the sanitized projector list");
        }

        var service = ReadRepositorySource(
            "src", "MaterialEditor.Base", "MaterialEditService.cs");
        Contains(service, "MaterialEditorClipboardState.EnsureClipboard();");
        Contains(service,
            "MaterialEditorClipboardState.EnsureClipboard();\n            GetRepository(data).MaterialCopyEdits");
        Contains(service,
            "var clipboard = MaterialEditorPluginBase.CopyData;\n            if (clipboard == null)\n                return;\n            using (new MaterialEditorClipboardPasteLease(");
        Contains(service, "new MaterialEditorClipboardPasteLease(");
        DoesNotContain(service, "private sealed class ClipboardPasteLease");
    }

    private static void CharacterProjectorPasteUsesTheProjectorClipboardCount()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
                root,
                "src",
                "MaterialEditor.Core",
                "Core.MaterialEditor.CharaController.Edits.Core.cs"))
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
        var projectorPaste = Slice(
            source,
            "var targetProjector = GetProjectorList(objectType, go)",
            "public void MaterialCopyRemove(");

        Contains(projectorPaste,
            "i < CopyData.ProjectorPropertyList.Count");
        Contains(projectorPaste,
            "var projectorProperty = CopyData.ProjectorPropertyList[i];");
        DoesNotContain(projectorPaste,
            "i < CopyData.MaterialTexturePropertyList.Count");
    }

    private static void ClipboardCompatibilityUsesTheActualTarget()
    {
        var material = new UnityEngine.Material();
        var clipboard = new MaterialEditorAPI.CopyContainer();
        Equal(false,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                null),
            "empty clipboard");

        clipboard.MaterialFloatPropertyList.Add(
            new MaterialEditorAPI.CopyContainer.MaterialFloatProperty(
                "Gloss",
                0.5f));
        Equal(false,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                null),
            "property absent on target");
        material.AddProperty("_Gloss");
        Equal(true,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                null),
            "property present on target");

        clipboard.ClearAll();
        clipboard.MaterialCubemapPropertyList.Add(
            new MaterialEditorAPI.CopyContainer.MaterialCubemapProperty(
                "Environment",
                new byte[] { 1 }));
        Equal(false,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                null),
            "Cubemap property absent on target");
        material.AddProperty("_Environment");
        const string textureShader = "Tests/TextureTarget";
        const string cubemapShader = "Tests/CubemapTarget";
        var catalogs = MaterialEditorAPI.MaterialEditorPluginBase
            .XMLShaderProperties;
        catalogs[textureShader] = new Dictionary<
            string,
            MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData>
        {
            ["Environment"] =
                new MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData(
                    "Environment",
                    MaterialEditorAPI.MaterialAPI.ShaderPropertyType.Texture)
        };
        catalogs[cubemapShader] = new Dictionary<
            string,
            MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData>
        {
            ["Environment"] =
                new MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData(
                    "Environment",
                    MaterialEditorAPI.MaterialAPI.ShaderPropertyType.Cubemap)
        };
        try
        {
            material.shader.name = textureShader;
            Equal(false,
                MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                    clipboard,
                    material,
                    null),
                "Cubemap payload is rejected by a Texture target");
            Equal(true,
                MaterialEditorAPI.MaterialEditorClipboardPolicy
                    .IsCompatibleCubemapProperty(
                        material,
                        "Environment",
                        cubemapShader),
                "a copied shader change is considered before Cubemap paste");
            material.shader.name = cubemapShader;
            Equal(true,
                MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                    clipboard,
                    material,
                    null),
                "Cubemap-only clipboard can paste to a Cubemap target");

            clipboard.ClearAll();
            clipboard.MaterialTexturePropertyList.Add(
                new MaterialEditorAPI.CopyContainer.MaterialTextureProperty(
                    "Environment",
                    new byte[] { 1 },
                    null,
                    null));
            Equal(false,
                MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                    clipboard,
                    material,
                    null),
                "Texture payload is rejected by a Cubemap target");
            material.shader.name = textureShader;
            Equal(true,
                MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                    clipboard,
                    material,
                    null),
                "Texture payload can paste to a Texture target");
        }
        finally
        {
            catalogs.Remove(textureShader);
            catalogs.Remove(cubemapShader);
        }

        clipboard.ClearAll();
        clipboard.ProjectorPropertyList.Add(
            new MaterialEditorAPI.CopyContainer.ProjectorProperty(
                MaterialEditorAPI.MaterialAPI.ProjectorProperties.FieldOfView,
                40f));
        Equal(false,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                null),
            "projector-only clipboard cannot paste to a normal material row");
        Equal(true,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                new UnityEngine.Projector()),
            "projector-only clipboard can paste to projector context");
    }

    private static void TextureAndCubemapPasteStayTypeSafe()
    {
        var root = FindRepositoryRoot();
        var sources = new[]
        {
            Path.Combine(
                root,
                "src",
                "MaterialEditor.Core",
                "Core.MaterialEditor.CharaController.Edits.Core.cs"),
            Path.Combine(
                root,
                "src",
                "MaterialEditor.Core.Studio",
                "Core.MaterialEditor.SceneController.Edits.Core.cs")
        };
        foreach (var sourcePath in sources)
        {
            var source = File.ReadAllText(sourcePath)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
            Contains(source,
                "MaterialEditorClipboardPolicy.IsCompatibleTextureProperty(");
            Contains(source,
                "MaterialEditorClipboardPolicy.IsCompatibleCubemapProperty(");
            Contains(source, "targetShaderName");
        }
    }

    private static void UnknownShaderDoesNotInferCubemapFromGlobalFallback()
    {
        const string propertyName = "LegacyEnvironment";
        var catalogs = MaterialEditorAPI.MaterialEditorPluginBase
            .XMLShaderProperties;
        Dictionary<
            string,
            MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData>
            fallback;
        var createdFallback = !catalogs.TryGetValue("default", out fallback);
        if (createdFallback)
        {
            fallback = new Dictionary<
                string,
                MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData>();
            catalogs["default"] = fallback;
        }

        MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData previous;
        var hadPrevious = fallback.TryGetValue(propertyName, out previous);
        fallback[propertyName] =
            new MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData(
                propertyName,
                MaterialEditorAPI.MaterialAPI.ShaderPropertyType.Cubemap);
        try
        {
            var material = new UnityEngine.Material();
            material.shader.name = "Tests/UnknownLegacyShader";
            material.AddProperty("_" + propertyName);
            Equal(false,
                MaterialEditorAPI.MaterialEditorClipboardPolicy
                    .IsCompatibleCubemapProperty(material, propertyName),
                "global fallback never infers Cubemap for an unknown shader");
            Equal(true,
                MaterialEditorAPI.MaterialEditorClipboardPolicy
                    .IsCompatibleTextureProperty(material, propertyName),
                "unknown shader preserves legacy Texture compatibility");
        }
        finally
        {
            if (hadPrevious)
                fallback[propertyName] = previous;
            else
                fallback.Remove(propertyName);
            if (createdFallback)
                catalogs.Remove("default");
        }
    }

    private static void ClipboardCompatibilityToleratesNullListsAndItems()
    {
        var material = new UnityEngine.Material();
        var clipboard = new MaterialEditorAPI.CopyContainer
        {
            MaterialFloatPropertyList = null,
            MaterialColorPropertyList = null,
            MaterialVectorPropertyList = null,
            MaterialTexturePropertyList = null,
            MaterialCubemapPropertyList = null,
            MaterialShaderList = null,
            MaterialKeywordPropertyList =
                new List<MaterialEditorAPI.CopyContainer.MaterialKeywordProperty>
                {
                    null
                },
            ProjectorPropertyList =
                new List<MaterialEditorAPI.CopyContainer.ProjectorProperty>
                {
                    null
                }
        };

        Equal(true, clipboard.IsEmpty, "null lists and null items are empty");
        Equal(false,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                new UnityEngine.Projector()),
            "null entries never enable Paste");

        clipboard.MaterialKeywordPropertyList.Add(
            new MaterialEditorAPI.CopyContainer.MaterialKeywordProperty(
                "DETAIL_ON",
                true));
        Equal(false, clipboard.IsEmpty, "one valid entry is non-empty");
        Equal(true,
            MaterialEditorAPI.MaterialEditorClipboardPolicy.CanPaste(
                clipboard,
                material,
                null),
            "one valid keyword enables Paste despite unrelated null lists");
    }

    private static void ProjectorIdentityFlowsFromTheRowToBothBackends()
    {
        var presenter = ReadSource("UI.MaterialSectionPresenter.cs");
        Contains(presenter,
            "context.Edits.CopyMaterialEdits(\n                        context.Material,\n                        context.Projector)");
        Contains(presenter,
            "context.Edits.PasteMaterialEdits(\n                            context.Material,\n                            context.Projector)");

        var service = ReadRepositorySource(
            "src", "MaterialEditor.Base", "MaterialEditService.cs");
        Contains(service, "Projector projector,");
        Contains(service, "repository.GetProjectorPropertyValue(");
        Contains(service, "repository.SetProjectorProperty(");
        Contains(service, "clipboard.ProjectorPropertyList =");

        var character = ReadRepositorySource(
            "src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.Edits.Core.cs");
        Contains(character,
            "x.ProjectorName == projector.NameFormatted()");
        var studio = ReadRepositorySource(
            "src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.Edits.Core.cs");
        Contains(studio,
            "projectorProperty.ProjectorName == projector.NameFormatted()");
    }

    private static string ReadSource(string fileName)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
                root,
                "src",
                "MaterialEditor.Base",
                "UI",
                fileName))
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
    }

    private static string ReadRepositorySource(params string[] parts)
    {
        var path = new string[parts.Length + 1];
        path[0] = FindRepositoryRoot();
        Array.Copy(parts, 0, path, 1, parts.Length);
        return File.ReadAllText(Path.Combine(path))
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
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
        throw new DirectoryNotFoundException("Repository root not found.");
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

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0)
            throw new InvalidOperationException("Missing slice start: " + start);
        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            throw new InvalidOperationException("Missing slice end: " + end);
        return source.Substring(startIndex, endIndex - startIndex);
    }

    private static void Contains(string source, string value)
    {
        if (source.IndexOf(value, StringComparison.Ordinal) < 0)
            throw new InvalidOperationException("Missing source contract: " + value);
    }

    private static void DoesNotContain(string source, string value)
    {
        if (source.IndexOf(value, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("Forbidden source contract: " + value);
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
