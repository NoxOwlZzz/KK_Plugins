# Material Editor manifest metadata schema 2

Schema 2 is an additive UI-metadata layer for shader properties. It does not
change shader property names, persistence keys, material values, or the outer
Sideloader manifest contract.

## Version envelope

Keep the outer Sideloader manifest at its supported schema and version only the
Material Editor payload:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest schema-ver="1">
  <guid>example.shader.pack</guid>
  <name>Example shader pack</name>
  <version>1.0.0</version>

  <MaterialEditor SchemaVersion="2">
    <!-- Shader declarations -->
  </MaterialEditor>
</manifest>
```

`schema-ver="1"` belongs to Sideloader. `SchemaVersion="2"` belongs to Material
Editor. A missing Material Editor `SchemaVersion` uses schema-1 compatibility.
A malformed or non-positive Material Editor version warns and falls back to
schema-1 compatibility instead of crashing. A future positive version warns
and reads the latest known schema-2 subset, so known additive metadata is not
discarded merely because a newer authoring tool added fields this build does
not understand.

## Property attributes

All new attributes are optional.

| Attribute | Value | Default/fallback | Meaning |
| --- | --- | --- | --- |
| `Name` | shader property name without Unity's leading `_` | Required | Stable internal name and persistence key; Material Editor adds `_` when it accesses the material. |
| `Type` | `Float`, `Color`, `Texture`, `Cubemap`, `Keyword`, `Vector`; schema-2 aliases `Boolean`, `Enum` | Required | Storage/value family. Existing types keep their behavior. `Cubemap` targets a native shader `Cube` property. The schema-2 aliases normalize to Float storage plus a default semantic editor. |
| `Category` | text | Uncategorized | Foldable category name. |
| `Hidden` | Boolean | `False` | Statically excludes the property from the UI. |
| `Range` | invariant `min,max` | Editor default | Existing Float slider bounds; Vector editors apply the same explicit bounds independently to each displayed component. |
| `DefaultValue` | existing type format | Existing behavior | Existing default-value metadata. |
| `DisplayName` | text | Tooltip-catalog property `DisplayName`, then `Name` | UI label and, when authored in the manifest, search label; never a persistence key. |
| `Order` | integer | Declaration/fallback order | Legacy local compatibility only. New manifests should use declaration order. |
| `CategoryOrder` | integer | Category declaration order | Legacy local compatibility only. New manifests should use declaration order. |
| `Editor` | semantic editor name | Editor implied by `Type` | Selects Enum or Vector2/3/4, or explicitly names a legacy editor. |
| `Tooltip` | text | Tooltip catalog, if present | Inline English hover hint. A resolved catalog entry takes precedence over inline text. |
| `Group` | text | Empty | Logical grouping metadata available to descriptors. Phase 1 does not promise a separate generic group header. |
| `ShowIf` | simple condition | No condition | Removes the row when false. |
| `VectorComponentCount` | `2`, `3`, or `4` | From `Editor`, otherwise 4 | Number of vector components displayed. |
| `Enums` | comma-separated `label,value` pairs | Empty | Canonical options for an Enum editor. Values are invariant finite floats. |
| `Invert` | Boolean | `false` | Swaps the fixed Boolean values so off writes `1` and on writes `0`. |
| `OffValue` / `OnValue` | finite invariant Float | `0` / `1` | Legacy local compatibility only. New manifests should use fixed `0`/`1` plus `Invert`. |

Texture attributes from schema 1 (`DefaultValueAssetBundle`, `AnisoLevel`,
`FilterMode`, and `WrapMode`) remain unchanged. Cubemap properties intentionally
do not expose 2D offset, scale, normal-map conversion, or Timeline controls.

## Category and Subcategory hierarchy

Schema 2 can organize properties into one explicit, fixed hierarchy:
`Category > Subcategory > Property`. This hierarchy changes presentation only;
it does not change a property's type, value, default, persistence key,
`ShowIf`, or any other property metadata.

Category IDs are stable within one shader. Subcategory IDs are local to their
parent Category, so two Categories may each contain a Subcategory with the same
ID. `DisplayName` is the user-facing label and may change without changing the
stable ID.

Repeated declarations with the same ID must agree on their explicit
`DisplayName` and ordering metadata. If they conflict, Material Editor warns
and deterministically keeps the first declaration in manifest order; it never
drops the later properties. Reusing the same Subcategory ID under different
Categories is not a conflict.

Every hierarchy feature is opt-in. A schema-2 manifest may remain completely
flat, use only legacy `Category="..."` attributes, add explicit Categories
without Subcategories, or add Subcategories. `Subcategory` does not imply
`ShowIf`, shader activation, variant selection, or any material write.
Manifests that declare none of these additions keep their existing presentation
and property semantics.

Hierarchy metadata is shader-local. Material Editor removes Category and
Subcategory IDs when it builds the shared `default` property fallback,
preventing one shader's hierarchy from leaking into another shader that happens
to use the same property name. All non-hierarchy property semantics remain
intact in that fallback.

| Header attribute | Value | Default/fallback | Meaning |
| --- | --- | --- | --- |
| `Id` | non-empty text | Required | Stable grouping identity. An invalid hierarchy declaration warns and falls back without changing material data. |
| `DisplayName` | text | `Id` | User-facing header label. |

```xml
<Category Id="main-color-2nd" DisplayName="Main Color 2nd">
  <Subcategory Id="textures" DisplayName="Textures &amp; Masks">
    <Property Name="Main2ndTex" Type="Texture"
              DisplayName="Texture" />
    <Property Name="Main2ndBlendMask" Type="Texture"
              DisplayName="Blend Mask" />
  </Subcategory>

  <Subcategory Id="blending" DisplayName="Blending">
    <Property Name="Main2ndEnableLighting" Type="Float"
              DefaultValue="1" />
  </Subcategory>
