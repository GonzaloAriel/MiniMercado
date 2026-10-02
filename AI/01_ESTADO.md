FORMATO: 1.0
FECHA: 2026-10-02
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
- **Working tree:** limpio
- **Commits sin pushear:** ninguno

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
- `dotnet-tools.json` pide **`dotnet-ef 10.0.5`** con `rollForward: false`, contra EF Core
  6.0.36. **Incompatible**: cualquier `dotnet ef` falla.

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
`MiniMercadoDB` creada y las 58 migraciones aplicadas. Es **descartable**: no hay datos que
valgan. El detalle de cómo levantarlo está en `00_MEMORIA.md` §"Entorno local".

**Lo que sigue sin decidir:** dónde vive la base en el entorno definitivo (D-008 / D-010).

### 4.2 Migraciones

- **58 migraciones** + snapshot. Última:
  `20261002143249_AddSuscripcionColumnsToTenant`.
- `Program.cs` ejecuta `db.Database.Migrate()` en **cada arranque** → las migraciones
  pendientes se aplican solas al iniciar.

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

---

## 5. Casos urgentes a corregir

Ordenados por gravedad.

> **CONSOLIDADO 2026-10-02:** los tres casos URGENTE (1, 2 y 3) están
> **CONSOLIDADOS**: corregidos **y verificados en ejecución** contra SQL Server
> 2022. Ver el detalle de la verificación al final de cada caso. Los casos
> ALTA/MEDIA (4 a 11) siguen open.

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

### 🟡 MEDIA 10 — Sin tests

Ningún proyecto de tests. Un refactor de stock o de cálculo de caja no tiene red de
seguridad. `FAQ_EGRESOS.md` sugiere que hubo problemas con el cálculo de egresos antes.

### 🟡 MEDIA 11 — Sin logging

`ILogger<T>` no está inyectado en **ningún** archivo. Cuando algo falla en producción, no hay
traza. `Program.cs` ejecuta el parche SQL sin `try/catch`: si falla, la app muere sin
registro.

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

### 6.3 Restricciones que impone el código actual

Estas no son preferencias: son cosas que **no funcionan** en Linux/contenedor tal como están.

| Restricción | Ubicación | Por qué |
|---|---|---|
| **T-SQL en el parche de arranque** | `Program.cs:54-66` | Usa `sys.columns` y `OBJECT_ID`. No es portable. Hay que convertirlo en migración real. |
| **Autenticación Windows** | `appsettings.json` | `Trusted_Connection=True` no existe en Linux. Necesita SQL auth. |
| **`dotnet-ef 10.0.5`** | `dotnet-tools.json` | Incompatible con EF 6 y con `rollForward:false`. `dotnet ef` falla hoy. |
| **`\` en rutas** | `.csproj` (`<Folder Include="wwwroot\Images\" />`) | Backslash de Windows. No rompe Linux, pero denota residuo. |
| **`FromSqlRaw` con T-SQL** | 19 lugares | Si se migra a PostgreSQL, hay que reescribirlos. |
| **Sin `global.json`** | raíz | El SDK no está pinneado: cada build puede usar una versión distinta. |
| **Ejecución como root** | no configurado | La imagen debe correr como usuario no root, como en LiteState. |

### 6.4 Lo que hay que decidir (bloqueante)

Estas decisiones cambian el resto del plan. **No se avanza sin resolverlas.**

| # | Decisión | Opciones | Por qué importa |
|---|---|---|---|
| D-1 | **Motor de base de datos** | (a) SQL Server en Linux (contenedor oficial / VM) · (b) PostgreSQL · (c) SQL Server en Azure otra vez | (b) es más liviano para contenedor pero implica reescribir 19 `FromSqlRaw` y el parche T-SQL. (a) es lo más cercano al código actual. |
| D-2 | **Framework destino** | .NET 8 LTS · .NET 10 LTS | .NET 6 está en EOL. Hay que decidir antes de escribir el Dockerfile. |
| D-3 | **¿Dónde vive la app?** | Solo local por ahora · VPS propio (como LiteState) · Nube | Define si hace falta reverse proxy y HTTPS desde el inicio. |
| D-4 | **Sobre las contraseñas ya expuestas** | ¿Rotar solo en la base? ¿También limpiar el historial de git? | Limpiar el historial reescribe commits. Es decisión del TEAM LEAD. |
| D-5 | **Alcance del arreglo de arquitectura** | ¿Mover lógica a services ahora o dejarlo? | Los 20 controllers con DbContext directo no bloquean Docker. Es deuda, no urgencia. |

