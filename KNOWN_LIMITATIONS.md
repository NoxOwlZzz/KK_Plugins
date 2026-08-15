# Material Editor known limitations

## Scope

This file distinguishes remaining functional/runtime limits from optimizations
already implemented in the candidate derived from checkpoint
`3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`.

The critical optimization phase does not authorize new user features, visual
redesign, persistence-format changes, dependency upgrades, or public API
changes. A source-linked test is not reported as a real Unity, visual,
persistence, or native-memory result.

## Status legend

- **Implemented**: present in the optimized source and covered by focused
  automated evidence.
- **Confirmed limitation**: established by current source/contract.
- **Intentional compatibility limit**: deliberately retained for existing
  runtime/data behavior.
- **Pending runtime verification**: requires the final DLL in Maker/Studio.
- **Deferred feature**: outside this optimization scope.

## Functional and visual limitations

| Area | Current state/limit | Status |
| --- | --- | --- |
| Toggle/Dropdown/Enum | Schema-2 aliases and rows exist; Dropdown options/font and numeric fields have targeted readability guards | **Implemented**; real rendered/readability check **PENDING** |
| Category main toggle | `Group` metadata is parsed/exposed, but there is no category-level toggle control | **Deferred feature** |
| Modified Only | No Modified Only filter exists | **Deferred feature** |
| Advanced modified count | Reliable built-in changes are counted; opaque custom editor changes are not guessed | **Confirmed limitation**, conservative by design |
| Mixed editing | Existing models/editor contracts support Mixed state; no broader batch-edit feature is added by optimization | **Confirmed boundary** |
| Property/category actions | Property/category clipboard, category reset, reset-all, presets, and favorites are not added here | **Deferred feature** |
| Vector Timeline | No guaranteed native Vector interpolation registration is added; existing Color Timeline behavior remains the compatibility boundary | **Confirmed limitation** |
| UI layout | Programmatic hierarchy still uses many fixed metrics; this phase did not create a responsive layout | **Confirmed limitation** |
| Theme/assets | No selectable theme, new typography, icon registry, or authored UI prefab | **Deferred feature** |
| Extension editors | Opaque editors control their own reliable modified/Mixed reporting | **Confirmed limitation** |

No screenshot, font, spacing, color, icon, density, or layout acceptance is
claimed for the optimized candidate.

## Search, providers, and conditions

### Implemented

- Search parses each filter/rebuild once, reuses wildcard matchers, and keeps
  legacy comma/underscore, escaping, wildcard, case-insensitive, renderer,
  material, property, and projector behavior.
- Search/shader refresh uses latest-wins frame-scoped coalescing with null/throw
  scheduling recovery.
- Provider registration order has a generation-backed sorted snapshot. Provider
  calls remain `P * M`, preserve priority/sequence, isolate exceptions, and
  materialize a failed enumeration atomically.
- Condition source values are memoized only within one material build; failed
  reads can retry.
- A presentation-scoped dependency graph now performs selective invalidation:
  one transactional presentation decision for `ShowIf` without listener churn.

### Remaining limits

- Provider results can contain dynamic callbacks and target contexts and are
  intentionally not cached globally.
- Conditions support the documented single numeric/keyword grammar, not
  arbitrary Boolean expressions. Malformed, missing, or unsupported sources
  continue to fail open.
- A `ShowIf` visibility change still requires a presentation rebuild because
  row membership/order can change.
- The small selective condition graph adds measured presentation-scoped managed
  allocation in the synthetic Advanced case (7,096 B -> 7,528 B). It is
  released with the presentation.
- Real extension providers and condition-driven UI changes remain
  **PENDING runtime verification**.

## Invalidation and idle work

### Implemented

- A frame-scoped coordinator deduplicates pending Search, Shader, and Condition
  work and drains reentrant work without orphaning its gate.
- Search is dominant in a shared batch; valid newer condition edits cancel
  obsolete deferred population according to the tested precedence.
- `ShowIf` updates evaluate dependent visibility transactionally before
  rebuilding the presentation when its membership changes.
- `VirtualList` returns on an unchanged frame based on dirty state, scroll
  position, and viewport geometry.
- Character pending-texture update returns when no path is queued.
- Weak normal-map cleanup is bounded instead of swept every update.
- Clothes/body refresh bursts coalesce to one end-of-frame execution and recover
  if coroutine scheduling returns null or throws.

### Remaining limits

- Frame-coalesced work may still rebuild a full presentation when search,
  shader, category/mode, or `ShowIf` semantics require membership/order changes.
- Tooltip and shader-underline components remain active-window behavior; no
  redesign converts them to a different event system.
- Synthetic/source guards do not establish actual Unity frame time or managed
  allocation while open/idle/scrolling. Those measurements remain
  **PENDING**.

