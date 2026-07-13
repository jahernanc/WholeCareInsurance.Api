using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap the old free-text Status values to the new 8-value enum.
            // ELSE Status is a safety net: any value not explicitly mapped is left
            // untouched rather than blanked out, so no row can silently lose its status.
            migrationBuilder.Sql(@"
                UPDATE Policies SET Status = CASE
                    WHEN Status = 'Cancelled' THEN 'Cancelado'
                    WHEN Status IN ('Active', 'activa') THEN 'Procesado'
                    WHEN Status = 'Expired' THEN 'Cancelado'
                    ELSE Status
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reverse mapping. Lossy: both 'Cancelled' and 'Expired' collapsed
            // into 'Cancelado' going forward, so this cannot distinguish which was which.
            migrationBuilder.Sql(@"
                UPDATE Policies SET Status = CASE
                    WHEN Status = 'Cancelado' THEN 'Cancelled'
                    WHEN Status = 'Procesado' THEN 'Active'
                    ELSE Status
                END;
            ");
        }
    }
}
