using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1707, CA1861

#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task7_AddLockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Analyses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedAtUtc",
                table: "Analyses",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedByUserId",
                table: "Analyses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                table: "Analyses");
        }
    }
}
