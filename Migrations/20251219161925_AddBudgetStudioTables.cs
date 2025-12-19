using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetStudioTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VociSpesaBudget",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodiceSpesa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetodoPagamentoDefault = table.Column<int>(type: "int", nullable: false),
                    NoteDefault = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VociSpesaBudget", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetSpeseMensili",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoceSpesaBudgetId = table.Column<int>(type: "int", nullable: false),
                    Anno = table.Column<int>(type: "int", nullable: false),
                    Mese = table.Column<int>(type: "int", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Pagata = table.Column<bool>(type: "bit", nullable: false),
                    MetodoPagamento = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetSpeseMensili", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetSpeseMensili_VociSpesaBudget_VoceSpesaBudgetId",
                        column: x => x.VoceSpesaBudgetId,
                        principalTable: "VociSpesaBudget",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "DisplayOrder", "Icon", "PageName", "PageUrl", "ShowInMenu" },
                values: new object[] { 210, "AMMINISTRAZIONE", "Budget Studio - pianificazione spese mensili", 30, "fas fa-coins", "Budget Studio", "/Amministrazione/BudgetStudio", true });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSpeseMensili_VoceSpesaBudgetId_Anno_Mese",
                table: "BudgetSpeseMensili",
                columns: new[] { "VoceSpesaBudgetId", "Anno", "Mese" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VociSpesaBudget_CodiceSpesa",
                table: "VociSpesaBudget",
                column: "CodiceSpesa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetSpeseMensili");

            migrationBuilder.DropTable(
                name: "VociSpesaBudget");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 210);
        }
    }
}
