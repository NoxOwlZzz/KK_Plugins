# Material Editor final runtime test checklist

## Purpose and status

Use this checklist only with the frozen final optimized binary. Every runtime
item is **PENDING** until it is actually performed in the named runtime.
Production-linked/source tests and the synthetic harness cover the selective
condition graph, property-ID cache, target cleanup, texture ownership,
Enum/Dropdown/Boolean reuse, and disabled instrumentation fast path, but they do
not satisfy any Unity UI, persistence, visual, or native-memory check below.

The clean build, analyzer, metadata, all-target compile, harness, package, safe
deployment, installed-file/hash, and permitted plugin-load smoke gates have
passed. Material Editor itself was never opened during the smoke.

An installed rollback anchor already exists at
`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260808-0800-before-critical-optimization`.
Its working Material Editor DLL SHA-256 is
`11CD1B91661E44B3620DA0FFE2D6FE20C2998F31791F6368A24E6E8F6CEEA550`;
its `libwebp` SHA-256 is
`8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`.
This does not replace backups of the cards, coordinates, scenes, and current
configuration used for the runtime test.

Record before starting:

| Field | Value |
| --- | --- |
| Test date | 2026-08-08 plugin-load smoke; manual test **PENDING** |
| Tester | Automated hidden smoke; manual tester **PENDING** |
| Commit | Optimized code `0ea899fd`; restore/script HEAD `189562ee` |
| DLL path | `D:\Games\Koikatsu\BepInEx\plugins\KK_Plugins\KK_MaterialEditor.dll` |
| DLL SHA-256 | `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26` |
| Game / build | Koikatu + CharaStudio plugin-load smoke **PASS**; interactive/manual session **PENDING** |
| BepInEx / KKAPI / Extended Save versions | BepInEx 5.4.23.2 / KKAPI 1.42.2 / ExtendedSave 20 / Sideloader 20 |
| Test card / coordinate / scene backup paths | **PENDING** |
| Log path | `D:\Games\Koikatsu\BepInEx\LogOutput.log` (CharaStudio smoke); Koikatu log was inspected before the next launch replaced it |

The artifact and deployment fields above refer to the final deployment rebuild,
not the superseded pre-deployment artifact. The smoke record proves plugin load
only; manual interaction/UI/persistence fields remain **PENDING**.

## Safety and deployment preflight

- [x] **PASS** Functional plugin rollback anchor and read-only config snapshot
  were prevalidated before deployment.
- [ ] **PENDING** Separately back up the cards, coordinates, and Studio scenes
  selected for the manual persistence test.
- [x] **PASS** Final deployment rebuild completed with the safe clean script;
  API/analyzers, metadata, harness, and all six targets passed.
- [x] **PASS** Deployment preflight confirmed the exact game root, plugin
  destination, source artifact, and backup destination.
- [x] **PASS** Exactly one loadable Material Editor DLL remains. Do not add the
  official DLL beside this fork.
- [x] **PASS** Only `KK_MaterialEditor.dll` and `libwebp.lib` were copied; no
  shared game/BepInEx/KKAPI dependency was copied or overwritten.
- [x] **PASS** Restore `--verify-only` identified and validated the intended
  functional backup without changing live files.
- [x] **PASS** Installed DLL equals the final build hash; installed
  `libwebp.lib` remains
  `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`.
- [x] **PASS** Deployment copied no configuration. During plugin-load BepInEx
  added exactly four diagnostic entries: diagnostics/counters/summary are
  `false` and threshold is `5`; all 42 pre-existing values stayed identical.
  Final config SHA-256 is
  `D3F46013C007B0F6BFB18BA591E48CA90354620F04A9BD335FC25BBF3C98FFC7`;
  the read-only safety snapshot remains `CCE2D1A2...C11`.
- [x] **PASS** Final Cecil re-audit used the exact byte-identical
  build/installed DLL: 16/16 assembly, 428/428 type, 2,275/2,275 member
  references and all release/audit guards passed with 0 failures.

## Minimal plugin-load smoke

Completed with Material Editor left closed:

- [x] **PASS** Hidden Koikatu process loaded `Material Editor 4.0.3` and
  `Material Editor Maker 4.0.3`; Chainloader startup completed once and the
  captured log contained 0 Material Editor-attributed errors.
- [x] **PASS** Hidden CharaStudio process loaded `Material Editor 4.0.3` and
  `Material Editor Studio 4.0.3`; Chainloader startup completed once and the
  captured log contained 0 Material Editor-attributed errors.
