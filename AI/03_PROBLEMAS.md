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

- **P-006 (ALTA) — Autorizacion demasiado permisiva.** Sin roles en
  `CajaController`, `EgresosController`, `FinanzasController`,
  `AlertasController`, `VentaController`, `AyudaController`, `CuentaController`,
  `NotificacionesController`, `ProductosApiController`, `AdminTempController`.
  Efecto: `AdminGlobal` opera el POS y abre/cierra caja; el rol `Caja` ve el
  dashboard financiero y las alertas; `VentaAdminController` con
  `Roles = "Admin,Caja"` permite **anular ventas** a un cajero.
  Afecta a: autorizacion en 10 controllers.

- **P-007 (ALTA) — `GetTenantId().Value` sin verificar.** 86 ocurrencias en 15
  archivos desenvuelven el claim con `.Value` sin comprobar que exista.
  Lanza excepcion para anonimos y para `AdminGlobal`, que por diseño no tiene
  `TenantId`. Combinado con P-006, un `AdminGlobal` que entra a esas pantallas
  obtiene 500 en vez de una pantalla coherente.
  El patron correcto ya existe en `EtiquetasController`
  (`if (!tenantIdNullable.HasValue) return Unauthorized();`) y sirve de referencia.
  Afecta a: 15 archivos.

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

- **P-014 (ALTA, NUEVO 2026-10-02) — El monto cobrado nunca se guardaba.**
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

- **P-015 (MEDIA, NUEVO 2026-10-02) — Segunda divergencia modelo/migraciones en
  `ImpresionEtiqueta`.** Al generar la migracion de suscripcion aparecieron de
  paso cambios que no tenian nada que ver: `IX_ImpresionesEtiquetas_TenantId` y
  `FK_ImpresionesEtiquetas_Tenants_TenantId`. El modelo declara la navegacion
  `Tenant` con borrado en cascada, pero la migracion original de la tabla creo
  solo el FK a `Productos` y su indice. O sea habia **dos** divergencias, no una.
  Efecto: borrados en cascada silenciosamente inexistentes y consultas por
  tenant sin indice.
  **Solucion:** quedaron incluidas en la misma migracion y verificadas en base
  nueva. Patron a no repetir: el mismo que P-002.

- **P-016 (MEDIA, NUEVO 2026-10-02) — `PrecioSugerido` acepta input del
  cliente.** En `RegistrarPagoViewModel` era una propiedad publica sin
  restringir, aunque su unico uso legitimo es prellenar el formulario. No era
  explotable (el POST no la leia) pero era una bomba de tiempo: si alguien la
  conectaba al calculo del cobro, el precio pasaria a controlarse desde el
  cliente.
  **Solucion:** `[BindNever]` en `PrecioSugerido` y en `FechaVencimiento` (esta
  ultima es ademas un campo muerto: existe en el ViewModel pero **no hay input**
  en `RegistrarPago.cshtml`, por lo que nunca se envia). Verificado: un POST con
  `PrecioSugerido=999999999` no altera el monto registrado.

- **P-013 (MEDIA) — Runtime .NET 6 no instalado en el sistema.** En la maquina
  hay SDK 10.0.112 y runtime ASP.NET Core 10.0.12 en `/usr/lib/dotnet`, pero
  ningun runtime 6. El proyecto **compila** en Linux, pero el binario `net6.0`
  no puede arrancar con el muxer del sistema.
  **Workaround 2026-10-02 (verificado):** se instalaron los runtimes 6.0.36
  (`Microsoft.NETCore.App` y `Microsoft.AspNetCore.App`) con `dotnet-install.sh`
  en `~/.dotnet`, **sin sudo y sin tocar el sistema**. Se compila con el SDK del
  sistema y se ejecuta con el muxer de `~/.dotnet`:
  `~/.dotnet/dotnet bin/Debug/net6.0/MiniMercado.dll`.
  `dotnet run` pelado sigue sin servir, porque usa el muxer del sistema.
  Reversible con `rm -rf ~/.dotnet`.
  **Pendiente:** D-009 (framework destino). El workaround no decide D-009, solo
  permite ver la app mientras tanto.
  Afecta a: ejecucion local.

- **P-014 (MEDIA) — Sin `global.json`.** El SDK no esta pinneado: cada build puede
  usar una version distinta. Acompana a P-013.
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

- **P-017 (MEDIA) — Sin tests.** Ningun proyecto de tests. Un refactor de stock o
  del calculo de caja no tiene red de seguridad. `FAQ_EGRESOS.md` sugiere que ya
  hubo problemas con el calculo de egresos.
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

- **P-010 follow-up — la app no arranca con `dotnet run`.** No es un problema de
  codigo sino del entorno: ver P-013 (workaround con `~/.dotnet`).