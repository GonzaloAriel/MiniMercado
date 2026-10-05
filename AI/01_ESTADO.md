FORMATO: 1.0
FECHA: 2026-10-05
PROPOSITO: Estado real del sistema + camino hacia el stack Docker

---

## 0. Cómo leer este documento

Este archivo describe **lo que el código hace hoy**, no lo que debería hacer. Cuando algo
dice PENDIENTE, es que no existe.

Regla de estados (misma que LiteState):

| Estado | Significado |
|---|---|
| CONSOLIDADO | Probado y confirmado por el TEAM LEAD |
| EN PROGRESO | Existe, funciona parcial, incompleto |
| PENDIENTE | No existe o no evaluado |
| ROTO | Existe pero tiene un defecto confirmado |
| **ALTA** | ⚠️ Defecto que puede causar pérdida de datos o acceso indebido |

Este documento **no registra ninguna decisión tomada**. Las decisiones se anotan en
`02_DECISIONES.md` cuando las confirma el TEAM LEAD.

> **Memoria completa en `AI/`:** `00_MEMORIA.md` (índice), `01_ESTADO.md` (este),
> `02_DECISIONES.md` (D-XXX), `03_PROBLEMAS.md` (P-XXX), `PROMPT_INICIO.md`
> (arranque). Los casos de §5 de este documento están registrados como P-001 a
> P-021 en `03_PROBLEMAS.md`.

---

## 1. Identidad del proyecto

**MiniMercado** es un ERP/POS SaaS multi-tenant para comercios minoristas. Varios comercios
comparten infraestructura y base de datos, con aislamiento por `TenantId`.

- **Repositorio:** https://github.com/GonzaloAriel/MiniMercado.git (**público**)
- **Rama:** `main`, sincronizada con `origin/main`
- **Último commit:** `9e8fcf0` — 2026-07-11 — "Ajustes de vistas y textos en Analisis
  comercial y Edicion de productos"
- **Working tree:** **con cambios sin commitear** (a 2026-10-05)
- **Commits sin pushear:** ninguno

> ⚠️ **Desde 2026-10-05 el working tree tiene trabajo sin commitear**, cerrado y verificado
> pero sin commit: P-025 (paso 2), P-026, P-030, P-031, **P-006 (autorización, D-014)** y
> **P-017 (tests de humo)**, **P-033 (atomicidad de venta)** y **P-032 (formula de
> caja)**. Son **26 entradas** de `git status` (22 modificadas / borradas, 4 sin trackear: las
> 2 de la migración P-030, `tools/verificar-esquema.sh` y el proyecto `tests/`). Es el primer
> rato que el árbol no está limpio, y el motivo es que esos cambios se hicieron y probaron
> sin commitear.

> ⚠️ **Los encabezados de `03_PROBLEMAS.md` no son confiables al 100%.** Varios problemas se
> resolvieron a lo largo del trabajo y se documento el "qué se hizo" en su propia sección, pero
> el encabezado del listado quedó diciendo `P-0XX (ALTA)` sin marcar. Al retomar, **verificar el
> estado real de cada problema antes de trabajar sobre él**, no confiar en el encabezado. Casos
> conocidos: P-006, P-021, P-022, P-025 (PARCIAL: pasos 1 y 2 hechos, falta CI y paso 3), P-026.
> No se corrigió de forma masiva a propósito: los marcadores de los cuerpos son ambiguos y
> actualizarlos a ciegas sería peor que dejarlos señalizados.

### 1.1 Contexto de infraestructura (cambió)

| | Antes | Ahora |
|---|---|---|
| Hosting | Azure App Service (Windows, plan B1) | **Nada. No hay hosting.** |
| Base de datos | Azure SQL Database | **Desconocido. Hay que definirlo.** |
| Observabilidad | Application Insights | **No existe en el código** |
| Sistema operativo | Windows | **Nunca corrió en Linux** |

> ⚠️ **No hay ningún artefacto de despliegue en el repositorio.** Cero `Dockerfile`, cero
> `compose.yaml`, cero `*.pubxml`, cero IaC. Las referencias a Azure en `.gitignore` son
> plantilla estándar de Visual Studio, no configuración real.

---

## 2. Stack tecnológico (real)

| Capa | Tecnología | Estado |
|---|---|---|
| Framework | ASP.NET Core **6.0** (MVC + Razor Pages) | ⚠️ **EOL** (soporte terminó 11/2024) |
| Lenguaje | C# / .NET 6, `Nullable` y `ImplicitUsings` activos | EN PROGRESO |
| ORM | EF Core **6.0.36** | EN PROGRESO |
| Base de datos | SQL Server (proveedor `EFCore.SqlServer` 6.0.36) | Ver §4 |
| Autenticación | ASP.NET Core Identity + `ApplicationUser` | EN PROGRESO |
| UI | Bootstrap 5 + jQuery, tema oscuro propio | EN PROGRESO |
| Contenedores | **Ninguno** | PENDIENTE |

**Paquetes notables:**

- `ClosedXML 0.105.0` — importador Excel
- `Microsoft.EntityFrameworkCore.Sqlite 6.0.35` — **instalado y no usado** (la app usa SQL
  Server). Se puede desinstalar.
- `dotnet-tools.json` pide **`dotnet-ef 6.0.36`** (`rollForward: false`). **Consolidado
  2026-10-02**; antes pedía `10.0.5`, incompatible con EF Core 6, y `dotnet ef` fallaba
  siempre. Ver §4.2.

---

## 3. Estado por módulo

### 3.1 Núcleo del negocio — EN PROGRESO

