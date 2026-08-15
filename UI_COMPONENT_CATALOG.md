# Material Editor UI component catalog

> Historical pre-implementation catalog. At `8c36e8df`, the internal
> `MaterialEditorPopupMenu`, `MaterialEditorRowActionMenu`, exact empty-state
> policy and responsive VirtualList growth now exist. A footer, generic warning
> surface and splitter remain absent. See `UI_BEFORE_AFTER_STRUCTURE.md` for
> the implemented composition and `KNOWN_UI_LIMITATIONS.md` for omissions.

## Catalog rules

This catalog maps proposed visual components to current code and real backend.
“Backend absent” means the control must not be rendered as functional. A new
internal view wrapper may reorganize an existing callback; it may not invent a
new editing operation.

## Composition and infrastructure

| Component | Current source | Lifetime / backend | Redesign disposition |
| --- | --- | --- | --- |
| Window | `MaterialEditorWindowView` | One per plugin UI; owns Canvas hierarchy | Retain owner; split implementation only where it reduces risk |
| Main panel | `MaterialEditorWindowView.MainPanel` | Config geometry and dragged runtime position | Restyle; preserve drag and public static references |
| Top bar | Header + ModePanel in `MaterialEditorWindowView` | Direct callbacks for Search/mode/close/panel/category actions | Compose as one/two visual rows without changing callback order |
| Theme/layout | `MaterialEditorStyles`, `MaterialEditorLayout` | Internal static partial token seam | Centralize in internal token types; no public theming API |
| Control factory | `MaterialEditorControlFactory` | Wraps `UIUtility` and style application | Keep as the sole factory path |
| Category navigator | `CategoryNavigatorView` | Viewport-following section/category projection | Restyle and add explicit collapsed rail; keep semantics |
| Renderer/Materials panels | `SelectListPanel` | Dynamic multi-select entries and independent filters | Reuse; add header state/count and independent view collapse |
| Rename panel | `SelectListPanel` + Rename controls | Repository-backed material-name writes | Preserve as a specialized state; do not lose context on close/reopen |
| Central list | `VirtualList` | Initial viewport-sized pool, fixed row height, anchor restoration | Preserve; no variable-height rows or splitter in first pass |
| Row view | `RowView` | One complete 18-family hierarchy per pooled slot | Restyle template once; never create a Canvas/menu/popup per row |
| Row binding | `RowBinder`, family binders, `ListenerScope` | Exact callback cleanup | Reset every recyclable visual state during bind/release |
| Numeric input | `NumericInputView` | Culture parsing, edit/display modes, Mixed | Preserve; apply visual shell without changing parsing |
| Dropdown option cache | `EnumDropdownOptionCache` | Per-row options; retained cap 128 after shrink | Preserve allocation and caption semantics |
| Shader dropdown filter | `DropdownFilter` | Popup filter/clear UI and reusable setup | Preserve; restyle with popup capture tests |
| Tooltip popup | `TooltipManager` | One rendered panel; target `Tooltip` behaviors register with it | Reuse one popup; target components are allowed, popup per row is not |
| Shader hint | `ShaderHintUnderline` | Shift-selected metadata hint | Restyle indicator without changing policy |
| Color picker adapter | Maker/Studio `IMaterialEditorColorPalette` implementations | Existing runtime palette and fixed reopen lifecycle | Preserve exactly; no custom color-picker rewrite |
| Context-menu service | None | **Backend absent as UI infrastructure** | May be added internally as one reusable surface before moving actions |
| Footer/status view | None | **Backend absent**; only some presentation values exist | Add only mode/row count/persist/Advanced values listed in design |
| Empty-state view | None | **Backend absent** | Do not show until a real read-only presentation state exists |
| Warning surface | None | Warnings currently log only | Do not show until a real warning model exists |
| Runtime splitter | None | Current pool has no resize operation | Deferred |

## Window-level control catalog

| Control | Backend / state owner | Writes material? | Planned presentation |
| --- | --- | --- | --- |
| Material Editor title | Literal view text | No | Stable title |
| Shader context | Per-section shader names in presentation | No | Show one name only if all agree; otherwise `Multiple shaders` |
| Basic | `ChangeUiMode`, presentation policy | No | Left segment |
| Advanced | `ChangeUiMode`, presentation policy | No | Right segment |
| Advanced change navigation | `FirstAdvancedChange`, collapse dictionaries, `ScrollToIndex` | No | Compact `Advanced · N` |
| Search | `HandleFilterChanged`, invalidation coordinator | No | Compact field; preserve query on mode change |
| Persist Search | `PersistFilter` ConfigEntry | No | Labelled compact checkbox |
| Hidden Advanced results | `HiddenAdvancedSearchResultCount` | No | Existing count + Show behavior |
| Left collapse | `CategoryNavigatorView._expanded` | No | Explicit chevron/rail; optional UI config later |
| All-category collapse | Presentation category targets | No | Secondary action; retain exact semantics |
| Full right collapse | `Session.ListsVisible` today | No | Default visible in redesigned view; explicit rail when collapsed |
| Renderer section collapse | None | No editing backend needed; new view-local state | Add only as UI state, never persistence data |
| Materials section collapse | None | No editing backend needed; new view-local state | Add only as UI state, never persistence data |
| Close | `Visible = false` | No | Clear close affordance |

## Row component catalog

