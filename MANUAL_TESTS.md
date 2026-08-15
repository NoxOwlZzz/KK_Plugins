# Material Editor Phase 1 manual test plan

This file separates observed build evidence from tests that still require a
running game. A source build or metadata test is not visual/runtime evidence.

> Current revision note: Basic/Advanced and `UiLevel` were development-only
> prototypes and are no longer part of Material Editor. Historical evidence
> below remains labeled as historical; the active checklist tests the optional
> presentation-only Category/Subcategory hierarchy instead.

## Status legend

- **PASS**: executed with the recorded result.
- **FAIL**: executed and did not meet the expected result.
- **PENDING**: not executed in the required environment.
- **N/A**: deliberately unsupported, with a documented reason.

## Recorded evidence as of 2026-08-08

| Check | Configuration | Status | Evidence/notes |
| --- | --- | --- | --- |
| Unmodified upstream KK Material Editor build | Release | **PASS** | `dotnet build src/MaterialEditor.KK/KK.MaterialEditor.csproj -c Release`; 0 errors and 282 pre-existing XML-documentation warnings. Executed before source edits at commit `1502cced6e372c6c328703eb7b7caea589bff4a7`. |
| Unmodified upstream API build | Debug | **PASS** | 0 warnings and 0 errors. PublicApiAnalyzers was part of the project build. |
| Unmodified upstream metadata tests | Release | **PASS** | Console test suite completed successfully. |
| Modified API Release build/PublicApiAnalyzers | Release | **PASS** | Final clean build: 0 warnings, 0 errors; `PublicAPI.Unshipped.txt` accepted. |
| Modified metadata tests | Release | **PASS** | 34/35 numbered runtime-independent cases passed; #30 passed as the build-time PublicApiAnalyzers gate. The real `UI.VirtualList.cs` viewport tests, KK 1.42.2/20.0 compatibility tests, schema-2 Toggle/Dropdown type-alias tests, and Vector/dropdown readability guards all pass. |
| Modified KK Material Editor build | Release | **PASS** | Final critical-optimization build: 0 errors. Assembly `4.0.3.0`; 1,008,640 bytes; deployed SHA-256 `86E287983E61A87EA40079A94182D364D44EB129F3DFB07A6626374E3F512E26`. |
| Installed-runtime binary compatibility | Release | **PASS** | Compiled against KKAPI 1.42.2, Extended Save 20.0 and Sideloader 20.0. Cecil audit of the exact deployed DLL resolved 16/16 assembly, 428/428 type and 2,275/2,275 member references; no missing reference and none of the six unavailable KKAPI 1.45.1 types remain. |
| Deployment safety preflight | `--verify-only` | **PASS** | `build`, `restore`, and `deploy` preflights verified closed processes, the exact `20260808-0800-before-critical-optimization` backup, one loadable Material Editor DLL, protected config, and the final artifact without copying. |
| Critical optimization deployment | Exact test install | **PASS** | The real deploy rebuilt every gate, then copied only `KK_MaterialEditor.dll` and `libwebp.lib`. Build and installed DLL hashes match; native remains `8931C0A1...3966`; no config was copied; exactly one loadable DLL remains. Post-smoke BepInEx added four expected diagnostic keys at safe defaults without changing any of the 42 existing values. |
| Koikatu core/Maker plugin-load smoke | Runtime, editor closed | **PASS** | The deployed optimized DLL loaded Material Editor and Material Editor Maker 4.0.3 exactly once and reached `Chainloader startup complete`; 0 Material Editor-attributed errors. No UI interaction or visual validation was performed. |
| UI readability runtime diagnostics | Prior Phase 1 Debug runtime | **PASS** | Historical Phase 1 evidence measured X/Y/Z/W Vector inputs at 58 UI and a readable long Enum caption. This was not rerun against the critical-optimization build and is not final visual evidence. |
| CharaStudio plugin-load smoke | Release runtime, editor closed | **PASS** | The deployed optimized DLL loaded Material Editor and Material Editor Studio 4.0.3 exactly once and reached `Chainloader startup complete`; 0 Material Editor-attributed errors. No UI interaction or visual validation was performed. |
| Visual UI validation/screenshots | Prior Phase 1 runtime | **PASS** | Historical targeted Phase 1 evidence exists, but visual validation was deliberately not rerun during critical optimization; the exhaustive final visual checklist remains pending. |

