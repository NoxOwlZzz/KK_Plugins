# Material Editor UI structural validation

## Evidence boundary

This report describes automated, clean-build, binary-audit, deployment and
editor-closed load-smoke evidence for commit `8c36e8df`. The metadata
executable links selected production policies and uses source contracts/stubs
for other Unity-facing behavior. The smoke launches loaded the plugin but kept
Material Editor closed. None of these gates renders the editor, clicks real
uGUI controls, saves a card/coordinate/scene or measures Unity frame time. All
58 visual-review fields therefore remain **PENDING** in
`USER_VISUAL_REVIEW_CHECKLIST.md`.

## Recorded gates

| Gate | Result at `8c36e8df` | Meaning |
| --- | --- | --- |
| Metadata/regression executable | **PASS** | All registered Phase 1-10 and optimization guards completed |
| Clean API/PublicApiAnalyzers | **PASS** | Shipped and unshipped public-API hashes remained unchanged |
| Clean all-target Release matrix | **PASS**, 0 errors | AI, EC, HS2, KK, KKS and PH completed from the exact UI source |
| `git diff --check` for product delivery | **PASS** | No whitespace-error diff |
| Harness comparison against `pre-three-panel-ui.json` | **PASS**, synthetic only | 23/23 scenarios and 21/21 invariants passed with the same semantic fingerprint; this harness does not link/render the real UI |
| Binary reference/release audit | **PASS** | 16/16 AssemblyRefs, 432/432 TypeRefs and 2,302/2,302 MemberRefs resolved; Release guards passed |
| Restore verification | **PASS** | The immediate pre-UI backup was accepted by the restore script in read-only `--verify-only` mode |
| Deployment and installed-byte verification | **PASS** | Only the DLL/native runtime pair was deployed; installed and build DLL bytes are identical |
| Maker/Studio load smoke of final redesigned binary | **PASS**, editor closed | Base + Maker and base + Studio load lines were present with one Chainloader startup and zero attributed errors per log |
| Maker/Studio visual interaction | **PENDING** | Not exercised by these gates |
| Save/load and manual persistence | **PENDING** | Not exercised by these gates |
| Runtime profiler/FPS/GPU memory | **PENDING** | Not exercised by these gates |

The focal KK build reported 284 warnings: 8 `CS1572`, 64 `CS1573`, 211
`CS1591`, and one `NU1504`. The first 283 are existing XML-documentation
warnings. `NU1504` is the pre-existing duplicate
`Microsoft.Unity.Analyzers` reference from `Directory.Build.props` (`1.*`) and
`src/MaterialEditor.KK/KK.MaterialEditor.csproj` (`1.25.0`). No warning points
to a Phase 2-10 UI file. This report does not relabel warnings as errors or
claim a warning-clean KK build.

## Final build and deployment evidence

| Item | Exact final evidence |
| --- | --- |
| UI source | `8c36e8df` |
| Shipped API hash | `F844B75DEB8025FEBEBF4E6FFC8700903FAAF4B04E0952221EB0D63031268367` |
| Unshipped API hash | `ED5FAEB493FE464CC96EE7CC81322C72575557A7EFCC73D0F050E04D621B4023` |
| Build/installed DLL | 1,050,624 bytes; SHA-256 `B1B02AEED5FA9B86D2317855FA419BC59C08A0DA9650030D6AC8F8EE12154CFC`; file version 4.0.3 |
| Final DLL MVID | `DB6524AC-A227-4BEC-9A88-6C3759ADA957` |
| Documentation XML | 421,700 bytes; SHA-256 `7275F96E4FE99A1DE8BF6DC23A64A942399EC48790A01F1D76BAC54783D1F1BB` |
| Native library | 604,672 bytes; SHA-256 `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` |
| Package | 872,982 bytes; SHA-256 `18A50C367B3FAA5B59C665A1D81BF4DADF17A95BB079EE2B8D1E6F0C03762826` |
| Final synthetic report | 32,241 bytes; SHA-256 `C06A755CD9E011519BEFC28F4FF34C02774ABD7F90EBE55B6CEB3D91E16059AD` |

The deployment created
`D:/Games/Koikatsu/BepInEx/PluginBackups/MaterialEditor/three-panel-ui/20260809-023401-before-three-panel-ui`
before replacing the two runtime files. Its `BACKUP_INFO.txt` SHA-256 is
`0E90A898FBAAEF92B6960029655EB30B1BCDAC0DC08DFDA1BB455BEE6B5D340E`.
The active configuration stayed byte-identical before deployment, after
deployment and after both smoke launches: 46 settings, SHA-256
`6A483723C8B143B678F719BAA8A726FEC50CBB0D4CA94F288052193FA25C92B6`.

The retained editor-closed load logs are:

- `RuntimeValidation/Koikatu-plugin-load.log`: 19,511 bytes, SHA-256
  `FC7B573C9ED493BE793FB753002A778B2B0892DD78E757BC4C24F1C3C4E7A377`;
  two Material Editor loading lines (base + Maker), one complete Chainloader
  startup and zero attributed errors.