| Family | Current controls | Confirmed callback/repository | Approved visual change |
| --- | --- | --- | --- |
| Renderer | Name, Timeline, Export UV, Export OBJ | Export actions and Timeline selection | Neutral context row; consolidate export only through reusable menu |
| Renderer Enabled | Toggle, Reset | Renderer property set/reset | Consistent toggle + compact reset |
| Shadow Casting | Dropdown, Reset | Renderer property set/reset | Legible dropdown + compact reset |
| Receive Shadows | Toggle, Reset | Renderer property set/reset | Consistent toggle + compact reset |
| Update Off-Screen | Toggle, Reset | Renderer property set/reset; target-dependent | Same; do not force into KK where excluded |
| Recalculate Normals | Toggle, Reset | Renderer property set/reset | Preserve as explicit real property |
| Material | Fold, name, Copy Edits, Paste Edits, Copy/Remove, Rename | Existing material edit service | Compact context actions; no Paste Material/Reset Material |
| Shader | Fold, all-categories fold, Timeline, dropdown, Reset | Shader set/reset, deferred refresh | Neutral header/row; retain dropdown filter |
| Render Queue | Integer field, Reset | Set/reset render queue | Aligned numeric input + compact reset |
| Property Category | Fold, label | Session collapse + rebuild | Neutral header, small accent; no count/master toggle |
| Texture | Timeline, Export/No Texture, Import, Reset | Existing texture callbacks | Compact existing actions; no thumbnail/name |
| Offset/Scale | Four numeric inputs, Reset | Offset/scale set/reset | Aligned paired controls, fixed row height |
| Color | RGBA inputs, Timeline, swatch/picker, Reset | Color set/reset + palette adapter | Keep full RGBA and HDR values; compact swatch/reset |
| Float/Range | Slider, numeric input, Timeline, Reset | Float set/reset | Keep actual min/max; do not invent free-float variant |
| Keyword | Toggle, Reset | Keyword set/reset | Same visual toggle style, distinct semantics |
| Enum | Dropdown, Timeline, Reset | Float enum set/reset | Preserve `Mixed`, `Unknown (x)`, duplicates policy |
| Vector | 2-4 numeric inputs, Timeline, Reset | Whole/per-component callbacks | Fixed-height aligned inputs; preserve per-component Mixed |
| Float Toggle | Toggle, Timeline, Reset | Float off/on set/reset | Same toggle family; explicit Mixed indicator without programmatic write |

## Capability ledger

### Confirmed backend; safe to expose

- Basic and Advanced membership from metadata/presentation policy.
- Search, wildcard/comma/property-prefix behavior, Persist Search, hidden
  Advanced result count, and Advanced change navigation.
- Renderer and Material checkbox multi-selection plus independent filters.
- Export UV Map and Export `.obj`.
- Copy Edits, Paste Edits, Copy Material, Remove copied Material, and Rename.
- Renderer Enabled, Shadow Casting, Receive Shadows, target-dependent Update
  Off-Screen, and Recalculate Normals.
- Shader selection/reset and Render Queue set/reset.
- Texture import/export/reset and Offset/Scale.
- Color with current palette, Float/Range, Keyword, Enum/Dropdown, Vector, and
  float-backed Toggle.
- Material/shader/category collapse, collapse-all, category navigation.
- Modified comparison/reset activation for current row families.
- Enum/Vector/Float-Toggle explicit Mixed semantics.
- `ShowIf`, old/new manifests, providers, MaterialAPI, Extension
  API, label-click events, Timeline affordances, and tooltips.

### Partial; label precisely

| Capability | Existing portion | Missing portion |
| --- | --- | --- |
| Active context | Selected renderer/material collections and material sections | No singular active renderer/material contract |
| Left collapse | In-memory `_expanded` toggle | No collapsed rail or config persistence |
| Full right collapse | One in-memory visibility toggle | Current default hidden; no independent section collapse |
| Item counts | Private selection entry dictionaries | No current header count projection |
| Footer | Mode, row count, Persist Search, Advanced counts exist separately | No footer component or generic totals |
| Mixed | Explicit in Enum/Vector/Float Toggle | No general Mixed field for every row family |
| Long names | Masks and best-fit | No deliberate ellipsis + full-name tooltip policy in lists |
| Warnings | Logger diagnostics | No user-facing warning model |

### Backend absent; never render as working

- Undo or undo history.
- Modified Only filter.
- Category Copy, Category Paste, category master toggle, or category modified
  count.
- Global Reset Material or Paste Material.
- Advanced previous/next change navigation beyond the existing first-change
  action.
- Favorites, presets, profiles, or saved UI layouts.
- Technical Info panel/action.
- Generic `available`/`modified` footer totals.
- Texture thumbnail, texture-name projection, or channel preview.
- Visible row warning/error badges and error details dialog.
- Reusable context menu (until implemented as internal infrastructure).
- Runtime UI scale editor, light theme, theme editor, animations, or skins.
- Runtime side-panel splitter and safe VirtualList pool resizing.
- Variable-height/two-line Vector rows.

## Action-menu mapping

If the internal reusable menu is implemented, its model must be built from the
currently bound row model and contain only non-null actions:

| Context | Menu actions |
| --- | --- |
| Renderer | Export UV Map; Export `.obj` |
| Material | Copy Edits; Paste Edits; Copy Material or Remove Material; Rename |
| Texture | Export Texture only when `Exists`; Import Texture; Reset only when changed |
| Generic property | Reset only when current family exposes it and is changed |

The service holds no `RowModel`, Unity target, or callback after close. Opening
or closing it does not call repository methods. A pooled row supplies the
current context on demand; no menu GameObject is attached to every row.

## Persistence map

Material values, shader/render queue, textures, renderer properties, names,
and material copies continue through existing repositories/controllers. View
state—mode, search behavior, window geometry, panel collapse, section collapse,
selection, focus, hover, tooltip, and menu context—must stay out of card,
coordinate, scene, material, manifest, and public-extension payloads.
