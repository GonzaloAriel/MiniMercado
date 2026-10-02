# MANUAL — MEMORIA PROGRESIVA PARA AGENTES IA

FORMATO: 1.0

ALCANCE: Genérico. Aplicable a cualquier proyecto de software, cualquier stack,
cualquier modelo de IA.

---

## 1. Cómo usar este manual

Este documento es un prompt autocontenido. Se entrega a un agente IA junto con
acceso al proyecto y el agente obtiene todo lo necesario para construir y
mantener una memoria progresiva.

Tres modos de uso:

- MODO A — Proyecto sin memoria (`AI/` no existe): ejecutar diagnóstico
  completo (sección 5) y construcción (sección 6).
- MODO B — Proyecto con memoria (`AI/` ya existe): ejecutar directamente el
  protocolo de arranque (sección 7) y mantenimiento (sección 8).
- MODO C — Proyecto con memoria desactualizada: ejecutar arranque (sección 7),
  luego reconciliar la memoria con la realidad del código antes de trabajar.

Si algo de este manual contradice una decisión VIGENTE registrada en la
memoria del proyecto, gana la decisión registrada.

---

## 2. Concepto: qué es la memoria progresiva

La memoria progresiva es un sistema de archivos Markdown que captura el ESTADO,
las DECISIONES y los PROBLEMAS de un proyecto, de forma que cualquier agente —
o humano — pueda continuar el trabajo SIN conocer las conversaciones anteriores.

La conversación es efímera. La memoria es permanente.

La memoria no guarda lo que se habló: guarda lo que se decidió, lo que existe y
lo que falla. Es un contrato operativo entre agentes sucesivos.

### Principios irrenunciables

1. **Continuidad**: la memoria reemplaza a la conversación. Un agente nuevo con
   la memoria debe poder trabajar como si hubiera estado siempre.
2. **Costo mínimo de arranque**: con leer 2 archivos se tiene contexto
   actualizado. Nada más.
3. **Lectura dirigida**: existen rutas de lectura por tipo de tarea. Nunca se
   lee todo: se lee lo que la tarea requiere.
4. **Fuente única**: la memoria operativa (estado/decisiones/problemas) no
   duplica la documentación de producto (DOCS/). Solo la referencia.
5. **Confirmación humana**: solo el TEAM LEAD (el humano responsable) confirma
   que una etapa está terminada y funcionando. Nunca se auto-consolida.
6. **Historial inmutable**: las decisiones se reemplazan, no se borran. El
   error corregido también es conocimiento.
7. **Registro selectivo**: no guardar conversaciones, logs, razonamientos
   internos, opiniones ni propuestas no aprobadas.
8. **Memoria actualizada**: una memoria desactualizada es peor que no tener
   memoria. Actualizar es parte de terminar una tarea, no un extra.

---

## 3. Roles

**TEAM LEAD** — El humano dueño del proyecto.
- Única autoridad que confirma CONSOLIDADO.
- Toma las decisiones finales de arquitectura y producto.
- Aprueba o rechaza propuestas del agente.

**AGENTE** — Ejecuta, registra y propone.
- Lee la memoria al arrancar. Siempre.
- Respeta las decisiones VIGENTE. No las re-discute salvo pedido explícito.
- Ante duda de arquitectura: detenerse, explicar, proponer alternativas.
  NO decidir sin confirmación.
- Al terminar una tarea: actualiza los registros correspondientes.
- Nunca marca CONSOLIDADO por sí mismo.

---

## 4. Estructura de archivos

