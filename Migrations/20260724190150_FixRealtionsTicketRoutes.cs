using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class FixRealtionsTicketRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Boardings_TicketId",
                table: "Boardings");

            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "Routes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_TicketId",
                table: "Routes",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boardings_TicketId",
                table: "Boardings",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Tickets_TicketId",
                table: "Routes",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Tickets_TicketId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_TicketId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Boardings_TicketId",
                table: "Boardings");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "Routes");

            migrationBuilder.CreateIndex(
                name: "IX_Boardings_TicketId",
                table: "Boardings",
                column: "TicketId",
                unique: true);
        }
    }
}
