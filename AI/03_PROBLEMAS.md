# 03_PROBLEMAS.md

FORMATO: 1.0

## Reglas

Guardar problemas conocidos que no estan resueltos.

Un problema resuelto se mueve a RESUELTOS con causa y solucion: es patron
a no repetir.

Severidad: ALTA = puede causar perdida de datos o acceso indebido.
MEDIA = deuda o riesgo acotado.

## Problemas

- **P-002 (RESUELTO 2026-10-02) — Divergencia entre modelo y migraciones.**
  7 columnas de `Tenant` (`Plan`, `FechaInicioSuscripcion`, `FechaProximoPago`,
  `EstadoSuscripcion`, `MesesPagados`, `SaldoAFavorMeses`,
  `UltimoPagoRegistrado`) estaban en el modelo y en el snapshot de EF, pero
  ninguna de las 57 migraciones las creaba. Por eso existia el parche crudo en
  `Program.cs`.
  **Causa real:** el snapshot habia generado esas columnas a mano (o desde un
  modelo mas nuevo) sin generar la migracion correspondiente. El snapshot es la
  foto del modelo, no el historial: si el modelo cambia y no se corre
  `dotnet ef migrations add`, el snapshot miente.
  **Solucion:** se quito del snapshot **solo** esas 7 propiedades, se genero
  `20261002143249_AddSuscripcionColumnsToTenant` y EF volvio a escribirlas en el
  snapshot. Verificado: base borrada y reconstruida desde cero con
  `Database.Migrate()` -> 58 migraciones, 28 tablas y las 7 columnas presentes,
  sin parche T-SQL.
  **Cuidado al migrar bases existentes:** si una base ya tiene las columnas
  (porque corrio el parche viejo), la migracion falla con columna duplicada.
  Hay que recrear la base o dropear las columnas antes de aplicar.
  Patron a no repetir: si el modelo cambio, generar migracion en el mismo
  commit.

- **P-003 (RESUELTO 2026-10-02) — Parche T-SQL no portable + orden de ejecucion
  incorrecto.** `Program.cs` ejecutaba un `ExecuteSqlRaw` idempotente con
  `sys.columns` y `OBJECT_ID` antes de `Migrate()`, sin `try/catch`. Si fallaba,
  la app moria sin registro.
  **Correccion 2026-10-02 (verificada):** la afirmacion anterior de que "el
  T-SQL no corre en Linux ni en SQL Server sobre Linux" era **falsa**. El T-SQL
  es especifico de **SQL Server**, no de Windows: se ejecuto sin problema
  contra SQL Server 2022 en contenedor Linux. El bloqueo real de portabilidad es
  **PostgreSQL**, no Linux.
  **Bug real encontrado al arrancar contra una base nueva:** el parche corria
  **antes** de `Migrate()`, asi que en una base recien creada la tabla `Tenants`
  todavia no existia, `OBJECT_ID('Tenants')` devolvia NULL, el `IF NOT EXISTS`
  daba true y el `ALTER TABLE` reventaba con *"Cannot find the object Tenants"*.
  La app moria en el primer arranque de cualquier entorno nuevo, o sea
  exactamente el caso de Docker. Corregido el orden: ahora va `Migrate()` y
  despues el parche.
  **Solucion definitiva:** el parche se **elimino por completo**. Con las 7
  columnas ahora en una migracion real (P-002) ya no hacia falta. Arranque
  verificado 3 veces sobre base creada desde cero: 0 parches, 0 fallos.
  El limite de portabilidad que queda es el proveedor SQL Server del modelo.

- **P-006 (ALTA, RESUELTO 2026-10-05) — Autorizacion demasiado permisiva.**
  Cerrado con **D-014**. Los 7 controllers de tenant que tenian `[Authorize]`
  pelado ahora llevan `Roles = "Admin,Caja"`: `VentaController`, `CajaController`,
  `EgresosController`, `FinanzasController`, `AlertasController`,
  `NotificacionesController` y `ProductosApiController`.
  **El navbar tambien se alineo**, porque era la otra mitad de la contradiccion:
  `_Layout.cshtml` le mostraba POS, Egresos y Finanzas a `AdminGlobal`, mientras
  `Index.cshtml:684` ya lo excluia. Ahora el menu muestra exactamente lo que el
  servidor autoriza.
  **`ProductosController` era un caso aparte**, no un `[Authorize]` pelado mas:
  tenia `AdminGlobal` en 21 atributos de metodo, pero **el codigo nunca usa el
  rol** y solo 2 de esas acciones no tocan `TenantId`. Las demas (15) llaman
  `GetTenantId().Value`, o sea que para `AdminGlobal` **daban 500**. Decision
  (D-014): `AdminGlobal` conserva solo `BuscarProductoBase` y `GetProductoBase`
  (consulta del catalogo global), y pierde las otras 20.
  **Tambien se borro `AdminTempController.cs`**, que P-006 listaba como uno de
  los 10 controllers sin roles pero estaba **vacio y ni siquiera heredaba de
  `Controller`**: no ruteaba nada. Era codigo muerto.
  **Verificado en ejecucion**, 3 roles x 15 endpoints, base nueva: la matriz
  queda exacta y **no queda ningun 500**. Detalle en D-014.
  **Lo que quedo abierto:** el rol `Caja` sigue pudiendo anular ventas, por
  decision explicita del TEAM LEAD. Y las denegaciones devuelven **302** (el
  scheme de Identity re-lanza al login) en vez de **403**; el acceso esta
  denegado igual, pero un `403` seria lo correcto para un usuario ya autenticado.

