using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateObamaCareToHealthInsuranceACA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Policies SET Type = 'Health Insurance (ACA)' WHERE Type = 'Obama Care'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Policies SET Type = 'Obama Care' WHERE Type = 'Health Insurance (ACA)'");
        }
    }
}
