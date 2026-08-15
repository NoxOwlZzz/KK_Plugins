# Material Editor user visual review checklist

## How to use this checklist

This is the required manual acceptance matrix for commit `8c36e8df`. None of
the 58 cases below has been completed by source inspection, Metadata tests or a
compile. Every row begins **PENDING**.

For every case, record game (`Koikatu` Maker or `CharaStudio`), plugin build,
resolution/aspect, `UI Scale`, `UI Width`, `UI Height`, `UI List Width`, target
object/material/shader and any relevant tooltip/filter settings. Do not mark a
case PASS without performing the action and attaching both requested evidence
fields. Redact personal paths or card names if needed; do not omit exceptions.

Suggested filenames:

```text
V##_game_resolution_scale_action.png
V##_game_resolution_scale_action.log.txt
```

## A. Startup, theme and truthful feedback

| ID | Action | Expected result | Log requested from user | Capture requested from user | Status |
| --- | --- | --- | --- | --- | --- |
| V01 | In Maker, open Material Editor on a representative material using a simple/no-manifest shader. | One usable three-panel window opens through the legacy fallback; no white legacy surface, overlap or missing center content. | BepInEx span from UI open through first presentation; name the shader and include every Material Editor warning/error. | Full window screenshot with all three regions and the simple shader context visible. | **PENDING** |
| V02 | In Studio, open Material Editor on a representative KKLT material/item. | The shared dark structure and KKLT metadata categories open; Studio-specific header control remains present and usable. | Studio BepInEx span from open through first KKLT presentation; identify plugin/shader versions. | Full Studio window screenshot including KKLT categories and header context slot. | **PENDING** |
| V03 | Inspect window, panels, headers, rows, inputs and popups under default settings. | Fixed dark palette is coherent; controls remain distinguishable and Color swatches retain their actual value. | Log tail covering inspection, even if no UI messages are emitted. | One full-window image plus close crops of a row, input and popup. | **PENDING** |
| V04 | Hover, press and disable representative Button, Toggle, Dropdown and row controls. | Default, hover, pressed and disabled states are visibly distinct without squared/double-dark tint. | Log span covering the interactions and any exception. | Four-state collage or short video showing each state. | **PENDING** |
| V05 | Compare title, label, input, placeholder, button and tooltip text. | Text role, alignment and hierarchy are consistent; placeholder/disabled text stays legible but subordinate. | Log tail during inspection. | Annotated crop containing every text role. | **PENDING** |
| V06 | Exercise one Scrollbar, Slider, Toggle and Dropdown. | Track/fill/handle/checkmark/list surfaces use coherent chrome and remain clickable. | Log span from first interaction until controls settle. | Screenshot or short video of all four active controls. | **PENDING** |
| V07 | Enter a central search with no result, then clear it; also inspect a genuinely empty presentation if available. | Filtered empty state says `No matches`; unfiltered zero-row state says `No rows in this view`; message disappears when rows return. | Log span containing the filter changes and refresh. | Before/empty/after screenshots. | **PENDING** |
| V08 | Inspect categories and bottom of the window with normal and problematic materials. | No fabricated footer, global modified total, category warning badge or generic warning scanner appears. | Full log span; identify any real backend warning separately. | Full window and bottom/category crops proving the actual surfaces present. | **PENDING** |

## B. Top bar and global actions

