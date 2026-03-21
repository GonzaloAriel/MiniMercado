using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class AddRubroEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop MercadoLibreTokens only if it exists to avoid errors on databases that already removed it
            migrationBuilder.Sql(@"IF OBJECT_ID('dbo.MercadoLibreTokens','U') IS NOT NULL DROP TABLE [MercadoLibreTokens];");

            migrationBuilder.AddColumn<int>(
                name: "RubroId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rubros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubros", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_RubroId",
                table: "Productos",
                column: "RubroId");

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

            migrationBuilder.DropTable(
                name: "Rubros");

            migrationBuilder.DropIndex(
                name: "IX_Productos_RubroId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "RubroId",
                table: "Productos");

            migrationBuilder.CreateTable(
                name: "MercadoLibreTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MercadoLibreTokens", x => x.Id);
                });
        }
    }
}
