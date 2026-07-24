using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class FixMockCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Origins_Destinations_DestinationId",
                table: "Origins");

            migrationBuilder.DropIndex(
                name: "IX_Origins_DestinationId",
                table: "Origins");

            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "Origins");

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 1,
                column: "CityAcronym",
                value: "Indi");

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 2,
                column: "CityAcronym",
                value: "Udia");

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 3,
                column: "CityAcronym",
                value: "Reri");

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 4,
                column: "CityAcronym",
                value: "Ura");

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 1,
                column: "CityAcronym",
                value: "Indi");

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 2,
                column: "CityAcronym",
                value: "Udia");

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 3,
                column: "CityAcronym",
                value: "Reri");

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 4,
                column: "CityAcronym",
                value: "Ura");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationId",
                table: "Origins",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 1,
                column: "CityAcronym",
                value: "");

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 2,
                column: "CityAcronym",
                value: "");

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 3,
                column: "CityAcronym",
                value: "");

            migrationBuilder.UpdateData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 4,
                column: "CityAcronym",
                value: "");

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CityAcronym", "DestinationId" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CityAcronym", "DestinationId" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CityAcronym", "DestinationId" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CityAcronym", "DestinationId" },
                values: new object[] { "", null });

            migrationBuilder.CreateIndex(
                name: "IX_Origins_DestinationId",
                table: "Origins",
                column: "DestinationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Origins_Destinations_DestinationId",
                table: "Origins",
                column: "DestinationId",
                principalTable: "Destinations",
                principalColumn: "Id");
        }
    }
}
