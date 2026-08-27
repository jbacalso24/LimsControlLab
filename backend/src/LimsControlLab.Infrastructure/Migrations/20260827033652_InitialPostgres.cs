using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LimsControlLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    BeforeValues = table.Column<string>(type: "text", nullable: true),
                    AfterValues = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Site = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SamplingMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Site = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamplingMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Site = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Site = table.Column<int>(type: "integer", nullable: false),
                    AnalysisType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ShiftPattern = table.Column<int>(type: "integer", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "text", nullable: true),
                    ExclusionRules = table.Column<string>(type: "text", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Analyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SampleId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    TemplateVersionId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedByUserId = table.Column<int>(type: "integer", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AnalysisId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AttemptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationLogs_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnalysisId = table.Column<int>(type: "integer", nullable: false),
                    TestId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CapturedByUserId = table.Column<int>(type: "integer", nullable: false),
                    InstrumentId = table.Column<int>(type: "integer", nullable: true),
                    ValidationResult = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CalibratedValue = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Readings_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Readings_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExceptionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnalysisId = table.Column<int>(type: "integer", nullable: false),
                    ReadingId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: true),
                    DecisionComment = table.Column<string>(type: "text", nullable: true),
                    DecidedByUserId = table.Column<int>(type: "integer", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExceptionRecords_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExceptionRecords_Readings_ReadingId",
                        column: x => x.ReadingId,
                        principalTable: "Readings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AnalysisTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Site = table.Column<int>(type: "integer", nullable: false),
                    CurrentVersionId = table.Column<int>(type: "integer", nullable: true),
                    IsRetired = table.Column<bool>(type: "boolean", nullable: false),
                    MinTolerance = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxTolerance = table.Column<decimal>(type: "numeric", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    TestConfiguration = table.Column<string>(type: "text", nullable: true),
                    CalculationDefinitions = table.Column<string>(type: "text", nullable: true),
                    ValidationRules = table.Column<string>(type: "text", nullable: true),
                    MinTolerance = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxTolerance = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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
                name: "CalibrationCurves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AnalysisTemplateId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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
                name: "Samples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Identifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AnalysisTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Site = table.Column<int>(type: "integer", nullable: false),
                    CurrentSite = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Samples_AnalysisTemplates_AnalysisTemplateId",
                        column: x => x.AnalysisTemplateId,
                        principalTable: "AnalysisTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CalibrationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CalibrationCurveId = table.Column<int>(type: "integer", nullable: false),
                    XValue = table.Column<decimal>(type: "numeric", nullable: false),
                    YValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "SampleTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SampleId = table.Column<int>(type: "integer", nullable: false),
                    FromSite = table.Column<int>(type: "integer", nullable: false),
                    ToSite = table.Column<int>(type: "integer", nullable: false),
                    TransferredByUserId = table.Column<int>(type: "integer", nullable: false),
                    TransferredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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
                name: "IX_Analyses_SampleId",
                table: "Analyses",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_StartedAtUtc",
                table: "Analyses",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_TemplateId",
                table: "Analyses",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_TemplateVersionId",
                table: "Analyses",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTemplates_CurrentVersionId",
                table: "AnalysisTemplates",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTemplates_Site_Name",
                table: "AnalysisTemplates",
                columns: new[] { "Site", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTemplateVersions_TemplateId_Version",
                table: "AnalysisTemplateVersions",
                columns: new[] { "TemplateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TimestampUtc",
                table: "AuditLogs",
                column: "TimestampUtc");

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

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionRecords_AnalysisId",
                table: "ExceptionRecords",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionRecords_ReadingId",
                table: "ExceptionRecords",
                column: "ReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_IsActive",
                table: "Instruments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Site_Name",
                table: "Instruments",
                columns: new[] { "Site", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_AnalysisId",
                table: "IntegrationLogs",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_AttemptedAtUtc",
                table: "IntegrationLogs",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_TargetSystem_Status",
                table: "IntegrationLogs",
                columns: new[] { "TargetSystem", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_AnalysisId",
                table: "Readings",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_InstrumentId",
                table: "Readings",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_TestId",
                table: "Readings",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_AnalysisTemplateId",
                table: "Samples",
                column: "AnalysisTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_Site_Identifier",
                table: "Samples",
                columns: new[] { "Site", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleTransfers_SampleId",
                table: "SampleTransfers",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleTransfers_TransferredAtUtc",
                table: "SampleTransfers",
                column: "TransferredAtUtc");

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_AnalysisTemplateVersions_TemplateVersionId",
                table: "Analyses",
                column: "TemplateVersionId",
                principalTable: "AnalysisTemplateVersions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_AnalysisTemplates_TemplateId",
                table: "Analyses",
                column: "TemplateId",
                principalTable: "AnalysisTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Analyses_Samples_SampleId",
                table: "Analyses",
                column: "SampleId",
                principalTable: "Samples",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_AnalysisTemplates_AnalysisTemplateVersions_CurrentVersionId",
                table: "AnalysisTemplates");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CalibrationPoints");

            migrationBuilder.DropTable(
                name: "ExceptionRecords");

            migrationBuilder.DropTable(
                name: "IntegrationLogs");

            migrationBuilder.DropTable(
                name: "SampleTransfers");

            migrationBuilder.DropTable(
                name: "SamplingMethods");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "CalibrationCurves");

            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Analyses");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropTable(
                name: "Samples");

            migrationBuilder.DropTable(
                name: "AnalysisTemplateVersions");

            migrationBuilder.DropTable(
                name: "AnalysisTemplates");
        }
    }
}
