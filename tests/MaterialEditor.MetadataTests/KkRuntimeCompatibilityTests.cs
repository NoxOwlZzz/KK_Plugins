using BepInEx.Logging;
using ExtensibleSaveFormat;
using KK_Plugins;
using KK_Plugins.MaterialEditor;
using MaterialEditorAPI;
using MessagePack;
using System.Xml;

internal static class KkRuntimeCompatibilityTests
{
    private const string TextureKey = "TextureDictionary";

    internal static void Run()
    {
        ExactInstalledKkDependencyVersionsArePinned();
        UnavailableKkApiTypesAreNotReferenced();
        TextureDictionaryKeysRemainStable();
        BundledTextureSaveAndLoadRoundtrip();
        DeduplicatedTextureReadPathsRemainCompatible();
        LocalTextureReadPathRemainsCompatible();
        MalformedTextureDataDegradesToEmpty();
        FutureTextureFormatIsSkippedWithoutMutation();
        ImageIdentificationDoesNotDependOnKkApi();
        Console.WriteLine("KK 1.42.2/20.0 runtime compatibility regression tests passed.");
    }

    private static void ExactInstalledKkDependencyVersionsArePinned()
    {
        var projectPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MaterialEditor.KK",
            "KK.MaterialEditor.csproj");
        var project = new XmlDocument();
        project.Load(projectPath);