</Category>
```

The example above is the complete organizational feature: it changes no shader
or material state. Boolean and Keyword properties remain ordinary property
rows. Use an explicit `ShowIf` condition when another property's value should
control a row's visibility.

Subcategory headers and their children are indented relative to Categories so
the two levels remain visually distinct.

## Visibility rules

`Hidden` remains the static legacy exclusion. `ShowIf` is the only dynamic
metadata that removes a property during normal browsing. Material Editor does
not classify properties into UI levels or provide a visibility mode that
filters them.

```xml
<Property Name="MainTex" Type="Texture"
          Category="Main"
          DisplayName="Main Texture" />

<Property Name="BlendOpFA" Type="Float"
          Category="Rendering"
          DisplayName="Forward Add Blend Operation" />
```

Search considers both `DisplayName` and `Name`. While a search is active,
Category and Subcategory collapse state cannot hide matching properties and the
stored collapse state is not changed. A matching property whose `ShowIf` is
false is shown disabled with the blocking condition as its reason. This
diagnostic search presentation does not satisfy the condition, enable the row,
or write a material value. `DisplayName` does not rename the Unity shader
property and does not change serialized data.

Tooltip-catalog schema 1 also accepts an optional literal `DisplayName` on each
`Property`. This is a backward-compatible addition; catalogs without the
attribute retain their existing behavior. Label resolution is, from highest to
lowest priority:

1. A non-empty schema-2 manifest `Property DisplayName`.
2. A non-empty tooltip-catalog `Property DisplayName` resolved for the shader.
3. The existing/internal property label, normally `Name`.

Catalog labels do not rename shader properties or persistence keys. They merge
independently from tooltip text, and tooltip `Ref` aliases do not copy them. A
catalog-only label is a rendered-label fallback, not an additional search key;
search still matches `Name` and any manifest-authored `DisplayName`.

## Editors

### Schema-2 Type aliases

Schema 2 accepts convenience `Type` values for common Float-backed controls.
They are syntactic sugar, not new storage or persistence families:

| Schema-2 shorthand | Default normalized form when `Editor` is omitted |
| --- | --- |
| `Type="Boolean"` | Float-backed Boolean control |
| `Type="Enum"` | Float-backed enum control |

`Type="Boolean"` writes fixed Float values `0` and `1`; `Invert="true"` swaps
those values. `Type="Enum"` uses the `Enums` attribute. After parsing, both
aliases have the ordinary Float backing type, so they do not add public
`ShaderPropertyType` values or new serialized keys.

An explicit `Editor` attribute keeps its normal precedence over the alias's
default editor, subject to the same Float-backing compatibility checks as any
other declaration. Authors should normally omit `Editor` when using an alias.
If an Enum alias has no valid `Enums` pair, Material Editor warns and falls
back to the Float editor without writing a material value.

The explicit `Type="Float" Editor="Enum"` form remains supported. The aliases are
recognized only inside `MaterialEditor SchemaVersion="2"`; a missing version
or schema-1 payload treats these names as unknown types under the unchanged
legacy rules.

Recommended `Editor` values are case-insensitive:

| Editor | Required backing type | Behavior |
| --- | --- | --- |
| `Float` | Float | Existing slider/numeric field. |
| `Color` | Color | Existing RGBA editor and color picker. |
| `Texture` | Texture | Existing 2D texture controls. |
| `Cubemap` | Cubemap | Native Cubemap Import, Export and Reset controls. |
| `Keyword` (legacy `Editor="Boolean"` is accepted) | Keyword | Existing shader-keyword toggle. Do not confuse this editor alias with `Type="Boolean"`, which is Float-backed. |
| `Enum` | Float | Dropdown backed by explicit numeric options. |
| `Vector2` | Vector | X/Y inputs; Z/W are preserved. |
| `Vector3` | Vector | X/Y/Z inputs; W is preserved. |
| `Vector4` | Vector | X/Y/Z/W inputs. |

For new vector data, use `Type="Vector"`. `Color` remains a color semantic type
for backward compatibility. `Editor="Vector"` plus
`VectorComponentCount="2|3|4"` is accepted, but the explicit Vector2/3/4 form is
clearer.

An unknown editor warns for that declaration during manifest load and falls back to the editor
implied by `Type`. An invalid component count uses the editor default. No fallback
writes a material value.

### Enum

Enum options use alternating label/value tokens. Values are invariant finite
floats; labels and values must both be unique:

```xml
<Property Name="Cull" Type="Enum"
          DisplayName="Cull Mode" Category="Rendering"
          Enums="Off,0,Front,1,Back,2" />
