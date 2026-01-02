using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddScadenzaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GiorniPreavvisoScadenza",
                table: "EntitaDinamiche",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDataScadenza",
                table: "CampiEntita",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiorniPreavvisoScadenza",
                table: "EntitaDinamiche");

            migrationBuilder.DropColumn(
                name: "IsDataScadenza",
                table: "CampiEntita");
        }
    }
}
