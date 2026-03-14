using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEnVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioId",
                table: "Ventas");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId1",
                table: "Ventas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_UsuarioId1",
                table: "Ventas",
                column: "UsuarioId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioId",
                table: "Ventas",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioId1",
                table: "Ventas",
                column: "UsuarioId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioId",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioId1",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_UsuarioId1",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioId1",
                table: "Ventas");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioId",
                table: "Ventas",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
