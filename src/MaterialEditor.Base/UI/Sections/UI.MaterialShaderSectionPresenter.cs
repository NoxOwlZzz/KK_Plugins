using System;

namespace MaterialEditorAPI
{
    internal sealed class MaterialShaderSectionPresenter
    {
        private readonly MaterialEditorPresentationActions _actions;

        internal MaterialShaderSectionPresenter(
            MaterialEditorPresentationActions actions)
        {
            _actions = actions;
        }

        internal ShaderRowModel AddRows(MaterialSectionContext context)
        {
            var originalShaderName = context.Edits.GetOriginalShader(
                context.Material);
            if (originalShaderName.IsNullOrEmpty())
                originalShaderName = context.ShaderName;
            var shaderItem = new ShaderRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                ShaderName = context.ShaderName,
                OriginalShaderName = originalShaderName,
                TooltipText = ShaderUiMetadataRegistry.GetShaderTooltip(
                    context.ShaderName),

                ShaderNameOnChange = value =>
                {
                    context.Edits.SetShader(
                        context.Material,
                        value);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                ShaderNameOnReset = () =>
                {
                    context.Edits.ResetShader(context.Material);
                    _actions.RefreshDeferred(
                        context.GameObject,
                        context.Data,
                        context.Filter);
                },
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        context.GameObject,
                        RowModel.RowItemType.Shader,
                        context.MaterialName,
                        string.Empty,
                        string.Empty)
            };
            context.Rows.Add(shaderItem);

            var originalRenderQueue =
                context.Edits.GetOriginalRenderQueue(context.Material)
                ?? context.Material.renderQueue;
            context.Rows.Add(new ShaderRenderQueueRowModel()
            {
                GameObject = context.GameObject,
                Data = context.Data,
                Material = context.Material,
                Projector = context.Projector,
                Value = context.Material.renderQueue,
                OriginalValue = originalRenderQueue,
                ValueOnChange = value =>
                    context.Edits.SetRenderQueue(
                        context.Material,
                        value),
                ValueOnReset = () =>
                    context.Edits.ResetRenderQueue(context.Material)
            });
            return shaderItem;
        }
    }
}
