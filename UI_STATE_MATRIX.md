# Material Editor UI state matrix

> This matrix began as a current/target planning artifact. The target source is
> implemented at `8c36e8df`: right lists default visible, individual folds are
> view-local, rails are explicit, responsive category collapse is visual-only,
> and there is still no splitter. Runtime observation remains **PENDING**; use
> `USER_VISUAL_REVIEW_CHECKLIST.md` for acceptance evidence.

## Rules

This matrix defines state precedence and the no-write boundary for the Minimal
Three-Panel redesign. “Current” records optimized source behavior; “Target” is
the approved visual projection. A visual state never becomes a second source
of material truth.

## Window and panel states

| State | Current | Target | Material write / persistence |
| --- | --- | --- | --- |
| Window closed | Transient rows/list entries/listeners released; valid target retained | Same | No material write; no card/scene state |
| Window reopened | Rebuild from retained target/filter/mode | Same, restore UI panel preference | No write during bind |
| Target destroyed/replaced | Strong target context, palette, selections, pending OBJ released | Same | No write |
| Left expanded | In-memory default true; visible only with categories | Neutral full category list | UI-only |
| Left collapsed | Navigator hidden; header glyph changes | Narrow rail with explicit expand control | UI-only, never card/scene |
| Right expanded | Fresh-session default false; one toggle shows both lists | Redesigned default true; both lists visible | UI-only |
| Right collapsed | Both lists hidden together | Narrow rail; selections retained | UI-only |
| Renderers expanded/collapsed | Independent state absent | Independent view-local state | No write; no selection/filter clear |
| Materials expanded/collapsed | Independent state absent | Independent view-local state | No write; no selection/filter clear |
| Rename panel | Replaces normal right lists and retains active context while visible | Preserve specialized state | Rename writes only on explicit Rename |
| Narrow width | Fixed external anchors; no coordinated responsive layout | Real-Canvas clamp; category may auto-collapse visually while logical preference survives | No write/config mutation from responsive collapse |
| Panel resize | No splitter | Still no splitter; config/Canvas dimension events recompute bounded layout | No material write |

## Mode and Search states

| State/transition | Authoritative source | Target visual | Write behavior |
| --- | --- | --- | --- |
| Basic active | `Session.UiMode`, presentation policy | Selected Basic segment | No material write |
| Advanced active | Same | Selected Advanced segment | No material write |
| Basic -> Advanced | `ChangeUiMode`; rebuild with anchor | Segment changes; Search/selections retained | No material write |
| Advanced -> Basic | Same | Same preservation | No material write |
| Advanced changes > 0 | `Presentation.AdvancedChangeCount` | `Advanced · N` action in Basic | Navigation only |
| Navigate Advanced change | `FirstAdvancedChange`, collapse keys, scroll index | Opens parents and scrolls | No material write |
| Search empty/nonempty | `Session.Filter` | Empty placeholder / query text | No material write |
| Search edit burst | invalidation coordinator | Latest query wins | At most coalesced presentation rebuild |
| Hidden Advanced matches | `HiddenAdvancedSearchResultCount` | Count plus Show action | Show changes mode only |
| Persist Search off/on | `PersistFilter` ConfigEntry | Labelled checkbox | Config write only |

## Selection and context states

| State | Current semantic source | Target projection | Constraint |
| --- | --- | --- | --- |
| No selected renderer | Empty selected collection means presenter uses available renderers | `All available` or neutral context | Do not call it one active renderer |
| One renderer checked | `SelectedRenderers` | Check + selected surface | Selection callback/rebuild unchanged |
| Multiple renderers checked | Same collection | Count/checks, never singular active renderer | Preserve multi-selection |
| One/multiple materials | `SelectedMaterials` | Count/checks | Preserve multi-selection |
| One shader across sections | Section shader names agree | Show shader name | Read-only projection |
| Multiple shaders | Section shader names differ | `Multiple shaders` | Never display first shader as global truth |
| Empty renderer/material list | Exact total entry count plus visible-match count | `No renderers` / `No materials` when total is zero; `No matches` only when a real filter hides all entries | Passive precreated Text; selection/filter callbacks remain unchanged |

## Common row visual precedence

Highest precedence is first. Combined markers may remain visible where noted.

| Priority | State | Current | Target | Interaction |
| ---: | --- | --- | --- | --- |
| 1 | Hidden family / `ShowIf` false | Family alpha 0 or row absent | Not rendered | No raycast/write |
| 2 | Mixed | Family-specific; explicit only for Enum/Vector/Float Toggle | Text/glyph/per-component marker, no strong fill | First explicit edit resolves affected value |
| 3 | Modified | Black overlay; Reset enabled | Selected/changed surface + compact active Reset | Reset uses existing callback/repository |
| 4 | Focused | Unity EventSystem focus | Existing selectable focus path; final rendered visibility is pending | Focus alone never writes |
| 5 | Pressed | Unity selectable state | Pressed surface | Writes only for explicit editing action |
| 6 | Hovered | Tooltip pointer state | Common hover surface; Reset visibility remains model-owned | No write |
| 7 | Selected navigation/list | Category color or checkbox | Selected surface + navigator marker or list check | Selection semantics unchanged |
| 8 | Default | Family color/default controls | Neutral surface | No write |

