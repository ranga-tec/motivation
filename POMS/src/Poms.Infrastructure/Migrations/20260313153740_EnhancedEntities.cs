using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Provinces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AmputationTypeOther",
                table: "ProstheticEpisodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Patients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedText",
                table: "PatientDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "PatientDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "OcrLanguage",
                table: "PatientDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OcrProcessedAt",
                table: "PatientDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrStatus",
                table: "PatientDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Adjustments",
                table: "Fittings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FittingNumber",
                table: "Fittings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextFittingDate",
                table: "Fittings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextSteps",
                table: "Fittings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientFeedback",
                table: "Fittings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedBy",
                table: "Fittings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Fittings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtractedText",
                table: "EpisodeDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrLanguage",
                table: "EpisodeDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OcrProcessedAt",
                table: "EpisodeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrStatus",
                table: "EpisodeDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Districts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ComponentCatalogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Centers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CenterId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Centers",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Centers",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Centers",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Districts",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Districts",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Districts",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Districts",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Districts",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Provinces",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Provinces",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Provinces",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Provinces",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Provinces",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsActive",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CenterId",
                table: "AspNetUsers",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Centers_CenterId",
                table: "AspNetUsers",
                column: "CenterId",
                principalTable: "Centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Centers_CenterId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CenterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Provinces");

            migrationBuilder.DropColumn(
                name: "AmputationTypeOther",
                table: "ProstheticEpisodes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ExtractedText",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "OcrLanguage",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "OcrProcessedAt",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "OcrStatus",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "Adjustments",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "FittingNumber",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "NextFittingDate",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "NextSteps",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "PatientFeedback",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "PerformedBy",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "ExtractedText",
                table: "EpisodeDocuments");

            migrationBuilder.DropColumn(
                name: "OcrLanguage",
                table: "EpisodeDocuments");

            migrationBuilder.DropColumn(
                name: "OcrProcessedAt",
                table: "EpisodeDocuments");

            migrationBuilder.DropColumn(
                name: "OcrStatus",
                table: "EpisodeDocuments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Districts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ComponentCatalogs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Centers");

            migrationBuilder.DropColumn(
                name: "CenterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");
        }
    }
}
