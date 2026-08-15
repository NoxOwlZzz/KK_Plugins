# Material Editor UI baseline

## Scope and evidence boundary

This is the Phase 1, pre-redesign baseline for the
`feature/material-editor-minimal-three-panel-ui` branch. It describes the
optimized UI at commit `525a8c4bd5b11db44871fd54bf5f1fd4b5bbd238` before any
visual source change.

The inventory is based on source inspection, the clean build record in
`UI_BACKUP_INFO.md`, and the latest deterministic report at
`bin/build/materialeditor-performance-final.json`. Material Editor was not
opened to inspect its appearance, no game screenshot was produced, and no
runtime visual result is claimed. Counts labelled **source-derived** describe
the programmatic hierarchy; counts labelled **synthetic** come from the .NET 8
harness and are not Unity frame, retained-memory, or rendered-layout results.

## Baseline identity and gate

| Field | Baseline |
| --- | --- |
| Source branch | `feature/material-editor-critical-optimization` |
| Working branch | `feature/material-editor-minimal-three-panel-ui` |
| Source commit | `525a8c4bd5b11db44871fd54bf5f1fd4b5bbd238` |
| Safety branch | `backup/material-editor-before-three-panel-ui-20260808-205924` |
| Clean optimized build | PASS, `build-materialeditor-optimized.bat --clean` |
| Metadata/regression executable | PASS |
| Performance baseline comparison | PASS |
| MaterialEditor.API / PublicApiAnalyzers | PASS |
| Release targets | AI, EC, HS2, KK, KKS, and PH PASS with zero errors |
| KK DLL | `1,008,640` bytes, assembly `4.0.3.0` |
| DLL SHA-256 from the final pre-redesign sync | `804A70B90EE8F0429B7B2B7739F952D5B72DC95CD67865720E441BFCF4C5EB97` |
| Runtime visual validation | PENDING; prohibited for Codex in this phase |
| Full persistence/lifecycle roundtrips | PENDING user verification, as listed in `PRECONDITION_REPORT.md` |

The user explicitly authorized the redesign after the exact optimized build
was synchronized locally. Pending runtime cases remain pending; authorization
does not convert them into executed tests.

## Existing hierarchy

The UI is created at runtime; there is no authored Material Editor UI prefab.
The stable hierarchy is approximately:

```text
MaterialEditorCanvas (Canvas, CanvasScaler, GraphicRaycaster)
├─ Panel / MainPanel (Image, Outline)
│  ├─ Draggable / HeaderPanel (Image, MovableWindow)
│  │  ├─ Nametext
│  │  ├─ CategoryNavigatorButton
│  │  ├─ Filter (InputField)
│  │  ├─ PersistSearch (Toggle)
│  │  ├─ PersistSearchText
│  │  ├─ CollapseAllCategoriesButton
│  │  ├─ CloseButton
│  │  │  ├─ x1
│  │  │  └─ x2
│  │  └─ ViewListButton
│  ├─ MaterialEditorModePanel
│  │  ├─ MaterialEditorBasicModeButton
│  │  ├─ MaterialEditorAdvancedModeButton
│  │  ├─ MaterialEditorAdvancedChangesButton
│  │  ├─ MaterialEditorHiddenAdvancedResultsText
│  │  └─ MaterialEditorShowAdvancedResultsButton
│  ├─ MaterialEditorWindow (central ScrollRect)
│  │  └─ Viewport / Content (VerticalLayoutGroup, ContentSizeFitter)
│  │     └─ viewport-sized pool of ListEntry RowViews
│  │        └─ 18 mutually exclusive row-family panels
│  ├─ CategoryNavigatorPanel (left, conditional)
│  │  ├─ CategoryNavigatorMaterial
│  │  ├─ CategoryNavigatorShaderHeader
│  │  │  └─ CategoryNavigatorShader
│  │  └─ CategoryNavigatorScrollView
│  │     └─ dynamic CategoryNavigationEntry rows
│  ├─ RendererListPanel (right/top, initially hidden)
│  │  ├─ title
│  │  ├─ filter
│  │  └─ independent ScrollRect with dynamic toggle rows
│  ├─ MaterialListPanel (right/bottom, initially hidden)
│  │  ├─ title
│  │  ├─ filter
│  │  └─ independent ScrollRect with dynamic toggle rows
│  └─ MaterialRenameListPanel (alternate right/top, initially hidden)
│     ├─ title and filter
│     ├─ independent ScrollRect with renderer toggle rows
│     ├─ MaterialEditorRenameField
│     ├─ MaterialEditorRenameButton
│     └─ MaterialEditorRenameMaterial
└─ TooltipPanel (one shared rendered tooltip surface)
   └─ ToolTipText
```

The category navigator is anchored immediately to the left of the main panel.
The renderer and material lists are anchored immediately to its right and split
50/50 vertically. The current default session leaves both right lists hidden;
one header button toggles them together. The rename workflow temporarily uses a
separate right-side panel instead of the normal two lists.

## Architecture and ownership

