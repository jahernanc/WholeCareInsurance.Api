using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Policies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Backfill (§18.1, columna "Registration date"): toda póliza ya guardada tiene al
            // menos un PolicyHistory inicial (RecordStatusChange en Create, tanto en el flujo
            // en vivo como en el script de migración de datos), así que MIN(ChangedAt) es una
            // fuente real, no un sentinel — sin re-leer ningún xlsx.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.CreatedAt = ph.MinChangedAt
                FROM Policies p
                INNER JOIN (
                    SELECT PolicyId, MIN(ChangedAt) AS MinChangedAt
                    FROM PolicyHistories
                    GROUP BY PolicyId
                ) ph ON ph.PolicyId = p.Id;

                -- Red de seguridad para el caso borde (no esperado hoy) de una póliza sin
                -- ningún PolicyHistory: cae a UpdatedAt en vez de quedar con el sentinel 0001-01-01.
                UPDATE Policies
                SET CreatedAt = UpdatedAt
                WHERE CreatedAt = '0001-01-01T00:00:00.0000000';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Policies");
        }
    }
}