| ID | Action | Expected result | Log requested from user | Capture requested from user | Status |
| --- | --- | --- | --- | --- | --- |
| V09 | Inspect the two top-bar rows at default width. | Title, shader context, Studio slot where applicable, More, Close and search do not overlap; no obsolete Basic/Advanced selector or indicator appears. | Log tail after open. | Top-bar crop with element labels readable. | **PENDING** |
| V10 | Select materials that all use one shader. | Read-only shader context displays that real shader name and does not alter selection/materials. | Log span covering selection change. | Header screenshot plus selected material context. | **PENDING** |
| V11 | Select materials with different shaders. | Shader context says `Multiple shaders`; it does not guess the first material. | Log span covering multi-shader selection. | Header and right-selection screenshot. | **PENDING** |
| V12 | Compare the same property set in a flat manifest and an explicit Category/Subcategory manifest. | Subcategory provides only indented visual organization and folding; no extra control or classification indicator is invented and material values/rendering remain unchanged. | Log span covering both manifest presentations and any warning/error; identify manifest/shader versions. | Same-camera flat and hierarchical screenshots, plus expanded/collapsed hierarchy. | **PENDING** |
| V13 | Search by display name, internal-name prefix/underscore and comma-separated tokens, then clear. | Existing search semantics remain; categories prune/return without stale rows. | Log span containing each search term in order. | Screenshot for each representative result and final cleared view. | **PENDING** |
| V14 | Toggle Persist Search, enter a filter, close/reopen as supported, then disable persistence. | Persist behavior follows existing config; it changes UI filter state only and never material values. | Config/log excerpt showing setting and reopen sequence. | Filter before close and after reopen. | **PENDING** |
| V15 | Search across collapsed Categories/Subcategories, including one matching property whose `ShowIf` is false. | Matching rows are discoverable without a mode switch; collapsed hierarchy cannot hide results, and a false-`ShowIf` match is disabled with its blocking condition without changing the material. | Log span covering search, condition state and clear. | Results while searching and restored collapse state after clear. | **PENDING** |
| V16 | Open More; test Collapse/Expand all, category visibility and right-panel visibility; dismiss by Escape and outside click. | One global menu appears, each real view action works, Escape/outside closes it, and no row-action menu remains simultaneously open. | Log span for all actions/dismissals and any exception. | Open-menu screenshot plus before/after panel screenshots or video. | **PENDING** |

## C. Left category navigation

| ID | Action | Expected result | Log requested from user | Capture requested from user | Status |
| --- | --- | --- | --- | --- | --- |
| V17 | Open a shader with several categories and inspect the expanded left panel. | Header, material/shader names, list, scrollbar, selected marker and collapse control are readable. | Log span after presentation build. | Full left-panel screenshot. | **PENDING** |
| V18 | Scroll the central list through multiple material/shader sections. | Left navigation follows the section intersecting the central viewport; active category changes without writing data. | Log span during slow scroll. | Short video showing center and left marker together. | **PENDING** |
| V19 | With responsive auto-collapse not forced, record the center rect, collapse the left panel with its header control, then expand from the rail. | Panel and 24-unit rail are mutually exclusive; the expanded left footprint stays reserved, the center rect remains identical, and preference changes only by the explicit click. | Log span covering both clicks plus center dimensions in all three states. | Expanded, rail and restored screenshots with matching center edges. | **PENDING** |
| V20 | At high scale/narrow width with right side expanded, trigger responsive category auto-collapse; then restore enough space. | Left collapses visually, logical preference survives, and it automatically returns when space permits. | Log/config excerpt with dimensions before/after. | Narrow forced state and restored state screenshots. | **PENDING** |
| V21 | While responsive collapse is forced, hover/click the local rail expansion and global category action. | Controls are disabled and truthfully advise reducing UI scale or the configured right-panel width; collapsing the right sidebar is not offered as recovery and no inert preference flip occurs. | Log span for attempted interactions. | Tooltips on local and global constrained actions. | **PENDING** |
| V22 | Use very long material, shader and category names in the navigator. | Lines truncate/best-fit without wrap/overlap; standard tooltips expose the full real names. | Log span while hovering each name. | Crops of truncated labels with each tooltip visible. | **PENDING** |
| V23 | Click Category and Subcategory disclosure, label and header backgrounds; rebind by scrolling away/back. | Exactly one fold occurs per click, no property value changes, and the hierarchy preserves its state after pooling. | Log span with click/write counts and scrolling. | Expanded/collapsed and restored screenshots plus optional click video. | **PENDING** |

## D. Right selection and Rename surfaces

