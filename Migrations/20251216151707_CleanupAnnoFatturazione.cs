using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class CleanupAnnoFatturazione : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MandatiClienti_AnniFatturazione_AnnoFatturazioneId",
                table: "MandatiClienti");

            migrationBuilder.DropForeignKey(
                name: "FK_ScadenzeFatturazione_AnniFatturazione_AnnoFatturazioneId",
                table: "ScadenzeFatturazione");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
