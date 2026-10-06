# 00_MEMORIA.md

FORMATO: 1.0

## Proyecto

MiniMercado

ERP/POS SaaS multi-tenant para comercios minoristas. Varios comercios comparten
la misma base de datos con aislamiento por `TenantId`. NO es un sistema de
punto de venta de escritorio ni un monolito de un solo comercio.

## Stack (real, verificado en el código)

| Capa | Tecnología |
|------|-----------|
| Lenguaje | C# / .NET 6 (`net6.0`), `Nullable` + `ImplicitUsings` |
| Framework | ASP.NET Core 6.0, MVC + Razor Pages |
| ORM | EF Core 6.0.36, `Migrations/` con 60 migraciones |
| Base de datos | SQL Server (`EFCore.SqlServer` 6.0.36) — connection string **por variable de entorno** |
| Auth | ASP.NET Core Identity + `ApplicationUser` + `CustomClaimsPrincipalFactory` |
| UI | Bootstrap 5 + jQuery, tema oscuro propio, ViewComponents |
| Importación | ClosedXML 0.105.0 |
| Contenedores | Ninguno |
| Tests | Ninguno |

SDK instalado en la máquina: **10.0.112** (el único SDK). Runtimes en
`/usr/lib/dotnet`: **10.0.12** y **6.0.36** (los dos, desde 2026-10-06, ver
P-013). Con el runtime 6 en el sistema, `dotnet run` y `dotnet test` funcionan
con el dotnet del sistema, sin hacks.

## Arquitectura

```
Views (Razor)  ->  Controllers  ->  Services  ->  MiniMercadoContext  ->  SQL Server
                                        |              |
                                        |              +-- HasQueryFilter global
                                        |                  por TenantId
                                        +-- TenantService (resuelve claim TenantId)

Middleware/TenantValidationMiddleware: corre antes que cualquier request,
bloquea si el tenant esta vencido o inactivo.
```

Reglas inviolables:

1. **Aislamiento**: nunca escribir una consulta sin tener presente el `TenantId`.
   Las entidades que implementan `ITenantEntity` necesitan `HasQueryFilter` en
   `MiniMercadoContext.cs`. Si se agrega una entidad nueva hay que agregar el filtro
   en la misma línea; si no, es una fuga entre tenants.
2. **Stock**: nunca modificar `Producto.Stock` a mano. El origen de verdad son los
   `LoteProducto`; `Producto.Stock` se recalcula con
   `ProductoService.SincronizarStock`.
3. **Auditoría**: todo cambio crítico pasa por `AuditoriaService.Registrar`.
4. **Finanzas**: no persistir totales calculados. Todo con `SumAsync` en vivo
   sobre `Ventas` activas y `EgresosCaja` no anulados.
5. **Sin T-SQL fuera de migraciones**: hoy `Program.cs:49-69` ejecuta un parche
   crudo (ver P-003). No se agregan más `ExecuteSqlRaw` con T-SQL.
6. **Claims**: `AdminGlobal` **no tiene `TenantId`** por diseño, y sin `TenantId` los
   query filters se desactivan. Antes de usar `GetTenantId()`, comprobar que tiene valor.

## Entorno local (verificado 2026-10-06)

La app **arranca y se ve en el navegador** en `http://127.0.0.1:5267`. Login:
`admin@demo.com` (Admin del tenant demo) o `global@admin.com` (AdminGlobal).

No hay nada de esto en el repositorio: es estado de la maquina, y es
**descartable**.

**1. Base: contenedor Docker `minimercado-sql`**

```
docker run -d --name minimercado-sql \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<password>' -e MSSQL_PID=Developer \
  -p 127.0.0.1:1433:1433 --restart unless-stopped \
  mcr.microsoft.com/mssql/server:2022-latest
```

Tarda ~10 s en aceptar logins. La base `MiniMercadoDB` **no hay que crearla a
mano**: la crea `Database.Migrate()` en el primer arranque, junto con las 60
migraciones.

