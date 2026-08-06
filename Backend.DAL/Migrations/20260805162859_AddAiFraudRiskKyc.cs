using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFraudRiskKyc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControleKyc",
                schema: "dbo",
                columns: table => new
                {
                    IdControle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    IdDocument = table.Column<int>(type: "int", nullable: true),
                    Champ = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValeurDossier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValeurExtrait = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Conforme = table.Column<bool>(type: "bit", nullable: false),
                    Niveau = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceExtraction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateDetection = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TraiteeParUserId = table.Column<int>(type: "int", nullable: true),
                    DateTraitement = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Commentaire = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControleKyc", x => x.IdControle);
                    table.ForeignKey(
                        name: "FK_ControleKyc_AppReg_Users_TraiteeParUserId",
                        column: x => x.TraiteeParUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ControleKyc_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegleAntifraude",
                schema: "dbo",
                columns: table => new
                {
                    IdRegle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Seuil = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SeuilSecondaire = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PeriodeJours = table.Column<int>(type: "int", nullable: false),
                    Poids = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Priorite = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegleAntifraude", x => x.IdRegle);
                });

            migrationBuilder.CreateTable(
                name: "ScoreRisque",
                schema: "dbo",
                columns: table => new
                {
                    IdScore = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Niveau = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ComposantsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Synthese = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DateCalcul = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreRisque", x => x.IdScore);
                    table.ForeignKey(
                        name: "FK_ScoreRisque_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertesFraude",
                schema: "dbo",
                columns: table => new
                {
                    IdAlerte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    IdRegle = table.Column<int>(type: "int", nullable: true),
                    IdMouvement = table.Column<int>(type: "int", nullable: true),
                    NumCompte = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Agence = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Niveau = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DonneesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateDetection = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TraiteeParUserId = table.Column<int>(type: "int", nullable: true),
                    DateTraitement = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Commentaire = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertesFraude", x => x.IdAlerte);
                    table.ForeignKey(
                        name: "FK_AlertesFraude_AppReg_Users_TraiteeParUserId",
                        column: x => x.TraiteeParUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AlertesFraude_Client_IdClient",
                        column: x => x.IdClient,
                        principalSchema: "dbo",
                        principalTable: "Client",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertesFraude_RegleAntifraude_IdRegle",
                        column: x => x.IdRegle,
                        principalSchema: "dbo",
                        principalTable: "RegleAntifraude",
                        principalColumn: "IdRegle",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "RegleAntifraude",
                columns: new[] { "IdRegle", "Actif", "Code", "DateCreation", "DateMaj", "Description", "Nom", "PeriodeJours", "Poids", "Priorite", "Seuil", "SeuilSecondaire", "Type" },
                values: new object[,]
                {
                    { 1, true, "MONTANT_MAXIMAL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si un mouvement dépasse le montant absolu configuré (en dinars).", "Mouvement d'un montant élevé", 30, 30, 1, 50000m, null, "MontantMaximal" },
                    { 2, true, "MONTANT_ANORMAL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si un mouvement dépasse le multiplicateur du montant moyen du client.", "Montant anormal vs historique", 90, 35, 2, 5m, null, "MontantAnormal" },
                    { 3, true, "PIC_DEBIT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si le total débité sur une journée dépasse le multiplicateur de la moyenne journalière.", "Pic de débits", 30, 25, 3, 4m, null, "PicDebit" },
                    { 4, true, "MULTI_COMPTES", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si le client détient plus de comptes que le seuil configuré.", "Multiplication des comptes", 0, 15, 4, 3m, null, "MultiComptes" },
                    { 5, true, "FREQUENCE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si le nombre de mouvements dépasse le seuil sur la période.", "Fréquence anormale de mouvements", 30, 15, 5, 200m, null, "Frequence" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertesFraude_Fingerprint",
                schema: "dbo",
                table: "AlertesFraude",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesFraude_IdClient_DateDetection",
                schema: "dbo",
                table: "AlertesFraude",
                columns: new[] { "IdClient", "DateDetection" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertesFraude_IdRegle",
                schema: "dbo",
                table: "AlertesFraude",
                column: "IdRegle");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesFraude_Statut",
                schema: "dbo",
                table: "AlertesFraude",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesFraude_TraiteeParUserId",
                schema: "dbo",
                table: "AlertesFraude",
                column: "TraiteeParUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ControleKyc_IdClient_DateDetection",
                schema: "dbo",
                table: "ControleKyc",
                columns: new[] { "IdClient", "DateDetection" });

            migrationBuilder.CreateIndex(
                name: "IX_ControleKyc_TraiteeParUserId",
                schema: "dbo",
                table: "ControleKyc",
                column: "TraiteeParUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RegleAntifraude_Code",
                schema: "dbo",
                table: "RegleAntifraude",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreRisque_IdClient_DateCalcul",
                schema: "dbo",
                table: "ScoreRisque",
                columns: new[] { "IdClient", "DateCalcul" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertesFraude",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ControleKyc",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ScoreRisque",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RegleAntifraude",
                schema: "dbo");
        }
    }
}
