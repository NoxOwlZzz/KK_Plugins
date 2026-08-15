# Material Editor functional baseline

## Authority and behavior to preserve

The user confirmed manually that the current modified Material Editor works.
Checkpoint `3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed` is therefore the functional
source of truth. Optimization must preserve its visible UI, property sets,
categories, Basic/Advanced behavior, search, conditions, Mixed semantics,
copy/paste/reset behavior, persistence and public API.

No Character Maker or CharaStudio session was started to establish this
baseline. Detailed visual and functional validation remains the user's task.

## Environment

- Date: `2026-08-08`.
- Branch: `feature/material-editor-critical-optimization`.
- Checkpoint: `3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`.
- .NET SDK: `10.0.302`.
- Configuration: `Release`.
- API analyzer: `PublicApiAnalyzers`, executed by the API project build.

Package versions were not updated. A restore with the repository-only
`nuget.config` failed before compilation because that file does not contain a
source for `Microsoft.Unity.Analyzers`. Restore with the existing global NuGet
configuration resolved the already pinned versions. KK restore also reports a
pre-existing `NU1504` duplicate analyzer PackageReference (`1.*` and `1.25.0`).

## Clean baseline results

API, KK and the test executable were cleaned before the measured build.

| Step | Result | Wall time |
| --- | --- | ---: |
| API restore using global configured sources | PASS | 3,442 ms |
| KK restore | PASS, pre-existing `NU1504` | 4,830 ms |
| Metadata-test restore | PASS | 5,757 ms |
| API clean | PASS, 0 warnings / 0 errors | 1,496 ms |
| KK clean | PASS, 0 warnings / 0 errors | 1,899 ms |
| Metadata-test clean | PASS, 0 warnings / 0 errors | 1,868 ms |
| API Release build and PublicApiAnalyzers | PASS, 0 warnings / 0 errors | 6,224 ms |
| KK Release build | PASS, 282 pre-existing XML warnings / 0 errors | 6,665 ms |
| Metadata/regression executable | PASS | 9,836 ms |

All other available Material Editor targets were restored and built from the
same checkpoint without an error:

| Target | Build result | Build time |
| --- | --- | ---: |
| AI | PASS, 280 warnings | 4,331 ms |
| EC | PASS, 226 warnings | 6,131 ms |
| HS2 | PASS, 280 warnings | 9,257 ms |
| KKS | PASS, 283 warnings | 4,979 ms |
| PH | PASS, 280 warnings | 5,450 ms |

Shared projects are imported into these concrete targets and were compiled as
part of each build.

## Test baseline

The only test project in the repository is the console executable
`tests/MaterialEditor.MetadataTests/MaterialEditor.MetadataTests.csproj`.
Its Release run reported:

```text
34/35 phase-one automated cases passed; #30 PublicApiAnalyzers remains a build-time check.
Schema 2 Toggle/Dropdown type-alias regression tests passed.
Vector-input and dropdown-readability regression guards passed.
Virtual-list viewport-context regression tests passed.
KK 1.42.2/20.0 runtime compatibility regression tests passed.
Material Editor metadata regression tests passed.
```

Case 30 passed through the separate analyzer-enabled API build.

## Baseline artifacts and API

| Artifact | Size | SHA-256 |
| --- | ---: | --- |
| `bin/build/API.MaterialEditor/MaterialEditor.dll` | 403,968 | `670F00536057A4566B0DAFE7951C6FF4E038706A03E2DA94C9626FB25C0645BF` |
| `bin/build/KK.MaterialEditor/KK_MaterialEditor.dll` | 952,320 | `516B3CEDF06BF5E24C0A885909A1DC11F4AF86B2260076CB37AB094DA458DB5B` |
| `bin/out/KK_MaterialEditor_v4.0.3.zip` | 821,885 | `67B11A81363C17021BE1D8D9527988CEEFC9D2907D781CA6F46F11F1CEBF50D7` |

The installed functional DLL has the same source/version but a different
non-deterministic build hash recorded in `BACKUP_SOURCE_INFO.md`.

Public API baselines at the checkpoint:

| File | Size | SHA-256 |
| --- | ---: | --- |
| `PublicAPI.Shipped.txt` | 28,191 | `F844B75DEB8025FEBEBF4E6FFC8700903FAAF4B04E0952221EB0D63031268367` |
| `PublicAPI.Unshipped.txt` | 29,961 | `ED5FAEB493FE464CC96EE7CC81322C72575557A7EFCC73D0F050E04D621B4023` |

## Pre-existing issues and pending evidence

- 282 XML-documentation warnings in the KK target.
- Repository-only NuGet restore cannot resolve `Microsoft.Unity.Analyzers`.
- Duplicate analyzer PackageReference warning during KK restore.
- The detailed runtime save/load, partial-coordinate, Studio import, mixed
  multi-target and third-party integration matrix in `MANUAL_TESTS.md` remains
  pending where explicitly marked.
- Absolute build times are environmental evidence only. Optimization gates use
  invariant counts and paired harness comparisons rather than fragile time
  thresholds.
