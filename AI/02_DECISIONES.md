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
| D-013 | `Producto.RubroId` es nullable: un producto puede no tener rubro | 2026-10-05 | VIGENTE |
| D-014 | Matriz de roles: `AdminGlobal` es back-office, no entra a pantallas de tenant | 2026-10-05 | VIGENTE |
| D-015 | Tests de humo contra SQL Server real, no en memoria | 2026-10-05 | VIGENTE |

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

Consecuencias: estas 7 columnas las crea la migracion
`20261002143249_AddSuscripcionColumnsToTenant` desde 2026-10-02 (antes era el
patch T-SQL en `Program.cs` y P-002). Al agregar o cambiar una de estas columnas
hay que agregar la migracion en el mismo commit y correr `SchemaCheck` (§5 de
`00_MEMORIA.md`).

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

### D-013

Fecha: 2026-10-05

Decision: **`Producto.RubroId` es nullable.** Un producto puede existir sin rubro.
El modelo lo declara asi (`public int? RubroId`, `public Rubro? Rubro`, sin
`[Required]`) y la columna es `int NULL` con `ON DELETE SET NULL`.

El borrado de un rubro sigue siendo **logico**: `RubroController` no borra en
hard, solo hace `Activo = false` (`DeleteConfirmed` y `DesactivarAjax`). No se
agrego el mensaje de "tiene N productos" porque no hace falta: nunca se borra la
fila.

Motivo: era lo que el codigo ya hacia. `Views/Productos/Edit.cshtml` ofrecia
"-- Sin rubro --", todas las vistas y controllers desreferencian con `?.`
(`producto.Rubro?.Nombre ?? "-"`), `AnalisisComercialController` agrupa con
`v.Producto.Rubro != null ? ... : "Sin Rubro"`, y la migracion
`20260321231347_FixRubroIdNullable` ya habia hecho la columna nullable. Lo unico
que decia lo contrario era el `[Required]` del modelo, que ademas rompia el DDL.

Lo que se corrige como efecto lateral: `Edit.cshtml` ofrecia "-- Sin rubro --" y
el `[Required]` lo rechazaba, asi que **no se podia guardar un producto sin
rubro**. Bug de validacion, no de esquema.

Consecuencias:

- Un producto sin rubro se muestra como `"-"` (listado), `"Sin Clasificar"`
  (detalle) y `"Sin Rubro"` (análisis comercial). **No cambiar esos tres textos
  sin updatear los tres lugares.**
- La navegacion `Rubro` es nullable: nunca desreferenciarla sin `?.` ni chequeo.
- El FK `Productos -> Rubros` queda `SetNull` sobre columna nullable, que es
  coherente. No volver a poner `[Required]` ni `public Rubro Rubro` (no-nullable):
  eso reproduce P-030, porque `SetNull` sobre `NOT NULL` es un DDL que SQL Server
  no puede crear.
- Como toda FK `SetNull` del modelo, sigue aplica el aviso de EF: un filtro
  global combinado con una relacion obligatoria puede hacer desaparecer datos
  segun como se navegue (ver el refuerzo a P-006/P-007).

Reemplaza a: nada (D-013 estaba PENDIENTE; se eligio la opcion (c) de las tres
framed en `01_ESTADO.md` §6.4).

Estado: VIGENTE

---

Al tomar una decision nueva, agregar una ficha con el ID siguiente y
actualizar el indice.

Registrar bajo confirmacion del TEAM LEAD.
### D-014

Fecha: 2026-10-05

Decision: **matriz de roles explicita. `AdminGlobal` es back-office y no entra
a ninguna pantalla que pertenece a un tenant.** Los tres roles quedan asi:

| Rol | Puede |
|---|---|
| `AdminGlobal` | `Tenants`, `AdminGlobalUsuarios`, y **solo** consultar el catalogo global (`BuscarProductoBase`, `GetProductoBase`). Es el unico rol sin `TenantId`. |
| `Admin` | Todo lo del tenant: productos, proveedores, clientes, rubros, usuarios, configuracion, dashboard, analisis comercial, y tambien el POS y la caja. |
| `Caja` | Operar el POS (crear venta, listado), abrir/cerrar caja, egresos, dashboard financiero, alertas, notificaciones, buscar en el catalogo, y **anular ventas**. |