Disabled may still show Modified or Mixed markers so state is not lost, but its
controls remain non-interactable. Hover/focus never overrides Disabled.

## Family-specific states

| Family | Default | Mixed | Modified | Unknown/empty | Rebind cleanup required |
| --- | --- | --- | --- | --- | --- |
| Float/Range | Slider + number | No explicit current Mixed source | Compare value/original | Explicit usable range shows slider + number; unbounded Float shows expanded number only | label, range presence, width, value, Reset, focus, listeners |
| Boolean/Keyword | Two-state toggle | No explicit current Mixed source | Compare current/original | N/A | toggle without notify, Reset, listeners |
| Float Toggle | Two-state toggle | `IsMixed`; current label prefix | Mixed or value compare | N/A | Mixed marker, toggle, Reset, Timeline action |
| Enum | Current caption | `Mixed` synthetic option | Mixed/value compare | `Unknown (value)`; never auto-change | option/value cache, caption, Reset, listener |
| Vector | 2-4 components | `Mixed` per component | Any Mixed or vector difference | Hidden unused components | count, bounds, Mixed flags, inputs, listeners |
| Color | RGBA + swatch | No explicit current Mixed source | Color compare | HDR/alpha retained | RGBA, swatch, palette callback, Reset |
| Texture | Export/No Texture + Import | No explicit current Mixed source | `Changed` | `Exists=false` disables export | existence text/state, callbacks, Reset |
| Offset/Scale | Four numeric inputs | No explicit current Mixed source | Either pair differs | N/A | all four values and listeners |
| Shader | Dropdown | No explicit current Mixed source | Name differs | Unknown original reset limitation retained | options/caption/folds/Reset/filter context |
| Render Queue | Integer input | No explicit current Mixed source | Integer differs | Invalid parse restores value | input, Reset, listeners |
| Category | Chevron + label | N/A | No category count backend | Full-name standard tooltip plus real metadata hint when supplied | fold glyph, tooltip, callbacks |
| Material | Direct fold/name + shared More trigger | N/A | No generic modified badge | Copy/remove/rename may be unavailable | live action visibility/enabled state, owner/generation and context |
| Renderer | Direct name/Timeline + shared More trigger | N/A | N/A | Timeline/export may be unavailable | live action visibility/context/listeners |

## Tooltip and menu states

| State | Current/target behavior | No-write/lifetime rule |
| --- | --- | --- |
| Tooltip idle | One shared `TooltipPanel` inactive; target components registered | No string construction per frame |
| Standard hover | Shared popup shows standard text | No write |
| Shift shader hint | Existing policy prefers shader hint | No write |
| Drag | Tooltip suppressed | No write |
| Menus closed | Global and row-action reusable surfaces are inactive; row lease owns no target/callback | Must release previous context |
| Global menu open | One window-owned reusable surface; opening cross-closes the row-action menu | Opening performs no action/write |
| Row-action menu open | One window-owned four-button surface with owner/generation lease; opening cross-closes the global menu | Opening performs no action/write |
| Menu item invoked | Dispatch only a confirmed active/interactable action; close and clear context before callback | Existing action defines read/write behavior |
| Row recycled while row-action menu open | Owner/generation invalidation closes menu and clears context | Never invoke a prior-row callback |

There may be lightweight `Tooltip` components on eligible controls, including
pooled targets. There must be only one rendered tooltip panel. Likewise, no
context-menu GameObject may be created per row.

## No-write transition matrix

| Interaction | Repository setter/reset allowed? | Rebuild allowed? |
| --- | --- | --- |
| Switch Basic/Advanced | No | Yes, one semantic rebuild |
| Type/clear Search | No | Yes, coalesced |
| Select/jump category | No | Scroll only |
| Fold material/shader/category | No | Yes, existing rebuild |
| Collapse any side panel | No | Layout update only |
| Hover/focus/tooltip | No | No semantic rebuild |
| Open/close menu | No | No semantic rebuild |
| Programmatic Toggle/Dropdown/Slider/Input bind | No | No |
| Copy Edits | No material write | No required rebuild |
| Export UV/OBJ/Texture | No material write | No required rebuild |
| Paste Edits | Yes, through existing service | Yes as existing action |
| Reset/value change | Yes, through existing service | Only when existing semantics require it |
| Copy/Remove Material or Rename | Yes, through existing service | Yes as existing action |

## Pooling invariant

Before a `RowView` is rebound, exact listeners are removed and all inactive
families are hidden. The implemented bind/release paths additionally overwrite
or clear label, tooltip, selection/changed, Mixed, Disabled, Reset, menu
context, toggle/dropdown/slider/input state, focus, hover, glyph and swatch
state from the previous model.
