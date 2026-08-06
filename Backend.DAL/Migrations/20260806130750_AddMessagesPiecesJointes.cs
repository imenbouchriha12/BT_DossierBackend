using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagesPiecesJointes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PieceJointeNom",
                schema: "dbo",
                table: "MessagesPrives",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PieceJointeType",
                schema: "dbo",
                table: "MessagesPrives",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PieceJointeUrl",
                schema: "dbo",
                table: "MessagesPrives",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PieceJointeNom",
                schema: "dbo",
                table: "MessagesPrives");

            migrationBuilder.DropColumn(
                name: "PieceJointeType",
                schema: "dbo",
                table: "MessagesPrives");

            migrationBuilder.DropColumn(
                name: "PieceJointeUrl",
                schema: "dbo",
                table: "MessagesPrives");
        }
    }
}
