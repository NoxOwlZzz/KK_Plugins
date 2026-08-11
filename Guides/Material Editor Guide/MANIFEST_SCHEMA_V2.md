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
`Type="Toggle"` remains accepted as a read-compatible alias, but new manifests
should use `Boolean`.

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
