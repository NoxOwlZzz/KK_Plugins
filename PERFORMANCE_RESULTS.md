# Material Editor optimization performance results

## Evidence boundary

The performance harness is a deterministic .NET 8 synthetic workload linking
production metadata, condition, search, provider, property-ID, CRC, and
instrumentation helpers. It does not execute Unity, Character Maker,
CharaStudio, native texture/GPU work, or the actual rendered UI.

Scenario fingerprints and exact operation-count invariants are semantic gates.
Managed allocation is useful supporting evidence. Wall-clock timing is not a
release gate and the recorded values are synthetic .NET timings, not Unity UI
frame times. The exact clean final capture is recorded below without extending
its meaning to Maker or Studio.

## Current status

| Evidence | Status |
| --- | --- |
| Clean build script | **PASS**, `build-materialeditor-optimized.bat --clean`, exit code 0 |
| Metadata/regression executable | **PASS**: 34/35 gates; gate 30 is PublicApiAnalyzers at build time |
| Harness comparison with `pre-optimization.json` | **PASS**: 21/21 invariants and all 23 baseline scenario outcome fingerprints |
| Current aggregate fingerprint | `FF5F3CFC956BFC040F14F94537F418C1956FBDAA09827C57B44857FABC852C1F` |
| API Release / PublicApiAnalyzers | **PASS**, 0 warnings / 0 errors |
| AI, EC, HS2, KK, KKS, and PH Release matrix | **PASS**, 0 errors for every target |
| Final performance JSON | **PASS**, written and hashed |
| Final deployed KK DLL/ZIP | **PASS**, details below |
| Final deployed DLL Cecil re-audit | **PASS**, exact build/installed bytes; all assembly/type/member references resolved; release/audit guards clean |
| Build/restore/deploy `--verify-only` | **PASS**, non-mutating |
| Actual deployment and installed optimized-file verification | **PASS**: build/installed DLL hashes match, native unchanged, no config copied, one loadable DLL; post-smoke BepInEx added four expected default-off diagnostic entries without changing any existing value |
| Minimal Maker/Studio plugin-load smoke with Material Editor closed | **PASS**, base + Maker/Studio 4.0.3 loaded; Chainloader complete once per process; 0 attributed errors |
| Full runtime persistence and visual validation | **PENDING** |

No interactive Maker/Studio UI, visual, GPU-memory, or persistence result is
claimed in this document. Game processes were started only for the recorded
plugin-load smokes, with Material Editor closed, and then terminated.

## Reproducible inputs

- Baseline report:
  `tests/MaterialEditor.PerformanceHarness/baselines/pre-optimization.json`.
- Baseline report SHA-256:
  `0CE378B147A449568BF868480AC6F3A2610CCB000E260C340FE81B9528C5DD29`.
- Baseline aggregate fingerprint:
  `28D358B51ADF3766B70C866FE924A518A30A821C6833B72141E81F52106FBCA9`.
- Configuration: Release, no debugger.
- Functional source checkpoint:
  `3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`.
- Frozen optimized code commit: `0ea899fd`.
- Reproducible restore/script HEAD used for the clean run: `189562ee`.
- Final report generated `2026-08-08T17:55:27.3446459Z`:
  `bin\build\materialeditor-performance-final.json`, 32,238 B,
  SHA-256
  `D15C07DDE8BAC2BAF06AA810520BC1CEA9E85719C73667F429572321DF794743`.

The current aggregate differs from the baseline aggregate because the current
report contains additional named invariants. The comparison gate intentionally
matches each shared scenario's deterministic outcome fingerprint; all of those
match. The aggregate itself is recorded to detect drift between repeated runs
of the same current invariant set.

## Final paired P95 evidence

These values come from the final clean JSON comparison. Allocation is managed
.NET allocation, not Unity retained memory; time is harness execution time, not
a rendered frame measurement.

| Scenario/path | P95 allocated, baseline -> final | P95 ms, baseline -> final |
| --- | ---: | ---: |
| Simple presentation rebuild | 1,704 B -> 1,360 B | 0.0079 -> 0.0107 |
| KKLT-like Basic rebuild | 7,096 B -> 7,096 B | 0.0689 -> 0.1057 |
| KKLT-like Advanced rebuild | 7,096 B -> 7,528 B | 0.0830 -> 0.1202 |
| KKLT-like search | 44,056 B -> 9,224 B | 0.4248 -> 0.3260 |
| 100-target equal | 44,320 B -> 7,528 B | 0.5725 -> 0.6561 |
| Longevity composite | 7,338,520 B -> 3,866,008 B | 87.1052 -> 84.4036 |

The small Basic/Advanced/simple timing movements are reported exactly and are
not hidden. The deterministic semantic gates, not timing direction, decide
equivalence.

Additional focal allocation checks:

| Path | Baseline/legacy -> final | Interpretation |
| --- | ---: | --- |
| Numeric CRC64, 10,000 calls | 1,440,000 B -> 0 B | Public numeric result unchanged |
| Contractual CRC check-value path, 10,000 calls | 320,000 B -> 320,000 B | Required new `byte[8]` return retained |
| Enum/Dropdown stable rebind, 500 cycles | 128,000 B -> 0 B after warm-up | Same options/selection; no callbacks |
| Enum/Dropdown 64 -> 2 -> 64, 500 cycles | current 0 B after warm-up | High-water slots reused; no callbacks |
| Stable Unknown Enum caption, 500 cycles | current 0 B after warm-up | One value/string pair retained per row |
| Disabled focal Mixed aggregation, 10,000 calls | current 0 B | Counters and timers remain untouched |

