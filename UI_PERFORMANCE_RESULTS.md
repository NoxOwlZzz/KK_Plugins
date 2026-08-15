# Material Editor UI performance results

## Evidencia final post-build y post-deploy

El candidato final fue compilado y desplegado desde el commit fuente
`8bcf8649`. El build completo terminó con exit code 0; metadata/regression,
API, harness y la matriz Release AI/EC/HS2/KK/KKS/PH terminaron sin errores.
El cierre documental no volvió a ejecutar esos comandos: verificó los
artefactos que dejaron.

### Artefactos finales verificados

| Artefacto | Tamaño | SHA-256 | Estado |
| --- | ---: | --- | --- |
| DLL de build e instalado | 1,088,000 B | `72EA76AC8AB31BAE28E67FA53AED658A28E585BA78B85CCF08DC80F9B8405F95` | bytes idénticos; assembly `4.0.3.0`, file version `4.0.3` |
| XML de documentación (build/ZIP) | 423,700 B | `0765558075A34C37E76BD75159E21BC2E1B029819BB29A78BD8B0F3A1215EDFC` | incluido en el paquete; no es input runtime desplegado |
| `libwebp.lib` de build/instalación | 604,672 B | `8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966` | sin cambios |
| ZIP final | 891,917 B | `E4DAD44BAA86DDFFD5F2282D04D6664D004520677DCC0FD34B5D0CF9E1584A9F` | tres entradas: DLL, XML y native; mtime UTC `2026-08-09T17:56:50.2698159Z` |
| JSON de rendimiento final | 59,092 B | `8986ADAA352AA00D342641A7DE7E24471CA218EDDDCC67A482D84BB2792703CC` | schema 2; 45 resultados y 45 invariantes |

Antes de copiar se creó
`D:/Games/Koikatsu/BepInEx/PluginBackups/MaterialEditor/20260809-135846-before-ui-fixes`.
Su `BACKUP_INFO.txt` tiene SHA-256
`4A0EDEFEB8194E24EEF4D36F10D6BD5EBCB7723216E01D4222E039A53DCCA9B2`;
registra el DLL previo de 1,088,000 B con SHA-256
`4E0D4C6C5F5582EC9CE2F88A75ECA12739220A3943D2DC2C65808C241BE0DFFA`
y el native sin cambios. El deploy verificó payload y manifest al crear el
backup; no se volvió a ejecutar un restore `--verify-only` separado sobre este
backup durante el cierre documental. El deploy no copió configuración ni
datos. La configuración post-deploy observada tiene SHA-256
`19B5F5E4569E0A3E8F640C6FBAF0C8140FE9BA331498C6DC1BE0A31CC6621AA7`;
sin un hash inmediatamente anterior registrado para este despliegue, no se
afirma una comparación byte a byte entre candidatos.

### Harness final

- Estrategia:
  `linked-production-helpers-plus-synthetic-ui-operation-count-model`.
- Runtime: .NET `8.0.21`.
- Generado UTC: `2026-08-09T17:56:34.4741749Z`.
- Fingerprint:
  `06EA97B153643891C182D99009B0AA4EFC2FB1B2C410D7B660F21397A8E93AF2`.
- 45/45 resultados tienen `semanticChecksPassed = true`.
- 45/45 invariantes pasan.
- Ningún resultado termina con listeners activos en el modelo.

Los 11 pares legacy/optimized del bloque `ui-fixes` del JSON final son:

