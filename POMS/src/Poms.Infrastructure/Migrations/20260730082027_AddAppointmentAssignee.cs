using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentAssignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedClinicianName",
                table: "Appointments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedClinicianUserId",
                table: "Appointments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AssignedClinicianUserId",
                table: "Appointments",
                column: "AssignedClinicianUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_AssignedClinicianUserId",
                table: "Appointments",
                column: "AssignedClinicianUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AspNetUsers_AssignedClinicianUserId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_AssignedClinicianUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AssignedClinicianName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AssignedClinicianUserId",
                table: "Appointments");
        }
    }
}