- **P-007 (RESUELTO 2026-10-06) — `GetTenantId().Value` sin verificar.** 86 ocurrencias
  en 15 archivos desenvuelven el claim con `.Value` sin comprobar que exista.
  Lanza excepcion para anonimos y para `AdminGlobal`, que por diseño no tiene
  `TenantId`: el usuario ve un 500 en vez de una pantalla coherente.
  **Actualizacion 2026-10-05:** con P-006 cerrado los **85 `.Value` restantes** (14
  archivos) quedaron todos en controllers `Admin`/`Caja`, que siempre tienen
  `TenantId`, asi que el sintoma dejo de ser alcanzable y el problema paso a ser
  defensa en profundidad.
  **Solucion 2026-10-06:** helper unico `Services/TenantServiceExtensions.cs`, una
  extension de `ITenantService` que devuelve `false` cuando no hay claim
  (`TryGetTenantId`). Los 85 sitios pasaron a la forma
  `if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();`
  (el patron de referencia de `EtiquetasController`). Tres casos especiales:
  `ProveedoresController.ProveedorExists` (metodo privado bool) falla cerrado con
  `false` -> NotFound; `ClienteController.Create` subio la guardia arriba del
  initializer `cliente.TenantId = ...`; y `AuditoriaService.Registrar` (service,
  no puede devolver IActionResult) lanza `InvalidOperationException` con mensaje que
  cita P-007, porque no se registra auditoria sin tenant.
  **Verificado:** `dotnet build` 0 errores; **94 tests verdes** (88 previos + 6
  nuevos en `tests/MiniMercado.Tests/SinTenantIdTests.cs`: 4 actions con
  `TenantServiceStub(null)` devuelven `UnauthorizedResult` en vez de lanzar,
  `AuditoriaService` lanza el mensaje claro, y un guardia de fuente —mismo patron
  que el de P-032— que falla si `GetTenantId().Value` vuelve a aparecer en
  Controllers/ o Services/); humo de arranque de la app con 11 rutas anonimas
  (200/302, cero excepciones en el log).
  Patron a no repetir: **nunca desenvolver un claim con `.Value` a secas**: el claim
  puede no existir, y el fallo tiene que ser una denegacion explicita, no un 500.
  La solucion de fondo (middleware que resuelve el tenant una vez + base que deniega
  por omision) sigue pendiente de **D-012**.

- **P-008 (MEDIA) — Falta antiforgery en POST.** Sin
  `[ValidateAntiForgeryToken]`: `UsuariosController` (3 POST, crear/editar/borrar
  usuarios), `AdminGlobalUsuariosController` (reset de password), `RubroController`
  (6 de 7), `ProductosController.Importar` y `ConfirmarImportacion`,
  `ClienteController.CrearRapido`, `ProductosApiController.CrearDesdeBase`,
  `NotificacionesController.MarcarLeida`.
  Afecta a: CSRF.

- **P-009 (MEDIA) — GETs que modifican datos.** `ClienteController.Desactivar`,
  `RubroController.Delete`, `Desactivar` y `DeleteConfirmed` no tienen
  `[HttpPost]` ni antiforgery: mutan estado con un GET, alcanzable desde un
  `<img src>` en una pagina externa.
  Afecta a: integridad de datos via CSRF.

- **P-011 (MEDIA) — Sin logging.** `ILogger<T>` no esta inyectado en ningun
  archivo. Cuando algo falla en produccion no hay traza. El parche de
  `Program.cs` corre sin `try/catch`.
  Afecta a: diagnostico, produccion.

- **P-012 (RESUELTO 2026-10-02) — `dotnet-ef` incompatible.** `dotnet-tools.json`
  pedia `dotnet-ef 10.0.5` con `rollForward: false` contra EF Core 6.0.36, asi
  que cualquier `dotnet ef` fallaba y no se podia generar ni aplicar migracion.
  **Solucion:** SDK 6.0.428 instalado en `~/.dotnet` (sin sudo) y tool fijada en
  `dotnet-ef 6.0.36` con `rollForward: latestPatch`. `dotnet tool restore` y
  `dotnet ef --version` funcionan.
  **Trampa:** `dotnet ef` exige una connection string valida en configuracion
  aunque nunca se conecte a la base. Sin ella falla con
  *"Value cannot be null. (Parameter 'connectionString')"*. Se resuelve pasando
  la connection string por variable de entorno, igual que la app:
  `ConnectionStrings__MiniMercadoDB=... ~/.dotnet/dotnet ef migrations add ...`.

- **P-022 (ALTA, NUEVO 2026-10-02) — El monto cobrado nunca se guardaba.**
  `SuscripcionService.RegistrarPagoAsync` recibia `tenantId, meses, plan,
  observacion` y guardaba en `CobroTenant.Monto` el valor
  `tenant.PrecioMensual * meses`. El `Monto` que el administrador escribia en
  `AdminGlobal/RegistrarPago` **se descartaba**: no viajaba al servicio.
  `PrecioMensual` solo es una sugerencia y vale 0 para los tenants sembrados,
  asi que **todos los cobros quedaban en 0.00** y el KPI "Recaudacion Total"
  (`AdminGlobalController.Dashboard`, que suma `CobroTenant.Monto`) mostraba
  $0 para siempre.
  **Solucion:** `RegistrarPagoAsync` ahora recibe `decimal monto` y guarda ese
  valor real; el controller pasa `model.Monto`. Verificado: un cobro de 45000
  queda como 45000.00 y la recaudacion del dashboard muestra $45,000.
  Patron a no repetir: el ViewModel y la firma del servicio tienen que coincidir;
  agregar un campo al formulario y no pasarlo es un bug silencioso.

