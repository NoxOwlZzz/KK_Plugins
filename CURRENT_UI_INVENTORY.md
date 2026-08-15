# Current Material Editor UI inventory

> **Historical snapshot of the pre-hierarchy UI.** Basic/Advanced rows and
> indicators listed below were subsequently removed and are not current
> requirements. Use the active runtime checklists for present behavior.

## Purpose

This source-level inventory freezes the optimized UI before the Minimal
Three-Panel visual implementation. It records what exists, what is only
implicit, and what does not exist. No runtime visual inspection or screenshot
was used.

Status vocabulary:

- **Confirmed**: present in current source and connected to a real callback.
- **Partial**: a semantic or visual fragment exists, but not the requested full
  behavior.
- **Absent**: no current UI component/backend was found; it must not appear as
  a working control.
- **Pending runtime**: source exists, but its visual/interactive result has not
  been verified in Maker or Studio for this baseline.

## Window, header, and mode strip

| Element | Status | Current implementation and behavior |
| --- | --- | --- |
| Canvas/window | Confirmed | Screen-space overlay Canvas, sorting order 1000, `CanvasScaler`, `GraphicRaycaster` |
| Main panel | Confirmed | White `Image`, black `Outline`, movable by the 20-unit header |
| Header title | Confirmed | Centered `Material Editor` text |
| Current shader in top bar | Absent | Shader appears inside each material section, not in the window header |
| Main Search | Confirmed | Always-visible 79-unit field labelled `Filter`; latest-wins refresh is coalesced |
| Persist Search | Confirmed | Header checkbox writes only the BepInEx `Persist Filter` config entry |
| Category navigator toggle | Confirmed | `>`/`<` text button; toggles the left navigator in memory |
| Collapse all categories | Confirmed | Double fold glyph; applies to property categories across the current presentation |
| Right-lists toggle | Confirmed | One `>`/`<` button toggles Renderers and Materials together |
| Close | Confirmed | Procedural two-line X; closes and releases transient UI content |
| Global overflow menu | Absent | No reusable context-menu service or `⋮` menu exists |
| Basic selector | Confirmed | Fixed 65-unit button in a second 20-unit strip |
| Advanced selector | Confirmed | Fixed 80-unit button; active mode is represented by non-interactable state |
| Advanced change count/navigation | Confirmed | Optional `Advanced changes: N` button navigates to the first modified Advanced row |
| Hidden Advanced search count | Confirmed | Optional text and `Show` button switch to Advanced without clearing Search |
| Footer/status bar | Absent | No lower status surface or general row/modified counters |

The header and mode strip use fixed offsets. Their combined height is 40 units,
and the main property viewport begins below them. The current arrangement is
compact but becomes crowded when status text is active.

## Three working zones

### Left: category navigator

`CategoryNavigatorView` is **confirmed**. It is a 150-unit panel anchored left
of the main panel. It displays the material name, shader name, and category
entries for the material section intersecting the central viewport anchor.
Each entry has an independent collapse button and navigation button. The
active entry changes from purple to green.

It has a clamped independent `ScrollRect`. It appears only when the current
section has named categories and `_expanded` is true. Collapse state is not a
BepInEx preference and there is no collapsed rail. There are no category
modified counts, warning badges, ellipsis implementation, or explicit
accessible selected marker beyond color.

### Center: virtualized property list

The center has one main `ScrollRect`, one `VerticalLayoutGroup`, one
`ContentSizeFitter`, and one `VirtualList`. The virtual list creates
`ceil(initial viewport height / 22)` complete `RowView` clones, then rebinds
them to the final presenter sequence while scrolling. It retains top-row
identity across presentation replacement and restores focus to an exact model
when possible.

Material and shader rows are part of this same virtual sequence; there is no
separate fixed context strip. Renderer rows precede material sections. Every
current active row family is one line high.

### Right: renderer and material selection

`RendererListPanel` and `MaterialListPanel` are **confirmed** and anchored to
the right of the main panel, split equally top/bottom. Each has a title, filter,
independent `ScrollRect`, and dynamic rows whose entire surface toggles a
checkbox. Multi-selection is preserved.

Both panels start hidden in a fresh session and are shown/hidden together.
They do not show item counts, explicit empty states, deliberate ellipsis, or
full-name tooltips. They cannot collapse independently. Their width is
controlled by the existing `UI List Width` config (default 180, range 100-500),
not by a drag splitter.

The Rename workflow uses a separate right panel with a renderer checklist,
rename input, and Rename button. It replaces the normal selection panels while
active.

## Actions and backend connections

