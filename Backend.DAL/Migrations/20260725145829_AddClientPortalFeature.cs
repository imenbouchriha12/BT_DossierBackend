using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPortalFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeClient",
                schema: "dbo",
                table: "Clients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ClientPortalTokens",
                schema: "dbo",
                columns: table => new
                {
                    IdToken = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstRevoque = table.Column<bool>(type: "bit", nullable: false),
                    NbAcces = table.Column<int>(type: "int", nullable: false),
                    DerniereIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DernierAcces = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPortalTokens", x => x.IdToken);
                    table.ForeignKey(
                        name: "FK_ClientPortalTokens_Clients_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTemplates",
                schema: "dbo",
                columns: table => new
                {
                    IdTemplate = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeClient = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NomDocument = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstObligatoire = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    FormatsAcceptes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TailleMaxMo = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplates", x => x.IdTemplate);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "DocumentTemplates",
                columns: new[] { "IdTemplate", "DateCreation", "Description", "EstObligatoire", "FormatsAcceptes", "IsActive", "NomDocument", "Ordre", "TailleMaxMo", "TypeClient" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Recto et verso en cours de validité", true, "pdf,jpg,jpeg,png", true, "Carte d'Identité Nationale (CIN)", 1, 5, "Particulier" },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Facture eau/électricité ou attestation de résidence (-3 mois)", true, "pdf,jpg,jpeg,png", true, "Justificatif de domicile", 2, 5, "Particulier" },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "3 derniers bulletins de salaire", true, "pdf", true, "Fiche de paie", 3, 5, "Particulier" },
                    { 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Relevé d'identité bancaire", true, "pdf,jpg,jpeg,png", true, "RIB bancaire", 4, 5, "Particulier" },
                    { 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Photo récente fond blanc", false, "jpg,jpeg,png", true, "Photo d'identité", 5, 2, "Particulier" },
                    { 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Extrait du registre de commerce", true, "pdf,jpg,jpeg,png", true, "Registre de commerce (RC)", 1, 10, "Entreprise" },
                    { 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Statuts constitutifs signés et certifiés", true, "pdf", true, "Statuts de la société", 2, 10, "Entreprise" },
                    { 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Numéro d'identification fiscale", true, "pdf,jpg,jpeg,png", true, "NIF / Identifiant fiscal", 3, 5, "Entreprise" },
                    { 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bilan des 2 derniers exercices", true, "pdf,xlsx,xls", true, "Bilan comptable", 4, 20, "Entreprise" },
                    { 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pièce d'identité du représentant légal", true, "pdf,jpg,jpeg,png", true, "CIN du gérant", 5, 5, "Entreprise" },
                    { 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "RIB du compte professionnel", true, "pdf,jpg,jpeg,png", true, "RIB bancaire entreprise", 6, 5, "Entreprise" },
                    { 12, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Passeport en cours de validité", true, "pdf,jpg,jpeg,png", true, "Passeport", 1, 5, "VIP" },
                    { 13, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Titre de propriété ou relevé d'investissement", true, "pdf", true, "Justificatif de patrimoine", 2, 20, "VIP" },
                    { 14, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Déclaration fiscale des 2 dernières années", true, "pdf", true, "Déclaration de revenus", 3, 10, "VIP" },
                    { 15, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "RIB compte bancaire principal", true, "pdf,jpg,jpeg,png", true, "RIB bancaire VIP", 4, 5, "VIP" },
                    { 16, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lettre d'un partenaire ou conseiller financier", false, "pdf", true, "Lettre de recommandation", 5, 5, "VIP" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientPortalTokens_IdClient",
                schema: "dbo",
                table: "ClientPortalTokens",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPortalTokens_Token",
                schema: "dbo",
                table: "ClientPortalTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_TypeClient",
                schema: "dbo",
                table: "DocumentTemplates",
                column: "TypeClient");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_TypeClient_NomDocument",
                schema: "dbo",
                table: "DocumentTemplates",
                columns: new[] { "TypeClient", "NomDocument" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientPortalTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DocumentTemplates",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "TypeClient",
                schema: "dbo",
                table: "Clients");
        }
    }
}
