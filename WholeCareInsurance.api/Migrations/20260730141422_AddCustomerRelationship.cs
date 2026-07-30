using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitularCustomerId = table.Column<int>(type: "int", nullable: false),
                    DependentCustomerId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerRelationships_Customers_DependentCustomerId",
                        column: x => x.DependentCustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerRelationships_Customers_TitularCustomerId",
                        column: x => x.TitularCustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRelationships_DependentCustomerId",
                table: "CustomerRelationships",
                column: "DependentCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRelationships_TitularCustomerId_DependentCustomerId",
                table: "CustomerRelationships",
                columns: new[] { "TitularCustomerId", "DependentCustomerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerRelationships");
        }
    }
}