The deployment itself did not copy or edit configuration. The plugin-load
smokes caused BepInEx to append four performance settings with safe defaults;
all 42 existing values remained identical. The active config now has SHA-256
`D3F46013C007B0F6BFB18BA591E48CA90354620F04A9BD335FC25BBF3C98FFC7`;
the immutable functional backup and its `CCE2D1...C11` safety snapshot remain
intact.

Earlier Phase 1 runtime logs and screenshots are preserved outside the loadable plugin
tree under
`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\RuntimeValidation-20260807-2244-final`.
`Koikatu-Maker-final.log` has SHA-256
`5FDA824F486F3DBDF970C2B6FFA141A882D81A6452016532D8C01AC3D671E065`;
`CharaStudio-final-224404.log` has SHA-256
`2BEB3375DDD1A752B8DACA14B77AA784723E4A962C3260CC53EEB1CA2C296B18`.
The folder also contains the final Basic/Advanced Maker captures and the three
Studio scroll-preservation captures with their hashes recorded by the final
verification command.

The UI-readability logs are preserved at
`D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260808-0734-before-ui-readability\RuntimeValidation`.
Their Sideloader/archive errors before the Material Editor load are pre-existing
unrelated content. This is load/initialization and targeted visual evidence,
not card/coordinate/scene save-roundtrip evidence.

## Safety prerequisites

1. Close the actual game processes: `Koikatu.exe`, `KoikatuVR.exe`, and
   `CharaStudio.exe`.
2. Verify the functional backup exists outside the loadable plugin tree:
   `D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260808-0800-before-critical-optimization`.
3. Verify the backup DLL SHA-256 is
   `11CD1B91661E44B3620DA0FFE2D6FE20C2998F31791F6368A24E6E8F6CEEA550`.
4. Verify backup `libwebp.lib` SHA-256 is
   `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966`.
5. Verify backup config SHA-256 is
   `CCE2D1A271BD796186B6FB74F519A44B935073107169FC1FC680C32343464C11`.
6. Build with `build-materialeditor-optimized.bat --clean` and record its output hashes.
7. Deploy only with `deploy-materialeditor-optimized-test.bat` after its backup and
   duplicate-DLL checks pass.
8. Confirm exactly one loadable `*MaterialEditor*.dll` exists below
   `D:\Games\Koikatsu\BepInEx\plugins`.

> Do not use this build together with the official Material Editor DLL.

The deployment script copies only `KK_MaterialEditor.dll` and the normal
`libwebp.lib`. It must not copy game, BepInEx, Unity, KKAPI, or other plugin
dependencies, and it must not modify the user's configuration.

## Build and automated checks

| ID | Test | Expected result | Status |
| --- | --- | --- | --- |
| B01 | Build API Release | 0 errors; PublicApiAnalyzers accepts shipped/unshipped surface. | **PASS** |
| B02 | Run metadata tests Release | All legacy and schema-2 cases pass. | **PASS** |
| B03 | Build KK target Release | `bin\build\KK.MaterialEditor\KK_MaterialEditor.dll` is produced. | **PASS** |
| B04 | Inspect output | Only the plugin DLL and normal native `libwebp.lib` are deployment inputs; no game/shared DLL is copied. | **PASS** |
| B05 | Review diff | No GUID, target framework, shipped API, persistence key, or schema-1 behavior changed unintentionally. | **PASS** |
| B06 | Audit final DLL against installed dependencies | All assembly/type/member references resolve against KKAPI 1.42.2, Extended Save 20.0 and Sideloader 20.0. | **PASS** |