| Módulo | Estado | Nota |
|---|---|---|
| Tenant (altas/bajas) | EN PROGRESO | `TenantsController`, solo `AdminGlobal` |
| Suscripción | EN PROGRESO | `SuscripcionService`; columnas NO están en migraciones (§4.2) |
| Productos / stock | EN PROGRESO | Lotes como fuente de verdad; `ProductoService.SincronizarStock` |
| Importador Excel | EN PROGRESO | `ProductoImportService`, ClosedXML |
| Ventas (POS) | EN PROGRESO | Lógica de venta **dentro del controller**, no en services |
| Caja / turnos | EN PROGRESO | `CajaController`, sin roles → `AdminGlobal` entra |
| Egresos | EN PROGRESO | Módulo nuevo 2026, `EgresosController` |
| Clientes / fiados | EN PROGRESO | `ClienteController` |
| Proveedores | EN PROGRESO | `ProveedorService` — uno de los pocos bien separados |
| Etiquetas | EN PROGRESO | `EtiquetasController`, `ImpresionEtiqueta` |
| Analisis comercial | EN PROGRESO | Solo rol `Admin` |
| Notificaciones / alertas | EN PROGRESO | Se disparan **de forma oportunista en cada request** |
| Auditoría | EN PROGRESO | `AuditoriaService` síncrono, sin filtro (§5.1) |
| Centro de ayuda (FAQ) | EN PROGRESO | `AyudaController` |

### 3.2 Infraestructura — PENDIENTE

| Ítem | Estado |
|---|---|
| Dockerfile | **No existe** |
| `compose.yaml` / orquestador | **No existe** |
| Variables de entorno / `.env` | **No existe** |
| `appsettings.Production.json` | **No existe** |
| Logging estructurado | **No existe** (`ILogger` no está inyectado en ningún archivo) |
| Application Insights / OpenTelemetry | **No existe** (la documentación lo daba por activo; el código no lo tiene) |
| `BackgroundService` / `IHostedService` | **No existe** — todo corre por request |
| Tests automatizados | **No existe** ningún proyecto de tests |
| `global.json` (pin de SDK) | **No existe** |
| CI/CD | **No existe** |
| Verificación modelo↔migraciones | **Existe** — `tools/SchemaCheck` + `tools/verificar-esquema.sh`. Herramientas de desarrollo (fuera de la app y de la imagen Docker), **se corren a mano**, no hay workflow que las dispare (P-025) |

---

## 4. Base de datos

### 4.1 Conexión — RESUELTA para desarrollo local

**Estado 2026-10-02.** Antes `appsettings.json` traía la connection string en texto plano apuntando
a `.\SQLEXPRESS` con `Trusted_Connection=True` (autenticación Windows), y el archivo además era
**JSON inválido** (una línea `//Server=...` suelta fuera del objeto que el provider ignoraba en
silencio).

**Ahora:** no hay ninguna connection string en el repositorio. Se pasa por variable de entorno:

```
ConnectionStrings__MiniMercadoDB='Server=127.0.0.1,1433;Database=MiniMercadoDB;User Id=sa;Password=<...>;TrustServerCertificate=True;'
```

`appsettings.json` quedó como JSON válido. Ninguna credencial vive en un archivo versionado.

**Base local de desarrollo (fuera del repo):** contenedor Docker `minimercado-sql`
(`mcr.microsoft.com/mssql/server:2022-latest`) con el puerto atado a `127.0.0.1:1433`, base
`MiniMercadoDB` creada y las **60** migraciones aplicadas. Es **descartable**: no hay datos que
valgan. El detalle de cómo levantarlo está en `00_MEMORIA.md` §"Entorno local".

**Lo que sigue sin decidir:** dónde vive la base en el entorno definitivo (D-008 / D-010).

### 4.2 Migraciones

- **60 migraciones** + snapshot. Última: `20261005101937_RubroIdOpcional`.
- `Program.cs` **ya no** aplica migraciones en cada arranque a ciegas. **Consolidado
  2026-10-05 (P-026).** Ahora `Migrate()` y el seed están detrás de las banderas
  `Bootstrap:ApplyMigrations` y `Bootstrap:Seed`, **activas por defecto solo en
  Development** e inactivas en cualquier otro entorno. En un entorno no-Development sin
  banderas la app **arranca sin migrar** y deja en el log cuántas migraciones faltan y
  cuáles son las 3 primeras. El detalle está en `00_MEMORIA.md` §4.

**✅ Divergencia RESUELTA 2026-10-02 (P-002 + P-003).** Las 7 columnas de `Tenant`
(`Plan`, `FechaInicioSuscripcion`, `FechaProximoPago`, `EstadoSuscripcion`, `MesesPagados`,
`SaldoAFavorMeses`, `UltimoPagoRegistrado`) ahora las crea la migración
`20261002143249_AddSuscripcionColumnsToTenant`, y **el parche T-SQL de `Program.cs` se
eliminó por completo**. Verificado borrando la base y dejándola reconstruir solo con
`Database.Migrate()`: 58 migraciones, 28 tablas, las 7 columnas presentes, app arrancando
sin un solo error.

> **Cómo se hizo:** el snapshot de EF ya contenía esas 7 columnas sin que existiera migración
> alguna. Se quitaron **solo** del snapshot, se generó la migración y EF las reescribió ahí.
> El snapshot es la foto del modelo, no el historial: si cambia el modelo sin correr
> `migrations add`, el snapshot miente y la app se rompe en cualquier base nueva.

> **Segunda divergencia encontrada de paso:** el modelo declara navegación `Tenant` en cascada
> en `ImpresionEtiqueta`, pero su migración original solo creó el FK a `Productos`. Faltaban
> `IX_ImpresionesEtiquetas_TenantId` y `FK_ImpresionesEtiquetas_Tenants_TenantId`. También
> quedaron resueltos en la misma migración (P-023).

