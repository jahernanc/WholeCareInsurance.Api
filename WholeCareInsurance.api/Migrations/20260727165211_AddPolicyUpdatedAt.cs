using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Policies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Backfill para las pólizas ya migradas: el default de EF (año 1) las dejaría
            // todas empatadas al fondo de "Últimas pólizas". EffectiveDate es el mejor proxy
            // disponible de "cuándo se tocó por última vez" para datos migrados sin este campo.
            migrationBuilder.Sql("UPDATE Policies SET UpdatedAt = COALESCE(EffectiveDate, GETUTCDATE())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Policies");
        }
    }
}
