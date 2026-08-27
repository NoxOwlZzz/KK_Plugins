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

Each `Property` requires `Name` and `Type`; all other schema-2 attributes are
optional.

| Attribute | Value | Default/fallback | Meaning |
| --- | --- | --- | --- |
| `Name` | shader property name without Unity's leading `_` | Required | Stable internal name and persistence key; Material Editor adds `_` when it accesses the material. |
| `Type` | `Float`, `Color`, `Texture`, `Cubemap`, `Keyword`, `Vector`; schema-2 aliases `Boolean`, `Enum` | Required | Storage/value family. `Cubemap` targets a native shader `Cube` property. The schema-2 aliases normalize to Float storage plus a default semantic editor. |
| `Category` | text | Uncategorized | Foldable category name. |
| `Hidden` | Boolean | `False` | Statically excludes the property from the UI. |
| `Range` | invariant `min,max` | Editor default | Float slider bounds; Vector editors apply the same explicit bounds independently to each displayed component. |
| `DefaultValue` | property type format | Type default | Value applied when Material Editor assigns the shader. |
| `DisplayName` | text | Tooltip-catalog property `DisplayName`, then `Name` | UI label and, when authored in the manifest, search label; never a persistence key. |
| `Order` | integer | Declaration/fallback order | Explicit ascending property order; the configured sort and declaration order resolve ties. |
| `CategoryOrder` | integer | Category declaration order | Explicit ascending category order; declaration order resolves ties. |
| `Editor` | semantic editor name | Editor implied by `Type` | Selects Enum or Vector2/3/4, or explicitly names another compatible editor. |
| `Tooltip` | text | Tooltip catalog, if present | Inline English hover hint. A resolved catalog entry takes precedence over inline text. |
| `Group` | text | Empty | Logical grouping metadata exposed to descriptors; it does not create a separate group header. |
| `ShowIf` | simple condition | No condition | Removes the row when false. |
| `VectorComponentCount` | `2`, `3`, or `4` | From `Editor`, otherwise 4 | Number of vector components displayed. |
| `Enums` | comma-separated `label,value` pairs | Empty | Canonical options for an Enum editor. Values are invariant finite floats. |
| `Invert` | Boolean | `false` | Swaps the fixed Boolean values so off writes `1` and on writes `0`. |

Texture properties support `DefaultValueAssetBundle`, `AnisoLevel`,
`FilterMode`, and `WrapMode`. Cubemap properties intentionally
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

A schema-2 manifest may remain flat, use `Category="..."` attributes, define
explicit Categories, or add Subcategories. Category and Subcategory affect
presentation only; omitting them keeps a flat property list.

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

The hierarchy above changes presentation only. Use `ShowIf` when another
property's value should control a row's visibility.

Subcategory headers and their children are indented relative to Categories so
the two levels remain visually distinct.

## Visibility rules

`Hidden` excludes a row unconditionally. `ShowIf` hides it during normal
browsing while its condition is false.

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

Tooltip-catalog schema 1 accepts an optional literal `DisplayName` on each
`Property`. Label resolution is, from highest to lowest priority:

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

The explicit `Type="Float" Editor="Enum"` form is also accepted. The aliases
require `MaterialEditor SchemaVersion="2"`; schema 1 treats them as unknown
types.

Recommended `Editor` values are case-insensitive:

| Editor | Required backing type | Behavior |
| --- | --- | --- |
| `Float` | Float | Slider and numeric field. |
| `Color` | Color | RGBA editor and color picker. |
| `Texture` | Texture | 2D texture controls. |
| `Cubemap` | Cubemap | Native Cubemap Import, Export and Reset controls. |
| `Keyword` (`Editor="Boolean"` is accepted) | Keyword | Shader-keyword toggle. Do not confuse this editor alias with `Type="Boolean"`, which is Float-backed. |
| `Enum` | Float | Dropdown backed by explicit numeric options. |
| `Vector2` | Vector | X/Y inputs; Z/W are preserved. |
| `Vector3` | Vector | X/Y/Z inputs; W is preserved. |
| `Vector4` | Vector | X/Y/Z/W inputs. |

Use `Type="Vector"` for vector semantics. `Type="Color"` uses color semantics
and persistence. `Editor="Vector"` plus
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
the color picker. `Type="Color"` retains Color persistence even when a Vector
editor is selected for presentation. If a property changes from `Type="Color"`
to `Type="Vector"`, Material Editor migrates a matching Color-backed override
from RGBA to XYZW after the active shader is known. Native Vector data wins when
both records exist; the next save stores `MaterialVectorPropertyList` and drops
the migrated Color record while preserving current and original/reset values.

Versions without native Vector persistence cannot apply this record and may
discard it when resaving. Back up cards, coordinates, and scenes before changing
an existing property's storage type.

### Float-backed Boolean

```xml
<Property Name="UseBacklight" Type="Boolean"
          DisplayName="Back Light" Category="Back Light" />
```

This shorthand creates a Boolean control backed by a Float. It writes only
`0` or `1` after an explicit user change and does not enable or disable a shader keyword. Use
`Type="Keyword"` for a real keyword.

### Cubemap

```xml
<Property Name="EnvironmentCube" Type="Cubemap"
          DisplayName="Environment" Category="Environment"
          Tooltip="Environment reflection cubemap." />
```

`Cubemap` is a native shader `Cube` property available to schema-1 and schema-2
manifests. Import accepts equirectangular PNG or Radiance RGBE `.hdr` panoramas;
non-2:1 sources are stretched with a warning. Sources are limited to 64 MiB,
8,388,608 decoded pixels, and 8192 pixels per dimension. HDR values above 1 are
preserved on import. Export produces a 2:1 SDR PNG, clips HDR values above 1,
and rejects face sizes above 1024. Reset restores the original Cubemap,
including an original null value.
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
[`shader_manifest_template.xml`](shader_manifest_template.xml) for a complete
Sideloader envelope containing schema-1 and schema-2 examples.

## Backward compatibility and safe fallback

- A shader without a manifest keeps its existing behavior.
- Schema 1 continues to parse all existing types, ranges, defaults, categories,
  Hidden flags and texture options.
- Explicit Category hierarchy, `Subcategory`, and `ShowIf` are independent
  opt-ins; omitting hierarchy metadata preserves a flat presentation.
- `Type="Boolean"` and `Type="Enum"` are schema-2-only canonical aliases
  normalized to Float; schema 1 treats them as unknown types.
- Unknown `Editor` uses the type editor.
- Enum without valid options uses the Float editor.
- Invalid conditions fail open.
- Vector declarations with invalid component metadata use a safe default.
- Presentation metadata never changes a material automatically.
