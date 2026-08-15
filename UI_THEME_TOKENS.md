# Material Editor UI theme tokens

## Status and scope

This document records the current source-defined UI tokens.
`MaterialEditorTheme` is an internal, fixed dark presentation for the
programmatic Unity uGUI surface. It is not a theme-selection API, a persisted
preference, a light theme, or a runtime theme editor.

The authoritative implementation is
`src/MaterialEditor.Base/UI/UI.Theme.cs`; common control application is in
`UI.StyleSystem.cs`. Values below are source facts. Their final appearance in
Koikatu and CharaStudio is **PENDING visual validation**.

## Fixed palette

| Token | Hex | Current semantic use |
| --- | --- | --- |
| Window | `#15181C` | Outer window surface |
| LeftPanel | `#1B2026` | Category navigator surface |
| CenterPanel | `#20262D` | Main editor and scroll surface |
| RightPanel | `#1E242B` | Renderer/Material sidebar and scrollbar track |
| NeutralHeader | `#252C34` | Top bars and neutral headers |
| PropertyRow | `#282F38` | Default property-row surface |
| PropertyRowAlternate | `#242B33` | Alternate property-row surface |
| InputSurface / DropdownSurface | `#222830` | Inputs and closed dropdown controls |
| PopupSurface | `#1B2026` | Popup and tooltip RGB base |
| RendererHeader | `#263A4D` | Renderer structural header |
| MaterialHeader | `#3A3042` | Material structural header |
| ShaderHeader | `#29404A` | Shader structural header |
| CategoryHeader | `#40334A` | Category structural header |
| CategoryHeaderHover / Expanded | `#4C3D57` / `#594666` | Persistent category states |
| Hover | `#303844` | Selectable hover surface |
| Pressed | `#394553` | Pressed control surface |
| Selected | `#3A74A8` | Selected surface |
| DisabledSurface | `#252B33` | Disabled control surface |
| ModifiedIndicator / ModifiedSurface | `#D7A44A` / `#4A3920` | Modified state |
| Primary text | `#ECEFF3` | Titles, buttons, values, slider handle |
| Secondary text | `#AAB1BC` | Labels, placeholders and scrollbar handle |
| Disabled text | `#8A929D` | Disabled labels |
| Accent | `#4AA3FF` | Focus, toggle mark, slider fill, active marker, hint underline |
| InputBorder / StrongBorder / Divider | `#8D9BAA` / `#738291` / `#3E4854` | Interactive, panel and decorative boundaries |
| HandlePressed | `#8DC6FF` | Pressed slider and scrollbar handle |
| Warning | `#D7A44A` | Reserved semantic warning color; no aggregate warning UI exists |
| Error | `#DD6670` | Reserved semantic error color |
| Success | `#62B985` | Reserved semantic success color |

`TintIdentity` is opaque white. uGUI multiplies a `Selectable` color block by
its target graphic color, so common selectables keep the graphic at the white
multiplication identity and place semantic color in the `ColorBlock`. Color
swatches remain binder-owned and use `Transition.None`; the theme does not tint
the material color shown by the swatch.

## Semantic surfaces and controls

| Role | Source token(s) |
| --- | --- |
| Main panel | CenterPanel |
| Left / right side panel | LeftPanel / RightPanel |
| Neutral / category header | NeutralHeader / CategoryHeader |
| Renderer / material / shader row | RendererHeader / MaterialHeader / ShaderHeader |
| Default control | InputSurface -> Hover -> Pressed |
| Disabled control | DisabledSurface, disabled alpha `0.55` where applicable |
| Input / dropdown | InputSurface / DropdownSurface with Primary text and Secondary placeholder |
| Toggle | InputSurface with Accent checkmark |
| Slider | Divider track, Accent fill, Primary handle |
| Scroll view | CenterPanel viewport, RightPanel track, StrongBorder/Secondary scrollbar |
| Tooltip | PopupSurface at alpha `0.98`, primary tooltip text |
| Selected / modified surface | Selected / ModifiedSurface |

The visual-state vocabulary is `Default`, `Hovered`, `Pressed`, `Selected`,
`Focused`, `Disabled`, `Mixed`, and `Modified`. These names are a projection of
existing model/control state; they do not create a second editing state.

## Geometry

| Metric | Value |
| --- | ---: |
| Canvas reference | `1920 x 1080` |
| UI scale range / default | `1..3` / `1.75` |
| Margin | `5` |
| Header / top bar | `20` / `40` |
| Fixed virtual row height | `22` |
| Category panel / collapsed rail | `150` / `24` |
| Right collapsed rail | `24` |
| Right panel width range / default | `100..500` / `180` |
| Responsive center minimum | `500 x 138` when the real Canvas permits it |
| Responsive outer margin | `5%` |
| Standard content width | `316` |
| Small, reset and interpolable button width | `20` |
| Tooltip width / maximum height | `280` / `360` |

The responsive policy consumes the real Canvas rectangle and supports the
configured scale/width/height bounds. Manual left/right collapse replaces the
visible contents with a rail but keeps each expanded footprint reserved, so the
central rectangle does not shift or resize. Only the responsive fallback may
compact the left reservation when the 500-unit center cannot otherwise fit;
that forced collapse does not change the logical user preference. Extremely
small canvases where all minimums physically cannot fit remain a documented
limitation.

## Typography

The current font remains Unity's built-in `Arial.ttf`; no font asset or
localization system was added.

| Role | Size / floor |
| --- | --- |
| Primary | `16` |
| Secondary | `14` |
| Icon/glyph | `16` |
| Dropdown | `16`, minimum `12` |
| Vector component | `16`, minimum `12` |
| Property/category/selection long-name floor | `12` |
| Tooltip | `11` |
| Generic input best-fit floor | `2` (requires runtime visual review) |

Long property, material, renderer, shader, category and selection names use
single-line truncation/best-fit according to their view and retain the complete
name in the standard tooltip where a real full name exists. Label click targets
remain active.

## Spacing and glyphs

Control spacing is `2`; section spacing and margin are `5`; property row
padding is `1` on every edge. Selection content inset is `2`. Tooltips use
horizontal padding `4`, vertical padding `2`, and a cursor offset of `5`.

Text glyphs are deliberately asset-free:

| Purpose | Glyph |
| --- | --- |
| Fold collapsed / expanded | `∨` / `∧` |
| Reset | `↺` |
| Material collapsed / expanded | `+` / `-` |
| Right / left chevron | `>` / `<` |
| Interpolable | `O` |

Glyph rendering, alignment and availability in the game font are **PENDING
visual validation**.

## Source-level contrast gate

`UiThemeTokenContractTests` computes sRGB relative luminance and requires at
least 4.5:1 for Primary/Window and 3:1 for Secondary/Window and
Disabled/Window. Calculated from the tokens, the ratios are approximately
15.44:1, 8.25:1 and 3.93:1 respectively. This is a source-level token check,
not a claim about every composite state, display, post-processing path or
rendered screenshot.

## Validation references

- `UiThemeTokenContractTests.cs`: normalized colors, metrics, contrast,
  visual-state vocabulary, authoritative row height and consumer guards.
- `UiDarkThemeContractTests.cs`: exact palette, common semantic chrome and
  binder-owned color swatch.
- `UiControlReadabilityRegressionTests.cs`: Vector and dropdown source/layout
  guards.
- `USER_VISUAL_REVIEW_CHECKLIST.md`: required runtime evidence.
