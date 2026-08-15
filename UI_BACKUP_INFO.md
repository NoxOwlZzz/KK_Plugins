# Material Editor UI backup information

## Latest final UI-fix backup and deployment

This section records the current deployed candidate. The three-panel snapshots
below remain historical and do not describe the current DLL.

- Source commit: `8bcf8649` (`ui: restore dark theme and stabilize side panel
  collapse`).
- Build and installed DLL: 1,088,000 bytes, SHA-256
  `72EA76AC8AB31BAE28E67FA53AED658A28E585BA78B85CCF08DC80F9B8405F95`.
- Build/package XML: 423,700 bytes, SHA-256
  `0765558075A34C37E76BD75159E21BC2E1B029819BB29A78BD8B0F3A1215EDFC`.
- Build and installed `libwebp.lib`: 604,672 bytes, SHA-256
  `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`.
- Final ZIP: 891,917 bytes, SHA-256
  `E4DAD44BAA86DDFFD5F2282D04D6664D004520677DCC0FD34B5D0CF9E1584A9F`;
  exactly the DLL, XML and native library.

Immediately before deployment, the workflow created:

`D:/Games/Koikatsu/BepInEx/PluginBackups/MaterialEditor/20260809-135846-before-ui-fixes`

Its 667-byte `BACKUP_INFO.txt`, created at
`2026-08-09T17:58:57.5352235Z`, has SHA-256
`4A0EDEFEB8194E24EEF4D36F10D6BD5EBCB7723216E01D4222E039A53DCCA9B2`.
The manifest records exactly two prior runtime files:

- previous DLL: 1,088,000 bytes, SHA-256
  `4E0D4C6C5F5582EC9CE2F88A75ECA12739220A3943D2DC2C65808C241BE0DFFA`;
- previous native library: 604,672 bytes, SHA-256
  `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`.

The documentation XML, configuration and data were not copied. The latest
backup pointer matches this directory and manifest. Deployment copied only
`KK_MaterialEditor.dll` and `libwebp.lib`; their installed bytes match the
final build. The observed post-deploy configuration has SHA-256
`19B5F5E4569E0A3E8F640C6FBAF0C8140FE9BA331498C6DC1BE0A31CC6621AA7`.
Because no immediately-before hash was recorded for this deployment, this
document does not claim a cross-candidate byte comparison for the config.

Payload and manifest hashes were checked while the backup was created. A
separate restore `--verify-only` was not rerun for this newest backup during
the documentation sync. No Maker or CharaStudio load smoke was recorded for
the exact final DLL; load, visual, interactive, persistence and runtime-profile
validation remain **PENDING**.

## Historical three-panel source snapshot

- Captured: 2026-08-08 20:59:24 America/Santo_Domingo.
- Source branch: `feature/material-editor-critical-optimization`.
- Source commit: `525a8c4bd5b11db44871fd54bf5f1fd4b5bbd238` (`Fix Studio color palette reopening`).
- Safety branch: `backup/material-editor-before-three-panel-ui-20260808-205924`.
- Working branch: `feature/material-editor-minimal-three-panel-ui`.
- Tracked modifications before branching: none.
- Untracked files before branching: none.

## Pre-redesign build gate

`build-materialeditor-optimized.bat --clean` completed successfully before any
UI source change. It restored dependencies, ran metadata/regression tests,
compared the performance harness with the frozen baseline, ran
PublicApiAnalyzers, and built AI, EC, HS2, KK, KKS, and PH Release targets.
Nothing was deployed by this command.

- Build DLL: `bin/build/KK.MaterialEditor/KK_MaterialEditor.dll`.
- Assembly version: `4.0.3.0`.
- Build DLL size: `1,008,640` bytes.
- Build DLL SHA-256: `804A70B90EE8F0429B7B2B7739F952D5B72DC95CD67865720E441BFCF4C5EB97`.
- Performance report: `bin/build/materialeditor-performance-final.json`.
- Performance report size: `32,245` bytes.
- Performance report SHA-256: `8EA77105462FABC12B1E491E30798003CEA0D23A76BA3BBD194FCBF10A7C4BBC`.
- `build-materialeditor-optimized.bat --verify-only`: PASS.

## Pre-redesign optimized synchronization (historical)

The initial audit found a different installed DLL. After Koikatu was closed,
that exact state received a verified backup at:

`D:/Games/Koikatsu/BepInEx/PluginBackups/MaterialEditor/20260808-210636-before-optimized-sync`

