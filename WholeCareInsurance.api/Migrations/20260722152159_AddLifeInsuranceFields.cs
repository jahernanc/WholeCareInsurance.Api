using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddLifeInsuranceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalInformation",
                table: "Policies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AdditionalOrAlternatePolicy",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalOrAlternatePolicyDetail",
                table: "Policies",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingType",
                table: "Policies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConsentSigned",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasExistingLifeInsurance",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReplacingExistingPolicy",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsMedicalRequirements",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicianAddress",
                table: "Policies",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicianName",
                table: "Policies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedPeriodicModalPremium",
                table: "Policies",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PremiumFrequency",
                table: "Policies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProvideComparativeInfoForm",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceOfFunds",
                table: "Policies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnderwritingRequirements",
                table: "Policies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsingFundsFromInforcePolicy",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BackDateToSaveAge",
                table: "Customers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfBirth",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CurrentlyEmployed",
                table: "Customers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverLicenseNumber",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasDriverLicense",
                table: "Customers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Height",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HouseholdIncome",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HouseholdNetWorth",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MilitaryOrganizationMember",
                table: "Customers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetWorth",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SpentMoreThan4MonthsAbroad",
                table: "Customers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weight",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PolicyBeneficiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<int>(type: "int", nullable: false),
                    TypeOfRelationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SocialSecurityNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyBeneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyBeneficiaries_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBeneficiaries_PolicyId",
                table: "PolicyBeneficiaries",
                column: "PolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AdditionalInformation",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AdditionalOrAlternatePolicy",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AdditionalOrAlternatePolicyDetail",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "BillingType",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "ConsentSigned",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "HasExistingLifeInsurance",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "IsReplacingExistingPolicy",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "NeedsMedicalRequirements",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "PhysicianAddress",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "PhysicianName",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "PlannedPeriodicModalPremium",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "PremiumFrequency",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "ProvideComparativeInfoForm",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "SourceOfFunds",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "UnderwritingRequirements",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "UsingFundsFromInforcePolicy",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BackDateToSaveAge",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CountryOfBirth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CurrentlyEmployed",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DriverLicenseNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "HasDriverLicense",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "HouseholdIncome",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "HouseholdNetWorth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MilitaryOrganizationMember",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NetWorth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SpentMoreThan4MonthsAbroad",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Customers");
        }
    }
}
