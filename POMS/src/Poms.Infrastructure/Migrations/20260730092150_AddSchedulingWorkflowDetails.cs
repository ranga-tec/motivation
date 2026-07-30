using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingWorkflowDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "FollowUps",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "FollowUps",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryTime",
                table: "Deliveries",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Assessments",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Assessments",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Appointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "DeliveryTime",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Appointments");
        }
    }
}
