# Material Editor modernization roadmap

## Current checkpoint

The compatibility/optimization foundation and the ten-stage UI delivery are in
source at commit `8c36e8df`. The clean API, Metadata, synthetic harness and
AI/EC/HS2/KK/KKS/PH Release matrix pass. Binary audit, restore verification,
two-file deployment, installed-byte verification and editor-closed Maker and
Studio load smokes also pass for the final build. In-game visual acceptance,
interaction with Material Editor open, persistence roundtrips and Unity runtime
profiling remain **PENDING**; the load smoke is not visual acceptance.

The UI is one internal fixed dark presentation. It preserves the plugin GUID,
schema/persistence formats and public API; it does not create a new theme API or
material storage route.

## Completed UI source phases

| Phase | Commit | Source delivery | Acceptance status |
| --- | --- | --- | --- |
| 1 - baseline and design | `c51828c7` | Frozen pre-redesign inventory/baseline, preconditions and minimal three-panel design | Source baseline complete; no rendered acceptance claimed |
| 2 - theme tokens and common chrome | `50c67539` | Fixed dark palette, metrics, typography, semantic states and common control styling | Automated token/contracts pass; visual review pending |
| 3 - top bar | `25d1d8df` | Dedicated two-row top bar, shader context, Studio slot and one reusable global view menu | Structural contract passes; visual review pending |
| 4 - left navigation | `b273a12f` | Category panel/rail, active marker, full-name tooltips and reusable entries | Structural contract passes; visual review pending |
| 5 - right panels | `76421d11` | Renderers + Materials visible together by default, real totals/filters/selection, individual/global collapse and legacy-safe Rename | Structural contract passes; visual review pending |
| 6 - row actions | `90e2e539` | One leased shared row menu; existing Renderer and Material secondary callbacks consolidated | Structural/lifetime contract passes; runtime actions pending |
| 7 - property categories | `449b0140` | Neutral full-surface 22-unit header with one action and passive glyph/name | Structural contract passes; visual review pending |
| 8 - property rows | `d854aa90` | Fifteen Reset captions replaced by compact `↺`; explicit Float range policy; long-name tooltips and pooled layout restore | Structural/behavior contracts pass; visual review pending |
| 9 - truthful feedback | `e6f5ba72` | Exact central/right empty states; no fabricated warning/footer/global totals | Structural contract passes; visual review pending |
| 10 - responsive layout | `8c36e8df` | Real-Canvas bounds, responsive category collapse, aggregate drag clamp and bounded resizable VirtualList | Matrix/lifecycle, clean all-target and editor-closed load gates pass; visual/runtime resize pending |

Current structural totals include nine existing actions moved to menus: three
global view actions, two Renderer exports and four Material actions. There are
two reusable popup surfaces per window (global and row action), one shared
`TooltipPanel`, one inactive VirtualList template and at most 44 cached row
clones. These are source counts, not runtime memory measurements.

## Acceptance path for the current UI

1. Complete all 58 cases in `USER_VISUAL_REVIEW_CHECKLIST.md` in Maker and
   Studio with requested screenshots and logs.
2. Record failures in `KNOWN_UI_LIMITATIONS.md`; do not silently waive them.
3. Perform card/coordinate/scene roundtrips for view-only use and real edits.
4. Capture runtime open/idle/scroll/filter/resize/menu/lifecycle profiles as
   defined in `UI_PERFORMANCE_RESULTS.md`.
5. **COMPLETE for `8c36e8df`:** run the clean build, API, Metadata, harness,
   all-target, binary-audit, restore and two-file deployment workflow.
6. **COMPLETE for `8c36e8df`:** verify one loadable Material Editor DLL,
   installed/build byte identity and editor-closed Maker/Studio plugin loading.

Items 1-4 remain required. Completing items 5-6 does not approve the visual
design or establish runtime performance.

## Post-redesign functional candidates

These are future features, not claims about phases 1-10 above.

### Focused editing workflows

- Modified Only filter using trustworthy repository state.
- Typed per-property Copy/Paste.
- Category Copy/Paste/Reset with compatibility summaries.
- Reset all modified values without silently switching shader.
- Category modified counts/actions only after a real aggregate backend exists.
- Registered Direction Pad and Min/Max composite editors.

Before implementation, define clipboard compatibility, texture inclusion,
transaction/Undo behavior and failure reporting. Reuse the existing copy/edit
service and persistence routes; do not create parallel material storage.

### Organization and reusable profiles

- Favorites and property pinning.
- Exportable, versioned per-shader presets with explicit affected properties
  and texture policy.
- Render-state and stencil presets.
- Compare two compatible materials.
- Per-shader custom layouts and localization.

Presets must never switch shader or overwrite incompatible properties silently.

### Authoring and batch tools

- Apply a category to selected compatible materials.
- Multi-material batch editing with real mixed-value aggregation.
- Shader-author metadata validator and explicit draft generator.
- Texture channel preview and R/G/B/A preview.
- Gradient/curve editors only where runtime semantics exist.

Automatic classification from names/prefixes remains prohibited; shader
authors own Basic/Advanced and condition metadata.

## Deferred integration work

### Timeline and Vector

Existing Timeline paths remain. A native Vector interpolation contract requires
a stable Timeline API, component interpolation/reset semantics and verified
Maker/Studio availability. Vector must not be routed through Color simply to
obtain a Timeline UI.

### Modified-state accuracy

The Advanced count is conservative and omits custom editors/types without a
trustworthy override-state source. A future semantic callback must be read-only,
explicit and never run expensive provider work per frame.

### Undo

No parallel Undo stack is planned. Future grouped operations must use a real
host/upstream route or document that Undo is unavailable.

## Runtime non-goals

The runtime plugin will not hide UnityEditor/source-asset tooling:

- `ShaderUtil` or `AssetDatabase` operations;
- asset reimport/import-setting changes;
- shader compilation, stripping, variant generation or conversion;
- texture baking or writes to original source assets;
- VRChat avatar/build tooling.

Any authoring tool would be a separate development-time project with its own
safety and licensing review.

## Invariants for every future change

- Preserve `com.deathweasel.bepinex.materialeditor`, persistence keys,
  schema-1 manifests and shipped public API.
- Route material edits through the existing repository/edit service.
- Keep view state out of cards, coordinates, scenes and material overrides.
- Preserve filter-before-row-creation and bounded VirtualList pooling.
- No Harmony/reflection/GameObject-name hacks against Material Editor's UI.
- No per-frame XML parsing, global scans, rebuilds, reflection or logs.
- No KKLT-specific property/category names in `MaterialEditor.Base`.
- No warning/count/action without a truthful backend.
- Never install the fork beside the official Material Editor DLL.

## Licensing and attribution

This work remains GPL-3.0 and retains upstream IllusionMods/KK_Plugins license,
copyright, author credits and history. NightOwlZzz / Owl receives additional
modification credit without replacing upstream authors. Distribution must
include corresponding source, identify the fork/modification and disclose that
it cannot be installed beside the official Material Editor DLL.
