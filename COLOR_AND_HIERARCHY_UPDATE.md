# Actualización de color y jerarquía visual

## Dirección aplicada

La superficie programática de uGUI usa ahora una base oscura fija, conservando
la estructura moderna de tres paneles. Los colores se
eligen por rol estructural y estado, nunca por nombres como `Lighting`,
`Emission`, `MatCap` o cualquier categoría específica de KKLT.

Estado de evidencia:

- Tokens, adaptadores y contratos automatizados: **PRESENTES**.
- Revisión visual en Maker/Studio, escalas y monitores reales: **PENDING**.
- Screenshots generados por Codex: **NINGUNO**.

## Paleta estructural efectiva

Los valores actuales de `UI.Theme.cs` son:

| Rol | Token | Color |
| --- | --- | --- |
| Ventana | `Window` | `#15181C` |
| Panel izquierdo | `LeftPanel` | `#1B2026` |
| Panel central | `CenterPanel` | `#20262D` |
| Panel derecho | `RightPanel` | `#1E242B` |
| Header neutral | `NeutralHeader` | `#252C34` |
| Fila de propiedad | `PropertyRow` | `#282F38` |
| Fila alterna | `PropertyRowAlternate` | `#242B33` |
| Input | `InputSurface` | `#222830` |
| Dropdown | `DropdownSurface` | `#222830` |
| Popup | `PopupSurface` | `#1B2026` |
| Header Renderer | `RendererHeader` | `#263A4D` |
| Header Material | `MaterialHeader` | `#3A3042` |
| Header Shader | `ShaderHeader` | `#29404A` |
| Header Category | `CategoryHeader` | `#40334A` |
| Category hover | `CategoryHeaderHover` | `#4C3D57` |
| Category expandida | `CategoryHeaderExpanded` | `#594666` |
| Hover común | `Hover` | `#303844` |
| Pressed común | `Pressed` | `#394553` |
| Selección | `Selected` | `#3A74A8` |
| Texto seleccionado | `SelectedText` | `#FFFFFF` |
| Superficie deshabilitada | `DisabledSurface` | `#252B33` |
| Indicador modificado / warning | `ModifiedIndicator`, `Warning` | `#D7A44A` |
| Superficie modificada | `ModifiedSurface` | `#4A3920` |
| Texto principal | `Primary` | `#ECEFF3` |
| Texto secundario | `Secondary` | `#AAB1BC` |
| Texto deshabilitado | `Disabled` | `#8A929D` |
| Acento | `Accent` | `#4AA3FF` |
| Borde de input | `InputBorder` | `#8D9BAA` |
| Borde fuerte | `StrongBorder` | `#738291` |
| Divisor | `Divider` | `#3E4854` |
| Handle presionado | `HandlePressed` | `#8DC6FF` |
| Error | `Error` | `#DD6670` |
| Success | `Success` | `#62B985` |

Todos esos tokens son opacos; `TooltipSurface` reutiliza `PopupSurface` con
alpha 0.98. Los contratos exigen contraste mínimo 4.5:1 para texto principal,
disabled y selected en sus superficies verificadas, y 3:1 para selección
frente al popup y para bordes de input/panel. Son contratos matemáticos de los
tokens, no una aprobación del render final de Unity.

## Jerarquía común

`MaterialEditorPanelRole` convierte los roles en superficies uniformes:

- todos los renderers comparten el mismo azul;
- todos los materiales comparten la misma lavanda;
- todos los shaders comparten el mismo azul claro;
- todas las categorías comparten el mismo violeta;
- las propiedades permanecen neutrales y pueden alternar gris;
- Selected, Hover, Mixed, Modified y Disabled son estados semánticos separados.

La navegación lateral aplica un fondo común, texto claro en reposo, hover
oscuro y selección azul con texto blanco y marcador lateral. El nombre de la
categoría se usa solo como contenido/identidad de presentación, no como llave
de una paleta. La jerarquía también utiliza chevrons, bordes, indentación,
espaciado y peso tipográfico, de modo que el significado no depende únicamente
del color.

## Consumidores y protección del tema

El seam común es consumido por Window, TopBar, CategoryNavigator,
SelectListPanel, filas Renderer/Material/Shader/Category/Property, popup menus,
dropdowns, inputs, toggles, sliders, scrollbars, tooltips y shader hints. Los
tests de contrato rechazan colores semánticos locales en esos consumidores y
verifican que todos obtengan los tokens desde `MaterialEditorTheme`.

La muestra del color picker es una excepción de contenido: conserva el color
real enlazado por el binder y desactiva la multiplicación visual del tema. El
tema no debe teñir el valor RGBA del material.

## Cobertura automatizada existente

`UiDarkThemeContractTests` valida la paleta oscura exacta,
los roles estructurales, el chrome común, la propagación de font/material y la
propiedad de la muestra de color. `UiThemeTokenContractTests` valida:

- componentes finitos y normalizados;
- fondos de panel y niveles estructurales distintos;
- contraste mínimo de texto primario, secundario, disabled y selected;
- estados Default/Hovered/Pressed/Selected/Focused/Disabled/Mixed/Modified;
- altura de fila única entre layout y virtualización;
- integración de consumidores sin colores semánticos duplicados.

La presencia de esos contratos no demuestra cómo compone Unity los colores en
el display final. No se ejecutaron de nuevo durante esta edición documental.

## PENDING — revisión visual

El usuario debe validar la UI oscura, la separación Renderer/Material/Shader/
Category, Selected frente a Hover, texto disabled, nombres largos, color picker
y escalas 1, 1.75 y 3. Los casos correspondientes están en
`USER_UI_FIXES_TEST_CHECKLIST.md`. Hasta entonces no se afirma aprobación
estética, ausencia de clipping ni legibilidad final en el juego.