| ID | Action | Expected result | Log requested from user | Capture requested from user | Status |
| --- | --- | --- | --- | --- | --- |
| V24 | Open a fresh Material Editor session. | Renderers and Materials are visible simultaneously by default and center remains usable. | Log span from session open. | Full three-panel screenshot. | **PENDING** |
| V25 | Compare right header counts with manually counted renderer/material entries. | Counts equal real total entries, not filtered visible counts or invented availability. | Log span after lists populate. | Headers plus enough list evidence to verify totals. | **PENDING** |
| V26 | Filter Renderers to one match and then no matches. | Matching rows alone remain; count header stays total; passive empty state says `No matches` only at zero visible. | Log span with filter terms. | One-match and no-match screenshots. | **PENDING** |
| V27 | Repeat the filter sequence for Materials. | Material filtering and empty feedback behave independently without changing Renderer filter/selection. | Log span with both panels visible. | Before/after screenshots containing both panels. | **PENDING** |
| V28 | Open a target with genuinely no renderers/materials where supported, then compare with a filtered nonempty target. | Empty text distinguishes `No renderers`/`No materials` from `No matches`. | Log span for both target contexts. | One screenshot for genuine absence and one for filtered absence. | **PENDING** |
| V29 | Select/deselect multiple renderer and material entries. | Check state and selected surface agree; central presentation follows existing selection semantics; selections survive view-only folds. | Log span covering selection callbacks. | Selection screenshots before and after folding. | **PENDING** |
| V30 | Collapse and expand only Renderers. | Renderer becomes a compact header while Materials receives remaining height; no selection/filter is cleared. | Log span for both clicks. | Collapsed and restored right panel. | **PENDING** |
| V31 | Collapse and expand only Materials. | Material becomes a compact header while Renderers receives remaining height; no selection/filter is cleared. | Log span for both clicks. | Collapsed and restored right panel. | **PENDING** |
| V32 | Collapse both individual lists. | Two 20-unit headers stack at the top without a blank full-height gap; both remain independently recoverable. | Log span covering collapse order. | Right panel with both compact headers. | **PENDING** |
| V33 | Record the center rect, globally collapse right panels, restore them, open Rename, then return. | The 24-unit rail points left within the fixed right reservation; the center rect never changes, Rename temporarily replaces lists, and returning preserves the global visibility preference. | Log span for global toggle/Rename/back plus center dimensions in every state. | Four-state sequence or short video with matching center edges. | **PENDING** |

## E. Row actions and property controls

