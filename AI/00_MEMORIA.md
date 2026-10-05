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

SDK instalado en la máquina: **10.0.112**. Runtime ASP.NET Core instalado:
**10.0.12**. No hay runtime 6 → `dotnet run` falla hasta migrar de framework.

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

## Entorno local (verificado 2026-10-02)

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

**2. Runtime .NET 6** instalado con `dotnet-install.sh` en `~/.dotnet`
(sin sudo, reversible con `rm -rf ~/.dotnet`). El SDK del sistema es el 10 y
**no** puede ejecutar un binario `net6.0`.

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

`dotnet run` **no sirve**: usa el muxer del sistema, que no tiene runtime 6.

Las tres variables `Seed__*` son obligatorias: sin ellas el seed no crea
ningun usuario (ver P-001 en `03_PROBLEMAS.md`).

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
eso: no hay `Dockerfile`, no hay `compose.yaml`, no hay `.env`, no hay
`appsettings.Production.json` y no hay healthchecks.

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

**Pendiente, en este orden:**

1. **P-007 (defensa en profundidad, ya no alcanzable).** Quedan **85 `.Value`** en 14
   archivos. Con P-006 cerrado ninguno es alcanzable, porque `AdminGlobal` es el único rol
   sin `TenantId` y ya no entra a ningún controller que los tenga. El cierre de verdad es un
   helper único que falle cerrado, pero son 85 cambios mecánicos y ahora ya hay red de tests
   (P-017), asi que este es el siguiente paso natural.
2. **P-017 (tests) — EMPEZADO 2026-10-05.** Ya no es lo pendiente: existe
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
3. **P-025 (lo que queda).** Correr `tools/verificar-esquema.sh` solo, sin que
   nadie se acuerde: eso es elegir plataforma de CI y el repo no tiene ninguna.
   El script es agnostico a proposito, asi que el dia es pegarlo en un workflow.
   El paso 3 de P-025 (regla de equipo) tambien sigue siendo solo una regla.
4. **P-028.** Health checks. Hoy una base atrasada solo se detecta en el log de
   arranque; con un endpoint de salud se detecta en runtime.
5. **P-027.** Framework EOL desde nov-2024: depende de D-009.

Nota: **P-006 cerrado 2026-10-05 (D-014).** `AdminGlobal` es back-office; solo gestiona
tenants y consulta el catálogo global. **Y P-026 ya no bloquea al `Dockerfile`**, asi que el
orden de arriba no esta atado a ninguna decision pendiente.

Prioridad acordada: seguir con los arreglos que **no dependen de ninguna decision
de infraestructura** antes de escribir el Dockerfile o el compose.

**Estado del entorno al cerrar la sesion (2026-10-02, fin del dia):**

- Contenedor `minimercado-sql` (SQL Server 2022) **levantado** en `127.0.0.1:1433`.
- La app **esta parada**: se levanta con el comando de la seccion 3.
- `MiniMercadoDB` tiene **60 migraciones** aplicadas, incluida
  `20261005101937_RubroIdOpcional`.
- `SchemaCheck` da **exit 0: "Sin deriva"**, sin aviso de comparacion parcial
  (estado alcanzado el 2026-10-05 al cerrar P-030). Rebuildar la herramienta
  antes de correrla.
- No hay cliente `sqlcmd` en la maquina ni en el contenedor, ni `pyodbc` en
  Python. Para tocar la base a mano, usar `SchemaCheck` o escribir SQL desde un
  controller; no instalar tooling nuevo sin necesidad.

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