```

This shorthand is equivalent to `Type="Float" Editor="Enum"` when no explicit
Editor overrides it; the canonical Float-plus-Editor form also remains valid.

Invalid or duplicate labels/values are ignored with a warning. If no valid
options remain,
Material Editor falls back to the property's type editor. A current numeric
value not present in the list is displayed as unknown and is not rewritten until
the user explicitly chooses an option.

### Vector

```xml
<Property Name="ScrollDirection" Type="Vector" Editor="Vector2"
          DisplayName="Scroll Direction" Category="Main" DefaultValue="0,0,0,0"
          Tooltip="Texture scroll speed on X and Y." />
```

Vector2 and Vector3 preserve unshown components on edit. A Vector never opens
the color picker. Keeping `Type="Color"` keeps the existing Color persistence
family, even if a Vector editor is selected for presentation. If an existing
property is deliberately changed from `Type="Color"` to `Type="Vector"`, the
fork migrates a matching legacy Color-backed override from RGBA to XYZW in
memory after the active shader is known. Native Vector data wins when both
records exist; the next save stores `MaterialVectorPropertyList` and drops the
migrated Color record while preserving current and original/reset values.

This migration is backward-readable by the fork, not forward-readable by the
official upstream plugin: upstream does not apply the new Vector key and may
drop it when it resaves the same card, coordinate, or scene. Do not reclassify a
Color property casually, and keep backups when testing native Vector metadata.

### Float-backed Boolean

```xml
<Property Name="UseBacklight" Type="Boolean"
          DisplayName="Back Light" Category="Back Light" />
```

This shorthand creates a Boolean control backed by a Float. It writes only
`0` or `1` after an explicit user change and does not enable or disable a shader keyword. Use
`Type="Keyword"` for a real keyword.

For compatibility, this local branch still reads pre-review schema-2
`Type="Dropdown"`, child `Option`, `Order`, `CategoryOrder`, `OffValue`, and
`OnValue` syntax. `Enums` and `Invert` take precedence whenever present. These
legacy forms are not emitted by the template and are not part of the upstream
PR contract.

### Cubemap

```xml
<Property Name="EnvironmentCube" Type="Cubemap"
          DisplayName="Environment" Category="Environment"
          Tooltip="Environment reflection cubemap." />
