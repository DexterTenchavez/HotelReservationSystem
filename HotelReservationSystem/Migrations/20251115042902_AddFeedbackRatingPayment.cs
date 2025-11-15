using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackRatingPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Rooms",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalRatings",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Reservations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Reservations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Reservations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Reservations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatingDate",
                table: "Reservations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "Reservations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 1,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 2,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 3,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 4,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 5,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 6,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 7,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 8,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 9,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 10,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 11,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 12,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 13,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 14,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 15,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 16,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 17,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 18,
                columns: new[] { "AverageRating", "TotalRatings" },
                values: new object[] { 4.2m, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "TotalRatings",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RatingDate",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Reservations");
        }
    }
}
