using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class CityRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Distances_DistanceId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DistanceId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DistanceId",
                table: "Tickets");

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Origins",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Destinations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CityName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Acronym = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DistanceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Distances_DistanceId",
                        column: x => x.DistanceId,
                        principalTable: "Distances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RouteId",
                table: "Tickets",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Origins_CityId",
                table: "Origins",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_CityId",
                table: "Destinations",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_City_State",
                table: "City",
                column: "State",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_DistanceId",
                table: "Routes",
                column: "DistanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Destinations_City_CityId",
                table: "Destinations",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Origins_City_CityId",
                table: "Origins",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Routes_RouteId",
                table: "Tickets",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Destinations_City_CityId",
                table: "Destinations");

            migrationBuilder.DropForeignKey(
                name: "FK_Origins_City_CityId",
                table: "Origins");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Routes_RouteId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "City");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_RouteId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Origins_CityId",
                table: "Origins");

            migrationBuilder.DropIndex(
                name: "IX_Destinations_CityId",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Origins");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Destinations");

            migrationBuilder.AddColumn<Guid>(
                name: "DistanceId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DistanceId",
                table: "Tickets",
                column: "DistanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Distances_DistanceId",
                table: "Tickets",
                column: "DistanceId",
                principalTable: "Distances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
