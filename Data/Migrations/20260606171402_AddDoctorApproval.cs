using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmEasyClone_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Doctors",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Doctors");
        }
    }
}