| Visible action | Current location | Real backend |
| --- | --- | --- |
| Export UV Map | Renderer row, long button | `Export.ExportUVMaps` |
| Export `.obj` | Renderer row, long button | Deferred request in session, then current OBJ exporter |
| Copy Edits | Material row, long button | `MaterialEditorEditService.CopyMaterialEdits` |
| Paste Edits | Material row, long button | `MaterialEditorEditService.PasteMaterialEdits`; disabled when copy buffer empty |
| Copy/Remove Material | Material row, long button | `CopyOrRemoveMaterial`; unavailable for projector material |
| Rename material instances | Material row, `>` button | Existing rename panel and repository-backed name setter |
| Shader selection | Shader row dropdown | Existing shader repository setters and deferred refresh |
| Reset shader | Shader row `Reset` | Existing repository reset |
| Render Queue | Dedicated input row | Existing set/reset callbacks |
| Recalculate Normals | Renderer toggle row | Existing renderer property set/reset |
| Import Texture | Texture row, long button | Existing file dialog, repository, watcher, persistence |
| Export/No Texture | Texture row, long button | Existing texture exporter; visual text changes with existence |
| Timeline interpolation | `O` buttons when Timeline is available | Existing interpolation selection callbacks |
| Color picker | Blank swatch button | Maker/Studio palette adapters; reopen lifecycle fix is in baseline commit |

There is no confirmed global Reset Material, Paste Material, Technical Info
view, category Copy/Paste, or Undo backend.

## Row family inventory

| `RowItemType` | Current control | Modified/reset behavior | Mixed support |
| --- | --- | --- | --- |
| `Renderer` | Name, Timeline, Export UV, Export OBJ | None | None |
| `RendererEnabled` | Toggle | Changed overlay + Reset | No explicit Mixed model |
| `RendererShadowCastingMode` | Dropdown | Changed overlay + Reset | No explicit Mixed model |
| `RendererReceiveShadows` | Toggle | Changed overlay + Reset | No explicit Mixed model |
| `RendererUpdateWhenOffscreen` | Toggle, non-KK targets | Changed overlay + Reset | No explicit Mixed model |
| `RendererRecalculateNormals` | Toggle | Changed overlay + Reset | No explicit Mixed model |
| `Material` | Fold, name, Copy/Paste/Copy-or-Remove/Rename | Section collapse; no general reset | None |
| `Shader` | Fold, category fold, Timeline, dropdown, Reset | Changed overlay + Reset | No explicit Mixed model |
| `ShaderRenderQueue` | Integer input, Reset | Changed overlay + Reset | No explicit Mixed model |
| `PropertyCategory` | Fold + category label | Collapse only | None |
| `TextureProperty` | Timeline, Export/No Texture, Import, Reset | `Changed` flag + Reset | No explicit Mixed field |
| `TextureOffsetScale` | Offset X/Y, Scale X/Y, Reset | Composite compare + Reset | No explicit Mixed field |
| `ColorProperty` | RGBA numeric inputs, swatch/picker, Timeline, Reset | Value compare + Reset | No explicit Mixed field |
| `FloatProperty` | Slider, numeric input, Timeline, Reset | Value compare + Reset | No explicit Mixed field |
| `KeywordProperty` | Toggle, Reset | Value compare + Reset | No explicit Mixed field |
| `EnumProperty` | Dropdown, Timeline, Reset | Mixed/value compare + Reset | Explicit caption/label Mixed |
| `VectorProperty` | X/Y/Z/W numeric inputs, Timeline, Reset | Per-component Mixed/value compare + Reset | Explicit per component |
| `FloatToggleProperty` | Toggle, Timeline, Reset | Mixed/value compare + Reset | Explicit label Mixed; not tri-state |

Float rows always have a slider because the current float editor exposes
minimum/maximum values. The future UI must not manufacture sliders for any new
free-float semantic type without a real range source.

## LayoutGroups, masks, and geometry

- Main properties: `ScrollRect` + `Mask` + `VerticalLayoutGroup` +
  `ContentSizeFitter`.
- Categories: independent `ScrollRect` with a one-unit vertical spacing.
- Renderer, Material, and Rename lists: one independent `ScrollRect` each.
- Every selection entry uses a `HorizontalLayoutGroup` inside a masked row.
- Every pooled family uses a `CanvasGroup` and `HorizontalLayoutGroup`.
- `RowLayoutCatalog` applies family column specifications and runtime debug
  diagnostics.
- Default row height is 22; headers are not taller than value rows.
- Common row padding is 1; property labels add a 3-unit left inset.
- Main margin is 5; scrollbar offset is -15.
- The center has no horizontal `ScrollRect`.

