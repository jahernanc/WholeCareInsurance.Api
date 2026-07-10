using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentAssignmentAndDemographics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEncargado",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssistantAgentId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordAgentId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Customers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AgentId",
                table: "Customers",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AssistantAgentId",
                table: "Customers",
                column: "AssistantAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_RecordAgentId",
                table: "Customers",
                column: "RecordAgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_AgentId",
                table: "Customers",
                column: "AgentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_AssistantAgentId",
                table: "Customers",
                column: "AssistantAgentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_RecordAgentId",
                table: "Customers",
                column: "RecordAgentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_AgentId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_AssistantAgentId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_RecordAgentId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_AgentId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_AssistantAgentId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_RecordAgentId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsEncargado",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AssistantAgentId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "County",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RecordAgentId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Customers");
        }
    }
}
