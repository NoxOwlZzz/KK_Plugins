# Material Editor Phase 1 delivery

## Source provenance

- Upstream repository: `IllusionMods/KK_Plugins`.
- Original commit: `1502cced6e372c6c328703eb7b7caea589bff4a7`.
- Working branch: `feature/material-editor-basic-advanced-ui`.
- Safety branch: `backup/material-editor-before-ui-overhaul-20260807-2005`.
- The working tree contains the complete delivery and is intentionally not
  committed: 58 tracked files are modified and 22 files are new.
- Original plugin GUID, target framework, shipped API baseline, schema-1
  behavior and existing persistence identifiers are retained.

## Build artifacts

| Artifact | Result |
| --- | --- |
| `bin/build/KK.MaterialEditor/KK_MaterialEditor.dll` | Version `4.0.3.0`; 952,320 bytes; SHA-256 `11CD1B91661E44B3620DA0FFE2D6FE20C2998F31791F6368A24E6E8F6CEEA550` |
| `bin/build/KK.MaterialEditor/libwebp.lib` | 604,672 bytes; SHA-256 `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` |
| `bin/out/KK_MaterialEditor_v4.0.3.zip` | 821,924 bytes; SHA-256 `7F5C248E9A855BCF1C2490F503880D7E2ED023BC972C413436E2F9090404A84C`; exactly DLL, XML and `libwebp.lib` |

The final DLL is installed at
`D:\Games\Koikatsu\BepInEx\plugins\KK_Plugins\KK_MaterialEditor.dll`.
Exactly one loadable Material Editor DLL exists below `BepInEx\plugins`.

## Backup and restoration

The immutable pre-change backup is outside the loadable plugin tree at
`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260807-195335`.
It contains the original 3.12 DLL, `libwebp.lib`, config and `BACKUP_INFO.txt`.
`restore-materialeditor-backup.bat` prevalidates all expected hashes and the
3.12 assembly identity before replacing any file, then validates the restored
files again.

The immediately preceding Phase 1 DLL, native library and active config were
also snapshotted before the alias-only redeployment at
`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260807-2328-before-type-aliases`.

The alias-enabled Release DLL, native library and active config were
snapshotted again immediately before the Vector/dropdown readability fix at
`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260808-0734-before-ui-readability`.

## Public API additions

- Extension API version `1.2.0` and capability flags for UI levels, Enum,
  Vector, Float Toggle and conditional visibility.
- `MaterialEditorUiMode` and `MaterialEditorPropertyUiLevel`.
- Enum option/editor, Vector editor, Float Toggle editor, condition and
  comparison contracts.
- Optional descriptor metadata for UI level, group, conditions, enum options,
  vector component count and toggle values.
- `ShaderPropertyType.Vector`, Vector edit-service operations, public
  `MaterialAPI.SetVector`, Vector clipboard data and `VectorProperty` label
  click identity.

The complete analyzer-approved symbol list is in
`src/MaterialEditor.API/PublicAPI.Unshipped.txt`; the shipped baseline was not
removed or rewritten.

## Compatibility and deliberate limits

- The DLL is compiled for the installed KKAPI 1.42.2, Extended Save 20.0 and
  Sideloader 20.0. No shared dependency was copied or upgraded.
- Schema-2 `Type="Toggle"`, `Type="Dropdown"` and `Type="Enum"` are parser
  aliases backed by Float. They add no public `ShaderPropertyType` value and no
  persistence key; schema 1 remains unchanged.
- Binary audit resolved 16/16 assembly references, 422/422 type references and
  2,108/2,108 member references against the exact game installation.
- The compatibility texture handler writes the original bundled version-1
  format and reads bundled, local and deduplicated version-2 data. Unknown
  future versions are left untouched and skipped with a warning.
- Native Vector persistence is additive and backward-readable from legacy
  Color data when a manifest explicitly reclassifies the property. The
  official upstream plugin cannot preserve this new native Vector key when it
  resaves data.
- Presets, favorites, Direction Pad, composite Min/Max, Gradient, curves,
  channel preview, mass editing and shader compilation remain roadmap work.

