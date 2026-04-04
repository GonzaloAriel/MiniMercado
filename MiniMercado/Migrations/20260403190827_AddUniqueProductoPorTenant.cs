using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class AddUniqueProductoPorTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_TenantId",
                table: "Productos");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TenantId_CodigoBarra",
                table: "Productos",
                columns: new[] { "TenantId", "CodigoBarra" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_TenantId_CodigoBarra",
                table: "Productos");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TenantId",
                table: "Productos",
                column: "TenantId");
        }
    }
}
