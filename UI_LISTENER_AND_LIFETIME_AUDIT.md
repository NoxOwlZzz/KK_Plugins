# Auditoría de listeners, invalidaciones y lifetime de la UI

## Conclusión

El árbol actual tiene ownership explícito para listeners reciclables, targets
de menús, caches de dropdown, navegación lateral y rangos de la lista virtual.
Los contratos automatizados y el harness sintético no muestran listeners
activos al terminar sus escenarios. Esto reduce los caminos conocidos de
callback obsoleto, pero no demuestra todavía que Unity no retenga objetos o
UnityEvents después de 100 ciclos reales.

- Candidato auditado: compilado y desplegado desde el commit fuente `8bcf8649`;
  build completo exit code 0, incluidos metadata/regression, API, harness y
  Release AI/EC/HS2/KK/KKS/PH.
- Harness final schema 2: **PASS**, 45/45 resultados e invariantes; todos
  terminan con `activeListeners = 0` en el modelo. El JSON de 59,092 bytes,
  generado `2026-08-09T17:56:34.4741749Z`, tiene SHA-256
  `8986ADAA352AA00D342641A7DE7E24471CA218EDDDCC67A482D84BB2792703CC`.
- DLL de build e instalado: bytes idénticos, SHA-256
  `72EA76AC8AB31BAE28E67FA53AED658A28E585BA78B85CCF08DC80F9B8405F95`.
- No se registró un smoke post-deploy de Maker o CharaStudio para este DLL
  exacto. El smoke anterior corresponde al DLL previo ahora guardado en el
  backup y no prueba inicialización, listeners ni lifetime de este candidato.
- Auditoría/profiler en Maker/Studio con la ventana abierta: **PENDING**.
- El cierre documental no volvió a compilar ni ejecutar tests; contrasta los
  resultados y artefactos producidos por el build final ya completado.

## Mapa de ownership

| Owner | Registro | Baja / invalidación | Riesgo controlado |
| --- | --- | --- | --- |
| `ListenerScope` | Callback exacto para Button, Toggle, Dropdown, Input y Slider | `Clear` los quita en orden inverso; nunca usa `RemoveAllListeners` para una fila reciclada | Duplicados y callbacks del modelo anterior |
| `RowBinder` | Un scope por RowView y generación de binding | Limpia antes de cada bind, suspend, release y destroy; neutraliza caches Enum/Shader | Rebind A -> B y referencias a RowModel viejo |
| `VirtualList` | Modelos visibles en un pool high-water | Suspende antes de `SetList`/`ReplaceRange`; libera slots fuera de viewport/capacidad | Filas invisibles con listeners activos |
| Category navigator | Tres listeners permanentes por entrada reutilizable | `Bind(null)` quita el target semántico; listeners llaman al target actual | Closure de categoría obsoleta |
| `DropdownFilter` | ValueChanged, EndEdit y Clear solo con popup abierto | Guard `_listenersBound`; baja en disable/destroy | Registro repetido al reabrir |
| `MaterialEditorDropdownItemStyle` | Un listener por Toggle clonado | Guard y baja en disable/destroy | Estado selected/texto residual |
| Row action menu | Cuatro botones precreados y un lease owner/generation | Cierra en rebind/release/scroll; invalida owner y limpia acciones | Ejecutar acción contra fila reciclada |
| Clipboard view | Un listener global por Canvas activo; suscriptores de filas | Baja global en disable/destroy; cada fila se desuscribe por `ListenerScope` | Retener filas tras cambios del clipboard |
| Right selection entry | Toggle/click propios de una entrada | `ReleaseEntries` quita listeners, tooltip, destruye y borra diccionario | Closures de Renderer/Material viejos |

`RemoveAllListeners` permanece únicamente en controles totalmente propiedad del
plugin que se destruyen o reemplazan (por ejemplo una entrada lateral o Rename),
no como mecanismo de limpieza de un RowView compartido con extensiones.

## Rebind y callbacks obsoletos

`RowBinder.Bind` evita rebind si conserva exactamente el mismo modelo activo.
Cuando cambia:

1. invalida cualquier menú cuyo owner sea esa fila;
2. incrementa la generación;
3. elimina los callbacks exactos del binding anterior;
4. libera el contexto Enum/Shader que no corresponda;
5. oculta todos los controles;
6. enlaza únicamente la familia del nuevo RowModel.

