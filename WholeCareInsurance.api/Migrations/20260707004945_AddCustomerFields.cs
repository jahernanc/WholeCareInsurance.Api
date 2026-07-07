using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear dev data so the unique index on SocialSecurityNumber can be created
            migrationBuilder.Sql("DELETE FROM [Customers]");

            migrationBuilder.DropIndex(
                name: "IX_Customers_DocumentNumber",
                table: "Customers");

            // Name → LastName (was a full-name field; split into FirstName + LastName)
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Customers",
                newName: "LastName");

            // DocumentNumber → SocialSecurityNumber (logical rename)
            migrationBuilder.RenameColumn(
                name: "DocumentNumber",
                table: "Customers",
                newName: "SocialSecurityNumber");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SocialSecurityNumber",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Customers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MigrationStatus",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Otro");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SocialSecurityNumber",
                table: "Customers",
                column: "SocialSecurityNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_SocialSecurityNumber",
                table: "Customers");

            migrationBuilder.DropColumn(name: "FirstName", table: "Customers");
            migrationBuilder.DropColumn(name: "DateOfBirth", table: "Customers");
            migrationBuilder.DropColumn(name: "Address", table: "Customers");
            migrationBuilder.DropColumn(name: "Phone", table: "Customers");
            migrationBuilder.DropColumn(name: "MigrationStatus", table: "Customers");

            migrationBuilder.RenameColumn(
                name: "SocialSecurityNumber",
                table: "Customers",
                newName: "DocumentNumber");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Customers",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentNumber",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DocumentNumber",
                table: "Customers",
                column: "DocumentNumber",
                unique: true);
        }
    }
}
