# Visibilidad de Copy Edits y Paste Edits

## Resultado implementado

Cada fila de material crea dos botones de texto directos:

```text
[ Copy Edits ] [ Paste Edits ] [ ⋮ ]
```

Mientras existe una fila con contexto de material, ambos controles permanecen
en la fila. `Paste Edits` cambia `interactable`; no se oculta cuando el
clipboard está vacío o no es compatible. Las acciones secundarias de copiar o
eliminar un material y Rename continúan en `⋮`; Copy/Paste Edits no se mueven a
ese menú.

Estado de evidencia:

- Construcción, binding y política de disponibilidad: **PRESENTES**.
- Pruebas automatizadas de contratos y lógica pura: **PRESENTES**, no
  reejecutadas en esta actualización documental.
- Click real, resultado renderizado y responsive en Unity: **PENDING**.

## Disponibilidad y feedback

`Copy Edits` invoca el callback existente de la fila. `Paste Edits` se habilita
solo si `MaterialEditorClipboardPolicy.CanPaste` encuentra al menos una edición
aplicable al target real:

- shader o keyword;
- propiedad float, color, vector o textura que exista en el material;
- propiedad de projector solamente cuando el contexto tiene ese projector.

Cuando Paste está habilitado, el tooltip dice que aplicará las ediciones
copiadas. Cuando no lo está, diferencia entre clipboard vacío y datos
incompatibles. El label usa texto principal o disabled según el mismo estado.
El callback vuelve a comprobar compatibilidad antes de ejecutar Paste, por lo
que una mutación entre render y click no fuerza una escritura inválida.

## Conservación del backend

Los botones delegan en `RowModel.Copy` y `RowModel.Paste`, que conservan las
rutas de edición existentes. La corrección de infraestructura no introduce un
nuevo formato de clipboard ni un nuevo registro de Undo.

Para que una excepción o una lista pública con entradas nulas no contamine el
backend, `MaterialEditorClipboardPasteLease` crea vistas sanitizadas y restaura
las referencias públicas originales en `Dispose`. El projector concreto fluye
desde la fila hasta los backends Character y Studio; la disponibilidad y el
paste usan el mismo contexto.

`MaterialEditorClipboardState.NotifyChanged` ofrece una señal explícita para
las mutaciones realizadas por el facade. Como `CopyData` y sus listas siguen
siendo públicas por compatibilidad, una única vista local al Canvas mantiene un
snapshot semántico para detectar también reemplazos externos. Las suscripciones
de cada fila a esa vista se eliminan mediante su `ListenerScope` al reciclarse.

## Coste y límite responsive

La vista de clipboard ejecuta un scan semántico sin allocations mientras el
Canvas está activo; con la ventana desactivada no recibe `Update`. Es trabajo de
compatibilidad deliberado y se documenta como tal: no equivale a “cero trabajo
absoluto por frame”. Debe medirse en el juego con clipboards grandes.

Los dos botones se crean con ancho compacto fijo dentro del layout de la fila.
El código conserva su presencia, pero no implementa una segunda disposición
responsive que cambie padding o mueva dinámicamente otras acciones. La
ausencia de overlap en anchos extremos es **PENDING** y figura como limitación
hasta la prueba visual.

## Cobertura automatizada existente

`UiCopyPasteVisibilityTests` cubre:

- presencia de ambos botones y uso de los callbacks existentes;
- Paste disabled en vez de oculto;
- actualización dirigida y baja segura de listeners;
- mutación externa y varias vistas;
- sanitización/restauración del clipboard en todas las salidas;
- conteo correcto de projector edits;
- compatibilidad contra el material/projector real;
- listas/elementos nulos;
- identidad de projector en Character y Studio;
- construcción de UI sin ejecutar Copy ni Paste.

## PENDING — validación del usuario

Falta probar un clipboard vacío, uno compatible y uno incompatible en Maker y
Studio; copiar desde un material y pegar en otro; projector; múltiples
materiales; anchos reducido/medio/amplio; y los roundtrips de card, coordinate
y scene. Deben conservarse el log completo del intento fallido y capturas que
muestren simultáneamente la fila, el estado enabled/disabled y el resultado.
