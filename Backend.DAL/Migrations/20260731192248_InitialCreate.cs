using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AppReg_Roles",
                schema: "dbo",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReg_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "AppReg_Users",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Matricule = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HasBeenActivated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReg_Users", x => x.UserId);
                });

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreation",
                schema: "dbo",
                table: "Client",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "Statut",
                schema: "dbo",
                table: "Client",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Actif");

            migrationBuilder.AddColumn<bool>(
                name: "EstArchive",
                schema: "dbo",
                table: "Client",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotifBlocage",
                schema: "dbo",
                table: "Client",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeClient",
                schema: "dbo",
                table: "Client",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Particulier");

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

            migrationBuilder.CreateTable(
                name: "HistoriqueModifications",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableModifiee = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntiteId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AncienneValeur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NouvelleValeur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    UtilisateurNom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateHeure = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueModifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppReg_UserRoles",
                schema: "dbo",
                columns: table => new
                {
                    UserRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReg_UserRoles", x => x.UserRoleId);
                    table.ForeignKey(
                        name: "FK_AppReg_UserRoles_AppReg_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppReg_UserRoles_AppReg_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "dbo",
                columns: table => new
                {
                    IdNotification = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Lien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstLu = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.IdNotification);
                    table.ForeignKey(
                        name: "FK_Notifications_AppReg_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistory",
                schema: "dbo",
                columns: table => new
                {
                    IdSearchHistory = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SearchQuery = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SearchType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResultsCount = table.Column<int>(type: "int", nullable: false),
                    SearchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutionTimeMs = table.Column<int>(type: "int", nullable: false),
                    Filters = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistory", x => x.IdSearchHistory);
                    table.ForeignKey(
                        name: "FK_SearchHistory_AppReg_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

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
                        name: "FK_ClientPortalTokens_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientTags",
                schema: "dbo",
                columns: table => new
                {
                    IdTag = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTags", x => x.IdTag);
                    table.ForeignKey(
                        name: "FK_ClientTags_AppReg_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientTags_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<string>(
                name: "StatutValidation",
                schema: "dbo",
                table: "DocumentClient",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "En attente");

            migrationBuilder.AddColumn<int>(
                name: "UploadedByUserId",
                schema: "dbo",
                table: "DocumentClient",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TexteExtrait",
                schema: "dbo",
                table: "DocumentClient",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonneesExtraites",
                schema: "dbo",
                table: "DocumentClient",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreConfiance",
                schema: "dbo",
                table: "DocumentClient",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateVerificationOCR",
                schema: "dbo",
                table: "DocumentClient",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultatVerification",
                schema: "dbo",
                table: "DocumentClient",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reminders",
                schema: "dbo",
                columns: table => new
                {
                    IdReminder = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.IdReminder);
                    table.ForeignKey(
                        name: "FK_Reminders_AppReg_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reminders_AppReg_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reminders_Client_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTags",
                schema: "dbo",
                columns: table => new
                {
                    IdTag = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDocument = table.Column<int>(type: "int", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTags", x => x.IdTag);
                    table.ForeignKey(
                        name: "FK_DocumentTags_AppReg_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTags_DocumentClient_IdDocument",
                        column: x => x.IdDocument,
                        principalSchema: "dbo",
                        principalTable: "DocumentClient",
                        principalColumn: "IdDocument",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "AppReg_Roles",
                columns: new[] { "RoleId", "IsActive", "RoleName" },
                values: new object[,]
                {
                    { 1, true, "Admin" },
                    { 2, true, "Agent" },
                    { 3, true, "Superviseur" }
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
                name: "IX_AppReg_UserRoles_RoleId",
                schema: "dbo",
                table: "AppReg_UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReg_UserRoles_UserId",
                schema: "dbo",
                table: "AppReg_UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReg_Users_Email",
                schema: "dbo",
                table: "AppReg_Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppReg_Users_Matricule",
                schema: "dbo",
                table: "AppReg_Users",
                column: "Matricule",
                unique: true);

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
                name: "IX_ClientTags_CreatedBy",
                schema: "dbo",
                table: "ClientTags",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTags_IdClient",
                schema: "dbo",
                table: "ClientTags",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTags_Tag",
                schema: "dbo",
                table: "ClientTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentClient_IdClient",
                schema: "dbo",
                table: "DocumentClient",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentClient_UploadedByUserId",
                schema: "dbo",
                table: "DocumentClient",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTags_CreatedBy",
                schema: "dbo",
                table: "DocumentTags",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTags_IdDocument",
                schema: "dbo",
                table: "DocumentTags",
                column: "IdDocument");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTags_Tag",
                schema: "dbo",
                table: "DocumentTags",
                column: "Tag");

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

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                schema: "dbo",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_AssignedToUserId_DueDate",
                schema: "dbo",
                table: "Reminders",
                columns: new[] { "AssignedToUserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ClientId",
                schema: "dbo",
                table: "Reminders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_CreatedByUserId_Status",
                schema: "dbo",
                table: "Reminders",
                columns: new[] { "CreatedByUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_UserId_SearchDate",
                schema: "dbo",
                table: "SearchHistory",
                columns: new[] { "UserId", "SearchDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppReg_UserRoles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ClientPortalTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ClientTags",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "TypeClient",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "MotifBlocage",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "EstArchive",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "Statut",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "DateCreation",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropTable(
                name: "DocumentTags",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DocumentTemplates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HistoriqueModifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Reminders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SearchHistory",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AppReg_Roles",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "ResultatVerification",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropColumn(
                name: "DateVerificationOCR",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropColumn(
                name: "ScoreConfiance",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropColumn(
                name: "DonneesExtraites",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropColumn(
                name: "TexteExtrait",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropColumn(
                name: "StatutValidation",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropTable(
                name: "AppReg_Users",
                schema: "dbo");
        }
    }
}