| Layer | Existing owner | Baseline responsibility |
| --- | --- | --- |
| Plugin/session | `MaterialEditorUI`, `MaterialEditorSessionState` | Current target/data, filter, mode, selection, collapse keys, close/reopen lifetime |
| Window composition | `MaterialEditorWindowView` | Creates Canvas, header, mode strip, central list, side panels, and rename panel |
| Selection | `MaterialEditorSelectionController` | Renderer/material multi-selection, filters, side-panel visibility, rename context |
| Presentation | `MaterialEditorPresenter` | Filters renderers/materials/projectors and builds the final row sequence |
| Material sections | `MaterialSectionPresenter` | Material, shader, render queue, category, property, condition, and action models |
| Category navigation | `CategoryNavigatorView` | Tracks the central viewport section, category highlight, jump, and collapse |
| Semantic rows | `RowModel` subclasses | Values, original values, Mixed fields where supported, and callbacks |
| Recyclable view | `RowView`, `RowBinder`, `ListenerScope` | Switches the active family and removes exact listeners |
| View construction | `RowViewFactory.*`, `MaterialEditorControlFactory` | Builds one complete row template programmatically |
| Virtualization | `VirtualList` | Creates `ceil(initial viewport height / 22)` views, rebinds them, preserves anchor |
| Styling/layout | `MaterialEditorStyles`, `MaterialEditorLayout`, `RowLayoutCatalog` | Partial tokens, typography normalization, family column widths, diagnostics |
| Tooltips | `TooltipManager`, `Tooltip`, `ShaderHintUnderline` | One popup surface, hover/Shift policy, bounds clamping, shader-hint underline |
| Writes | `MaterialEditService` through repositories | All persisted edits, resets, copy/paste, shader/texture/property operations |

This is a presenter plus programmatic view architecture, not strict MVVM. The
presenter remains the authority for Basic/Advanced membership, search,
`ShowIf`, category ordering, provider rows, and modified counts.

## Programmatic template and control budget

The following counts are source-derived and define precisely what is being
counted.

### Window-level controls constructed once

| Kind | Count | Included controls |
| --- | ---: | --- |
| Buttons | 9 | Four header actions, four mode/status actions, Rename |
| Input fields | 5 | Main filter, three side-list filters, Rename value |
| Toggles | 1 | Persist Search |
| ScrollRects | 5 | Central, categories, renderers, materials, rename |
| Shared tooltip popup | 1 | One `TooltipPanel`; eligible targets have lightweight `Tooltip` behaviors |

The 15 Buttons/InputFields/Toggles are the persistent interactive-widget count.
Dynamic category/selection entries, dropdown popup items, scrollbars, and the
central row pool are excluded. Some persistent widgets are inactive until
their state is relevant.

### One pooled `RowView`

Every pooled `RowView` contains all 18 family panels, with only one visible at
a time. The template contains 60 source-derived interactive controls across
those families and 15 buttons whose current caption is `Reset`. This does not
mean 60 controls are visible per row; inactive families have alpha zero and do
not block raycasts. It does mean every geometry/style change must respect the
cost of cloning the complete template.

| Family | Interactive controls in template |
| --- | ---: |
| Renderer | 3 |
| Renderer Enabled | 2 |
| Renderer Shadow Casting Mode | 2 |
| Renderer Receive Shadows | 2 |
| Renderer Update When Off-Screen | 2 |
| Renderer Recalculate Normals | 2 |
| Material | 5 |
| Shader | 5 |
| Shader Render Queue | 2 |
| Property Category | 1 |
| Texture | 4 |
| Texture Offset/Scale | 5 |
| Color | 7 |
| Float/Range | 4 |
| Keyword | 2 |
| Enum/Dropdown | 3 |
| Vector | 6 |
| Float-backed Toggle | 3 |
| **Total** | **60** |

## Current metrics and palette

| Metric | Current value |
| --- | ---: |
| Margin | 5 px-equivalent |
| Header/mode-strip height | 20 each |
| Row height | 22 |
| Category navigator width | 150 |
| Side-list width config default/range | 180 / 100-500 |
| Main width config default | 0.33 of reference canvas width |
| Main height config default | 0.30 of reference canvas height |
| UI scale config default/range | 1.75 / 1-3 |
| Default font | Built-in `Arial.ttf` through `UIUtility` |
| Default font size | 16 |
| Dropdown font/minimum | 16 / 12 |
| Vector component font/minimum | 16 / 12 |
| Row padding | 1 on each side |

Current colors are light and role-based but visually coarse: white main panel,
gray header, `0.42` gray side panels, 60%-opaque white rows, orange renderer
headers, green material headers, purple category headers, and a 30%-opaque
black changed overlay. The main panel and category navigator have black
outlines. Unity `DefaultControls` sprites provide background, standard,
input-field, knob, checkmark, dropdown-arrow, and mask assets. The close glyph
is two procedural black lines; fold and Timeline affordances are text glyphs.
`Resources/MaterialEditorIcon.png` is not a row/icon registry.

## Baseline visual and semantic states