```
proyecto/
├── AI/                          <- Memoria operativa (obligatoria)
│   ├── 00_MEMORIA.md            <- Índice maestro. Puerta de entrada.
│   ├── 01_ESTADO.md             <- Estado real por módulo.
│   ├── 02_DECISIONES.md         <- Registro de decisiones técnicas (D-XXX).
│   ├── 03_PROBLEMAS.md          <- Problemas conocidos (P-XXX).
│   ├── PROMPT_INICIO.md         <- Prompt de arranque específico del proyecto.
│   └── SESION_YYYYMMDD_*.md     <- Resúmenes de sesión (capa efímera).
│
└── DOCS/                        <- Documentación de producto (recomendada)
    ├── 00_VISION.md             <- Visión y filosofía del producto.
    ├── 01_AGENTS.md             <- Reglas del proyecto para agentes.
    └── ...                      <- Roadmap, arquitectura, UX, DER, etc.
```

### Responsabilidad de cada capa

| Capa | Guarda | No guarda |
|------|--------|-----------|
| `DOCS/` | El "qué" y el "por qué" del producto: visión, reglas de negocio, diseño, roadmap | Estado actual, decisiones puntuales, problemas |
| `AI/` | El "cómo estamos hoy": estado, decisiones tomadas, problemas abiertos | Documentación de producto, conversaciones, logs |
| `SESION_*` | Resumen de qué cambió en una sesión concreta | Nada permanente (se consolida y queda como referencia) |

Si el proyecto ya tiene documentación propia (README, wiki, carpeta docs),
NO se duplica: se referencia desde `00_MEMORIA.md` en "Fuentes de verdad".

---

## 5. PASO 0 — Diagnóstico del proyecto

Antes de crear un solo archivo, el agente debe conocer la realidad. La memoria
describe lo que ES, no lo que se supone que es.

Analizar y anotar:

1. **Stack tecnológico**: lenguajes, framework, ORM/base de datos, auth, UI,
   patrones en uso (no los declarados: los reales).
2. **Arquitectura**: flujo de capas, convenciones de carpetas, reglas
   inviolables que ya existen en el código.
3. **Módulos**: lista de áreas funcionales del sistema y su madurez aparente.
4. **Convenciones**: nomenclatura, estilos, patrones repetidos en el código.
5. **Historia**: revisar `git log --oneline` reciente, README, comentarios de
   configuración. Buscar decisiones ya tomadas que deban registrarse.
6. **Documentación existente**: qué hay y dónde vive (para referenciarla).

Resultado del diagnóstico: una ficha breve que el agente presenta al TEAM LEAD
antes de generar los archivos. Si algo es ambiguo, preguntar. No inventar.

---

## 6. Construcción de la memoria (bootstrap)

Orden de creación (importante: primero los registros, último el índice):

**PASO 1 — Crear carpeta `AI/`** en la raíz del proyecto.

**PASO 2 — Crear `AI/02_DECISIONES.md`** con su plantilla (sección 6.3).
Sembrar las decisiones ya detectadas en el diagnóstico (IDs D-001, D-002...
en orden cronológico aproximado). Si una "decisión" es en realidad una regla
del stack o del estilo de código, pertenece a DOCS/ o al índice de 00_MEMORIA,
no aquí. Solo decisiones reales tomadas.

**PASO 3 — Crear `AI/03_PROBLEMAS.md`** con su plantilla (sección 6.4).
Registrar problemas conocidos y abiertos del diagnóstico (P-001, P-002...).

**PASO 4 — Crear `AI/01_ESTADO.md`** con su plantilla (sección 6.2).
Listar cada módulo detectado con su estado honesto según evidencia:
CONSOLIDADO solo si hay confirmación explícita previa del TEAM LEAD; si no,
EN PROGRESO o PENDIENTE. Ante duda, PENDIENTE.

**PASO 5 — Crear `AI/00_MEMORIA.md`** con su plantilla (sección 6.1),
completando: descripción del proyecto, stack real, arquitectura real,
objetivo actual (consultar al TEAM LEAD), fuentes de verdad, y las rutas de
lectura adaptadas a este proyecto.

**PASO 6 — Crear `AI/PROMPT_INICIO.md`** (sección 6.5): el prompt de arranque
específico del proyecto, con lectura mínima obligatoria, rutas de lectura por
tipo de tarea y reglas inviolables del stack.

