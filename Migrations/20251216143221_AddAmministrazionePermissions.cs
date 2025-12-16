using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAmministrazionePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "DisplayOrder", "Icon", "PageName", "PageUrl", "ShowInMenu" },
                values: new object[,]
                {
                    { 200, "AMMINISTRAZIONE", "Dashboard riepilogo fatturazione", 20, "fas fa-chart-line", "Dashboard Fatturazione", "/Amministrazione", true },
                    { 201, "AMMINISTRAZIONE", "Gestione mandati professionali", 21, "fas fa-file-contract", "Mandati Clienti", "/Amministrazione/Mandati", true },
                    { 202, "AMMINISTRAZIONE", "Gestione scadenze e fatturazione", 22, "fas fa-file-invoice-dollar", "Scadenze Fatturazione", "/Amministrazione/Scadenze", true },
                    { 203, "AMMINISTRAZIONE", "Gestione spese pratiche mensili", 23, "fas fa-receipt", "Spese Pratiche", "/Amministrazione/SpesePratiche", true },
                    { 204, "AMMINISTRAZIONE", "Registrazione accessi clienti", 24, "fas fa-door-open", "Accessi Clienti", "/Amministrazione/AccessiClienti", true },
                    { 205, "AMMINISTRAZIONE", "Gestione Fatture in Cloud", 25, "fas fa-cloud", "Fatture in Cloud", "/Amministrazione/FattureCloud", true },
                    { 206, "AMMINISTRAZIONE", "Gestione Bilanci CEE", 26, "fas fa-balance-scale", "Bilanci CEE", "/Amministrazione/BilanciCEE", true },
                    { 207, "AMMINISTRAZIONE", "Gestione incassi fatture", 27, "fas fa-money-bill-wave", "Incassi", "/Amministrazione/Incassi", true },
                    { 208, "AMMINISTRAZIONE", "Report incassi per professionista", 28, "fas fa-user-tie", "Report Professionisti", "/Amministrazione/ReportProfessionisti", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 201);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 202);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 203);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 204);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 208);
        }
    }
}