- [x] **PASS** Both smoke processes were closed after log inspection.

This smoke did not prove reaching an interactive Maker character or Studio
scene. Material Editor was never opened; no screenshot, UI interaction, visual
check, edit, save, or load was performed.

## Character Maker UI and editing

- [ ] **PENDING** Open Material Editor and compare placement, row height,
  labels, colors, category order, and visible property set with the functional
  checkpoint.
- [ ] **PENDING** Confirm Float and numeric Vector components accept typed
  values, decimal editing, negative values where valid, and commit on the same
  interaction as before.
- [ ] **PENDING** Confirm Float-backed `Boolean` rows display and change their
  fixed 0/1 values correctly; the removed development `Type="Toggle"` spelling
  is not used.
- [ ] **PENDING** Confirm `Dropdown`/`Enum` rows show every option, readable font
  size, correct current value, and correct menu selection.
- [ ] **PENDING** Confirm Color, Texture, Texture offset/scale, Keyword, Vector,
  Boolean, Dropdown/Enum, renderer, material, and projector rows still expose
  the expected controls.
- [ ] **PENDING** Compare a flat legacy manifest with Category-only and
  Subcategory examples. Verify that each feature appears only when declared,
  all properties remain discoverable, and hierarchy collapse preserves the
  scroll anchor without writing a material value.
- [ ] **PENDING** Verify ordinary Boolean/Keyword rows inside a Subcategory
  preserve Mixed/modified/Timeline/Reset state across fold/unfold and recycled
  row binding.
- [ ] **PENDING** Exercise search with plain text, commas, `_Property` terms,
  `*`, `?`, regex punctuation as literals, mixed case, spaces, and Unicode/IME
  input. Compare results and immediate update behavior with the checkpoint.
- [ ] **PENDING** Exercise `ShowIf` source changes. Verify hidden rows
  appear/disappear without stale state.
- [ ] **PENDING** Verify property copy/paste/reset, renderer/material reset,
  category operations that already exist, and texture import/export.
- [ ] **PENDING** Verify no control from a recycled row leaks value, Mixed state,
  options, focus, tooltip, or callback into the next row.

## Multi-target and selection behavior

- [ ] **PENDING** Test 1, 5, 20, and a large practical renderer/material
  selection for equal values.
- [ ] **PENDING** Test Mixed values, absent properties, and different shader
  variants. Verify display and edit fan-out match the checkpoint.
- [ ] **PENDING** Verify renderer and material selection order remains stable.
- [ ] **PENDING** Close the window, destroy/replace or switch an underlying
  renderer/material through a normal game operation, then reopen. Verify stale
  selections are gone, valid selections keep their order, and no missing-object
  exception occurs.
- [ ] **PENDING** Switch to another character/coordinate/object and verify no row
  or callback still edits the previous target.

## Close/reopen, Rename, and lifecycle

- [ ] **PENDING** Close and reopen the same target with a non-empty filter,
  collapsed Categories/Subcategories, selection, and non-zero scroll. Verify
  the retained interaction state is restored without adding material data.
- [ ] **PENDING** Close while the Rename panel is hidden. Reopen it and verify it
  is rebuilt for the current material.
- [ ] **PENDING** Close while the Rename panel is visible. Reopen and finish or
  cancel the rename; verify the same material/renderer context is used and no
  stale target is renamed.
- [ ] **PENDING** Repeat 100 close/open cycles and 100 target changes. Record
  exceptions, listener duplication, row-pool growth, managed retained objects,
  and native/GPU memory if profiler access is available.
- [ ] **PENDING** Leave the editor open and unchanged for several minutes, then
  closed for several minutes. Compare CPU/GC activity and confirm no recurring
  heavy work or monotonic memory growth.

The optimized close path intentionally retains the session's current
`GameObject`, data context, valid renderer/material selections, and a visible
Rename context for reopen fidelity. The profiler check should distinguish this
bounded intentional retention from growth across different targets/cycles.

## Texture and watcher behavior

- [ ] **PENDING** Import a texture, edit it externally with texture watching
  enabled, and verify one correct refresh with no duplicate callbacks.
- [ ] **PENDING** Delete or invalidate the watched file and verify the watcher
  disposes safely; repeat import afterward to prove it is not stuck.
- [ ] **PENDING** Exercise clothes/body main-texture refresh bursts and verify
  the final value is correct after end-of-frame coalescing.
