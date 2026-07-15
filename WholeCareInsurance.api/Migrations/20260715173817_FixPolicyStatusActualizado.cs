using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class FixPolicyStatusActualizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "En corrección" no existe en los datos reales (análisis del archivo real de
            // migración, Health/Obamacare) — el 8vo valor real es "Actualizado". Red de
            // seguridad para Test/Prod si ya hubiera pólizas con el valor viejo.
            migrationBuilder.Sql(@"
                UPDATE Policies SET Status = 'Actualizado' WHERE Status = 'En corrección';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Policies SET Status = 'En corrección' WHERE Status = 'Actualizado';
            ");
        }
    }
}
