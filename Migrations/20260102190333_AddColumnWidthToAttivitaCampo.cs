using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnWidthToAttivitaCampo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColumnWidth",
                table: "AttivitaCampi",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnWidth",
                table: "AttivitaCampi");
        }
    }
}