- **P-023 (MEDIA, NUEVO 2026-10-02) — Segunda divergencia modelo/migraciones en
  `ImpresionEtiqueta`.** Al generar la migracion de suscripcion aparecieron de
  paso cambios que no tenian nada que ver: `IX_ImpresionesEtiquetas_TenantId` y
  `FK_ImpresionesEtiquetas_Tenants_TenantId`. El modelo declara la navegacion
  `Tenant` con borrado en cascada, pero la migracion original de la tabla creo
  solo el FK a `Productos` y su indice. O sea habia **dos** divergencias, no una.
  Efecto: borrados en cascada silenciosamente inexistentes y consultas por
  tenant sin indice.
  **Solucion:** quedaron incluidas en la misma migracion y verificadas en base
  nueva. Patron a no repetir: el mismo que P-002.

- **P-024 (MEDIA, NUEVO 2026-10-02) — `PrecioSugerido` acepta input del
  cliente.** En `RegistrarPagoViewModel` era una propiedad publica sin
  restringir, aunque su unico uso legitimo es prellenar el formulario. No era
  explotable (el POST no la leia) pero era una bomba de tiempo: si alguien la
  conectaba al calculo del cobro, el precio pasaria a controlarse desde el
  cliente.
  **Solucion:** `[BindNever]` en `PrecioSugerido` y en `FechaVencimiento` (esta
  ultima es ademas un campo muerto: existe en el ViewModel pero **no hay input**
  en `RegistrarPago.cshtml`, por lo que nunca se envia). Verificado: un POST con
  `PrecioSugerido=999999999` no altera el monto registrado.

- **P-013 (RESUELTO 2026-10-06) — Runtime .NET 6 no instalado en el sistema.**
  En la maquina hay SDK 10.0.112 y runtime ASP.NET Core 10.0.12 en
  `/usr/lib/dotnet`, pero no habia ningun runtime 6: el proyecto **compilaba**
  en Linux, pero el binario `net6.0` no podia arrancar con el muxer del sistema.
  *Workaround 2026-10-02:* runtimes 6.0.36 en `~/.dotnet` (sin sudo) y muxer
  explicito para app y tests (`~/.dotnet/dotnet ...`, `DOTNET_ROOT=...`).
  **Solucion 2026-10-06 (verificado):** los runtimes 6.0.36
  (`Microsoft.NETCore.App` y `Microsoft.AspNetCore.App`) ahora estan en
  `/usr/lib/dotnet`, instalados con `dotnet-install.sh --runtime ... --version 6.0.36
  --install-dir /usr/lib/dotnet --no-path` (con sudo). El SDK 10 sigue siendo el
  unico SDK y ahora compila **y ejecuta** `net6.0`. Verificado: `dotnet run`
  arranca, `dotnet test` da **94/94** sin `DOTNET_ROOT`, y VS Code debuggea con
  F5 (`.vscode/launch.json` + `.env`). `~/.dotnet` queda solo para el SDK 6 y
  `dotnet ef`.
  **Sigue pendiente:** D-009 (framework destino) — instalar el runtime 6 no
  decide a donde migra el proyecto.
  Afecta a: ejecucion local.

- **P-014 (RESUELTO 2026-10-06) — Sin `global.json`.** El SDK no estaba pinneado:
  cada build podia usar una version distinta.
  **Solucion:** `global.json` en la raiz, `version: 6.0.428` con
  `rollForward: latestMajor` (exige >= 6.0.428 pero permite saltar al SDK 10 del
  sistema; un pin estricto a 6 romperia los builds locales). En CI instala 6.0.x.
  Afecta a: reproducibilidad de build.

- **P-015 (MEDIA) — Logica de negocio en los controllers.** 20 de 26 controllers
  usan el `DbContext` directo; solo 6 usan services. `ProductosController` tiene
  905 lineas con CRUD, import Excel y logica de stock inline. `VentaController`
  (529) tiene la logica de venta, pago y sincronizacion de stock dentro del
  controller. La arquitectura declarada dice "Services = logica de negocio".
  No es un bug: es deuda que sube de costo cada vez que se toca el codigo.
  `ProveedorService` es el modelo de como deberian verse los demas.
  Afecta a: mantenibilidad de productos, ventas.

- **P-016 (MEDIA) — `FromSqlRaw` repetido 19 veces.** Todas parametrizadas (`{0}`),
  sin inyeccion SQL. Pero repiten a mano la lista de columnas de `Ventas`: agregar
  una columna a `Venta` rompe las 19 en runtime. Ademas el raw SQL omite el filtro
  global de tenant y la mayoria lo repone con `WHERE TenantId = {0}`, lo cual es
  fragile. Si se migra a PostgreSQL hay que reescribirlas.
  Afecta a: ventas, portabilidad.

- **P-017 (MEDIA) — Sin tests. -> EN PROGRESO, red inicial el 2026-10-05.**
  Ya existe `tests/MiniMercado.Tests` con **88 tests verdes** sobre SQL Server real
  (ver D-015). Cubren los tres flujos de dinero: cobro de suscripcion (18), cierre de
  caja (12) y venta del POS (24), mas la infraestructura de la fixture.
  Al escribirlos aparecieron 5 bugs reales, todos corregidos y cubiertos:
  NRE en `VentaController.Create` con producto inexistente o ajeno; venta con total
  descuadrado por linea de cantidad negativa; primera venta del dia sin `CajaId` en modo
  automatico; suscripcion vencida por horas (comparaba instantes y no dias calendario);
  y la regresion de P-022 (monto cobrado que no se guardaba) protegida.
  Siendo ese flag inverso aparecio un sexto bug, fuera del alcance original de P-017:
  `FinanzasController.Dashboard` usaba `v.Activa` para "ventas del día y del mes", o sea que
  sumaba las ventas **anuladas** como ingresos, con lo que el balance del día salia al reves.
  Corregido y cubierto con 5 tests mas.
  Sigue sin cubrir: las vistas de caja y el resto de controllers.
  Afecta a: todos los modulos con calculo.

