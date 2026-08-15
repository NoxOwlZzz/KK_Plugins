# Basic/Advanced presentation design

> **Historical development record — not the current contract.** Basic/Advanced,
> `UiLevel`, and their selector/state were removed before release. Material
> Editor now keeps compatible non-Hidden properties discoverable; `ShowIf` is
> the only dynamic manifest visibility rule. Do not use this document to author
> manifests or plan current tests.

## Intent

The selector `[ Basic | Advanced ]` belongs to the original Material Editor UI.
It changes presentation only. It does not create a second editor, change the
plugin GUID, patch private UI with Harmony, use reflection to reach binders, or
locate controls through GameObject names/child indexes.

Changing mode must not write a material, toggle a keyword, change a shader,
reset an override, create persistence data, save a card/coordinate/scene, or
create an Undo operation.

## Modes and compatibility

| Metadata | Basic mode | Advanced mode |
| --- | :---: | :---: |
| `Hidden="True"` | Hidden | Hidden |
| no `UiLevel` | Visible | Visible |
| `UiLevel="Basic"` | Visible | Visible |
| `UiLevel="Advanced"` | Hidden | Visible |

Missing or unknown `UiLevel` safely falls back to Basic. There are no name,
prefix, category or render-state heuristics. Existing schema-1 manifests
therefore keep all non-Hidden properties accessible in Basic.

## Ownership of state

`MaterialEditorUiMode` has `Basic` and `Advanced` values. Mode is stored only in
BepInEx configuration/session state:

- `Default UI Mode = Basic`
- `Remember Last UI Mode = true`
- `Last UI Mode` is an internal preference used only when remembering is enabled
- `Show Advanced Change Count = true`
- `Show Hidden Search Result Count = true`

None of these values belong in a character card, coordinate card, Studio scene,
or material override record. Category expansion remains local presentation
state.

## Presentation pipeline

The static organizer parses and orders manifest definitions. Dynamic state is
applied once when Material Editor rebuilds its presentation:

```text
manifest/extension descriptors
        |
        v
static category + declaration ordering
        |
        v
shader/property compatibility and blacklist
        |
        v
Hidden and ShowIf
        |
        v
Basic/Advanced UiLevel
        |
        v
property search (DisplayName and internal Name)
        |
        v
drop empty categories
        |
        v
create RowModels -> pooled VirtualList RowViews
```

The same policy is applied to manifest properties and extension descriptors.
An Advanced definition hidden by Basic is never instantiated as a row and then
disabled. This prevents empty separators, recycled-row artifacts and avoidable
allocations.

`ShowIf` removes the row before row creation. A source-property edit requests a
presentation refresh only when that property is referenced by a condition;
there is no per-frame evaluator.

## Search

Property search retains the existing `_`-prefixed behavior. It matches both
`DisplayName` and the stable internal property name.

In Basic mode, only Basic results are rows. If the same query matches Advanced
definitions, the compact status reports `N advanced results hidden` and exposes
`Show`. The action switches to Advanced and preserves the current query; it
does not temporarily leak Advanced rows into Basic.

## Advanced changes

Basic mode may show `Advanced changes: N`. The status does not expose or reset
the properties. Activating it changes to Advanced and attempts to expand and
navigate to the first counted property.

The count is intentionally conservative. A property is counted only when it is
still compatible and the repository can provide an original/override value
that can be compared to the live material. Opaque custom extension editors and
any type without a reliable original value are omitted rather than guessed.
Consequently, the number is a lower bound in the presence of custom editors.
Float/color/vector comparisons follow the existing Material Editor value
semantics; Phase 1 does not introduce a new fuzzy-comparison policy.

## Sorting

Ordering is deterministic:

1. Named categories precede `Uncategorized`; `Uncategorized` stays last.
2. A category with `CategoryOrder` precedes an unordered category, then sorts by
   numeric value, declaration order and name.
3. A property with `Order` precedes an unordered property and sorts by its
   numeric value.
4. Existing `SortPropertiesByType` and `SortPropertiesByName` settings are
   tie-breakers after explicit `Order`.
5. Declaration/fallback order is the stable final tie-breaker.

When `SortPropertiesByCategory` is disabled, properties use the existing
uncategorized presentation. Authors should use one consistent `CategoryOrder`
for every declaration in a category.

## Editors and writes

The new rows are alternate presentations of repository-backed values:

- Enum writes a Float chosen from explicit options. An unknown current value is
  displayed without writing a replacement.
- Toggle writes configured Float `OffValue`/`OnValue`; it is not a Keyword.
- Vector2/3/4 writes Vector data and preserves components not shown by a compact
  editor. It does not open the color picker.
- Keyword remains the existing keyword path.

Creating, binding, searching, changing mode, showing a mixed/unknown value,
expanding a category and displaying a tooltip are read-only. A setter is called
only from an explicit user edit or reset action.

Vector persistence is an additive type distinction. A declaration that remains
`Type="Color"` remains Color-backed even when it uses a compact Vector editor;
its existing Color record and Timeline behavior are preserved. When a shader
author explicitly reclassifies that property as `Type="Vector"`, the fork can
read a legacy Color-backed override for the same material/property and migrate
its RGBA values to XYZW in memory after shader overrides are active. Native
Vector data wins if a transitional save contains both records, and overrides
equal to the original value are discarded. The next card, coordinate, or scene
save writes `MaterialVectorPropertyList` and removes the migrated legacy Color
record; the visual value and original/reset value are preserved.

The official upstream plugin does not understand the new Vector persistence
key. Restoring it remains safe for the installation, but native Vector overrides
will not be applied and may be lost if that card/coordinate/scene is resaved
there. Keep the pre-test backup and use the fork when working with native Vector
data. A Phase 1 native Vector row does not promise a new Timeline interpolation
type; adding that contract remains follow-up work.

## Rebuild and performance policy

A controlled rebuild is allowed when the editor opens or when material, shader,
search, mode, relevant condition source, or extension registration changes.
Mode selection requests at most one list rebuild. XML parsing, global material
search, condition evaluation, full value comparison, row creation and logging
must not run continuously per frame.

Before that rebuild, `VirtualList` captures the top visible row by semantic
identity (row type plus its renderer/material/shader/category/property context)
and its partial-row offset. After the new model is installed it restores the
exact row when it survives; otherwise it chooses the nearest surviving row in
the same category, then shader, then material. The category navigator receives
one final viewport publication, and a silent post-layout correction prevents
Unity's deferred `ScrollRect` clamp from moving the restored context. This
correction does not build or install the row model again.

The implementation is shared by Maker and Studio through MaterialEditor.Base;
game-specific repositories retain their existing responsibilities.

## Risks and manual gates

- Confirm no setter/repository write occurs during repeated mode switches.
- Confirm pooled rows do not retain stale enabled, mixed, enum or vector state.
- Confirm schema-1 and no-manifest shaders remain fully accessible.
- Confirm vector save/load in cards, coordinates and scenes, including the
  documented one-time Color-to-Vector migration only for properties explicitly
  reclassified as `Type="Vector"`.
- Confirm extension plugins query capabilities before using new metadata.
- Confirm only one `KK_MaterialEditor.dll` is loadable.
- Confirm Maker and Studio behavior visually; source inspection and automated
  metadata tests are not substitutes for these checks.

## License and attribution

This is a GPL-3.0 modification of the upstream IllusionMods/KK_Plugins Material
Editor. Original license, copyright, credits, authorship and Git history remain
intact. NightOwlZzz / Owl is credited for these changes without replacing any
upstream credit. lilToon informed UX analysis only; its inspector code was not
copied.
