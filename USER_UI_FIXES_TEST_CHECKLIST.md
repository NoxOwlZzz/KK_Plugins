# Checklist manual de UI fixes para el usuario

## Estado

Codex **no ejecutó** esta checklist. Todos los casos comienzan en **PENDING** y
solo el usuario debe cambiarlos a PASS/FAIL después de realizar la acción en el
juego y guardar la evidencia. Un build o test automatizado no convierte un
caso visual en PASS.

Antes de probar:

1. cierre instancias anteriores y use una copia descartable de card,
   coordinate o scene;
2. anote versión, tamaño y SHA-256 del DLL realmente instalado;
3. conserve la configuración, shader/mod usado, resolución y UI scale;
4. para un fallo, copie el tramo de
   `D:/Games/Koikatsu/BepInEx/LogOutput.log` desde antes de la acción hasta que
   el estado se estabilice o aparezca la excepción;
5. no combine los roundtrips de card, coordinate y scene en una sola prueba.

## Categorías, subcategorías y navegación

| ID | Acción | Resultado esperado | Log que copiar si falla | Captura útil | Estado |
| --- | --- | --- | --- | --- | --- |
| CAT-01 | En un shader con categorías muy juntas, pulse cinco categorías distintas en orden, usando alternativamente fondo, texto y espacio lateral de cada entrada. | Cada click navega e ilumina exactamente la misma categoría; una acción produce un solo cambio. | Intervalo completo; repita con `PerformanceDiagnostics` habilitado e incluya líneas `[MaterialEditor category]`. | Video/captura donde se vean puntero, categoría lateral y header central. | **PENDING** |
| CAT-02 | Pulse primera, intermedia y última categoría; repita tras scroll al final. | Las tres quedan marcadas correctamente; la última permanece seleccionada aunque no pueda alinearse arriba. | Líneas de click/navigation/highlight y posiciones de scroll. | Antes/después de cada extremo con ambos paneles visibles. | **PENDING** |
| CAT-03 | Cierre y abra una categoría desde el chevron, nombre y fondo del header central y desde el panel izquierdo. | Cada superficie alterna una vez, conserva altura/alineación y no activa una categoría vecina. | Tramo de los cuatro clicks y cualquier excepción. | Secuencia expandida/cerrada/restaurada. | **PENDING** |
| CAT-04 | Navegue por click y luego haga scroll manual lento y rápido. | El click programático conserva su highlight; el scroll manual vuelve a actualizarlo sin saltos ni filas stale. | Diagnóstico con `scrollOrigin=programmatic` y después `user`. | Video continuo con centro y navegador. | **PENDING** |
| CAT-05 | Cambie a otro shader, aplique Filter, limpie Filter y vuelva al anterior. | Las entradas se rebindean al nuevo contexto; no queda listener, tooltip o highlight del shader previo. | Desde antes del cambio de shader hasta el retorno. | Navigator y nombre de shader antes/después. | **PENDING** |
| HIER-01 | Compare un manifest plano con ejemplos Category-only y Subcategory; repita con Filter. | Cada función aparece únicamente cuando se declara. Subcategory solo organiza, no añade `ShowIf`, no cambia shader y ninguna apertura/fold escribe material. | Parse warnings, rebuilds y errores de cada manifest. | Misma selección mostrando los tres casos y el resultado de Filter. | **PENDING** |
| HIER-02 | Pruebe flecha, nombre y fondo de Category/Subcategory; después haga scroll para reciclar las filas. | Cada click alterna un solo grupo, no modifica propiedades y el estado de fold se conserva tras el rebind. | Callbacks, valores antes/después y rebuilds. | Expandido, cerrado y restaurado. | **PENDING** |

## Base oscura, jerarquía y legibilidad

| ID | Acción | Resultado esperado | Log que copiar si falla | Captura útil | Estado |
| --- | --- | --- | --- | --- | --- |
| VIS-01 | Abra una vista con Renderer, Material, Shader, Category y propiedades visibles. | La UI usa una base oscura; Renderer azul oscuro, Material ciruela, Shader azul verdoso, Category violeta y propiedades neutrales se distinguen sin depender del nombre. | Log de apertura y selección. | Ventana completa sin edición de color. | **PENDING** |
| VIS-02 | Compare normal, hover, selected, modified, Mixed y disabled. | Selected no se confunde con Hover; texto disabled sigue legible; Mixed/Modified no dependen solo del color. | Tramo de interacción y valores usados. | Collage a resolución original de todos los estados. | **PENDING** |
| VIS-03 | Use nombres muy largos de shader, renderer, material, categoría y propiedad a scales 1, 1.75 y 3. | No hay overlap; truncado/best-fit conserva el control y el tooltip muestra el nombre real. | Config de scale y log de hover. | Cada label truncado con su tooltip. | **PENDING** |
| COLOR-01 | Abra el color picker, ciérrelo con el mismo control y vuelva a abrirlo cinco veces; repita después de hacer scroll/rebind y cambie un color una vez. | Abre en todos los ciclos, el segundo click lo oculta, el siguiente lo reabre y la edición afecta solo al color previsto. | Secuencia completa y cualquier mensaje de palette/excepción. | Video continuo de los cinco ciclos y el rebind. | **PENDING** |

