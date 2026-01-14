using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgencyProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancellationDeadlineHours",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDeadlineHours",
                table: "Trips");
        }
    }
}