- **P-018 (MEDIA) — Notificaciones y alertas oportunistas.** `AlertasService` y las
  notificaciones se disparan dentro de cada request, no hay `BackgroundService` ni
  `IHostedService`. El aviso depende de que alguien navegue.
  Afecta a: notificaciones, alertas.

- **P-019 (BAJA) — Tabla huerfana.** La migracion `20260315212409_AgregaMLToken`
  crea `MercadoLibreTokens` pero no hay entidad ni `DbSet`: resto de una
  integracion de MercadoLibre abandonada.
  Afecta a: nada funcional.

- **P-020 (BAJA) — Residuo de Windows en el csproj.**
  `<Folder Include="wwwroot\Images\" />` con backslash. No rompe Linux, pero denota
  residuo de la maquina original.
  Afecta a: nada funcional.

- **P-021 (BAJA) — Paquete instalado y sin usar.**
  `Microsoft.EntityFrameworkCore.Sqlite 6.0.35` esta en el csproj pero la app usa
  SQL Server. Se puede desinstalar.

- **P-029 (ALTA, RESUELTO 2026-10-02) — `EgresosCaja.Anulado` no existia en la
  base: el modulo Egresos daba 500 en todos los entornos.** Cuarta instancia de
  la misma clase que P-002 y P-023. El modelo declara
  `EgresoCaja.Anulado` (`bool`, default `false`), el snapshot tambien, pero
  **ninguna migracion creaba la columna**. Como ademas se usa en el filtro
  global de `EgresosCaja` (`MiniMercadoContext.cs:64`) y en
  `EgresosController` y `Views/Egresos/Index.cshtml`, toda consulta a esa tabla
  reventaba.
  **Verificado en ejecucion:** `GET /Egresos` devolvia **500** con
  *"Invalid column name 'Anulado'"*, con el error originado en el pipeline de
  consultas de EF al aplicar el filtro global.
  **Solucion:** migracion `20261002163400_AgregaAnuladoA_EgresosCaja`
  (`bit NOT NULL DEFAULT 0`). Verificado: `GET /Egresos` paso a **200** y el log
  quedo sin errores de columna. Suma **59 migraciones**.
  Como se detecto: por la herramienta `tools/SchemaCheck` (ver P-025), en su
  primera ejecucion. Nadie lo habia visto en meses.

- **P-030 (RESUELTO 2026-10-05) — `Producto.RubroId` era NOT NULL en el modelo
  con un FK `SetNull`: DDL imposible y bug de validacion.** Cerrado con **D-013**
  (rubro opcional). El diagnostico original de este problema estaba **equivocado
  en su causa**; ac va lo que se verifico de verdad.
  **Causa real:** `Producto.cs` declaraba `[Required]` sobre `int? RubroId` y la
  navegacion `public Rubro Rubro` (no-nullable). El `[Required]` es lo que hacia
  que EF marcara la columna NOT NULL: `ProveedorId` y `ProductoBaseId`, tambien
  `int?`, salen **opcionales** en el snapshot precisamente porque no llevan
  `[Required]` y su navegacion es `?`. No era el `DeleteBehavior.SetNull`.
  **La contradiccion estaba en el historial de migraciones, no solo en el
  modelo:** `20260321231347_FixRubroIdNullable` (2026-03-21) hizo la columna
  nullable, y **9 dias despues `20260322122848_AddActivoEnRubro` la revirto a
  `NOT NULL` sin tocar el FK**. Desde entonces migrations y modelo estaban
  ambos mal, pero de forma distinta.
  **Bug de validacion que nobody habia registrado:** `Views/Productos/Edit.cshtml`
  ofrecia la opcion "-- Sin rubro --" y el `[Required]` la rechazaba, asi que no
  se podia guardar un producto sin rubro aunque la base lo aceptara y todas las
  vistas ya lo trataran como opcional (`producto.Rubro?.Nombre ?? "-"`,
  `v.Producto.Rubro != null ? ... : "Sin Rubro"`).
  **Lo que P-030 afirmaba y era falso:** que `RubroController.Delete` reventaba
  con 500 al borrar un rubro con productos. **`RubroController` nunca borra en
  hard:** `Delete` (GET) solo muestra la confirmacion, y `DeleteConfirmed` y
  `DesactivarAjax` hacen `Activo = false`. No hay ningun `.Remove()` ni
  `DELETE FROM Rubros` en todo el proyecto. El 500 solo existia a nivel de SQL
  crudo. (Nota: `DeleteConfirmed` no tiene antiforgery, eso si es P-008.)
  **Solucion:** quitar el `[Required]`, pasar la navegacion a `Rubro?`, corregir
  el asterisco mentiroso de `Create.cshtml` y generar la migracion
  `20261005101937_RubroIdOpcional` (`AlterColumn` a nullable).
  **Verificado en ejecucion** (base de descarte `MiniMercadoDB_p030`, dropeada al
  terminar): 60 migraciones aplicadas desde cero; `SchemaCheck` **exit 0** por
  primera vez (sin deriva **y** sin el aviso de comparacion parcial, que era lo
  que P-030 bloqueaba); alta de rubro, alta de producto con rubro, edicion
  dejandolo "Sin rubro" -> **302** y persistido como NULL (`Detalle` muestra
  "Sin Clasificar"), y round-trip de vuelta a un rubro -> "Bebidas".
  `/Productos`, `/Productos/Detalle/1`, `/AnalisisComercial/ProductosMasVendidos`,
  `/AnalisisComercial/ProductosSinRotacion` y `/Rubro`: **200**, sin errores en el
  log.
  Patron a no repetir: **un `[Required]` sobre un `int?` cambia el DDL sin que
  nadie lo note**, y una vista puede ofrecer una opcion que el modelo prohibe.
  Antes de tocar una FK o una nulabilidad, correr `SchemaCheck`.

