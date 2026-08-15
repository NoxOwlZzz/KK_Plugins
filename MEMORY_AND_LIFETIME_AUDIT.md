# Material Editor memory and lifetime audit

## Scope and evidence language

This document updates the original audit of checkpoint
`3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed` with the lifetime changes now in
the optimized candidate. It distinguishes implementation/source-test evidence
from Unity profiler evidence.

- **Implemented**: present in the current optimized source.
- **Automated PASS**: exercised by production-linked pure tests, compatibility
  stubs, or focused source guards.
- **Intentional retention**: kept to preserve an existing behavior and bounded
  by an explicit owner/lifetime.
- **Pending runtime measurement**: requires Maker/Studio, Unity object identity,
  logs, or a native/GPU memory profiler.
- **Deferred**: deliberately outside this optimization phase.

No full Material Editor UI, visual comparison, persistence roundtrip, or Unity
memory-profiler session is claimed here. The final build/package,
deployment/hash audit, and minimal plugin-load smoke pass; interactive lifecycle
and runtime memory evidence remain **PENDING**.

## Current ownership map

| Owner | Objects held | Current release rule | Evidence/status |
| --- | --- | --- | --- |
| `MaterialEditorUI` static state | Canvas, active UI/view, session, label-click handlers, texture watcher | Normal close disposes watcher and releases transient UI; total invalidation clears target state; plugin destruction removes active edit service | **Implemented**, automated guards |
| `MaterialEditorUI` instance | Presenter, presentation, selection controller, `VirtualList`, invalidation coordinators | Close/target invalidation cancels deferred work and drops presentation/selection bindings | **Implemented**, automated integration tests |
| `MaterialEditorSessionState` | Current target/data, filter, mode, collapse/navigation, selections, Rename/OBJ state | Valid target/data survives normal close for direct reopen; `ClearTargetReferences` handles destroyed/replaced/total invalidation and cancels OBJ work | **Intentional retention** plus automated destroyed-target tests |
| `MaterialEditorPresentation` | Sections, row models, condition graphs, navigation targets | Released on close or invalidation; replaced atomically on rebuild | **Implemented** |
| `VirtualList` | Bounded `RowView` pool and active model bindings | Models/bindings released while pool hierarchy and scroll anchor remain reusable | **Implemented**; retained pool size needs runtime profiling |
| `RowBinder` / `ListenerScope` | Current model and exact UnityEvent removal callbacks | Old scope disposed on every bind/release; plugin-owned callbacks removed exactly | **Automated PASS**, including 500-cycle balance |
| Enum/Dropdown row cache | Displayed value snapshot, reusable `OptionData`, one Unknown caption pair | Per pooled row; retained `OptionData` capped at 128; >128 active options shown, tail dropped/compacted after shrink | **Automated PASS** |
| Side-selection panels | Renderer/material/Rename entries and click closures | Transient entries released on close/target invalidation; valid Rename workflow context may survive normal close | **Implemented**, intentional Rename retention |
| Category navigator | Current presentation plus reusable entry views | Presentation/entries released with transient UI | **Implemented** |
| Condition dependency graph | Conditional definitions, direct dependents, presentation row handles | Created only when conditions exist and owned by one presentation | **Implemented**, bounded by presentation |
| Property-ID cache | Runtime property string/ID handles | Static main-thread FIFO, at most 512 entries; no scene target values | **Implemented**, bounded |
| Extension registry | Provider/editor/selection registrations and active edit service | Public registration token/dispose contract unchanged; active service cleared at shutdown | **Implemented contract**; external plugin disposal remains external responsibility |
| Character controller | Per-character texture and edit dictionaries, event subscriptions | Owned textures disposed on destruction; named event handlers unsubscribe exactly; replacement invalidates only owned UI target | **Implemented**, automated ownership/source guards |
| Studio scene controller | Scene textures, edit state, scene/object events | Clear/load disposes abandoned containers and clears UI target state; import temporary owners release in `finally`; events unsubscribe | **Implemented**, automated ownership/source guards |
| Studio color palette | Engine/wrapper callbacks and selected target context | Target switch/invalidation closes palette; `Close` clears callbacks in `finally` | **Implemented**, automated source guards |
| Timeline helper | Interpolable target references | Rejects/prunes Unity fake-null targets; live target selection is preserved across normal switch | **Implemented**; real Timeline integration remains runtime-pending |
| Shared texture manager | Content-key backing, reference counts, bytes, generated Unity textures | `TextureContainer.Dispose` releases one token; generated owned resources die after last release | **Implemented design**, native destruction still runtime-pending |
| Normal-map manager | Weak source keys and converted results | Weak ownership plus bounded periodic sweep | **Implemented** idle bound |

