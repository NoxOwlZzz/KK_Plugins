# Known Material Editor UI limitations

## Status

These limitations apply to the final UI-fix candidate compiled and deployed
from source commit `8bcf8649`.
`CURRENT_BUILD_AUDIT.md` records the exact final artifact hashes. “PENDING”
means source or automated coverage exists but the behavior has not been
accepted through the required Maker/Studio visual, interactive or
runtime-profile evidence.

## Current UI-fix evidence boundary

- The complete final build finished with exit code 0. Metadata/regression,
  API, harness and Release targets AI, EC, HS2, KK, KKS and PH are **PASS**.
- The final 59,092-byte schema-2 performance JSON has SHA-256
  `8986ADAA352AA00D342641A7DE7E24471CA218EDDDCC67A482D84BB2792703CC`
  and records 45/45 passing results and invariants. It is a .NET 8
  operation-count model, not Unity UI.
- Final deployment is **PASS**. The build and installed DLL are byte-identical
  at SHA-256
  `72EA76AC8AB31BAE28E67FA53AED658A28E585BA78B85CCF08DC80F9B8405F95`.
- Backup payload/manifest creation is **PASS**. A separate restore
  `--verify-only` was not rerun for the new backup, so that check remains
  **PENDING** for this exact backup.
- Deployment did not copy configuration or data. The observed post-deploy
  config hash is recorded in `CURRENT_BUILD_AUDIT.md`; no unrecorded
  cross-candidate byte comparison is claimed.
- Maker and CharaStudio load smokes for this exact DLL are **PENDING**. The
  earlier editor-closed CharaStudio smoke belongs to the previous DLL now in
  the backup and is not evidence for this candidate.
- All 29 cases in `USER_UI_FIXES_TEST_CHECKLIST.md` are **PENDING** and were not
  executed by Codex.
- No screenshots were generated in this correction phase.

## Validation limitations

- The corrected UI has not received the 29-case user checklist or the earlier
  exhaustive 58-case in-game visual review.
- No post-redesign card, coordinate or Studio-scene persistence roundtrip has
  been recorded.
- No post-redesign FPS, frame-time, managed/native/GPU memory or long-session
  profile exists.
- The clean Release matrix passes for AI, EC, HS2, KK, KKS and PH, but a build
  proves compilation rather than rendering or interaction in those targets.
- No exact-candidate Maker or CharaStudio load smoke was recorded. Controls,
  listeners, color picker and responsive layout therefore remain unexercised
  in runtime evidence for this candidate.

## Theme and typography

- There is one fixed dark theme. No theme picker, light variant, custom palette,
  runtime skin, high-contrast variant or public theme API exists.
- The font remains built-in Arial. There is no localization or user font-size
  setting beyond the existing global UI scale.
- Tooltips use size 11 and are capped at 280 by 360 units; very long text is
  truncated.
- Generic input best-fit may shrink to the source floor of 2. Property labels,
  dropdowns and Vector components have a 12-unit floor, but numeric extremes
  still require visual review.
- Glyphs are text characters rather than authored icons; final font rendering
  and alignment remain **PENDING**.

## Layout and input

- Rows are fixed at 22 units. Multiline labels, wrapped Vector controls and
  variable-height custom editors are not supported by the current VirtualList
  math.
- Compact buttons are 20 units, appropriate to the dense desktop inspector but
  not a general touch-target design.
- The responsive center targets 500 by 138 and reserves the expanded side
  footprints even when a manual collapse shows only a 24-unit rail. Manual
  Categories or Renderers/Materials collapse therefore does not move or resize
  the center. On a Canvas physically too small for all minimums, every region
  cannot remain fully usable simultaneously.
- When space is insufficient, the category panel is visually auto-collapsed.
  Its preference is preserved, but expansion is disabled until UI scale or the
  configured right-panel width is reduced. Collapsing the right sidebar does
  not release its reserved footprint.
- Right panel width is config-driven from 100 to 500. There is no drag splitter
  or saved per-panel width.
- Keyboard/tab order and assistive-technology behavior are not comprehensively
  defined or tested. Some popup buttons intentionally use `Navigation.None`.

## Feedback deliberately not implemented

- No global footer/status bar exists. There is no truthful global
  visible/available/modified aggregate backend.
- No category modified count, warning badge, category action menu or master
  toggle exists.
- No generic warning scanner parses names/logs/materials to manufacture UI
  warnings. Only real model/presentation state may drive feedback.
- Central empty feedback says `No matches` or `No rows in this view`; it does
  not claim “No properties” because presentation count includes structural
  rows.
- Right empty feedback distinguishes no renderer/material entries from no
  filter matches using exact total and visible counts.

