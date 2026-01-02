using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnWidthsToEntita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LarghezzaColonnaCliente",
                table: "EntitaDinamiche",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LarghezzaColonnaStato",
                table: "EntitaDinamiche",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LarghezzaColonnaTitolo",
                table: "EntitaDinamiche",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LarghezzaColonnaCliente",
                table: "EntitaDinamiche");

            migrationBuilder.DropColumn(
                name: "LarghezzaColonnaStato",
                table: "EntitaDinamiche");

            migrationBuilder.DropColumn(
                name: "LarghezzaColonnaTitolo",
                table: "EntitaDinamiche");
        }
    }
}
