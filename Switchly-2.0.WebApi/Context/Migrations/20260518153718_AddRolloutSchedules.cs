using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Switchly_2._0.WebApi.Context.Migrations
{
    /// <inheritdoc />
    public partial class AddRolloutSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolloutSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureFlagEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentStepIndex = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTransitionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PausedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PreScheduleSnapshot = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolloutSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolloutSchedules_FeatureFlagEnvironments_FeatureFlagEnviron~",
                        column: x => x.FeatureFlagEnvironmentId,
                        principalTable: "FeatureFlagEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolloutSchedules_Variants_TargetVariantId",
                        column: x => x.TargetVariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RolloutScheduleSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RolloutScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepIndex = table.Column<int>(type: "integer", nullable: false),
                    Percentage = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PromotedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolloutScheduleSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolloutScheduleSteps_RolloutSchedules_RolloutScheduleId",
                        column: x => x.RolloutScheduleId,
                        principalTable: "RolloutSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolloutSchedules_FeatureFlagEnvironmentId",
                table: "RolloutSchedules",
                column: "FeatureFlagEnvironmentId",
                unique: true,
                filter: "\"Status\" IN ('Active', 'Paused', 'Draft')");

            migrationBuilder.CreateIndex(
                name: "IX_RolloutSchedules_TargetVariantId",
                table: "RolloutSchedules",
                column: "TargetVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_RolloutScheduleSteps_RolloutScheduleId_StepIndex",
                table: "RolloutScheduleSteps",
                columns: new[] { "RolloutScheduleId", "StepIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolloutScheduleSteps");

            migrationBuilder.DropTable(
                name: "RolloutSchedules");
        }
    }
}