---

## 7. Lo que está bien (no tocar)

Para no romper cosas al ordenar el código:

- **El patrón de aislamiento funciona.** `HasQueryFilter` + claim `TenantId` +
  `TenantValidationMiddleware` es correcto. Los 2 bugs son **huecos**, no un diseño malo.
- **`EtiquetasController` tiene el patrón correcto** de manejo de `TenantId` nulo:
  sirve de referencia para arreglar el resto.
- **`ProveedorService` está bien separado**: sirve de modelo para los otros services.
- **Las 58 migraciones** dan una base sólida. Las 7 columnas de suscripción ya no son el
  problema: las crea `20261002143249_AddSuscripcionColumnsToTenant` y el parche T-SQL desapareció.
- **El importador Excel** con ClosedXML y control de duplicados funciona.
- **La UI oscura** está definida y es consistente.
- **Git limpio**: sin commits pendientes.

---

## 8. Registro de cambios

| Fecha | Qué cambió |
|---|---|
| 2026-10-01 | Documento inicial: auditoría completa del estado, 11 casos urgentes, plan Docker en 10 pasos. Sin cambios en código. |
| 2026-10-01 | Memoria progresiva completa según `MANUAL_MEMORIA_AGENTES.md`: creados `00_MEMORIA.md`, `02_DECISIONES.md`, `03_PROBLEMAS.md`, `PROMPT_INICIO.md`. D-008 a D-012 quedan abiertas esperando al TEAM LEAD. Sin cambios en código. |
| 2026-10-02 | **Objetivo cumplido: la app arranca y se ve en el navegador.** Base local creada (SQL Server 2022 en Docker) + runtime 6 en `~/.dotnet`. Corregidos y **verificados en ejecución** los 3 casos URGENTE (1, 2, 3) → CONSOLIDADO. Corregido el orden `Migrate()`/parche T-SQL, que mataba el primer arranque en bases nuevas. `appsettings.json` quedó válido y sin credenciales. **No se resolvió ninguna decisión pendiente (D-008 a D-012 siguen abiertas).** |
| 2026-10-02 | **P-002, P-003 y P-012 cerrados; aparecieron P-022, P-023 y P-024.** `dotnet-ef` arreglado (6.0.36 + SDK 6.0.428) y migración real `20261002143249_AddSuscripcionColumnsToTenant` generada; **el parche T-SQL de `Program.cs` se eliminó**. Verificado **borrando la base y reconstruyéndola solo con migraciones**: 58 migraciones, 28 tablas, app sana. Al hacerlo aparecieron **3 problemas nuevos**: el **monto cobrado nunca se guardaba** (`CobroTenant.Monto` usaba `PrecioMensual * meses` en vez del monto real → todos los cobros en 0 y el KPI "Recaudación Total" en $0, P-022), una **segunda divergencia modelo/migraciones** en `ImpresionEtiqueta` (faltaban el índice y el FK a `Tenants`, P-023) y `PrecioSugerido` aceptando input del cliente (P-024, cerrado con `[BindNever]`). Los tres corregidos y verificados: cobro de 45000 guardado como 45000.00 y recaudación mostrando $45,000. **D-008 a D-012 siguen abiertas.** |
| 2026-10-02 | **Revisión de arquitectura previa a Dockerizar: P-025 a P-028.** Se documentó la deuda que va a doler a futuro y que **no** era visible en desarrollo: nada detecta la deriva modelo↔migraciones (P-025, máximo prioriod porque produjo P-002 y P-023), `Database.Migrate()` en cada arranque no escala a más de una instancia porque EF Core 6 no tiene lock de migraciones (P-026, resolver antes del Dockerfile), framework EOL desde nov-2024 (P-027) y ausencia de health checks (P-028). Además se reforzaron cuatro problemas ya existentes en vez de duplicarlos: P-006/P-007 (autorización: la solución es structurally falla cerrada, no parchear 86 `.Value`), P-016 (sin tests: los flujos de dinero no tienen red de seguridad, P-022 lo prueba), P-011 (logging) y P-001/D-011 (historial público: rotar no alcanza, el commit `a5d20ba` sigue ahí). **Sin cambios en código.** |