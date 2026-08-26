#pragma warning disable CA1707, CA1861

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task4TemplatesSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionId",
                table: "AnalysisTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetired",
                table: "AnalysisTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TemplateVersionId",
                table: "Analyses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AnalysisTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    TestConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalculationDefinitions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinTolerance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxTolerance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisTemplateVersions_AnalysisTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "AnalysisTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SamplingMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Site = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamplingMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Site = table.Column<int>(type: "int", nullable: false),
                    AnalysisType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ShiftPattern = table.Column<int>(type: "int", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExclusionRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTemplates_CurrentVersionId",
                table: "AnalysisTemplates",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_TemplateVersionId",
                table: "Analyses",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTemplateVersions_TemplateId_Version",
                table: "AnalysisTemplateVersions",
                columns: new[] { "TemplateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SamplingMethods_IsActive",
                table: "SamplingMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SamplingMethods_Site_Name",
                table: "SamplingMethods",
                columns: new[] { "Site", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_AssignedToUserId",
                table: "Schedules",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_IsActive",
                table: "Schedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Site_Name",
                table: "Schedules",
                columns: new[] { "Site", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_AnalysisTemplateVersions_TemplateVersionId",
                table: "Analyses",
                column: "TemplateVersionId",
                principalTable: "AnalysisTemplateVersions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysisTemplates_AnalysisTemplateVersions_CurrentVersionId",
                table: "AnalysisTemplates",
                column: "CurrentVersionId",
                principalTable: "AnalysisTemplateVersions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analyses_AnalysisTemplateVersions_TemplateVersionId",
                table: "Analyses");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalysisTemplates_AnalysisTemplateVersions_CurrentVersionId",
                table: "AnalysisTemplates");

            migrationBuilder.DropTable(
                name: "AnalysisTemplateVersions");

            migrationBuilder.DropTable(
                name: "SamplingMethods");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_AnalysisTemplates_CurrentVersionId",
                table: "AnalysisTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Analyses_TemplateVersionId",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "CurrentVersionId",
                table: "AnalysisTemplates");

            migrationBuilder.DropColumn(
                name: "IsRetired",
                table: "AnalysisTemplates");

            migrationBuilder.DropColumn(
                name: "TemplateVersionId",
                table: "Analyses");
        }
    }
}
