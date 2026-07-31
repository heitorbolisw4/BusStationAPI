using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class FixBoardingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boardings_Tickets_TicketId",
                table: "Boardings");

            migrationBuilder.DropIndex(
                name: "IX_Boardings_TicketId",
                table: "Boardings");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "Boardings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "Boardings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Boardings_TicketId",
                table: "Boardings",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boardings_Tickets_TicketId",
                table: "Boardings",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
