using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitaDinamiche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntitaDinamiche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NomePluruale = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Colore = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CollegataACliente = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntitaDinamiche", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampiEntita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntitaDinamicaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoCampo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ShowInList = table.Column<bool>(type: "bit", nullable: false),
                    UseAsFilter = table.Column<bool>(type: "bit", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ColWidth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampiEntita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampiEntita_EntitaDinamiche_EntitaDinamicaId",
                        column: x => x.EntitaDinamicaId,
                        principalTable: "EntitaDinamiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatiEntita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntitaDinamicaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ColoreTesto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ColoreSfondo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsFinale = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatiEntita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatiEntita_EntitaDinamiche_EntitaDinamicaId",
                        column: x => x.EntitaDinamicaId,
                        principalTable: "EntitaDinamiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecordsEntita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntitaDinamicaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    StatoEntitaId = table.Column<int>(type: "int", nullable: true),
                    Titolo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordsEntita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordsEntita_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecordsEntita_EntitaDinamiche_EntitaDinamicaId",
                        column: x => x.EntitaDinamicaId,
                        principalTable: "EntitaDinamiche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecordsEntita_StatiEntita_StatoEntitaId",
                        column: x => x.StatoEntitaId,
                        principalTable: "StatiEntita",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecordsEntita_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ValoriCampiEntita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordEntitaId = table.Column<int>(type: "int", nullable: false),
                    CampoEntitaId = table.Column<int>(type: "int", nullable: false),
                    Valore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoriCampiEntita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoriCampiEntita_CampiEntita_CampoEntitaId",
                        column: x => x.CampoEntitaId,
                        principalTable: "CampiEntita",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ValoriCampiEntita_RecordsEntita_RecordEntitaId",
                        column: x => x.RecordEntitaId,
                        principalTable: "RecordsEntita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampiEntita_EntitaDinamicaId_Nome",
                table: "CampiEntita",
                columns: new[] { "EntitaDinamicaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntitaDinamiche_Nome",
                table: "EntitaDinamiche",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecordsEntita_ClienteId",
                table: "RecordsEntita",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordsEntita_CreatedByUserId",
                table: "RecordsEntita",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordsEntita_EntitaDinamicaId",
                table: "RecordsEntita",
                column: "EntitaDinamicaId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordsEntita_StatoEntitaId",
                table: "RecordsEntita",
                column: "StatoEntitaId");

            migrationBuilder.CreateIndex(
                name: "IX_StatiEntita_EntitaDinamicaId_Nome",
                table: "StatiEntita",
                columns: new[] { "EntitaDinamicaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoriCampiEntita_CampoEntitaId",
                table: "ValoriCampiEntita",
                column: "CampoEntitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoriCampiEntita_RecordEntitaId_CampoEntitaId",
                table: "ValoriCampiEntita",
                columns: new[] { "RecordEntitaId", "CampoEntitaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValoriCampiEntita");

            migrationBuilder.DropTable(
                name: "CampiEntita");

            migrationBuilder.DropTable(
                name: "RecordsEntita");

            migrationBuilder.DropTable(
                name: "StatiEntita");

            migrationBuilder.DropTable(
                name: "EntitaDinamiche");
        }
    }
}
