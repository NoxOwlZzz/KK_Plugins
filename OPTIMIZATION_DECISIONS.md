# Material Editor optimization decisions

## Scope

This log describes the frozen optimized code at `0ea899fd`, built from repository
HEAD `189562ee` after the reproducible restore/script follow-up. It derives from
the user-confirmed functional checkpoint
`3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`. The governing rule remains
behavioral equivalence: no visual redesign, persistence-format change, public
API break, dependency upgrade, or unrelated feature expansion.

The final clean build, harness, artifact, package, deployment/hash, and permitted
plugin-load smoke gates are complete. Manual UI, persistence, visual, and
runtime-lifecycle validation remain **PENDING**.

## Implemented decisions

### Diagnostics remain off by default

- Production counters and timers are internal and opt-in.
- `TryStart` reads the mode once; when disabled it takes one branch and does not
  touch `Stopwatch`, `Interlocked`, profiler APIs, strings, or arrays.
- Disabled focal Mixed aggregation is measured at 0 B over 10,000 calls.
- Deterministic fingerprints and operation counts are gates. Wall-clock timing
  is supporting evidence only and is not used to claim a speedup from a noisy
  capture.

Reason: the optimization can be measured without adding normal runtime logging
or allocation cost.

### Search and providers cache only stable work

- Legacy comma/underscore tokenization and wildcard behavior are preserved.
- Each rebuild prepares one matcher per term and reuses it across renderer,
  material, property, and projector matching.
- Search refreshes use latest-wins, frame-scoped coalescing.
- Provider registrations have one priority/sequence-sorted snapshot per
  registry generation; registration and disposal invalidate it.
- Every provider is still called for every material in the same order, with
  exception isolation and atomic handling of a failed enumeration.
- Provider results, descriptors, callbacks, and target contexts are never put
  in a process-lifetime cache.

Reason: matcher plans and registration metadata are reusable; provider output
is dynamic and can retain scene targets.

### Conditions use per-build caching plus selective invalidation

- Each unique supported condition source is read once per material build.
- Failed reads are not cached, so a later evaluation can retry; malformed or
  unsupported conditions retain fail-open behavior.
- A dependency graph is allocated only for materials that have conditions.
- Condition edits are deduplicated into a frame-scoped batch.
- `ShowIf` changes are evaluated transactionally and cause at most one
  presentation rebuild for the batch.
- Graphs and source values are presentation-scoped; no static cache retains a
  `Material`.

Reason: this removes repeated reads and unnecessary rebuilding without making
condition state stale or weakening recovery behavior.

### Runtime shader property IDs are bounded and internal

- Material API calls use a bounded FIFO cache of at most 512 runtime handles.
- A cache hit creates no prefixed string and does not call
  `Shader.PropertyToID`; the `PropertyToIdCalls` metric counts only misses that
  resolve an ID.
- Resolution occurs before eviction so a failed Unity call cannot corrupt the
  cache.
- Property-name strings remain the identity at all metadata, API, persistence,
  and diagnostic boundaries.
- The cache follows Unity's main-thread material-access ownership and is not a
  general concurrent dictionary.

Reason: native lookup work is avoidable, but changing public/string identities
would be incompatible.

### UI work is released according to target lifetime

- Normal `Visible = false` releases transient presentation state, category
  navigation, row bindings/listeners, side-list entries, deferred population,
  and the texture watcher while retaining the bounded row-view hierarchy.
- A valid current target/data context, filter, mode, collapse state, selection
  order, scroll anchor, and an active Rename workflow are preserved for direct
  reopen.
- Destroyed or replaced targets take a stronger path: transient content,
  selections, callbacks, palette state, pending OBJ export, and strong target
  references are cleared.
- Maker exit and Studio clear/load perform total target invalidation.
- Character replacement and Studio object deletion use exact identity and an
  explicit ownership policy; a null/fake-null object is never interpreted as
  permission to clear unrelated state.
- Timeline entries for live targets are preserved across a normal switch;
  destroyed entries are pruned.