**2. Runtimes .NET 6.0.36** (`Microsoft.NETCore.App` y
`Microsoft.AspNetCore.App`) en `/usr/lib/dotnet`, instalados con
`dotnet-install.sh --runtime ... --version 6.0.36 --install-dir /usr/lib/dotnet --no-path`
(2026-10-06, con sudo; reversible quitando esas dos carpetas de
`shared/`). Tambien quedan en `~/.dotnet` el SDK 6.0.428 y los mismos runtimes
(sin sudo), que sigue siendo donde viven `dotnet ef`.

**3. Compilar con el SDK del sistema, ejecutar con el muxer de `~/.dotnet`:**

```
cd MiniMercado   # proyecto: <repo>/MiniMercado
dotnet build
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS='http://127.0.0.1:5267' \
ConnectionStrings__MiniMercadoDB='Server=127.0.0.1,1433;Database=MiniMercadoDB;User Id=sa;Password=<password>;TrustServerCertificate=True;' \
Seed__PasswordAdminGlobal='<password>' \
Seed__PasswordAdminDemo='<password>' \
Seed__PasswordCajaDemo='<password>' \
~/.dotnet/dotnet bin/Debug/net6.0/MiniMercado.dll
```

`dotnet run` **sirve desde 2026-10-06**: el muxer del sistema ya encuentra el
runtime 6 y usa el perfil `http` de `launchSettings.json` (Development, puerto
5267). Pero `launchSettings.json` **no** incluye la connection string, y en
Development la app hace seed al arrancar: sin ella muere. Exportala a mano
(mismo valor que en el comando de tests de mas abajo) o usa F5 en VS Code, que
la lee de `.env` (ver mas abajo).

Las tres variables `Seed__*` son obligatorias: sin ellas el seed no crea
ningun usuario (ver P-001 en `03_PROBLEMAS.md`).

**Tests (P-017):** desde 2026-10-06 corren con el dotnet del sistema y sin
ningun hack (los runtimes 6.0.36 ya estan en `/usr/lib/dotnet`, ver P-013).
Desde `tests/MiniMercado.Tests`:

```
ConnectionStrings__MiniMercadoDB='Server=127.0.0.1,1433;Database=MiniMercadoDB;User Id=sa;Password=<password>;TrustServerCertificate=True;' \
  dotnet test
```

(antes habia que agregar `DOTNET_ROOT=$HOME/.dotnet` y llamar al muxer de
`~/.dotnet`; ya no.)

Cada test crea su propia base `MiniMercadoDB_test_<guid>` y la dropea al terminar,
asi que tarda minutos: la ultima corrida completa fue **94 tests en ~3 min**
(2026-10-06, verificado sin `DOTNET_ROOT`).

**VS Code (F5):** `.vscode/launch.json` (commiteable, **sin secretos**) arranca
`bin/Debug/net6.0/MiniMercado.dll` en `http://localhost:5267` con
`ASPNETCORE_ENVIRONMENT=Development` y toma la connection string de `.env` en la
raiz del proyecto (cubierto por el gitignore `*.env`, por eso no sube al repo).
`source .env` **no sirve en bash**: el `;` de la connection string parte la linea
en comandos — en terminal exportala a mano. Si ya hay una instancia de la app en
el 5267, cortala antes de un F5 (puerto ocupado).

**Banderas de arranque (P-026):** las migraciones y el seed **no se ejecutan
automáticamente** fuera de Development. Si se corre la app con
`ASPNETCORE_ENVIRONMENT=Development` (el comando de arriba), se siguen
aplicando solas y no hay que hacer nada. En cualquier otro entorno hay que
pedirlas:

```
Bootstrap__ApplyMigrations=true   # aplica las migraciones pendientes
Bootstrap__Seed=true              # crea roles, tenant demo y catalogo
```

Sin las banderas, en un entorno no-Development la app **arranca sin migrar y sin
seed**, y deja en el log cuantas migraciones faltan y cuales son las 3
primeras. Ojo: los valores por defecto son distintos por entorno, asi que al
testear en Production hay que ponerlas a mano.

