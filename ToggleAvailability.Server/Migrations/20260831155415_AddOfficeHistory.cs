using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToggleAvailability.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalTimeInOffice",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "OfficeHistories",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    TimeInOffice = table.Column<long>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeHistories", x => new { x.UserId, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "OfficeHistoryOutOfOffice",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeHistoryOutOfOffice", x => new { x.UserId, x.Date, x.Status });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficeHistories");

            migrationBuilder.DropTable(
                name: "OfficeHistoryOutOfOffice");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TotalTimeInOffice",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
