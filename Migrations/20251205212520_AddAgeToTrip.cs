using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgencyProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAgeToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AgeLimition",
                table: "Trips",
                newName: "AgeLimitaion");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AgeLimitaion",
                table: "Trips",
                newName: "AgeLimition");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