**PASO 7 — Validar coherencia** con el checklist de la sección 9.

**PASO 8 — Informar al TEAM LEAD**: presentar la memoria creada, pedir
correcciones y confirmación del objetivo actual en 00_MEMORIA.md.

A partir de aquí, todo trabajo se rige por las secciones 7 y 8.

---

## 7. Protocolo de arranque (toda sesión, todo agente)

Rito de inicio obligatorio, en orden:

1. Leer `AI/00_MEMORIA.md` → índice, arquitectura, plan activo, rutas de lectura.
2. Leer `AI/01_ESTADO.md` → qué está CONSOLIDADO y qué no.
3. Identificar el TIPO DE TAREA y leer solo los archivos que la ruta indica
   (ver rutas de lectura en 00_MEMORIA.md).
4. Verificar que ninguna decisión VIGENTE se contradice con lo que se va a hacer.
5. Recién entonces, trabajar.

No leer más archivos de arranque. Las sesiones anteriores son resumen ya
consolidado: solo leerlas si la tarea lo pide explícitamente.

---

## 8. Protocolo de mantenimiento (durante y al cierre de cada tarea)

### Qué va a dónde (tabla rápida)

| Evento | Registro destino |
|--------|------------------|
| Se toma una decisión técnica o de arquitectura | `02_DECISIONES.md` (nuevo D-XXX) |
| Una decisión anterior deja de aplicar | Se marca REEMPLAZADA/CANCELADA; nunca se borra |
| Algo no funciona o empieza a fallar | `03_PROBLEMAS.md` (nuevo P-XXX) |
| Un problema se resuelve | Se mueve a RESUELTOS con causa y solución |
| Una etapa termina Y el TEAM LAAD la confirma | `01_ESTADO.md` pasa a CONSOLIDADO con fecha |
| Cambios estructurales del proyecto (stack, arquitectura) | `00_MEMORIA.md` |
| Fin de una sesión de trabajo intensa | `SESION_YYYYMMDD_*.md` (opcional, tras consolidar) |

### Reglas de IDs

- Decisiones: `D-001`, `D-002`... correlativo global, sin reutilizar números.
- Problemas: `P-001`, `P-002`... correlativo global, sin reutilizar números.
- Sesiones: `SESION_YYYYMMDD_TEMA.md`.

### Formato de una decisión (ficha)

Fecha / Decision / Motivo / Consecuencias / Reemplaza a / Estado.
Actualizar SIEMPRE el índice al agregar una ficha.

### Cierre de sesión (checklist obligatorio)

1. ¿Lo decidido está en 02_DECISIONES.md?
2. ¿Lo roto está en 03_PROBLEMAS.md?
3. ¿Lo confirmado por el TEAM LEAD está en 01_ESTADO.md?
4. ¿00_MEMORIA.md sigue siendo cierto (objetivo, plan activo)?
5. Verificación técnica ejecutada (compilar, tests, lint: según proyecto).
6. Commit solo bajo pedido explícito del TEAM LEAD.

Si la sesión fue extensa y lo amerita, crear `SESION_YYYYMMDD_TEMA.md`
con: Objetivo / Estado resultante / Cambios por archivo / Decisiones nuevas /
Reglas aplicadas / Pendientes.

---

## 9. Checklist de validación (al crear y periódicamente)

- [ ] Existen los 5 archivos base de `AI/`.
- [ ] Cada archivo lleva su línea `FORMATO: X.Y` y sus reglas embebidas.
- [ ] `00_MEMORIA.md` describe el stack y la arquitectura REALES.
- [ ] Las rutas de lectura cubren los tipos de tarea típicos del proyecto.
- [ ] Ninguna etapa está marcada CONSOLIDADO sin confirmación humana.
- [ ] Ninguna decisión fue borrada del historial.
- [ ] La memoria no duplica contenido de `DOCS/` (solo lo referencia).
- [ ] `PROMPT_INICIO.md` funciona solo: un agente nuevo puede arrancar con él.
- [ ] Objetivo actual y plan activo coinciden con la realidad de hoy.

