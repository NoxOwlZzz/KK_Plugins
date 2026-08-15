# Material Editor feature-gap analysis

This audit describes the upstream behavior at commit
`1502cced6e372c6c328703eb7b7caea589bff4a7`, before the Basic/Advanced work.
It is a source audit, not evidence that the current branch has passed runtime or
visual testing.

The target is the existing GPL-3.0 Material Editor, not a companion editor. The
original plugin GUID, persistence keys, card/coordinate/scene formats and public
API remain the compatibility boundary.

## Audit provenance and recovery point

| Item | Recorded value |
| --- | --- |
| Upstream repository | `https://github.com/IllusionMods/KK_Plugins.git` |
| Initial branch/status | `master`, clean before task changes |
| Original commit | `1502cced6e372c6c328703eb7b7caea589bff4a7` |
| Safety branch | `backup/material-editor-before-ui-overhaul-20260807-2005` |
| Working branch | `feature/material-editor-basic-advanced-ui` |
| Installed original | `KK_MaterialEditor.dll` 3.12 / assembly 3.12.0.0 |
| Verified external backup | `D:\Games\Koikatsu\BepInEx\PluginBackups\MaterialEditor\20260807-195335` |

The backup contains only the installed Material Editor DLL, its normal
`libwebp.lib`, the relevant BepInEx config and `BACKUP_INFO.txt`. It is outside
the loadable plugin tree. No shared BepInEx, KKAPI, Unity, game or unrelated
plugin DLL was classified as Material Editor-owned.

## Target runtime compatibility

The target installation loads KKAPI 1.42.2, Extended Save 20.0 and Sideloader
20.0. Upstream Material Editor 4.0.3 builds against newer local/deduplicated
texture APIs that are absent from KKAPI 1.42.2, so lowering dependency
attributes alone is not safe.

The KK project is compiled against the exact installed dependency generation.
Its compatibility texture handler writes the original bundled version-1
`TextureDictionary` representation and retains read support for bundled,
`LOCAL_` and `DEDUPED_` version-2 data. No shared dependency DLL is packaged or
deployed. A binary-reference audit against the installed assemblies is part of
the delivery evidence in `MANUAL_TESTS.md`.

## Upstream inventory

| Area | Upstream finding |
| --- | --- |
| Target/runtime | .NET Framework 3.5, Unity 5.6-era Koikatsu runtime |
| KK output | `KK_MaterialEditor.dll` |
| Plugin GUID | `com.deathweasel.bepinex.materialeditor` |
| Shared projects | `MaterialEditor.Base`, `MaterialEditor.Core`, `MaterialEditor.Core.Maker`, `MaterialEditor.Core.Studio`, `UIUtility`, `Shared`, and `Shared.TextureContainer` |
| UI list | A pooled `VirtualList`; only presentation models should reach it |
| Property organization | Shader manifest properties, optional categories, user sorting options, and an uncategorized fallback |
| Search | Renderer/material search plus property search when the query starts with `_` |
| Categories | Foldable categories and a category navigator already exist |
| Tooltips | Existing hover tooltips and shader tooltip catalogs already exist |
| Persistence | Repository/edit-service paths for character, coordinate, and Studio data |
| Copy/reset | Material-level copy/paste and per-property reset exist; category/property clipboard operations do not |
| Public extension surface | Semantic descriptor/editor registry and edit-service facade; UI internals are intentionally not public |
| Tests | `tests/MaterialEditor.MetadataTests` plus PublicApiAnalyzers in the API project |

## Gap table

“Exists” and “Partial” refer to the upstream baseline, not the unverified Phase 1
working tree.

