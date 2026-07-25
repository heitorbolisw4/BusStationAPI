using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationsBoardingRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Boardings_BoardingId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_BoardingId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "BoardingId",
                table: "Routes");

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Boardings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Boardings_RouteId",
                table: "Boardings",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boardings_Routes_RouteId",
                table: "Boardings",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boardings_Routes_RouteId",
                table: "Boardings");

            migrationBuilder.DropIndex(
                name: "IX_Boardings_RouteId",
                table: "Boardings");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Boardings");

            migrationBuilder.AddColumn<int>(
                name: "BoardingId",
                table: "Routes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_BoardingId",
                table: "Routes",
                column: "BoardingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Boardings_BoardingId",
                table: "Routes",
                column: "BoardingId",
                principalTable: "Boardings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
