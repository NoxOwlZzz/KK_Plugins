using System;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Permanent listener target for one pooled category navigator entry.
    /// Rebinding replaces only the semantic target; the installed callbacks
    /// always snapshot the current target and never retain an older category.
    /// </summary>
    internal sealed class CategoryNavigationEntryBinding
    {
        private readonly Action<CategoryNavigationTarget> _navigate;
        private readonly Action<CategoryNavigationTarget> _toggle;

        internal CategoryNavigationEntryBinding(
            Action<CategoryNavigationTarget> navigate,
            Action<CategoryNavigationTarget> toggle)
        {
            _navigate = navigate;
            _toggle = toggle;
        }

        internal CategoryNavigationTarget Target { get; private set; }

        internal void Bind(CategoryNavigationTarget target)
        {
            Target = target;
        }

        internal void InvokeNavigate()
        {
            var target = Target;
            if (target != null)
                _navigate(target);
        }

        internal void InvokeToggle()
        {
            var target = Target;
            if (target != null)
                _toggle(target);
        }
    }
}
