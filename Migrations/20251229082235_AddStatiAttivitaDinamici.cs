using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddStatiAttivitaDinamici : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatoAttivitaTipoId",
                table: "ClientiAttivita",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StatiAttivitaTipo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttivitaTipoId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ColoreTesto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ColoreSfondo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsFinale = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatiAttivitaTipo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatiAttivitaTipo_AttivitaTipi_AttivitaTipoId",
                        column: x => x.AttivitaTipoId,
                        principalTable: "AttivitaTipi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientiAttivita_StatoAttivitaTipoId",
                table: "ClientiAttivita",
                column: "StatoAttivitaTipoId");

            migrationBuilder.CreateIndex(
                name: "IX_StatiAttivitaTipo_AttivitaTipoId_Nome",
                table: "StatiAttivitaTipo",
                columns: new[] { "AttivitaTipoId", "Nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientiAttivita_StatiAttivitaTipo_StatoAttivitaTipoId",
                table: "ClientiAttivita",
                column: "StatoAttivitaTipoId",
                principalTable: "StatiAttivitaTipo",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ============ MIGRAZIONE DATI ============
            // Crea stati di default per ogni AttivitaTipo esistente
            // Stati basati sull'enum StatoAttivita originale:
            // 0 = Da Fare, 1 = Completata, 2 = Da inviare Entratel, 3 = DR Inviate, 4 = Sospesa
            migrationBuilder.Sql(@"
                -- Per ogni AttivitaTipo esistente, crea 5 stati di default
                INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                SELECT 
                    Id,
                    'Da Fare',
                    'fas fa-clock',
                    '#000000',
                    '#ffc107',
                    0,
                    1,  -- IsDefault = true (stato iniziale)
                    0,  -- IsFinale = false
                    1,
                    GETDATE()
                FROM AttivitaTipi;

                INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                SELECT 
                    Id,
                    'Completata',
                    'fas fa-check-circle',
                    '#FFFFFF',
                    '#28a745',
                    1,
                    0,
                    1,  -- IsFinale = true (stato completamento)
                    1,
                    GETDATE()
                FROM AttivitaTipi;

                INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                SELECT 
                    Id,
                    'Da inviare Entratel',
                    'fas fa-paper-plane',
                    '#FFFFFF',
                    '#17a2b8',
                    2,
                    0,
                    0,
                    1,
                    GETDATE()
                FROM AttivitaTipi;

                INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                SELECT 
                    Id,
                    'DR Inviate',
                    'fas fa-envelope-open-text',
                    '#FFFFFF',
                    '#6f42c1',
                    3,
                    0,
                    0,
                    1,
                    GETDATE()
                FROM AttivitaTipi;

                INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                SELECT 
                    Id,
                    'Sospesa',
                    'fas fa-pause-circle',
                    '#FFFFFF',
                    '#dc3545',
                    4,
                    0,
                    0,
                    1,
                    GETDATE()
                FROM AttivitaTipi;
            ");

            // Migra i dati esistenti in ClienteAttivita collegandoli ai nuovi stati
            migrationBuilder.Sql(@"
                -- Aggiorna ClienteAttivita con il nuovo StatoAttivitaTipoId basato sul vecchio campo Stato
                UPDATE ca
                SET ca.StatoAttivitaTipoId = sat.Id
                FROM ClientiAttivita ca
                INNER JOIN AttivitaAnnuali aa ON ca.AttivitaAnnualeId = aa.Id
                INNER JOIN StatiAttivitaTipo sat ON sat.AttivitaTipoId = aa.AttivitaTipoId
                WHERE 
                    (ca.Stato = 0 AND sat.Nome = 'Da Fare') OR
                    (ca.Stato = 1 AND sat.Nome = 'Completata') OR
                    (ca.Stato = 2 AND sat.Nome = 'Da inviare Entratel') OR
                    (ca.Stato = 3 AND sat.Nome = 'DR Inviate') OR
                    (ca.Stato = 4 AND sat.Nome = 'Sospesa');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientiAttivita_StatiAttivitaTipo_StatoAttivitaTipoId",
                table: "ClientiAttivita");

            migrationBuilder.DropTable(
                name: "StatiAttivitaTipo");

            migrationBuilder.DropIndex(
                name: "IX_ClientiAttivita_StatoAttivitaTipoId",
                table: "ClientiAttivita");

            migrationBuilder.DropColumn(
                name: "StatoAttivitaTipoId",
                table: "ClientiAttivita");
        }
    }
}
