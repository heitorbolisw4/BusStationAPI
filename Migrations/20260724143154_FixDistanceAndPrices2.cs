using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class FixDistanceAndPrices2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Distances",
                columns: new[] { "Id", "DestinationId", "Kilometers", "OriginId" },
                values: new object[,]
                {
                    { 1, 2, 60, 1 },
                    { 2, 1, 60, 2 },
                    { 3, 3, 45, 2 },
                    { 4, 2, 45, 3 },
                    { 5, 4, 100, 2 },
                    { 6, 2, 100, 4 }
                });

            migrationBuilder.InsertData(
                table: "Prices",
                columns: new[] { "Id", "DistanceId", "PricePerKm" },
                values: new object[,]
                {
                    { 1, 1, 0.5f },
                    { 2, 2, 0.5f },
                    { 3, 3, 0.35f },
                    { 4, 4, 0.35f },
                    { 5, 5, 0.9f },
                    { 6, 6, 0.9f }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Prices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Prices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Prices",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Prices",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Prices",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Prices",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Distances",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Distances",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Distances",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Distances",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Distances",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Distances",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
