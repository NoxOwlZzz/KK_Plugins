# Material Editor UI redesign readiness

> Historical pre-redesign readiness report. Its body records the optimized
> source state that preceded the UI phases. The redesign now ends at
> `8c36e8df`: clean API/Metadata/harness/all-target gates, binary audit, restore
> verification, two-file deployment, installed-byte verification and
> editor-closed Maker/Studio load smokes **PASS**. All 58 visual cases,
> interaction with Material Editor open, persistence roundtrips and Unity
> runtime profiling remain **PENDING**. Current evidence is in
> `UI_STRUCTURAL_VALIDATION.md`, `UI_PERFORMANCE_RESULTS.md`,
> `UI_BACKUP_INFO.md` and `USER_VISUAL_REVIEW_CHECKLIST.md`.

## Purpose and boundary

This document maps the post-optimization candidate so a later visual phase can
start from explicit seams without changing behavior accidentally. The
functional reference remains checkpoint
`3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`.

No theme, appearance, icon, font, density, layout, navigation, or interaction
redesign is included in the optimization. Final rendered UI, persistence, and
runtime lifecycle validation are **PENDING**, so this document does not approve
visual work yet.

| Final release field | Status |
| --- | --- |
| Clean API/metadata/harness/all-target build | **PASS** |
| Frozen DLL/package path, version, size, and SHA-256 | **PASS**: DLL 4.0.3.0, 1,008,640 B, `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26`; ZIP 852,106 B, `8293F987827F8E4F3841F4FD833C82EB4F5E96CA2EE11833236C6C03D5A5B369` |
| Final deployed DLL Cecil re-audit | **PASS**: build/installed bytes identical; 16/16 assembly, 428/428 type, 2,275/2,275 member references; 0 failures/release-audit violations |
| Deployment and installed-file/config verification | **PASS**: only DLL/native copied, one loadable DLL; post-smoke BepInEx added four expected safe-default diagnostic keys and preserved all 42 existing values |
| Minimal Maker/Studio plugin-load smoke with Material Editor closed | **PASS**: base + Maker/Studio 4.0.3 loaded, one complete Chainloader startup per process, 0 attributed errors |

Status terms:

- **Confirmed**: present in current source and protected by automated evidence.
- **Prepared seam**: an internal boundary made safer by the optimization.
- **Pending runtime/visual**: needs the frozen DLL in Maker/Studio.
- **Deferred**: belongs to a separately authorized visual-redesign phase.

## Current architecture

| Layer | Current responsibility | Readiness observation |
| --- | --- | --- |
| Window/view composition | `MaterialEditorWindowView`, `CategoryNavigatorView`, `SelectListPanel`, `TooltipManager` | Programmatic hierarchy remains stable; geometry is still concentrated in runtime builders |
| Selection/lifetime | `MaterialEditorSelectionController`, `MaterialEditorSessionState`, shared UI invalidation helpers | Normal close and destroyed/replaced target lifetimes are now distinct and tested |
| Presentation orchestration | `MaterialEditorPresenter`, frame-scoped invalidation coordinator | Search/Shader/Condition requests are coalesced with explicit precedence/recovery |
| Material section semantics | `MaterialSectionPresenter`, condition dependency graph, property row factories | Compatibility, mode, search, conditions, providers, categories, and rows remain one semantic pipeline |
| Static metadata | `PropertyOrganizer`, manifest/tooltip registries, presentation policies | Suitable for semantics; must not own targets or visual assets |
| Runtime material lookup | bounded `MaterialPropertyIdCache` | Internal Unity optimization only; public/persistence property strings remain authoritative |
| Row models | built-in/custom row models | Models still carry values, targets, and callbacks; never cache them as theme assets |
| Row view/control tree | generic `RowView` plus family controls | One bounded pooled hierarchy contains all supported families |
| Row binders | `RowBinder`, exact `ListenerScope`, family binders | Correct seam for recyclable visual state and callback guards |
| Enum projection | per-row `EnumDropdownOptionCache` | Reuses options with a 128-entry retained cap while displaying larger active lists fully |
| Virtualization | `VirtualList` | Owns bounded pool, anchor identity, explicit release, and dirty/idle behavior |
| Style/layout helpers | `MaterialEditorStyles`, `MaterialEditorLayout`, `RowStyle`, column overrides, `NumericInputView` | Partial design-token seam exists; direct values still remain elsewhere |

The implementation is closer to presenter plus programmatic view than strict
MVVM. Renaming layers or moving target-bearing callbacks merely to resemble a
framework is not a redesign-readiness improvement.

## Semantic boundary for a future view

The current presenter pipeline decides:

- compatibility and Hidden/UI-level filtering;
- Basic versus Advanced membership;
- legacy search behavior;
- `ShowIf` visibility;
- provider contributions, category membership, and modified counts;
- final row type, Mixed/changed semantics, and edit/reset callbacks.