## VirtualList, rows, and listeners

### Implemented

- Normal close releases models, presentation, bound row contexts, category
  navigation, transient side entries, exact row listeners, and deferred work;
  the bounded viewport-sized row hierarchy remains pooled.
- Listener scopes reuse storage while preserving exact removal. Pooled row
  cleanup does not use blanket `RemoveAllListeners`.
- Automated cycles preserve listener add/remove balance and a stable pool.
- Enum/Dropdown option/value snapshots are reused after warm-up; the stable
  500-rebind case reports 128,000 B legacy versus 0 B optimized.
- The cache retains at most 128 `Dropdown.OptionData` objects per pooled row.
  Larger active lists remain complete and shrink back to bounded capacity.
- Mixed, Unknown, null, duplicate-value, last-duplicate selection, same-index
  caption refresh, and A-to-B listener replacement are guarded.
- Float-backed Toggle skips an unchanged programmatic update.

### Remaining limits

- One generic pooled row still contains all supported control families. No
  runtime profiler evidence justifies splitting it into type-specific pools.
- The 128-entry cap bounds retained `OptionData`, but a currently displayed
  descriptor with more options necessarily holds its full active list until it
  shrinks or is released.
- Focus, IME/input behavior, real Dropdown caption/font rendering, real Unity
  event storage, and heterogeneous row scrub remain **PENDING runtime/visual
  verification**.

## Memory and target lifetime

### Implemented

- Normal close releases transient UI state while intentionally preserving the
  valid target/data and lightweight reopen/Rename context.
- Destroyed/replaced targets clear strong target/data references, selections,
  Rename state, target callbacks, color palette callbacks, and pending OBJ
  export.
- Deferred destroyed-target pruning resets its gate and runs an immediate
  fallback after a null/throw coroutine start.
- Maker exit and Studio scene clear/load perform total target-state
  invalidation.
- Character and Studio object deletion use exact identity plus an ownership
  policy, preventing a null/fake-null or a character-bone parent from clearing
  an unrelated item target.
- Named Maker/Studio event handlers have exact unsubscribe paths.
- Timeline helpers reject/prune fake-null targets while preserving unrelated
  live selections.

### Remaining limits

- A valid live target remains strongly referenced across an ordinary close to
  preserve direct reopen. This is intentional and means “window closed” is not
  equivalent to “live target collectible”.
- The bounded control pool remains allocated for reuse.
- External provider/handler registrations live until their external owners
  dispose them.
- Source-linked tests do not prove Unity native object collection or retained
  byte size. A real profiler and repeated lifecycle run remain **PENDING**.

See `MEMORY_AND_LIFETIME_AUDIT.md` for the ownership map.

## Texture limitations and compatibility

### Implemented cleanup

- Abandoned character/scene dictionaries and temporary import owners are
  disposed on destruction, clear/load, import completion, and exception paths.
- Shared-token refcounts preserve adopted destination textures.
- Pending processing and weak normal-map sweeps have bounded no-work paths.
- Numeric CRC64 avoids the temporary array while the public byte-array path is
  unchanged.

### Intentional compatibility limits

- KK writes the original bundled version-1 texture dictionary because the
  installed KKAPI 1.42.2 writer surface is the compatibility target.
- Bundled, `LOCAL_`, and `DEDUPED_` version-2 inputs remain readable; this fork
  does not start emitting newer local/deduplicated saves.
- Texture keys, format, hash meaning, reference ownership, and deduplication
  policy are unchanged.
- Material Editor destroys only resources it owns.

### Pending runtime verification

- Unchanged-versus-changed texture hash/encode counts in the real game.
- Card/coordinate/scene load/import and character-destruction acquire/release
  balance under Unity.
- Native/GPU retained memory and destruction timing.

Thumbnail creation/caching remains outside scope.

## Persistence risk boundary

- Native Vector persistence is an additive fork format. Official upstream may
  not understand the Vector key and may discard it when resaving; keep backups
  when alternating writers.
- Schema 1 remains supported. `Type="Toggle"`, `Type="Dropdown"`, and
  `Type="Enum"` aliases are schema-2-only.
- UI filter/mode/collapse and cached runtime IDs never become saved material,
  card, coordinate, or scene data.
- Save/load remains independent of Basic/Advanced visibility and whether the UI
  was opened.

The following final optimized runtime tests are all **PENDING**:

- character card save/reload;
- coordinate and partial-coordinate save/load;
- Studio scene save/reload;
- Studio scene import with ID remapping;
- opening/closing without editing and proving no data change;
- bundled and readable version-2 texture inputs.

The compatibility stubs and bundled roundtrip test do not execute Koikatsu's
complete MessagePack/ExtendedSave lifecycle.

