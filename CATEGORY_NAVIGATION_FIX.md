# Corrección de navegación de categorías

## Estado y alcance de la evidencia

La corrección está presente en el árbol de trabajo de
`fix/material-editor-ui-stability-performance`. Este documento describe el
código y las pruebas automatizadas que existen en el repositorio; no representa
una validación visual dentro de Maker o Studio.

- Implementación y cobertura automatizada: **PRESENTES**.
- Ejecución de tests durante esta actualización documental: **NO EJECUTADA**.
- Click, hitbox, scroll y highlight reales en Unity: **PENDING**.

## Identidad y flujo corregidos

La categoría ya no se resuelve por la posición de una entrada reciclada. Cada
`CategoryNavigationTarget` conserva una identidad compuesta por la sección de
material y el nombre lógico de la categoría. Los índices de fila son anclas
mutables de presentación; no son la identidad del click.

El contrato que se mantiene es:

```text
clickedStableKey == navigationStableKey == highlightStableKey
```

El flujo implementado es el siguiente:

1. Cada entrada reutilizable instala sus listeners una sola vez.
2. `CategoryNavigationEntryBinding.Bind` sustituye el target semántico actual.
3. El callback toma un snapshot de ese target al invocarse, por lo que un
   rebind de A a B no puede navegar de nuevo a A.
4. La navegación resuelve de nuevo `sectionId` y `categoryId` después de un
   rebuild que expanda padres o cambie la visibilidad de filas.
5. El destino resuelto se entrega a `VirtualList.ScrollToIndex`; solo se publica
   el anchor actual como fallback si la categoría dejó de existir.

El marcador y los textos pasivos tienen `raycastTarget = false`. El botón de
collapse posee su zona, el botón de nombre posee la suya y el botón del fondo
cubre el espacio restante. Los tres caminos usan el target enlazado actual; no
hay una capa visual pasiva capaz de interceptar el click con otra identidad.

## Navegación atómica y scroll-spy

El click normal llama primero a `EnsureParentsExpanded`. Si ningún padre cambia,
va directamente a `VirtualList.ScrollToIndex` y no reconstruye la presentación.
Solo cuando `EnsureParentsExpanded` cambia estado —o cuando el propio collapse
modifica filas visibles— se usa `RebuildAndScrollToCategory`, que realiza esa
mutación como una operación síncrona:

- cancela refreshes pendientes;
- reconstruye la presentación con `publishViewportAnchor = false`;
- vuelve a localizar la categoría por sus claves estables;
- desplaza la lista al índice ya recalculado;
- evita publicar un highlight intermedio correspondiente al viewport anterior.

La navegación programática fija temporalmente el anchor solicitado. El
scroll-spy no lo reemplaza mientras la posición siga siendo la producida por
esa navegación. Cuando el contenido se mueve de nuevo por scroll del usuario,
el pin se libera y el highlight vuelve a seguir el viewport. La última
categoría conserva su identidad aun cuando el `ScrollRect` deba limitar la
posición porque no puede alinearla exactamente en la parte superior.

## Collapse y cambios de filas

El collapse de categoría guarda las claves antes de reconstruir y vuelve a
resolverlas después. Las anclas repetidas de una categoría se mantienen en la
presentación y sus índices se desplazan cuando una mutación anterior inserta o
elimina filas. Esto evita el drift provocado por Basic/Advanced, filtros,
categorías repetidas y cambios de secciones Renderer/Material/Shader.

## Diagnóstico opt-in

`UI.CategoryInteractionDiagnostics.cs` reutiliza el switch existente
`PerformanceDiagnostics`, desactivado por defecto. Cuando se habilita, registra
solo en eventos de click/navegación/highlight:

- posición del puntero;
- índice visual y número de entrada lateral;
- stable key, listener key, clicked key, navigation key y highlight key;
- bounds mundiales de la fila;
- posiciones de scroll lateral y central;
- origen `programmatic` o `user`.

Con el switch apagado, el camino normal sale antes de formatear o escribir el
mensaje; no existe logging continuo por frame.

## Cobertura automatizada existente

`CategoryNavigationPresentationTests` cubre claves estables tras cambios de
filas, categorías lógicas repetidas, expansión de padres y rebind A -> B de una
entrada con listeners permanentes. `VirtualListScrollContextTests` cubre:

- primera, intermedia y última ancla;
- filas Advanced insertadas y decoys;
- fallback dentro de la categoría;
- publicación atómica sin frame intermedio;
- pin programático y recuperación por scroll manual;
- limpieza del pin durante el ciclo de vida;
- igualdad click/navegación/highlight;
- chevron y navegación usando el mismo target;
- un rebuild de modo sin restore duplicado.

Estas pruebas son contratos de presentación y componentes con stubs; no
simulan el EventSystem, el CanvasScaler ni la geometría renderizada del juego.

## PENDING — validación del usuario en Unity

Quedan pendientes los casos `CAT-*` de `USER_UI_FIXES_TEST_CHECKLIST.md`, en
especial categorías muy juntas, primera/última, cambio de shader, scroll rápido
y cinco o más ciclos de rebind. Si falla, el informe útil debe incluir el tramo
de `BepInEx/LogOutput.log` con `PerformanceDiagnostics` habilitado y una captura
que muestre simultáneamente la categoría pulsada, el destino central y el
highlight lateral.
