# Material Editor optimization source backup

## Preserved source state

- Captured: `2026-08-08T07:55:29-04:00`.
- Source branch: `feature/material-editor-basic-advanced-ui`.
- Source commit before the functional changes: `1502cced6e372c6c328703eb7b7caea589bff4a7`.
- Functional checkpoint commit: `3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed`.
- Safety branch: `backup/material-editor-working-before-optimization-20260808-075506`.
- Optimization branch: `feature/material-editor-critical-optimization`.

Before the checkpoint, the working tree contained 58 modified tracked files and
22 untracked files. Nothing was discarded, stashed, restored or replaced from
upstream. The tracked diff against `1502cced` contained 4,472 insertions and
521 deletions. The checkpoint records all 80 files in one immutable tree; its
complete modified/new file lists are also recorded in `PHASE1_DELIVERY.md`.

The exact preserved lists can be reproduced without consulting a mutable
working tree:

```powershell
git diff --name-status 1502cced6e372c6c328703eb7b7caea589bff4a7 3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed
git show --stat --summary 3f8cf4a5ad302d9c8a396d4a1744dbf4d6b6c8ed
```

The mandatory pre-change inspection was executed before branching:

```powershell
git status
git branch --show-current
git rev-parse HEAD
git diff --stat
git diff
```

No optimization source edit was made before the checkpoint and functional
baseline described in `FUNCTIONAL_BASELINE.md`.

## Preserved installed build

The user-confirmed functional installation was copied outside every BepInEx
loadable path to:

`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260808-0800-before-critical-optimization`

Verified contents:

| File | Size | SHA-256 |
| --- | ---: | --- |
| `KK_MaterialEditor.dll` | 952,320 | `11CD1B91661E44B3620DA0FFE2D6FE20C2998F31791F6368A24E6E8F6CEEA550` |
| `libwebp.lib` | 604,672 | `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` |
| `com.deathweasel.bepinex.materialeditor.cfg` | 8,216 | `CCE2D1A271BD796186B6FB74F519A44B935073107169FC1FC680C32343464C11` |
| `BACKUP_INFO.txt` | 1,404 | `747AC8C0DF40A175BE490E878B885F907856A25F0CF0B6969AC16E19703A240D` |

The config is a safety snapshot only. Deployment and normal restoration must
not overwrite the user's active config.
