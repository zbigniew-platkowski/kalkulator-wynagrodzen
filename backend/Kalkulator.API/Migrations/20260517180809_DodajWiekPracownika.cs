using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalkulator.API.Migrations
{
    /// <inheritdoc />
    public partial class DodajWiekPracownika : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WiekObecny",
                table: "Pracownicy",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WiekObecny",
                table: "Pracownicy");
        }
    }
}
