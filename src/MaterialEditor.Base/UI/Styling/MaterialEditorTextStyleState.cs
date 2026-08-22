using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorTextStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorTextRole _role;
        [SerializeField] private bool _assigned;

        internal MaterialEditorTextRole Role => _role;
        internal bool Assigned => _assigned;

        internal void SetRole(MaterialEditorTextRole role)
        {
            _role = role;
            _assigned = true;
        }

        private void OnEnable()
        {
            var text = GetComponent<Text>();
            if (!_assigned)
            {
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
                return;
            }

            MaterialEditorPanelTextStyles.ApplyText(text, _role);
            var owner = GetComponentInParent<MaterialEditorControlStyleState>();
            if (owner != null)
                MaterialEditorStyles.ReapplyControlState(owner);
        }
    }

    internal sealed class MaterialEditorPanelStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorPanelRole _role;

        internal MaterialEditorPanelRole Role => _role;

        internal void SetRole(MaterialEditorPanelRole role)
        {
            _role = role;
        }
    }

    internal enum MaterialEditorControlStyleRole
    {
        Button,
        PropertyCategory,
        PropertySubcategory,
        CategoryNavigation,
        SelectionListRow,
        Swatch,
        InputField,
        Toggle,
        Dropdown,
        Slider
    }

    internal enum MaterialEditorControlAvailabilityMode
    {
        Disabled,
        LegacyPassive,
        Hidden,
        TimelineSlot
    }

    internal sealed class MaterialEditorControlStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorControlStyleRole _role;
        [SerializeField] private bool _logicalState;
        [SerializeField] private bool _available = true;
        [SerializeField] private MaterialEditorControlAvailabilityMode _availabilityMode;
        [SerializeField] private bool _assigned;
        private bool _applying;

        internal MaterialEditorControlStyleRole Role => _role;
        internal bool LogicalState => _logicalState;
        internal bool Available => _available;
        internal MaterialEditorControlAvailabilityMode AvailabilityMode =>
            _availabilityMode;

        internal static MaterialEditorControlStyleState Assign(
            Selectable selectable,
            MaterialEditorControlStyleRole role)
        {
            if (selectable == null)
                return null;

            var state = selectable.GetComponent<MaterialEditorControlStyleState>();
            if (state == null)
                state = selectable.gameObject.AddComponent<MaterialEditorControlStyleState>();
            if (!state._assigned)
            {
                state._available = true;
                state._availabilityMode =
                    MaterialEditorControlAvailabilityMode.Disabled;
            }
            state._role = role;
            state._assigned = true;
            return state;
        }

        internal void SetLogicalState(bool value)
        {
            if (_logicalState == value)
                return;
            _logicalState = value;
            if (_assigned && !_applying)
                MaterialEditorStyles.ReapplyControlState(this);
        }

        internal void SetAvailability(
            bool available,
            MaterialEditorControlAvailabilityMode mode)
        {
            _available = available;
            _availabilityMode = mode;
        }

        internal bool BeginApply()
        {
            if (_applying)
                return false;
            _applying = true;
            return true;
        }

        internal void EndApply()
        {
            _applying = false;
        }

        private void OnEnable()
        {
            // AddComponent invokes OnEnable before Assign can set the role.
            // Existing pooled controls, however, already own complete semantic
            // state and must restore it synchronously when reactivated.
            if (_assigned && !_applying)
                MaterialEditorStyles.ReapplyControlState(this);
        }
    }
    internal sealed class MaterialEditorScrollStyleState : MonoBehaviour
    {
        [SerializeField] private bool _popup;

        internal bool Popup => _popup;

        internal static MaterialEditorScrollStyleState Assign(
            ScrollRect scrollRect,
            bool popup)
        {
            if (scrollRect == null)
                return null;

            var state = scrollRect.GetComponent<MaterialEditorScrollStyleState>()
                        ?? scrollRect.gameObject.AddComponent<MaterialEditorScrollStyleState>();
            state._popup = popup;
            return state;
        }
    }

    internal sealed class MaterialEditorGraphicStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorThemeColorRole _role;

        internal MaterialEditorThemeColorRole Role => _role;

        internal static MaterialEditorGraphicStyleState Assign(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return null;
            var state = graphic.GetComponent<MaterialEditorGraphicStyleState>()
                        ?? graphic.gameObject.AddComponent<MaterialEditorGraphicStyleState>();
            state._role = role;
            return state;
        }
    }

    internal sealed class MaterialEditorOutlineStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorThemeColorRole _role;

        internal MaterialEditorThemeColorRole Role => _role;

        internal static MaterialEditorOutlineStyleState Assign(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return null;
            var state = graphic.GetComponent<MaterialEditorOutlineStyleState>()
                        ?? graphic.gameObject.AddComponent<MaterialEditorOutlineStyleState>();
            state._role = role;
            return state;
        }
    }
}