| Escenario (100 operaciones cuando se indica) | Mediana/P95 ms legacy -> optimized | Mediana/P95 alloc legacy -> optimized | Rebuilds lógicos legacy -> optimized | Lectura honesta |
| --- | ---: | ---: | ---: | --- |
| All expanded | 0.0388/0.0599 -> 0.0338/0.0506 | 60,224/60,224 -> 60,224/60,224 B | 1 -> 1 | Tiempos observados menores, pero mismo trabajo y allocation modelados; no prueba ganancia estructural |
| Renderers collapsed | 0.0002/0.0015 -> 0.0003/0.0023 | 120/120 -> 120/120 B | 0 -> 0 | Mediana y P95 mayores; sin ganancia de allocation |
| Sidebar collapsed | 0.0002/0.0020 -> 0.0002/0.0015 | 120/120 -> 120/120 B | 0 -> 0 | Mediana igual y P95 menor; el trabajo idle modelado no cambia |
| Renderer toggle 100 | 2.4773/2.6582 -> 0.0095/0.0132 | 6,009,720/6,009,720 -> 120/120 B | 100 -> 0 | Reemplazo dirigido de filas en el modelo, sin full rebuild |
| Sidebar toggle 100 | 0.0017/0.0037 -> 0.0017/0.0033 | 120/120 -> 120/120 B | 0 -> 0 | Mediana igual, P95 ligeramente menor y una invalidación de layout por toggle en ambos modos; sin ganancia de allocation |
| Rapid scroll 100 | 0.0199/0.0268 -> 0.0198/0.0268 | 120/120 -> 120/120 B | 0 -> 0 | Mediana apenas menor y P95 igual; diferencia host-sensitive |
| Category click 100 | 2.6580/3.1116 -> 0.0195/0.0227 | 6,010,520/6,010,520 -> 120/120 B | 100 -> 0 | Trabajo dirigido del viewport; cero rebuild, source enumeration y refresh modelados en optimized |
| Dropdown open/close 100 | 0.1158/0.1813 -> 0.0907/0.1204 | 40,920/40,920 -> 40,920/40,920 B | 0 -> 0 | Tiempos observados menores y listeners balanceados; allocation modelada sin cambio |
| Dropdown rebind 100 x 20 | 0.4847/1.4097 -> 0.0181/0.0230 | 120/120 -> 120/120 B | 0 -> 0 | Rebuilds de opciones acumulados: 16,000 -> 160 en 8 muestras |
| Shader change 100 | 2.9619/4.3070 -> 2.5299/2.7332 | 6,010,520/6,010,520 -> 6,010,520/6,010,520 B | 100 -> 100 | Tiempos observados menores, pero mismo trabajo y allocation modelados; no prueba ganancia estructural |
| Window open/close 100 | 2.5146/3.9804 -> 2.4219/2.4837 | 6,010,520/6,010,520 -> 6,010,520/6,010,520 B | 100 -> 100 | Tiempos observados menores, pero el ciclo completo sigue reconstruyendo; no prueba ganancia estructural |

Las cifras cambian entre ejecuciones por JIT, warm-up y scheduling. No se
convierten en porcentajes de FPS ni allocations de Unity. Las mejoras
estructurales afirmables se limitan a los contadores modelados de renderer
toggle, category click y dropdown rebind; el resto se informa sin fabricar una
mejora.

En los 22 resultados UI, `retainedMemoryDeltaBytes` queda entre 96 y 120 B.
Esa métrica y `approximateMemoryBytes` pertenecen al proceso sintético .NET;
no son retained size de Unity ni permiten afirmar una mejora de fugas o memoria
del juego.

### Smoke y frontera runtime

No se registró un smoke post-deploy de Maker o CharaStudio para el DLL exacto
`72EA76AC...05F95`. El smoke anterior con el editor cerrado corresponde al DLL
previo `4E0D4C6C...DFFA`, ahora guardado en el backup, y no demuestra la
inicialización de este candidato.

Siguen **PENDING** el load smoke del candidato exacto, los 29 casos de
`USER_UI_FIXES_TEST_CHECKLIST.md`, apariencia e interacción, persistencia, FPS,
frame-time, allocations por frame, memoria managed/native/GPU, latencia y 100
ciclos reales. Build y deploy no prueban esos resultados ni ausencia de fugas
con la ventana abierta.

## Historial anterior del rediseño de tres paneles

### Honest status at that snapshot

No post-redesign Unity FPS, frame-time, managed-allocation, native-memory,
GPU-memory, input-latency or 100-cycle Maker/Studio measurement has been run.
Those results are **PENDING**. A successful compile, metadata contract or
synthetic .NET 8 harness is not a runtime UI performance measurement.

The synthetic harness was regenerated from commit `8c36e8df` and compared with
the frozen `pre-three-panel-ui.json` baseline. Its linked production helpers do
not include `MaterialEditorWindowView`, rows, popup menus or `VirtualList`, and
it neither creates Unity objects nor renders a frame. The numbers below are a
semantic/allocation regression gate for the algorithms it does exercise, not
evidence of UI FPS, scroll allocation, idle CPU or responsiveness.

### Final synthetic comparison at that snapshot

- Report: `bin/build/materialeditor-performance-three-panel-ui.json`.
- Generated UTC: `2026-08-09T06:32:39.3794532Z`.
- Size: 32,241 bytes.
- SHA-256:
  `C06A755CD9E011519BEFC28F4FF34C02774ABD7F90EBE55B6CEB3D91E16059AD`.
- Runtime: .NET 8.0.21.
- Result: 23/23 scenarios and 21/21 invariants passed.
- Semantic fingerprint:
  `FF5F3CFC956BFC040F14F94537F418C1956FBDAA09827C57B44857FABC852C1F`,
  unchanged from the frozen baseline.