## UI close versus target invalidation

The optimized source deliberately has two different lifetime boundaries.

### Normal close of a valid target

`Visible = false` now:

- cancels deferred presentation population and frame-coalesced work;
- disposes the texture watcher;
- releases the current presentation, condition graphs, navigation presentation,
  row models/bindings/listeners, and transient selection entries;
- keeps the bounded row-view control hierarchy for reuse;
- preserves filter, Basic/Advanced mode, collapse state, selected order, window
  state, and viewport anchor;
- preserves the live current target/data and valid selection/Rename context
  needed to reopen the same target without changing behavior.

That final item is intentional retention. Ordinary close is not documented as
making a live character/item collectible.

### Destroyed, replaced, or globally invalidated target

The stronger invalidation path additionally:

- clears current target/data and renderer/material selections;
- clears Rename selection/context;
- cancels pending OBJ export;
- closes the Studio palette and clears its engine/wrapper callbacks;
- releases target-bound side-list closures and presentation callbacks;
- prunes destroyed Timeline interpolables while preserving unrelated live
  Timeline targets;
- is safe when Unity equality reports a destroyed fake-null object;
- uses `finally` blocks so session release still occurs if a close step throws.

`ScheduleDestroyedUiTargetPrune` cannot leave its scheduling gate stuck: a null
coroutine return or exception resets the gate and invokes the immediate prune
fallback.

## Ownership boundaries for Maker and Studio

### Character ownership policy

The target policy treats the exact character root as owned. A descendant is
owned only when the retained data is character `ObjectData`. This avoids
cross-invalidating a Studio item (whose data is an integer item ID) merely
because it is parented under a character bone.

Character destruction captures the `ChaControl`/root before Unity fake-null can
erase identity. Studio dropdown cleanup uses exact `ChaControl` identity; a CLR
null argument is a no-op rather than a hidden “clear all” command. Maker's
static subscriptions use named handlers so each `+=` has an exact `-=` during
destruction.

### Studio object and scene ownership

- Scene Clear/Load performs total target-state invalidation and texture-owner
  cleanup.
- Object deletion uses the exact integer item ID or exact character/root; null
  never means “delete all”.
- A live target switch closes the target-bound color palette but preserves live
  Timeline selection.
- If the previous target has become Unity fake-null, the switch also prunes its
  old Timeline references.
- Scene-load subscription and unsubscription use the same handler.
- PH setup behavior was intentionally left unchanged.

## Rows, listeners, and retained option data

- `VirtualList` reuses a bounded viewport-sized row pool and has an unchanged
  frame fast path.
- Closing/replacing content releases bound models and exact listeners without
  destroying the pool.
- Exact listener removals are preserved; blanket `RemoveAllListeners` is used
  only where a control is fully plugin-owned, not as pooled-row cleanup.
- Enum/Dropdown projection reuses value snapshots and `OptionData` after
  warm-up. A 64 -> 2 -> 64 oscillation over 500 cycles measured 0 B managed
  allocation and no callbacks in the focused test.
- A 192-option active descriptor still displays all 192 values. After a shrink
  to two values, retained option/value buffers are at or below the 128-slot cap.
- Stable Unknown text retains exactly one value/string pair per row; changed
  Unknown values replace it.
- Rebinding model A to model B removes A's exact listener; a subsequent
  selection writes only to B.

These are managed/source-linked results, not proof of UnityEvent storage or
native UI memory in the game process.

## Conditions, providers, search, and property IDs

- Search matchers live for one build and do not retain targets globally.
- Provider registration snapshots are generation-bounded; provider results and
  callback-bearing descriptors remain presentation-owned.
- Condition source values and dependency graphs are presentation-owned. A
  zero-condition material allocates no graph/resolver/catalog state.
- `ShowIf` batches decide visibility transactionally before any old graph state
  is committed.
