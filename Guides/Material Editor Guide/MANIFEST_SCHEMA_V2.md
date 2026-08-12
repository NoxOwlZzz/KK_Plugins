# Material Editor manifest schema 2

Sideloader's outer `<manifest schema-ver="1">` remains unchanged. Opt in to
Material Editor metadata on the inner element:

```xml
<MaterialEditor SchemaVersion="2">
```

An absent, invalid, or unsupported `SchemaVersion` uses legacy schema 1
behavior. In that mode the metadata and type aliases below are not enabled.

The existing `shader_manifest_template.xml` remains the complete legacy/general
template, including game tags, tooltip catalogs, and the established property
types. Use `shader_manifest_schema_v2_template.xml` for the focused schema 2
example described here.

## Property metadata

Schema 2 adds optional attributes to `<Property>`:

- `DisplayName`: label shown in Material Editor; defaults to `Name`.
- `UiLevel`: `Basic` (the default) or `Advanced`. When a shader declares at
  least one Advanced property, its shader row shows a Basic/Advanced selector.
  Advanced property rows show a vertical accent bar when visible. The selection
  is maintained independently for each shader during the current session.

## Float-backed controls

`Type="Boolean"` is a Float-backed Boolean control. It writes `0` for off and
`1` for on. The optional `Invert="true"` attribute reverses the displayed state
while keeping stored values limited to `0` and `1`; it defaults to `false`.

`Type="Enum"` is an alias for a Float property using the enum editor. Declare
choices as alternating labels and numeric values in the `Enums` attribute,
matching Unity's enum-property style:

```xml
<Property Name="BlendMode"
          Type="Enum"
          DisplayName="Blend mode"
          Enums="Opaque,0,Cutout,1"/>
```

Labels and values must appear in pairs. Labels cannot be empty, values must be
finite invariant-culture numbers, and duplicate labels or values are ignored
with a warning.

The aliases are case-insensitive, but only recognized when
`SchemaVersion="2"` is active.

## Cubemap

`Type="Cubemap"` targets a native shader `Cube` property and is backed by an
actual `UnityEngine.Cubemap`, not by a `Texture2D`. Material Editor exposes
Import, Export, and Reset for this type. Cubemap is a real property type rather
than a schema 2 alias, so it is also available to legacy schema 1 manifests.

Import prefers an equirectangular 2:1 PNG (for example, 2048x1024). Other PNG
proportions are also accepted and are deterministically stretched across a 2:1
panorama, with a warning in the plugin log. Input is limited to 8,388,608
decoded pixels, 8192 pixels per dimension, and a 64 MiB PNG file. The original
file bytes remain the persisted and cached source. Export writes the assigned
Cubemap as an equirectangular 2:1 PNG; face sizes above 1024 are rejected to
keep runtime readback memory bounded. Reset removes only the Material Editor
override and restores the original Cubemap, including an original `null` value.

Cubemap rows do not expose texture offset/scale or Timeline interpolation.
Those operations apply to 2D textures and are not valid for a native `Cube`
property.

## Conditional visibility

`ShowIf` accepts these forms:

```text
Source
!Source
Source == value
Source != value
Source > value
Source >= value
Source < value
Source <= value
```

`value` can be a number, `true`, or `false`; booleans map to `1` and `0`.
The leading underscore in a source property name is optional. Only manifest
properties of type `Float` or `Keyword` can be resolved as sources; this also
includes properties marked `Hidden`. Conditions fail open: an absent or
incompatible source, a resolver error, or an invalid condition leaves the
dependent property visible.

See `shader_manifest_schema_v2_template.xml` for a complete minimal schema 2
example.
