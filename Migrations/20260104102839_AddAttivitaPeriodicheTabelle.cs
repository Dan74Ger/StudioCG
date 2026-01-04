using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAttivitaPeriodicheTabelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttivitaPeriodiche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NomePlurale = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icona = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Colore = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CollegataACliente = table.Column<bool>(type: "bit", nullable: false),
                    OrdineMenu = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LarghezzaColonnaCliente = table.Column<int>(type: "int", nullable: false),
                    LarghezzaColonnaTitolo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttivitaPeriodiche", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipiPeriodo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttivitaPeriodicaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroPeriodi = table.Column<int>(type: "int", nullable: false),
                    EtichettePeriodi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateInizioPeriodi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateFinePeriodi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icona = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Colore = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MostraInteressi = table.Column<bool>(type: "bit", nullable: false),
                    PercentualeInteressiDefault = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MostraAccordion = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipiPeriodo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipiPeriodo_AttivitaPeriodiche_AttivitaPeriodicaId",
                        column: x => x.AttivitaPeriodicaId,
                        principalTable: "AttivitaPeriodiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampiPeriodici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoPeriodoId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LabelPrimoPeriodo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoCampo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ShowInList = table.Column<bool>(type: "bit", nullable: false),
                    UseAsFilter = table.Column<bool>(type: "bit", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ColWidth = table.Column<int>(type: "int", nullable: false),
                    ColumnWidth = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCalculated = table.Column<bool>(type: "bit", nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampiPeriodici", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampiPeriodici_TipiPeriodo_TipoPeriodoId",
                        column: x => x.TipoPeriodoId,
                        principalTable: "TipiPeriodo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientiAttivitaPeriodiche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttivitaPeriodicaId = table.Column<int>(type: "int", nullable: false),
                    TipoPeriodoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AnnoFiscale = table.Column<int>(type: "int", nullable: false),
                    CodCoge = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PercentualeInteressi = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiAttivitaPeriodiche", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientiAttivitaPeriodiche_AttivitaPeriodiche_AttivitaPeriodicaId",
                        column: x => x.AttivitaPeriodicaId,
                        principalTable: "AttivitaPeriodiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiAttivitaPeriodiche_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiAttivitaPeriodiche_TipiPeriodo_TipoPeriodoId",
                        column: x => x.TipoPeriodoId,
                        principalTable: "TipiPeriodo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RegoleCampi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampoPeriodicoId = table.Column<int>(type: "int", nullable: false),
                    TipoRegola = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NomeRegola = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CampoOrigineId = table.Column<int>(type: "int", nullable: true),
                    CampoDestinazioneId = table.Column<int>(type: "int", nullable: true),
                    CondizioneRiporto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Operatore = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ValoreConfronto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColoreTesto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ColoreSfondo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Grassetto = table.Column<bool>(type: "bit", nullable: false),
                    Icona = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApplicaA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priorita = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegoleCampi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegoleCampi_CampiPeriodici_CampoDestinazioneId",
                        column: x => x.CampoDestinazioneId,
                        principalTable: "CampiPeriodici",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegoleCampi_CampiPeriodici_CampoOrigineId",
                        column: x => x.CampoOrigineId,
                        principalTable: "CampiPeriodici",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegoleCampi_CampiPeriodici_CampoPeriodicoId",
                        column: x => x.CampoPeriodicoId,
                        principalTable: "CampiPeriodici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValoriPeriodi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteAttivitaPeriodicaId = table.Column<int>(type: "int", nullable: false),
                    NumeroPeriodo = table.Column<int>(type: "int", nullable: false),
                    Valori = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValoriCalcolati = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataAggiornamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoriPeriodi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoriPeriodi_ClientiAttivitaPeriodiche_ClienteAttivitaPeriodicaId",
                        column: x => x.ClienteAttivitaPeriodicaId,
                        principalTable: "ClientiAttivitaPeriodiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttivitaPeriodiche_Nome",
                table: "AttivitaPeriodiche",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampiPeriodici_TipoPeriodoId_Nome",
                table: "CampiPeriodici",
                columns: new[] { "TipoPeriodoId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivitaPeriodiche_AttivitaPeriodicaId",
                table: "ClientiAttivitaPeriodiche",
                column: "AttivitaPeriodicaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivitaPeriodiche_ClienteId",
                table: "ClientiAttivitaPeriodiche",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivitaPeriodiche_TipoPeriodoId_ClienteId_AnnoFiscale",
                table: "ClientiAttivitaPeriodiche",
                columns: new[] { "TipoPeriodoId", "ClienteId", "AnnoFiscale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegoleCampi_CampoDestinazioneId",
                table: "RegoleCampi",
                column: "CampoDestinazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_RegoleCampi_CampoOrigineId",
                table: "RegoleCampi",
                column: "CampoOrigineId");

            migrationBuilder.CreateIndex(
                name: "IX_RegoleCampi_CampoPeriodicoId",
                table: "RegoleCampi",
                column: "CampoPeriodicoId");

            migrationBuilder.CreateIndex(
                name: "IX_TipiPeriodo_AttivitaPeriodicaId_Nome",
                table: "TipiPeriodo",
                columns: new[] { "AttivitaPeriodicaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoriPeriodi_ClienteAttivitaPeriodicaId_NumeroPeriodo",
                table: "ValoriPeriodi",
                columns: new[] { "ClienteAttivitaPeriodicaId", "NumeroPeriodo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegoleCampi");

            migrationBuilder.DropTable(
                name: "ValoriPeriodi");

            migrationBuilder.DropTable(
                name: "CampiPeriodici");

            migrationBuilder.DropTable(
                name: "ClientiAttivitaPeriodiche");

            migrationBuilder.DropTable(
                name: "TipiPeriodo");

            migrationBuilder.DropTable(
                name: "AttivitaPeriodiche");
        }
    }
}
