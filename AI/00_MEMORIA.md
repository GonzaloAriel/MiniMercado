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
| ORM | EF Core 6.0.36, `Migrations/` con 57 migraciones |
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
mano**: la crea `Database.Migrate()` en el primer arranque, junto con las 57
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

**5. Verificar que migraciones y modelo no divergan (P-025):**

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

## Objetivo actual

**Ciclo 2026-10-02: la app arranca en local y el esquema es confiable.**

Dos objetivos ya cumplidos:

1. **La app corre y se ve en el navegador** (base local SQL Server 2022 en Docker
   + runtime 6 en `~/.dotnet`).
2. **La base se construye sola y existe una barrera que lo verifica.** Hay 59
   migraciones, el parche T-SQL de `Program.cs` se elimino, y la herramienta
   `tools/SchemaCheck` detecta cualquier divergencia entre el modelo y las
   migraciones.

Sigue pendiente llevarla a un despliegue con Docker Compose. Lo que falta para
eso: no hay `Dockerfile`, no hay `compose.yaml`, no hay `.env`, no hay
`appsettings.Production.json` y no hay healthchecks.

Pendiente de decisión del TEAM LEAD (no resuelto, no inventado): motor de base de
datos, framework destino, dónde vive la app, historial de git, alcance del
refactor, y la semantica del borrado de rubros (P-030, ver `02_DECISIONES.md`).

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

**Pendiente, en este orden:**

1. **P-030 — requiere decision del TEAM LEAD.** Borrar un rubro con productos
   falla: el FK es `SET_NULL` sobre `Producto.RubroId`, que el modelo declara
   obligatorio. Verificado en ejecucion. Son tres caminos (bloquear con mensaje,
   `Restrict`, o hacer `RubroId` nullable) y todos cambian comportamiento visible.
   Ademas, mientras no se resuelva, el DDL del modelo no se puede crear entero y
   la comparacion de `SchemaCheck` queda **parcial**.
2. **P-026.** Sacar `Database.Migrate()` del arranque. **Tiene que resolverse
   antes de escribir el `Dockerfile`**, porque con varias replicas EF Core 6 no
   tiene lock de migraciones y dos pods arrancando juntos se pelean por la misma
   migracion. Hoy es un cambio de una linea.
3. **P-025 (parte 2).** Conectar `SchemaCheck` a CI (hoy se corre a mano).
4. **P-028.** Health checks.
5. **P-016.** Tests de humo sobre los flujos de dinero (venta, cobro de
   suscripcion, cierre de caja). Subieron de prioridad porque P-022 y P-029
  demostraron que no hay red de seguridad.
6. **P-027.** Framework EOL desde nov-2024: depende de D-009.

Prioridad acordada: seguir con los arreglos que **no dependen de ninguna decision
de infraestructura** antes de escribir el Dockerfile o el compose.

**Estado del entorno al cerrar la sesion (2026-10-02, fin del dia):**

- Contenedor `minimercado-sql` (SQL Server 2022) **levantado** en `127.0.0.1:1433`.
- La app **esta parada**: se levanta con el comando de la seccion 3.
- `MiniMercadoDB` tiene **59 migraciones** aplicadas, incluida
  `20261002163400_AgregaAnuladoA_EgresosCaja`.
- `SchemaCheck` da **"Sin deriva"** pero sale con codigo 1: la deriva esta
  resuelta, y lo que queda es la contradiccion del modelo de P-030, que hoy
  vuelve **parcial** la comparacion.
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