- Runtime property handles are bounded to 512. Cache keys/values contain names
  and IDs, not a `Material`, renderer, `GameObject`, or persistence object.

External registrations remain process-lifetime if their owners never dispose
them; Material Editor cannot safely revoke another plugin's public registration
without changing the API contract.

## Texture ownership and native/GPU resources

### Implemented cleanup

- Studio scene Clear/Load disposes each abandoned `TextureContainer` before
  replacing/clearing the dictionary.
- Studio scene import and character import/partial-load release temporary
  dictionaries in `finally` after destination ownership has been established.
- Character destruction disposes all remaining owned texture containers.
- Purge paths still dispose only the entries they remove.
- Shared-token refcounts preserve an adopted destination texture when a
  temporary owner is released.
- No ordinary row-display path encodes a texture merely to render the row.
- Pending-file update returns before cleanup work when no path is queued.

### Compatibility retained

- New KK saves use the original bundled version-1 texture data.
- Bundled, local, and deduplicated version-2 inputs remain readable.
- Hash meaning, keys, byte ownership, and persistence format are unchanged.
- Material Editor destroys only resources it owns.

Automated owner/refcount tests establish managed ownership behavior. They do
not prove the timing or retained byte size of Unity native/GPU destruction.

## Instrumentation boundaries relevant to this audit

Instrumentation does not provide a universal memory census:

| Metric | Exact scope |
| --- | --- |
| `MixedCalculations` / `MixedComparisons` | Production Vector Mixed-flag aggregation; harness also uses the names for its own synthetic comparisons |
| `Save` / `Load` | `TextureSaveHandler` payload serialization/deserialization only |
| `CacheInvalidations` | `VirtualList.SetList` calls only |
| `PropertyToIdCalls` | Property-ID cache misses/resolutions only |

With diagnostics disabled, the focal Mixed helper measured 0 B over 10,000
calls and records no counts. This supports the disabled fast-path claim; it is
not evidence that all UI activity allocates zero memory.

## Evidence still pending

Final non-runtime gates already complete:

- optimized code `0ea899fd`, built from restore/script HEAD `189562ee`;
- clean build exit 0, API 0 warnings/errors, metadata 34/35 with analyzer as the
  separate build-time gate, and six Release targets with 0 errors;
- final JSON 21/21 invariants and all 23 baseline scenario outcomes;
- installed/build DLL hash match at
  `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26`;
- deployment copied only DLL/native, preserved the native hash, copied no
  config, and left exactly one loadable Material Editor DLL;
- plugin-load made BepInEx append four diagnostic settings with safe defaults;
  all 42 pre-existing config values remained identical;
- hidden Koikatu/CharaStudio plugin-load smoke loaded base + Maker/Studio 4.0.3,
  completed Chainloader once per process, produced 0 Material Editor-attributed
  errors, and closed both processes without opening Material Editor.

The Cecil re-audit of that exact final deployed DLL also passes: build and
installed bytes are identical; 16/16 assembly, 428/428 type, and 2,275/2,275
member references resolve with 0 failures; release/audit guards contain no
incompatible KKAPI-type use, `RegisterForAudit`, assertion call, or disabled
optimization flag.

- Real Maker and Studio close/reopen, target destroy/replace, scene clear/load,
  and shutdown behavior with logs.
- One hundred in-game close/open cycles and one hundred target changes without
  monotonic growth in presentations, row bindings, selection closures,
  listeners, watchers, palettes, or targets.
- Unity profiler weak-reference/retained-size evidence for old renderers,
  materials, projectors, characters, items, and storage contexts.
- Native/GPU resource evidence for duplicate textures, import exception paths,
  scene reload, and character destruction.
- Card, coordinate, partial-coordinate, scene save/load, and scene import data
  roundtrips.
- Representative third-party providers and Timeline integration in the real
  runtime.

Until those checks run, the correct conclusion is: the identified ownership
and release paths are implemented and regression-guarded, but the optimized
build is not claimed to be runtime leak-free.

## Deferred changes

- Destroying/recreating the bounded UI pool on every close without profiler
  evidence.
- Revoking external registrations automatically.
- Changing texture format, deduplication policy, keys, or public disposal
  behavior.
- Pooling target-bearing row models or provider outputs beyond one
  presentation.
- Adding thumbnails, texture previews, a new cache, or any visual redesign.
