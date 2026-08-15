# Material Editor extension API 1.2 changes

This document covers additive API changes for Basic/Advanced metadata and the
new semantic editor families. It supplements
[`Guides/Material Editor Guide/Extension API.md`](Guides/Material%20Editor%20Guide/Extension%20API.md)
and the shipped compatibility policy.

## Compatibility contract

- `MaterialEditorExtensionApi.ApiVersion` is `1.2.0` in this branch.
- No shipped public type, member, constructor signature, optional default, or
  existing enum numeric value is removed or renumbered.
- The existing public `ShaderPropertyData` constructor is unchanged. New
  metadata is additive through fields initialized to backward-compatible
  defaults.
- `MaterialAPI.ShaderPropertyType.Vector = 4` is appended after the existing
  Texture/Color/Float/Keyword values.
- New public symbols are recorded in `PublicAPI.Unshipped.txt` and checked by
  PublicApiAnalyzers.
- UI row/view/binder types and Unity hierarchy details remain internal.

Companion plugins should query capability flags, not assume that a particular
Material Editor file version provides every optional feature.

```csharp
var required =
    MaterialEditorApiCapability.PropertyDescriptorProviders |
    MaterialEditorApiCapability.PropertyEditors |
    MaterialEditorApiCapability.UiVisibilityLevels |
    MaterialEditorApiCapability.EnumPropertyEditors |
    MaterialEditorApiCapability.VectorPropertyEditors |
    MaterialEditorApiCapability.ToggleFloatPropertyEditors |
    MaterialEditorApiCapability.ConditionalPropertyVisibility;

if (!MaterialEditorExtensionApi.Supports(required))
    return;
```

## New capabilities

Capability values are append-only.

| Capability | Value | Contract |
| --- | ---: | --- |
| `UiVisibilityLevels` | 64 | Descriptors can declare Basic/Advanced visibility. |
| `EnumPropertyEditors` | 128 | Float-backed semantic enum editor is available. |
| `VectorPropertyEditors` | 256 | Vector2/3/4 semantic editors are available. |
| `ConditionalPropertyVisibility` | 512 | Visibility conditions are honored. |
| `ToggleFloatPropertyEditors` | 1024 | Float-backed semantic toggle editor is available. |

Earlier flags from API 1.1 retain their values and behavior.

## New metadata contracts

### UI level

`MaterialEditorPropertyUiLevel` contains `Basic = 0` and `Advanced = 1`.
`MaterialEditorUiMode` uses the same two-value ordering for presentation state.
A descriptor or shader definition defaults to Basic. Hidden manifest properties
remain outside the extension visibility contract and never become visible.

### Conditions

`MaterialEditorPropertyCondition` is a UI-independent numeric comparison:

```csharp
new MaterialEditorPropertyCondition(
    "UseBacklight",
    MaterialEditorConditionComparison.NotEqual,
    0f);
```

Supported comparisons are Equal, NotEqual, GreaterThan,
GreaterThanOrEqual, LessThan and LessThanOrEqual. The property name refers to a
Float/Enum/Toggle or Keyword on the same material. Unknown values fail open so
an extension cannot accidentally make a property inaccessible.

### Descriptor additions

`MaterialEditorPropertyDescriptor` adds:

| Member | Purpose |
| --- | --- |
| `Group` | Optional logical grouping metadata. |
| `UiLevel` | Basic/Advanced presentation level. |
| `VisibilityCondition` | `ShowIf` equivalent; false omits the row. |
| `EnumOptions` | Numeric labels for the enum editor. |
| `VectorComponentCount` | Two through four displayed components. |
| `OffValue` / `OnValue` | Values written by a Float-backed Toggle. |

Existing `DisplayName`, `Order`, `Category`, `PropertyName`, `TooltipText`,
`Minimum`, `Maximum`, and `Tag` continue to work. `DisplayName` never replaces
`PropertyName`; providers must keep a stable `Id` and backing property name.

## Built-in editor IDs

The following constants are additive:

```csharp
MaterialEditorPropertyEditorIds.Enum    // materialeditor.enum
MaterialEditorPropertyEditorIds.Vector2 // materialeditor.vector2
MaterialEditorPropertyEditorIds.Vector3 // materialeditor.vector3
MaterialEditorPropertyEditorIds.Vector4 // materialeditor.vector4
MaterialEditorPropertyEditorIds.Toggle  // materialeditor.toggle
```

Existing Float, Color, Boolean and Texture IDs are unchanged. Built-in IDs
cannot be replaced by a plugin.

