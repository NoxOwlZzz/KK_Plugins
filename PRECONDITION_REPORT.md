# Material Editor three-panel UI precondition report

## Decision

**GO by explicit user authorization, with runtime evidence carried forward.**

The optimized source is healthy and reproducible. The repository does not
independently close every runtime precondition. After the exact optimized build
was synchronized locally, the user explicitly instructed Codex to proceed with
the UI redesign. That direction authorizes work despite the recorded evidence
gaps; it does not imply that the user reviewed this report or executed the
pending cases. Those cases remain mandatory items in the later visual/runtime
checklist.

## Confirmed baseline

- Source baseline: `feature/material-editor-critical-optimization` at
  `525a8c4bd5b11db44871fd54bf5f1fd4b5bbd238`.
- Safety branch:
  `backup/material-editor-before-three-panel-ui-20260808-205924`.
- Working branch: `feature/material-editor-minimal-three-panel-ui`.
- The source worktree was clean before branching.
- `build-materialeditor-optimized.bat --clean`: PASS, exit code 0.
- Metadata/regression executable: PASS.
- Performance harness baseline comparison: PASS.
- MaterialEditor.API and PublicApiAnalyzers: PASS.
- AI, EC, HS2, KK, KKS, and PH Release targets: PASS with zero errors.
- Basic/Advanced no-write behavior: covered by automated regression tests.
- VirtualList pooling, listener balance, idle fast path, and bounded reuse:
  covered by automated regression tests.
- Card, coordinate, and scene persistence paths exist and are protected by
  structural/ownership tests.
- Source backup and `restore-materialeditor-backup.bat` exist.
- The user reports that the updated functions were manually tested and work.

## Evidence still missing or ambiguous

The existing final records explicitly leave these checks pending:

1. Current-build runtime roundtrip for character card save/load.
2. Current-build runtime roundtrip for coordinate save/load.
3. Current-build partial-load behavior.
4. Current-build Studio scene save/load.
5. Current-build Studio scene import/remapping.
6. Interactive Basic/Advanced behavior in Maker and Studio for the exact
   optimized baseline.
7. Runtime lifecycle behavior across repeated open/close and target changes.

The general statement that the new functions work is acknowledged, but it does
not identify these persistence and lifecycle cases individually. The existing
`UI_REDESIGN_READINESS.md`, `PERFORMANCE_RESULTS.md`,
`MEMORY_AND_LIFETIME_AUDIT.md`, and `REGRESSION_TESTS.md` continue to label them
as pending runtime evidence.

The installed/build mismatch found during the initial audit is now resolved.
After the game was closed, the prior installed state was backed up at
`BepInEx/PluginBackups/MaterialEditor/20260808-210636-before-optimized-sync`.
The clean deploy gate passed and the installed/build DLLs are byte-identical:
`1,008,640` bytes, SHA-256
`804A70B90EE8F0429B7B2B7739F952D5B72DC95CD67865720E441BFCF4C5EB97`.
The native and active configuration hashes did not change, and exactly one
loadable Material Editor DLL remains.

## Authorization and remaining verification

The user authorized continuation on 2026-08-08 after the exact optimized build
was synchronized locally. The following cases remain user-owned verification
items because Codex is prohibited from opening Material Editor for visual or
interactive testing:

- card save/load;
- coordinate save/load;
- partial load;
- Studio scene save/load;
- Studio scene import;
- Basic/Advanced in Maker and Studio;
- repeated open/close and target switching without duplicated listeners or
  stale UI state.

Before the first later UI test deployment, Koikatu and CharaStudio must again
be closed and the then-installed optimized state must receive an additional
timestamped backup.

## Work not included in the precondition phase

- No visual tokens were added.
- No hierarchy, panel, header, row, menu, tooltip, or footer was changed.
- No Material Editor public API or persistence code was changed.
- No runtime visual inspection or screenshot was performed.