---

## 10. Antipatrones (qué NUNCA hacer)

1. **Auto-consolidar**: marcar CONSOLIDADO sin confirmación del TEAM LEAD.
2. **Borrar decisiones**: se reemplazan, nunca se eliminan.
3. **Guardar la conversación**: la memoria no es un log de chat.
4. **Duplicar documentación**: DOCS/ ya dice el "qué"; la memoria referencia.
5. **Registrar opiniones o propuestas no aprobadas**: solo hechos confirmados.
6. **Leer todo al arrancar**: seguir las rutas de lectura por tipo de tarea.
7. **Dejar la memoria para después**: actualizar es parte de la tarea.
8. **Inventar estado**: ante duda, PENDIENTE y preguntar.

---
---

# PLANTILLAS

Copiar tal cual y reemplazar los `{{PLACEHOLDERS}}`.

---

## PLANTILLA A — `AI/00_MEMORIA.md`

```markdown
# 00_MEMORIA.md

FORMATO: 1.0

## Proyecto

{{NOMBRE_DEL_PROYECTO}}

{{DESCRIPCION_EN_UN_PARRAFO: qué es y qué NO es}}

## Stack

{{LISTA_DEL_STACK_REAL: lenguaje, framework, BD, auth, UI, patrones}}

## Arquitectura

{{FLUJO_DE_CAPAS_Y_REGLAS_INVIOLABLES}}

Nunca romper este flujo.

{{QUIEN_COORDINA}}: solo coordinan.

{{DONDE_VIVEN_LAS_REGLAS}}: contienen las reglas.

{{COMO_SE_ACCede_A_DATOS}}: consultan datos.

## Objetivo actual

{{QUE_SE_ESTA_CONSTRUYENDO_AHORA_Y_QUE_QUEDA_PARA_DESPUES}}

## Plan activo

Estado del ciclo: {{PLANIFICACION|IMPLEMENTACION|VERIFICACION|CONSOLIDACION}}

Registrar aqui unicamente el paso en curso. Actualizar bajo confirmacion del TEAM LEAD.

Siguiente paso: {{PASO_CONCRETO_EN_CURSO}}

Ciclo en curso ({{FECHA}}, orden de prioridad definido por TEAM LEAD):
1) {{PRIORIDAD_1}}
2) {{PRIORIDAD_2}}

## Fuentes de verdad

{{DOCS_O_DOCUMENTACION_EXISTENTE}} contiene la documentacion funcional y de
producto. No se duplica en AI/.

- {{RUTA}} - {{QUE_CONTIENE}}
- {{RUTA}} - {{QUE_CONTIENE}}

## Archivos de esta memoria

Leer en este orden segun la tarea.

- 01_ESTADO.md - estado real por modulo. Solo el TEAM LEAD confirma CONSOLIDADO.
- 02_DECISIONES.md - decisiones tecnicas preservadas (IDs D-XXX).
- 03_PROBLEMAS.md - problemas conocidos (IDs P-XXX) y resueltos.

## Rutas de lectura por tarea

Toda tarea: leer este archivo y 01_ESTADO.md.

- Tarea de implementacion: leer ademas 02_DECISIONES.md y las secciones de
  03_PROBLEMAS.md que afecten al modulo.
- Tarea de arquitectura o tecnologia: leer 02_DECISIONES.md completo.
- Tarea de diagnostico o bug: leer 03_PROBLEMAS.md completo y 01_ESTADO.md
  del modulo.
- Tarea de producto o UX: consultar {{FUENTES_DE_VERDAD_DE_PRODUCTO}}.

(Agregar rutas especificas del proyecto: frontend, datos, deploy, etc.)

## Reglas de la memoria

Solo el TEAM LEAD confirma que una etapa esta terminada, aprobada y funcionando.

Nunca auto-consolidar una etapa.

Una implementacion existente pero no confirmada no es CONSOLIDADO.

Lo que falla se registra en 03_PROBLEMAS.md.

Lo que se decide se registra en 02_DECISIONES.md.

Lo que se confirma se registra en 01_ESTADO.md.

No guardar conversaciones, logs, razonamientos internos ni opiniones temporales.

No duplicar la documentacion de producto.

La memoria permite a cualquier modelo continuar el trabajo sin conocer
conversaciones anteriores.
```