The backup contains the prior DLL, `libwebp.lib`, a read-only configuration
snapshot, and `BACKUP_INFO.txt`. The optimized deploy script then rebuilt every
mandatory gate and copied only `KK_MaterialEditor.dll` and `libwebp.lib`.

- Installed/build DLL size: `1,008,640` bytes.
- Installed/build DLL SHA-256: `804A70B90EE8F0429B7B2B7739F952D5B72DC95CD67865720E441BFCF4C5EB97`.
- Installed file version: `4.0.3`.
- Installed `libwebp.lib` SHA-256: `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`.
- Active configuration SHA-256 remained
  `6A483723C8B143B678F719BAA8A726FEC50CBB0D4CA94F288052193FA25C92B6`.
- Loadable `KK_MaterialEditor` assembly count: `1`.
- Game/Studio process count after deployment: `0`.

## Final three-panel UI build

The clean `8c36e8df` workflow passed API/PublicApiAnalyzers, Metadata and the
synthetic harness, then built AI, EC, HS2, KK, KKS and PH Release targets with
zero errors. The two public-API baselines remained unchanged:

- Shipped:
  `F844B75DEB8025FEBEBF4E6FFC8700903FAAF4B04E0952221EB0D63031268367`.
- Unshipped:
  `ED5FAEB493FE464CC96EE7CC81322C72575557A7EFCC73D0F050E04D621B4023`.

The deploy workflow performed one final clean rebuild. These are the current
post-rebuild artifacts, not the earlier preliminary hashes:

| Artifact | Size | SHA-256 |
| --- | ---: | --- |
| `bin/build/KK.MaterialEditor/KK_MaterialEditor.dll` | 1,050,624 B | `B1B02AEED5FA9B86D2317855FA419BC59C08A0DA9650030D6AC8F8EE12154CFC` |
| `bin/build/KK.MaterialEditor/KK_MaterialEditor.xml` | 421,700 B | `7275F96E4FE99A1DE8BF6DC23A64A942399EC48790A01F1D76BAC54783D1F1BB` |
| `bin/build/KK.MaterialEditor/libwebp.lib` | 604,672 B | `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` |
| `bin/out/KK_MaterialEditor_v4.0.3.zip` | 872,982 B | `18A50C367B3FAA5B59C665A1D81BF4DADF17A95BB079EE2B8D1E6F0C03762826` |
| `bin/build/materialeditor-performance-three-panel-ui.json` | 32,241 B | `C06A755CD9E011519BEFC28F4FF34C02774ABD7F90EBE55B6CEB3D91E16059AD` |

The final DLL has file version 4.0.3 and MVID
`DB6524AC-A227-4BEC-9A88-6C3759ADA957`. Its binary audit passed 16/16 assembly,
432/432 type and 2,302/2,302 member-reference resolutions plus the Release
guards.

## Final three-panel UI backup and deployment

Immediately before replacement, the deploy workflow created:

`D:/Games/Koikatsu/BepInEx/PluginBackups/MaterialEditor/three-panel-ui/20260809-023401-before-three-panel-ui`

The backup manifest is `BACKUP_INFO.txt`, 693 bytes, SHA-256
`0E90A898FBAAEF92B6960029655EB30B1BCDAC0DC08DFDA1BB455BEE6B5D340E`.
It records:

- previous DLL: 1,008,640 bytes, SHA-256
  `804A70B90EE8F0429B7B2B7739F952D5B72DC95CD67865720E441BFCF4C5EB97`;
- previous native library: 604,672 bytes, SHA-256
  `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`;
- assembly/file version: 4.0.3.0 / 4.0.3;
- exactly two runtime files;
- documentation XML absent from the installed source and therefore not copied;
- configuration and data not copied or modified.

`restore-materialeditor-backup.bat --verify-only` accepted that backup without
writing to the game. Deployment copied only `KK_MaterialEditor.dll` and
`libwebp.lib`. The installed DLL is byte-identical to the final build at
`B1B02AEED5FA9B86D2317855FA419BC59C08A0DA9650030D6AC8F8EE12154CFC`.
The installed native file remains
`8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`.

The active configuration remained at 46 settings and SHA-256
`6A483723C8B143B678F719BAA8A726FEC50CBB0D4CA94F288052193FA25C92B6`
before deployment, after deployment and after the editor-closed Maker/Studio
load smokes. No Material Editor window, screenshot, card, coordinate or scene
was opened by those smoke tests.