## Inputs, dropdowns, and focus

`NumericInputView` is **confirmed** and gives numeric controls an unclipped
viewport, culture-aware parsing with invariant fallback, `0.####` display
format, round-trip edit format, and a literal `Mixed` state. Programmatic sets
do not notify listeners.

Dropdown text is explicitly 16 with minimum 12 and a one-unit vertical inset.
Enum option data is cached per pooled row with a retained cap of 128 after
shrink; active lists over 128 remain complete. Shader dropdowns add a filter
field/clear control inside their popup and auto-scroll to the selection.
Programmatic Dropdown/Toggle/Slider/Input updates have callback guards.

Rendered caret, popup position, long option clipping, keyboard focus, and
heterogeneous row recycling remain **pending runtime**.

## Typography, sprites, colors, and borders

The UI uses only the built-in `Arial.ttf` loaded by `UIUtility`; no external
font is distributed. `MaterialEditorTextRole` centralizes alignment intent,
and `RowTextVisualCenter` adjusts vertical glyph centering. Most text is capped
at 16 and can use Unity best-fit; minimum sizes can fall to 2 for generic
buttons/input fields, while Dropdown and Vector explicitly stop at 12.

Unity DefaultControls sprites are loaded from the embedded
`DefaultResources.unity3d` bundle. The current palette is:

| Role | Current color |
| --- | --- |
| Main panel | white |
| Header/mode panel | gray |
| Side panel | `(0.42, 0.42, 0.42, 1)` |
| Navigator shader header | `(0.64, 0.64, 0.64, 1)` |
| Ordinary row | white at 0.6 alpha |
| Renderer row | orange at 0.5 alpha |
| Material row | green at 0.5 alpha |
| Category row | purple at 0.5 alpha |
| Property row base | transparent white |
| Modified overlay | black at 0.3 alpha |
| Scrollbar | white at 0.6 alpha |
| Shader hint underline | bright blue |

The main panel and category navigator have black outlines. Buttons and inputs
largely inherit Unity DefaultControls borders and color blocks, which produces
many similarly weighted boxes.

## Tooltips, warnings, and empty states

Tooltips are **confirmed**. There is one shared popup, 280 units wide and at
most 360 high, clamped to the Canvas. Eligible controls have a `Tooltip`
behavior which stores standard and shader-hint text. Standard tooltips show on
hover; holding Shift prefers the shader hint. Dragging suppresses display.
There is no configurable delay in current code.

User-facing row warnings, warning badges, global error summaries, and explicit
empty-state messages are **absent**. Manifest/condition/layout problems are
logged, with bounded warning-once behavior where applicable. The redesign must
not claim those log diagnostics are already a visible warning backend.

## Current state and persistence ownership

| State | Owner | Saved in card/coordinate/scene? |
| --- | --- | --- |
| Material edits | Repository/controllers | Yes, through existing persistence paths |
| Main Search text | Session; optionally BepInEx behavior via Persist Filter | No |
| Persist Search setting | BepInEx config | No |
| Basic/Advanced | Session; optional last-mode BepInEx config | No |
| Selected renderer/material | Session | No |
| Material/shader/category folds | Session dictionaries | No |
| Left navigator expanded | `CategoryNavigatorView` memory | No |
| Right lists visible | Session memory | No |
| Individual right-section collapse | Absent | No |
| Window width/height/scale/list width | BepInEx config | No |
| Window dragged position | Runtime RectTransform memory | No |

## Concrete usability issues identified from source

- Fifteen row-family Reset buttons retain the full word and a fixed 40-unit
  column in the pooled template.
- Orange, green, and purple full-row fills dominate hierarchy rather than
  providing a small recognition cue.
- Labels, input fields, and buttons have similar Unity-default visual weight.
- Several 20-22 unit interactions are smaller than the requested target.
- Secondary actions are expanded permanently in active Renderer, Material, and
  Texture rows.
- Header and mode content depend on fixed offsets and can compress.
- Right lists are not visible by default and cannot collapse independently.
- Left collapse has no dedicated collapsed rail or persisted UI preference.
- Long names rely on clipping/best-fit instead of deliberate ellipsis plus
  full-name tooltip.
- There is no reusable action menu, footer, explicit empty state, or visible
  warning component.
- Selected category relies on a full-color change; disabled uses alpha only;
  Float Toggle Mixed lacks a true indeterminate glyph.
- Dynamic resizing is unsafe without extending the VirtualList pool-resize
  contract, so only collapse is approved for the first implementation.

These are source-level observations. Whether they are visually distracting in
Maker or Studio is intentionally left to the user's later review checklist.
