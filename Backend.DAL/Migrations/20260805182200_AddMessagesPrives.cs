using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagesPrives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessagesPrives",
                schema: "dbo",
                columns: table => new
                {
                    IdMessage = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpediteurUserId = table.Column<int>(type: "int", nullable: false),
                    DestinataireUserId = table.Column<int>(type: "int", nullable: false),
                    Contenu = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DateEnvoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lu = table.Column<bool>(type: "bit", nullable: false),
                    DateLecture = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagesPrives", x => x.IdMessage);
                    table.ForeignKey(
                        name: "FK_MessagesPrives_AppReg_Users_DestinataireUserId",
                        column: x => x.DestinataireUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessagesPrives_AppReg_Users_ExpediteurUserId",
                        column: x => x.ExpediteurUserId,
                        principalSchema: "dbo",
                        principalTable: "AppReg_Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessagesPrives_DestinataireUserId",
                schema: "dbo",
                table: "MessagesPrives",
                column: "DestinataireUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagesPrives_ExpediteurUserId_DestinataireUserId_DateEnvoi",
                schema: "dbo",
                table: "MessagesPrives",
                columns: new[] { "ExpediteurUserId", "DestinataireUserId", "DateEnvoi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessagesPrives",
                schema: "dbo");
        }
    }
}
