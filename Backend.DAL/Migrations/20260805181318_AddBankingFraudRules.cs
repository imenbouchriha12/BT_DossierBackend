using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBankingFraudRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "dbo",
                table: "RegleAntifraude",
                columns: new[] { "IdRegle", "Actif", "Code", "DateCreation", "DateMaj", "Description", "Nom", "PeriodeJours", "Poids", "Priorite", "Seuil", "SeuilSecondaire", "Type" },
                values: new object[,]
                {
                    { 6, true, "DECOUPAGE_ANORMAL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si plusieurs mouvements juste sous le seuil de déclaration se répètent sur le même compte (smurfing).", "Structuration (découpage de fonds)", 14, 35, 6, 10000m, 7m, "DecoupageAnormal" },
                    { 7, true, "RAPID_IN_OUT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si un crédit est suivi d'un débit équivalent dans une fenêtre courte.", "Passage de fonds (aller-retour)", 15, 30, 7, 5000m, 2m, "RapidInOut" },
                    { 8, true, "COMPTE_REACTIVE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si un compte inactif depuis longtemps reçoit soudainement un crédit important.", "Compte dormant réactivé", 200, 30, 8, 180m, 50000m, "CompteReactive" },
                    { 9, true, "ACTIVITE_NOCTURNE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alerte si de nombreux mouvements ont lieu la nuit ou le week-end.", "Activité hors horaires bancaires", 30, 20, 9, 5m, 500m, "ActiviteNocturne" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "RegleAntifraude",
                keyColumn: "IdRegle",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "RegleAntifraude",
                keyColumn: "IdRegle",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "RegleAntifraude",
                keyColumn: "IdRegle",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "RegleAntifraude",
                keyColumn: "IdRegle",
                keyValue: 9);
        }
    }
}