El menú valida owner, generación, binding activo, modelo enabled y GameObject
activo antes de abrir o invocar. La entrada de categoría usa el mismo principio
con un `CategoryNavigationEntryBinding` permanente cuyo `Target` se reemplaza.

## Invalidaciones y layout

| Operación | Trabajo lógico actual |
| --- | --- |
| Full `SetList` | una invalidación de cache, una de filas visibles y un pass virtualizado sucio |
| Renderer collapse | `ReplaceRange`: una cache + una visible + un layout síncrono; sin full presentation rebuild |
| Category click ya visible | scroll/rebind de viewport y un anchor programático; sin source enumeration |
| Category click que expande padres | rebuild silencioso y scroll al target resuelto, sin publicación de highlight intermedio |
| Sidebar horizontal | no-op si el estado es igual; una invalidación de layout si cambia |
| Enum value estable | mueve selección; no reconstruye OptionData si el fingerprint no cambió |
| Filter sin cambio | fast path; no modifica GameObjects ni scrollbar |

`VirtualList.ReplaceRange` ejecuta su `Update` sucio antes de retornar. Esto es
intencional: asegura que una child RowView removida y su grafo de listeners no
permanezcan activos hasta el próximo frame. El callback normal del frame vuelve
después al fast path idle.

## Retención acotada

| Estado/cache | Límite y lifetime |
| --- | --- |
| RowViews | High-water por viewport; máximo teórico responsive 44 clones, más template inactivo |
| Enum `OptionData` | 128 shells reutilizables por RowView; una lista activa mayor se muestra completa y compacta al reducirse |
| Filtro persistente de dropdown | 16 keywords de proceso |
| Renderer collapse | 512 claves colapsadas por sesión; clear al invalidar target |
| Renderer child RowModels | Se crean lazy y se retienen solo dentro de la presentación actual tras la primera expansión |
| Category entries | High-water del mayor número de categorías visto por esa vista; targets se neutralizan al liberar presentación |
| Menús | Una superficie global y una de fila, ambas precreadas; acciones se limpian al cerrar |

High-water significa reutilización, no memoria cero después de cerrar. Hace
falta un profiler de Unity para medir el retained size real.

## Trabajo idle conocido

`VirtualList`, dropdown popup, menús y diagnósticos tienen salidas tempranas o
se deshabilitan cuando no hay cambio. `PerformanceDiagnostics` está apagado por
default y no formatea logs de categoría en ese estado.

Existe una excepción deliberada: `MaterialEditorClipboardViewState.Update`
captura un fingerprint semántico mientras el Canvas está activo para detectar
mutaciones directas en las listas públicas legacy de `CopyData`. El scan está
diseñado sin allocation y publica solo si cambió el snapshot, pero su coste es
O(número de edits copiados). Con el Canvas inactivo Unity no ejecuta ese
`Update`. Esta excepción debe incluirse en cualquier perfil idle honesto.

## Evidencia automatizada existente

- Tests de Category: listener permanente y rebind A -> B.
- Tests de Renderer: 100 closes/opens, owner obsoleto, release inmediato,
  invalidaciones y pool.
- Tests de Enum: 500 rebinds, listener del modelo anterior, 64 -> 2 -> 64 y
  cap 128.
- Tests de Copy/Paste: varias vistas, baja por scope, mutación externa y lease
  restaurable.
- Tests de right panels: release lineal de entradas y no-op de layout.
- Harness: listener add/remove balanceado en scroll, categorías, renderer,
  dropdown y 100 open/close.

Los tests con stubs no inspeccionan internals de UnityEvent ni native objects.
El escenario sintético dropdown open/close conserva 40,920 B P95 en ambas rutas
modeladas; la corrección de listeners no afirma eliminar la creación propia del
popup uGUI.

## PENDING — cierre de auditoría

Se requieren 100 ciclos reales de ventana, popup, collapse y cambio de target;
conteo de listeners/callbacks; weak references de renderers/materiales; managed
snapshot; native/GPU memory; logs y comparación antes/después del cierre. Hasta
entonces la conclusión es “rutas conocidas acotadas y regression-guarded”, no
“runtime leak-free”.
