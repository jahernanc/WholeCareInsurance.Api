using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAgency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Agency",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Agency",
                table: "Users");
        }
    }
}