## Verification performed

- Clean API Release build/PublicApiAnalyzers: 0 warnings, 0 errors.
- Metadata and Phase 1 suite: 34/35 numbered runtime tests pass; case 30 is the
  successful build-time analyzer gate.
- Dedicated parser tests pass for Toggle/Dropdown/Enum normalization, custom
  Toggle values, options, explicit-editor precedence, safe fallbacks,
  case/whitespace handling, schema-1 rejection and canonical syntax.
- Dedicated readability guards cover authoritative nonzero Vector input
  widths, X/Y/Z/W font floors, visible input backgrounds, dropdown caption/item
  font floors and template-level Debug validation.
- The real `UI.VirtualList.cs` is exercised for exact scroll identity,
  category fallback, one atomic viewport publication and stale deferred-restore
  cancellation.
- KK runtime-compatibility texture tests pass.
- Clean KK Release build: 0 errors and the same 282 pre-existing XML warnings
  as the unmodified baseline.
- Koikatu Character Maker validated the Phase 1 UI build. CharaStudio Debug
  then measured each Vector input at 58 UI, the long Enum caption at rendered
  font size 16 in an 18 UI text area, and stable pooled clones. The final
  Release loaded the current 4.0.3 core and Studio components and completed the
  chainloader with zero Error/Fatal lines at or after Material Editor load.
- On a stable KKLT/Opaque Studio sphere, the scrolled list body was identical
  across Basic/Advanced/Basic (`0/258,960` differing pixels) and the rendered
  sphere was identical (`0/11,025`).

Logs and screenshots are preserved at
`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\RuntimeValidation-20260807-2244-final`.
The UI-readability Debug and final Release logs are preserved under the
`RuntimeValidation` child of the `20260808-0734-before-ui-readability` snapshot.
See `MANUAL_TESTS.md` for exact hashes and the remaining manual matrix.

## Verification still pending

- Real character-card, coordinate and Studio-scene save/reload roundtrips for
  every new editor family and the no-edit file-size check.
- Broad clothes/hair/accessory/body/face/map/projector coverage.
- Third-party MaterialAPI/Extension API/HairShadowColorControl integration.
- Exhaustive schema-1 third-party manifest, rapid focused-input, mixed
  multi-selection and profiler/allocation passes.

## Modified tracked files

