# Minimal Three-Panel Material Editor design

> Historical design input: implementation is complete in source at
> `8c36e8df`. Final geometry/tokens and deliberate deltas are recorded in
> `UI_THEME_TOKENS.md` and `UI_BEFORE_AFTER_STRUCTURE.md`. In particular, the
> shipped source keeps 22-unit rows, a 40-unit top bar, no footer/warning bar
> and no splitter; illustrative counts/status strips below were not invented.
> Rendered acceptance remains **PENDING**.

## Design decision

The redesign keeps the existing workflow and semantic pipeline while making
the three zones read as one coherent window:

1. category navigation on the left;
2. one virtualized property surface in the center;
3. Renderer and Materials selection lists, visible together, on the right.

This document is a structural specification, not proof of rendered quality.
It was adapted to the current code without opening Material Editor. No new
editing feature, persistence schema, public API, undo system, or KKLT-specific
logic is authorized.

## Non-negotiable behavior

- `MaterialEditorPresenter` remains authoritative for Basic/Advanced, Search,
  metadata, providers, `ShowIf`, rows, and categories.
- `MaterialEditService` and current Maker/Studio repositories remain the only
  path for material writes and resets.
- The center retains one principal `ScrollRect`, `VirtualList`, pooled
  `RowView`, `RowBinder`, and exact `ListenerScope` cleanup.
- Renderer and Materials remain simultaneous lists, never exclusive tabs.
- Multi-selection semantics remain unchanged. There is no singular “active
  renderer” backend; the design must say “selected renderer(s)” or show a
  count/list-derived context.
- Basic/Advanced, panel collapse, Search focus, hover, tooltip, and menu open
  are view actions and must not write materials.
- Old and schema-2 manifests, MaterialAPI, Extension API, cards, coordinates,
  scenes, and imports retain their current semantic paths.

## Approved default layout

The following wireframe uses only values that can be produced by the current
presentation/session or by view-local state. Counts marked `n` are list counts,
not invented editing totals.

