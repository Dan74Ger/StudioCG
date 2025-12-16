using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGestioneAnniPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "DisplayOrder", "Icon", "PageName", "PageUrl", "ShowInMenu" },
                values: new object[] { 209, "AMMINISTRAZIONE", "Gestione anni di fatturazione", 29, "fas fa-calendar-alt", "Gestione Anni", "/Amministrazione/GestioneAnni", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 209);
        }
    }
}
