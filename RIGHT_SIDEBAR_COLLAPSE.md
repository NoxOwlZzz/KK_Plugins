# Collapse horizontal del sidebar derecho

## Comportamiento implementado

La acción global de Renderers/Materials cambia el estado lateral, no la altura:

```text
Expandido:  [ panel central fijo ][ Renderers / Materials (reserva W) ]
Colapsado:  [ panel central fijo ][< rail 24 | reserva W restante ]
```

Al cerrar, se ocultan las superficies Renderers/Materials y aparece un rail de
24 unidades con chevron hacia la izquierda para reabrir. El ancho expandido
configurado continúa reservado: el rail reemplaza el contenido dentro de ese
slot y el espacio restante no se entrega al centro. Al abrir, el botón del
header usa chevron hacia la derecha y vuelve a mostrar el contenido en la misma
reserva lateral.

En un mismo Canvas, colapsar Categories, Renderers/Materials o ambos conserva
exactamente los anchors, ancho, alto, viewport y límites horizontales de drag
del panel central. El auto-compact responsive de Categories todavía puede
reducir la reserva izquierda cuando el Canvas real no puede alojar el mínimo;
si el navegador está realmente `Hidden` porque no existen categorías, su
footprint sin uso también puede liberarse.

El default de sesión mantiene `ListsVisible = true`; Renderer y Material se
muestran simultáneamente. No se convierten en tabs excluyentes.

## Estado conservado

El toggle global cambia solo `MaterialEditorSessionState.ListsVisible` y llama
a `SetRightPanelState`. `ToggleVisibility` activa/desactiva los paneles
existentes; no reconstruye sus entradas. Por diseño conserva:

- listas de selección en `MaterialEditorSessionState`;
- texto y patrón de cada filter;
- posición de cada `ScrollRect`;
- estado vertical local de Renderer y Material;
- target y valores del material.

El panel Rename es una capa separada. Abrirlo no cambia la preferencia global;
al volver se restaura la visibilidad que existía antes.

La conservación anterior está sustentada por propiedad/ciclo de vida del
código. Su resultado interactivo exacto en Unity sigue **PENDING**.

## Una transición de layout

`MaterialEditorWindowView.SetRightPanelState` sale inmediatamente si el estado
no cambió. En una transición real:

1. actualiza las dos banderas de vista;
2. activa paneles, Rename o rail según corresponda;
3. registra una sola `LayoutInvalidations`;
4. ejecuta una sola aplicación de settings/responsive layout.

No cambia el rect central solicitado. La reserva derecha expandida se limita al
rango 100..500; el rail visible usa 24 dentro de esa misma reserva.
`OnRectTransformDimensionsChange` recalcula solo cuando el Canvas cambia y no
añade un nuevo loop de layout por frame. En espacio insuficiente, el consejo
veraz es reducir `UI Scale` o el ancho derecho configurado; colapsar el sidebar
no libera su reserva.

## Collapse vertical individual

Cada lista conserva un control independiente para plegar solo su contenido en
vertical. Ese control oculta filter/scroll de esa lista y redistribuye la altura
entre Renderers y Materials:

- ambas abiertas: mitad y mitad;
- una cerrada: header compacto y la otra ocupa el resto;
- ambas cerradas: dos headers compactos apilados.

Este comportamiento no reemplaza ni invierte la acción horizontal global.

## Cobertura automatizada existente

`UiRightPanelsPhaseFiveContractTests` cubre defaults, inicialización única,
rail/chevrons, mutación view-only, no-op, una invalidación, layouts verticales,
Rename y liberación lineal de listeners de entradas.
`UiResponsivePhaseTenContractTests` compara las cuatro combinaciones
Expanded/Collapsed y exige el mismo rect central, reserva derecha y límites de
drag tanto en ancho normal como bajo auto-compact estrecho.

Parte de esa cobertura inspecciona contratos de fuente; no instancia una
ventana completa de Unity para medir filtros, selección y scroll antes/después.

## PENDING — validación del usuario

Quedan pendientes los clicks reales de cerrar/reabrir, anchos mínimo/default/
500, escalas 1/1.75/3, ambos collapses verticales, filters independientes,
scroll preservado, Rename y múltiples renderers/materiales. Un fallo útil debe
incluir el log desde antes del toggle, las dimensiones del rect central y
capturas con el ancho completo antes y después. Los bordes del centro deben
coincidir en los estados expandido, rail y restaurado.
