using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class CubemapRowTypeBinder : IRowTypeBinder
    {
        private readonly TextureRowControls _controls;

        internal CubemapRowTypeBinder(RowControlSet controls)
        {
            // Cubemap has its own row model and behavior, but intentionally
            // reuses the stateless Import/Export/Reset visual controls.
            _controls = controls.Texture;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            BindCubemap((CubemapPropertyRowModel)item, listeners);
        }

        private void BindCubemap(
            CubemapPropertyRowModel item,
            ListenerScope listeners)
        {
            _controls.SetVisible(true);
            TooltipBinding.Bind(_controls.Label.gameObject, item.TooltipText);

            System.Action refreshState = () =>
                ChangedStateBinding.Apply(
                    _controls.Label,
                    item.LabelText,
                    item.Changed,
                    _controls.ResetButton,
                    _controls.Panel);
            System.Action refreshExport = () =>
            {
                var text = _controls.ExportButton.GetComponentInChildren<Text>();
                _controls.ExportButton.enabled = item.Exists;
                text.text = item.Exists ? "Export Cubemap" : "No Cubemap";
                text.color = item.Exists ? Color.black : Color.gray;
            };

            _controls.ImportButton.GetComponentInChildren<Text>().text = "Import Cubemap";
            _controls.SelectInterpolableButton.gameObject.SetActive(false);
            TooltipBinding.Bind(
                _controls.ImportButton.gameObject,
                "Import a PNG as a Cubemap. Non-2:1 images are stretched automatically.");
            TooltipBinding.Bind(
                _controls.ExportButton.gameObject,
                "Export the assigned Cubemap as an equirectangular 2:1 PNG.");

            refreshState();
            refreshExport();
            System.Action refreshBoundState = () =>
            {
                refreshExport();
                refreshState();
            };
            item.RefreshState = refreshBoundState;
            listeners.OnDispose(() =>
            {
                if (item.RefreshState == refreshBoundState)
                    item.RefreshState = null;
            });
            listeners.Listen(_controls.ExportButton, () => item.Export());
            listeners.Listen(_controls.ImportButton, () => item.Import());
            listeners.Listen(_controls.ResetButton, () =>
            {
                item.Changed = false;
                item.Reset();
                refreshExport();
                refreshState();
            });
            LabelClickBinding.Bind(
                listeners,
                _controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.CubemapProperty,
                () => item.PropertyName);
        }
    }
}
