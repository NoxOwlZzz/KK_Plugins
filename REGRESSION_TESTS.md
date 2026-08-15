# Material Editor optimization regression tests

## Current automated status

This document records the final clean automated/build gates for optimized code
commit `0ea899fd`, built from restore/script HEAD `189562ee`, and distinguishes
them from interactive Unity UI/persistence checks that have not yet run.

| Gate | Current evidence | Final-release status |
| --- | --- | --- |
| Clean build/deployment rebuild | **PASS**, `build-materialeditor-optimized.bat --clean`, exit 0 | Complete |
| Metadata/regression executable | **PASS**: 34/35 gates; gate 30 is the separate analyzer build-time gate | Complete |
| Harness vs checked-in pre-optimization baseline | **PASS**, 21/21 invariants and all 23 scenario outcome fingerprints | Complete; final JSON hashed |
| API Release / PublicApiAnalyzers | **PASS**, 0 warnings / 0 errors | Complete |
| AI, EC, HS2, KK, KKS, PH Release | **PASS**, 0 errors for each target | Complete |
| Final DLL/ZIP | **PASS**, exact hashes and contents recorded | Complete |
| Final deployed DLL Cecil re-audit | **PASS** against byte-identical build/installed DLL | Complete |
| Build/restore/deploy `--verify-only` | **PASS**, non-mutating | Complete |
| Actual deployment and installed-file audit | **PASS**: only DLL/native copied; build/installed DLL match; native unchanged; one loadable DLL; post-smoke config delta verified as four new safe-default entries only | Complete |
| Maker/Studio plugin-load smoke, editor closed | **PASS**: base + Maker/Studio 4.0.3 loaded, Chainloader completed once per process, 0 attributed errors | Complete |
| Manual UI, persistence, lifecycle-profiler, and visual matrix | Not automated | **PENDING** |

## Metadata/regression executable

Command:

```powershell
dotnet run --project tests\MaterialEditor.MetadataTests\MaterialEditor.MetadataTests.csproj -c Release --no-restore
```

Confirmed coverage includes:

- functional Phase 1 metadata/schema behavior and schema-1 compatibility;
- schema-2 `Toggle`, `Dropdown`, and `Enum` aliases plus metadata fallbacks;
- Vector numeric input and Dropdown readability source/layout guards;
- VirtualList viewport identity and anchor preservation;
- CRC64 golden values, randomized differential cases, invalid input, numeric
  zero-allocation behavior, and the retained byte-array contract;
- shared texture ownership, duplicate content/refcounts, exceptional cleanup,
  character destruction, Studio clear/load/import, and source ownership guards;
- KK 1.42.2 / Extended Save 20.0 stable keys, bundled texture roundtrip,
  readable version-2 modes, malformed/future input, and API availability;
- search wildcard, escaping, case-insensitive, comma-token, and underscore-token
  differential behavior;
- provider priority/sequence, `P * M` calls, generation snapshots, registration
  disposal, exception isolation, and atomic failed enumeration;
- per-build condition source caching, fail-open behavior, retry after exception,
  and unique-source read counts;
- frame-scoped invalidation coordination, latest-wins search/shader behavior,
  reentrant draining, and null/throw coroutine-start recovery;
- selective condition dependency graphs: deduplicated source batches,
  transactional `ShowIf`, stale-handle rejection, and no binding/listener
  churn;
- bounded property-ID cache behavior, hit/miss counts, failure-safe eviction,
  null/empty compatibility, 512-entry capacity, and PH-safe source use;
- diagnostics default-off behavior, one-read `TryStart`, counter-only/timed
  modes, balanced profiler samples, and disabled fast-path allocation checks;
- production Vector Mixed aggregation with exact short-circuit comparison
  counts;
- idle pending-texture/normal-map/VirtualList guards and coalesced texture
  refresh gates;
- close/reopen release of presentation, row bindings, side lists, watcher, and
  deferred work while preserving valid reopen state and bounded row views;
- destroyed/replaced target cleanup across Maker and Studio, exact ownership
  boundaries, pending OBJ cancellation, color-palette callback release,
  fake-null handling, Timeline pruning, and event unsubscription guards;
- Enum/Dropdown/Toggle Priority 9 semantics and allocation stress: Mixed,
  Unknown, nulls, duplicate values, same-index caption refresh, exact listener
  replacement, 500 stable rebinds, 64/2 oscillation, and 192/2 cap compaction.

The confirmed allocation lines include:

```text
CRC64 differential tests passed; legacy numeric allocations=1440000 B/10000,
optimized numeric allocations=0 B/10000,
check-value allocations=320000 B/10000.

Enum dropdown Priority 9 allocations: legacy=128000 B/500,
optimized=0 B/500.
```

These tests mix pure production-linked behavior, compatibility stubs, and
source guards. They do not instantiate the real Unity UI or prove native/GPU
collection.

## Performance harness

Command:

```powershell
dotnet run --project tests\MaterialEditor.PerformanceHarness\MaterialEditor.PerformanceHarness.csproj -c Release --no-restore -- --compare-baseline tests\MaterialEditor.PerformanceHarness\baselines\pre-optimization.json
```

Confirmed gates:

