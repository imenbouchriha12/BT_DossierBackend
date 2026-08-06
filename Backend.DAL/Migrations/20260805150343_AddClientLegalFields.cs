using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddClientLegalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatriculeFiscal",
                schema: "dbo",
                table: "Client",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistreCommerce",
                schema: "dbo",
                table: "Client",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "DocumentTemplates",
                columns: new[] { "IdTemplate", "DateCreation", "Description", "EstObligatoire", "FormatsAcceptes", "IsActive", "NomDocument", "Ordre", "TailleMaxMo", "TypeClient" },
                values: new object[,]
                {
                    { 17, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Recto et verso en cours de validité", true, "pdf,jpg,jpeg,png", true, "Carte d'Identité Nationale (CIN)", 1, 5, "Professionnel" },
                    { 18, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Attestation d'affectation du matricule fiscal", true, "pdf,jpg,jpeg,png", true, "Matricule fiscal", 2, 5, "Professionnel" },
                    { 19, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Extrait du registre de commerce (si assujetti)", false, "pdf,jpg,jpeg,png", true, "Registre de commerce", 3, 10, "Professionnel" },
                    { 20, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "RIB du compte professionnel", true, "pdf,jpg,jpeg,png", true, "RIB bancaire professionnel", 4, 5, "Professionnel" },
                    { 21, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dernier avis d'imposition", true, "pdf", true, "Avis d'imposition", 5, 10, "Professionnel" },
                    { 22, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Récépissé de déclaration à la DGRH", true, "pdf,jpg,jpeg,png", true, "Récépissé de déclaration", 1, 10, "Association" },
                    { 23, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Statuts déposés et visés", true, "pdf", true, "Statuts de l'association", 2, 10, "Association" },
                    { 24, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Composition du bureau et PV d'élection", true, "pdf,jpg,jpeg,png", true, "Liste du bureau exécutif", 3, 5, "Association" },
                    { 25, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pièce d'identité du président", true, "pdf,jpg,jpeg,png", true, "CIN du président", 4, 5, "Association" },
                    { 26, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "RIB du compte association", true, "pdf,jpg,jpeg,png", true, "RIB bancaire association", 5, 5, "Association" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 17);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 18);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 19);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 22);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 23);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 24);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 25);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "DocumentTemplates",
                keyColumn: "IdTemplate",
                keyValue: 26);

            migrationBuilder.DropColumn(
                name: "MatriculeFiscal",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "RegistreCommerce",
                schema: "dbo",
                table: "Client");
        }
    }
}
