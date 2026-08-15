# Material Editor current-state audit

> **Historical snapshot of the pre-hierarchy optimization branch.** It is kept
> as evidence for that checkpoint, not as a description of the current source.
> In particular, Basic/Advanced and `UiLevel` no longer exist.

## Scope and provenance

- Functional checkpoint: `3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`.
- Upstream/base commit: `1502cced6e372c6c328703eb7b7caea589bff4a7`.
- Optimization branch: `feature/material-editor-critical-optimization`.
- Safety branch: `backup/material-editor-working-before-optimization-20260808-075506`.
- Delta from the upstream/base commit: 80 files, 17,655 insertions and 521
  deletions after including the formerly untracked delivery files.

The workspace implementation, not earlier prompts, was inspected. Full file
lists and hashes are in `BACKUP_SOURCE_INFO.md` and `PHASE1_DELIVERY.md`.

## Functional matrix

Risk columns are qualitative before optimization: L = low, M = medium,
H = high. CPU/Alloc describe rebuild cost; Memory describes retained objects;
Compat describes regression impact if changed incorrectly.

| Function | State and principal code | Automated evidence | CPU / Alloc / Memory / Compat |
| --- | --- | --- | --- |
| Basic / Advanced | Exists. `UI.WindowView.cs`, `UI.cs:ChangeUiMode`, schema/policy in `UI.MaterialPropertyMetadata.cs`, filtering in `UI.MaterialSectionPresenter.cs`. Advanced definitions are filtered before RowModel creation in Basic. | PhaseOne #2-11/#33 and VirtualList tests. | H / H / M / L |
| `UiLevel` | Exists in schema 2 and descriptor API; schema 1 defaults compatibly to Basic. | PhaseOne #2-5/#28. | L / L / L / L |
| `DisplayName` | Exists with manifest then tooltip-catalog then internal-name fallback; `PropertyName` remains stable. | Metadata Program tests and PhaseOne #12. | L / M / L / L |
| `Order` / `CategoryOrder` | Exists. Manifest ordering is cached by `PropertyOrganizer`; extension descriptors are grouped/sorted on each rebuild. | PhaseOne #13. | M / H / M / L |
| Enum / Dropdown | Exists, including schema-2 `Type` aliases, unknown/Mixed presentation and exact Float values. | Alias tests and PhaseOne #14-16. | M / H / M / L |
| Vector2/3/4 | Exists in descriptor/factory/model/binder/repository and native persistence. | PhaseOne #17-20 plus persistence source guards. | M / M / M / H |
| Float-backed Toggle | Exists with Off/On values and remains distinct from Keyword. | Alias tests and PhaseOne #21/#22. | L / M / L / L |
| `ShowIf` | Exists for one parsed numeric condition. Source edits currently request a full presentation refresh; no per-source graph/cache. | PhaseOne #23/#24/#27. | H / M / L / M |
| Collapsible categories | Exists with per-session state, all-collapse and navigator. | PhaseOne #32 covers state; viewport tests cover atomic presentation replacement. | H / H / M / L |
| Category main toggle | Does not exist. `Group` metadata is parsed/exposed but no category toggle control consumes it. | None. | N/A; out of scope |
| Advanced search-result count | Exists in Basic and switches to Advanced while preserving the filter. | PhaseOne #9/#10. | H / H / L / L |
| Advanced modified count | Partially exists: reliable built-in values are counted; opaque custom editors are deliberately not guessed. | PhaseOne #11. | H / M / L / M |
| Modified Only | Does not exist. | None. | N/A; out of scope |
| Extended tooltips | Exists for shader/category/property catalogs, inline metadata and Shift shader hints. | Metadata tests and tooltip-policy tests. | L-M / M / M / L |
| Descriptor providers | Exists with priority, registration disposal, exception isolation and custom editor factories. | API capability/build tests; lifecycle/provider stress coverage was absent at baseline. | H / H / H / M |
| New RowModels | Enum, Vector and FloatToggle exist; common models carry `Enabled`, public descriptor and refresh callback. | Semantic/source guards. | M / H / H / L |
| RowView | One generic pooled view; it restores layout/numeric configuration for every clone. | Layout guards and VirtualList tests. | M / M / M / L |
| RowBinders | Family binders exist, including Enum/Vector/Toggle. `ListenerScope` removes only listeners it registered. | Semantic tests; no baseline 500-cycle listener test. | H / H / H / L |
| VirtualList | Fixed initial pool, recycled rows, viewport identity/bookmark restoration and listener suspension. | `VirtualListScrollContextTests`. | M / H / H / M |
| Manifest schema | Schema 2 exists and schema 1 remains accepted. Type aliases are schema-2 only. | Metadata, PhaseOne and alias suites. | Load-only / M / L / H |
| Capabilities | Extension API 1.2 adds UI levels, Enum, Vector, conditions and Float Toggle flags. | PhaseOne #29 and PublicApiAnalyzers. | L / L / L / H |