---

## PLANTILLA B — `AI/01_ESTADO.md`

```markdown
FORMATO: 1.0

## Regla de estados

CONSOLIDADO - probado y confirmado explicitamente por el TEAM LEAD como
terminado, aprobado y funcionando.

EN PROGRESO - trabajo activo, aun no terminado ni confirmado.

PENDIENTE - no evaluado.

## Estado actual

- {{MODULO_1}}: {{ESTADO}} ({{FECHA}}, {{QUIEN_CONFIRMO}}).
- {{MODULO_2}}: {{ESTADO}} ({{FECHA}}, {{QUIEN_CONFIRMO}}).
- {{MODULO_N}}: {{ESTADO}}.

(Una linea por modulo o sub-modulo. Detalle suficiente para saber QUE existe
y EN QUE condicion, sin duplicar la documentacion.)
```

---

## PLANTILLA C — `AI/02_DECISIONES.md`

```markdown
# 02_DECISIONES.md

FORMATO: 1.0

## Reglas

Guardar unicamente decisiones tecnicas o de arquitectura que no deben volver
a discutirse.

Una decision puede ser reemplazada por otra. Nunca se borra historial: la
decision vieja se marca como REEMPLAZADA.

Solo registrar decisiones reales tomadas. No opiniones ni propuestas no aprobadas.

Cuando se registra una decision, indicar el numero siguiente libre
(D-001, D-002, etc).

## Indice

| ID | Decision | Fecha | Estado |
|----|----------|-------|--------|
| D-001 | {{RESUMEN_EN_UNA_LINEA}} | {{AAAA-MM-DD}} | VIGENTE |

## Detalle

### D-001

Fecha: {{AAAA-MM-DD}}

Decision: {{QUE_SE_DECIDIO, EXACTO}}

Motivo: {{POR_QUE}}

Consecuencias: {{QUE_IMPLICABA_A_FUTURO, QUE_NO_HACER}}

Reemplaza a: {{NINGUNA | D-XXX (en que parte)}}

Estado: VIGENTE | REEMPLAZADA por D-XXX | CANCELADA ({{MOTIVO}})

Al tomar una decision nueva, agregar una ficha con el ID siguiente y
actualizar el indice.

Al reemplazar una decision, marcar la ficha anterior como REEMPLAZADA por
D-XXX y agregar la nueva.

Registrar bajo confirmacion del TEAM LEAD.
```

---

## PLANTILLA D — `AI/03_PROBLEMAS.md`

```markdown
FORMATO: 1.0

## Reglas

Guardar problemas conocidos que no estan resueltos.

Un problema resuelto se mueve a RESUELTOS con causa y solucion: es patron
a no repetir.

## Problemas

- P-XXX: {{DESCRIPCION_BREVE}}. {{SINTOMA}}. {{CAUSA_SUSPECHADA}}.
  {{AFECTA_A_QUE_MODULOS}}.

## RESUELTOS

- P-XXX: {{DESCRIPCION}}. Causa: {{CAUSA_RAIZ}}. Solucion: {{QUE_SE_HIZ}}.
  Resuelto {{AAAA-MM-DD}}.
```

---

## PLANTILLA E — `AI/PROMPT_INICIO.md`

