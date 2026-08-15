# Material Editor performance baseline

## Measurement boundary

This baseline was captured after adding internal diagnostics but before applying
any optimization. The functional source remains checkpoint
`3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`; instrumentation is disabled by
default and does not change visible UI, persistence or public API behavior.

The standalone harness is synthetic. It links the production metadata parser,
condition policy, legacy search filter and diagnostics core, but it does not
claim to reproduce Unity Canvas, native `Material`, GPU or BepInEx timing.
Timing is evidence only; semantic fingerprints and operation-count invariants
are the primary comparison gates.

## Reproduction

Environment:

- Captured: `2026-08-08T13:44:55Z`.
- Runtime: `.NET 8.0.21` for the harness.
- Configuration: `Release`, no debugger.
- Harness schema: `1`.
- Strategy: `synthetic-workloads-with-linked-production-helpers`.
- Semantic fingerprint:
  `28D358B51ADF3766B70C866FE924A518A30A821C6833B72141E81F52106FBCA9`.

Command:

```powershell
dotnet run --project .\tests\MaterialEditor.PerformanceHarness\MaterialEditor.PerformanceHarness.csproj --configuration Release --no-build --no-restore -- --save-baseline .\tests\MaterialEditor.PerformanceHarness\baselines\pre-optimization.json
```

The generated JSON is
`tests/MaterialEditor.PerformanceHarness/baselines/pre-optimization.json`,
SHA-256
`0CE378B147A449568BF868480AC6F3A2610CCB000E260C340FE81B9528C5DD29`.

## Selected results

| Scenario | Iterations | Median ms | P95 ms | Median alloc | P95 alloc |
| --- | ---: | ---: | ---: | ---: | ---: |
| simple parse, 20 properties / 4 categories | 15 | 0.230 | 0.351 | 43,424 B | 43,424 B |
| simple rebuild | 20 | 0.007 | 0.008 | 1,704 B | 1,704 B |
| KKLT-like parse, 250 properties / 30 categories | 10 | 2.214 | 5.439 | 442,200 B | 442,200 B |
| KKLT-like Basic rebuild | 15 | 0.045 | 0.069 | 7,096 B | 7,096 B |
| KKLT-like Advanced rebuild | 15 | 0.071 | 0.083 | 7,096 B | 7,096 B |
| KKLT-like search | 15 | 0.404 | 0.425 | 44,056 B | 44,056 B |
| 100 equal targets | 6 | 0.544 | 0.573 | 44,320 B | 44,320 B |
| longevity composite | 1 | 87.105 | 87.105 | 7,338,520 B | 7,338,520 B |

Absolute values vary with machine load, JIT and antivirus activity. A retained
optimization must keep the same semantic outcomes and improve a relevant count
or paired allocation/timing metric; timing alone is not sufficient.

## Baseline invariants

All harness invariants passed:

- Simple dataset: 20 properties, 4 categories and one material.
- KKLT-like dataset: 250 properties, 30 categories, 100 Advanced,
  40 conditions, 20 enums, 10 vectors, 15 toggles, tooltips and Hidden rows.
- Legacy search preserves comma/underscore tokenization, `*`, `?`, case
  insensitivity and literal regex-character escaping.
- Equal, Mixed, absent-property and shader-variant inputs remain distinguishable
  for 1, 5, 20 and 100 targets.
- Listener registrations equal removals after every complete scenario.
- Synthetic row-pool high-water is bounded at 32 views.
- Provider calls remain `P * M`; the pre-optimization strategy builds one
  sorted provider snapshot per material.
- Forty conditions cause forty evaluations and forty uncached source reads per
  Advanced material rebuild.
- Current renderer/projector source handling is represented as two source
  enumerations per rebuild.
- Longevity performs 100 open/close, 200 material changes, 100 mode changes,
  100 searches, 500 explicit bind/unbind cycles, 100 condition invalidations
  and 100 provider register/remove cycles without residual synthetic listeners
  or provider registrations.

## Build and API gates at this baseline

- Metadata/regression executable: PASS, including performance-instrumentation
  disabled/counter/timing tests.
- Performance harness: PASS.
- API Release and PublicApiAnalyzers: PASS, 0 warnings / 0 errors.
- KK Release: PASS, 0 errors.
- AI, EC, HS2, KKS and PH Release builds: PASS, 0 errors; their pre-existing
  XML-documentation warning families remain.

No Maker or Studio process was started and no visual test or screenshot was
performed for this baseline.
