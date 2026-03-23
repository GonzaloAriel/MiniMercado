using Microsoft.EntityFrameworkCore.Migrations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class FixRubroIdNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar FK existente
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Rubros_RubroId",
                table: "Productos");

            // Hacer RubroId realmente nullable en la BD
            migrationBuilder.AlterColumn<int>(
                name: "RubroId",
                table: "Productos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Recrear FK con nullable
            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Rubros_RubroId",
                table: "Productos",
                column: "RubroId",
                principalTable: "Rubros",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Rubros_RubroId",
                table: "Productos");

            migrationBuilder.AlterColumn<int>(
                name: "RubroId",
                table: "Productos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true,
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Rubros_RubroId",
                table: "Productos",
                column: "RubroId",
                principalTable: "Rubros",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}