```markdown
# PROMPT DE INICIO - {{PROYECTO}}

Eres un agente de desarrollo del proyecto {{PROYECTO}},
{{DESCRIPCION_EN_UNA_LINEA_CON_STACK}}.

Root del proyecto: {{RUTA_ABSOLUTA}}

=====================================================
ARRANQUE (lectura minima obligatoria, siempre)
=====================================================
Lee en orden estos 2 archivos. Con ellos tenes contexto actualizado al minimo costo:
1. AI\00_MEMORIA.md  -> indice, stack, arquitectura, plan activo, rutas de lectura
2. AI\01_ESTADO.md   -> estado real por modulo (que esta CONSOLIDADO y que falta)

No leas mas archivos de arranque. Los SESION_*.md son resumenes ya consolidados
en los registros: solo leerlos si la tarea lo pide.

=====================================================
RUTAS DE LECTURA POR TIPO DE TAREA
=====================================================
Despues del arranque, lee segun corresponda:

- MODULO NUEVO / CRUD nuevo:
    AI\02_DECISIONES.md          (reglas de arquitectura vigentes)
    AI\03_PROBLEMAS.md           (patrones a no repetir)
    {{DOCUMENTACION_DE_CONVENCIONES}}
    {{DOCUMENTACION_DE_ARQUITECTURA}}

- FRONTEND / UI:
    {{DESIGN_SYSTEM_O_EQUIVALENTE}}

- MODELO DE DATOS / ENTIDADES / MIGRACIONES:
    {{DER_O_MODELO_DE_DATOS}}
    AI\02_DECISIONES.md          (decisiones que afectan el schema)

- NEGOCIO / REGLAS / PRODUCTO:
    {{BUSINESS_RULES}}
    {{VISION}}

- ROADMAP / PRIORIDADES:
    {{ROADMAP}}

- BUG / DIAGNOSTICO:
    AI\03_PROBLEMAS.md (completo)
    AI\01_ESTADO.md    (modulo afectado)
    AI\02_DECISIONES.md (decisiones del modulo)

- DEPLOY / PRODUCCION:
    {{NOTAS_DE_DEPLOY}}

=====================================================
REGLAS OBLIGATORIAS (aplicar siempre)
=====================================================
1. {{REGLA_INVOLUBLE_1_DE_ARQUITECTURA}}
2. {{REGLA_INVOLUBLE_2_DE_DATOS}}
3. {{REGLA_INVOLUBLE_3_DE_ESTILO}}
...
N. Si hay duda de arquitectura: NO decidir; detenerse, explicar el problema
   y proponer alternativas.
N+1. No auto-consolidar etapas: solo el TEAM LEAD confirma CONSOLIDADO.
     No modificar AI\01_ESTADO.md sin confirmacion.
N+2. Al terminar una tarea: verificar compilacion/tests y actualizar la memoria.
N+3. Commit solo bajo pedido explicito del TEAM LEAD.
```

---

## PLANTILLA F — `AI/SESION_YYYYMMDD_TEMA.md`

```markdown
# SESION {{AAAA-MM-DD}}: {{TEMA_EN_MAYUSCULAS}}

## Objetivo
1. {{QUE_SE_PROPUSO_HACER}}

## Estado
- {{ENTREGABLE_1}}: {{ESTADO}} ({{aprobado por TEAM LEAD | pendiente de confirmacion}}, {{FECHA}}).

## Cambios

### {{AREA_1}}
- {{ARCHIVO_RUTA}}: {{QUE_SE_MODIFICO_Y_POR_QUE}}.
- {{ARCHIVO_NUEVO}} (nuevo): {{FUNCION}}.

## Decisiones nuevas
- {{D-XXX}}: {{RESUMEN}}.

## Reglas de negocio aplicadas
- {{D-XXX}}: {{COMO_AFECTO_LA_IMPLEMENTACION}}.

## Pendientes
1. {{QUEDA_PENDIENTE}}
2. {{CONFIRMACIONES_ESPERADAS_DEL_TEAM_LEAD}}
```

---

*Fin del manual. Este documento es genérico: copiarlo a la raíz de cualquier
proyecto y entregarlo al agente junto con el modo de uso (sección 1).*
