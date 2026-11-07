using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AgregarBorradoLogicoPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "paciente",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activo",
                table: "paciente");
        }
    }
}
