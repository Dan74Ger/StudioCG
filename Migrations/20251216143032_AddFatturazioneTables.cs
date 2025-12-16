using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFatturazioneTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContatoriDocumenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    UltimoNumero = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContatoriDocumenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MandatiClienti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    ImportoAnnuo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoScadenza = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatiClienti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MandatiClienti_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScadenzeFatturazione",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MandatoClienteId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    DataScadenza = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportoMandato = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumeroProforma = table.Column<int>(type: "int", nullable: true),
                    DataProforma = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumeroFattura = table.Column<int>(type: "int", nullable: true),
                    DataFattura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    StatoIncasso = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScadenzeFatturazione", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScadenzeFatturazione_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScadenzeFatturazione_MandatiClienti_MandatoClienteId",
                        column: x => x.MandatoClienteId,
                        principalTable: "MandatiClienti",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccessiClienti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    ScadenzaFatturazioneId = table.Column<int>(type: "int", nullable: false),
                    UtenteId = table.Column<int>(type: "int", nullable: true),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OraInizioMattino = table.Column<TimeSpan>(type: "time", nullable: true),
                    OraFineMattino = table.Column<TimeSpan>(type: "time", nullable: true),
                    OraInizioPomeriggio = table.Column<TimeSpan>(type: "time", nullable: true),
                    OraFinePomeriggio = table.Column<TimeSpan>(type: "time", nullable: true),
                    TariffaOraria = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessiClienti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessiClienti_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessiClienti_ScadenzeFatturazione_ScadenzaFatturazioneId",
                        column: x => x.ScadenzaFatturazioneId,
                        principalTable: "ScadenzeFatturazione",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccessiClienti_Users_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BilanciCEE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataScadenza = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScadenzaFatturazioneId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BilanciCEE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BilanciCEE_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BilanciCEE_ScadenzeFatturazione_ScadenzaFatturazioneId",
                        column: x => x.ScadenzaFatturazioneId,
                        principalTable: "ScadenzeFatturazione",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FattureCloud",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataScadenza = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScadenzaFatturazioneId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FattureCloud", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FattureCloud_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FattureCloud_ScadenzeFatturazione_ScadenzaFatturazioneId",
                        column: x => x.ScadenzaFatturazioneId,
                        principalTable: "ScadenzeFatturazione",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IncassiFatture",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScadenzaFatturazioneId = table.Column<int>(type: "int", nullable: false),
                    DataIncasso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportoIncassato = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncassiFatture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncassiFatture_ScadenzeFatturazione_ScadenzaFatturazioneId",
                        column: x => x.ScadenzaFatturazioneId,
                        principalTable: "ScadenzeFatturazione",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpesePratiche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    ScadenzaFatturazioneId = table.Column<int>(type: "int", nullable: false),
                    UtenteId = table.Column<int>(type: "int", nullable: true),
                    Descrizione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpesePratiche", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpesePratiche_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpesePratiche_ScadenzeFatturazione_ScadenzaFatturazioneId",
                        column: x => x.ScadenzaFatturazioneId,
                        principalTable: "ScadenzeFatturazione",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpesePratiche_Users_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IncassiProfessionisti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncassoFatturaId = table.Column<int>(type: "int", nullable: false),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    Percentuale = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncassiProfessionisti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncassiProfessionisti_IncassiFatture_IncassoFatturaId",
                        column: x => x.IncassoFatturaId,
                        principalTable: "IncassiFatture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncassiProfessionisti_Users_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessiClienti_ClienteId",
                table: "AccessiClienti",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessiClienti_ScadenzaFatturazioneId",
                table: "AccessiClienti",
                column: "ScadenzaFatturazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessiClienti_UtenteId",
                table: "AccessiClienti",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_BilanciCEE_ClienteId_Anno",
                table: "BilanciCEE",
                columns: new[] { "ClienteId", "Anno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BilanciCEE_ScadenzaFatturazioneId",
                table: "BilanciCEE",
                column: "ScadenzaFatturazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_ContatoriDocumenti_Anno_TipoDocumento",
                table: "ContatoriDocumenti",
                columns: new[] { "Anno", "TipoDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FattureCloud_ClienteId_Anno",
                table: "FattureCloud",
                columns: new[] { "ClienteId", "Anno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FattureCloud_ScadenzaFatturazioneId",
                table: "FattureCloud",
                column: "ScadenzaFatturazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_IncassiFatture_ScadenzaFatturazioneId",
                table: "IncassiFatture",
                column: "ScadenzaFatturazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_IncassiProfessionisti_IncassoFatturaId",
                table: "IncassiProfessionisti",
                column: "IncassoFatturaId");

            migrationBuilder.CreateIndex(
                name: "IX_IncassiProfessionisti_UtenteId",
                table: "IncassiProfessionisti",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatiClienti_ClienteId_Anno",
                table: "MandatiClienti",
                columns: new[] { "ClienteId", "Anno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScadenzeFatturazione_ClienteId",
                table: "ScadenzeFatturazione",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ScadenzeFatturazione_MandatoClienteId",
                table: "ScadenzeFatturazione",
                column: "MandatoClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_SpesePratiche_ClienteId",
                table: "SpesePratiche",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_SpesePratiche_ScadenzaFatturazioneId",
                table: "SpesePratiche",
                column: "ScadenzaFatturazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_SpesePratiche_UtenteId",
                table: "SpesePratiche",
                column: "UtenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessiClienti");

            migrationBuilder.DropTable(
                name: "BilanciCEE");

            migrationBuilder.DropTable(
                name: "ContatoriDocumenti");

            migrationBuilder.DropTable(
                name: "FattureCloud");

            migrationBuilder.DropTable(
                name: "IncassiProfessionisti");

            migrationBuilder.DropTable(
                name: "SpesePratiche");

            migrationBuilder.DropTable(
                name: "IncassiFatture");

            migrationBuilder.DropTable(
                name: "ScadenzeFatturazione");

            migrationBuilder.DropTable(
                name: "MandatiClienti");
        }
    }
}