- simple and KKLT-like parse/presentation/search workloads;
- 1, 5, 20, and 100 targets for equal, Mixed, missing-property, and shader
  variant outcomes;
- longevity work covering open/close, material/mode/search cycles, 500
  bind/unbind cycles, condition invalidation, and provider lifecycle;
- every shared scenario outcome fingerprint matches the baseline;
- provider calls remain `P * M`, priority/order is stable, and registry count
  returns to zero;
- listener registrations equal removals and the synthetic row pool remains
  bounded;
- source enumerations, pattern builds, condition reads, and provider snapshot
  builds satisfy their optimized exact-count invariants.

Confirmed final aggregate fingerprint:

```text
FF5F3CFC956BFC040F14F94537F418C1956FBDAA09827C57B44857FABC852C1F
```

The baseline aggregate is
`28D358B51ADF3766B70C866FE924A518A30A821C6833B72141E81F52106FBCA9`.
The aggregates differ because the current report has additional named
invariants; aggregate equality is therefore not the baseline gate. The current
value is recorded so repeated runs of the same invariant set can detect drift.

Final JSON generated `2026-08-08T17:55:27.3446459Z`:
`bin\build\materialeditor-performance-final.json`, SHA-256
`D15C07DDE8BAC2BAF06AA810520BC1CEA9E85719C73667F429572321DF794743`.
All 21 invariants pass and all 23 shared scenario outcome fingerprints match
the baseline. Its exact P95 allocation/time pairs are recorded in
`PERFORMANCE_RESULTS.md`; synthetic timings are not Unity frame-time claims.

## Public API and compatibility gates

The final clean PublicAPI hashes are:

- `PublicAPI.Shipped.txt`:
  `F844B75DEB8025FEBEBF4E6FFC8700903FAAF4B04E0952221EB0D63031268367`;
- `PublicAPI.Unshipped.txt`:
  `ED5FAEB493FE464CC96EE7CC81322C72575557A7EFCC73D0F050E04D621B4023`.

Final release checklist:

- [x] **PASS** Clean API Release / PublicApiAnalyzers: 0 warnings, 0 errors.
- [x] **PASS** Both PublicAPI hashes match the recorded values.
- [x] **PASS** Clean AI, EC, HS2, KK, KKS, and PH Release builds: 0 errors.
- [x] **PASS** Final JSON written and compared: 21/21 invariants and 23/23
  scenario outcome fingerprints.
- [x] **PASS** Cecil re-audit of the exact final build/installed DLL: 16/16
  assembly, 428/428 type, and 2,275/2,275 member references; 0 failures; 0 use
  of six incompatible KKAPI types; 0 `RegisterForAudit`; 0 assertion calls;
  Release `DebuggableAttribute` modes = 2 without `DisableOptimizations`.
- [x] **PASS** Final DLL: `bin\build\KK.MaterialEditor\KK_MaterialEditor.dll`,
  version 4.0.3.0, 1,008,640 B, SHA-256
  `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26`.
- [x] **PASS** Final ZIP: `bin\out\KK_MaterialEditor_v4.0.3.zip`, 852,106 B,
  SHA-256
  `8293F987827F8E4F3841F4FD833C82EB4F5E96CA2EE11833236C6C03D5A5B369`;
  exactly DLL/XML/`libwebp.lib`, no PDB/shared dependency.
- [x] **PASS** Build, restore, and deploy `--verify-only` safety checks.
- [x] **PASS** Actual deployment copied only DLL and `libwebp.lib`; installed
  DLL equals the build hash, `libwebp.lib` remains
  `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`,
  no config was copied, and exactly one loadable Material Editor DLL was found.
- [x] **PASS** Post-smoke config audit: all 42 pre-existing key/value pairs
  remain identical. BepInEx added only `PerformanceDiagnostics=false`,
  `PerformanceCountersEnabled=false`, `PerformanceLogThresholdMs=5`, and
  `PerformanceSummaryOnClose=false`; final config SHA-256 is
  `D3F46013C007B0F6BFB18BA591E48CA90354620F04A9BD335FC25BBF3C98FFC7`.
- [x] **PASS** Minimal hidden plugin-load smoke: Koikatu loaded Material Editor
  base + Maker 4.0.3; CharaStudio loaded base + Studio 4.0.3; each Chainloader
  completed once with 0 Material Editor-attributed errors. Both processes were
  closed and Material Editor itself was never opened.

The compile matrix is compatibility evidence, not proof that every supported
game was runtime-tested.

The smoke proves plugin initialization only. It did not enter/operate Material
Editor, take screenshots, inspect visual layout, edit values, or exercise
persistence.

## Evidence that remains manual

The automated suites do not prove rendered layout, typed numeric interaction,
focus, real Dropdown font readability, real Unity row recycling, Unity object
collection, or the full Koikatsu ExtendedSave lifecycle.

The following remain **PENDING** in `USER_RUNTIME_TEST_CHECKLIST.md`:

- UI/interaction and visual equivalence in Maker and Studio;
- character card, coordinate, partial-coordinate, scene save/load, and scene
  import/remapping roundtrips;
- representative third-party provider/API behavior;
- 100-cycle in-game target/window lifetime behavior and native/GPU profiling.