- **P-031 (NUEVO 2026-10-05) — El snapshot puede quedar desfasado respecto del
  modelo y `migrations add` genera una migracion VACIA.** Se documento en
  `00_MEMORIA.md` §4 como trampa de P-002 y se topo en vivo otra vez al cerrar
  P-030. Al corregir el modelo, **sincronizar el snapshot a mano** (quitar el
  `.IsRequired()` de `RubroId`) produce que EF no tenga nada que diffear contra
  el historial y genere `Up()` vacio: `SchemaCheck` seguia reportando la deriva
  y la migracion no arreglaba nada.
  **El orden correcto es el inverso:** dejar el snapshot como esta en las
  migraciones (con `NOT NULL`), y dejar que `migrations add` genere el
  `AlterColumn` y **EF reescriba el snapshot solo**. Si el snapshot va adelantado,
  la migracion sale vacia; y una migracion vacia applied no arregla nada.
  Corolario: **siempre mirar el `Up()` generado antes de dar por buena una
  migracion.** Un `Up()` vacio significa snapshot adelantado.

- **P-026 (RESUELTO 2026-10-05) — `Database.Migrate()` en cada arranque no escala a
  mas de una instancia.** Cerrado con banderas explicitas en `Program.cs`.
  **El problema era mas grande de lo que documentaba.** `Program.cs` no tenia una
  cosa que tocar en el arranque sino **dos**: `db.Database.Migrate()` y
  `SeedData.Inicializar(...)`. Quitar solo `Migrate()` **no cerraba P-026**: el
  seed sigue corriendo en cada boot de cada replica y tiene tres guardas
  `Any()` / `RoleExistsAsync()` que son **TOCTOU**:
  | Guarda | Carrera | Como falla |
  |---|---|---|
  | `RoleExistsAsync` → `CreateAsync` | dos replicas ven "no existe" | las dos insertan en `AspNetRoles` → `DbUpdateException`, una muere |
  | `db.Tenants.Any()` | dos crean el tenant demo | dos tenants demo |
  | `db.ProductosBase.Any()` | dos insertan el catalogo | `IX_ProductoBase_CodigoBarras` es **UNIQUE** → duplicate key, una muere |
  Ademas ninguna de las dos estaba en `try/catch`: si la base no respondia, la
  app moria sin registro (P-011).
  **Solucion:** `Bootstrap:ApplyMigrations` y `Bootstrap:Seed`. Default:
  **activas en Development** (el flujo local "levantá y anda" no cambia) e
  **inactivas en cualquier otro entorno**, donde pasan a ser un paso del deploy
  (`.env` / variable de entorno: `Bootstrap__ApplyMigrations=true`). Ambos pasos
  van con `try/catch` + `LogCritical` y re-lanzan: si se piden explicitamente, si
  fallan no tiene sentido seguir.
  **Extra que quedo:** cuando las migraciones estan desactivadas se consulta
  `GetPendingMigrationsAsync()` y se avisa por log **cuantas faltan y cuales son
  las 3 primeras**. Sin eso, un deploy que se olvida de migrar se ve como "la app
  levanta y despues da 500". Trampa: **si la base no existe, EF no tira
  excepcion**, responde con todas las migraciones pendientes (las 60), asi que
  el detalle se recorta a proposito.
  **Verificado en ejecucion** (4 escenarios, bases de descarte dropeadas al
  terminar):
  - Development, base inexistente, sin banderas → crea la base, aplica las **60**
    migraciones, siembra, login `admin@demo.com` funciona, `/Productos` **200**.
  - Production, base atrasada 1 migracion, sin banderas → **no migra**, seed
    apagado, warning `La base esta atrasada: 1 migracion(es)... 20261005101937_RubroIdOpcional`.
  - Production, banderas `true` → aplica `RubroIdOpcional`, siembra, login y
    `/Productos` **200**.
  - **Dos replicas en paralelo** (`:5276` y `:5277`, Production, banderas
    apagadas, base al dia) → las dos levantan, 0 excepciones, ambas sirven
    (`GET /` 200). Ese es el escenario que P-026 describe, ya sin la carrera.
  Patron a no repetir: **`Database.Migrate()` en el arranque es un supuesto de
  una sola instancia.** Y ojo con el seed: es tan parte del arranque como las
  migraciones, y es el que tiene el indice unico que explota antes.
  Follow-up: cuando exista CI (P-025 parte 2), agregar que corra `SchemaCheck`
  y las migraciones; y P-028 (health checks) es lo que va a detectar una base
  atrasada en runtime, que hoy solo aparece en el log de arranque.

