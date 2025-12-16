using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnoFatturazione : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnnoFatturazioneId",
                table: "ScadenzeFatturazione",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnnoFatturazioneId",
                table: "MandatiClienti",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnniFatturazione",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnniFatturazione", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScadenzeFatturazione_AnnoFatturazioneId",
                table: "ScadenzeFatturazione",
                column: "AnnoFatturazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatiClienti_AnnoFatturazioneId",
                table: "MandatiClienti",
                column: "AnnoFatturazioneId");

            migrationBuilder.AddForeignKey(
                name: "FK_MandatiClienti_AnniFatturazione_AnnoFatturazioneId",
                table: "MandatiClienti",
                column: "AnnoFatturazioneId",
                principalTable: "AnniFatturazione",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScadenzeFatturazione_AnniFatturazione_AnnoFatturazioneId",
                table: "ScadenzeFatturazione",
                column: "AnnoFatturazioneId",
                principalTable: "AnniFatturazione",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MandatiClienti_AnniFatturazione_AnnoFatturazioneId",
                table: "MandatiClienti");

            migrationBuilder.DropForeignKey(
                name: "FK_ScadenzeFatturazione_AnniFatturazione_AnnoFatturazioneId",
                table: "ScadenzeFatturazione");

            migrationBuilder.DropTable(
                name: "AnniFatturazione");

            migrationBuilder.DropIndex(
                name: "IX_ScadenzeFatturazione_AnnoFatturazioneId",
                table: "ScadenzeFatturazione");

            migrationBuilder.DropIndex(
                name: "IX_MandatiClienti_AnnoFatturazioneId",
                table: "MandatiClienti");

            migrationBuilder.DropColumn(
                name: "AnnoFatturazioneId",
                table: "ScadenzeFatturazione");

            migrationBuilder.DropColumn(
                name: "AnnoFatturazioneId",
                table: "MandatiClienti");
        }
    }
}
