# Current Material Editor build audit

## Final UI-fix candidate

The final runtime candidate was compiled and deployed from source commit
`8bcf8649`. The complete build finished with exit code 0, including
metadata/regression, API, the schema-2 performance harness and Release targets
AI, EC, HS2, KK, KKS and PH.

### Final artifacts

| Artifact | Size | SHA-256 |
| --- | ---: | --- |
| Build and installed `KK_MaterialEditor.dll` | 1,088,000 B | `72EA76AC8AB31BAE28E67FA53AED658A28E585BA78B85CCF08DC80F9B8405F95` |
| Build/package `KK_MaterialEditor.xml` | 423,700 B | `0765558075A34C37E76BD75159E21BC2E1B029819BB29A78BD8B0F3A1215EDFC` |
| Build and installed `libwebp.lib` | 604,672 B | `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` |
| `KK_MaterialEditor_v4.0.3.zip` | 891,917 B | `E4DAD44BAA86DDFFD5F2282D04D6664D004520677DCC0FD34B5D0CF9E1584A9F` |
| `materialeditor-performance-ui-fixes.json` | 59,092 B | `8986ADAA352AA00D342641A7DE7E24471CA218EDDDCC67A482D84BB2792703CC` |

The DLL is assembly version `4.0.3.0` and file/product version `4.0.3`.
Installed and build DLL bytes are identical. The deployment copied the runtime
DLL/native pair only; the documentation XML remains a build/package artifact.

The final ZIP has exactly three entries:

| Entry | Uncompressed size |
| --- | ---: |
| `BepInEx/plugins/KK_Plugins/KK_MaterialEditor.dll` | 1,088,000 B |
| `BepInEx/plugins/KK_Plugins/KK_MaterialEditor.xml` | 423,700 B |
| `BepInEx/plugins/KK_Plugins/libwebp.lib` | 604,672 B |

It contains no PDB, tests, harness, logs, backups, game/shared assembly or
configuration.

### Backup, restore and configuration

The pre-deploy runtime backup is:

`D:/Games/Koikatsu/BepInEx/PluginBackups/MaterialEditor/20260809-135846-before-ui-fixes`

Its `BACKUP_INFO.txt` SHA-256 is
`4A0EDEFEB8194E24EEF4D36F10D6BD5EBCB7723216E01D4222E039A53DCCA9B2`.
The manifest records the previous 1,088,000-byte DLL
`4E0D4C6C5F5582EC9CE2F88A75ECA12739220A3943D2DC2C65808C241BE0DFFA`
and the unchanged native hash. Backup payload and manifest hashes were verified
during deployment. A separate restore `--verify-only` was not rerun for this
new backup during the documentation sync.

No configuration or data was copied. The post-deploy Material Editor config was
observed at SHA-256
`19B5F5E4569E0A3E8F640C6FBAF0C8140FE9BA331498C6DC1BE0A31CC6621AA7`.
That value differs from the earlier documented candidate, so this audit does
not claim an unrecorded cross-candidate config comparison.

### Runtime smoke boundary

No post-deploy Maker or CharaStudio load smoke was recorded for the exact
`72EA76AC...05F95` DLL. The earlier editor-closed CharaStudio log belongs to the
previous `4E0D4C6C...DFFA` DLL now captured in the latest backup; it is not
initialization evidence for this candidate.

Exact-candidate load smoke, the 29-case visual/interactive checklist,
persistence roundtrips and runtime profiling therefore remain **PENDING**.

## Frozen pre-fix reference boundary

The reference package is the frozen copy at:

`C:/KMT/Backups/KK_Plugins_UI_Fixes/20260809-100810/KK_MaterialEditor_v4.0.3.zip`

It was extracted only under `reference-zip-readonly`; no package content was
extracted into the repository, BepInEx, Assets or the active installation.
The current repository source remains authoritative for all changes.

### Frozen reference package contents

The package has exactly three entries:

| Entry | Size | SHA-256 |
| --- | ---: | --- |
| `BepInEx/plugins/KK_Plugins/KK_MaterialEditor.dll` | 1,050,624 B | `B1B02AEED5FA9B86D2317855FA419BC59C08A0DA9650030D6AC8F8EE12154CFC` |
| `BepInEx/plugins/KK_Plugins/KK_MaterialEditor.xml` | 421,700 B | `7275F96E4FE99A1DE8BF6DC23A64A942399EC48790A01F1D76BAC54783D1F1BB` |
| `BepInEx/plugins/KK_Plugins/libwebp.lib` | 604,672 B | `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` |

No PDB, test binary, harness, shared game assembly or dependency is bundled.

### Frozen reference assembly audit

- Assembly: `KK_MaterialEditor`.
- Assembly version: `4.0.3.0`.
- File/product version: `4.0.3`.
- Runtime: `v2.0.50727`; `I386`, `ILOnly`.
- MVID: `DB6524AC-A227-4BEC-9A88-6C3759ADA957`.
- Dependency resolution against the active Koikatsu installation: 16/16
  AssemblyRefs, 432/432 TypeRefs and 2,302/2,302 MemberRefs, zero failures.
- Release guards: no incompatible KKAPI type use, no `RegisterForAudit`, no
  external assertion calls and no `DisableOptimizations` flag.

This package is the frozen pre-fix binary from the completed three-panel source
line; its MVID and hashes do not describe the later UI-fix candidate. The prior
three-panel documentation/build-script commits did not change product source.
Public API analyzer files and the plugin GUID/assembly name remain the source
of truth; this correction phase must not add PR #401/SubCategory API work.