## Existing cache and invalidation architecture

Already cached or event driven:

- XML and shader declarations load at startup/manifest discovery rather than per
  frame.
- `ShaderUiMetadataRegistry` stores tooltip/display metadata by shader.
- `PropertyOrganizer.PropertyOrganization` stores static non-Hidden ordering by
  shader and refreshes only when manifests or sorting config change.
- VirtualList owns a fixed initial RowView pool and rebinds it on scroll.
- Texture containers use content hashes/refcounts for persisted texture data.
- ShowIf and Mixed are evaluated during presentation construction or
  explicit bind/refresh, not by a per-frame global scan.

Missing or partial at the checkpoint:

- No invalidation coordinator or dirty-level model.
- No provider registry generation/sorted snapshot.
- No cached shader property IDs.
- Search reconstructs wildcard regex input for every comparison.
- No precomputed condition dependency plan or per-build source-value cache.
- No normalized dropdown UI-option cache.
- Closing the window deactivates it but leaves presentation/pooled bindings and
  selection rows referencing the last scene target.

## Maker, Studio and persistence

- Shared Base/Core code supplies the UI and edit semantics to every game target.
- Maker routes edits through `CharaMaterialEditRepository`.
- Studio routes character `ObjectData` through the character repository and
  item IDs through `SceneMaterialEditRepository`.
- Native Vector data is represented in copy/paste, character card, coordinate,
  partial coordinate, Studio scene and import/remapping lists. Legacy Color data
  is migrated only when the active manifest classifies the property as Vector.
- The compatibility texture handler writes bundled version-1 data and reads
  bundled, deduplicated and local version-2 data. This must not be reformatted
  by the optimization phase.
- Clean baseline builds passed for AI, EC, HS2, KK, KKS and PH, so shared changes
  have a concrete multi-target compile gate.

Persistence risk is high even when code appears redundant. Optimizations must
not change keys, ordering semantics, version values, partial-load filters,
scene ID remapping or texture byte ownership.

## Public API delta

The checkpoint's additive public surface is recorded in
`src/MaterialEditor.API/PublicAPI.Unshipped.txt` and includes:

- UI mode/level and condition contracts.
- Enum options/editors, Vector editors and Float Toggle editors.
- Descriptor metadata and capability flags.
- `ShaderPropertyType.Vector = 4`.
- Vector edit-service/MaterialAPI/copy operations.
- `MaterialEditorLabelType.VectorProperty = 9`.

No optimization may remove or renumber these symbols. The exact shipped and
unshipped baseline hashes are in `FUNCTIONAL_BASELINE.md`.

## Principal measured candidates

1. Search/filter pattern construction and synchronous per-keystroke full builds.
2. Repeated provider sorting/materialization when no provider or registry change
   occurred.
3. Rebuilding manifest condition-kind/dependency collections for each material
   and repeatedly reading the same source value in one build.
4. Duplicate renderer/projector list materialization in `PopulateList` and
   `BuildRows`.
5. Per-frame VirtualList calculations while open and completely idle.
6. ListenerScope and Enum index-list allocation on every pooled-row bind.
7. Strong target references retained by presentation, pooled rows and selection
   list closures after the window is closed.
8. String interpolation and Unity string-property lookup for every material read.
9. Per-character pending-texture try/finally work when no file is queued.
10. Normal-map weak-cache sweeping on every plugin frame.

These are candidates, not claimed improvements. Each retained change requires a
paired count/allocation result in `OPTIMIZATION_DECISIONS.md`.
