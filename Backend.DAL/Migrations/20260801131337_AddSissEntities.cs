using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSissEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origine",
                schema: "dbo",
                table: "DocumentClient",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereSynchroSISS",
                schema: "dbo",
                table: "Client",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstImporteSISS",
                schema: "dbo",
                table: "Client",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HashReferentiel",
                schema: "dbo",
                table: "Client",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SynchronisationLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NbInsert = table.Column<int>(type: "int", nullable: false),
                    NbUpdate = table.Column<int>(type: "int", nullable: false),
                    NbRejet = table.Column<int>(type: "int", nullable: false),
                    DureeMs = table.Column<long>(type: "bigint", nullable: false),
                    Declencheur = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true),
                    MessageErreur = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchronisationLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SynchronisationRejet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSynchronisation = table.Column<int>(type: "int", nullable: false),
                    Fichier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroLigne = table.Column<int>(type: "int", nullable: true),
                    Motif = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContenuBrut = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchronisationRejet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SynchronisationRejet_SynchronisationLog_IdSynchronisation",
                        column: x => x.IdSynchronisation,
                        principalTable: "SynchronisationLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compte_IdClient",
                schema: "dbo",
                table: "Compte",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_Compte_NumCompte_Agence",
                schema: "dbo",
                table: "Compte",
                columns: new[] { "NumCompte", "Agence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cours_Journee_Devise",
                schema: "dbo",
                table: "Cours",
                columns: new[] { "Journee", "Devise" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SynchronisationLog_DateDebut",
                table: "SynchronisationLog",
                column: "DateDebut");

            migrationBuilder.CreateIndex(
                name: "IX_SynchronisationRejet_IdSynchronisation",
                table: "SynchronisationRejet",
                column: "IdSynchronisation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cours_Journee_Devise",
                schema: "dbo",
                table: "Cours");

            migrationBuilder.DropIndex(
                name: "IX_Compte_NumCompte_Agence",
                schema: "dbo",
                table: "Compte");

            migrationBuilder.DropIndex(
                name: "IX_Compte_IdClient",
                schema: "dbo",
                table: "Compte");

            migrationBuilder.DropTable(
                name: "SynchronisationRejet");

            migrationBuilder.DropTable(
                name: "SynchronisationLog");

            migrationBuilder.DropColumn(
                name: "Origine",
                schema: "dbo",
                table: "DocumentClient");

            migrationBuilder.DropColumn(
                name: "DateDerniereSynchroSISS",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "EstImporteSISS",
                schema: "dbo",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "HashReferentiel",
                schema: "dbo",
                table: "Client");
        }
    }
}