> **Al migrar una base que ya tenía las columnas** (porque corrió el parche viejo), la
> migración falla por columna duplicada: recrear la base o dropear esas columnas primero.

> **Corrección 2026-10-02.** Este documento afirmaba que el parche "no es portable a Linux, a
> PostgreSQL, ni a SQL Server en Linux". **Es falso para Linux:** el T-SQL es específico de
> *SQL Server*, no de Windows, y se ejecutó sin cambios contra SQL Server 2022 en un contenedor
> Linux. El bloqueo real es **PostgreSQL**.

**Tabla huérfana:** la migración `20260315212409_AgregaMLToken` crea `MercadoLibreTokens`,
pero no hay entidad ni DbSet → resto de una integración de MercadoLibre abandonada.

### 4.3 Verificación de que el modelo y las migraciones no divergen

**CONSOLIDADO 2026-10-05 (P-025).** Es la pieza que hoy impide que un cambio de esquema
llegue a la base sin su migración. Se corre a mano:

```
ConnectionStrings__MiniMercadoDB='...' tools/verificar-esquema.sh
```

Tres chequeos, y cada uno cubre un agujero distinto:

| | Chequeo | Detecta | Necesita base |
|---|---|---|---|
| A | `tools/SchemaCheck` compara una base creada con migraciones contra una creada desde el modelo | Deriva real (columnas, índices, FKs) | sí |
| B | Genera una migración de prueba y mira si tiene contenido | "Cambié el modelo y no generé migración" | no |
| C | `git status` sobre `Migrations/` | "Generé la migración y olvidé el `git add`" | no |

**Estado actual: sale con exit 0** (o sea, modelo y migraciones coinciden). A y B se
complementan: A no ve el error humano si el snapshot ya tiene el cambio, y B no ve la
deriva real si el snapshot va adelantado (genera un `Up()` vacío y pasa). Por eso están
los dos.

**Lo que falta:** que corra **solo**. El repo no tiene ninguna CI (ni `.github/`, ni GitLab,
ni Azure Pipelines) y elegir plataforma es una decisión de infraestructura pendiente. El
script es **agnóstico a propósito** para que ese día sea solo pegarlo en un workflow. El
paso 3 de P-025 (regla de equipo: un cambio de esquema nunca se pushea sin su migración en
el mismo commit) sigue siendo **solo una regla escrita**.

**Un aviso sobre el estado actual:** el script hoy sale con **exit 4** en este working tree,
porque la migración de P-030 todavía **no está commiteada**. No es un defecto del esquema.

---

## 5. Casos urgentes a corregir

Ordenados por gravedad.

> **CONSOLIDADO 2026-10-02:** los tres casos URGENTE (1, 2 y 3) están
> **CONSOLIDADOS**: corregidos **y verificados en ejecución** contra SQL Server
> 2022. Ver el detalle de la verificación al final de cada caso. Los casos
> ALTA/MEDIA (4 a 11) siguen open.

> **Nota 2026-10-05 (P-025).** Este bloque §5 grew antes que `03_PROBLEMAS.md` y quedó
> desfasado respecto de ahí. Desde 2026-10-05 `03_PROBLEMAS.md` es la lista vigente de
> problemas abiertos (P-025 a P-031), y §5 se conserva como registro histórico de la
> auditoría inicial. En concreto: el caso **MEDIA 10 (sin tests)** sigue abierto y es el
> siguiente prioritized (P-016), pero **el riesgo que este bloque describe —"un bug que el
> flujo local no puede detectar"— ya no es cierto**: la deriva de esquema hoy la atrapa
> `tools/verificar-esquema.sh` (§4.3). Lo que sigue sin red son los **flujos de dinero**
> (venta, cobro, cierre de caja), que ningún chequeo automático cubre.

### ✅ CONSOLIDADO — Contraseñas de administrador en el repositorio público

`Data/SeedData.cs` líneas 56, 76 y 96 (ya corregido):

| Email | Rol | Contraseña (legacy, ya eliminada del código) | TenantId |
|---|---|---|---|
| `global@admin.com` | **AdminGlobal** | `(ver historial git a5d20ba — no se repite acá)` | `null` (ve todo) |
| `admin@demo.com` | Admin | `(idem)` | tenant demo |
| `caja@demo.com` | Caja | `(idem)` | tenant demo |

Tres agravantes:

1. **El repo es público.** Las contraseñas están en el historial, accesible para cualquiera.
2. **El seed corre en cada arranque**, no solo la primera vez. Cualquier base contra la que
   se levante la app recibe esos usuarios con esas contraseñas.
3. `AdminGlobal` **no tiene `TenantId`** → los query filters se desactivan → **ve las tablas
   completas de todos los tenants**.

**Qué hay que hacer:** sacar las contraseñas del fuente (variable de entorno o generar una
aleatoria solo en Development) **y** rotarlas. Rotar después de sacarlas no sirve si el historial
público sigue teniendo las viejas.

**✅ CONSOLIDADO 2026-10-02.** Las tres contraseñas ya **no están en el fuente**: `SeedData` las
lee de `IConfiguration` con las claves `Seed:PasswordAdminGlobal`, `Seed:PasswordAdminDemo` y
`Seed:PasswordCajaDemo`. Si no están definidas, el usuario **no se crea** y se emite un
`LogWarning` con el nombre de la clave. No hay default en ningún `appsettings*.json`
(`appsettings.Development.json` está versionado, por eso no sirve como escondite).
**Verificado en ejecución:** con las tres variables exportadas el seed creó los tres usuarios con
sus roles correctos y el login por formulario funciona.
**Lo que queda abierto (no bloquea):** rotar las claves de bases ya creadas con las viejas y la
decisión **D-011** sobre el historial de git (commit `a5d20ba`).