## Semantic editor types

### Enum

`MaterialEditorEnumOption(float value, string displayName)` represents one
numeric choice. `MaterialEditorEnumPropertyEditor` receives current/original
Float values, at least one option, a value callback and reset callback. It
preserves an unknown current value until the user selects a known option.

```csharp
var options = new[]
{
    new MaterialEditorEnumOption(0f, "Off"),
    new MaterialEditorEnumOption(1f, "Front"),
    new MaterialEditorEnumOption(2f, "Back")
};

return new MaterialEditorEnumPropertyEditor(
    value,
    original,
    options,
    newValue => edits.SetFloat(material, "Cull", newValue),
    () => edits.ResetFloat(material, "Cull"));
```

### Vector

`MaterialEditorVectorPropertyEditor` always carries a four-component backing
value and validates `componentCount` from 2 through 4. Compact editors preserve
components they do not show.

```csharp
return new MaterialEditorVectorPropertyEditor(
    value,
    original,
    3,
    newValue => edits.SetVector(material, "Direction", newValue),
    () => edits.ResetVector(material, "Direction"));
```

New repository-backed operations are exposed through
`MaterialEditorEditService.GetOriginalVector`, `SetVector`, and `ResetVector`.
`MaterialAPI.SetVector` is also additive. Vector persistence is separate from
Color for properties declared as native Vector. A property that remains
Color-backed is not migrated merely because it uses a compact Vector editor. If
the shader metadata explicitly reclassifies an existing Color-backed property as
Vector, the character/scene repositories migrate a matching legacy RGBA record
to XYZW in memory after the active shader is known. Native Vector data wins if
both records exist; the next save writes `MaterialVectorPropertyList` and removes
the migrated Color record while preserving current and original/reset values.

This is backward compatibility for old saves opened by the fork. The official
upstream plugin does not understand the new Vector persistence key, so it will
not apply native Vector overrides and may discard them when resaving. Keep a
backup and do not alternate writers for data that uses native Vector storage.

Timeline integration is optional through `SelectInterpolable`. API 1.2 does not
guarantee a new native Timeline Vector interpolation type. Providers should only
set this callback when they own a compatible Timeline registration.

### Float-backed Toggle

`MaterialEditorTogglePropertyEditor` carries Float current/original values plus
explicit off/on values. It calls `Action<float>` and never manipulates a shader
keyword implicitly.

```csharp
return new MaterialEditorTogglePropertyEditor(
    value,
    original,
    0f,
    1f,
    newValue => edits.SetFloat(material, "UseBacklight", newValue),
    () => edits.ResetFloat(material, "UseBacklight"));
```

Continue using `MaterialEditorBooleanPropertyEditor` for true shader keywords.

## Complete descriptor example

```csharp
yield return new MaterialEditorPropertyDescriptor(
    "backlight-mode",
    "Back Light Mode",
    MaterialEditorPropertyEditorIds.Enum)
{
    PropertyName = "BacklightMode",
    Category = "Back Light",
    Group = "Main",
    Order = 20,
    UiLevel = MaterialEditorPropertyUiLevel.Advanced,
    VisibilityCondition = new MaterialEditorPropertyCondition(
        "UseBacklight",
        MaterialEditorConditionComparison.NotEqual,
        0f),
    EnumOptions = new[]
    {
        new MaterialEditorEnumOption(0f, "Normal"),
        new MaterialEditorEnumOption(1f, "View dependent")
    },
    TooltipText = "Selects the back-light calculation mode."
};
```

Providers are evaluated during controlled presentation rebuilds. They must be
deterministic and fast, and returned descriptors should be treated as immutable.
Conditions are not polled every frame.

## Modified-count limitation

The compact Advanced-change count can evaluate built-in repository-backed
types. It deliberately skips opaque custom editors when Material Editor cannot
obtain a trustworthy original/override value. A companion plugin must not rely
on this UI count as a persistence inventory.

## Validation

Run the API compatibility build with:

```text
dotnet build src/MaterialEditor.API/API.MaterialEditor.csproj -c Release
```

The project treats relevant PublicApiAnalyzers diagnostics as errors. Then run
the metadata tests and the KK target. Passing compile-time analysis does not
replace Maker/Studio compatibility testing.

This API remains part of the GPL-3.0 Material Editor fork. Original upstream
credits remain intact; NightOwlZzz / Owl is an additional contributor credit.
Do not load this fork and the official Material Editor DLL simultaneously.
