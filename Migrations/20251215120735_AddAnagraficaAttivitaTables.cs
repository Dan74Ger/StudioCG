using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAnagraficaAttivitaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnualitaFiscali",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualitaFiscali", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttivitaTipi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttivitaTipi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RagioneSociale = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Indirizzo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Citta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provincia = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    CAP = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PEC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CodiceFiscale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    PartitaIVA = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttivitaAnnuali",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttivitaTipoId = table.Column<int>(type: "int", nullable: false),
                    AnnualitaFiscaleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DataScadenza = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttivitaAnnuali", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttivitaAnnuali_AnnualitaFiscali_AnnualitaFiscaleId",
                        column: x => x.AnnualitaFiscaleId,
                        principalTable: "AnnualitaFiscali",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttivitaAnnuali_AttivitaTipi_AttivitaTipoId",
                        column: x => x.AttivitaTipoId,
                        principalTable: "AttivitaTipi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttivitaCampi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttivitaTipoId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ShowInList = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttivitaCampi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttivitaCampi_AttivitaTipi_AttivitaTipoId",
                        column: x => x.AttivitaTipoId,
                        principalTable: "AttivitaTipi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientiSoggetti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    TipoSoggetto = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cognome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodiceFiscale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Indirizzo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Citta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provincia = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    CAP = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    QuotaPercentuale = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiSoggetti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientiSoggetti_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientiAttivita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AttivitaAnnualeId = table.Column<int>(type: "int", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    DataCompletamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiAttivita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientiAttivita_AttivitaAnnuali_AttivitaAnnualeId",
                        column: x => x.AttivitaAnnualeId,
                        principalTable: "AttivitaAnnuali",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientiAttivita_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientiAttivitaValori",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteAttivitaId = table.Column<int>(type: "int", nullable: false),
                    AttivitaCampoId = table.Column<int>(type: "int", nullable: false),
                    Valore = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiAttivitaValori", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientiAttivitaValori_AttivitaCampi_AttivitaCampoId",
                        column: x => x.AttivitaCampoId,
                        principalTable: "AttivitaCampi",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientiAttivitaValori_ClientiAttivita_ClienteAttivitaId",
                        column: x => x.ClienteAttivitaId,
                        principalTable: "ClientiAttivita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AnnualitaFiscali",
                columns: new[] { "Id", "Anno", "CreatedAt", "Descrizione", "IsActive", "IsCurrent" },
                values: new object[] { 1, 2025, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anno Fiscale 2025", true, true });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "DisplayOrder", "Icon", "PageName", "PageUrl", "ShowInMenu" },
                values: new object[,]
                {
                    { 100, "ANAGRAFICA", "Gestione anagrafica clienti", 10, "fas fa-building", "Clienti", "/Clienti", true },
                    { 101, "ANAGRAFICA", "Gestione annualità fiscali", 11, "fas fa-calendar-alt", "Annualità Fiscali", "/Annualita", true },
                    { 102, "ANAGRAFICA", "Gestione tipi attività", 12, "fas fa-cogs", "Tipi Attività", "/AttivitaTipi", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualitaFiscali_Anno",
                table: "AnnualitaFiscali",
                column: "Anno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttivitaAnnuali_AnnualitaFiscaleId",
                table: "AttivitaAnnuali",
                column: "AnnualitaFiscaleId");

            migrationBuilder.CreateIndex(
                name: "IX_AttivitaAnnuali_AttivitaTipoId_AnnualitaFiscaleId",
                table: "AttivitaAnnuali",
                columns: new[] { "AttivitaTipoId", "AnnualitaFiscaleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttivitaCampi_AttivitaTipoId",
                table: "AttivitaCampi",
                column: "AttivitaTipoId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivita_AttivitaAnnualeId",
                table: "ClientiAttivita",
                column: "AttivitaAnnualeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivita_ClienteId_AttivitaAnnualeId",
                table: "ClientiAttivita",
                columns: new[] { "ClienteId", "AttivitaAnnualeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivitaValori_AttivitaCampoId",
                table: "ClientiAttivitaValori",
                column: "AttivitaCampoId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivitaValori_ClienteAttivitaId_AttivitaCampoId",
                table: "ClientiAttivitaValori",
                columns: new[] { "ClienteAttivitaId", "AttivitaCampoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiSoggetti_ClienteId",
                table: "ClientiSoggetti",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientiAttivitaValori");

            migrationBuilder.DropTable(
                name: "ClientiSoggetti");

            migrationBuilder.DropTable(
                name: "AttivitaCampi");

            migrationBuilder.DropTable(
                name: "ClientiAttivita");

            migrationBuilder.DropTable(
                name: "AttivitaAnnuali");

            migrationBuilder.DropTable(
                name: "Clienti");

            migrationBuilder.DropTable(
                name: "AnnualitaFiscali");

            migrationBuilder.DropTable(
                name: "AttivitaTipi");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 102);
        }
    }
}