- Deferred destroyed-target pruning resets its scheduling gate and falls back
  to immediate pruning if coroutine scheduling returns null or throws.

Reason: direct reopen behavior is intentional, while dead/replaced scene
objects must not remain reachable through UI callbacks or session state.

### Texture ownership and idle work are explicit

- Abandoned `TextureContainer` owners are disposed during scene clear/load,
  character destruction, import completion, and exceptional partial-load paths.
- Shared content tokens keep reference counts, so releasing a temporary owner
  does not destroy an adopted destination texture.
- The numeric CRC64 path avoids the old temporary `byte[8]`; the public
  `CalculateCheckValue` byte-array contract is unchanged.
- Pending texture processing is skipped when no path is queued.
- Weak normal-map cleanup is bounded instead of scanned every frame.
- Clothes/body end-of-frame refresh bursts are coalesced, with gates released
  on completion, null coroutine return, or scheduler exception.

Reason: cleanup follows ownership, and idle paths return only when they have no
observable work.

### VirtualList and Enum/Dropdown/Toggle reuse is bounded

- `VirtualList` has an unchanged-frame fast path keyed by dirty state, scroll
  position, and viewport geometry.
- Presentation replacement reuses materialized renderer/projector snapshots and
  constructor-owned row collections where safe.
- Exact listener removal remains in place; `RemoveAllListeners` is not used for
  pooled rows.
- Each pooled Enum/Dropdown row reuses its option/value snapshot and retains at
  most 128 `Dropdown.OptionData` objects. An active list larger than 128 still
  displays in full; shrinking compacts retained buffers back to the cap.
- Mixed, Unknown, null-option, duplicate-value, and last-duplicate-selection
  semantics are preserved. A stable Unknown caption retains one value/string
  pair per row.
- Float-backed Toggle binding skips a programmatic `Set` when the desired state
  is already displayed.

Reason: the hot rebind path can be allocation-free after warm-up without an
unbounded option cache or event-semantic change.

## Instrumentation scope

Metric names are stable, but their scope is deliberately narrow:

| Metric | What it measures in production |
| --- | --- |
| `MixedCalculations` | One Vector-row Mixed-flag aggregation |
| `MixedComparisons` | Boolean component flags examined by that aggregation, including short-circuiting |
| `Save` / `Load` | `TextureSaveHandler` payload serialization/deserialization only |
| `CacheInvalidations` | `VirtualList.SetList` presentation replacement only |
| `PropertyToIdCalls` | Bounded property-ID cache misses/resolutions only |

The standalone harness also uses the Mixed metric names for synthetic
multi-target comparisons. External providers may compute their own Mixed state,
so these counters are not global totals for all Material Editor activity.

## Deliberately retained behavior

- Normal close keeps the valid target/data context needed for direct reopen.
- A visible Rename workflow keeps its live context until completion, target
  replacement/destruction, or total invalidation.
- The row-view hierarchy is pooled and bounded rather than destroyed on every
  close.
- The legacy bundled texture writer and readable version-2 inputs are unchanged.
- Provider `P * M` invocation, priority/order, exception isolation, and
  registration ownership are unchanged.
- Public property strings and PublicAPI files remain compatibility identities.

These are bounded compatibility choices, not claims that all Unity objects are
collectible immediately after an ordinary close.

## Rejected or deferred alternatives

| Alternative | Decision | Reason |
| --- | --- | --- |
| Global/unbounded regex cache | Rejected | User-entered keys could grow without bound |
| Global provider-result or target cache | Rejected | Results can contain dynamic callbacks and scene references |
| Condition values cached across presentations | Rejected | Risks stale values and retained `Material` objects |
| Destroy/recreate the whole UI pool on every close | Deferred | The current control pool is bounded; runtime profiling must justify a different policy |
| Clear a valid target on normal close | Rejected for this phase | Changes direct-reopen behavior |
| Guess a replacement Rename context | Rejected | Could apply an action to the wrong material/renderer set |
| `RemoveAllListeners` for pooled controls | Rejected | Could remove listeners owned by another component |
| Unbounded Enum option cache | Rejected | Large dynamic descriptor sets could become process-lifetime retention |
| Change texture format/deduplication policy | Rejected | Cross-version persistence boundary |
| Remove `CalculateCheckValue` allocation/API | Rejected | Observable byte-array return contract |
| Theme, layout, typography, icon, or prefab changes | Deferred | Belongs to the later visual-redesign phase |