Lo que **no** cambio, por decision explicita: **el rol `Caja` sigue pudiendo
anular ventas**, como estaba (`VentaAdminController` con
`Roles = "Admin,Caja"`). Se.Considero y se dejo asi.

Motivo: el codigo ya decia cual era la intencion, pero en dos lugares
contradictorios y sin nada que lo respaldara en el servidor.

- `Views/Home/Index.cshtml:684` excluye explicitamente a `AdminGlobal` del POS
  (`IsInRole("Caja") || IsInRole("Admin")`), igual que de caja y alertas.
- `_Layout.cshtml` le mostraba a `AdminGlobal` POS, Egresos y Finanzas.
- Y como los controllers solo tenian `[Authorize]` pelado, `AdminGlobal` no
 llegaba a nada: entraba al POS y devolvía **500** por `GetTenantId().Value`.

O sea: la intencion estaba escrita en las vistas, era contradictoria, y no
estaba respaldada por nada del lado del servidor. La decision toma la version
que el home ya aplicaba y la hace cumplir en el servidor.

Consecuencias:

- Si en el futuro `AdminGlobal` **necesita** operar el POS de un tenant, no
  alcanza con agregar el rol: `AdminGlobal` no tiene `TenantId` por diseno, asi
  que habria que construir un mecanismo de "operar en nombre de un tenant"
  (selector de tenant + tenant activo en la sesion). Es un feature, no un
  `[Authorize]`.
- `AdminGlobal` conserva acceso a `ProductosController` **solo** por las dos
  acciones del catalogo global. Las otras 20 requieren tenant.
- **Los `[Authorize]` de clase y de metodo se combinan con AND, no con OR.**
  Poner `Admin,Caja` en la clase y `AdminGlobal,Admin` en el metodo deja solo
  `Admin`: es la interseccion. Por eso `ProductosController` lleva los tres
  roles en la clase y cada accion de tenant lleva `Roles = "Admin"`.

### D-015

Los tests de humo de P-017 corren contra **SQL Server real**, no contra InMemory ni
SQLite. Cada test crea su propia base descartable `MiniMercadoDB_test_<guid>`, la construye
solo con `Database.Migrate()` y la dropea al terminar.

Por que:

- El modelo usa **19 `FromSqlRaw` con T-SQL**. En InMemory no hay T-SQL; en SQLite la
  sintaxis no es la misma y los `decimal(18,2)` se comportan distinto. Un test que pasa en
  SQLite puede fallar en SQL Server y al reves, que es justo la clase de bug (redondeo de
  plata) que mas duele.
- `Database.Migrate()` es el mismo camino que usa la app. `EnsureCreated()` no: ademas de no
  servir para comparar el modelo contra las migraciones (ver `tools/verificar-esquema.sh`),
  en EF Core 6 aplica las migraciones que el contexto tenga, asi que ocultaria justo la deriva
  que P-025 verifica.
- El servidor de desarrollo ya existe (contenedor `minimercado-sql`), asi que la dependencia
  externa es practicamente nula. En CI habra que agregar un service container, pero hace
  falta igual para `SchemaCheck`.

Como se conecta: por variable de entorno `ConnectionStrings__MiniMercadoDB` —nunca por
`appsettings.json` (P-010)— y solo se usa el **servidor** de ese connection string; el nombre
de la base se reemplaza por el descartable. Asi el proyecto no tiene ninguna credencial.

Consecuencias:

- Los tests **no corren sin SQL Server**. Es un requisito consciente, no un descuido: la
  alternativa (SQLite) solo serviria para el flujo de cobro de suscripcion, que es el unico
  de los tres que no usa T-SQL.
