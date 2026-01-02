using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculatedFieldsToCampoEntita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColumnWidth",
                table: "CampiEntita",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Formula",
                table: "CampiEntita",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCalculated",
                table: "CampiEntita",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnWidth",
                table: "CampiEntita");

            migrationBuilder.DropColumn(
                name: "Formula",
                table: "CampiEntita");

            migrationBuilder.DropColumn(
                name: "IsCalculated",
                table: "CampiEntita");
        }
    }
}
