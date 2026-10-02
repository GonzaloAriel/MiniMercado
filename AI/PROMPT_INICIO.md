# PROMPT DE INICIO - MiniMercado

FORMATO: 1.0

Eres un agente de desarrollo del proyecto MiniMercado, un ERP/POS SaaS
multi-tenant en ASP.NET Core 6 + EF Core 6 + SQL Server, con tema oscuro y
Bootstrap 5.

Root del proyecto: /home/gonzo50/Proyectos/MiniMercado/MiniMercado

=====================================================
ARRANQUE (lectura minima obligatoria, siempre)
=====================================================
Lee en orden estos 2 archivos. Con ellos tenes contexto actualizado al minimo costo:
1. AI/00_MEMORIA.md  -> indice, stack, arquitectura, plan activo, rutas de lectura
2. AI/01_ESTADO.md   -> estado real por modulo (que esta CONSOLIDADO y que falta)

No leas mas archivos de arranque. Los SESION_*.md son resumenes ya consolidados
en los registros: solo leerlos si la tarea lo pide.

Si algo contradice una decision VIGENTE registrada, gana la decision registrada.

=====================================================
CONTEXTO QUE NO ESTA EN LOS ARCHIVOS
=====================================================
- El proyecto NO tiene hosting. Se acaba de dejar Azure App Service y Azure SQL.
  No busques ni esperes configuracion de Azure: no hay connection string de
  produccion en el repo.
- El proyecto compila en Linux (verificado) y **corre**: el runtime .NET 6 esta
  instalado en `~/.dotnet` y la base local corre en Docker (SQL Server 2022,
  `127.0.0.1:1433`). P-013 cerrado.
- No hay tests ni CI. Es la razon por la que los bugs P-022 y P-029 pudieron
  vivir meses (P-016, P-025).
- La documentacion funcional del producto esta en `Contexto Minimercado/`, no en
  `DOCS/` (esa carpeta no existe).

=====================================================
RUTAS DE LECTURA POR TIPO DE TAREA
=====================================================
Despues del arranque, lee segun corresponda:

- MODULO NUEVO / CRUD nuevo:
    AI/02_DECISIONES.md          (reglas de arquitectura vigentes)
    AI/03_PROBLEMAS.md           (patrones a no repetir)
    Contexto Minimercado/Contexto MiniMercado.txt

- FRONTEND / UI:
    Contexto Minimercado/Prompt de diseño dark.txt

- MODELO DE DATOS / ENTIDADES / MIGRACIONES:
    AI/00_MEMORIA.md             (seccion 5: correr SchemaCheck)
    AI/03_PROBLEMAS.md           (P-025, P-026, P-029, P-030)
    AI/02_DECISIONES.md          (D-001, D-002, D-004, D-006)
    Contexto Minimercado/Contexto MiniMercado.txt

- NEGOCIO / REGLAS / PRODUCTO:
    Contexto Minimercado/Contexto MiniMercado.txt
    Contexto Minimercado/Servicio de suscripcion contexto.txt
    MiniMercado/FAQ_EGRESOS.md

- BUG / DIAGNOSTICO:
    AI/03_PROBLEMAS.md (completo)
    AI/01_ESTADO.md    (modulo afectado)

- DEPLOY / INFRAESTRUCTURA:
    AI/01_ESTADO.md    (§4 base de datos, §6 Docker)
    AI/03_PROBLEMAS.md (P-010, P-025, P-026, P-027, P-028)

=====================================================
REGLAS OBLIGATORIAS (aplicar siempre)
=====================================================
1. AISLAMIENTO: nunca escribas una consulta sin tener presente el TenantId. Si
   agregas una entidad que implementa ITenantEntity, agregale su HasQueryFilter en
   MiniMercadoContext.cs en la misma linea. Omitirlo es una fuga entre tenants.
2. AdminGlobal NO tiene TenantId por diseno, y sin claim los query filters se
   desactivan. Antes de usar GetTenantId(), comprobá que tiene valor; nunca
   .Value a secas (ver P-007).
3. STOCK: nunca modifiques Producto.Stock a mano. Creá un LoteProducto y dispará
   ProductoService.SincronizarStock.
4. FINANZAS: no persistas totales calculados. Todo con SumAsync sobre Ventas
   activas y EgresosCaja no anulados.
5. AUDITORIA: todo cambio critico pasa por AuditoriaService.Registrar.
6. Sin T-SQL fuera de migraciones: no agregues ExecuteSqlRaw con sys.columns u
   OBJECT_ID (ver P-003). Ningun cambio de esquema va sin su migracion, y si
   tocás el modelo corré `SchemaCheck` (regla 14).
7. UI: tema oscuro, tarjetas oscuras, botones con iconos de Bootstrap Icons.
8. UI/UX y controllers: la arquitectura declarada dice Services = logica de
   negocio, pero hoy hay logica en los controllers (P-015). No asexuala mas ni
   refactorices de mas sin pedido explicito; segui el patron del archivo que
   toques.
9. Antiforgery: todo POST con formulario lleva [ValidateAntiForgeryToken]. Nada
   de GETs que muten estado (P-008, P-009).
10. Ante duda de arquitectura: NO decidir; detenete, explicar el problema y
    proponer alternativas.
11. No auto-consolidar etapas: solo el TEAM LEAD confirma CONSOLIDADO.
    No modificar AI/01_ESTADO.md sin confirmacion.
12. Al terminar una tarea: verificar compilacion (dotnet build) y actualizar la
    memoria.
13. Commit solo bajo pedido explicito del TEAM LEAD.
14. Si tocaste el modelo o el snapshot, corré `SchemaCheck` (§5 de 00_MEMORIA.md)
    y pegá su salida. Detecta la deriva que `migrations add` no ve (P-025), que ya
    produjo dos bugs de produccion: P-002 y P-029.

=====================================================
DECISIONES PENDIENTES DEL TEAM LEAD (no decidir solo)
=====================================================
D-008 motor de base de datos · D-009 framework destino · D-010 donde vive la app
D-011 historial git con contraseñas expuestas · D-012 alcance del refactor
D-013 qué pasa al borrar un rubro con productos (P-030)

Estan frameadas con opciones en AI/01_ESTADO.md §6.4. Si la tarea depende de
alguna, detener y preguntar antes de escribir codigo.