A future view must consume that result. It must not independently reimplement
which properties exist, are visible, enabled, Mixed, modified, or persisted.

### Invalidation state now prepared

- Search, Shader, and Condition requests share a frame-scoped typed
  coordinator.
- Search refresh is latest-wins; gates recover from null/throw coroutine start.
- The condition graph exists only when conditions exist.
- `ShowIf` changes make one transactional rebuild decision for the batch.
- Stale presentation handles are rejected.

This is a prepared seam for future visual state. It is not permission to add a
second visual polling loop or to bypass presentation semantics.

## Views and runtime hierarchy

### Confirmed

- `MaterialEditorWindowView` creates the Canvas, main/header/mode panels,
  filter, status controls, main scroll view, category navigator, and side
  panels.
- `CategoryNavigatorView` follows the VirtualList viewport anchor.
- `SelectListPanel` builds renderer, material, and Rename entries dynamically.
- `TooltipManager`, `Tooltip`, and `ShaderHintUnderline` implement hover/Shift
  help and underline behavior.
- `DropdownFilter`, `NumericInputView`, `FloatLabelDragTrigger`, and
  `AutoScrollToCenter` remain focused interaction components.
- No authored Material Editor UI prefab is used. `UILib.UIUtility` creates
  controls, `MaterialEditorWindowView` builds the window, and
  `RowViewFactory.CreateTemplate` builds the row template at runtime.
- The template is cloned into the bounded pool; the original hidden template is
  destroyed after initialization.
- The close symbol is procedural; fold/navigation affordances are text glyphs;
  `Resources/MaterialEditorIcon.png` is not a row/theme icon registry.

### Prepared lifetime contract

- Normal close releases presentation models, row bindings/listeners, category
  navigation, transient side entries, watcher, and deferred work.
- The bounded control pool and scroll anchor remain reusable.
- Valid target/data, filter/mode/collapse/selection order, and a live Rename
  workflow remain for direct reopen.
- Destroyed/replaced/scene-invalidated targets clear their strong references,
  callbacks, palette, selection/Rename state, and pending OBJ work.
- Maker/Studio ownership checks use exact identity and fake-null-safe cleanup.

A future view must preserve the distinction between normal close and destroyed
target invalidation.

## RowModels, RowViews, RowBinders

### Confirmed

- Built-in families have dedicated model/binder behavior.
- Enum/Dropdown, Vector2/3/4, and Float-backed Toggle are distinct from legacy
  Float/Color/Keyword semantics.
- One runtime template contains all control families; binding hides the old
  family before configuring the new one.
- Exact listener scopes remove only the current bind's callbacks.
- Programmatic updates retain callback guards, including unchanged Toggle and
  Dropdown selection handling.
- Layout is partly expressed through reusable column specifications and
  `RowColumnLayoutOverride`.
- Vector numeric fields and Dropdown text have centralized readability metrics
  and source/layout regression guards.

### Enum/Dropdown preparation completed

- Option/value storage is owned by one pooled row.
- Stable 500-cycle rebinding and 64 -> 2 -> 64 oscillation allocate 0 B after
  warm-up in the focused managed test and invoke no callbacks.
- Mixed, Unknown, null entries, duplicates, last-duplicate selection, and
  same-index caption refresh retain their semantics.
- Retained `Dropdown.OptionData` is capped at 128 per row. More than 128 options
  remain visible while active; buffers compact after shrink.
- Rebinding A to B removes A's listener before B becomes active.

This reduces allocation risk for a later visual change, but real Dropdown font,
caption, focus, and popup rendering remain **PENDING visual validation**.

### Safe future seams

- `MaterialEditorStyles.ApplyPanel`, `ApplyText`, `ApplyButton`,
  `ApplyInputField`, `ApplyDropdown`, and `ApplyRow` are the safest current
  global styling hooks.
- `RowViewFactory.<family>.cs` is the structural seam for one control family.
- Row column specifications are safer than post-clone ad-hoc RectTransform
  edits.
- Binder methods are the correct place to reset every recyclable visual state.

### High-risk changes

- Row height, template hierarchy, control names, or layout-component order can
  break VirtualList math, runtime assertions, Dropdown lookup, focus restore,
  and clone behavior.
- Moving programmatic `Set` calls without callback guards can write values while
  opening/recycling a row.
- Caching a RowModel, condition handle, provider result, or binder context in a
  theme system can retain scene targets.
- Splitting the generic row pool by type needs profiling and transition tests;
  it is not automatically an improvement.

## VirtualList readiness

### Completed preparation

- bounded viewport-sized pool and reuse;
- explicit model/binding release while keeping the pool;
- unchanged-frame dirty/scroll/viewport fast path;
- stable anchor/context on presentation replacement;
- listener-balance and pool-stability automated stress;
- no blanket pooled-row listener removal.

