using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class AgregarCobrosTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo agregamos las columnas a Tenants
            migrationBuilder.AddColumn<bool>(
                name: "Bloqueado",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioMensual",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Creamos la nueva tabla de Cobros
            migrationBuilder.CreateTable(
                name: "CobrosTenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CobrosTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CobrosTenants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CobrosTenants_TenantId",
                table: "CobrosTenants",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CobrosTenants");

            migrationBuilder.DropColumn(
                name: "Bloqueado",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PrecioMensual",
                table: "Tenants");
        }
    }
}