## Runtime and visual checklist

Record the game, test asset, timestamp, result and relevant log lines for every
executed item.

| ID | Manual test | Expected result | Status |
| --- | --- | --- | --- |
| R01 | Open Material Editor in Character Maker | Window opens and remains usable. | **PASS (Phase 1 historical); PENDING (critical build)** |
| R02 | Open Material Editor in CharaStudio | Window opens and remains usable. | **PASS (Phase 1 historical); PENDING (critical build)** |
| R03 | First run/default configuration | No Basic/Advanced selector or indicator appears; every compatible non-Hidden property remains discoverable. | **PENDING** |
| R04 | Open a schema-2 manifest using only explicit Category/Subcategory containers | The hierarchy organizes rows and performs no material write. | **PENDING** |
| R05 | Open a flat schema-1/schema-2 manifest that opts into no hierarchy features | Presentation and editing match the pre-hierarchy behavior. | **PENDING** |
| R06 | Collapse/expand Category and Subcategory repeatedly | Only presentation changes; rendered material and stored values remain identical. | **PENDING** |
| R07 | Edit an input, then fold/unfold its parents rapidly | No accidental commit, exception, stuck focus, or duplicate rebuild symptom. | **PENDING** |
| R08 | Inspect a manifest containing old prototype `UiLevel` metadata | `UiLevel` adds no filtering or indicator; Hidden remains the static exclusion. | **PENDING** |
| R09 | Collapse/expand Categories and Subcategories from the disclosure, label and header background | Each interaction folds exactly one structural group without editing a property. | **PENDING** |
| R10 | Filter until a category has no rows | Category header, separator and height disappear. | **PENDING** |
| R11 | Scroll through a long KKLT property list | Scroll stays responsive; no blank/stale recycled rows. | **PASS (Phase 1 historical); PENDING (critical build)** |
| R12 | Switch material while scrolled | Rows, values and labels belong to the new material. | **PENDING** |
| R13 | Switch shader | Compatibility/filtering rebuilds once and no stale rows remain. | **PENDING** |
| R14 | Shader without schema-2 metadata | All compatible non-Hidden legacy properties remain visible. | **PENDING** |
| R15 | Schema-1 manifest with categories/ranges/defaults/types | Behavior matches the official/upstream editor. | **PENDING** |
| R16 | Schema-1 Hidden property | It remains hidden without any mode-dependent behavior. | **PENDING** |
| R17 | Schema-2 optional-feature matrix: flat, Category-only, Subcategory, and `ShowIf` | Each feature appears only when explicitly declared; hierarchy alone implies no condition, shader change or material write. | **PENDING** |
| R18 | Future `SchemaVersion`, malformed version and unknown Editor | Future positive versions warn and preserve the known schema-2 subset; malformed versions use schema 1; editor fallback writes nothing. | **PENDING** |
| R19 | Malformed condition/enum/vector metadata | Safe fallback, no crash, no automatic material edit. | **PENDING** |
| R20 | Search a property by DisplayName and internal Name | The correct row appears regardless of stored Category/Subcategory collapse. | **PENDING** |
| R21 | Search for a property whose `ShowIf` is false | It appears disabled with the blocking condition; search does not change the condition or material. | **PENDING** |
| R22 | Clear a hierarchy-spanning search | Stored Category/Subcategory collapse returns unchanged. | **PENDING** |
| R23 | Modify a Float, then hide it through explicit `ShowIf` or collapse | Its value/rendering persists and no reset occurs. | **PENDING** |
| R24 | Inspect ordinary Boolean/Keyword rows in normal, Mixed and modified states | Their normal property rows retain existing value, Timeline and Reset semantics inside or outside a Subcategory. | **PENDING** |
| R25 | Open, close and recycle a Subcategory containing condition source and dependent rows | Folding changes only presentation; ordinary properties retain values, state and callbacks after recycling. | **PENDING** |
| R26 | Load duplicate hierarchy IDs with conflicting labels/order metadata | One deterministic first declaration wins, all properties remain accessible, and warnings identify the conflict. | **PENDING** |
| R27 | Enum choose every option | Exact declared Float is written and persists. | **PENDING** |
| R28 | Enum with unknown current Float | Unknown/mixed presentation does not write until explicit selection. | **PENDING** |
| R29 | Enum reset | Original Float is restored through the normal repository. | **PENDING** |
| R30 | Vector2 edit X/Y | X/Y update; Z/W are preserved; color picker never opens. | **PENDING** |
| R31 | Vector3 edit X/Y/Z | Edited components update; W is preserved. | **PENDING** |
| R32 | Vector4 edit/reset | X/Y/Z/W save and reset together. | **PENDING** |
| R33 | Float-backed Toggle | Exact configured OffValue/OnValue is written as Float. | **PENDING** |
| R34 | Keyword toggle | Existing keyword path still enables/disables the keyword, separate from Toggle Float. | **PENDING** |
| R35 | `ShowIf` parent edit | Dependent row appears/disappears immediately after the relevant edit, preserving its value. | **PENDING** |
| R37 | Tooltip catalog and inline Tooltip | Existing hover interaction works; no permanent help paragraph appears. | **PASS (Phase 1 historical); PENDING (critical build)** |
| R38 | Per-property reset for legacy/new editors | Uses normal repository and returns to original. | **PENDING** |
| R39 | Existing material Copy/Paste | Compatible values paste; shader is not silently changed. | **PENDING** |
| R40 | Existing texture import/export and reset | Behavior is unchanged. | **PENDING** |
| R41 | Save/reload character card | Float, Enum, Toggle, Vector, Color, Keyword and textures persist. | **PENDING** |
| R42 | Save/reload coordinate | Same values persist; hierarchy collapse is not material data. | **PENDING** |
| R43 | Save/reload Studio scene | Same values persist; hierarchy collapse is not material data. | **PENDING** |
| R44 | Save unedited card/coordinate/scene | Material Editor does not add data or change file size because the UI was opened/switched. | **PENDING** |
| R45 | Clothes, hair, accessories, body and face materials | Shared UI works across Maker targets. | **PENDING** |
| R46 | Studio items, maps and projectors where supported | Shared UI works and projector controls remain intact. | **PENDING** |
| R47 | MaterialAPI-dependent plugins | Existing consumers load and operate without missing members. | **PENDING** |
| R48 | Extension API plugins | Existing 1.1-style consumers work; 1.2 consumers gate features with `Supports`. | **PENDING** |
| R49 | HairShadowColorControl | Loads and continues to edit its supported values. | **PENDING** |
| R50 | Third-party shader manifests | Schema-1 properties remain accessible and editable. | **PENDING** |
| R51 | KKLT without hierarchy metadata | Its complete compatible property set remains available through the legacy flat presentation. | **PENDING** |
| R52 | KKLT with optional hierarchy metadata | Categories/Subcategories organize the same properties without adding shader-specific behavior to Material Editor. | **PENDING** |
| R53 | Window density at normal/minimum supported width | Search/status and hierarchy headers remain compact and do not obscure existing controls. | **PENDING** |
| R54 | Fresh `LogOutput.log` review | Modified build/version loads once; no new exception or repeated warning spam. | **PASS** |
| R55 | Open and type in representative Vector2/3/4 rows | Every active component has a visible editable field and no control overlaps Reset. | **PENDING** |
| R56 | Open representative Enum/Dropdown captions and option lists | Caption and options remain readable at the configured UI scale; long labels truncate instead of shrinking below 12. | **PENDING** |
| R57 | Import the maximum accepted PNG and Radiance HDR equirectangular Cubemaps in Maker and Studio | Disk read/hash occur off the Unity thread; HDR decode and panorama projection advance incrementally; the indivisible main-thread PNG decode/GetPixels32 phase is timed and remains acceptable on each target; HDR values above 1 survive in the material; completion/error is logged once. | **PENDING** |
| R58 | Export a direction-labelled, color-coded readable Cubemap and the same content through the non-readable GPU path | All six face orientations and colors match between CPU and GPU exports on every supported target. | **PENDING** |
| R59 | Re-import each Cubemap export | Export/re-import preserves orientation and SDR color closely enough for the shader use; no face is mirrored, rotated or gamma-shifted unexpectedly. Radiance HDR import remains HDR in the material, while its documented PNG export clips values above 1. | **PENDING** |
| R60 | Profile maximum accepted Cubemap import/export | Peak managed/native/GPU memory remains within the documented conversion budget and returns after leases/temporary resources are released. | **PENDING** |
| R61 | Duplicate/reload an object after changing renderer/material traversal order without changing its material count | Cubemap reset restores each original by stable renderer path/component/slot identity, or rejects an ambiguous remap; it never restores by list index. | **PENDING** |
| R62 | Import an invalid Texture2D and a valid Texture2D in Maker and Studio | Invalid input is not reported as existing/successful; valid input updates Changed/Exists only after the controller completion callback. | **PENDING** |

