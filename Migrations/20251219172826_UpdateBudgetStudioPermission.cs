using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBudgetStudioPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 210,
                columns: new[] { "Category", "PageUrl" },
                values: new object[] { null, "/BudgetStudio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 210,
                columns: new[] { "Category", "PageUrl" },
                values: new object[] { "AMMINISTRAZIONE", "/Amministrazione/BudgetStudio" });
        }
    }
}