- `RuntimeValidation/CharaStudio-plugin-load.log`: 22,692 bytes, SHA-256
  `18D0D3480BB73CCC956523D3963A0C01E29A89AE682C31F50A0E1E50C52A3428`;
  two Material Editor loading lines (base + Studio), one complete Chainloader
  startup and zero attributed errors.

Material Editor remained closed. No screenshot, card, coordinate or scene was
created or loaded, so the smoke is deliberately not visual, interactive or
persistence acceptance.

## Phase delivery and contract coverage

| UI phase | Commit | Delivered source structure | Automated guard |
| --- | --- | --- | --- |
| 1 - baseline/design | `c51828c7` | Frozen pre-redesign inventory, three-panel design and baseline | Baseline/performance precondition documents |
| 2 - tokens/common dark chrome | `50c67539` | Internal fixed palette, metrics, typography, factory/style seam | `UiThemeTokenContractTests`, `UiDarkThemeContractTests` |
| 3 - top bar | `25d1d8df` | Stable two-row top bar, shader context, one reusable global menu, Studio slot | `UiTopBarPhaseThreeContractTests` |
| 4 - left navigation | `b273a12f` | Category panel/rail, viewport-following selection, permanent entry listeners | `UiLeftNavigatorPhaseFourContractTests` |
| 5 - right panels | `76421d11` | Renderers and Materials visible together by default, real counts/filters, independent folds, global rail, Rename overlay | `UiRightPanelsPhaseFiveContractTests` |
| 6 - row actions | `90e2e539` | One Canvas-level four-action row menu with owner/generation lease | `UiRowActionsPhaseSixContractTests` |
| 7 - category headers | `449b0140` | One full-surface action, passive glyph/name, fixed 22-unit row | `UiPropertyCategoryPhaseSevenContractTests` |
| 8 - property rows | `d854aa90` | Compact reset glyph, explicit float range policy, full-name tooltips, pooled layout restore | `UiPropertyRowsPhaseEightContractTests` |
| 9 - truthful feedback | `e6f5ba72` | Reusable central/right empty states from exact counts; deliberate warning/footer omission | `UiFeedbackPhaseNineContractTests` |
| 10 - responsive layout | `8c36e8df` | Real-Canvas policy, bounded drag, visual auto-collapse and resizable high-water VirtualList | `UiResponsivePhaseTenContractTests` |

## Structural invariants currently guarded

- Fifteen functional Reset controls/templates changed caption from `Reset` to
  `↺` while preserving their existing changed/interactable callbacks. The
  shader Dropdown's semantic `Reset` option remains textual.
- Nine existing actions moved into shared menus: three global view actions,
  two Renderer export actions and four Material actions. The window owns two
  reusable popup surfaces; it does not allocate a popup per row.
- The three surfaces use one programmatic Canvas; no authored Material Editor
  prefab or second theme API was introduced.
- Main, side and collapsed footprints are ordered and finite for the tested
  scale, window, side-width and 16:9/4:3/21:9 matrices.
- The center is at least 500 by 138 when the real Canvas can satisfy it. At
  height 138, the conservative selection viewport is 22.5 units and the
  Rename viewport is exactly 22 for a 22-unit row.
- Collapse and responsive auto-collapse are view-only and do not write
  material, card, coordinate, scene or configuration data.
- `VirtualList` retains a clean inactive template, caps the high-water cache at
  44 views, adds one overscan slot, and separates active capacity from retained
  cache capacity. Shrinking releases stale models/listeners.
- Search still has one immediate UI callback feeding the existing coalesced
  presentation path; no new polling refresh was added.
- The global menu and row-action menu are mutually exclusive reusable Canvas
  surfaces. A row action validates owner, binding generation, active state and
  interactability before invocation.
- Row/category/selection listeners are installed once or replaced through the
  existing listener scope; pooled bind/release paths clear stale context.
- Empty-state overlays are precreated passive viewport children, never
  per-rebuild GameObjects and never children of virtualized content.
- Unsupported aggregate warnings, global modified totals and a footer remain
  absent rather than fabricated.

## Reproduction

Metadata/regression gate:

```powershell
dotnet run --project tests\MaterialEditor.MetadataTests\MaterialEditor.MetadataTests.csproj --no-restore
```

Full clean delivery gate:

```powershell
cmd /c build-materialeditor-three-panel-ui.bat
cmd /c build-materialeditor-three-panel-ui.bat --verify-only
```

The final synthetic comparison and its exact hashes are documented in
`UI_PERFORMANCE_RESULTS.md`. It is a post-source-redesign algorithmic gate, not
a Unity UI benchmark.

## Pending evidence

- Rendered screenshots in Maker and Studio for all 58 review cases.
- Interaction logs with the editor open, including dropdown, color picker,
  menus, scrolling, filters, collapse, resize and Rename.
- Card/coordinate/scene roundtrips after actual edits and after view-only use.
- Runtime frame-time, allocation, native/GPU memory and long-session evidence.
- Keyboard-only navigation, focus visibility, glyph rendering, high-DPI and
  non-16:9 visual review.