**4. Migraciones:** el SDK 6.0.428 y `dotnet-ef` 6.0.36 viven en `~/.dotnet`.
`dotnet ef` **exige una connection string** aunque nunca se conecte:

```
cd MiniMercado
ConnectionStrings__MiniMercadoDB='Server=127.0.0.1,1433;Database=MiniMercadoDB;User Id=sa;Password=<password>;TrustServerCertificate=True;' \
~/.dotnet/dotnet ef migrations add <Nombre> --output-dir Migrations
```

**Trampa al mantener migraciones:** si el modelo ya tiene columnas que ninguna
migración crea, `migrations add` no genera nada para ellas porque el snapshot
las contiene. Hay que sacarlas del snapshot **antes** de generar la migración
(P-002). El snapshot describe el modelo, no el historial.

**Trampa inversa (P-031):** si el snapshot está **adelantado** respecto del
historial, `migrations add` genera un `Up()` **vacío**. Pasa cuando se corrige
el modelo y se "acomoda" el snapshot a mano en lugar de dejarlo como está. El
orden correcto es el inverso: **dejar el snapshot como está en las migraciones**,
correr `migrations add`, y que **EF reescriba el snapshot solo**. Siempre mirar
el `Up()` generado antes de dar por buena una migración: si está vacío, el
snapshot iba adelantado y la migración no arregla nada.

**5. Cambiar los permisos de un controller (P-006 / D-014):** la matriz de roles
vive en los `[Authorize]` y **los de clase y de metodo se combinan con AND, no con
OR**. Poner `Roles = "Admin,Caja"` en la clase y `Roles = "AdminGlobal,Admin"` en el
metodo deja permitted **solo a `Admin`**: es la interseccion, y es facil creer que es
una union. Para dejar una excepcion hay que hacer al reves: los tres roles en la
clase, y el metodo de excepcion repetido los que correspondan. Asi esta
`ProductosController`: clase `Admin,Caja,AdminGlobal`, y cada accion de tenant con
`Roles = "Admin"`.

**De paso:** la denegacion devuelve **302** (redirige al login) y no **403**, porque el
scheme de cookies de Identity re-lanza en vez de forbidding. El acceso esta denegado
igual, pero para un usuario ya autenticado un `403` seria lo correcto. Queda anotado
en P-006, no arreglado.

**6. Verificar que migraciones y modelo no divergan (P-025):**

```
cd MiniMercado   # proyecto
~/.dotnet/dotnet build tools/SchemaCheck/SchemaCheck.csproj
ConnectionStrings__MiniMercadoDB='...' ~/.dotnet/dotnet run --project tools/SchemaCheck
```

Compara una base creada con migraciones contra una creada desde el modelo
(salida 1 = hay deriva). **Siempre reconstruir la herramienta antes de correrla**
(sin `--no-build`): su carpeta de salida tiene su propia copia de la app y con
`--no-build` corre contra un binario viejo que puede no tener la migración más
nueva. Descarta las bases al terminar. Es herramienta de desarrollo: no va en la
imagen Docker.

**Ojo: para verificar el esquema, correr el script, no `SchemaCheck` a mano.**
`SchemaCheck` es solo el chequeo A (deriva modelo/migraciones). El script agrega
los otros dos y el que importa es el que mira si generaste una migración y la
olvidate de commitear:

```
ConnectionStrings__MiniMercadoDB='...' tools/verificar-esquema.sh
```

| Código | Significado |
|---|---|
| 0 | todo bien |
| 1 | hay deriva entre el modelo y las migraciones (chequeo A) |
| 2 | falta `ConnectionStrings__MiniMercadoDB` |
| 3 | el modelo está adelantado al snapshot (chequeo B) |
| 4 | hay migraciones sin commitear (chequeo C) |
| 5 | error interno (no se pudo compilar o correr) |