Lists above 128 Enum options still display in full. When such a list later
shrinks, retained `OptionData` and value-buffer capacity are compacted to the
explicit per-row cap of 128; this prevents the optimization itself from
becoming an unbounded lifetime cache.

The +432 B Advanced result is reported rather than hidden: selective
invalidation retains a small presentation-scoped graph so `ShowIf` changes can
make one transactional rebuild decision. It is released with the presentation.

## Exact operation-count evidence

| Workload/counter | Baseline -> current | Preserved invariant |
| --- | ---: | --- |
| Renderer/projector source enumerations, simple rebuild | 40 -> 20 | One materialized source use per rebuild instead of two |
| Renderer/projector source enumerations, KKLT search | 30 -> 15 | One per rebuild |
| Renderer/projector source enumerations, longevity | 1,000 -> 500 | One per rebuild |
| Search pattern builds, KKLT search | 5,670 -> 15 | One prepared pattern per term/rebuild |
| Search pattern builds, longevity | 48,600 -> 100 | One pattern for each search rebuild in this scenario |
| Condition material reads, KKLT Advanced | 600 -> 75 | Evaluations remain; each unique source is read once per build |
| Provider snapshot builds, KKLT Advanced | 15 -> 1 | Provider calls remain `P * M` and retain priority/order |
| Provider snapshot builds, 100-target equal | 600 -> 1 | All 2,400 provider calls remain |
| Provider snapshot builds, longevity | 2,500 -> 1 | Registration/removal still advances the generation |

The harness also preserves listener registrations equal to removals, a
synthetic view-pool high-water of 32, and zero remaining provider registrations
after its longevity scenario.

## Final binary evidence

- DLL: `bin\build\KK.MaterialEditor\KK_MaterialEditor.dll`.
- Assembly version: 4.0.3.0.
- DLL size: 1,008,640 B.
- DLL SHA-256:
  `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26`.
- ZIP: `bin\out\KK_MaterialEditor_v4.0.3.zip`, 852,106 B, SHA-256
  `8293F987827F8E4F3841F4FD833C82EB4F5E96CA2EE11833236C6C03D5A5B369`.
- ZIP contents: exactly `KK_MaterialEditor.dll`, `KK_MaterialEditor.xml`, and
  `libwebp.lib` under `BepInEx/plugins/KK_Plugins`; no PDB or shared dependency.
- Cecil re-audit of the exact final build/installed bytes: **PASS** — 16/16
  assembly references, 428/428 type references, and 2,275/2,275 member
  references; 0 failures; 0 use of six incompatible KKAPI types; 0
  `RegisterForAudit`; 0 assertion calls. Release `DebuggableAttribute` modes = 2
  without `DisableOptimizations`.

## Focal runtime instrumentation checks

The current production Vector binder delegates Mixed-flag aggregation to the
instrumented helper. Exact short-circuit semantics were confirmed:

| Input flags | `MixedCalculations` | `MixedComparisons` | Result |
| --- | ---: | ---: | --- |
| four false | 1 | 4 | false |
| first true | 1 | 1 | true |
| second true | 1 | 2 | true |
| diagnostics disabled | 0 | 0 | same Boolean result |

These names have two deliberately documented contexts:

- production counts Vector-row Boolean component aggregation;
- the standalone harness uses the same names for synthetic multi-target
  comparisons.

External providers may compute Mixed values themselves, so neither counter is a
global count of all Mixed work.

Other metric scopes are equally narrow:

- `Save` / `Load` measure only `TextureSaveHandler` payload
  serialization/deserialization;
- `CacheInvalidations` counts only `VirtualList.SetList` presentation
  replacement;
- `PropertyToIdCalls` counts only cache misses/resolutions in the bounded
  runtime property-ID cache;
- `TextureHashing` covers creation/acquisition of a texture container, not every
  possible hash performed by dependencies;
- `TextureEncoding` is not evidence that every encode in Unity is counted.

## Lifecycle evidence and limit

Pure and source-linked tests cover release of presentations, models, row
bindings, category navigation, selection closures, pending OBJ work, palettes,
and destroyed/replaced targets. They also cover exact listener removal,
schedule-failure recovery, texture-owner disposal, and the bounded Enum cache.

Normal close intentionally retains the valid session target/data and lightweight
reopen state. Destroyed/replaced target paths clear those strong references.
This is not a Unity memory-profiler result; stable native/GPU retained size and
100-cycle in-game lifecycle behavior remain **PENDING**.

## Reproduction

```powershell
dotnet run --project tests\MaterialEditor.PerformanceHarness\MaterialEditor.PerformanceHarness.csproj -c Release --no-restore -- --compare-baseline tests\MaterialEditor.PerformanceHarness\baselines\pre-optimization.json
```

The safe clean deployment rebuild wrote and compared the final JSON
successfully. Build, restore, and deploy `--verify-only` pass, and actual
deployment also passed: only DLL and `libwebp.lib` were copied, the installed
DLL equals the final build hash, the native hash stayed unchanged, and one
loadable Material Editor DLL remains. During the later smoke BepInEx appended
four new diagnostic settings with safe defaults; all 42 pre-existing values
remained identical. The minimal hidden Koikatu/CharaStudio
plugin-load smoke also passed with the editor never opened. UI interaction,
runtime persistence, native/GPU memory, and visual fields remain **PENDING**.
