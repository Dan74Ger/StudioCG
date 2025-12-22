using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMacroVociBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MacroVoceBudgetId",
                table: "VociSpesaBudget",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MacroVociBudget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codice = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroVociBudget", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VociSpesaBudget_MacroVoceBudgetId",
                table: "VociSpesaBudget",
                column: "MacroVoceBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_MacroVociBudget_Codice",
                table: "MacroVociBudget",
                column: "Codice",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VociSpesaBudget_MacroVociBudget_MacroVoceBudgetId",
                table: "VociSpesaBudget",
                column: "MacroVoceBudgetId",
                principalTable: "MacroVociBudget",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VociSpesaBudget_MacroVociBudget_MacroVoceBudgetId",
                table: "VociSpesaBudget");

            migrationBuilder.DropTable(
                name: "MacroVociBudget");

            migrationBuilder.DropIndex(
                name: "IX_VociSpesaBudget_MacroVoceBudgetId",
                table: "VociSpesaBudget");

            migrationBuilder.DropColumn(
                name: "MacroVoceBudgetId",
                table: "VociSpesaBudget");
        }
    }
}
