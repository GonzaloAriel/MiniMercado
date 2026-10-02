# 02_DECISIONES.md

FORMATO: 1.0

## Reglas

Guardar unicamente decisiones tecnicas o de arquitectura que no deben volver
a discutirse.

Una decision puede ser reemplazada por otra. Nunca se borra historial: la
decision vieja se marca como REEMPLAZADA.

Solo registrar decisiones reales tomadas. No opiniones ni propuestas no
aprobadas.

Cuando se registra una decision, indicar el numero siguiente libre
(D-001, D-002, etc).

## Indice

| ID | Decision | Fecha | Estado |
|----|----------|-------|--------|
| D-001 | Multi-tenancy por discriminador `TenantId` con query filters globales de EF Core | 2025 | VIGENTE |
| D-002 | El stock se deriva de `LoteProducto`, nunca se edita `Producto.Stock` | 2025 | VIGENTE |
| D-003 | Los totales financieros se calculan con `SumAsync`, no se persisten | 2026 | VIGENTE |
| D-004 | Catálogo global normalizado en `ProductoBase`, separado del `Producto` del tenant | 2026 | VIGENTE |
| D-005 | Hosting fuera de Azure: la app se prepara para Linux/Docker | 2026-10-01 | VIGENTE |
| D-006 | El modelo de `Tenant` incluye estado y vencimiento de suscripción | 2026 | VIGENTE |
| D-007 | UI con tema oscuro, tarjetas y Bootstrap Icons | 2026 | VIGENTE |
| D-008 | Motor de base de datos destino | — | **PENDIENTE DEL TEAM LEAD** |
| D-009 | Framework destino (subir de .NET 6 EOL) | — | **PENDIENTE DEL TEAM LEAD** |
| D-010 | Dónde vive la app (VPS / solo local / nube) | — | **PENDIENTE DEL TEAM LEAD** |
| D-011 | Tratamiento del historial git que expone contraseñas | — | **PENDIENTE DEL TEAM LEAD** |
| D-012 | Alcance del refactor de arquitectura (mover lógica a services) | — | **PENDIENTE DEL TEAM LEAD** |

D-008 a D-012 están bloqueadas y framed en `01_ESTADO.md` §6.4. No registrar
ninguna ficha hasta que el TEAM LEAD elija.

## Detalle

### D-001

Fecha: 2025

Decision: Base de datos única con discriminador `TenantId`. Todas las entidades que
implementan `ITenantEntity` llevan un `HasQueryFilter` en `MiniMercadoContext.cs`
del tipo `e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId`. El
`TenantId` se inyecta en el usuario autenticado por `CustomClaimsPrincipalFactory`.

Motivo: aislamiento automatico de datos sin repetir filtros en cada consulta.

Consecuencias: agregar una entidad `ITenantEntity` nueva **obliga** a agregar su
filtro en la misma línea. Omitirlo es una fuga entre tenants (ya ocurrio:
`ImpresionEtiqueta`). `AdminGlobal` no tiene `TenantId`, y por eso sin claim los
filtros se desactivan y ve todas las tablas.

Reemplaza a: ninguna

Estado: VIGENTE

### D-002

Fecha: 2025

Decision: La fuente de verdad del stock son los `LoteProducto` (con costo,
vencimiento y origen). `Producto.Stock` es una suma dinamica mantenida por
`ProductoService.SincronizarStock`.

Motivo: trazabilidad de lotes, vencimientos y costo real.

Consecuencias: **nunca** editar `Producto.Stock` directamente, ni en un
controller ni en un importador. El importador Excel suma stock creando lotes.

Reemplaza a: ninguna

Estado: VIGENTE

### D-003

Fecha: 2026

Decision: Ningun total financiero calculado se guarda en base de datos. El
dashboard financiero, el arqueo de caja y los resultados de mes se calculan con
`SumAsync` sobre `Ventas` activas y `EgresosCaja` no anulados.

Motivo: los datos siempre estan actualizados y no hay que reconciliar un campo
derivado.

Consecuencias: las consultas de arqueo deben filtrar `!e.Anulado` y las ventas
activas. El filtro global de `EgresoCaja` ya incluye `!e.Anulado`.

Reemplaza a: ninguna

Estado: VIGENTE

### D-004

Fecha: 2026

Decision: `ProductoBase` es un catalogo global sin `TenantId`, y `Producto`
(opcionalmente vinculado) es el inventario local del comercio. La importacion
masiva usa esta normalizacion para evitar duplicados entre tenants.

Motivo: estandarizar el catalogo sin duplicar el catalogo global por comercio.

Consecuencias: `ProductoBase` **no** implementa `ITenantEntity` a proposito.
Campos stock/precios/vencimiento viven en `Producto`, no en `ProductoBase`.

Reemplaza a: ninguna

Estado: VIGENTE

### D-005

Fecha: 2026-10-01

Decision: MiniMercado deja de depender de Azure App Service (Windows, plan B1) y
Azure SQL Database. No hay hosting hoy y el proyecto se prepara para correr en
Linux con la base contenizada.

Motivo: el proyecto no se actualiza desde 2026-07-11, el hosting Azure ya no esta,
y el objetivo declarado es un stack Docker replicando el patron de LiteState.

Consecuencias: el parche T-SQL de `Program.cs` y los 19 `FromSqlRaw` con T-SQL son
deuda que bloquea portabilidad. La `connection string` con `Trusted_Connection=True`
no funciona fuera de Windows. `Dockerfile`, `compose.yaml`, `.env` y `global.json`
quedan a crear.

Reemplaza a: el despliegue en Azure App Service descrito en la documentacion vieja.

Estado: VIGENTE

### D-006

Fecha: 2026

Decision: `Tenant` modela el estado de suscripcion como parte de la entidad:
`Plan`, `FechaInicioSuscripcion`, `FechaProximoPago`, `EstadoSuscripcion`,
`MesesPagados`, `SaldoAFavorMeses`, `UltimoPagoRegistrado`.

Motivo: bloqueo por vencimiento controlado por codigo antes de cada request.

Consecuencias: estas 7 columnas existen en el modelo pero **no estan en ninguna
migracion** (ver P-002). Hay que agregar la migracion antes de tocar otra cosa de
este modulo.

Reemplaza a: ninguna

Estado: VIGENTE

### D-007

Fecha: 2026

Decision: la UI se construye con tema oscuro nativo, tarjetas oscuras, botones con
iconos de Bootstrap Icons y respuestas rapidas.

Motivo: consistencia visual de todas las vistas.

Consecuencias: las vistas nuevas deben seguir el sistema dark ya definido, ver
`Contexto Minimercado/Prompt de diseño dark.txt`.

Reemplaza a: ninguna

Estado: VIGENTE

---

Al tomar una decision nueva, agregar una ficha con el ID siguiente y
actualizar el indice.

Al reemplazar una decision, marcar la ficha anterior como REEMPLAZADA por
D-XXX y agregar la nueva.

Registrar bajo confirmacion del TEAM LEAD.