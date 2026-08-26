using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1707
#pragma warning disable CA1861
#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task5_Instruments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Site = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_InstrumentId",
                table: "Readings",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_IsActive",
                table: "Instruments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Site_Name",
                table: "Instruments",
                columns: new[] { "Site", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Readings_Instruments_InstrumentId",
                table: "Readings",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Readings_Instruments_InstrumentId",
                table: "Readings");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropIndex(
                name: "IX_Readings_InstrumentId",
                table: "Readings");
        }
    }
}