| State | Current representation |
| --- | --- |
| Basic / Advanced active | Active mode button becomes non-interactable |
| Modified | Row image changes from transparent to 30%-opaque black; Reset becomes interactable |
| Unmodified | Reset remains present but non-interactable |
| Enum Mixed | Label prefixed `Mixed ·`; dropdown caption `Mixed`; changed treatment active |
| Vector Mixed | Individual numeric components display `Mixed` |
| Float-toggle Mixed | Label prefixed `Mixed ·`; ordinary two-state toggle remains visually off |
| Hidden by `ShowIf` | Row is absent from the rebuilt presentation |
| Collapsed material/shader/category | Row membership changes after rebuild; text glyph shows state |
| Hidden Advanced search results | Compact text plus `Show` button in Basic mode |
| Advanced modifications | `Advanced changes: N` button navigates to first modified Advanced row |
| Unknown Enum | Synthetic option caption `Unknown (value)`; value is not changed automatically |
| Tooltip | Shared popup on hover; Shift chooses shader hint when available |

Explicit Mixed data exists for Enum, Vector components, and float-backed
Toggle extension editors. Float, Color, Boolean, Keyword, and Texture editor
contracts do not expose an equivalent general Mixed field. The redesign must
not visually invent Mixed for those families without a real semantic source.

## Synthetic performance checkpoint

Latest values below come from
`bin/build/materialeditor-performance-final.json`, generated
`2026-08-09T01:08:43.4274381Z`. The harness models UI work but does not execute
Unity or render the hierarchy.

| Scenario | Rows | Synthetic pool peak | P95 ms | P95 allocated | RowViews created / reused |
| --- | ---: | ---: | ---: | ---: | ---: |
| Simple rebuild | 25 | 25 | 0.0081 | 1,360 B | 500 / 0 |
| KKLT-like Basic | 182 | 32 | 0.0969 | 7,096 B | 480 / 2,250 |
| KKLT-like Advanced | 280 | 32 | 0.0895 | 7,528 B | 480 / 3,720 |
| KKLT-like search | 145 | 32 | 0.3210 | 9,224 B | 480 / 1,695 |
| 100-target equal | 973 | 32 | 2.3647 | 7,528 B | 192 / 5,646 |
| Longevity, 500 rebuilds | final 57 | 32 | 79.3577 | 3,866,008 B | 32 / 108,560 |

Longevity completed 130,560 binds and 130,560 unbinds, with 261,120 listener
registrations balanced by 261,120 removals, zero active listeners, and zero
provider registrations at completion. These are synthetic invariants. Runtime
pool size is derived from the initial Unity viewport height, not hard-coded to
32.

## Baseline risks carried into redesign

- The existing 22-unit row and 20-unit top strips are below the requested
  22-24 minimum interaction target in several places.
- Labels, inputs, buttons, and category bars rely heavily on Unity default
  surfaces and strong full-row colors.
- `Reset` consumes a fixed 40-unit column in 15 template families.
- Renderer/material/export/copy actions remain permanently expanded in their
  active rows.
- The header uses fixed offsets and can compress long text.
- Long-name truncation is mostly a consequence of rectangular clipping or
  best-fit text; a deliberate ellipsis policy is not present.
- The right pair is hidden by default and can only be toggled together.
- The left navigator can collapse, but its state is only in-memory and the
  collapsed form has no dedicated rail.
- Renderer and Materials cannot collapse independently.
- Side panels have config-driven width but no safe runtime splitter.
- `VirtualList` sizes its pool at initialization; a continuous splitter must
  not be introduced until viewport-resize behavior is explicitly handled.
- There is no reusable context-menu service, status bar, explicit empty-state
  view, or user-facing warning surface.
- Most diagnostics and metadata warnings go to the log.
- Click-through, focus, dropdown popup geometry, and visual contrast remain
  pending user runtime validation.

## Source references

Primary sources inspected for this baseline:

- `src/MaterialEditor.Base/UI/UI.WindowView.cs`
- `src/MaterialEditor.Base/UI/UI.CategoryNavigator.cs`
- `src/MaterialEditor.Base/UI/UI.SelectListPanel.cs`
- `src/MaterialEditor.Base/UI/UI.MaterialEditorPresenter.cs`
- `src/MaterialEditor.Base/UI/UI.MaterialSectionPresenter.cs`
- `src/MaterialEditor.Base/UI/UI.RowModel*.cs`
- `src/MaterialEditor.Base/UI/UI.RowView*.cs`
- `src/MaterialEditor.Base/UI/UI.RowBinder*.cs`
- `src/MaterialEditor.Base/UI/UI.VirtualList.cs`
- `src/MaterialEditor.Base/UI/UI.StyleSystem.cs`
- `src/MaterialEditor.Base/UI/UI.Tooltip*.cs`
- `src/MaterialEditor.Base/PluginBase.cs`
- `src/UIUtility/UIUtility.cs`
- `PERFORMANCE_RESULTS.md`
- `UI_REDESIGN_READINESS.md`
- `PRECONDITION_REPORT.md`
- `UI_BACKUP_INFO.md`
