using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyPeriodAndApplicants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfApplicants",
                table: "Policies",
                type: "int",
                nullable: true);

            // Default al año actual (no 0) por si esta migración corre alguna vez
            // contra una base ya con pólizas cargadas (Test/Prod) — 0 no sería un
            // Período válido para ninguna póliza preexistente.
            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "Policies",
                type: "int",
                nullable: false,
                defaultValue: 2026);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfApplicants",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "Policies");
        }
    }
}
