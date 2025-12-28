using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "DisplayOrder", "Icon", "PageName", "PageUrl", "ShowInMenu" },
                values: new object[,]
                {
                    { 103, "ANAGRAFICA", "Gestione scadenze documenti identità soggetti", 13, "fas fa-id-card", "Scadenze Documenti Identità", "/Clienti/ScadenzeDocumenti", true },
                    { 307, "DOCUMENTI", "Controllo scadenze documenti antiriciclaggio", 46, "fas fa-shield-alt", "Controllo Antiriciclaggio", "/Documenti/ControlloAntiriciclaggio", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 307);
        }
    }
}
