# Corrección de legibilidad y ciclo de vida de dropdowns

## Resultado implementado

Shader y Enum comparten el mismo sistema visual de dropdown. El template se
estiliza al construir el control y el popup clonado/reutilizado se vuelve a
estilizar al activarse, antes de que el usuario interactúe con sus opciones.
Esto cubre tanto el estado fuente como el objeto runtime creado por uGUI.

Estado de evidencia:

- Implementación y contratos automatizados: **PRESENTES**.
- Último gate de metadata comunicado para este checkpoint: **PASS**.
- Apertura, legibilidad y selección dentro de Maker/Studio: **PENDING**.

## Contraste efectivo

| Elemento | Tratamiento actual |
| --- | --- |
| Caption e item | texto `#ECEFF3`, alpha visible 1, font/material propagados |
| Control cerrado | superficie dropdown `#222830`, borde `#8D9BAA` y flecha `#AAB1BC` |
| Template y popup | superficie `#1B2026`, alpha 1, borde fuerte `#738291` |
| Opción normal | popup oscuro `#1B2026` con texto principal `#ECEFF3` |
| Hover | `#303844` con texto principal claro |
| Pressed | `#394553` con texto principal claro |
| Seleccionada | `#3A74A8` con texto `#FFFFFF` |
| Disabled | superficie `#252B33`, texto `#8A929D` |
| Filter | input `#222830`, texto `#ECEFF3`, placeholder `#AAB1BC` y botón `Clear` tematizado |

`ApplyDropdownText` asigna texto claro, alpha, font/material, tamaño y best-fit; el
texto no queda como raycast target. `ApplyDropdownScrollView` aplica superficie
oscura al viewport, configura el `Mask` y templa scrollbars. Cada item clonado usa
`MaterialEditorDropdownItemStyle` para refrescar texto y superficie según
selected/hover/pressed/disabled.

## Shader dropdown

La proyección base se crea una vez por `RowView` al construirla:

- `Reset` permanece en el índice cero;
- se añaden los nombres reales de `XMLShaderProperties` excepto `default`;
- abrir el popup no reconstruye esa lista;
- el listener de selección se registra después de preparar el valor inicial,
  por lo que construir/rebindear la fila no escribe el shader;
- un shader actual que no esté en el catálogo recibe un único `OptionData`
  temporal reutilizable, sin confundirse con `Reset`;
- al abandonar el contexto, esa opción temporal se elimina y neutraliza.

El filter conserva `Reset` aun cuando no coincide, soporta `*` y `?`, ajusta la
altura según opciones visibles y mapea la selección a la secuencia filtrada
para el autoscroll. La memoria de filtros por keyword está limitada a 16
entradas de proceso.

## Enum dropdown

`EnumDropdownOptionCache` proyecta las opciones del descriptor y conserva un
fingerprint del contenido, no solo el count. Así, una mutación in-place con el
mismo número de opciones invalida correctamente la proyección, mientras un
contexto estable actualiza únicamente la selección.

Se conservan las semánticas existentes:

- `Mixed` como opción no escribible;
- `Unknown (<valor>)` cuando el valor no existe en el descriptor;
- opciones null ignoradas de forma segura;
- valores duplicados y opciones de manifest completas;
- el callback solo acepta índices que mapearon a un valor real.

Hasta 128 shells `OptionData` se retienen por fila virtual para reutilización.
Una lista activa mayor sigue mostrándose completa; al reducirse, el tail se
elimina y las capacidades se compactan al límite. `ReleaseContext` borra las
opciones activas, referencias y texto residual antes de reciclar la fila.

## Listeners y popup

`DropdownFilter` usa un guard `_listenersBound`. Registra exactamente una vez
`onValueChanged`, `onEndEdit` y `Clear` mientras el popup está abierto, y los
quita en `OnDisable`/`OnDestroy`. Cada item clonado sigue el mismo patrón con su
listener de Toggle. Un filtro idéntico sale por fast path y solo solicita el
rebuild del scrollbar si realmente cambió visibilidad o altura.

## Cobertura automatizada existente

`EnumDropdownPriorityNineTests` cubre contexto estable, mutación con count
igual, release/reuso, Mixed, Unknown, nulls, duplicados, 500 rebinds sin
callbacks, límite 128, oscilación large-small-large, limpieza del listener del
modelo anterior y actualización de caption en el mismo índice.

`UiControlReadabilityRegressionTests` y los contratos de tema cubren tamaño
mínimo, font/material, alpha, template/popup, filtro, ausencia de listeners
duplicados y ausencia de trabajo cuando el popup está cerrado. El harness
sintético registrado cubre 100 open/close y 100 rebinds, pero no crea el popup
real de Unity.

## PENDING — validación en juego

Falta confirmar visualmente shader names largos, Reset, selección actual,
filter/clear, popup hacia arriba y hacia abajo, Enum Mixed/Unknown/disabled,
escalas 1/1.75/3 y cinco ciclos de abrir/cerrar/rebind. Un fallo debe incluir
`BepInEx/LogOutput.log` desde antes de abrir el popup y una captura con caption,
opciones, selección y filter visibles. No se generaron capturas en esta fase.
