using BepInEx;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns Material Editor asset selection, import, export, file watching, and
    /// Cubemap import coordination. Unity work remains hosted by the owning UI.
    /// </summary>
    internal sealed class MaterialEditorAssetWorkflow : IDisposable
    {
        private readonly MaterialEditorUI _host;
        private readonly MaterialEditService _editService;
        // The legacy UI allowed one watched texture globally. Keep the same
        // cross-host ownership while moving that state out of MaterialEditorUI.
        private static FileSystemWatcher _textureWatcher;
        private bool _disposed;

        internal MaterialEditorAssetWorkflow(
            MaterialEditorUI host,
            MaterialEditService editService)
        {
            if (host == null)
                throw new ArgumentNullException("host");
            if (editService == null)
                throw new ArgumentNullException("editService");
            _host = host;
            _editService = editService;
        }

        internal void ImportTexture(
            TexturePropertyRowModel textureItem,
            GameObject gameObject,
            object data,
            Material material,
            string propertyName)
        {
#if !API
            string fileFilter = KK_Plugins.ImageHelper.FileFilter;
#else
            string fileFilter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
#endif
            var propertyHandle = MaterialPropertyIdCache.Get(propertyName);
            KKAPI.Utilities.OpenFileDialog.Show(
                OnFileAccept,
                "Open image",
                ExportPath,
                fileFilter,
                ".png");

            void OnFileAccept(string[] files)
            {
                ThreadingHelper.Instance.StartSyncInvoke(
                    () =>
                    {
                        if (IsHostAlive())
                            _host.StartCoroutine(
                                ApplyFileSelectionOnMainThread(files));
                    });
            }

            IEnumerator ApplyFileSelectionOnMainThread(string[] files)
            {
                // StartSyncInvoke is drained by BepInEx.Update. Yield once so
                // disk reads run outside that drain.
                yield return null;

                if (material == null || gameObject == null)
                    yield break;

                if (files == null || files.Length == 0 || files[0].IsNullOrEmpty())
                {
                    textureItem.Changed =
                        !_editService.GetMaterialTextureValueOriginal(
                            data,
                            material,
                            propertyName,
                            gameObject);
                    var currentTexture = MaterialPropertyAccess.GetTexture(
                        material,
                        propertyHandle);
                    textureItem.Exists = currentTexture != null;
                    textureItem.RefreshState?.Invoke();
                    yield break;
                }

                string filePath = files[0];
                _editService.SetMaterialTexture(
                    data,
                    material,
                    propertyName,
                    filePath,
                    gameObject,
                    succeeded =>
                    {
                        if (!IsHostAlive()
                            || material == null
                            || gameObject == null)
                            return;

                        // Character and Studio repositories apply Texture2D imports
                        // on their next Update. Refresh only after that work reports
                        // completion, and derive both flags from the real edit/material
                        // state instead of assuming that decoding succeeded.
                        textureItem.Changed =
                            !_editService.GetMaterialTextureValueOriginal(
                                data,
                                material,
                                propertyName,
                                gameObject);
                        textureItem.Exists = MaterialPropertyAccess.GetTexture(
                            material,
                            propertyHandle) != null;
                        textureItem.RefreshState?.Invoke();

                        if (!succeeded)
                        {
                            MaterialEditorPluginBase.Logger.LogWarning(
                                $"Could not import texture '{filePath}' for {propertyName}.");
                        }
                    });

                DisposeTextureWatcher();
                if (!WatchTexChanges.Value)
                    yield break;

                var directory = Path.GetDirectoryName(filePath);
                if (directory == null)
                    yield break;

                _textureWatcher = new FileSystemWatcher(
                    directory,
                    Path.GetFileName(filePath));
                _textureWatcher.Changed += (sender, args) =>
                {
                    if (WatchTexChanges.Value && File.Exists(filePath))
                        ScheduleTextureWatcherImport(
                            data,
                            material,
                            propertyName,
                            filePath,
                            gameObject);
                };
                _textureWatcher.Deleted +=
                    (sender, args) => DisposeTextureWatcher();
                _textureWatcher.Error +=
                    (sender, args) => DisposeTextureWatcher();
                _textureWatcher.EnableRaisingEvents = true;
            }
        }

        internal void ImportCubemap(
            CubemapPropertyRowModel cubemapItem,
            GameObject gameObject,
            object data,
            Material material,
            string propertyName)
        {
            const string fileFilter = "Cubemap panoramas (*.png;*.hdr)|*.png;*.hdr|PNG images (*.png)|*.png|Radiance HDR images (*.hdr)|*.hdr|All files|*.*";
            var propertyHandle = MaterialPropertyIdCache.Get(propertyName);
            KKAPI.Utilities.OpenFileDialog.Show(
                OnFileAccept,
                "Open Cubemap source",
                ExportPath,
                fileFilter,
                ".png");

            void OnFileAccept(string[] files)
            {
                ThreadingHelper.Instance.StartSyncInvoke(
                    () =>
                    {
                        if (!IsHostAlive()
                            || material == null
                            || gameObject == null
                            || files == null
                            || files.Length == 0
                            || files[0].IsNullOrEmpty())
                            return;

                        var filePath = files[0];
                        try
                        {
                            var runner = _host.gameObject.AddComponent<
                                MaterialEditorCubemapImportRunner>();
                            runner.Begin(
                                filePath,
                                () => IsHostAlive()
                                      && material != null
                                      && gameObject != null
                                      && MaterialPropertyAccess.HasProperty(
                                          material,
                                          propertyHandle),
                                (encodedData, contentKey, warmLease) =>
                                {
                                    // Keep the preheated cache entry alive until
                                    // the repository has acquired its own lease.
                                    if (warmLease == null)
                                        return false;
                                    if (_editService.SupportsMaterialCubemapDataImport(
                                            data))
                                    {
                                        // A supported repository reports the
                                        // real persistence/application result.
                                        // Do not reinterpret failure as a reason
                                        // to retry through the legacy file API.
                                        return _editService.SetMaterialCubemap(
                                            data,
                                            material,
                                            propertyName,
                                            encodedData,
                                            contentKey,
                                            gameObject);
                                    }

                                    // External/legacy repositories only expose
                                    // the original file-path API. Current Chara
                                    // and Studio repositories use the byte seam,
                                    // so they do not repeat disk IO here.
                                    MaterialEditorPluginBase.Logger?.LogWarning(
                                        "The active Material Editor repository does not support "
                                        + "preloaded Cubemap data; using its legacy file import path.");
                                    _editService.SetMaterialCubemap(
                                        data,
                                        material,
                                        propertyName,
                                        filePath,
                                        gameObject);
                                    return true;
                                },
                                message => MaterialEditorPluginBase.Logger?.LogInfo(
                                    message),
                                message => MaterialEditorPluginBase.Logger?.LogWarning(
                                    message),
                                message => MaterialEditorPluginBase.Logger?.LogError(
                                    "Could not import Cubemap '"
                                    + propertyName
                                    + "': "
                                    + message),
                                succeeded =>
                                {
                                    if (!IsHostAlive()
                                        || material == null
                                        || gameObject == null)
                                        return;

                                    cubemapItem.Changed =
                                        !_editService.GetMaterialCubemapValueOriginal(
                                            data,
                                            material,
                                            propertyName,
                                            gameObject);
                                    cubemapItem.Exists =
                                        MaterialPropertyAccess.GetTexture(
                                            material,
                                            propertyHandle) is Cubemap;
                                    cubemapItem.RefreshState?.Invoke();
                                });
                        }
                        catch (Exception exception)
                        {
                            MaterialEditorPluginBase.Logger?.LogError(
                                "Could not start Cubemap import '"
                                + propertyName
                                + "': "
                                + exception.Message);
                        }
                    });
            }
        }

        internal void ExportTexture(Material material, string propertyName)
        {
            var texture = MaterialPropertyAccess.GetTexture(
                material,
                MaterialPropertyIdCache.Get(propertyName));
            if (texture == null)
                return;
            var materialName = SanitizeMaterialName(material);
            string filename = Path.Combine(
                ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{materialName}_{propertyName}.png");
            Instance.ConvertNormalMap(
                ref texture,
                propertyName,
                ConvertNormalmapsOnExport.Value);
            SaveTex(texture, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportCubemap(Material material, string propertyName)
        {
            var cubemap = MaterialPropertyAccess.GetTexture(
                material,
                MaterialPropertyIdCache.Get(propertyName)) as Cubemap;
            if (cubemap == null)
                return;

            var materialName = SanitizeMaterialName(material);
            string filename = Path.Combine(
                ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{materialName}_{propertyName}.png");
            byte[] pngData;
            string error;
            if (!MaterialEditorCubemapConversion.TryExport(
                    cubemap,
                    out pngData,
                    out error))
            {
                MaterialEditorPluginBase.Logger.LogError(error);
                MaterialEditorPluginBase.Logger.LogMessage(error);
                return;
            }

            File.WriteAllBytes(filename, pngData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal void ExportTextureOriginal(
            Material material,
            string propertyName,
            string extension,
            byte[] textureData)
        {
            var materialName = SanitizeMaterialName(material);
            string filename = Path.Combine(
                ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{materialName}_{propertyName}.{extension}");
            File.WriteAllBytes(filename, textureData);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }

        internal static void DisposeTextureWatcher()
        {
            var watcher = _textureWatcher;
            _textureWatcher = null;
            watcher?.Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            DisposeTextureWatcher();
        }

        private void ScheduleTextureWatcherImport(
            object data,
            Material material,
            string propertyName,
            string filePath,
            GameObject gameObject)
        {
            // FileSystemWatcher raises Changed on a ThreadPool thread, so
            // marshal the Texture2D import before it touches Unity objects.
            ThreadingHelper.Instance.StartSyncInvoke(() =>
            {
                if (IsHostAlive()
                    && material != null
                    && gameObject != null
                    && WatchTexChanges.Value
                    && File.Exists(filePath))
                    _editService.SetMaterialTexture(
                        data,
                        material,
                        propertyName,
                        filePath,
                        gameObject);
            });
        }

        private bool IsHostAlive()
        {
            return _host != null;
        }

        private static string SanitizeMaterialName(Material material)
        {
            var materialName = material.NameFormatted();
            return string.Concat(
                materialName.Split(Path.GetInvalidFileNameChars())).Trim();
        }
    }
}