## Performance observations to collect

| ID | Measurement | Acceptance | Status |
| --- | --- | --- | --- |
| P01 | Leave window idle for at least 60 seconds with profiler/log observation | No XML parsing, list rebuild, condition scan or recurring log work. | **PENDING** |
| P02 | Fold one Category or Subcategory once | At most one controlled presentation/VirtualList rebuild. | **PENDING** |
| P03 | Edit a property not referenced by a condition | No condition-driven full refresh. | **PENDING** |
| P04 | Edit a condition source | One prompt refresh; dependent rows update. | **PENDING** |
| P05 | Repeated long-list scrolling | No material allocation spike attributable to hierarchy headers or recycled rows. | **PENDING** |
| P06 | Maximum accepted Cubemap cache miss | Decode timing and incremental sampling metrics are recorded; no single projection batch causes a visible multi-second stall. | **PENDING** |
| P07 | Repeat the same Cubemap import | It is a cache hit, performs no second projection, and leaves one correctly owned controller lease. | **PENDING** |

## Known Phase 1 limitations to verify, not misreport

- Phase 1 does not add a guaranteed Timeline Vector interpolation type. Existing
  Color Timeline behavior must remain intact; native Vector Timeline behavior is
  **N/A** unless a compatible registration is separately supplied and tested.