| ID | Action | Expected result | Log requested from user | Capture requested from user | Status |
| --- | --- | --- | --- | --- | --- |
| V34 | On a disposable target, inspect a Renderer row, invoke direct Recalculate Normals once where available, then open its More action. | Renderer name, Timeline and Recalculate Normals remain direct; the recalculation callback runs once; only Export UV Map and Export `.obj` move into the shared row menu. | Log span covering Recalculate Normals and menu open/close, including callback/error output. | Renderer row before/after recalculation plus the open menu. | **PENDING** |
| V35 | Invoke Export UV Map and Export `.obj` on valid renderers. | Existing export workflows run once on the bound renderer; OBJ retains deferred request behavior; menu closes before rebuild/export. | Log excerpt for both exports including destination/error. | Menu action plus resulting file confirmation with private path redacted. | **PENDING** |
| V36 | On a disposable material, open More, invoke Copy Material, then open More on the created removable copy and invoke Remove Material. | Fold/name remain direct; the dynamic slot says Copy or Remove according to real context; each existing callback runs once and the menu never exceeds four actions. | Log span for both menu contexts and both invocations, including material identity/count before and after. | Copy and Remove menu variants plus before/copied/removed material-list captures. | **PENDING** |
| V37 | Open Material More before copying, copy edits, reopen on another material and Paste. | Paste is disabled when copy data is empty and enabled when live data exists; action applies through existing callback only once. | Log span from empty state through copy/paste. | Disabled and enabled Paste screenshots plus rendered result. | **PENDING** |
| V38 | Open row menu, then scroll, press Escape, click outside, open global menu and recycle the owner row. | Row menu closes on every invalidation, never coexists with global menu, leaves no stale action/focus and does not click through. | Full interaction log including any exception. | Short video showing all dismissal paths. | **PENDING** |
| V39 | Find changed and unchanged properties across Renderer, Shader, RenderQueue, Texture, Offset/Scale, Color, Float, Keyword, Enum, Vector and Float Toggle rows. | Compact `↺` is 20 units, enabled only under existing changed/interactable state; shader Dropdown's semantic `Reset` option remains text. | Log span while resetting representative families. | Collage of changed/unchanged rows and shader option list. | **PENDING** |
| V40 | Inspect and edit a Float with explicit valid minimum/maximum. | One-line row shows slider plus numeric input; both stay synchronized; reset/original behavior is unchanged. | Log span for slider, input and reset. | Before/edit/reset screenshots or video. | **PENDING** |
| V41 | Inspect and edit a Float with no declared range. | No slider is invented; numeric input expands to the content width; label drag/input/reset still work. | Log span for input, label drag and reset. | Full unbounded Float row before/edit/reset. | **PENDING** |
| V42 | In separate passes, edit Render Queue and all four Offset/Scale values; type negative, decimal and intermediate numeric text, cancel/commit as supported and drag an eligible label. | Each field changes only its intended existing value once; invalid/intermediate text restores safely; reset and programmatic rebind do not duplicate callbacks or overlap controls. | Log span with original/entered/result Render Queue and Offset/Scale values plus callback/error output. | Before/edit/reset captures for both row families and a short label-drag video. | **PENDING** |
| V43 | At UI scale 1, open the shader selector on a disposable material, select a different compatible shader and invoke its textual `Reset` option; at scales 1.75 and 3 reopen shader/property Dropdowns. | Each deliberate shader selection/reset uses the existing callback once; caption/options remain readable at >=12-unit source floor, popup stays on Canvas and semantic `Reset` text is unchanged. | Log/config excerpt for every scale plus original/selected/reset shader names and callback/error output. | Before/selected/reset shader captures and an open Dropdown screenshot at each scale. | **PENDING** |
| V44 | Inspect Enum normal, `Mixed`, unknown-value and very long-option states. | Exact semantic captions remain, list stays complete, long text truncates rather than corrupting selection. | Log span with values/options used. | Caption and option-list screenshots for each state. | **PENDING** |
| V45 | Inspect Keyword and float-backed Toggle normal, disabled and Mixed states; toggle explicitly. | Distinct semantics remain; Mixed is visible without a programmatic write; one explicit choice performs one write. | Log span including callback count/value. | Before/Mixed/after screenshots. | **PENDING** |
| V46 | Inspect/edit Vector2, Vector3 and Vector4, including component Mixed and reset. | All components remain on one 22-unit row, labels/inputs are readable, only intended components change and reset restores existing original. | Log span with component values. | Vector2/3/4 row collage and one edit video. | **PENDING** |
| V47 | Inspect Texture Import, Export, No Texture and Reset states. | Existing actions and enablement remain; no fabricated thumbnail/name appears; compact reset is aligned. | Log span for valid import/export/reset and any file error. | Texture row states plus result confirmation. | **PENDING** |

## F. Color, accessibility, responsive and lifecycle review

