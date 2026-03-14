using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    /// <inheritdoc />
    public partial class TablaAuditoriaCompleta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auditorias_Tenants_TenantId",
                table: "Auditorias");

            migrationBuilder.DropIndex(
                name: "IX_Auditorias_TenantId",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Detalles",
                table: "Auditorias");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioId",
                table: "Auditorias",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Accion",
                table: "Auditorias",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Auditorias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RegistroId",
                table: "Auditorias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tabla",
                table: "Auditorias",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "RegistroId",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Tabla",
                table: "Auditorias");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioId",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Accion",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Detalles",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_TenantId",
                table: "Auditorias",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Auditorias_Tenants_TenantId",
                table: "Auditorias",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
