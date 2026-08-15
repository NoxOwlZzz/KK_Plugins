# Material Editor UI fixes baseline

## Reproduction

The first clean checkout exposed one portable-test defect: the Phase 6 source
guard compared LF-only multiline text and failed on CRLF. Product/API compile
had already passed. Commit `683b5f4f` normalizes source line endings inside
that test only; it does not change the plugin. Metadata and the complete clean
baseline then passed.

Command:

```powershell
cmd /c build-materialeditor-three-panel-ui.bat
```

The successful run performed clean Release restores/builds, PublicApiAnalyzers,
Metadata/regression/structural tests, the synthetic performance comparison and
AI, EC, HS2, KK, KKS and PH builds. It deployed nothing.

## Frozen pre-fix artifacts

| Artifact | Size | SHA-256 |
| --- | ---: | --- |
| Baseline KK DLL | 1,050,624 B | `AE40F655F736E1DD4DA588856FC209AAAC8D4D3C64470AEF079E5A1FA17B16CF` |
| Documentation XML | 421,700 B | `7275F96E4FE99A1DE8BF6DC23A64A942399EC48790A01F1D76BAC54783D1F1BB` |
| `libwebp.lib` | 604,672 B | `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` |
| Baseline report / `pre-ui-fixes.json` | 32,243 B | `1FCA0D4374FA9EAF10E4DD6C7745E30E987C92130FC44B3229DDB6ED2364912E` |

The path-dependent clean-worktree DLL hash is recorded separately from the
installed reference hash; assembly identity and public surface remain equal.

## Synthetic baseline

- Generated UTC: `2026-08-09T14:12:45.6235333Z`.
- Semantic fingerprint:
  `FF5F3CFC956BFC040F14F94537F418C1956FBDAA09827C57B44857FABC852C1F`.
- Results/invariants: 23/23 and 21/21 PASS.
- Listener balance: zero active listeners after complete scenarios.

| Scenario | P95 ms | P95 allocated | Visible rows | Pool peak | Logical rebuilds |
| --- | ---: | ---: | ---: | ---: | ---: |
| Simple rebuild | 0.0420 | 1,360 B | 25 | 25 | 1 |
| KKLT-like Basic | 0.1253 | 7,096 B | 182 | 32 | 1 |
| KKLT-like Advanced | 0.1044 | 7,528 B | 280 | 32 | 1 |
| KKLT-like search | 0.6162 | 9,224 B | 145 | 32 | 1 |
| 100-target equal | 0.6848 | 7,528 B | 973 | 32 | 1 |
| Longevity composite | 102.6676 | 3,866,008 B | 57 | 32 | 500 |

These .NET 8 timings are host-sensitive and do not measure rendered Unity UI,
FPS, click accuracy or dropdown legibility. They are the frozen algorithmic
comparison point for this task.
