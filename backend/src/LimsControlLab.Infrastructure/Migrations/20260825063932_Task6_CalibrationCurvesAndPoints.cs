#pragma warning disable CA1707, CA1861

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task6_CalibrationCurvesAndPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalibrationCurves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AnalysisTemplateId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationCurves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationCurves_AnalysisTemplates_AnalysisTemplateId",
                        column: x => x.AnalysisTemplateId,
                        principalTable: "AnalysisTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CalibrationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalibrationCurveId = table.Column<int>(type: "int", nullable: false),
                    XValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationPoints_CalibrationCurves_CalibrationCurveId",
                        column: x => x.CalibrationCurveId,
                        principalTable: "CalibrationCurves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationCurves_AnalysisTemplateId_Name",
                table: "CalibrationCurves",
                columns: new[] { "AnalysisTemplateId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationCurves_IsActive",
                table: "CalibrationCurves",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationPoints_CalibrationCurveId_Order",
                table: "CalibrationPoints",
                columns: new[] { "CalibrationCurveId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalibrationPoints");

            migrationBuilder.DropTable(
                name: "CalibrationCurves");
        }
    }
}
