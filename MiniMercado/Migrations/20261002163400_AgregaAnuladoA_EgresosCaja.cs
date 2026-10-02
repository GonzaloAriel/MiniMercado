using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMercado.Migrations
{
    public partial class AgregaAnuladoA_EgresosCaja : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Anulado",
                table: "EgresosCaja",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anulado",
                table: "EgresosCaja");
        }
    }
}