### ✅ CONSOLIDADO — Fuga de datos entre tenants: `ImpresionEtiqueta` sin filtro global

`ImpresionEtiqueta` **implementa `ITenantEntity`** (tiene `TenantId`), pero **no tiene
`HasQueryFilter`** en `MiniMercadoContext.cs`. Hay 15 filtros manuales y este falta.

Impacto confirmado en `EtiquetasController.Reimprimir` (línea ~128):

```csharp
var impresion = _context.ImpresionesEtiquetas
    .Include(i => i.Producto)
    .FirstOrDefault(i => i.Id == id);     // ← sin filtro de tenant
```

Un usuario autenticado que conozca o adivine un `Id` puede reimprimir la etiqueta de **otro
tenant**. Las otras dos acciones del mismo controller (`Imprimir`, `Historial`) sí filtran
por `tenantId` manualmente: el patrón correcto ya existe, solo no se aplicó en todas.

**Qué hay que hacer:** agregar el `HasQueryFilter` + verificar el alcance de datos ya
filtrados en producción.

**✅ CONSOLIDADO 2026-10-02.** Agregado el `HasQueryFilter` de `ImpresionEtiqueta` en
`MiniMercadoContext.cs` (era el único `ITenantEntity` sin filtro, de 16) y `Reimprimir` ahora
sigue el patrón correcto del resto del controller (`if (!tenantId.HasValue) return Unauthorized()`
+ `i.TenantId == tenantId`). Un query filter no cambia el modelo, así que **no requirió
migración**.
**Verificado en ejecución:** se creó un segundo tenant con una `ImpresionesEtiquetas` propia y
`Reimprimir` desde la sesión del Admin del tenant 1 devuelve **404**, no la etiqueta ajena.

### ✅ CONSOLIDADO — Endpoint sin autenticación: `DashboardController.AuditoriaCajas`

`DashboardController` **no tiene `[Authorize]`** ni a nivel de clase ni en la acción.
`AuditoriaCajas` es accesible **sin iniciar sesión**.

El único control es `GetTenantId().Value` en la línea 26, que para un usuario anónimo
(`TenantId` nulo) lanza `InvalidOperationException` en vez de devolver 403. Es decir: falla
con un error 500, no con un 403 limpio. Y para un `AdminGlobal` authenticated (que tampoco
tiene claim) pasa lo mismo.

`Auditoria` además **no implementa `ITenantEntity`** ni tiene filtro global, así que el
filtrado es manual (línea 83: `a.TenantId == tenantId`).

**Qué hay que hacer:** agregar `[Authorize]` con el rol correspondiente y manejar el
`TenantId` nulo explícitamente.

**✅ CONSOLIDADO 2026-10-02.** `[Authorize(Roles = "Admin")]` a nivel de clase (el link de
`Views/Home/Index.cshtml` ya estaba dentro del bloque `@if (User.IsInRole("Admin"))`, así que ese
es el rol previsto) y el `.Value` a secas se reemplazó por el chequeo explícito que devuelve
`Forbid()`.
**Verificado en ejecución:** anónimo → **302** al login (antes **500** por excepción);
autenticado como Admin → **200**. `Auditoria` sigue sin filtro global: su filtrado manual por
`TenantId` es correcto y no se tocó.

### 🟠 ALTA 4 — `GetTenantId().Value` sin verificación: 86 ocurrencias

En 15 archivos, el claim se desenvuelve con `.Value` sin comprobar que exista:

```csharp
var tenantId = _tenantService.GetTenantId().Value;   // revienta si es null
```

Lanza `InvalidOperationException` cuando el claim no está: anónimos y **`AdminGlobal`**
(que por diseño no tiene `TenantId`).

Combinado con los `[Authorize]` sin roles de `CajaController`, `EgresosController`,
`FinanzasController`, `AlertasController` y `VentaController`, un `AdminGlobal` que entre a
esas acciones obtiene un error 500 en vez de una pantalla coherente.

`EtiquetasController` es de los pocos que usa el patrón correcto:
`if (!tenantIdNullable.HasValue) return Unauthorized();`

**Qué hay que hacer:** un helper único que devuelva un resultado explícito, y aplicarlo.

### 🟠 ALTA 5 — Autorización demasiado permisiva

| Controller | `[Authorize]` | Efecto |
|---|---|---|
| `DashboardController` | `Admin` | ✅ Corregido 2026-10-02 (era URGENTE 3) |
| `HomeController` | **ninguno** | Índice y Novedades públicos (parece intencional) |
| `CajaController` | sin roles | `AdminGlobal` abre/cierra cajas |
| `EgresosController` | sin roles | `AdminGlobal` registra egresos |
| `FinanzasController` | sin roles | rol `Caja` ve el dashboard financiero |
| `AlertasController` | sin roles | rol `Caja` ve alertas de stock |
| `VentaController` | sin roles | `AdminGlobal` opera el POS |
| `VentaAdminController` | `Admin,Caja` | rol `Caja` puede **anular ventas** |

Además, `AdminDashboardController` usa `[Authorize(Roles = "Admin")]` pero el rol real del
tenant es `Admin` — hay que verificar la coherencia con `UsuariosController`.

**Qué hay que hacer:** definir una matriz de roles (qué puede hacer cada uno) y aplicarla
explícitamente.

### 🟠 ALTA 6 — Falta antiforgery en endpoints POST

Sin `[ValidateAntiForgeryToken]`:

- `UsuariosController` — 3 POST (**crear/editar/borrar usuarios**)
- `AdminGlobalUsuariosController` — 1 POST (reset de password)
- `RubroController` — 6 de 7 POST
- `ProductosController` — `Importar`, `ConfirmarImportacion`
- `ClienteController.CrearRapido`, `ProductosApiController.CrearDesdeBase`,
  `NotificacionesController.MarcarLeida`

**Qué hay que hacer:** antiforgery en los POST con formulario; los que son API/JSON
necesitan otra protección (header anti-CSRF o Bearer).

### 🟠 ALTA 7 — GETs que modifican datos

`ClienteController.Desactivar` (línea ~282), `RubroController.Delete`,
`Desactivar` y `DeleteConfirmed` no tienen `[HttpPost]` ni antiforgery: mutan estado con un
GET. Un `<img src>` en una página externa alcanza para dispararlos.

### 🟡 MEDIA 8 — 20 de 26 controllers accediendo al `DbContext` directo

Solo 6 usan services. `ProductosController` tiene **905 líneas** con CRUD, import Excel,
reparación de stock y toda la lógica de stock inline. `VentaController` (529) tiene la
lógica de venta, pago y sincronización de stock dentro del controller.

La arquitectura del proyecto dice "Services = lógica de negocio". El código real tiene la
lógica en los controllers. `ProductoService`, `ProveedorService`, `AlertasService`,
`NotificacionService` y `ProductoImportService` están infrautilizados.

**Esto no es un bug:** es deuda que se vuelve más costosa cada vez que se toca el código. No
es urgente, pero conviene saberlo antes de agregar features.

### 🟡 MEDIA 9 — `FromSqlRaw` repetido 19 veces

Todas parametrizadas (`{0}`) → **no hay inyección SQL**. Pero:

- Repiten la lista de columnas de `Ventas` a mano en 19 lugares → agregar una columna a
  `Venta` rompe las 19 en runtime.
- El raw SQL **omite el filtro global** de tenant; la mayoría lo repone con
  `WHERE TenantId = {0}`, pero es fragile: depende de que cada consulta se acuerde.

### 🟡 MEDIA 10 — Tests de humo (P-017, en progreso)

**Resuelto en lo esencial el 2026-10-05.** Existe `tests/MiniMercado.Tests` con **88 tests
verdes** que cubren los tres flujos de dinero: cobro de suscripción, cierre de caja y venta
del POS.

Cada test crea su propia base descartable en el SQL Server (`MiniMercadoDB_test_<guid>`), la
construye solo con `Database.Migrate()` —el mismo camino que la app— y la dropea al terminar.
Los tests corren contra SQL Server real y no en memoria: ver la decisión en
`02_DECISIONES.md` (D-015).

Lo que los tests ya encontraron y se corrigió:

- **NRE en `VentaController.Create`** (`producto.PrecioEditable`): un `ProductoId` en 0 o
  perteneciente a otro tenant devolvía un 500 al cajero en mitad de una venta.
- **Venta con total descuadrado**: una línea con cantidad negativa restaba del total pero se
  perdía del detalle, así que `venta.Total` (0) no coincidía con la suma de sus renglones
  (1000) y la venta quedaba registrada como gratis.
- **Primera venta del día sin `CajaId`** en modo automático: la caja `AUTOMATICA` se creaba
  sin guardar, con `Id = 0`, así que esa venta quedaba huérfana y el resumen de cierre la
  contaba como "fuera de caja" aunque sí estuviera en el cajón.
- **Suscripción vencida por horas**: `CalcularEstado` y `CalcularDiasRestantes` comparaban
  instantes, no fechas. Un tenant que pagaba el día 5 a las 14:30 figuraba vencido a las
  15:30 del mismo día 5 y con un día menos de margen. Ahora se cuenta por días calendario.
- **Dashboard financiero al revés**: `FinanzasController.Dashboard` filtraba por `v.Activa`
  para "ventas del día" y del mes, pero con la convención invertida de `Venta.Activa` eso
  son las ventas **anuladas**. El panel mostraba como ingresos la suma de las canceladas, y
  un día con ventas reales y sin anulaciones aparecía en cero.
- **Regresión de P-022 protegida**: el monto realmente cobrado se persiste (antes se usaba
  el precio teórico del plan y los cobros quedaban en 0).

- **Venta y pago sin transacción (P-033, resuelto):** el POST de venta guardaba en tres
  `SaveChanges` independientes, así que un fallo al guardar el pago dejaba la venta
  registrada con el stock descontado y la caja acreditada, pero sin pago. Ahora todo el
  POST va en una transacción explícita que se confirma al final.

- **Fórmula del saldo de caja triplicada (P-032, resuelto):** el "Esperado" del arqueo estaba
  escrito en el controller y en las dos vistas de caja. Ahora vive en
  `Caja.CalcularSaldoEsperado()` y los tres la usan, así que el número que ve el usuario y el
  que queda en `Diferencia` no pueden separarse. Hay un test que falla si la fórmula reaparece
  escrita a mano en una vista.

**P-034 queda descartado por decisión** (D-017): no se suman IVA ni descuentos al alcance. No
había nada que corregir — `Precio` ya es el precio final y el total es `Precio × Cantidad`. Si
algún día se agrega, el punto delicado es el cierre de caja, no la fórmula.

Con esto quedan **cerrados los tres flujos de dinero que P-017 dejó abiertos**: venta (P-033),
cobro, cierre de caja (P-032) y dashboard financiero.

Sigue pendiente de testear: las vistas de caja (la fórmula del saldo esperado está escrita en
`CajaController.cs`, `CerrarCaja.cshtml` y `ResumenCierre.cshtml`) y el resto de controllers.

