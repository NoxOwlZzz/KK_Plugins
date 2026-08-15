using BepInEx.Logging;
using ExtensibleSaveFormat;
using KK_Plugins;
using KK_Plugins.MaterialEditor;
using MaterialEditorAPI;
using MessagePack;
using System.Reflection;
using UnityEngine;

internal static class TextureOwnershipTests
{
    internal static void Run()
    {
        Equal(0, HolderCount(), "initial holder count");
        TemporaryOwnerReleaseKeepsAdoptedTextureAlive();
        DictionaryCleanupBalancesSharedTokens();
        MalformedBundledLoadReleasesPartialResult();
        BorrowedDuplicateSourceRemainsOwnedByItsController();
        ProductionOwnershipGuardsRemainPresent();
        Equal(0, HolderCount(), "final holder count");
        Console.WriteLine("Texture ownership and lifetime regression tests passed.");
    }

    private static void TemporaryOwnerReleaseKeepsAdoptedTextureAlive()
    {
        UnityEngine.Object.ResetDestroyedObjects();
        var temporary = new TextureContainer(new byte[] { 1, 2, 3, 4 });
        var adopted = new TextureContainer(temporary.Data);
        var temporaryDisposed = false;
        var adoptedDisposed = false;
        try
        {
            var temporaryToken = GetToken(temporary);
            var adoptedToken = GetToken(adopted);
            Equal(true, ReferenceEquals(temporaryToken, adoptedToken), "shared content token");
            Equal(2, adoptedToken.refCount, "shared token refcount");

            var texture = adopted.Texture;
            Equal(true, texture is RenderTexture, "manager-owned render texture");
            temporary.Dispose();
            temporaryDisposed = true;
            Equal(1, adoptedToken.refCount, "refcount after temporary release");
            Equal(true, ReferenceEquals(texture, adopted.Texture), "adopted texture remains usable");
            Equal(0, DestroyedRenderTextureCount(), "temporary release does not destroy shared texture");

            adopted.Dispose();
            adoptedDisposed = true;
            Equal(0, HolderCount(), "holder removed after final release");
            Equal(1, DestroyedRenderTextureCount(), "final release destroys render texture once");
        }
        finally
        {
            if (!temporaryDisposed)
                temporary.Dispose();
            if (!adoptedDisposed)
                adopted.Dispose();
        }
    }

    private static void DictionaryCleanupBalancesSharedTokens()
    {
        var first = new TextureContainer(new byte[] { 8, 9, 10 });
        var second = new TextureContainer(new byte[] { 8, 9, 10 });
        var textures = new Dictionary<int, TextureContainer>
        {
            [1] = first,
            [2] = second,
            [3] = first
        };

        Equal(1, HolderCount(), "duplicate-content holder count");
        var sharedToken = GetToken(first);
        Equal(2, sharedToken.refCount, "duplicate-content refcount");
        TextureSaveHandler.DisposeTextureContainers(textures);
        Equal(0, textures.Count, "cleanup clears dictionary");
        Equal(0, sharedToken.refCount, "each wrapper disposed exactly once");
        Equal(0, HolderCount(), "cleanup releases every wrapper");
    }

    private static void MalformedBundledLoadReleasesPartialResult()
    {
        EnsureLogger();
        var payload = new Dictionary<int, byte[]>
        {
            [1] = new byte[] { 11, 12, 13 },
            [2] = null
        };
        var data = new PluginData();
        data.data["TextureDictionary"] = MessagePackSerializer.Serialize(payload);
        var handler = new TextureSaveHandler(Path.GetTempPath());

        var loaded = handler.Load<Dictionary<int, TextureContainer>>(
            data,
            "TextureDictionary",
            isCharaController: true);

        Equal(0, loaded.Count, "malformed bundled result");
        Equal(0, HolderCount(), "partial bundled result released");
    }

