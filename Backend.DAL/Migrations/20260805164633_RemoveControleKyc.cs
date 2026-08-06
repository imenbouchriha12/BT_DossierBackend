using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveControleKyc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControleKyc",
                schema: "dbo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControleKyc",
                schema: "dbo",
                columns: table => new
                {
                    IdControle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    TraiteeParUserId = table.Column<int>(type: "int", nullable: true),
                    Champ = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Conforme = table.Column<bool>(type: "bit", nullable: false),
                    DateDetection = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTraitement = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdDocument = table.Column<int>(type: "int", nullable: true),
                    Niveau = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceExtraction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValeurDossier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValeurExtrait = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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
        }
    }
}