- P95 allocated bytes: identical to the frozen baseline in all 23 scenarios.

| Synthetic workload | Baseline P95 ms | Final P95 ms | Delta |
| --- | ---: | ---: | ---: |
| Simple rebuild | 0.0081 | 0.0083 | +2.5% |
| KKLT-like Basic rebuild | 0.0969 | 0.0837 | -13.6% |
| KKLT-like Advanced rebuild | 0.0895 | 0.0823 | -8.0% |
| KKLT-like search | 0.3210 | 0.2940 | -8.4% |
| 100-target equal aggregation | 2.3647 | 0.5207 | -78.0% |
| Longevity composite | 79.3577 | 78.2619 | -1.4% |

These microbenchmarks, especially the 100-target result, are sensitive to host
scheduling, JIT and warm-up noise. The deltas are reported exactly for
reproducibility; they are not claimed as user-visible improvements. The
important automated acceptance facts are unchanged semantics, unchanged P95
allocation counts and a passing comparison policy.

### Post-redesign evidence available at that snapshot

| Area | Source/contract evidence | Runtime result |
| --- | --- | --- |
| Virtual rows | High-water cache capped at 44, one overscan slot, active capacity released on shrink; 100 resize iterations in production-linked test | **PENDING Unity measurement** |
| Idle virtual list | Existing `Update` returns when scroll/viewport/dirty state is unchanged; idle contract observes no growth, rebind or layout mark | **PENDING Unity measurement** |
| Canvas resize | `OnRectTransformDimensionsChange` recomputes layout; no new responsive `Update` loop | **PENDING Unity measurement** |
| Row actions | One Canvas surface, four buttons/labels/tooltips precreated; no menu GameObject/listener creation per open | **PENDING Unity measurement** |
| Global menu | One reusable top-bar surface; Escape polling enabled only while open | **PENDING Unity measurement** |
| Category navigation | Reusable entry high-water with permanent listeners and bind/release | **PENDING Unity measurement** |
| Right filters | Visible count updated inside existing Add/Filter/Release passes; no second enumeration for empty state | **PENDING Unity measurement** |
| Empty feedback | One passive precreated Text per surface; event-driven updates | **PENDING Unity measurement** |
| Tooltips | One Canvas tooltip panel; per-control tooltip components retain text/state | **PENDING Unity measurement** |
| Property bind | Float range layout restores pooled width/visibility idempotently; no slider listener for an unbounded float | **PENDING Unity measurement** |

### Bounded responsive pool

The maximum responsive main height is 972 logical units. After the 40-unit
top bar and 7.5 units of margin allowance, the maximum central viewport is
924.5. With fixed 22-unit rows and one fractional-scroll overscan slot:

```text
ceil(924.5 / 22) + 1 = 44 cached row views maximum
```

The cache only grows to its high-water mark. When a viewport shrinks, excess
views remain available for reuse but are released: their model is null, their
listeners are inactive and they are not included in padding/offscreen math.
This is a bounded lifecycle policy, not proof of a particular in-game memory
number.

The pool owns one inactive clean template in addition to cached clones. The
default contract scenario with 13 models uses 13 active clones plus that
template; the template is never counted as an active `RowView`. This is a
source/test count, not an observed Unity object or memory total.

### Earlier optimization reference

The frozen synthetic report recorded, among other results:

- KKLT-like search allocation: 44,056 B -> 9,224 B P95;
- 100-target equal allocation: 44,320 B -> 7,528 B P95;
- longevity composite allocation: 7,338,520 B -> 3,866,008 B P95;
- numeric CRC64: 1,440,000 B -> 0 B over 10,000 calls;
- stable Enum/Dropdown rebind: 128,000 B -> 0 B after warm-up.

These earlier figures and the final comparison above test production-linked
algorithms outside Unity. They must not be quoted as Material Editor UI
allocations, FPS improvements or rendered-frame timings after the redesign.

### Required runtime measurement

To complete this document, run the same build in Maker and Studio and record:

1. cold and warm open time;
2. frame-time distribution while idle, scrolling and filtering;
3. managed allocation per frame during 100 stable idle frames;
4. allocation and active/bound row count during repeated viewport growth and
   shrink;
5. 100 open/close cycles and 100 menu open/close cycles;
6. retained managed/native memory after close and target replacement;
7. dropdown, color picker and tooltip interaction latency;
8. logs and profiler captures with exact game/build/config identifiers.

Until those artifacts exist, the runtime column remains **PENDING** and no FPS
or percentage improvement is claimed.
