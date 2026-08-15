# Material Editor UI before/after structure

## Evidence boundary

This compares the frozen pre-redesign source at `525a8c4b` with the completed
source at `8c36e8df`. It is a hierarchy/ownership comparison, not a rendered
before/after screenshot and not a claim of visual acceptance.

## Before: optimized pre-redesign structure

```text
MaterialEditorCanvas
└─ Main panel
   ├─ Header controls assembled in WindowView
   ├─ Mode/search row
   ├─ Central ScrollRect
   │  └─ fixed VirtualList cache created from initial viewport
   ├─ Category navigator panel
   ├─ Renderer list (global visibility default off)
   ├─ Material list (global visibility default off)
   └─ Rename list overlay

Rows
├─ Renderer: name + many direct actions
├─ Material: fold/name + many direct actions
├─ Property category: compact header/action
└─ Property families: fixed 22-unit slots with textual Reset buttons
```

The UI was already programmatic and pooled. Functional metadata,
Basic/Advanced, conditions, selection and persistence were established before
the visual work. The baseline is documented in `UI_BASELINE.md` and
`CURRENT_UI_INVENTORY.md`.

## After: completed three-panel structure

```text
MaterialEditorCanvas
├─ TooltipPanel (one reusable passive surface)
├─ MaterialEditorGlobalMenu (one reusable top-bar popup)
├─ MaterialEditorRowActionMenu (one reusable four-action row popup)
└─ Main panel
   ├─ TopBarView
   │  ├─ Header row: title, shader context, Studio slot, More, Close
   │  └─ Mode row: Basic/Advanced, search, persist-search/status actions
   ├─ Central ScrollRect
   │  ├─ VirtualList content (active capacity + bounded high-water cache)
   │  └─ one passive central empty-state Text
   ├─ CategoryNavigatorView
   │  ├─ expanded panel
   │  └─ mutually exclusive 24-unit collapsed rail
   ├─ Renderer SelectListPanel
   │  └─ header, filter, entries, passive empty-state Text
   ├─ Material SelectListPanel
   │  └─ header, filter, entries, passive empty-state Text
   ├─ one 24-unit global right collapsed rail
   └─ legacy-compatible Rename overlay

Rows
├─ Renderer: direct name/Timeline + More for Export UV/.obj
├─ Material: direct fold/name + More for Copy/Paste/Copy-or-Remove/Rename
├─ Property category: one full-surface Button; passive chevron and name
└─ Property families: fixed 22-unit slot, compact ↺, full-name tooltip;
   Float slider only when explicit usable bounds exist
```

## Structural delta

| Area | Before | After |
| --- | --- | --- |
| Theme | Coarse/default-light styling | One internal fixed dark token system |
| Top bar | Window-owned controls | Dedicated `TopBarView` plus one global menu |
| Shader context | Distributed/implicit | Read-only current/multiple-shader projection |
| Left navigation | Panel only | Panel plus collapsed rail and selected marker |
| Right panels | Globally hidden by default | Renderers and Materials visible together by default |
| Right folds | No independent fold | Independent view-only Renderer/Material folds plus global rail |
| Row actions | Multiple inline long buttons | Existing secondary callbacks consolidated into one leased shared menu |
| Category header | Smaller target | Entire neutral 22-unit surface is the single action |
| Reset | Text `Reset` | Compact `↺`, same changed/interactable contract |
| Float | Slider path assumed | Explicit `HasRange`; unbounded float receives expanded numeric input only |
| Long names | Uneven source exposure | Standard full-name tooltip on supported labels |
| Empty feedback | No shared policy | Exact count/filter-based passive overlays |
| Warning/footer | Proposed in design sketches | Deliberately absent because no truthful aggregate backend exists |
| Responsive | Fixed anchors; initial-size pool | Real-Canvas policy, bounded drag, auto-collapse, pool growth/shrink lifecycle |
| Public API/persistence | Existing contracts | Unchanged by UI structure; new types remain internal |

## Quantitative source accounting

The numbers below count source-owned templates/surfaces, not rendered pixels,
Unity memory or a fixed number of simultaneously visible controls. Visibility
depends on the active row family, data counts, Changed/Disabled state, Studio
context, filters, panel folds and whether a popup is open.