        Equal(
            "1.42.2",
            GetPackageVersion(project, "Include", "IllusionModdingAPI.KKAPI"),
            "KKAPI deployment version");
        Equal(
            "20.0",
            GetPackageVersion(project, "Include", "Sideloader.Koikatu"),
            "Sideloader deployment version");
        Equal(
            "20.0",
            GetPackageVersion(project, "Update", "ExtensibleSaveFormat.Koikatu"),
            "ExtendedSave deployment override");
    }

    private static string GetPackageVersion(
        XmlDocument project,
        string identityAttribute,
        string packageName)
    {
        foreach (XmlElement reference in project.SelectNodes("/Project/ItemGroup/PackageReference"))
        {
            if (reference.GetAttribute(identityAttribute) == packageName)
                return reference.GetAttribute("Version");
        }

        return null;
    }

    private static void UnavailableKkApiTypesAreNotReferenced()
    {
        var forbiddenTypes = new[]
        {
            "TextureSaveHandlerBase",
            "ImageTypeIdentifier",
            "CharaLocalTextures",
            "CharaTextureSaveType",
            "SceneLocalTextures",
            "SceneTextureSaveType"
        };
        var sourceRoot = Path.Combine(FindRepositoryRoot(), "src");
        var materialEditorRoots = Directory
            .EnumerateDirectories(sourceRoot, "MaterialEditor.*", SearchOption.TopDirectoryOnly)
            .ToArray();

        foreach (var file in materialEditorRoots.SelectMany(root =>
                     Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)))
        {
            if (HasPathSegment(file, "bin") || HasPathSegment(file, "obj"))
                continue;

            var source = File.ReadAllText(file);
            foreach (var forbiddenType in forbiddenTypes)
            {
                if (source.Contains(forbiddenType, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Unavailable KKAPI type '{forbiddenType}' is referenced by " +
                        Path.GetRelativePath(FindRepositoryRoot(), file) + ".");
            }
        }
    }

    private static bool HasPathSegment(string path, string segment) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));

    private static void TextureDictionaryKeysRemainStable()
    {
        var repositoryRoot = FindRepositoryRoot();
        foreach (var relativePath in new[]
                 {
                     Path.Combine("src", "MaterialEditor.Core", "Core.MaterialEditor.CharaController.cs"),
                     Path.Combine("src", "MaterialEditor.Core.Studio", "Core.MaterialEditor.SceneController.cs")
                 })
        {
            var source = File.ReadAllText(Path.Combine(repositoryRoot, relativePath));
            Equal(
                true,
                source.Contains(
                    "const string TexDicSaveKey = nameof(TextureDictionary);",
                    StringComparison.Ordinal),
                relativePath + " texture key");
        }
    }

    private static void BundledTextureSaveAndLoadRoundtrip()
    {
        EnsureLogger();
        var handler = new TextureSaveHandler(Path.GetTempPath());
        var expectedBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
        var textures = new Dictionary<int, TextureContainer>
        {
            [17] = new TextureContainer(expectedBytes)
        };
        var pluginData = new PluginData { version = 99 };

        handler.Save(pluginData, TextureKey, textures, isCharaController: true);

        Equal(1, pluginData.version, "bundled save version");
        Equal(true, pluginData.data.ContainsKey(TextureKey), "bundled save key");
        Equal(false, pluginData.data.ContainsKey("LOCAL_" + TextureKey), "no local save key");
        Equal(false, pluginData.data.ContainsKey("DEDUPED_" + TextureKey), "no deduped save key");
        var serialized = MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>(
            (byte[])pluginData.data[TextureKey]);
        BytesEqual(expectedBytes, serialized[17], "bundled serialized bytes");

        // Invalid alternate payloads prove that bundled data has read priority.
        pluginData.data["DEDUPED_" + TextureKey] = new byte[] { 0xFF };
        pluginData.data["LOCAL_" + TextureKey] = new byte[] { 0xFF };
        var loaded = handler.Load<Dictionary<int, TextureContainer>>(
            pluginData,
            TextureKey,
            isCharaController: true);
        Equal(1, loaded.Count, "bundled load count");
        BytesEqual(expectedBytes, loaded[17].Data, "bundled roundtrip bytes");
    }

    private static void DeduplicatedTextureReadPathsRemainCompatible()
    {
        EnsureLogger();
        const string hash = "0123456789ABCDEF";
        var expectedBytes = new byte[] { 4, 5, 6, 7 };
        var references = MessagePackSerializer.Serialize(
            new Dictionary<int, string>
            {
                [23] = hash,
                [24] = "AAAAAAAAAAAAAAAA"
            });
        var payload = MessagePackSerializer.Serialize(
            new Dictionary<string, byte[]> { [hash] = expectedBytes });

        var embeddedData = new PluginData { version = 2 };
        embeddedData.data["DEDUPED_" + TextureKey] = references;
        embeddedData.data["DEDUPED_" + TextureKey + "_DATA"] = payload;
        var embeddedHandler = new TextureSaveHandler(Path.GetTempPath());
        var embedded = embeddedHandler.Load<Dictionary<int, TextureContainer>>(
            embeddedData,
            TextureKey,
            isCharaController: false);
        Equal(1, embedded.Count, "embedded deduped count");
        BytesEqual(expectedBytes, embedded[23].Data, "embedded deduped bytes");

        var sceneData = new PluginData { version = 2 };
        sceneData.data["DEDUPED_" + TextureKey + "_DATA"] = payload;
        MEStudio.SceneController.ExtendedData = sceneData;
        try
        {
            var characterData = new PluginData { version = 2 };
            characterData.data["DEDUPED_" + TextureKey] = references;
            var sceneFallbackHandler = new TextureSaveHandler(Path.GetTempPath());
            var sceneFallback = sceneFallbackHandler.Load<Dictionary<int, TextureContainer>>(
                characterData,
                TextureKey,
                isCharaController: true);
            Equal(1, sceneFallback.Count, "scene-payload deduped count");
            BytesEqual(expectedBytes, sceneFallback[23].Data, "scene-payload deduped bytes");
        }
        finally
        {
            MEStudio.SceneController.ExtendedData = null;
        }
    }

    private static void LocalTextureReadPathRemainsCompatible()
    {
        EnsureLogger();
        const string hash = "FEDCBA9876543210";
        var expectedBytes = new byte[] { 8, 9, 10, 11 };
        var directory = Path.Combine(
            Path.GetTempPath(),
            "MaterialEditor.MetadataTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "ME_LocalTex_" + hash + ".png"), expectedBytes);
            var pluginData = new PluginData { version = 2 };
            pluginData.data["LOCAL_" + TextureKey] = MessagePackSerializer.Serialize(
                new Dictionary<int, string>
                {
                    [31] = hash,
                    [32] = "../not-a-hash"
                });
            var handler = new TextureSaveHandler(directory);

            var loaded = handler.Load<Dictionary<int, TextureContainer>>(
                pluginData,
                TextureKey,
                isCharaController: true);

            Equal(1, loaded.Count, "local load count");
            BytesEqual(expectedBytes, loaded[31].Data, "local load bytes");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void MalformedTextureDataDegradesToEmpty()
    {
        EnsureLogger();
        var pluginData = new PluginData();
        pluginData.data[TextureKey] = new byte[] { 0xFF, 0x00 };
        var handler = new TextureSaveHandler(Path.GetTempPath());

        var loaded = handler.Load<Dictionary<int, TextureContainer>>(
            pluginData,
            TextureKey,
            isCharaController: true);

        Equal(0, loaded.Count, "malformed data fallback");
    }

    private static void FutureTextureFormatIsSkippedWithoutMutation()
    {
        EnsureLogger();
        var payload = MessagePackSerializer.Serialize(
            new Dictionary<int, byte[]> { [41] = new byte[] { 12, 13, 14 } });
        var pluginData = new PluginData { version = 3 };
        pluginData.data[TextureKey] = payload;
        var handler = new TextureSaveHandler(Path.GetTempPath());

        var loaded = handler.Load<Dictionary<int, TextureContainer>>(
            pluginData,
            TextureKey,
            isCharaController: true);

        Equal(0, loaded.Count, "future-version result");
        Equal(3, pluginData.version, "future-version marker remains untouched");
        Equal(true, ReferenceEquals(payload, pluginData.data[TextureKey]), "future payload remains untouched");
    }

    private static void ImageIdentificationDoesNotDependOnKkApi()
    {
        Equal(
            "png",
            TextureSaveHandler.IdentifyImageExtension(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "PNG extension");
        Equal(
            "webp",
            TextureSaveHandler.IdentifyImageExtension(
                new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }),
            "WebP extension");
        Equal(
            "XXX",
            TextureSaveHandler.IdentifyImageExtension(new byte[] { 1, 2, 3 }, "XXX"),
            "unknown-image fallback");
    }

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

    private static void BytesEqual(byte[] expected, byte[] actual, string name)
    {
        if (actual == null || !expected.SequenceEqual(actual))
            throw new InvalidOperationException(name + ": byte arrays differ.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
    }
}
