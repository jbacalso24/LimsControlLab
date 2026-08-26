#pragma warning disable CA1707

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task6_AddCalibratedValueToReading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CalibratedValue",
                table: "Readings",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalibratedValue",
                table: "Readings");
        }
    }
}
