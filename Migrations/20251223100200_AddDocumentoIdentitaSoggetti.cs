using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentoIdentitaSoggetti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentoDataRilascio",
                table: "ClientiSoggetti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentoNumero",
                table: "ClientiSoggetti",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentoRilasciatoDa",
                table: "ClientiSoggetti",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentoDataRilascio",
                table: "ClientiSoggetti");

            migrationBuilder.DropColumn(
                name: "DocumentoNumero",
                table: "ClientiSoggetti");

            migrationBuilder.DropColumn(
                name: "DocumentoRilasciatoDa",
                table: "ClientiSoggetti");
        }
    }
}
