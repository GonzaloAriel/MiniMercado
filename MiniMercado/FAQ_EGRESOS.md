# FAQ: Módulo de Egresos - MiniMercado SaaS

Este documento detalla el funcionamiento técnico y las reglas de negocio aplicadas al módulo de Egresos.

## 1. ¿Cómo funciona EgresoCaja?
`EgresoCaja` representa una salida de dinero de la caja del comercio. Cada registro almacena:
- **Monto:** El valor monetario de la salida.
- **Categoría:** Un tipo predefinido (Enum `CategoriaEgreso`).
- **Beneficiario:** Nombre de la persona o entidad que recibe el dinero.
- **Descripción/Observaciones:** Detalles del gasto.
- **TenantId:** Identificador del comercio para aislamiento de datos.

## 2. Relación con Caja
Los egresos pueden estar vinculados a una `Caja` específica mediante `CajaId`. 
- Si hay una caja abierta para el usuario al momento de crear el egreso, se vincula automáticamente.
- Esto permite que el arqueo de caja reste estos montos del saldo esperado.
- **Importante:** Un egreso puede existir sin `CajaId` si se registra fuera de un turno de caja (aunque la UI actual intenta vincularlo siempre).

## 3. Relación con Proveedor
Un egreso puede estar vinculado opcionalmente a un `Proveedor`.
- Si se selecciona un proveedor, este actúa como el beneficiario principal.
- No afecta las cuentas corrientes de proveedores (módulo no implementado actualmente), es meramente informativo y para trazabilidad.

## 4. ¿Cómo se calcula el cierre de caja?
El saldo esperado al cerrar una caja se calcula dinámicamente:
`Saldo Esperado = Monto Inicial + Total Ventas - Total Egresos`

**Nota Técnica:** No almacenamos el total de egresos en la tabla `Caja`. Se realiza un `SUM(Monto)` de los `EgresosCaja` vinculados a ese `CajaId` en tiempo real.

## 5. ¿Cómo agregar nuevas categorías?
Las categorías están definidas en el enum `MiniMercado.Models.CategoriaEgreso`. Para agregar una nueva:
1. Agregue el valor al enum en `CategoriaEgreso.cs`.
2. El sistema lo incluirá automáticamente en los dropdowns de Filtros y Creación.

## 6. ¿Cómo funciona el filtro por Tenant?
El sistema utiliza un filtro global en Entity Framework Core definido en `MiniMercadoContext.cs`:
```csharp
modelBuilder.Entity<EgresoCaja>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
```
Esto garantiza que ningún comercio vea los egresos de otro. Además, el `EgresosController` refuerza esto obteniendo el `TenantId` del `ITenantService`.

## 7. Buenas Prácticas
- **No modificar Producto.Stock:** Los egresos son financieros. Si un egreso representa una compra de mercadería, el stock debe ingresarse mediante el módulo de Inventario/Lotes para mantener la trazabilidad.
- **Uso de IQueryable:** Para los filtros, siempre use `IQueryable` para que la base de datos realice el filtrado y solo traiga los registros necesarios (paginados).
- **Anulaciones:** Actualmente los egresos no se "anulan", se registran como movimientos definitivos. Si se requiere reversar, se debe registrar un ingreso (no implementado en este módulo) o eliminar el registro (si los permisos lo permiten).
