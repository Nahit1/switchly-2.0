using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Switchly_2._0.WebApi.Context.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorEventsAndGuardrails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErrorThreshold",
                table: "RolloutSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErrorWindowMinutes",
                table: "RolloutSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MinSeverity",
                table: "RolloutSchedules",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RolledBackReason",
                table: "RolloutSchedules",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FlagErrorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PropertiesJson = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlagErrorEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlagErrorEvents_FeatureFlags_FeatureFlagId",
                        column: x => x.FeatureFlagId,
                        principalTable: "FeatureFlags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlagErrorEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlagErrorEvents_ProjectEnvironments_ProjectEnvironmentId",
                        column: x => x.ProjectEnvironmentId,
                        principalTable: "ProjectEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlagErrorEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlagErrorEvents_FeatureFlagId_OccurredAt",
                table: "FlagErrorEvents",
                columns: new[] { "FeatureFlagId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FlagErrorEvents_OrganizationId",
                table: "FlagErrorEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagErrorEvents_ProjectEnvironmentId_FeatureFlagId_Occurred~",
                table: "FlagErrorEvents",
                columns: new[] { "ProjectEnvironmentId", "FeatureFlagId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FlagErrorEvents_ProjectId",
                table: "FlagErrorEvents",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlagErrorEvents");

            migrationBuilder.DropColumn(
                name: "ErrorThreshold",
                table: "RolloutSchedules");

            migrationBuilder.DropColumn(
                name: "ErrorWindowMinutes",
                table: "RolloutSchedules");

            migrationBuilder.DropColumn(
                name: "MinSeverity",
                table: "RolloutSchedules");

            migrationBuilder.DropColumn(
                name: "RolledBackReason",
                table: "RolloutSchedules");
        }
    }
}
