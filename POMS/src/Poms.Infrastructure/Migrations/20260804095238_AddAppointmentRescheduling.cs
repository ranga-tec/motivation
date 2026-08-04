using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentRescheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PreviousAppointmentDate",
                table: "Appointments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "PreviousAppointmentTime",
                table: "Appointments",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RescheduleReason",
                table: "Appointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduledAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousAppointmentDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PreviousAppointmentTime",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduleReason",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduledAt",
                table: "Appointments");
        }
    }
}
