using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class AddProductoBaseAndRelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductoBaseId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductosBase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodigoBarras = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrecioReferencia = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosBase", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_FechaVencimiento",
                table: "Tenants",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_ProductoBaseId",
                table: "Productos",
                column: "ProductoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CobrosTenants_FechaPago",
                table: "CobrosTenants",
                column: "FechaPago");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosBase_CodigoBarras",
                table: "ProductosBase",
                column: "CodigoBarras",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_ProductosBase_ProductoBaseId",
                table: "Productos",
                column: "ProductoBaseId",
                principalTable: "ProductosBase",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_ProductosBase_ProductoBaseId",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "ProductosBase");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_FechaVencimiento",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Productos_ProductoBaseId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_CobrosTenants_FechaPago",
                table: "CobrosTenants");

            migrationBuilder.DropColumn(
                name: "ProductoBaseId",
                table: "Productos");
        }
    }
}
