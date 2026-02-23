using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "VentaDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "MovimientosStock",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TenantId",
                table: "Ventas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_TenantId",
                table: "VentaDetalles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TenantId",
                table: "Productos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_TenantId",
                table: "MovimientosStock",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Tenants_TenantId",
                table: "MovimientosStock",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Tenants_TenantId",
                table: "Productos",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VentaDetalles_Tenants_TenantId",
                table: "VentaDetalles",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Tenants_TenantId",
                table: "Ventas",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Tenants_TenantId",
                table: "MovimientosStock");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Tenants_TenantId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_VentaDetalles_Tenants_TenantId",
                table: "VentaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Tenants_TenantId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_TenantId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_VentaDetalles_TenantId",
                table: "VentaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_Productos_TenantId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_TenantId",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "VentaDetalles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MovimientosStock");
        }
    }
}
