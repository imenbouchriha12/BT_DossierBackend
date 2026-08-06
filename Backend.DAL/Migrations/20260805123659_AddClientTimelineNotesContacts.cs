using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddClientTimelineNotesContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactClient",
                schema: "dbo",
                columns: table => new
                {
                    IdContact = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    TypeContact = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Telephone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Relation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactClient", x => x.IdContact);
                    table.ForeignKey(
                        name: "FK_ContactClient_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvenementClient",
                schema: "dbo",
                columns: table => new
                {
                    IdEvenement = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DonneesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true),
                    UtilisateurNom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateEvenement = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdLien = table.Column<int>(type: "int", nullable: true),
                    TypeLien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementClient", x => x.IdEvenement);
                    table.ForeignKey(
                        name: "FK_EvenementClient_AppReg_Users_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvenementClient_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteClient",
                schema: "dbo",
                columns: table => new
                {
                    IdNote = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Contenu = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreeParUserId = table.Column<int>(type: "int", nullable: false),
                    CreeParNom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifieParUserId = table.Column<int>(type: "int", nullable: true),
                    ModifieParNom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstEpinglee = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteClient", x => x.IdNote);
                    table.ForeignKey(
                        name: "FK_NoteClient_AppReg_Users_CreeParUserId",
                        column: x => x.CreeParUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NoteClient_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactClient_IdClient_TypeContact",
                schema: "dbo",
                table: "ContactClient",
                columns: new[] { "IdClient", "TypeContact" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementClient_IdClient_DateEvenement",
                schema: "dbo",
                table: "EvenementClient",
                columns: new[] { "IdClient", "DateEvenement" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementClient_Type",
                schema: "dbo",
                table: "EvenementClient",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementClient_UtilisateurId",
                schema: "dbo",
                table: "EvenementClient",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteClient_CreeParUserId",
                schema: "dbo",
                table: "NoteClient",
                column: "CreeParUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteClient_IdClient_DateCreation",
                schema: "dbo",
                table: "NoteClient",
                columns: new[] { "IdClient", "DateCreation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactClient",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EvenementClient",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "NoteClient",
                schema: "dbo");
        }
    }
}