### Still pending before row/layout redesign

- real focus and typed input across recycled rows;
- real popup/focus behavior for heterogeneous Dropdown/Toggle/Vector recycling;
- rendered hierarchy/layout invariants for every row family;
- Maker/Studio empty list, mode/material switch, collapsed category, and large
  practical list behavior;
- Unity CPU/allocation/retained-size measurement for idle, scroll, close, and
  reopen.

## Current visual-state boundaries

| State | Current representation | Future rule |
| --- | --- | --- |
| Basic/Advanced selected | selected mode button is non-interactable | Preserve event meaning/order and presentation membership |
| Changed | changed overlay color | Derive from existing row model only |
| Mixed | model/binder-specific text/control state | Do not recompute independently in the view |
| Hidden by `ShowIf` | property absent from rebuilt presentation | One semantic pipeline remains authoritative |
| Collapsed/expanded | category/all-category glyph state | Preserve keys and anchor behavior |
| Hidden Advanced search | status text plus Show action | Preserve mode/search semantics |
| Tooltip/Shift hint | hover panel plus underline | Preserve suppression and fallback behavior |
| Focus/programmatic update | EventSystem focus plus callback guards | Must not create writes during bind/recycle |

A passive internal visual-state projection could later name these states, but
it must not become a per-frame semantic poller.

## Colors, metrics, typography, and icons

### Centralized today

- `MaterialEditorStyles` contains primary panel/header/side/navigator/row,
  changed-overlay, scrollbar, and shader-hint colors.
- `MaterialEditorLayout` contains common margins/heights, list/navigator widths,
  row padding, common buttons, family control widths, and Dropdown/Vector font
  limits.
- `MaterialEditorTextRole` and `MaterialEditorTextStyleState` centralize role and
  alignment intent.
- `RowTextVisualCenter` adjusts glyph placement and remains font-metric
  sensitive.

### Direct values still present

- tooltip color/font/width/height/padding;
- some black outline/text and procedural close-glyph values;
- default UILib control color-block behavior;
- window header/status/side-panel anchors and offsets;
- additional fixed geometry in Dropdown filter, category navigator, selection
  panels, and glyph construction.

A later `ThemeData`, `UIMetrics`, typography data object, or icon registry must
first reproduce these exact current values and application order. It must not
introduce theme selection, custom fonts/artwork, asset bundles, or runtime
theme polling during optimization.

## Public API, persistence, and target boundaries

- UI implementation types are internal, but event ordering and public
  extension descriptors/editors are observable.
- Property names, descriptor/editor IDs, capability flags, and enum values are
  compatibility identities, not visual labels to rename.
- Runtime `Shader.PropertyToID` caching is bounded to 512 and internal; all API,
  metadata, persistence, and diagnostic property identities remain strings.
- The view must route writes through existing edit services/repositories.
  Opening, binding, scrolling, mode switching, or visual-state application must
  not create persisted edits.
- Maker uses character repositories. Studio can use character or scene
  repositories and supports items/projectors. The shared view must not assume
  every target belongs to a character root.
- Base UI compiles into AI, EC, HS2, KK, KKS, and PH; compile evidence is not
  runtime evidence for all targets.

## Readiness decision

The code is **architecturally better prepared but not yet approved for visual
redesign**.

Automated preparation completed:

- semantic presentation remains separate from runtime composition;
- final rows remain virtualized and bounded;
- close/unbind/listener and destroyed-target lifetime paths are explicit;
- selective condition and frame-scoped invalidation are implemented;
- property-ID lookup is bounded without changing public identities;
- Enum/Dropdown/Toggle rebind allocation/lifetime is bounded and guarded;
- search/provider/texture ownership and disabled instrumentation hot paths have
  deterministic evidence;
- final clean API/analyzer, metadata, 21-invariant/23-scenario harness, and
  AI/EC/HS2/KK/KKS/PH builds pass;
- final DLL/ZIP contents and hashes are recorded, and actual deployment passed
  installed DLL/native/config/duplicate checks;
- minimal hidden Koikatu/CharaStudio plugin-load smoke passed with Material
  Editor never opened and no screenshots taken.

Pending gates before visual work:

- user validation of existing layout, numeric entry, Dropdown font/popup,
  Toggle, search, conditions, Mixed, focus, and row recycling in Maker/Studio;
- card, coordinate, partial load, Studio scene, and scene-import persistence;
- in-game target/window lifecycle and Unity native/GPU retained-memory checks;
- representative layout/hierarchy captures only when the separate visual phase
  explicitly authorizes visual testing.

Deferred until that separate request:

- new theme, colors, typography, icons, density, or layout;
- new navigation, controls, actions, presets, or favorites;
- authored prefab or asset-bundle migration;
- public theming API;
- screenshots, visual comparisons, and visual acceptance work.
