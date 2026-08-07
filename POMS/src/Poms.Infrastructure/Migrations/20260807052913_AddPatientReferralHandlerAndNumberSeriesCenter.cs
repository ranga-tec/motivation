using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientReferralHandlerAndNumberSeriesCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedClinicianName",
                table: "Patients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedClinicianUserId",
                table: "Patients",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralPersonContactNumber",
                table: "Patients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralPersonName",
                table: "Patients",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CenterId",
                table: "NumberSeries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AssignedClinicianUserId",
                table: "Patients",
                column: "AssignedClinicianUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_AspNetUsers_AssignedClinicianUserId",
                table: "Patients",
                column: "AssignedClinicianUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_AspNetUsers_AssignedClinicianUserId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_AssignedClinicianUserId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "AssignedClinicianName",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "AssignedClinicianUserId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ReferralPersonContactNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ReferralPersonName",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CenterId",
                table: "NumberSeries");
        }
    }
}