## Public API and property IDs

### Implemented boundary

- Extension API 1.2, capabilities, editor/descriptor identities,
  `ShaderPropertyType.Vector = 4`, and
  `MaterialEditorLabelType.VectorProperty = 9` are unchanged.
- PublicAPI files remain analyzer gates. The final API build passed with 0
  warnings/errors, and both recorded PublicAPI SHA-256 values match.
- Runtime material calls use a bounded FIFO cache of 512 property handles.
- `PropertyToIdCalls` counts only cache misses/resolutions; it is not a global
  Shader API counter.
- Property-name strings remain the API, metadata, persistence, and diagnostic
  identities.

### Remaining limits

- The cache follows Unity's main-thread material API rule; it is not intended
  as a thread-safe general service.
- Passing PublicApiAnalyzers proves surface compatibility, not all third-party
  runtime behavior.
- Existing 1.1 consumers, 1.2 capability-aware consumers,
  MaterialAPI-dependent plugins, and representative providers remain
  **PENDING runtime verification**.

## Instrumentation and measurement limits

Metric scope must not be overstated:

- `MixedCalculations` / `MixedComparisons` measure production Vector Mixed-flag
  aggregation; the harness uses the same names for synthetic multi-target
  comparisons. External provider Mixed work is not globally counted.
- `Save` / `Load` cover only `TextureSaveHandler` payload
  serialization/deserialization.
- `CacheInvalidations` covers only `VirtualList.SetList`.
- `PropertyToIdCalls` covers only bounded-cache misses/resolutions.

The disabled focal path measured 0 B over 10,000 calls, but this is not a claim
that the whole editor allocates zero memory. Managed harness allocation is also
not Unity native/GPU retained memory.

The clean final harness capture is recorded and passes all 21 invariants and 23
scenario outcomes. Its exact P95 values are synthetic .NET evidence, not Unity
UI timing; the final multi-100 P95 is 0.6561 ms and longevity P95 is 84.4036 ms,
so no rendered-frame claim is inferred from them.

## Multi-target, build, and deployment limits

- Shared Base/Core code compiles into AI, EC, HS2, KK, KKS, and PH. Compile
  evidence is not runtime evidence for all six games.
- Final clean optimized build matrix: **PASS**, 0 errors for every target.
- API/PublicApiAnalyzers: **PASS**, 0 warnings / 0 errors; final PublicAPI hashes
  match.
- Final harness JSON: **PASS**, SHA-256
  `D15C07DDE8BAC2BAF06AA810520BC1CEA9E85719C73667F429572321DF794743`,
  21/21 invariants and 23/23 scenario outcomes.
- Final DLL: **PASS**, version 4.0.3.0, 1,008,640 B, SHA-256
  `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26`.
- Final ZIP: **PASS**, 852,106 B, SHA-256
  `8293F987827F8E4F3841F4FD833C82EB4F5E96CA2EE11833236C6C03D5A5B369`,
  exactly DLL/XML/`libwebp.lib`, no PDB/shared dependency.
- Final deployed DLL Cecil re-audit: **PASS** — byte-identical build/installed
  DLL; 16/16 assembly, 428/428 type, 2,275/2,275 member references; 0 failures,
  incompatible-KKAPI-type uses, `RegisterForAudit`, assertions, or disabled
  Release optimizations.
- Build/restore/deploy `--verify-only`: **PASS**.
- Actual deployment and installed-file/config audit: **PASS**. Build and
  installed DLL hashes match; native is unchanged; only DLL/native were copied;
  exactly one loadable DLL remains. The later smoke caused BepInEx to add four
  new diagnostic keys at safe defaults, with all 42 prior values unchanged;
  final config SHA-256 is
  `D3F46013C007B0F6BFB18BA591E48CA90354620F04A9BD335FC25BBF3C98FFC7`.
- Minimal Maker/Studio plugin-load smoke with Material Editor closed: **PASS**.
  Base + Maker/Studio 4.0.3 loaded, Chainloader completed once per process, 0
  Material Editor-attributed errors, and both hidden processes were closed.
- Full UI, visual, persistence, and profiler verification: **PENDING**.

The optimized fork must be the only loadable Material Editor DLL. Deployment
must copy only the intended Material Editor DLL and `libwebp`; it must not copy
shared game/BepInEx/KKAPI dependencies or overwrite configuration/data.

> Do not install this optimized fork beside the official Material Editor DLL.

## Remaining acceptance boundary

Automated build, harness, artifact, and deployment evidence is complete, but
final acceptance still requires:

- manual UI/interaction/visual equivalence;
- card, coordinate, partial-load, scene, and scene-import persistence;
- repeated in-game target/window lifecycle and native/GPU memory evidence;
- representative third-party runtime compatibility.