- [ ] **PENDING** Verify a scheduling failure or target transition does not
  permanently suppress later texture refreshes.
- [ ] **PENDING** Load duplicate-content textures, clear/reload, and confirm one
  owner's release does not destroy a texture still used by another owner.
- [ ] **PENDING** If a Unity memory profiler is available, verify abandoned
  Material Editor-owned textures are released and game-owned textures are not
  destroyed.
- [ ] **PENDING** Import the largest Cubemap accepted by the conversion budget
  in Maker and Studio. Confirm the projection progresses over frames instead of
  freezing one frame, and record decode/sampling timings plus peak managed,
  native, and GPU memory.
- [ ] **PENDING** Export a direction-labelled, color-coded Cubemap through both
  the CPU-readable and non-readable GPU paths, compare all face orientations
  and colors, then re-import both panoramas. Repeat on every supported target;
  `RenderTextureReadWrite.Default` remains an explicit runtime validation gate.
- [ ] **PENDING** Duplicate/reload an object after deliberately changing
  renderer/material traversal order while preserving the count. Reset must use
  renderer path/component/material-slot identity or reject ambiguity, never map
  originals by traversal index.
- [ ] **PENDING** Import an invalid Texture2D followed by a valid one in Maker
  and Studio. The row must refresh only after the deferred controller result;
  invalid input must not optimistically report `Exists` or success.

## Character persistence

Use copies of data for every destructive or resave test.

- [ ] **PENDING** Save a character card with Float, Color, Texture, Keyword,
  Vector, Toggle, and Dropdown/Enum edits; reload and compare every value.
- [ ] **PENDING** Save/reload a coordinate and verify clothing/accessory material
  edits and textures.
- [ ] **PENDING** Perform partial-coordinate load and verify included/excluded
  categories preserve the established behavior.
- [ ] **PENDING** Open and close Material Editor without editing, save, and prove
  the file is not marked/changed merely because the UI was opened.
- [ ] **PENDING** Load supported bundled and readable version-2 texture inputs;
  verify byte/content compatibility.

## CharaStudio editing and persistence

- [ ] **PENDING** Edit a character, item, and projector; verify row types,
  selection, interpolation actions, copy/paste/reset, and search.
- [ ] **PENDING** Save and reload the scene; compare all material/projector edits
  and textures.
- [ ] **PENDING** Import the scene into another scene and verify ID remapping,
  target ownership, and texture availability.
- [ ] **PENDING** Clear/load scenes repeatedly and check for stale callbacks,
  disposed-live textures, retained old scene targets, and log exceptions.
- [ ] **PENDING** Verify Timeline/animation behavior for existing supported
  properties is unchanged, including validation of malformed definitions.

## Public API and third-party compatibility

- [ ] **PENDING** Load representative existing 1.1-style consumers and confirm
  they initialize without missing API/member errors.
- [ ] **PENDING** Load a 1.2 capability-aware consumer using Toggle, Dropdown,
  Enum, or Vector descriptors.
- [ ] **PENDING** Exercise a provider with priorities, dynamic target-specific
  output, explicit disposal, and an intentional exception. Verify ordering,
  isolation, and later recovery.
- [ ] **PENDING** Verify MaterialAPI-dependent plugins do not observe changed
  property names, persistence identities, or event order.

## Final acceptance record

- [ ] **PENDING** No new exception or error attributable to Material Editor.
- [x] **PASS (plugin-load scope only)** Smoke logs contained 0 Material
  Editor-attributed errors in Koikatu and CharaStudio.
- [ ] **PENDING** No visible or interaction regression against the functional
  checkpoint.
- [ ] **PENDING** Card, coordinate, partial load, scene save/load, and scene
  import tests pass.
- [ ] **PENDING** No monotonic listener, row-model, selection-closure, provider,
  watcher, or owned-texture growth in repeated lifecycle tests.
- [x] **PASS** Final API analyzer, metadata, harness, KK, and all-target build
  gates passed from the exact deployed-build source.
- [x] **PASS** Final DLL/ZIP/JSON hashes and package contents are recorded in
  `REGRESSION_TESTS.md` and `PERFORMANCE_RESULTS.md`.
- [x] **PASS** Maker/Studio plugin-load smoke evidence is recorded in the release
  documents.
- [ ] **PENDING** Attach manual UI/persistence/visual/runtime evidence to the
  final acceptance record.

If any item fails, stop deployment, preserve the failing save/log, record exact
reproduction steps, and restore the backup before continuing.