- **Refuerzo a P-006 / P-007 (filtros globales y relaciones).** EF 6 avisa al
  generar migraciones: *"Entity 'Producto' has a global query filter defined and
  is the required end of a relationship with the entity 'ProductoProveedor'"*.
  Un filtro global combinado con una relacion obligatoria hace que los datos
  puedan desaparecer segun como se navegue. Vale revisarlo junto con la
  solucion de fondo de P-006/P-007.

## Deuda que va a doler (2026-10-02)

Hallazgos de una revision de arquitectura previa a Dockerizar. Varios ya estan
cubiertos por problemas de mas arriba; aca se dejan **priorizados** y se aclara
cual es la solucion de fondo, no el parche puntual.

- **P-025 (ALTA, RESUELTO 2026-10-06) — Nada detecta la deriva entre el modelo y las migraciones.**
  Los pasos 1 y 2 estan cerrados (`tools/SchemaCheck` y `tools/verificar-esquema.sh`, este ultimo con
  salida 4 explicita cuando encuentra migraciones sin commitear). **No esta 100% cerrado**: falta el
  paso 3 y que la barrera corra sola en CI; hoy se verifica a mano.
  Esto **no es un bug, es el proceso que produjo dos bugs**: P-002 (las 7
  columnas) y P-023 (el indice y el FK de `ImpresionEtiqueta`). Los dos estaban
  en el modelo y en el snapshot pero ninguna migracion los creaba.
  Lo que lo hace peligroso: el parche T-SQL de `Program.cs` **enmascaraba** la
  falla en desarrollo. En la maquina del autor todo andaba perfecto y la app se
  rompia en cualquier otro entorno. Es el peor modo de falla posible: un bug
  que el flujo de trabajo local no puede detectar.
  **Solucion de fondo (CI, no disciplina):**
  1. Levantar una base vacia desde cero con las migraciones y compararla contra
     el modelo.
  2. Fallar el build si `dotnet ef migrations add` genera un archivo que no esta
     commiteado.
  3. Regla de equipo: un cambio de esquema nunca se pushea sin su migracion en el
     mismo commit.
  **Prioridad maxima:** es lo primero que hay que construir.

  **Avance 2026-10-02 (paso 1 hecho).** Se creo `tools/SchemaCheck`, que
  levanta dos bases de descarte y las compara:
  - A) creada con `Database.Migrate()` (o sea, solo con migraciones);
  - B) creada con `Database.GenerateCreateScript()` (o sea, desde el modelo).
  Compara columnas (con tipo y nulabilidad), indices y claves foraneas
  incluyendo su accion de borrado, y devuelve codigo de salida 1 si hay deriva.
  ```
  ConnectionStrings__MiniMercadoDB='...' dotnet run --project tools/SchemaCheck
  ```
  **Dos trampas que tuvo que esquivar (patrones a no repetir):**
  - El chequeo barato no sirve. `dotnet ef migrations add` **no** detecta este
    problema: si el snapshot ya esta adelantado respecto de las migraciones, EF
    no genera nada y cree que todo esta bien. Hay que contrastar contra la base
    real.
  - En EF Core 6, `Database.EnsureCreated()` **aplica las migraciones** si el
    contexto las tiene, asi que no sirve para obtener el esquema del modelo: hay
    que usar `GenerateCreateScript()`.
**Resultado en la primera corrida:** encontro P-029 (columna `Anulado`
    inexistente, modulo Egresos con 500) y P-030. Es decir, la barrera que se
    pidio encontrar dos bugs vivos en minutos.
  **Actualizacion 2026-10-05: la barrera es por primera vez completa.** Al cerrar
    P-030 (D-013) el modelo se volvio implementable en SQL Server y `SchemaCheck`
    salio con **exit 0: sin deriva y sin el aviso de comparacion parcial**. La
    parcialidad la causaba P-030, asi que **P-025 ya no tiene excusas para
    quedar incompleta en su parte util**.
  **Avance 2026-10-05 (paso 2 hecho).** Se creo `tools/verificar-esquema.sh`,
    que corre los tres chequeos de la solucion de fondo y sale con un codigo
    distinto por fallo:
    - **A)** deriva modelo/migraciones (`SchemaCheck`). Es el unico que ve el
      snapshot adelantado a las migraciones, que es la trampa documentada mas
      arriba. Necesita SQL Server.
    - **B)** el modelo esta adelantado al snapshot: genera una migracion de
      prueba y mira si tiene algo dentro. **No necesita base de datos**, asi que
      es barato y sirve como pre-commit.
    - **C)** hay migraciones sin commitear: cierra el "genere la migracion y me
      olvide del `git add`".
    ```
    ConnectionStrings__MiniMercadoDB='...' tools/verificar-esquema.sh
    ```
    **Por que los tres y no uno:** A y B se complementan en las dos direcciones.
    A no ve el error humano si el snapshot ya tiene el cambio; B no ve la deriva
    real si el snapshot va adelantado (genera un `Up()` vacio y pasa). C no ve el
    modelo, solo el working tree.
    **Verificado con los tres fallos inyectados a mano:** una columna al modelo
    sin migracion la reportan A (con el detalle exacto de la columna) y B (2
    operaciones); el estado real del repo lo reporto C, porque la migracion de
    P-030 todavia no esta commiteada. La migracion de prueba que genera B se
    revierte siempre, incluso si el script se corta a la mitad.
  **Cierre 2026-10-06 (paso 3 hecho: la barrera corre sola).** Se creo
    `.github/workflows/ci.yml` (GitHub Actions; el remoto ya era GitHub): en cada
    push a `main` y en cada PR corre build + los 94 tests + `verificar-esquema.sh`
    contra un SQL Server 2022 en service container. La "regla de equipo" dejo de
    ser texto: un cambio de esquema sin su migracion en el mismo commit rompe el
    pipeline (lo atrapan A o B, segun donde este la deriva). El connection string
    llega de `secrets.MSSQL_SA_PASSWORD` (seteado con `gh secret set` el
    2026-10-06); la contrasena jamas se commitea. De paso se agrego `global.json`
    (cierra P-014). **Primera corrida en GitHub VERDE el 2026-10-06 (5m39s)**,
    al tercer intento: el 1ro fallo por el `rollForward` string del manifiesto
    (el SDK 10 lo rechaza y en CI el muxer elige el 10; fix: boolean), el 2do
    porque `SchemaCheck` abria conexion a la base de referencia `MiniMercadoDB`,
    que en CI no existe (fix: admin en `master`, igual que en `EliminarBase`).
    Ambos fixes probados en local simulando CI: muxer del sistema y connection
    string apuntando a una base inexistente. **Lo unico que CI no tapa:** el
    chequeo C (migraciones sin commitear) es vacio en un checkout limpio, su
    valor es local/pre-commit; el que impide el push peligroso es A+B, y los dos
    corren.

