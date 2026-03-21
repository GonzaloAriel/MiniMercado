using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class AddTenantToRubro : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Rubros",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Rubros_TenantId",
                table: "Rubros",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rubros_Tenants_TenantId",
                table: "Rubros",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rubros_Tenants_TenantId",
                table: "Rubros");

            migrationBuilder.DropIndex(
                name: "IX_Rubros_TenantId",
                table: "Rubros");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Rubros");
        }
    }
}