`FAQ_EGRESOS.md` sugiere que hubo problemas con el cálculo de egresos antes: el flujo de
cierre ya tiene 12 tests que cubren egresos anulados y egresos de otra caja.

### 🟡 MEDIA 11 — Sin logging

`ILogger<T>` no está inyectado en **ningún** archivo de controllers o services. Cuando algo
falla en producción, no hay traza.

> **Parcialmente cerrado 2026-10-05 (P-026).** `Program.cs` ahora **sí** loguea: las
> migraciones y el seed reportan lo que hacen, avisan cuántas migraciones pendientes hay
> cuando están desactivadas, y loguean `Critical` y re-lanzan si fueron solicitadas y
> fallan. Sigue faltando `ILogger<T>` en controllers y services: un error de negocio dentro
> de una acción no deja registro. El `catch` que este caso le atribuía al "parche SQL sin
> `try/catch`" ya no aplica: ese parche fue eliminado en 2026-10-02.

---

## 6. Misión: stack Docker

**Objetivo:** que MiniMercado corra en Linux dentro de Docker Compose, con la base
contenerizada, replicando el patrón que ya se está usando en LiteState.

### 6.1 Cómo se hace en LiteState (el patrón a replicar)

| Pieza | Cómo resuelve LiteState |
|---|---|
| Dockerfile | Multi-stage: SDK compila, runtime slim corre. Usuario no root. Healthcheck con `curl`/wget. |
| Base de datos | Contenedor oficial, puerto atado a `127.0.0.1` en el laboratorio. |
| Compose | `name:` fijo para que los volúmenes no cambien de nombre al renombrar la carpeta. |
| Secretos | `.env` **ignorado por git**; `*.env` en `.gitignore`. |
| Reverse proxy | Nginx, única puerta de entrada, `/api` y estáticos. |
| HTTPS | Nginx + Certbot (webroot). |
| Puertos | Nunca `-p 5432:5432` a secas: se ata a loopback. |
| Migraciones | `Database.Migrate()` en el arranque, revisado. |
| Deploy | Script documentado, paso a paso. |

Los detalles de cada pieza están en `Docs/Manual Docker - de cero a VPS.md` de LiteState
(fases 1 a 18).

### 6.2 Pasos del camino (propuesta, sin ejecutar)

Cada paso es verificable por separado y **no depende del siguiente** hasta que se decide.

| # | Paso | Qué implica | Bloqueado por |
|---|---|---|---|
| 1 | **Rotar y sacar las contraseñas del seed** | URGENTE 1 | Nada. Hacer primero. |
| 2 | **Arreglar los 2 bugs de aislamiento** | URGENTE 2 y 3 | Nada |
| 3 | **Arreglar connection string** | `appsettings.json` inválido, quitar `.\SQLEXPRESS`, dejar la variable para `IConfiguration` | Decidir dónde vive la BD |
| 4 | **Decidir motor de base de datos** | ¿SQL Server en Linux? ¿Migrar a PostgreSQL? | **Decisión del TEAM LEAD** |
| 5 | **Subir el framework** | .NET 6 (EOL) → .NET 8 LTS o .NET 10 | Puede ir antes o después del 4 |
| 6 | **Dockerfile** | Multi-stage para `netX` | Pasos 4 y 5 |
| 7 | **`compose.yaml`** | App + base + proxy | Paso 6 |
| 8 | **`.env` + `.gitignore`** | Secretos fuera del repo | Paso 7 |
| 9 | **Healthchecks y logging** | Para poder debugear el contenedor | Paso 7 |
| 10 | **Deploy documentado** | Igual que las fases 16-18 del manual | Paso 7 |

> 🔴 **Los pasos 4, 5 y 6 están acoplados.** El motor de base de datos determina el
> connection string, y el framework determina el Dockerfile. Cambiar los tres juntos
> multiplica las variables. Por eso conviene decidir el motor **antes** de escribir código.

> **Actualización 2026-10-05: P-026 ya no bloquea al paso 6.** El paso 6 (Dockerfile) venía
> depending de que se sacaran las migraciones del arranque, y eso está hecho (§4.2). Así que
> el paso 6 **solo** depende ahora del motor (D-1) y el framework (D-2). En una imagen con
> varias réplicas, la bandera `Bootstrap:ApplyMigrations` se activa en **una sola** (§6.3).

### 6.3 Restricciones que impone el código actual

Estas no son preferencias: son cosas que **no funcionan** en Linux/contenedor tal como están.
**Actualizado 2026-10-05:** las tres primeras ya están resueltas, quedaban listadas como
vigentes y era falso.

| Restricción | Ubicación | Por qué |
|---|---|---|
| ~~**T-SQL en el parche de arranque**~~ | `Program.cs` | ✅ **Resuelto 2026-10-02** (P-002/P-003): era T-SQL de SQL Server, no de Windows, y el parche **se eliminó por completo**, ahora son migraciones reales. |
| ~~**Autenticación Windows**~~ | `appsettings.json` | ✅ **Resuelto 2026-10-02**: no queda connection string en el repo, se pasa por variable de entorno. |
| ~~**`dotnet-ef 10.0.5`**~~ | `dotnet-tools.json` | ✅ **Resuelto 2026-10-02**: ahora `6.0.36`, y `dotnet ef` funciona. |
| **`\` en rutas** | `.csproj` (`<Folder Include="wwwroot\Images\" />`) | Backslash de Windows. No rompe Linux, pero denota residuo. |
| **`FromSqlRaw` con T-SQL** | 19 lugares | Si se migra a PostgreSQL, hay que reescribirlos. |
| **Sin `global.json`** | raíz | El SDK no está pinneado: cada build puede usar una versión distinta. |
| **Ejecución como root** | no configurado | La imagen debe correr como usuario no root, como en LiteState. |
| **Migraciones en el arranque sin candado** | `Program.cs` | ✅ **Mitigado 2026-10-05 (P-026)**: ahora depende de `Bootstrap:ApplyMigrations`. **Sigue siendo de un solo escritor**: si se corre en varias réplicas, hay que activar la bandera en **una sola**. |