- **P-026 — RESUELTO 2026-10-05.** Ver la ficha en la seccion de Problemas.
  Dos correcciones a lo que se creia saber: **no era un cambio de una linea**
  (habia que sacar tambien el seed del arranque, que tiene su propia carrera), y
  **el aviso de EF sobre `Producto` + `ProductoProveedor` que aparecia al
  compilar es preexistente y sigue sin corregir** (ver el refuerzo a P-006/P-007
  mas abajo, no es un efecto de este cambio).

- **P-027 (MEDIA) — Framework fuera de soporte (.NET 6 y EF Core 6, EOL desde
  nov-2024).** En una maquina de desarrollo da igual. Se vuelve deuda el dia que
  se publique: se hereda una imagen base sin parches de seguridad y sin soporte.
  **Relacionado:** D-009 (framework destino). P-013 quedo resuelto el
  2026-10-06 (runtimes 6 en `/usr/lib/dotnet`): lo que falta ahora es la
  decision, no el entorno. No hace falta hacerlo ya, pero la decision no se puede
  seguir postergando indefinidamente: cada mes que pasa encarece el salto.

- **P-028 (MEDIA) — Sin health checks ni verificacion de dependencias.** Si la
  base no esta disponible, `Database.Migrate()` revienta y la app muere. En
  Docker eso se ve como un contenedor en crash loop sin diagnostico. Deberia
  haber un endpoint de salud que reporte si la base responde, y distinguish en el
  log entre "no pude arrancar" y "arranque correcto".
  **Relacionado:** P-011 (sin logging).

- **Refuerzo a P-006 y P-007 (autorizacion).** Son el limite de seguridad del
  producto, no solo deuda: aca esta la diferencia entre un bug y una fuga entre
  tenants. Pero **no conviene parchear 86 `.Value` a mano**: la solucion de fondo
  es resolver el tenant una sola vez en el middleware, guardarlo en el contexto, y
  que los controllers deriven de una base que **falla cerrada** (por omision
  deniega). Hoy la autorizacion es opt-in, y lo que no se marca queda abierto.

- **Refuerzo a P-016 (sin tests).** Subir de prioridad: los flujos de **dinero**
  (venta, cobro de suscripcion, cierre de caja) no tienen ninguna proteccion
  contra regresiones. P-022 es la prueba: el monto cobrado nunca se guardaba y
  nadie lo noto porque no hay ningun test que lo cubra. Hoy todo se verifico a
  mano con `curl` contra la app corriendo.

- **Refuerzo a P-001 / D-011 (historial publico).** Rotar las claves no alcanza:
  el commit `a5d20ba` sigue disponible para cualquiera que clone el repo. Es una
  liability permanente; hay que decidir antes de que el repo crezca y la
  reescritura de historial sea mas costosa.


### Hallazgos de P-017: como quedaron

- **~~P-032 (MEDIA) — La formula del saldo esperado de caja esta triplicada.~~ -> RESUELTO 2026-10-05.**
  `MontoInicial + TotalVentas - TotalEgresos` estaba escrita en `CajaController.cs:96`,
  `Views/Caja/CerrarCaja.cshtml:34` y `Views/Caja/ResumenCierre.cshtml:37`. El controller
  hacia bien las cuentas, pero las vistas tenian su propia copia: si una se actualizaba y
  las otras no, el usuario veia un "Esperado" distinto del `Diferencia` que quedo guardado
  y no habia forma de saber cual de los dos numeros era el correcto.
  Ahora hay una sola definicion, `Caja.CalcularSaldoEsperado(totalEgresos)`, y el controller
  y las dos vistas la usan. Los egresos se siguen pasando por parametro porque viven en otra
  tabla.
  Cubierto por 11 tests: la aritmetica (incluidos centavos y saldo negativo), un test que
  ata la `Diferencia` persistida con la formula que las vistas muestran, y un guardia que
  falla si la formula vuelve a aparecer escrita a mano en una vista —probado revirtiendo el
  arreglo, que lo pone rojo.
  Afecta a: arqueo de caja, confianza del usuario en el sistema.