| ID | Action | Expected result | Log requested from user | Capture requested from user | Status |
| --- | --- | --- | --- | --- | --- |
| V48 | Open a Color picker, close it by its normal control, reopen; repeat at least five times and after scrolling/rebinding. | Picker appears every time, second click hides it, no stale owner/callback prevents reopening, and one edit changes the intended color. | Full log span for all cycles and any palette message/exception. | Continuous video of five cycles and rebind. | **PENDING** |
| V49 | Edit RGBA numerically, inspect swatch, use palette action and test an HDR/out-of-LDR value where supported. | Swatch shows binder value rather than theme tint; RGBA remains editable; palette callback works; HDR is not silently clamped by visual chrome. | Log span with exact entered/result values. | RGBA/swatch/palette before and after. | **PENDING** |
| V50 | Use very long property, renderer, material and shader names in center/right rows. | Labels remain single-line without control overlap; full real name appears in standard tooltip; label click still works where registered. | Log span for hover and label-click callback. | Each truncated label with tooltip. | **PENDING** |
| V51 | Inspect primary/secondary/disabled text, selected/changed/Mixed states and focus indicator on the actual display. | States are distinguishable in context and important meaning is not color-only; no text disappears into its surface. | Note display/color-profile settings and attach log tail. | Unedited full-resolution state collage. | **PENDING** |
| V52 | Hover standard tooltips, hold Shift for shader hints, drag a tooltip target, and move cursor to all Canvas edges. | Correct text source wins, hint underline behaves, drag suppresses tooltip, panel stays above menus/on Canvas and never blocks clicks. | Log span for tooltip settings and interactions. | Edge/tooltips/Shift screenshots or video. | **PENDING** |
| V53 | Navigate as far as supported by keyboard/controller; open row/global menus and dismiss with Escape from enabled and disabled-action areas. | Visible focus does not become trapped on inactive objects; Cancel closes the relevant menu; no accidental material action fires. | Input/log span including callback count and exceptions. | Video showing focus and Escape paths. | **PENDING** |
| V54 | Set UI scale 1 with minimum/default/extreme configured window and side widths. | Center/top bar remain ordered, side rails reachable, rows readable and no panel is offscreen when the Canvas can satisfy minimums. | Config plus log from setting change/reopen. | Full screenshots for the three width states. | **PENDING** |
| V55 | Repeat at UI scale 3 and side width 500, record the center rect, then collapse right. | Right width is capped as needed, category auto-collapse is truthful, center remains at least 500 when feasible, and right collapse keeps the same reserved footprint and center rect. | Config/log excerpt and center dimensions for each state. | Expanded constrained and collapsed-fixed-reservation screenshots. | **PENDING** |
| V56 | Test 16:9, 4:3 and 21:9 or closest available window sizes; resize while open and drag to every edge with Prevent Dragout enabled. | Real Canvas change recomputes layout/pool, aggregate left/right surfaces stay on Canvas when feasible, and drag cannot strand side panels offscreen. | Resolution/config log plus resize/drag span. | Full-window screenshot per aspect and edge-drag video. | **PENDING** |
| V57 | Using the same long property list, config and camera, record warm open, resize, fractional scroll, filter, close/reopen and target-change runs in the frozen optimized pre-redesign build and then this UI build. | Current UI shows no blank/stale/duplicate rows or listener symptoms and no obvious subjective responsiveness regression; record any perceived difference rather than converting it to FPS or a percentage claim. | Complete logs for both sequential builds, exact hashes/config and profiler excerpt if available; explicitly label subjective observations. | Matched continuous videos of the same sequence in both builds. | **PENDING** |
| V58 | Run isolated view-only then one-known-edit roundtrips for card load, coordinate load, Studio scene load, Partial Load and Scene Import; do not combine evidence between formats. | View-only use adds no material edit; the explicit edit persists only through each format's existing route; Partial Load and Scene Import retain their existing merge/import semantics; hierarchy collapse/panel state is never material data. | Separate before/save/load/import logs, hashes/timestamps and object/material identities for card, coordinate, scene, Partial Load and Scene Import; redact private paths. | Same-camera before/view-only/edited captures for each of the five isolated sequences. | **PENDING** |

## Completion record

| Result | Count |
| --- | ---: |
| PASS | 0 |
| FAIL | 0 |
| BLOCKED | 0 |
| **PENDING** | **58** |

When testing is complete, replace the summary counts, link the evidence for
each row, and move every failure into `KNOWN_UI_LIMITATIONS.md` or a tracked
fix. Do not convert an untested row to PASS based on an automated contract.
