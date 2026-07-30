using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeProfilesAndRestrictedClinicalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRestricted",
                table: "PatientDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRestricted",
                table: "FollowUps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRestricted",
                table: "Fittings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRestricted",
                table: "Episodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRestricted",
                table: "EpisodeDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRestricted",
                table: "Deliveries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRestricted",
                table: "Assessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmployeeProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WorkPhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CanAccessRestrictedClinicalData = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_IsRestricted",
                table: "PatientDocuments",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_IsRestricted",
                table: "FollowUps",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_Fittings_IsRestricted",
                table: "Fittings",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_IsRestricted",
                table: "Episodes",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeDocuments_IsRestricted",
                table: "EpisodeDocuments",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_IsRestricted",
                table: "Deliveries",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_IsRestricted",
                table: "Assessments",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_EmployeeNumber",
                table: "EmployeeProfiles",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_UserId",
                table: "EmployeeProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PatientDocuments_IsRestricted",
                table: "PatientDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_IsRestricted",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_Fittings_IsRestricted",
                table: "Fittings");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_IsRestricted",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeDocuments_IsRestricted",
                table: "EpisodeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_IsRestricted",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_IsRestricted",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "IsRestricted",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "IsRestricted",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "IsRestricted",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "IsRestricted",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "IsRestricted",
                table: "EpisodeDocuments");

            migrationBuilder.DropColumn(
                name: "IsRestricted",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "IsRestricted",
                table: "Assessments");
        }
    }
}
