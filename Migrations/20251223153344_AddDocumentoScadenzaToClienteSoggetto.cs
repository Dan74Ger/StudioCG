using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentoScadenzaToClienteSoggetto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentoScadenza",
                table: "ClientiSoggetti",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentoScadenza",
                table: "ClientiSoggetti");
        }
    }
}