- `Guides/Material Editor Guide/Extension API.md`
- `Guides/Material Editor Guide/Public API Compatibility.md`
- `Guides/Material Editor Guide/Shader Tooltip Catalogs.md`
- `Guides/Material Editor Guide/shader_manifest_template.xml`
- `src/MaterialEditor.API/PublicAPI.Unshipped.txt`
- `src/MaterialEditor.Base/CopyContainer.cs`
- `src/MaterialEditor.Base/IMaterialEditRepository.cs`
- `src/MaterialEditor.Base/LegacyMaterialEditRepository.cs`
- `src/MaterialEditor.Base/MaterialAPI.cs`
- `src/MaterialEditor.Base/MaterialEditService.cs`
- `src/MaterialEditor.Base/MaterialEditor.Base.projitems`
- `src/MaterialEditor.Base/MaterialEditorEditServiceFacade.cs`
- `src/MaterialEditor.Base/MaterialEditorExtension.Contracts.cs`
- `src/MaterialEditor.Base/MaterialEditorExtension.Registry.cs`
- `src/MaterialEditor.Base/PluginBase.cs`
- `src/MaterialEditor.Base/UI/UI.LabelClick.cs`
- `src/MaterialEditor.Base/UI/UI.LayoutDiagnostics.cs`
- `src/MaterialEditor.Base/UI/UI.MaterialSectionPresenter.cs`
- `src/MaterialEditor.Base/UI/UI.NumericInputView.cs`
- `src/MaterialEditor.Base/UI/UI.Presentation.cs`
- `src/MaterialEditor.Base/UI/UI.PropertyDescriptor.cs`
- `src/MaterialEditor.Base/UI/UI.PropertyOrganizer.cs`
- `src/MaterialEditor.Base/UI/UI.RowBinder.FloatKeyword.cs`
- `src/MaterialEditor.Base/UI/UI.RowBinder.cs`
- `src/MaterialEditor.Base/UI/UI.RowBinding.Common.cs`
- `src/MaterialEditor.Base/UI/UI.RowControls.cs`
- `src/MaterialEditor.Base/UI/UI.RowLayout.cs`
- `src/MaterialEditor.Base/UI/UI.RowModel.Properties.cs`
- `src/MaterialEditor.Base/UI/UI.RowModel.cs`
- `src/MaterialEditor.Base/UI/UI.RowView.cs`
- `src/MaterialEditor.Base/UI/UI.RowViewFactory.cs`
- `src/MaterialEditor.Base/UI/UI.SessionState.cs`
- `src/MaterialEditor.Base/UI/UI.ShaderUiMetadata.cs`
- `src/MaterialEditor.Base/UI/UI.StyleSystem.cs`
- `src/MaterialEditor.Base/UI/UI.VirtualList.cs`
- `src/MaterialEditor.Base/UI/UI.WindowView.cs`
- `src/MaterialEditor.Base/UI/UI.cs`
- `src/MaterialEditor.Core.Maker/Core.MaterialEditor.Maker.cs`
- `src/MaterialEditor.Core.Studio/Core.MaterialEditor.SceneController.Edits.Core.cs`
- `src/MaterialEditor.Core.Studio/Core.MaterialEditor.SceneController.Edits.Properties.cs`
- `src/MaterialEditor.Core.Studio/Core.MaterialEditor.SceneController.Models.cs`
- `src/MaterialEditor.Core.Studio/Core.MaterialEditor.SceneController.cs`
- `src/MaterialEditor.Core.Studio/Core.MaterialEditor.SceneRepository.cs`
- `src/MaterialEditor.Core.Studio/Core.MaterialEditor.Studio.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.CharaController.Edits.Core.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.CharaController.Edits.Properties.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.CharaController.Events.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.CharaController.Models.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.CharaController.Persistence.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.CharaController.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.CharaRepository.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.Import.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.Shaders.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.TextureSaveHandler.cs`
- `src/MaterialEditor.Core/Core.MaterialEditor.cs`
- `src/MaterialEditor.KK/KK.MaterialEditor.csproj`
- `tests/MaterialEditor.MetadataTests/MaterialEditor.MetadataTests.csproj`
- `tests/MaterialEditor.MetadataTests/Program.cs`

## New files

- `BASIC_ADVANCED_DESIGN.md`
- `EXTENSION_API_CHANGES.md`
- `FEATURE_GAP_ANALYSIS.md`
- `MANIFEST_SCHEMA_V2.md`
- `MANUAL_TESTS.md`
- `PHASE1_DELIVERY.md`
- `ROADMAP.md`
- `Guides/Material Editor Guide/KKLT/KKLT_UI_CLASSIFICATION.md`
- `Guides/Material Editor Guide/KKLT/manifest.schema-v2.xml`
- `build-materialeditor-kk.bat`
- `deploy-materialeditor-kk-test.bat`
- `restore-materialeditor-backup.bat`
- `src/MaterialEditor.Base/UI/UI.MaterialPropertyMetadata.cs`
- `src/MaterialEditor.Base/UI/UI.RowBinder.EnumVectorToggle.cs`
- `src/MaterialEditor.Base/UI/UI.RowViewFactory.EnumVectorToggle.cs`
- `tests/MaterialEditor.MetadataTests/KkRuntimeCompatibilityTests.cs`
- `tests/MaterialEditor.MetadataTests/ManifestPropertyTypeAliasTests.cs`
- `tests/MaterialEditor.MetadataTests/PhaseOneRegressionTests.cs`
- `tests/MaterialEditor.MetadataTests/ProductionDependencyStubs.cs`
- `tests/MaterialEditor.MetadataTests/TexturePersistenceStubs.cs`
- `tests/MaterialEditor.MetadataTests/UiControlReadabilityRegressionTests.cs`
- `tests/MaterialEditor.MetadataTests/VirtualListScrollContextTests.cs`