| Metric | Frozen pre-redesign source | Source at `8c36e8df` | Evidence boundary |
| --- | --- | --- | --- |
| Reset controls/templates | 15 buttons captioned `Reset` in the complete pooled `RowView` template | The same 15 functional Reset controls, captioned `↺`; shader Dropdown option `Reset` remains text | Source-derived template count; visible Reset count is active-family/state dependent |
| Existing actions consolidated | 3 global view actions, 2 Renderer exports and 4 Material actions were direct controls | The same 9 callbacks are presented through shared menus; no new edit/export operation was added | Exact callback migration count, not a net button-count claim |
| View/row menu surfaces | No `MaterialEditorPopupMenu` or `MaterialEditorRowActionMenu` | 2 reusable window-owned surfaces: one global and one row-action popup; they cross-close | Exact surface count; no menu surface or listener is created per open/per row |
| Shared rendered tooltip panels | 1 `TooltipPanel` | 1 `TooltipPanel`, brought above menus and kept non-raycasting | Exact rendered-panel count only |
| Tooltip target components | 46 `Tooltip` components in the complete pooled `RowView` template; other window/list targets remain context dependent | Clean `RowView` template: 40; one clone warmed across every family: source upper bound 50; shared row menu: 4 fixed window-level targets | These are lightweight target components, not rendered panels. Do not multiply them by the pool or claim a global runtime total without a hierarchy snapshot |
| Passive empty-state Text objects | None under a shared policy | 3 precreated objects in the normal three-panel view: central, Renderers and Materials | Exact normal-view surface count; Rename keeps its legacy hierarchy and adds none |
| Complete pooled-row controls | 60 interactive controls across 18 family panels; only one family active per clone | No final aggregate control census is claimed; action migration changes the template while family visibility remains exclusive | A source recount of every inactive child is outside this structural comparison; runtime visible count is **PENDING** |
| Simultaneously visible buttons | Conditional on row family, status actions, selection entries and data | Conditional on the same factors plus panel rails, Studio slot and the currently open shared menu | No single truthful constant; in-game census is **PENDING** |
| Total UI GameObjects | Programmatic hierarchy plus dynamic entries, dropdown items and initial row pool | Programmatic hierarchy plus reusable menus/empty Text, dynamic entries/dropdown items and bounded row cache | No Unity scene/object snapshot was captured; total and active counts are **PENDING** |
| Virtual `RowView` ownership | Initial cache `ceil(initial viewport / 22)`; no responsive high-water growth | 1 inactive clean template plus at most 44 cached clones; active capacity is released on shrink | Exact policy/cap. In the 13-model contract scenario: 13 active clones plus the template; not a runtime memory result |
| Presentation rebuilds | Event-driven presentation path; synthetic harness scenarios only | Existing event-driven/coalesced path retained; responsive layout and empty feedback add no polling rebuild | Per-interaction and per-session rebuild totals were not instrumented in Unity and remain **PENDING** |

Consequently, “2 menus”, “1 tooltip panel” and “44 cached clones” are bounded
ownership facts. They must not be added to dynamic target components or treated
as a claim about the number of visible buttons, total GameObjects, rebuilds,
allocations or frames in a real Maker/Studio session.

## Preserved boundaries

- All material writes still travel through existing row callbacks and
  repositories/edit services.
- Basic/Advanced, filter semantics, schema compatibility and persistence data
  are not replaced by the theme or panel state.
- Virtual rows remain exactly 22 units; no variable-height row or wrap was
  introduced.
- Studio keeps an explicit lifetime-safe header context slot.
- Rename keeps its legacy child assumptions and does not overwrite the global
  list visibility preference.
- Color swatches remain value-owned; common selectable tint does not overwrite
  their displayed color.
- There is no menu per row, footer, thumbnail cache, warning scanner, splitter,
  animation system, polling rebuild or new public UI API.

## Runtime comparison still required

Screenshots from the same object/material, resolution, UI scale and camera are
required to create a genuine visual before/after comparison. None is claimed
here. Use `USER_VISUAL_REVIEW_CHECKLIST.md` and attach the requested captures
and logs.
