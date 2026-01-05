using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingPagePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "DisplayOrder", "Icon", "PageName", "PageUrl", "ShowInMenu" },
                values: new object[,]
                {
                    { 104, "ANAGRAFICA", "Gestione dati attività annuali clienti", 14, "fas fa-tasks", "Attività", "/Attivita", true },
                    { 105, "ANAGRAFICA", "Gestione attività periodiche (LIPE, ecc.)", 15, "fas fa-calendar-check", "Attività Periodiche", "/AttivitaPeriodiche", true },
                    { 106, "ANAGRAFICA", "Gestione entità dinamiche", 16, "fas fa-cubes", "Entità", "/Entita", true },
                    { 400, "SISTEMA", "Gestione pagine dinamiche", 50, "fas fa-file-alt", "Pagine Dinamiche", "/DynamicPages", true },
                    { 402, "SISTEMA", "Gestione permessi utenti", 52, "fas fa-user-shield", "Permessi", "/Permissions", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 400);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 402);
        }
    }
}