## Rows and actions

- Texture rows do not show thumbnails or texture names because the row model
  does not provide a safe presentation source for them.
- A Float slider appears only when both finite bounds form a usable increasing
  range. An unbounded Float intentionally has numeric input and label drag, not
  an invented 0..1 slider.
- The shared row menu exposes at most four existing context actions. It does
  not add Undo, Technical Info, category Copy/Paste, Reset Material, Paste
  Material or placeholder actions.
- Renderer Timeline/name and Material fold/name remain direct. Shader,
  RenderQueue, Recalculate Normals and property controls remain direct.
- Paste availability is based on the existing in-memory copy container. No
  cross-version clipboard schema was added.
- Enum lists above 128 options remain complete; retained buffers may compact
  after shrinking, but long lists still require practical usability review.
- The existing color palette adapter remains external to the common theme.
  Reopen/focus behavior is covered structurally but requires final in-game
  review.

## State and persistence

- Panel, selection, mode, filter and collapse state are presentation/session
  concerns. They are not material/card/coordinate/scene data.
- Global Renderers/Materials visibility defaults true for a fresh session and
  is retained in `MaterialEditorSessionState`; individual right-panel fold
  state is view-local.
- Forced responsive category collapse is visual only and does not mutate the
  logical category preference.
- No new UI configuration entry was added for theme, layout mode, collapse or
  responsive behavior.

## UI-fix-specific limits

### Categories

- Stable category identity is session/presentation identity; it is not a new
  persistence key or public API. Repeated occurrences with the same logical
  category name inside one material section intentionally share a target and
  retain multiple row anchors.
- Programmatic navigation can preserve the exact category identity at the end
  of the list, but the physical ScrollRect position is still clamped; the final
  header is not guaranteed to sit at the top edge.
- Hit routing, Canvas scaling and dense-row geometry are covered only by source
  contracts/stubs. Real pointer behavior remains **PENDING**.
- Category diagnostics require the existing `PerformanceDiagnostics` switch.
  They are disabled by default and are not a permanent user-facing log.

### Copy/Paste

- `Copy Edits` and `Paste Edits` use compact fixed widths. Both remain in the
  material row, but an extremely narrow Canvas may still crowd the row; no
  second responsive action layout was added.
- Paste compatibility is derived from the current in-memory clipboard and
  target properties. There is no serialized, cross-process or cross-version
  clipboard format.
- To detect third-party direct mutations of legacy public CopyData lists, one
  Canvas-local snapshot scans their semantic contents every active frame. It is
  allocation-free by design but is not zero CPU work and is O(clipboard edits).

### Dropdowns

- Shader options are a snapshot created with each pooled Shader RowView. A real
  shader-catalog change requires row/view context reconstruction; opening the
  same popup does not poll the catalog.
- Persistent dropdown filters retain at most 16 process-local keywords. They
  are not saved as material/card data.
- Enum caches retain at most 128 reusable OptionData shells per row. Active
  lists above 128 remain complete, so a very large open popup can still create
  many uGUI item clones.
- The synthetic open/close scenario retains the same modeled P95 allocation
  (40,920 B) on both paths. Listener balance does not mean popup creation is
  allocation-free.

### Renderer and sidebar

- Renderer child RowModels are lazy. Once first expanded, they remain cached
  inside the current presentation while closed; they are not visible/bound and
  disappear when the presentation is released.
- Collapsed renderer state retains only 512 session keys and evicts oldest
  collapsed entries after the cap. This state clears on target invalidation.
- Horizontal sidebar collapse uses SetActive on the existing panels so their
  selection/filter/scroll objects survive structurally. The preservation result
  has not been accepted in a real Unity interaction session.
- Individual Renderer/Material vertical folds are view-local, not saved user
  preferences. There is no drag splitter.

### Performance and lifetime

- The virtual RowView pool and category entry list are high-water pools. Hidden
  instances neutralize models/listeners but remain allocated for reuse.
- Some UI listeners are intentionally permanent for the lifetime of their
  owned Canvas/entry/menu. The requirement is one listener with a replaceable
  semantic target, not destruction after every click.
- The schema-2 harness models operation counts and uses selected production
  helpers under .NET 8. It does not instantiate the production WindowView,
  render frames or measure UnityEvent/native/GPU retained memory.
- No claim of zero allocations, zero CPU, an FPS percentage, or runtime
  leak-freedom is made until the profiler checklist is complete.

## Deferred roadmap items

Presets, favorites, batch editing, true mixed-value aggregation across batch
targets, category operations/counts, texture previews, localization, Timeline
Vector interpolation and runtime author tooling remain outside this UI delivery.
See `ROADMAP.md` for the non-UI feature track.
