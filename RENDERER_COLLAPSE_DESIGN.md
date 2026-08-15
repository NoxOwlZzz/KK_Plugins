# Diseño de collapse de secciones Renderer

## Comportamiento implementado

El header de cada renderer alterna exclusivamente sus filas de edición:

```text
▼ a_Top                 ▶ a_Top
  Enabled
  Shadow Casting Mode
  Receive Shadows
  Update When Offscreen
  Recalculate Normals
```

El botón de fondo, el chevron y el click izquierdo sobre el nombre comparten el
mismo callback de collapse. `...` abre el menú de Export UV/OBJ y no alterna el
estado. Timeline continúa siendo una acción directa separada.

Esta operación no invoca setters de renderer, no crea Undo y no toca material,
card, coordinate ni scene. Solo cambia presentación y preferencia de sesión.

## Identidad estable y propiedad

`MaterialEditorSectionKeys.Renderer` combina:

- instance ID del contexto raíz;
- ruta relativa del Transform, incluyendo su longitud;
- índice del componente Renderer en el GameObject;
- instance ID del Renderer.

Por tanto, dos renderers con el mismo nombre no comparten estado. Cada sección
también contiene el `OwnerToken` de su `MaterialEditorPresentation`; un callback
de una presentación anterior se rechaza antes de mutar filas o índices.

El default es expandido. Solo se almacenan las claves actualmente colapsadas,
con límite FIFO de 512; se eliminan al expandir y se limpian al invalidar el
target. No se serializan en card, coordinate, scene, material ni configuración.

## Filas lazy y lista virtual

Si una sección nace colapsada, `GetRowsForState(true)` devuelve una lista vacía
y no ejecuta `BuildRendererChildRows`. En consecuencia no crea child RowModels,
no los añade a `VirtualList` y no puede bindear child RowViews/listeners.

La primera expansión construye las cinco filas una vez. Esas RowModels quedan
cacheadas dentro de la presentación actual para siguientes aperturas; al
volver a cerrar dejan de ser filas visibles y sus RowViews/listeners se liberan
síncronamente. Esta retención acotada a la presentación evita volver a leer los
valores originales o enumerar renderers en cada toggle, pero no debe
interpretarse como destrucción de los RowModels ya creados.

## Mutación dirigida

`TrySetRendererCollapsed` calcula el rango inmediatamente posterior al header,
inserta o elimina solo las filas hijas y desplaza los índices posteriores de
Renderer, Material y Category. No reconstruye la ventana ni vuelve a enumerar
la fuente.

`VirtualList.ReplaceRange`:

- suspende listeners antes de tocar los modelos;
- conserva o traslada el anchor de viewport, con fallback al header;
- registra una invalidación de cache y una de filas visibles;
- reutiliza el pool existente;
- ejecuta el único `Update` sucio de forma síncrona, de modo que al retornar no
  queda una child RowView antigua activa hasta el frame siguiente;
- marca un único layout virtualizado.

Los índices se actualizan con el delta del rango; no dependen de un recálculo
por frame ni de una coroutine.

## Cobertura automatizada existente

`RendererCollapsePresentationTests` cubre:

- identidad estable con nombres duplicados;
- default expandido y estado de sesión limitado/limpiable;
- rechazo de owner obsoleto;
- ausencia de creación de hijos cuando comienza cerrado;
- construcción lazy única y reuso al reabrir;
- shifts de índices posteriores;
- fallback al header sin coroutine;
- liberación inmediata de child RowViews/listeners;
- 100 ciclos con pool estable y una invalidación de cada tipo por transición;
- click de fondo/nombre/chevron y menú independiente;
- ausencia de setters, Undo o full rebuild en el método de toggle.

El último gate de metadata comunicado para este checkpoint es **PASS**. La
cobertura usa componentes/stubs y contratos de fuente; no prueba el EventSystem
ni la geometría real del Canvas.

## PENDING — validación en Maker/Studio

Falta probar varios renderers con nombres iguales, scroll rápido, 100 toggles,
menú `...`, cambio de target y persistencia view-only. También falta perfilar
RowModels/RowViews/listeners y memoria nativa en Unity. Hasta obtener log y
captura/video continuo, la experiencia visual y la ausencia de crecimiento de
memoria en juego permanecen **PENDING**.
