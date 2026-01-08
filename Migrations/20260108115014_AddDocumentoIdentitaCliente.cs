using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioCG.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentoIdentitaCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentoDataRilascio",
                table: "Clienti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentoNumero",
                table: "Clienti",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentoRilasciatoDa",
                table: "Clienti",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentoScadenza",
                table: "Clienti",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentoDataRilascio",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "DocumentoNumero",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "DocumentoRilasciatoDa",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "DocumentoScadenza",
                table: "Clienti");
        }
    }
}