| Function | Already exists | Partial | Missing | Applicable in Koikatsu runtime | Requires UnityEditor | Priority | Proposed phase |
| --- | :---: | :---: | :---: | :---: | :---: | --- | --- |
| Material copy/paste of persisted edits | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Property reset to original | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Property copy/paste | — | — | Yes | Yes | No | Medium | Phase 2 |
| Category copy/paste | — | — | Yes | Yes | No | Medium | Phase 2 |
| Category reset | — | — | Yes | Yes | No | Medium | Phase 2 |
| Reset all modified material values | — | — | Yes | Yes | No | Medium | Phase 2; reuse repository operations |
| Search | Yes | — | — | Yes | No | Preserve | Phase 1 extension |
| Search by display and internal property name | — | Yes | — | Yes | No | High | Phase 1 |
| Hidden Advanced-result count and Show action | — | — | Yes | Yes | No | High | Phase 1 |
| Category navigator | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Foldable categories | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Category header toggle | — | — | Yes | Yes | No | Medium | Phase 2 after manifest contract review |
| Category modified count/actions menu | — | — | Yes | Yes | No | Medium | Phase 2 |
| Basic/Advanced mode | — | — | Yes | Yes | No | Critical | Phase 1 |
| Per-property `UiLevel` | — | — | Yes | Yes | No | Critical | Phase 1 |
| Advanced-change count/navigation | — | — | Yes | Yes | No | High | Phase 1 |
| Empty-category removal after filtering | — | Yes | — | Yes | No | High | Phase 1 |
| Row virtualization | Yes | — | — | Yes | No | Critical | Preserve |
| Direct manifest tooltips | — | Yes | — | Yes | No | High | Phase 1, layered onto catalogs |
| Friendly display names | — | — | Yes | Yes | No | High | Phase 1 |
| Explicit category/property ordering | — | Yes | — | Yes | No | High | Phase 1 |
| Float editor | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Color editor/color picker | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Texture editor and transform controls | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Keyword toggle | Yes | — | — | Yes | No | Preserve | Phase 1 regression coverage |
| Float-backed toggle distinct from keyword | — | — | Yes | Yes | No | High | Phase 1 |
| Float-backed enum/dropdown | — | — | Yes | Yes | No | High | Phase 1 |
| Vector2/3/4 controls without color picker | — | Yes | — | Yes | No | High | Phase 1 |
| Vector persistence distinct from color | — | — | Yes | Yes | No | Critical | Phase 1, backward-compatible addition |
| Mixed-value presentation | — | — | Yes | Yes | No | Medium | Phase 1 editor contract; runtime aggregation remains limited |
| Simple `ShowIf` conditions | — | — | Yes | Yes | No | High | Phase 1 |
| Modified visual indicator | — | Yes | — | Yes | No | High | Phase 1 consistency work |
| Modified-only filter | — | — | Yes | Yes | No | Medium | Phase 2 |
| Favorites/pinned properties | — | — | Yes | Yes | No | Low | Phase 3 |
| Presets and a preset library | — | — | Yes | Yes | No | Medium | Phase 3 |
| Per-shader custom layouts | — | — | Yes | Yes | No | Medium | Phase 3 |
| Shader-author manifest validation | — | — | Yes | Yes | Optional | Medium | Phase 3; standalone tool preferred |
| Asset import/reimport settings | — | — | Yes | No | Yes | Out of scope | Do not add to runtime plugin |
| Shader compilation/variant generation | — | — | Yes | No | Yes | Out of scope | Do not add to runtime plugin |
| Texture baking or source-asset writes | — | — | Yes | No | Yes | Out of scope | Do not add to runtime plugin |

## lilToon UX comparison

lilToon is used only as a UX reference. No inspector code is copied. In
particular, the official changelog records that the historical simple settings
were removed in 1.10; the useful lesson is the separation of common and
technical settings, not a requirement to reproduce a particular lilToon
release.

| lilToon idea | Classification for Material Editor | Reason |
| --- | --- | --- |
| Compact common/advanced selection | Directly applicable | Presentation-only filtering is useful at runtime. |
| Foldable functional sections | Directly applicable | Material Editor already has categories and virtualization. |
| Controls revealed by a feature toggle | Applicable with adaptation | Use bounded `ShowIf` metadata and repository-backed values. |
| Enum dropdowns and float toggles | Directly applicable | They map to shader floats without changing persistence keys. |
| Vector fields | Applicable with adaptation | Preserve hidden components and keep Vector distinct from Color. |
| Organized render settings | Directly applicable | Requires explicit shader metadata; no name heuristics. |
| Compact warnings and hover help | Applicable with adaptation | Reuse Material Editor's tooltip policy; avoid permanent help paragraphs. |
| Copy/paste and reset by module | Applicable with adaptation | Reuse `CopyContainer` and repositories; category actions are deferred. |
| One-click quick configuration buttons | Applicable with adaptation | Model as explicit, reviewable presets later; never infer destructive shader changes. |
| Textures organized by function | Directly applicable | Use authored categories/groups while retaining the existing texture editor. |
| Presets | Applicable with adaptation | Runtime-safe in principle, but deliberately deferred. |
| Stencil writer/reader presets | Applicable with adaptation | Explicit data presets are possible later; raw fields remain metadata-driven. |
| AssetDatabase, ShaderUtil, import/bake/build tooling | Not applicable | These require UnityEditor and must not enter the Koikatsu runtime plugin. |

Primary references:

- [Official lilToon documentation](https://lilxyzw.github.io/lilToon/)
- [Official lilToon source](https://github.com/lilxyzw/lilToon)
- [Official changelog](https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/CHANGELOG.md)
- [Official stencil documentation](https://lilxyzw.github.io/lilToon/ja_JP/advanced/stencil.html)
- [Official shader property declarations](https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/Shader/lts.shader)

## Phase 1 conclusions

The clean integration point is the existing presentation pipeline. Static
manifest organization remains cached; compatibility, conditions, UI mode and
search are evaluated during controlled list rebuilds. Advanced properties are
removed before row creation, so the existing pooled `VirtualList` never binds a
hidden Advanced row in Basic mode.

The upstream DLL and this fork are mutually exclusive at runtime:

> Do not use this build together with the official Material Editor DLL.

This project remains GPL-3.0. Original IllusionMods/KK_Plugins authorship,
copyright, credits and history are retained. Phase 1 changes by NightOwlZzz /
Owl are additional credits and do not replace upstream attribution.