## Copy Edits / Paste Edits

| ID | Acción | Resultado esperado | Log que copiar si falla | Captura útil | Estado |
| --- | --- | --- | --- | --- | --- |
| CLIP-01 | Inspeccione una fila de material en ancho amplio, medio y reducido; pulse `Copy Edits`. | `Copy Edits` y `Paste Edits` siempre son visibles con contexto; Copy usa el backend una vez y no abre `...`. | Desde selección del material hasta el click, con identidad del target. | Fila completa en los tres anchos. | **PENDING** |
| CLIP-02 | Con clipboard vacío, inspeccione/hover `Paste Edits` e intente pulsarlo. | Permanece visible pero disabled, explica que primero hay que copiar y no escribe ni crea Undo. | Tramo del intento y estado previo del material. | Botón disabled y tooltip visible. | **PENDING** |
| CLIP-03 | Copie edits, pruebe Paste en material compatible y luego en uno incompatible; incluya projector si está disponible. | Compatible queda enabled y aplica una vez; incompatible sigue visible/disabled con razón correcta; projector usa el target correcto. | Valores antes/después, material/projector y errores. | Estados enabled/disabled y resultado renderizado. | **PENDING** |

## Dropdowns

| ID | Acción | Resultado esperado | Log que copiar si falla | Captura útil | Estado |
| --- | --- | --- | --- | --- | --- |
| DROP-01 | Abra Shader dropdown, verifique todos los nombres, seleccione otro compatible y use `Reset`. | Control, filter y popup son oscuros con texto claro; hover/pressed/selected/disabled se distinguen, la selección azul usa texto blanco; abrir no escribe y cada selección/reset escribe una vez y conserva caption. | Shader original, elegido, restaurado y callbacks/excepciones. | Caption, filter y lista abierta antes/seleccionada/reset. | **PENDING** |
| DROP-02 | Filtre un nombre largo con texto, `*` y `?`; use `Clear`; cierre/reabra 10 veces. | Filter y Clear funcionan, Reset nunca desaparece, no hay opciones residuales ni listeners acumulados. | Ciclos completos, términos y cualquier warning repetido. | Popup con filter, resultados y nombre largo. | **PENDING** |
| DROP-03 | Pruebe Enum normal, Mixed, Unknown, disabled, valor duplicado y descriptor de manifest largo. | Conserva cada semántica, muestra todas las opciones, no autoelige la primera ni escribe al abrir. | Descriptor/valor, selección y callback count si existe diagnóstico. | Caption y popup de cada estado representativo. | **PENDING** |

## Renderer y sidebar derecho

| ID | Acción | Resultado esperado | Log que copiar si falla | Captura útil | Estado |
| --- | --- | --- | --- | --- | --- |
| REND-01 | Cierre/abra un renderer desde fondo, chevron y nombre; haga 20 ciclos mientras observa sus propiedades. | Solo desaparecen/reaparecen sus cinco filas; ningún valor, card o scene cambia y no aparecen filas duplicadas. | Identidad del renderer, ciclos y cualquier setter/exception. | Expandido, cerrado y restaurado; idealmente video. | **PENDING** |
| REND-02 | Use dos renderers con el mismo nombre; cierre solo uno y abra `...` para Export UV/OBJ sin ejecutar si no es seguro. | Estados independientes; `...` no alterna collapse y pertenece al renderer correcto. | Ruta/índice de ambos renderers y apertura del menú. | Ambos headers y el menú abierto. | **PENDING** |
| SIDE-01 | Anote el rect central, colapse globalmente Renderers/Materials y reabra desde el rail. | El rail de 24 queda visible dentro de la reserva derecha; anchors, ancho, alto y viewport del centro son idénticos antes, durante y después. | Dimensiones/scale y rect central de los tres estados, más cualquier layout warning. | Ventana completa expandida, rail y restaurada con bordes centrales comparables. | **PENDING** |
| SIDE-02 | Seleccione entradas, escriba filters distintos, desplace ambas listas, cierre/reabra el sidebar. | Selección, filters y scroll de cada lista se conservan; no cambia material ni crea Undo. | Selecciones, filtros y scroll antes/después. | Comparativa antes/después con ambas listas. | **PENDING** |
| SIDE-03 | Pliegue Renderer y Material por separado, ambos juntos, abra Rename, vuelva y use el toggle global. | Los folds verticales son independientes; dos headers se apilan; Rename no cambia la preferencia horizontal. | Orden exacto de acciones y callbacks. | Los cuatro estados o video continuo. | **PENDING** |

