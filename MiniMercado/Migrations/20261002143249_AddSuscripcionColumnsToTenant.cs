using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class AddSuscripcionColumnsToTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoSuscripcion",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicioSuscripcion",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaProximoPago",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MesesPagados",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Mensual");

            migrationBuilder.AddColumn<int>(
                name: "SaldoAFavorMeses",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoPagoRegistrado",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpresionesEtiquetas_TenantId",
                table: "ImpresionesEtiquetas",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImpresionesEtiquetas_Tenants_TenantId",
                table: "ImpresionesEtiquetas",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImpresionesEtiquetas_Tenants_TenantId",
                table: "ImpresionesEtiquetas");

            migrationBuilder.DropIndex(
                name: "IX_ImpresionesEtiquetas_TenantId",
                table: "ImpresionesEtiquetas");

            migrationBuilder.DropColumn(
                name: "EstadoSuscripcion",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FechaInicioSuscripcion",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FechaProximoPago",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MesesPagados",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SaldoAFavorMeses",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UltimoPagoRegistrado",
                table: "Tenants");
        }
    }
}
