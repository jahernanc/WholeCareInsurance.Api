using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholeCareInsurance.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPhoneAndCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A diferencia de Policy.UpdatedAt (que usa un sentinel + backfill por SQL desde
            // otra columna), acá no hay ningún proxy mejor para las filas existentes sin dato
            // real (ej. Admin) que "ahora" — GETUTCDATE() en el default deja esas filas con la
            // fecha real de este deploy en vez de un sentinel falso. Los 41 agentes migrados
            // (§15.2) se pisan aparte con su Registration date real vía el backfill de §17.1.
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");
        }
    }
}
