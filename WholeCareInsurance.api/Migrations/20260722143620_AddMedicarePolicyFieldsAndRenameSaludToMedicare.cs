using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicarePolicyFieldsAndRenameSaludToMedicare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasMedicaid",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicaidLevel",
                table: "Policies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalCorporation",
                table: "Policies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReferredToMedicalCorporation",
                table: "Policies",
                type: "bit",
                nullable: true);

            // Rename del valor de Type "Salud" -> "Medicare" (§12.10/§1.1): no es un cambio
            // de esquema, solo de datos, así que EF no lo genera automáticamente.
            migrationBuilder.Sql("UPDATE [Policies] SET [Type] = 'Medicare' WHERE [Type] = 'Salud';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Policies] SET [Type] = 'Salud' WHERE [Type] = 'Medicare';");

            migrationBuilder.DropColumn(
                name: "HasMedicaid",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "MedicaidLevel",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "MedicalCorporation",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "ReferredToMedicalCorporation",
                table: "Policies");
        }
    }
}
