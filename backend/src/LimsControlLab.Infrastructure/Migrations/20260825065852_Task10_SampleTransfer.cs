using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
#pragma warning disable CA1707
    public partial class Task10_SampleTransfer : Migration
#pragma warning restore CA1707
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SampleTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SampleId = table.Column<int>(type: "int", nullable: false),
                    FromSite = table.Column<int>(type: "int", nullable: false),
                    ToSite = table.Column<int>(type: "int", nullable: false),
                    TransferredByUserId = table.Column<int>(type: "int", nullable: false),
                    TransferredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleTransfers_Samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "Samples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleTransfers_SampleId",
                table: "SampleTransfers",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleTransfers_TransferredAtUtc",
                table: "SampleTransfers",
                column: "TransferredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SampleTransfers");
        }
    }
}
