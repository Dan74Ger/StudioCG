using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCampiCustomClienteEMenuDinamico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampiCustomClienti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoCampo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ShowInList = table.Column<bool>(type: "bit", nullable: false),
                    UseAsFilter = table.Column<bool>(type: "bit", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampiCustomClienti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VociMenu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsGroup = table.Column<bool>(type: "bit", nullable: false),
                    ExpandedByDefault = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    TipoVoce = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VociMenu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VociMenu_VociMenu_ParentId",
                        column: x => x.ParentId,
                        principalTable: "VociMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValoriCampiCustomClienti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    CampoCustomClienteId = table.Column<int>(type: "int", nullable: false),
                    Valore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoriCampiCustomClienti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoriCampiCustomClienti_CampiCustomClienti_CampoCustomClienteId",
                        column: x => x.CampoCustomClienteId,
                        principalTable: "CampiCustomClienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValoriCampiCustomClienti_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurazioniMenuUtenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VoceMenuId = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CustomOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurazioniMenuUtenti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurazioniMenuUtenti_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfigurazioniMenuUtenti_VociMenu_VoceMenuId",
                        column: x => x.VoceMenuId,
                        principalTable: "VociMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampiCustomClienti_Nome",
                table: "CampiCustomClienti",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurazioniMenuUtenti_UserId_VoceMenuId",
                table: "ConfigurazioniMenuUtenti",
                columns: new[] { "UserId", "VoceMenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurazioniMenuUtenti_VoceMenuId",
                table: "ConfigurazioniMenuUtenti",
                column: "VoceMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoriCampiCustomClienti_CampoCustomClienteId",
                table: "ValoriCampiCustomClienti",
                column: "CampoCustomClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoriCampiCustomClienti_ClienteId_CampoCustomClienteId",
                table: "ValoriCampiCustomClienti",
                columns: new[] { "ClienteId", "CampoCustomClienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VociMenu_ParentId",
                table: "VociMenu",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurazioniMenuUtenti");

            migrationBuilder.DropTable(
                name: "ValoriCampiCustomClienti");

            migrationBuilder.DropTable(
                name: "VociMenu");

            migrationBuilder.DropTable(
                name: "CampiCustomClienti");
        }
    }
}