- El isolation es por base, no por transaccion. Cuesta ~5 s por test (migrar 60 migraciones).
  Si el suite crece y molesta, el siguiente paso es migrar una vez por corrida y envolver cada
  test en una transaccion que se revierte.
- Los controllers se instancian a mano con un `ClaimsPrincipal` y un `UrlHelper`, sin
  `WebApplicationFactory`. Alcanza para la logica de negocio; si hace falta probar
  middleware, antiforgery o rendering de vistas, ahi si va `WebApplicationFactory`.

---

## D-016 — La aritmética de negocio vive en el modelo, y hay tests que leen el código fuente

**Contexto:** P-032 (formula del saldo de caja triplicada en el controller y dos vistas) se
registró como "no corregible con tests" porque el arreglo parecía pedir renderizar las vistas.
No era así: el problema real no era que las vistas calcularan, sino que calcularan *por su
cuenta*.

**Decisión:**

- La aritmética que define un concepto de negocio va en el modelo, como método con parámetros
  explícitos para lo que vive en otra tabla: `Caja.CalcularSaldoEsperado(totalEgresos)`. El
  controller y las vistas la llaman. Ninguna vista vuelve a escribir la operación.
- Cuando el riesgo es *re-duplicación* (y no un valor mal calculado), un test de comportamiento
  no sirve: el controller calcula bien y el bug está en la copia vieja de la vista. Por eso hay
  además un guardia que lee el `.cshtml` y falla si la fórmula reaparece escrita a mano.

**Cómo se verificó que sirve:** se revirtió el arreglo y el guardia se puso rojo. Un test que
nunca se ha visto fallar no es un test, es decoración.

**Consecuencias:**

- Los guardias de fuente necesitan las fuentes en disco. En vez de buscar rutas en runtime (el
  proyecto web es hermano de la carpeta de tests, no ancestro, así que subir directorios no
  llega), `MiniMercado.Tests.csproj` las copia a la salida con `CopyToOutputDirectory` y el
  test lee `AppContext.BaseDirectory`. El guardia se rompe de forma ruidosa si alguien mueve
  la vista.
- Estos tests miran *cómo* está escrito el código, no *qué* hace. Es un trade-off explícito:
  se acepta porque la alternativa era dejar la duplicación sin ninguna red, y porque fallan
  con un mensaje que dice exactamente qué hacer.

---

## D-017 — Sin IVA y sin descuentos, por ahora (cierra P-034)

**Decisión del usuario (2026-10-05):** no se suman IVA ni descuentos al alcance.

**Qué implica en el código actual: nada.** No hay que tocar una línea. `Producto.Precio` es el
precio final de venta y el total de la venta es `Precio × Cantidad`; el modelo no tiene campo
de IVA, de impuesto ni de descuento, ni en `Venta`, ni en `VentaDetalle`, ni en `Producto`. Es
decir: el comportamiento que se pidió ya es el que hay.

**Por qué queda escrito igual:** P-034 estaba registrado como "pendiente, necesita definición
de negocio". Sin esta decisión quedaba abierto para siempre. Ahora es "descartado a
deliberado", que es una categoría distinta: si alguien lo reopen, ya sabe que fue una decisión
y no un olvido.

**Qué costaría si algún día se agrega** (para que no se pierda el análisis):

- Es un cambio de modelo, no un ajuste de fórmula: columnas nuevas en `Venta`, `VentaDetalle` y
  `Producto`, más migración.
- El precio deja de ser un número y pasa a ser dos (base imponible + impuesto), o tres con el
  descuento. Todos los lugares que hoy suman `Precio` hay que revisarlos, no solo el total.
- `CierreCaja` y el dashboard financiero comparan `Total` contra `MontoReal`; si el total pasa a
  incluir impuesto, el arqueo tiene que distinguir la plata que entra de la que incluye IVA, o el
  cierre deja de cuadrar. **Es el punto más delicate**, y hay que pensarlo antes de tocar nada.
- El precio que muestra el producto al cliente y el que se guarda en el detalle dejan de ser el
  mismo número.
