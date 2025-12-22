using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSistemaDocumenti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClausoleDocumenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Contenuto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClausoleDocumenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurazioniStudio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeStudio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Indirizzo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Citta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CAP = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Provincia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PIVA = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    CF = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PEC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Logo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Firma = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FirmaContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirmaFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurazioniStudio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateDocumenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Contenuto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RichiestaMandato = table.Column<bool>(type: "bit", nullable: false),
                    TipoOutputDefault = table.Column<int>(type: "int", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateDocumenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentiGenerati",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateDocumentoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    MandatoClienteId = table.Column<int>(type: "int", nullable: true),
                    NomeFile = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Contenuto = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoOutput = table.Column<int>(type: "int", nullable: false),
                    GeneratoDaUserId = table.Column<int>(type: "int", nullable: true),
                    GeneratoIl = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentiGenerati", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentiGenerati_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentiGenerati_MandatiClienti_MandatoClienteId",
                        column: x => x.MandatoClienteId,
                        principalTable: "MandatiClienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DocumentiGenerati_TemplateDocumenti_TemplateDocumentoId",
                        column: x => x.TemplateDocumentoId,
                        principalTable: "TemplateDocumenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentiGenerati_Users_GeneratoDaUserId",
                        column: x => x.GeneratoDaUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "DisplayOrder", "Icon", "PageName", "PageUrl", "ShowInMenu" },
                values: new object[,]
                {
                    { 301, "DOCUMENTI", "Sistema gestione documenti e template", 40, "fas fa-file-alt", "Documenti", "/Documenti", true },
                    { 302, "DOCUMENTI", "Configurazione dati e logo studio", 41, "fas fa-building", "Impostazioni Studio", "/Documenti/ImpostazioniStudio", true },
                    { 303, "DOCUMENTI", "Gestione clausole riutilizzabili", 42, "fas fa-paragraph", "Clausole", "/Documenti/Clausole", true },
                    { 304, "DOCUMENTI", "Gestione template documenti", 43, "fas fa-file-signature", "Template", "/Documenti/Template", true },
                    { 305, "DOCUMENTI", "Genera documento da template", 44, "fas fa-file-export", "Genera Documento", "/Documenti/Genera", true },
                    { 306, "DOCUMENTI", "Archivio documenti generati", 45, "fas fa-archive", "Archivio Documenti", "/Documenti/Archivio", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClausoleDocumenti_Categoria",
                table: "ClausoleDocumenti",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentiGenerati_ClienteId",
                table: "DocumentiGenerati",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentiGenerati_GeneratoDaUserId",
                table: "DocumentiGenerati",
                column: "GeneratoDaUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentiGenerati_GeneratoIl",
                table: "DocumentiGenerati",
                column: "GeneratoIl");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentiGenerati_MandatoClienteId",
                table: "DocumentiGenerati",
                column: "MandatoClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentiGenerati_TemplateDocumentoId",
                table: "DocumentiGenerati",
                column: "TemplateDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDocumenti_Categoria",
                table: "TemplateDocumenti",
                column: "Categoria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClausoleDocumenti");

            migrationBuilder.DropTable(
                name: "ConfigurazioniStudio");

            migrationBuilder.DropTable(
                name: "DocumentiGenerati");

            migrationBuilder.DropTable(
                name: "TemplateDocumenti");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 301);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 303);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 304);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 305);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 306);
        }
    }
}