### 6.4 Lo que hay que decidir (bloqueante)

Estas decisiones cambian el resto del plan. **No se avanza sin resolverlas.**

| # | Decisión | Opciones | Por qué importa |
|---|---|---|---|
| D-1 | **Motor de base de datos** | (a) SQL Server en Linux (contenedor oficial / VM) · (b) PostgreSQL · (c) SQL Server en Azure otra vez | (b) es más liviano para contenedor pero implica reescribir 19 `FromSqlRaw` y el parche T-SQL. (a) es lo más cercano al código actual. |
| D-2 | **Framework destino** | .NET 8 LTS · .NET 10 LTS | .NET 6 está en EOL. Hay que decidir antes de escribir el Dockerfile. |
| D-3 | **¿Dónde vive la app?** | Solo local por ahora · VPS propio (como LiteState) · Nube | Define si hace falta reverse proxy y HTTPS desde el inicio. |
| D-4 | **Sobre las contraseñas ya expuestas** | ¿Rotar solo en la base? ¿También limpiar el historial de git? | Limpiar el historial reescribe commits. Es decisión del TEAM LEAD. |
| D-5 | **Alcance del arreglo de arquitectura** | ¿Mover lógica a services ahora o dejarlo? | Los 20 controllers con DbContext directo no bloquean Docker. Es deuda, no urgencia. |
| D-6 | **Qué pasa al borrar un rubro con productos** (P-030) | ✅ **RESUELTA 2026-10-05** → **D-013**: opción (c), `RubroId` nullable de verdad. Era la única que bloqueaba el cierre de P-025, y P-025 ya está cerrado. | El modelo ya no puede declarar obligatoria una relación cuyo FK es `SET_NULL`, así que la opción (a) —bloquear con mensaje— era la incompatible con el modelo actual. |

---

## 7. Lo que está bien (no tocar)

Para no romper cosas al ordenar el código:

- **El patrón de aislamiento funciona.** `HasQueryFilter` + claim `TenantId` +
  `TenantValidationMiddleware` es correcto. Los 2 bugs son **huecos**, no un diseño malo.
- **`EtiquetasController` tiene el patrón correcto** de manejo de `TenantId` nulo:
  sirve de referencia para arreglar el resto.
- **`ProveedorService` está bien separado**: sirve de modelo para los otros services.
- **Las 60 migraciones** dan una base sólida. Las 7 columnas de suscripción ya no son el
  problema: las crea `20261002143249_AddSuscripcionColumnsToTenant` y el parche T-SQL desapareció.
- **El importador Excel** con ClosedXML y control de duplicados funciona.
- **La UI oscura** está definida y es consistente.
- **Existe una barrera que detecta la deriva modelo↔migraciones** (`tools/SchemaCheck` +
  `tools/verificar-esquema.sh`, §4.3). Antes dependía de que nadie se acordara de correrla; ahora
  es un script de un comando con código de salida por tipo de fallo.

---

## 8. Registro de cambios

