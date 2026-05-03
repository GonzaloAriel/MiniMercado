using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class RemoveEsPorPesoFromProductoBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsPorPeso",
                table: "ProductosBase");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsPorPeso",
                table: "ProductosBase",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