```text
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│ Material Editor   Shader: KKLT/Opaque   [ BASIC | ADVANCED · 7 ]   [ Search…      ]  P  × │
├──────────────────┬──────────────────────────────────────────────────┬───────────────────────┤
│ Categories     « │ Selected: 2 renderers · 1 material              │ Renderers · 12       «│
│ A.MECopy1        │                                                  │ [Filter…              ]│
│ KKLT/Opaque      ├──────────────────────────────────────────────────┤ ☑ b_Top                │
│                  │ v Material: A.MECopy1       [Copy] [Paste] [⋯]   │ ☑ b_Bra                │
│ ▌ Base Settings  │ v Shader: KKLT/Opaque             [shader list] │ ☐ a_Top                │
│   Lighting       │   Render Queue                         [2450] ↺ │                       │
│   Main Color     │                                                  ├───────────────────────┤
│   Alpha          │ v Base Settings                                  │ Materials · 5         «│
│   Shadow         │   Enabled                                [x] ↺ │ [Filter…              ]│
│   RimShade       │   Shadow Casting Mode                 [On v] ↺ │ ☐ A                    │
│   Emission       │                                                  │ ☑ A.MECopy1            │
│   Normal Map     │ v Lighting                                       │ ☐ A.MECopy2            │
│   Anisotropy     │ • As Unlit                 [------o---] [0.00] ↺ │ ☐ A.MECopy3            │
│   Back Light     │   Light Min                [---o------] [0.05]   │ ☐ A.MECopy4            │
│   Reflection     │   Light Max                [-------o--] [1.00]   │                       │
│   MatCap         │                                                  │                       │
│   Outline        │ v Main Color                                     │                       │
│   Dissolve       │   Color            R [1] G [1] B [1] A [1] [■] │                       │
│                  │   Main Texture             [Export] [Import] ↺ │                       │
├──────────────────┴──────────────────────────────────────────────────┴───────────────────────┤
│ Basic · 24 presentation rows · Search persisted · 7 Advanced changes                     │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

Notes:

- The top shader context is shown only when the current presentation sections
  agree. Otherwise it reads `Multiple shaders`; it never guesses from the first
  material.
- `Selected: ...` is derived from current selection collections. It does not
  introduce a singular active renderer.
- The footer says `presentation rows`, not `available`, `visible`, or total
  modified properties. Those general totals do not exist today.
- Texture thumbnail/name is intentionally absent. The current row model has
  only `Changed` and `Exists`, not a thumbnail/name presentation backend.
- Category modified counts and warning badges are intentionally absent.
- `More…` may be implemented only after one reusable menu surface exists and
  only with confirmed actions listed below. Until then, the existing real
  buttons remain visible.

## Panel geometry

The current side panels are external siblings positioned with anchors; they do
not participate in one `HorizontalLayoutGroup`. The redesign may make them
look contiguous, but Phase 1 does not pretend they already reflow as a single
layout.

Initial design metrics for later token implementation:

| Region | Minimum | Default | Maximum / rule |
| --- | ---: | ---: | --- |
| Left | 130 | 160 | 260 |
| Center | 420 | remaining | Must never become negative or overlap |
| Right | 170 | 210 | 320 |
| Top bar | 30 | 32 | Two rows at narrow widths |
| Context strip | 30 | 32 | May wrap only through a deliberate narrow variant |
| Every central virtual row slot | 28 | 28 | One fixed height for properties and all headers |
| Footer | 24 | 24 | Event-driven updates only |

Runtime splitters are **deferred**. `VirtualList` creates its pool from the
initial viewport and has no current pool-resize operation. A drag splitter
could expose too few views or cause uncontrolled layout work. The approved
first implementation provides collapse only and records resize in `ROADMAP.md`.

## Wide, narrow, and collapsed wireframes

### Left collapsed

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ Material Editor  Multiple shaders  [BASIC|ADVANCED] [Search…] P ×           │
├───┬────────────────────────────────────────────────┬─────────────────────────┤
│ » │ Selected: 3 renderers · 2 materials           │ Renderers · 8          «│
│   │                                                │ [Filter…                ]│
│   ├────────────────────────────────────────────────┤ ☑ body                   │
│   │ v Material [Copy] [Paste] [⋯] / property rows  ├─────────────────────────┤
│   │                                                │ Materials · 2          «│
│   │                                                │ ☑ skin_body             │
│   │                                                │ ☑ skin_head             │
├───┴────────────────────────────────────────────────┴─────────────────────────┤
│ Advanced · 68 presentation rows                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

The collapsed left rail contains only an unambiguous expand button. No
category icons are invented.

### Right collapsed

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ Material Editor  KKLT/Opaque  [BASIC|ADVANCED] [Search…] P ×                │
├──────────────────┬───────────────────────────────────────────────────────┬───┤
│ Categories     « │ Selected: 1 renderer · 1 material                    │ « │
│ ▌ Base Settings  │                                                       │   │
│   Lighting       ├───────────────────────────────────────────────────────┤   │
│   Main Color     │ v Material [Copy] [Paste] [⋯] / property rows         │   │
│   Shadow         │                                                       │   │
├──────────────────┴───────────────────────────────────────────────────────┴───┤
│ Basic · 24 presentation rows                                                │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Minimum-width mode

At approximately 700-760 units, both side panels remain available but one may
need to be collapsed by the user. The top bar becomes two compact rows; it does
not shrink text below the legibility floor.

```text
┌──────────────────────────────────────────────────────────────┐
│ Material Editor   Multiple shaders                    [More] ×│
│ [ BASIC | ADVANCED · 7 ]   [ Search…              ]  [P]     │
├───┬───────────────────────────────────────────────────────┬───┤
│ » │ Selected: 2 renderers · 1 material                   │ « │
│   │                                                       │   │
│   ├───────────────────────────────────────────────────────┤   │
│   │ v Material [Copy] [Paste] [⋯] / Lighting              │   │
│   │   Light Min          [---o-------]       [0.05]  ↺ │   │
│   │   Vector             X [0] Y [0] Z [0] W [0]  ↺ │   │
├───┴───────────────────────────────────────────────────────┴───┤
│ Basic · 24 presentation rows                                │
└──────────────────────────────────────────────────────────────┘
```

Vector remains one fixed-height row in the first implementation. A wrapped,
variable-height Vector would violate current `VirtualList` math, which assumes
every row is exactly `PanelHeight`; the two-line Vector concept is therefore
not approved without a separate virtualization change and measurement.

## Top bar behavior

- Title, mode selector, Search, Persist Search, and Close remain always
  reachable.
- Persist Search uses a clear labelled checkbox or compact `P` with tooltip;
  an ambiguous icon alone is not acceptable.
- Current shader/context is a read-only projection. Multiple materials with
  different shaders display `Multiple shaders`.
- Basic and Advanced remain a segmented pair. The active segment uses surface,
  accent line, and text state—not only `interactable = false`.
- `Advanced · N` retains the existing navigation backend. Hidden Advanced
  search results retain the existing `N advanced results hidden` + Show flow.
- Search keeps the exact wildcard/comma/property-prefix semantics and the
  frame-scoped coalesced refresh path.
- No menu item may parse manifests or enumerate renderers again.

## Left navigation

- Keep viewport-following semantics: categories belong to the material section
  currently intersecting the central viewport anchor.
- Use neutral rows, a 2-3 unit accent marker, clear selected surface, and full
  name tooltip.
- Keep independent category scroll, collapse control, navigation callback, and
  category tooltip.
- Do not show category modified count or warning badges until a real aggregate
  source exists.
- Expanded/collapsed panel state is UI-only. It must never enter card,
  coordinate, scene, material, or manifest data.

## Center context and action placement

The context strip is a read-only summary of selected renderer/material counts.
It must not call selection callbacks during binding and must not host actions
that require a singular row context.

Copy, paste, export, rename, import, and reset remain attached to the exact
currently bound `MaterialRowModel`, `RendererRowModel`, or property row. They
may be compacted into a row-owned menu, but cannot be promoted to a global
strip until the backend defines an unambiguous active target.

Confirmed actions eligible for consolidation:

| Group | Allowed items |
| --- | --- |
| Copy | `Copy Edits`, `Copy Material` or `Remove Material` when the current model exposes it |
| Paste | `Paste Edits` only |
| Renderer export | `Export UV Map`, `Export .obj` |
| Texture | `Import Texture`, `Export Texture`, `Reset` |
| More | `Rename material instances` when exposed by the model |

Not allowed: Paste Material, Reset Material, Technical Info, category
Copy/Paste, Undo, or any placeholder. `Recalculate Normals` remains the
existing renderer property control; moving it to a context menu would require
an exact current renderer model and must not imply singular selection.

One reusable menu surface may dispatch these existing callbacks. No menu is
created per pooled row. It must clear its previous model/context on close and
rebind, close on Escape/outside click, block click-through while open, and
release callbacks exactly.

## Central categories and rows

- Material, shader, and property category folds keep their current session
  keys and rebuild behavior.
- Category headers become neutral surfaces with a chevron and optional tooltip.
  No category master toggle, modified count, warning, or menu appears without
  backend.
- All labels use the existing display text. Internal property names remain
  available through existing tooltip/label-click metadata; they are not
  renamed.
- Reset becomes a compact `↺` control, enabled only when the existing changed
  calculation says reset is available. Hover/focus may reveal it, but the
  clickable geometry must remain stable and recyclable.
- Modified uses an accent marker plus active Reset so it is not color-only.
- Disabled uses lower contrast and disabled interaction; it must remain
  distinguishable from Mixed and unmodified.
- Enum preserves `Mixed` and `Unknown (value)` exactly.
- Vector preserves per-component Mixed and stays fixed-height.
- Float-backed Toggle preserves its distinct float semantics. A visual
  indeterminate marker may reflect `IsMixed`; it must not write a value until
  the user explicitly chooses on/off.
- Color keeps RGBA numeric entry and the current palette adapter. HDR values
  are not clamped to LDR.
- Texture retains current Import/Export/Reset behavior. No thumbnail or name is
  displayed because no current row presentation supplies them.

## Right selection panels

- Default redesigned state: full right panel visible; Renderer expanded;
  Materials expanded. This deliberately changes only view state from the
  current fresh-session default of hidden.
- Both lists remain simultaneous and preserve checkbox multi-selection.
- Each has an independent filter and `ScrollRect`.
- Header counts use the existing populated entry count; they are not material
  edit totals.
- Names use deliberate truncation and full-name tooltip.
- Renderer and Materials may collapse independently through new view-only
  state. Full-right collapse uses a narrow explicit reopen rail.
- Collapse does not clear selections or rebuild semantic values. A presentation
  rebuild is allowed only where the existing selection callback requires it.
- UI collapse preferences are BepInEx/session state only, never character or
  scene persistence.

## Footer

The first footer may display only data already available without a second
semantic pass:

- current UI mode;
- `presentation.Rows.Count`, labelled `presentation rows`;
- Persist Search state;
- `AdvancedChangeCount` in Basic when nonzero;
- `HiddenAdvancedSearchResultCount` when nonzero.

Do not display `available`, generic `modified`, or viewport-visible totals.
There is no reliable common backend for those labels. Updates occur only after
presentation/mode/search/config changes, never every frame.

## Empty, warning, tooltip, and selection states

The current presentation lacks explicit empty-state and warning view models.
Until those read-only states are added and tested, the redesigned UI leaves the
surface empty and relies on the log exactly as today; it does not show fake
messages. A later internal presentation state may add concise messages without
changing persistence.

Keep one shared tooltip panel. Lightweight `Tooltip` components remain on
eligible targets, including controls inside pooled templates; they are not
independent popup panels. Tooltip strings are updated during bind, not per
frame.

## No-write interaction contract

These actions must never call a repository setter/resetter: mode switch,
Search open/close/focus, category navigation, material/shader/category fold,
left/right/renderer/material panel collapse, hover, focus, tooltip, menu open,
menu close, window drag, and future splitter drag. Category folding currently
triggers a presentation refresh but not a material write.

Only explicit editing actions retain writes: value change, Reset, Paste Edits,
Copy/Remove Material where applicable, Rename, shader/render queue changes,
texture import/reset, and renderer property changes. Copy and export remain
read-only with respect to material persistence.

## Performance contract

The redesign is measured against `PERFORMANCE_RESULTS.md` and the latest JSON.
It introduces no per-frame semantic polling, manifest parsing, style traversal,
row menu allocation, tooltip popup per row, or continuous layout rebuild. The
orientation targets are at most 5-10% regression for opening, mode changes, and
Search; no periodic new scroll GC; no continuous idle allocations after
warm-up; and no pool/listener growth across repeated open/close.

## Deliberately deferred or excluded

- Runtime drag splitters and panel-width persistence beyond existing config.
- Variable-height Vector layout.
- Texture thumbnails/channel preview.
- Generic modified totals and category modified counts.
- Visible warnings/details until a presentation backend exists.
- Undo, Modified Only, category Copy/Paste/master toggle, favorites, presets,
  profile library, light theme, theme editor, external fonts, animations,
  gradients, glow, blur, bulk shader/texture operations, and new manifest
  schema.