    private static void BorrowedDuplicateSourceRemainsOwnedByItsController()
    {
        var source = new TextureContainer(new byte[] { 21, 22, 23 });
        var destination = new TextureContainer(source.Data);
        var destinationDisposed = false;
        var sourceDisposed = false;
        try
        {
            destination.Dispose();
            destinationDisposed = true;
            Equal(1, HolderCount(), "borrowed source holder remains");
            Equal(1, GetToken(source).refCount, "borrowed source refcount remains");
            Equal(true, source.Data.SequenceEqual(new byte[] { 21, 22, 23 }), "borrowed source bytes remain usable");

            source.Dispose();
            sourceDisposed = true;
            Equal(0, HolderCount(), "borrowed source owner releases itself");
        }
        finally
        {
            if (!destinationDisposed)
                destination.Dispose();
            if (!sourceDisposed)
                source.Dispose();
        }
    }

    private static void ProductionOwnershipGuardsRemainPresent()
    {
        var repositoryRoot = FindRepositoryRoot();
        var characterPersistence = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.Persistence.cs"));
        var duplicateStart = characterPersistence.IndexOf(
            "if (DuplicatingFrom.HasValue)",
            StringComparison.Ordinal);
        var duplicateEnd = characterPersistence.IndexOf(
            "DuplicatingFrom = null;",
            duplicateStart,
            StringComparison.Ordinal);
        if (duplicateStart < 0 || duplicateEnd < duplicateStart)
            throw new InvalidOperationException("Could not locate borrowed DuplicatingFrom block.");
        var duplicateBlock = characterPersistence.Substring(
            duplicateStart,
            duplicateEnd - duplicateStart);
        Equal(false, duplicateBlock.Contains("Dispose", StringComparison.Ordinal), "borrowed duplicate is not disposed");

        var temporaryStart = characterPersistence.IndexOf(
            "var importDictionaryTemp = TextureSaveHandler.Instance.Load",
            duplicateEnd,
            StringComparison.Ordinal);
        var temporaryEnd = characterPersistence.IndexOf(
            "//Debug for dumping all textures",
            temporaryStart,
            StringComparison.Ordinal);
        var temporaryBlock = characterPersistence.Substring(
            temporaryStart,
            temporaryEnd - temporaryStart);
        Equal(true, temporaryBlock.Contains("finally", StringComparison.Ordinal), "character temporary finally");
        Equal(true, temporaryBlock.Contains("DisposeTextureContainers(importDictionaryTemp)", StringComparison.Ordinal), "character temporary cleanup");

        var sceneSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "MaterialEditor.Core.Studio",
            "Core.MaterialEditor.SceneController.cs"));
        Equal(true, sceneSource.Contains("DisposeTextureContainers(TextureDictionary)", StringComparison.Ordinal), "scene clear/load cleanup");
        Equal(true, sceneSource.Contains("DisposeTextureContainers(importDictionaryTemp)", StringComparison.Ordinal), "scene import cleanup");

        var characterSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "MaterialEditor.Core",
            "Core.MaterialEditor.CharaController.cs"));
        Equal(true, characterSource.Contains("protected override void OnDestroy()", StringComparison.Ordinal), "character OnDestroy override");
        Equal(true, characterSource.Contains("base.OnDestroy();", StringComparison.Ordinal), "character OnDestroy base call");
    }

    private static TextureContainerManager.Token GetToken(TextureContainer container)
    {
        var field = typeof(TextureContainer).GetField(
            "_token",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (TextureContainerManager.Token)field.GetValue(container);
    }

    private static int HolderCount()
    {
        var field = typeof(TextureContainerManager).GetField(
            "_textureHolder",
            BindingFlags.Static | BindingFlags.NonPublic);
        var holder = field.GetValue(null);
        return (int)holder.GetType().GetProperty("Count").GetValue(holder);
    }

    private static int DestroyedRenderTextureCount() =>
        UnityEngine.Object.DestroyedObjects.Count(value => value is RenderTexture);

    private static void EnsureLogger()
    {
        if (MaterialEditorPluginBase.Logger == null)
            MaterialEditorPluginBase.Logger = new ManualLogSource();
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

        throw new DirectoryNotFoundException("Could not locate the KK_Plugins repository root.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
    }
}