```

`Cubemap` is an independent real property type, not a Texture subtype or a
schema-2 alias, so it is also available to schema-1 manifests. Cubemap import
accepts an equirectangular 2:1 PNG or Radiance RGBE `.hdr` panorama (for example
2048x1024) and converts it to a native `UnityEngine.Cubemap`. PNG imports use
RGBA32, while Radiance HDR imports preserve values above 1 in RGBAHalf. Other
proportions are also accepted and are deterministically stretched across a 2:1 panorama, with a
warning in the plugin log. Input is limited to 8,388,608 decoded pixels, 8192
pixels per dimension and a 64 MiB source file. The normalized resolution uses the
largest Cubemap face supported by both source axes, with a minimum 1-pixel face;
the original file bytes remain the persisted and cached source. Export produces
a 2:1 SDR PNG layout, so HDR values above 1 are clipped on export; face sizes
above 1024 are rejected to bound runtime readback memory. Reset removes only
the Material Editor override and restores the
original Cubemap, including an original null value.
Cubemap rows omit texture offset/scale and Timeline interpolation because those
operations apply to 2D textures.

## Conditions

Conditions may reference a Float, Enum, Float-backed Boolean, or Keyword on the
same material. Property names are normalized by removing leading underscores.
The supported grammar is intentionally small:

```text
Property                 equivalent to Property != 0
!Property                equivalent to Property == 0
Property == value
Property != value
Property > value
Property >= value
Property < value
Property <= value
```

`true` and `false` map to `1` and `0`. Numeric literals use invariant culture.
Escape `<` as `&lt;` in XML.

```xml
<Property Name="BacklightColor" Type="Color"
          Category="Back Light"
          ShowIf="UseBacklight != 0" />

<Property Name="HighQualityOnly" Type="Float"
          ShowIf="Quality >= 2" />
```

Malformed expressions, unsupported source types and missing source properties
fail open: the row remains visible and accessible, and a warning is emitted rather
than hiding data. Conditions are reevaluated on a relevant source edit or a
normal presentation rebuild, never by `eval`, dynamic compilation, per-frame
reflection, or continuous polling.

## Ordering interaction

Explicit `Order` is primary. Existing `SortPropertiesByType` and
`SortPropertiesByName` configuration remains active as a tie-breaker after
explicit order. Declaration order is the stable final fallback. If category
sorting is disabled, the existing uncategorized layout is used and category
ordering has no visible effect. Explicit Category/Subcategory headers are a
presentation feature of category sorting, so they are not applied in that mode;
all ordinary property rows remain available.

## Complete example

See
[`Guides/Material Editor Guide/shader_manifest_template.xml`](Guides/Material%20Editor%20Guide/shader_manifest_template.xml)
for a complete Sideloader envelope containing legacy and schema-2 examples.

## Backward compatibility and safe fallback

- A shader without a manifest keeps its existing behavior.
- Schema 1 continues to parse all existing types, ranges, defaults, categories,
  Hidden flags and texture options.
- Explicit Category hierarchy, `Subcategory`, and `ShowIf` are independent
  opt-ins; omitting all of them preserves the flat legacy
  presentation.
- `Type="Boolean"` and `Type="Enum"` are schema-2-only canonical aliases
  normalized to Float; schema 1 treats them as unknown types.
- Unknown `Editor` uses the type editor.
- Enum without valid options uses the Float editor.
- Invalid conditions fail open.
- Vector declarations with invalid component metadata use a safe default.
- Presentation metadata never changes a material automatically.

Two short-lived development-prototype forms are intentionally not compatibility
contracts: `UiLevel`/Basic/Advanced classification and `Type="Toggle"`. Neither
appeared in a released Material Editor manifest schema. `UiLevel` is ignored,
and authors must use the final `Type="Boolean"` spelling instead of
`Type="Toggle"`. This exception does not affect released schema-1 manifests or
manifests that simply omit the new hierarchy metadata.

Schema 2 is part of a GPL-3.0 modification of the upstream Material Editor.
Original upstream attribution is retained; NightOwlZzz / Owl is credited for
the additive work without replacing the original authors.