El **3 sale aunque no haya nada roto en la base**: significa que cambiaste el
modelo y todavía no generaste la migración. Se arregla con
`dotnet ef migrations add <Nombre>` y commiteando modelo y migración juntos. El
chequeo B genera una migración de prueba y la revierte siempre, incluso si el
script se corta a la mitad. Sale con **1** y con **3** a la vez no puede pasar: el
script corta en el primero.

**La barrera corre en CI (2026-10-06).** `.github/workflows/ci.yml` (GitHub
Actions; el remoto ya era GitHub) corre build + los 94 tests +
`verificar-esquema.sh` en cada push a `main` y en cada PR, contra un SQL Server
2022 en service container. El connection string sale de
`secrets.MSSQL_SA_PASSWORD` (seteado con `gh secret set`; la contrasena jamas se
commitea). Asi el paso 3 de P-025 lo impide el pipeline, no la disciplina.

Dos trampas del CI/local:
- `global.json` en la raiz fija SDK >= 6.0.428 con `rollForward: latestMajor`
  (P-014): permite build con el SDK 10 del sistema y en CI instala 6.0.x.
- `dotnet tool restore` con el SDK 10 **revienta** ("Se esperaba un True|False
  para la propiedad rollForward"): el manifiesto `MiniMercado/dotnet-tools.json`
  usa `"rollForward": "latestPatch"` y el SDK 6 lo acepta. Restaurar con
  `~/.dotnet/dotnet tool restore --tool-manifest MiniMercado/dotnet-tools.json`.
  `dotnet ef` si funciona desde el proyecto con cualquiera de los dos muxers.

## Objetivo actual

**Ciclo 2026-10-02: la app arranca en local y el esquema es confiable.**

Dos objetivos ya cumplidos:

1. **La app corre y se ve en el navegador** (base local SQL Server 2022 en Docker
   + runtime 6 en `~/.dotnet`).
2. **La base se construye sola y existe una barrera que lo verifica.** Hay 60
   migraciones, el parche T-SQL de `Program.cs` se elimino, y
   `tools/verificar-esquema.sh` detecta cualquier divergencia entre el modelo y
   las migraciones (comparando contra la base real), mas el error humano de
   cambiar el modelo sin generar la migración y de generarla sin commitearla.
   **Desde 2026-10-05 la barrera es completa: sale con exit 0** (antes solo
   comparable parcialmente por P-030, ya cerrado). **Lo que falta es que corra
   sola: hoy es un script y se corre a mano.**

Sigue pendiente llevarla a un despliegue con Docker Compose. Lo que falta para
eso: no hay `Dockerfile`, no hay `compose.yaml`, no hay `appsettings.Production.json`
y no hay healthchecks. (Existe un `.env` local desde 2026-10-06, gitignored:
es el que usa F5, no un `.env` de despliegue.)

Pendiente de decisión del TEAM LEAD (no resuelto, no inventado): motor de base de
datos, framework destino, dónde vive la app, historial de git, alcance del
refactor.

Resuelto por el TEAM LEAD el 2026-10-05: **qué pasa al borrar un rubro con
productos** -> `Producto.RubroId` es nullable (D-013, cierra P-030).

## Plan activo

Ciclo 2026-10-02. Ver el camino propuesto en `01_ESTADO.md` §6.2 (10 pasos) y las
decisiones bloqueantes en §6.4.

**Hecho y verificado en ejecucion:**

- Los tres casos URGENTE de `01_ESTADO.md` §5 (P-001, P-004, P-005).
- P-002 y P-003: las 7 columnas de suscripcion ahora las crea la migracion
  `20261002143249_AddSuscripcionColumnsToTenant` y el parche T-SQL **se elimino**.
  Verificado borrando la base y reconstruyendola solo con migraciones.
- P-012: `dotnet-ef` arreglado (6.0.36 + SDK 6.0.428).
- P-022, P-023 y P-024, aparecidos al hacer lo anterior.
- P-029: faltaba la columna `EgresosCaja.Anulado` y el modulo Egresos daba 500 en
  todos los entornos. Cerrado con `20261002163400_AgregaAnuladoA_EgresosCaja`.
- P-025 (parte 1): creada `tools/SchemaCheck`.
- P-030: cerrado con **D-013** (rubro opcional) + migracion
  `20261005101937_RubroIdOpcional`. **60 migraciones.** El diagnostico original
  de P-030 era erroneo: la causa era el `[Required]` de `Producto.RubroId`, no el
  `DeleteBehavior`, y `RubroController` nunca borraba en hard. Ademas aparecio
  **P-031**: el snapshot adelantado hace que `migrations add` genere un `Up()`
  vacio.
- P-026: cerrado. Migraciones y seed salen del arranque por defecto y quedan
  detras de `Bootstrap:ApplyMigrations` / `Bootstrap:Seed` (activas solo en
  Development). El problema real era doble: el seed tambien escribia en cada
  boot y tenia su propia carrera, con un indice unico que explota antes que las
  migraciones.
- P-007: cerrado 2026-10-06. Los **85 `GetTenantId().Value` de 14 archivos** ahora
  pasan por el helper `TenantServiceExtensions.TryGetTenantId` (extension de
  `ITenantService`) que falla cerrado: sin claim, la accion devuelve
  `Unauthorized()` en vez de un 500, `ProveedorExists` devuelve `false` (NotFound) y
  `AuditoriaService.Registrar` lanza una excepcion con mensaje claro que cita
  P-007. Verificado con **94 tests verdes** (88 previos + 6 nuevos en
  `SinTenantIdTests`, incluido un guardia de fuente que falla si
  `GetTenantId().Value` reaparece en Controllers/ o Services/) y humo de arranque
  con 11 rutas anonimas: 200/302, 0 excepciones en el log.

**Pendiente, en este orden:**

1. **P-017 (tests) — EMPEZADO 2026-10-05.** Ya no es lo pendiente: existe
   `tests/MiniMercado.Tests` con **88 tests verdes** contra SQL Server real (D-015) sobre
   los tres flujos de dinero (venta, cobro de suscripcion, cierre de caja) y el dashboard
   financiero. Escribirlos destapo 6 bugs reales, todos corregidos y cubiertos — ver
   `03_PROBLEMAS.md`. Sigue abierta la parte de las vistas de caja.

   **P-033 cerrado despues** (atomicidad de venta y pago, con transaccion explicita y tests
   que inyectan el fallo con un `SaveChangesInterceptor`).
   **P-032 cerrado despues** (la formula del saldo de caja estaba en tres archivos; ahora hay
   una sola definicion en `Caja.CalcularSaldoEsperado`, con un guardia que impide volver a
   duplicarla).
   **P-034 cerrado sin tocar codigo** (D-017): el usuario decidio no sumar IVA ni descuentos.
   Con esto, los tres flujos de dinero que P-017 dejo abiertos quedan cerrados.
2. **P-025 cerrado 2026-10-06.** La barrera corre en CI
   (`.github/workflows/ci.yml`): build + tests + `verificar-esquema.sh` en cada
   push a `main` y en cada PR, con SQL Server en service container. Lo unico que
   queda es ver pasar la primera corrida en GitHub (ocurre en el proximo push;
   si falla ahi, es bug del workflow, no de la barrera).
3. **P-028.** Health checks. Hoy una base atrasada solo se detecta en el log de
   arranque; con un endpoint de salud se detecta en runtime.
4. **P-027.** Framework EOL desde nov-2024: depende de D-009.

Nota: **P-006 cerrado 2026-10-05 (D-014).** `AdminGlobal` es back-office; solo gestiona
tenants y consulta el catálogo global. **Y P-026 ya no bloquea al `Dockerfile`**, asi que el
orden de arriba no esta atado a ninguna decision pendiente.

Prioridad acordada: seguir con los arreglos que **no dependen de ninguna decision
de infraestructura** antes de escribir el Dockerfile o el compose.

**Estado del entorno (2026-10-06):**

- Contenedor `minimercado-sql` (SQL Server 2022) **levantado** en `127.0.0.1:1433`,
  sin volumen Docker: si se borra el contenedor, se pierde la base (por ahora es
  descartable y no hay backups; decision tomada por el TEAM LEAD).
- `MiniMercadoDB` es la unica base de usuario, con **60 migraciones** aplicadas.
- Los runtimes **6.0.36** estan en `/usr/lib/dotnet` (ademas del 10.0.12 y del
  SDK 10.0.112): `dotnet run` y `dotnet test` funcionan con el dotnet del sistema.
- **94 tests verdes** en ~3 min, sin hacks de entorno (2026-10-06).
- **CI en GitHub Actions** (`.github/workflows/ci.yml`): build + tests +
  `verificar-esquema.sh` contra SQL Server en service container; secret
  `MSSQL_SA_PASSWORD` seteado vía `gh` (2026-10-06). `global.json` en la raiz
  fija el SDK (P-014).
- F5 en VS Code funciona via `.vscode/launch.json` + `.env` (ver "Entorno local").
- **Si hay `sqlcmd`:** en el contenedor, en `/opt/mssql-tools18/bin/sqlcmd` (v18,
  ya probado). Ejemplo:
  `docker exec -it minimercado-sql /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U sa -P '<password>' -C -d MiniMercadoDB`.
  En la maquina host no hay `sqlcmd` ni `pyodbc`; no instalar tooling nuevo sin
  necesidad.

## Fuentes de verdad

`Contexto Minimercado/` contiene la documentación funcional y de producto que ya
existe. No se duplica en `AI/`.

- `Contexto Minimercado/Contexto MiniMercado.txt` — arquitectura, entidades, reglas de oro del producto.
- `Contexto Minimercado/Prompt de diseño dark.txt` — sistema de diseño UI oscuro.
- `Contexto Minimercado/Prompt exportacion pdf.txt` —/features de exportacion PDF.
- `Contexto Minimercado/Modulo etiquetas y productos.txt` — módulo de etiquetas.
- `Contexto Minimercado/Guia para cambiar novedades.txt` — cómo editar el mensaje de novedades.
- `Contexto Minimercado/Servicio de suscripcion contexto.txt` — módulo de suscripción y bloqueo.
- `Contexto Minimercado/promtAdminPagos.txt` — pantallas de cobro del admin global.
- `Contexto Minimercado/Prompt Proveedores.txt` — módulo de proveedores.
- `Contexto Minimercado/Restricciones para prompt.txt` — restricciones al escribir prompts.
- `MiniMercado/FAQ_EGRESOS.md` — estados y reglas del módulo de egresos.
- `migracion_donweb.sql` — script SQL de migración masiva de productos.

No existe `DOCS/`. La documentación de producto vive en `Contexto Minimercado/`
y no se replica acá.

## Archivos de esta memoria

Leer en este orden segun la tarea.

- 01_ESTADO.md - estado real por modulo, casos urgentes y plan Docker. Solo el TEAM LEAD confirma CONSOLIDADO.
- 02_DECISIONES.md - decisiones tecnicas preservadas (IDs D-XXX).
- 03_PROBLEMAS.md - problemas conocidos (IDs P-XXX) y resueltos.

## Rutas de lectura por tarea

Toda tarea: leer este archivo y 01_ESTADO.md.

- Tarea de implementacion: leer ademas 02_DECISIONES.md y las secciones de
  03_PROBLEMAS.md que afecten al modulo.
- Tarea de arquitectura o tecnologia: leer 02_DECISIONES.md completo.
- Tarea de diagnostico o bug: leer 03_PROBLEMAS.md completo y 01_ESTADO.md
  del modulo.
- Tarea de producto o UX: consultar `Contexto Minimercado/`, en particular
  `Prompt de diseño dark.txt`.
- Tarea de datos/migraciones: leer `03_PROBLEMAS.md` P-025 a P-030, y la seccion
  5 de este archivo (verificar el esquema con `SchemaCheck`). El caso P-002 ya
  esta cerrado y su trampa ya esta documentada en la seccion 4.

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