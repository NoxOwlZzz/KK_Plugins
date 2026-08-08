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
- `Order`: property order within its category. Explicit values appear first,
  from lowest to highest; ties keep declaration order.
- `CategoryOrder`: category order derived from its properties. Explicit values
  appear first, from lowest to highest. Use the same value on every property in
  a category; if values disagree, the first explicit value declared wins.
- `UiLevel`: `Basic` (the default) or `Advanced`.

## Float-backed controls

`Type="Toggle"` is an alias for a Float property using the toggle editor.
`OffValue` and `OnValue` select the float values written for each state and
default to `0` and `1`.

`Type="Enum"` and `Type="Dropdown"` are equivalent aliases for a Float
property using the enum editor. Declare choices as direct children:

```xml
<Property Name="BlendMode" Type="Enum" DisplayName="Blend mode">
  <Option Value="0" DisplayName="Opaque"/>
  <Option Value="1" DisplayName="Cutout"/>
</Property>
```

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
