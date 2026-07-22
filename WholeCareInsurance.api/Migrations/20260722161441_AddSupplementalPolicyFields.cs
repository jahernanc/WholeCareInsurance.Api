using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplementalPolicyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AuthorizeMarketingInfo",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AuthorizedAutomaticPayment",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AutoPaymentDay",
                table: "Policies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountType",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EligibleForMedicare",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasExistingDentalCoverage",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InsuredIsAccountHolder",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InsuredPaysThePremium",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReplacingDentalCoverage",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeName",
                table: "Policies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeRelationship",
                table: "Policies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoutingNumber",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AuthorizeMarketingInfo",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AuthorizedAutomaticPayment",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AutoPaymentDay",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "BankAccountType",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "EligibleForMedicare",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "HasExistingDentalCoverage",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "InsuredIsAccountHolder",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "InsuredPaysThePremium",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "IsReplacingDentalCoverage",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "RepresentativeName",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "RepresentativeRelationship",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "RoutingNumber",
                table: "Policies");
        }
    }
}
