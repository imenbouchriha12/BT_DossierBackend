using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowValidation",
                schema: "dbo",
                columns: table => new
                {
                    IdWorkflow = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDocument = table.Column<int>(type: "int", nullable: false),
                    StatutGlobal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EtapeCourante = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MotifRejet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowValidation", x => x.IdWorkflow);
                    table.ForeignKey(
                        name: "FK_WorkflowValidation_AppReg_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkflowValidation_DocumentClient_IdDocument",
                        column: x => x.IdDocument,
                        principalSchema: "dbo",
                        principalTable: "DocumentClient",
                        principalColumn: "IdDocument",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EtapeValidation",
                schema: "dbo",
                columns: table => new
                {
                    IdEtape = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdWorkflow = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    RoleRequis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    NomUser = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapeValidation", x => x.IdEtape);
                    table.ForeignKey(
                        name: "FK_EtapeValidation_AppReg_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EtapeValidation_WorkflowValidation_IdWorkflow",
                        column: x => x.IdWorkflow,
                        principalSchema: "dbo",
                        principalTable: "WorkflowValidation",
                        principalColumn: "IdWorkflow",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtapeValidation_IdWorkflow_Ordre",
                schema: "dbo",
                table: "EtapeValidation",
                columns: new[] { "IdWorkflow", "Ordre" });

            migrationBuilder.CreateIndex(
                name: "IX_EtapeValidation_UserId",
                schema: "dbo",
                table: "EtapeValidation",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowValidation_CreatedByUserId",
                schema: "dbo",
                table: "WorkflowValidation",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowValidation_EtapeCourante",
                schema: "dbo",
                table: "WorkflowValidation",
                column: "EtapeCourante");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowValidation_IdDocument_StatutGlobal",
                schema: "dbo",
                table: "WorkflowValidation",
                columns: new[] { "IdDocument", "StatutGlobal" });

            // Rétro-compatibilité : crée un workflow pour chaque document existant
            // afin que la chaîne Agent → Superviseur → Admin soit disponible partout.
            migrationBuilder.Sql(@"
                DECLARE @UtcNow datetime2 = SYSUTCDATETIME();

                INSERT INTO dbo.WorkflowValidation (IdDocument, StatutGlobal, EtapeCourante, CreatedByUserId, DateCreation, DateMaj)
                SELECT d.IdDocument,
                       CASE d.StatutValidation
                            WHEN N'Validé' THEN N'Valide'
                            WHEN N'Rejeté' THEN N'Rejete'
                            ELSE N'EnCours' END,
                       CASE WHEN d.StatutValidation = N'En attente' THEN N'Agent' ELSE NULL END,
                       d.UploadedByUserId,
                       @UtcNow, @UtcNow
                FROM dbo.DocumentClient d;

                INSERT INTO dbo.EtapeValidation (IdWorkflow, Ordre, RoleRequis, Libelle, Statut, DecisionDate, Commentaire)
                SELECT w.IdWorkflow, 1, N'Agent', N'Validation Agent',
                       CASE WHEN w.StatutGlobal = N'Valide' THEN N'Valide'
                            WHEN w.StatutGlobal = N'Rejete' THEN N'Rejete'
                            ELSE N'EnAttente' END,
                       CASE WHEN w.StatutGlobal IN (N'Valide', N'Rejete') THEN @UtcNow ELSE NULL END,
                       CASE WHEN w.StatutGlobal = N'Rejete' THEN N'Rejeté avant mise en place du workflow' ELSE NULL END
                FROM dbo.WorkflowValidation w
                UNION ALL
                SELECT w.IdWorkflow, 2, N'Superviseur', N'Validation Superviseur',
                       CASE WHEN w.StatutGlobal = N'Valide' THEN N'Valide'
                            WHEN w.StatutGlobal = N'Rejete' THEN N'NonApplicable'
                            ELSE N'EnAttente' END,
                       CASE WHEN w.StatutGlobal = N'Valide' THEN @UtcNow ELSE NULL END,
                       NULL
                FROM dbo.WorkflowValidation w
                UNION ALL
                SELECT w.IdWorkflow, 3, N'Admin', N'Validation Admin',
                       CASE WHEN w.StatutGlobal = N'Valide' THEN N'Valide'
                            WHEN w.StatutGlobal = N'Rejete' THEN N'NonApplicable'
                            ELSE N'EnAttente' END,
                       CASE WHEN w.StatutGlobal = N'Valide' THEN @UtcNow ELSE NULL END,
                       NULL
                FROM dbo.WorkflowValidation w;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtapeValidation",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WorkflowValidation",
                schema: "dbo");
        }
    }
}