| Fecha | Qué cambió |
|---|---|
| 2026-10-01 | Documento inicial: auditoría completa del estado, 11 casos urgentes, plan Docker en 10 pasos. Sin cambios en código. |
| 2026-10-01 | Memoria progresiva completa según `MANUAL_MEMORIA_AGENTES.md`: creados `00_MEMORIA.md`, `02_DECISIONES.md`, `03_PROBLEMAS.md`, `PROMPT_INICIO.md`. D-008 a D-012 quedan abiertas esperando al TEAM LEAD. Sin cambios en código. |
| 2026-10-02 | **Objetivo cumplido: la app arranca y se ve en el navegador.** Base local creada (SQL Server 2022 en Docker) + runtime 6 en `~/.dotnet`. Corregidos y **verificados en ejecución** los 3 casos URGENTE (1, 2, 3) → CONSOLIDADO. Corregido el orden `Migrate()`/parche T-SQL, que mataba el primer arranque en bases nuevas. `appsettings.json` quedó válido y sin credenciales. **No se resolvió ninguna decisión pendiente (D-008 a D-012 siguen abiertas).** |
| 2026-10-02 | **P-002, P-003 y P-012 cerrados; aparecieron P-022, P-023 y P-024.** `dotnet-ef` arreglado (6.0.36 + SDK 6.0.428) y migración real `20261002143249_AddSuscripcionColumnsToTenant` generada; **el parche T-SQL de `Program.cs` se eliminó**. Verificado **borrando la base y reconstruyéndola solo con migraciones**: 58 migraciones, 28 tablas, app sana. Al hacerlo aparecieron **3 problemas nuevos**: el **monto cobrado nunca se guardaba** (`CobroTenant.Monto` usaba `PrecioMensual * meses` en vez del monto real → todos los cobros en 0 y el KPI "Recaudación Total" en $0, P-022), una **segunda divergencia modelo/migraciones** en `ImpresionEtiqueta` (faltaban el índice y el FK a `Tenants`, P-023) y `PrecioSugerido` aceptando input del cliente (P-024, cerrado con `[BindNever]`). Los tres corregidos y verificados: cobro de 45000 guardado como 45000.00 y recaudación mostrando $45,000. **D-008 a D-012 siguen abiertas.** |
| 2026-10-02 | **Revisión de arquitectura previa a Dockerizar: P-025 a P-028.** Se documentó la deuda que va a doler a futuro y que **no** era visible en desarrollo: nada detecta la deriva modelo↔migraciones (P-025, máximo prioriod porque produjo P-002 y P-023), `Database.Migrate()` en cada arranque no escala a más de una instancia porque EF Core 6 no tiene lock de migraciones (P-026, resolver antes del Dockerfile), framework EOL desde nov-2024 (P-027) y ausencia de health checks (P-028). Además se reforzaron cuatro problemas ya existentes en vez de duplicarlos: P-006/P-007 (autorización: la solución es structurally falla cerrada, no parchear 86 `.Value`), P-016 (sin tests: los flujos de dinero no tienen red de seguridad, P-022 lo prueba), P-011 (logging) y P-001/D-011 (historial público: rotar no alcanza, el commit `a5d20ba` sigue ahí). **Sin cambios en código.** |
| 2026-10-02 | **P-025 arranca y encuentra dos bugs vivos: P-029 cerrado, P-030 abierto.** Se construyó `tools/SchemaCheck` (herramienta de desarrollo, fuera de la app y de la imagen Docker): levanta dos bases de descarte, una **solo con migraciones** y otra **desde el modelo**, y compara columnas con su nulabilidad, índices y FKs con su acción de borrado. Devuelve código de salida 1 si hay deriva. **Primera ejecución:** encontró que `EgresosCaja.Anulado` estaba en el modelo y en el snapshot pero **en ninguna migración**, y como se usa en un filtro global, `GET /Egresos` daba **500** en todos los entornos → cerrado con `20261002163400_AgregaAnuladoA_EgresosCaja` y verificado en **200** (P-029). También encontró que **borrar un rubro con productos falla**: el FK es `SET_NULL` sobre una columna que el propio modelo declara obligatoria (P-030, abierto, requiere decisión de negocio). Dos trampas que la herramienta tuvo que esquivar: `migrations add` no detecta esta clase de problema cuando el snapshot está adelantado, y en EF Core 6 `EnsureCreated()` aplica las migraciones en vez de usar el modelo. **59 migraciones.** |
| 2026-10-05 | **P-030 cerrado por decisión del TEAM LEAD (D-013): el rubro es opcional.** El diagnóstico original de P-030 era **erróneo**: la causa no era el `DeleteBehavior` sino el `[Required]` de `Producto.RubroId` combinado con la navegación no-nullable `Rubro`, y `RubroController` nunca hace hard-delete (solo soft). Cerrado con `20261005101937_RubroIdOpcional`: `RubroId` y `Rubro` nullable, y el formulario de alta/edición ofrece "sin rubro". Verificado en ejecución: producto sin rubro persiste NULL y las páginas relacionadas dan 200. Al hacerlo apareció **P-031**: el snapshot estaba adelantado, así que la migración generada salió con un `Up()` **vacío**. **60 migraciones**, `SchemaCheck` **exit 0**. |
| 2026-10-05 | **P-026 cerrado: las migraciones y el seed salen del arranque por defecto.** `Program.cs` tenía **dos** escrituras por arranque, no una: `Migrate()` **y** `SeedData.Inicializar()`. Quitar solo `Migrate()` no cerraba nada, porque el seed seguía corriendo en cada boot y tenía su propia carrera —con un índice único en `ProductoBase.CodigoBarras` que hace fallar a la segunda réplica con duplicate key. Ahora ambas están detrás de banderas independientes (`Bootstrap:ApplyMigrations` / `Bootstrap:Seed`), **activas solo en Development** e inactivas en cualquier otro entorno, con `try/catch` + `LogCritical` que re-lanza. Extra: con migraciones desactivadas se avisa cuántas pendientes hay y cuáles son las 3 primeras. **Verificado en ejecución en 4 escenarios**, incluido **dos réplicas en paralelo levantando a la vez, ambas sanas y sirviendo tráfico**. |
| 2026-10-05 | **P-025 cerrado (paso 2): la barrera corre sola, con un comando.** Se creó `tools/verificar-esquema.sh`, que corre los **tres** chequeos de la solución de fondo y sale con código distinto por fallo: **A** deriva modelo/migraciones (`SchemaCheck`), **B** modelo adelantado al snapshot (genera una migración de prueba y mira si tiene contenido) y **C** migraciones sin commitear. Se agregó C porque A y B miden el modelo y ninguno miraba el working tree. **Los tres fallos se probaron inyectándolos a mano**: una columna al modelo sin migración la reportan A y B; el estado real del repo lo reporta C. La migración de prueba de B se revierte siempre, incluso si el script se corta. Es **agnóstico de plataforma** a propósito: el repo no tiene CI y elegir una es decisión de infraestructura pendiente. **P-025 no está 100% cerrado: falta que corra sola (CI) y el paso 3, que sigue siendo regla escrita.** |
| 2026-10-05 | **Este documento pasa a fecha 2026-10-05.** Se sincronizó con lo realmente hecho: **60** migraciones (decía 59), §4.2 reescrito (ya no dice que `Migrate()` corre en cada arranque), **§4.3 nuevo** con la barrera de verificación, `dotnet-ef` corregido en §2, filas nuevas en §3.2, **§6.3 saneada** (listaba como vigentes el parche T-SQL, la autenticación Windows y `dotnet-ef 10.0.5`, los tres ya resueltos desde 2026-10-02), **D-6 resuelta** y §6.2 anotando que P-026 ya no bloquea al Dockerfile. §5 se marca como **registro histórico**: la lista vigente de problemas abiertos está en `03_PROBLEMAS.md`. **Sin commit: el working tree tiene estos 11 archivos sin commitear.** |