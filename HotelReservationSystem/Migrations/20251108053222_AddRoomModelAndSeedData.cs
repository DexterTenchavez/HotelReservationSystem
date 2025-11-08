using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomModelAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomNumber",
                table: "Reservations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoomNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoomType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PricePerNight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxGuests = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Features = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "RoomId", "Description", "Features", "IsAvailable", "MaxGuests", "PricePerNight", "RoomNumber", "RoomType" },
                values: new object[,]
                {
                    { 1, "Cozy single room with basic amenities", null, true, 1, 1000m, "101", "Single" },
                    { 2, "Cozy single room with city view", null, true, 1, 1000m, "102", "Single" },
                    { 3, "Cozy single room with garden view", null, true, 1, 1000m, "103", "Single" },
                    { 4, "Single room with work desk", null, true, 1, 1000m, "104", "Single" },
                    { 5, "Single room with mountain view", null, true, 1, 1000m, "105", "Single" },
                    { 6, "Single room with premium amenities", null, true, 1, 1000m, "106", "Single" },
                    { 7, "Spacious double room with queen bed", null, true, 2, 2000m, "201", "Double" },
                    { 8, "Spacious double room with twin beds", null, true, 2, 2000m, "202", "Double" },
                    { 9, "Spacious double room with balcony", null, true, 2, 2000m, "203", "Double" },
                    { 10, "Double room with city skyline view", null, true, 2, 2000m, "204", "Double" },
                    { 11, "Double room with extra seating area", null, true, 2, 2000m, "205", "Double" },
                    { 12, "Double room with premium bedding", null, true, 2, 2000m, "206", "Double" },
                    { 13, "Luxury suite with living area", null, true, 4, 3500m, "301", "Suite" },
                    { 14, "Luxury suite with jacuzzi", null, true, 4, 3500m, "302", "Suite" },
                    { 15, "Presidential suite with ocean view", null, true, 4, 3500m, "303", "Suite" },
                    { 16, "Executive suite with kitchenette", null, true, 4, 3500m, "304", "Suite" },
                    { 17, "Family suite with separate bedrooms", null, true, 4, 3500m, "305", "Suite" },
                    { 18, "Honeymoon suite with private balcony", null, true, 4, 3500m, "306", "Suite" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId",
                table: "Reservations",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Rooms_RoomId",
                table: "Reservations",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Rooms_RoomId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RoomId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RoomNumber",
                table: "Reservations");
        }
    }
}