- **~~P-033 (ALTA) — Venta y pago no son atomicos.~~ -> RESUELTO 2026-10-05.**
  `VentaController.Create` guardaba en tres `SaveChanges` sin transaccion: la venta, el
  pago y la auditoria. Entre la primera y la segunda no habia nada que las atara, asi que
  un corte de conexion o un veto de la base dejaba la venta registrada con el stock ya
  descontado y el total ya acreditado a la caja, pero **sin su pago**: el cajon
  receiptiaba la plata y el sistema no la veia, sin forma de saber que habia pasado.
  Ahora todo el POST (lotes, stock, movimientos, venta, pago y auditoria) va dentro de una
  transaccion explicita que se confirma solo al final; los caminos de validacion que
  hacen `return View()` tambien revierten.
  Cubierto por 7 tests nuevos en `AtomicidadVentaTests`, que inyectan el fallo con un
  `SaveChangesInterceptor`. Al desactivar la transaccion, tres de ellos se ponen rojos.
  Afecta a: ventas, conciliacion de caja.

- **~~P-034 (BAJA) — No hay IVA ni descuento en el modelo.~~ -> DESCARTADO POR DECISION 2026-10-05.**
  `Venta`, `VentaDetalle` y `Producto` no tienen ningun campo de impuesto ni de descuento, y el
  formulario del POS no los pide. El total es `Precio x Cantidad` y `Precio` es el precio final.
  El usuario decidio **no sumar IVA ni descuentos al alcance** (D-017), asi que esto no es
  trabajo pendiente sino una decision consciente. No hizo falta tocar codigo: el comportamiento
  pedido ya era el que habia.
  Queda anotado en D-017 que, si algun dia se agrega, el punto mas delicado no es la formula sino
  el **cierre de caja**: hoy compara `Total` contra `MontoReal`, y si el total incluye impuesto el
  arqueo tiene que distinguir la plata que entra de la que lo incluye.
  Afecta a: facturacion.


## RESUELTOS

- **P-001 (ALTA) — Contraseñas de administrador en el repositorio.** Causa:
  `Data/SeedData.cs` creaba tres usuarios con contraseña fija en el fuente
  (`global@admin.com` AdminGlobal, `admin@demo.com` Admin, `caja@demo.com`
  Caja) y el seed corre en cada arranque, no solo la primera vez. Los valores
  literales **no se repiten acá**: siguen en el historial publico (commit
  `a5d20ba`). Solucion: las contraseñas se leen de `IConfiguration` con las
  claves `Seed:PasswordAdminGlobal`, `Seed:PasswordAdminDemo` y
  `Seed:PasswordCajaDemo` (o sea, variables de entorno). Si no estan
  definidas, el usuario **no se crea** y se emite un `LogWarning` con el nombre
  de la clave. No hay default en ningun `appsettings*.json` porque
  `appsettings.Development.json` esta versionado. Verificado 2026-10-02 en
  ejecucion: creo los 3 usuarios con sus roles y el login funciona.
  **Lo que queda abierto:** rotar las claves de bases ya creadas con las
  viejas y D-011 (historial de git).
  Patron a no repetir: **ningun secreto en un archivo versionado**; si falta la
  variable de entorno, no se crea el usuario y se avisa por log.

- **P-004 (ALTA) — Fuga entre tenants: `ImpresionEtiqueta` sin filtro global.**
  Causa: la entidad implementaba `ITenantEntity` pero no tenia
  `HasQueryFilter` (era el unico de 16 sin filtro), y
  `EtiquetasController.Reimprimir` consultaba por `Id` sin filtrar. Solucion:
  agregar el `HasQueryFilter` y usar el patron correcto del resto del
  controller. No requiere migracion (un query filter no cambia el modelo).
  Verificado 2026-10-02 en ejecucion: con un segundo tenant que tenia una
  `ImpresionesEtiquetas` propia, `Reimprimir` desde la sesion del Admin del
  tenant 1 devuelve **404**.
  Patron a no repetir: **agregar una entidad `ITenantEntity` obliga a agregar
  su filtro en la misma linea** (ver D-001).

- **P-005 (ALTA) — `DashboardController` sin `[Authorize]`.** Causa: sin
  `[Authorize]`, `AuditoriaCajas` era accesible sin iniciar sesion y el unico
  control era `GetTenantId().Value`, que para anonimos y para `AdminGlobal`
  lanza excepcion (500 en vez de 403). Solucion:
  `[Authorize(Roles = "Admin")]` a nivel de clase (el rol previsto, segun el
  bloque `@if (User.IsInRole("Admin"))` de `Views/Home/Index.cshtml`) y chequeo
  explicito del claim con `Forbid()`. Verificado 2026-10-02 en ejecucion:
  anonimo **302** al login, Admin autenticado **200**.

- **P-010 (MEDIA) — `appsettings.json` era JSON invalido y usaba autenticacion
  Windows.** Causa: el archivo tenia una linea `//Server=...` suelta fuera del
  objeto (el provider la ignoraba en silencio) y apuntaba a `.\SQLEXPRESS` con
  `Trusted_Connection=True`, que no existe fuera de Windows. Solucion: se
  elimino el bloque `ConnectionStrings` y **la connection string pasa por
  variable de entorno** `ConnectionStrings__MiniMercadoDB`. Verificado
  2026-10-02: JSON valido (parser) y la app arranco con la connection string de
  entorno. Patron: **connection strings solo por variable de entorno**.

- **P-010 follow-up — `dotnet run` sin connection string.** El follow-up viejo
  ("no arranca con `dotnet run`") era P-013, **resuelto 2026-10-06**: ahora
  `dotnet run` arranca. Lo que sigue siendo cierto: `launchSettings.json` no
  incluye la connection string y en Development la app muere al hacer seed sin
  ella. En terminal hay que exportar `ConnectionStrings__MiniMercadoDB` a mano;
  en VS Code, F5 la lee de `.env`.