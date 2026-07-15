using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceCompaniesAndPolicyPlanDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceCompany",
                table: "Policies");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "Policies",
                type: "datetime2",
                nullable: true);

            // defaultValue: 0 nunca se usa de verdad — Policies está vacía al momento
            // de este cambio, no hay filas existentes que necesiten backfill.
            migrationBuilder.AddColumn<int>(
                name: "InsuranceCompanyId",
                table: "Policies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InsurancePlan",
                table: "Policies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPremiumAmount",
                table: "Policies",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanType",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxCreditSubsidy",
                table: "Policies",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InsuranceCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceCompanies", x => x.Id);
                });

            // Seed inicial confirmado por el análisis del archivo real de migración
            // (Health/Obamacare) — la lista tenía 31 nombres, no 30, se siembran todos.
            migrationBuilder.InsertData(
                table: "InsuranceCompanies",
                columns: new[] { "Name", "IsActive" },
                values: new object[,]
                {
                    { "Aetna", true },
                    { "Ambetter", true },
                    { "AmeriHealth Caritas", true },
                    { "Ameritas", true },
                    { "Anthem", true },
                    { "Avmed", true },
                    { "Blue Cross Blue Shield", true },
                    { "Bright Health", true },
                    { "Care Source", true },
                    { "Cigna", true },
                    { "Community Health Choice", true },
                    { "Fl Health Care Plans", true },
                    { "Florida Blue", true },
                    { "Florida Blue Dental", true },
                    { "Friday", true },
                    { "Health First", true },
                    { "Kaiser Permanente", true },
                    { "Medicaid", true },
                    { "Molina Healthcare", true },
                    { "One Dental", true },
                    { "Oscar", true },
                    { "Scott And White", true },
                    { "Select Health", true },
                    { "Simply", true },
                    { "U Health Plans", true },
                    { "United", true },
                    { "Usable - Accidents", true },
                    { "Usable - Critical Illness", true },
                    { "Usable - Hospitalization", true },
                    { "Wellcare", true },
                    { "Wellpoint", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_InsuranceCompanyId",
                table: "Policies",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_Name",
                table: "InsuranceCompanies",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Policies_InsuranceCompanies_InsuranceCompanyId",
                table: "Policies",
                column: "InsuranceCompanyId",
                principalTable: "InsuranceCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Policies_InsuranceCompanies_InsuranceCompanyId",
                table: "Policies");

            migrationBuilder.DropTable(
                name: "InsuranceCompanies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_InsuranceCompanyId",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "InsuranceCompanyId",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "InsurancePlan",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "MonthlyPremiumAmount",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "TaxCreditSubsidy",
                table: "Policies");

            migrationBuilder.AddColumn<string>(
                name: "InsuranceCompany",
                table: "Policies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
