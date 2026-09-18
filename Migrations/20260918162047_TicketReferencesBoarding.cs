using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class TicketReferencesBoarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Routes_RouteId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Boarding",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "RouteId",
                table: "Tickets",
                newName: "BoardingId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_RouteId",
                table: "Tickets",
                newName: "IX_Tickets_BoardingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Boardings_BoardingId",
                table: "Tickets",
                column: "BoardingId",
                principalTable: "Boardings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Boardings_BoardingId",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "BoardingId",
                table: "Tickets",
                newName: "RouteId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_BoardingId",
                table: "Tickets",
                newName: "IX_Tickets_RouteId");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Boarding",
                table: "Tickets",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Routes_RouteId",
                table: "Tickets",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
