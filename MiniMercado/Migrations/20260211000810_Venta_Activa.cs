using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    /// <inheritdoc />
    public partial class Venta_Activa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Ventas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Ventas");
        }
    }
}