## Estrés y rendimiento subjetivo

| ID | Acción | Resultado esperado | Log que copiar si falla | Captura útil | Estado |
| --- | --- | --- | --- | --- | --- |
| PERF-01 | Con un shader largo, haga scroll rápido, clicks rápidos de categoría y Filter/Clear durante dos minutos. | Sin filas blancas/stale/duplicadas, highlight incorrecto, freeze ni spam de log. | Los dos minutos completos y config/shader. | Video continuo con contador/profiler si dispone de él. | **PENDING** |
| PERF-02 | Use muchos materiales y renderers; expanda/cierre secciones y sidebar repetidamente. | Pool y UI permanecen estables; filtros/listas responden y no se percibe degradación acumulativa. | Cantidades, 100 ciclos si es viable y errores. | Inicio/fin con mismo target y memoria del profiler si existe. | **PENDING** |
| PERF-03 | Compare la misma secuencia, cámara, config y build anterior/candidato. | No hay regresión subjetiva evidente; cualquier diferencia se describe como observación, no como FPS o porcentaje sin medición. | Hashes de ambos DLL, logs completos y profiler si existe. | Videos emparejados de la secuencia exacta. | **PENDING** |

## Funciones y persistencia

| ID | Acción | Resultado esperado | Log que copiar si falla | Captura útil | Estado |
| --- | --- | --- | --- | --- | --- |
| FUNC-01 | En una copia descartable pruebe Reset, texture import/export, Render Queue, Mixed/multi-selection y acciones UV/OBJ válidas. | Cada función existente sigue su backend y actúa una vez; el rediseño no elimina ni redirige semántica. | Acción, target, valor/path redacted y error completo. | Antes/después y confirmación del archivo cuando corresponda. | **PENDING** |
| COMPAT-01 | Abra contenido con manifest schema 1, schema 2 plano, schema 2 jerárquico y una versión futura. | Schema 1 y schema 2 plano conservan la presentación previa; la jerarquía es opt-in; una versión futura advierte y conserva el subconjunto schema 2 conocido. `UiLevel`/`Type="Toggle"` de prototipos no publicados no se tratan como contratos. | Identidad/versión del manifest y todos los parse warnings. | Filas equivalentes de los manifests planos y jerarquía solo en el que la declara. | **PENDING** |
| SAVE-01 | Card: primero abra/cierre sin editar; luego haga una edición conocida, guarde y recargue en un archivo nuevo. | La pasada view-only no añade edits; la edición explícita persiste por la ruta existente y el estado de panel no se serializa. | Log separado de load/view-only/save/reload con identidad. | Misma cámara antes, view-only, editada y recargada. | **PENDING** |
| SAVE-02 | Coordinate: repita el roundtrip aislado, incluyendo partial load si se usa normalmente. | Solo la edición explícita persiste; partial load conserva su semántica previa y no importa collapse/filter. | Log y timestamps/hashes del archivo descartable. | Secuencia antes/después/recarga. | **PENDING** |
| SAVE-03 | Studio scene: repita view-only y una edición; pruebe Scene Import por separado. | Scene load/import conserva datos reales y no serializa estado UI; no hay cross-target ni callback obsoleto. | Load/save/import completo con item/material/renderer IDs. | Misma cámara antes, editada, reload e import. | **PENDING** |

## Registro de cierre

| Resultado | Cantidad inicial |
| --- | ---: |
| PASS | 0 |
| FAIL | 0 |
| BLOCKED | 0 |
| **PENDING** | **30** |

Cada FAIL debe enlazarse desde `KNOWN_UI_LIMITATIONS.md` o convertirse en una
corrección rastreada. No borre evidencia de un fallo al repetir el caso.

## Secciones de renderer y material

- Abra el menú global `...` y elija `Collapse all renderer/material sections`.
- Verifique que se contraigan todos los encabezados de renderer y material de la lista principal, sin alterar el estado de las categorías.
- Reabra el menú, elija `Expand all renderer/material sections` y verifique que las mismas secciones se expandan nuevamente.
