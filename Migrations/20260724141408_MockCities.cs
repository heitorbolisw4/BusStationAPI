using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class MockCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "City",
                columns: new[] { "Id", "Acronym", "CityName", "State" },
                values: new object[,]
                {
                    { 1, "Indi", "Indianopolis", "MG" },
                    { 2, "Udia", "Uberlandia", "MG" },
                    { 3, "Reri", "Araguari", "MG" },
                    { 4, "Ura", "Uberaba", "MG" }
                });

            migrationBuilder.InsertData(
                table: "Destinations",
                columns: new[] { "Id", "CityAcronym", "CityId" },
                values: new object[,]
                {
                    { 1, "", 1 },
                    { 2, "", 2 },
                    { 3, "", 3 },
                    { 4, "", 4 }
                });

            migrationBuilder.InsertData(
                table: "Origins",
                columns: new[] { "Id", "CityAcronym", "CityId", "DestinationId" },
                values: new object[,]
                {
                    { 1, "", 1, null },
                    { 2, "", 2, null },
                    { 3, "", 3, null },
                    { 4, "", 4, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Destinations",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "City",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "City",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "City",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "City",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
