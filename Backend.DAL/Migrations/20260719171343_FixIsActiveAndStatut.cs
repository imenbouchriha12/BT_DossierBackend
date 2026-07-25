using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixIsActiveAndStatut : Migration
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

            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "dbo",
                columns: table => new
                {
                    IdClient = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeClient = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateNaissance = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sexe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nationalite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ville = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstArchive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.IdClient);
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
                        name: "FK_ClientTags_Clients_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetailClients",
                schema: "dbo",
                columns: table => new
                {
                    IdDetail = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    Profession = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Employeur = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecteurActivite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevenuMensuel = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SituationFamiliale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypeResidence = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailClients", x => x.IdDetail);
                    table.ForeignKey(
                        name: "FK_DetailClients_Clients_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentClients",
                schema: "dbo",
                columns: table => new
                {
                    IdDocument = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    TypeDocument = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomFichier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheminFichier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAjout = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatutValidation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentClients", x => x.IdDocument);
                    table.ForeignKey(
                        name: "FK_DocumentClients_AppReg_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_DocumentClients_Clients_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_DocumentTags_DocumentClients_IdDocument",
                        column: x => x.IdDocument,
                        principalSchema: "dbo",
                        principalTable: "DocumentClients",
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
                name: "IX_Clients_CodeClient",
                schema: "dbo",
                table: "Clients",
                column: "CodeClient",
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
                name: "IX_DetailClients_IdClient",
                schema: "dbo",
                table: "DetailClients",
                column: "IdClient",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentClients_IdClient",
                schema: "dbo",
                table: "DocumentClients",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentClients_UploadedByUserId",
                schema: "dbo",
                table: "DocumentClients",
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
                name: "IX_Notifications_UserId",
                schema: "dbo",
                table: "Notifications",
                column: "UserId");

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
                name: "ClientTags",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DetailClients",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DocumentTags",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HistoriqueModifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SearchHistory",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AppReg_Roles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DocumentClients",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AppReg_Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Clients",
                schema: "dbo");
        }
    }
}
