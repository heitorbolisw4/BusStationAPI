using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusStation_API.Migrations
{
    /// <inheritdoc />
    public partial class IsUnique_StateToAcronym_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_State",
                table: "City");

            migrationBuilder.CreateIndex(
                name: "IX_City_Acronym",
                table: "City",
                column: "Acronym",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Acronym",
                table: "City");

            migrationBuilder.CreateIndex(
                name: "IX_City_State",
                table: "City",
                column: "State",
                unique: true);
        }
    }
}