- Mixed-value editor presentation has semantic support, but broad multi-material
  batch editing is not a Phase 1 claim.
- Automated tests guard the production Vector model/save/load/migration wiring,
  but they do not execute Koikatsu's MessagePack/ExtendedSave lifecycle. The
  actual card, coordinate and scene roundtrips remain runtime tests R41-R44.
- The official upstream plugin does not apply the fork's native Vector key and
  may discard it when resaving; do not alternate writers without a backup.
- This compatibility build writes texture bytes in the original bundled
  version-1 format. It can read bundled, `LOCAL_` and `DEDUPED_` version-2
  texture data, but deliberately does not create new local/deduplicated saves
  because those writers require KKAPI APIs absent from the target installation.
- Property/category clipboard actions, category reset, Modified Only, presets and
  favorites are roadmap work.

## Restore after testing

1. Close Koikatsu and CharaStudio.
2. Run `restore-materialeditor-backup.bat` against the verified backup.
3. Confirm the restored DLL and native library hashes match the backup.
4. Confirm no second loadable Material Editor DLL remains.
5. Preserve test logs separately; do not delete user cards, scenes, config, or
   Material Editor data.

Runtime completion must record who tested, when, exact executable, representative
assets, result, and log path. The final Koikatu Maker and CharaStudio load gates,
targeted selector/tooltips, KKLT sphere render invariance and viewport-context
checks are recorded. Unmarked visual cases, save/load roundtrips and broader
performance criteria remain **PENDING** until their individual entries are
executed.
