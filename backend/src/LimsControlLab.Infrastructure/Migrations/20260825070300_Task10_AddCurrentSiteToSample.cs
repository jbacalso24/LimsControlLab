using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
#pragma warning disable CA1707
    public partial class Task10_AddCurrentSiteToSample : Migration
#pragma warning restore CA1707
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSite",
                table: "Samples",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSite",
                table: "Samples");
        }
    }
}