## Compatibility and release status

- Frozen optimized code: `0ea899fd`.
- Reproducible restore/script HEAD used for the clean build: `189562ee`.
- `build-materialeditor-optimized.bat --clean`: **PASS**, exit code 0.
- API Release / PublicApiAnalyzers: **PASS**, 0 warnings / 0 errors.
- Metadata suite: **PASS**, 34 of 35 gates executed there; gate 30 is the
  separate analyzer build-time gate.
- AI, EC, HS2, KK, KKS, and PH Release matrix: **PASS**, 0 errors for every
  target.
- `PublicAPI.Shipped.txt` SHA-256:
  `F844B75DEB8025FEBEBF4E6FFC8700903FAAF4B04E0952221EB0D63031268367`.
- `PublicAPI.Unshipped.txt` SHA-256:
  `ED5FAEB493FE464CC96EE7CC81322C72575557A7EFCC73D0F050E04D621B4023`.
- KK 1.42.2 / Extended Save 20.0 compatibility cases are in the metadata suite.
- Final deployed-build harness JSON (generated
  `2026-08-08T17:55:27.3446459Z`):
  `bin\build\materialeditor-performance-final.json`, SHA-256
  `D15C07DDE8BAC2BAF06AA810520BC1CEA9E85719C73667F429572321DF794743`;
  semantic fingerprint
  `FF5F3CFC956BFC040F14F94537F418C1956FBDAA09827C57B44857FABC852C1F`,
  21/21 invariants and all 23 scenario outcome fingerprints **PASS**.
- Final KK DLL: `bin\build\KK.MaterialEditor\KK_MaterialEditor.dll`, version
  4.0.3.0, 1,008,640 B, SHA-256
  `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26`.
- Final KK ZIP: `bin\out\KK_MaterialEditor_v4.0.3.zip`, 852,106 B, SHA-256
  `8293F987827F8E4F3841F4FD833C82EB4F5E96CA2EE11833236C6C03D5A5B369`.
  It contains exactly DLL, XML, and `libwebp.lib`; no PDB or shared dependency.
- Final deployed DLL Cecil re-audit: **PASS** against the exact
  `86E287...12E26` build/installed bytes — 16/16 assembly, 428/428 type, and
  2,275/2,275 member references; 0 failures, 0 use of six incompatible KKAPI
  types, 0 `RegisterForAudit`, and 0 assertion calls. Release
  `DebuggableAttribute` modes = 2 without `DisableOptimizations`.
- Build, restore, and deploy `--verify-only` checks: **PASS** and non-mutating.
- Actual deployment: **PASS**. Only DLL and `libwebp.lib` were copied; installed
  DLL hash equals the build hash, installed `libwebp.lib` SHA-256 remains
  `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`,
  no config was copied, and exactly one loadable Material Editor DLL was found.
  Plugin-load subsequently caused BepInEx to materialize four new performance
  settings at safe defaults; all 42 existing values remained identical and the
  resulting config SHA-256 is
  `D3F46013C007B0F6BFB18BA591E48CA90354620F04A9BD335FC25BBF3C98FFC7`.
- Minimal plugin-load smoke: **PASS**. Hidden Koikatu loaded `Material Editor
  4.0.3` and `Material Editor Maker 4.0.3`; hidden CharaStudio loaded
  `Material Editor 4.0.3` and `Material Editor Studio 4.0.3`. Each log contained
  one completed Chainloader startup and 0 Material Editor-attributed errors;
  both processes were closed. Material Editor was never opened.
- Full card/coordinate/partial-load/scene/import runtime roundtrip and visual UI
  validation: **PENDING**.
