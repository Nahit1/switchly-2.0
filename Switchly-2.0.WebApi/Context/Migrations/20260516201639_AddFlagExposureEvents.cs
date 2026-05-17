using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Switchly_2._0.WebApi.Context.Migrations
{
    /// <inheritdoc />
    public partial class AddFlagExposureEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlagExposureEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsOn = table.Column<bool>(type: "boolean", nullable: false),
                    UserKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlagExposureEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlagExposureEvents_FeatureFlags_FeatureFlagId",
                        column: x => x.FeatureFlagId,
                        principalTable: "FeatureFlags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlagExposureEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlagExposureEvents_ProjectEnvironments_ProjectEnvironmentId",
                        column: x => x.ProjectEnvironmentId,
                        principalTable: "ProjectEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlagExposureEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlagExposureEvents_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlagExposureEvents_FeatureFlagId_OccurredAt",
                table: "FlagExposureEvents",
                columns: new[] { "FeatureFlagId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FlagExposureEvents_OrganizationId",
                table: "FlagExposureEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagExposureEvents_ProjectEnvironmentId",
                table: "FlagExposureEvents",
                column: "ProjectEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagExposureEvents_ProjectId",
                table: "FlagExposureEvents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagExposureEvents_UserKey_FeatureFlagId",
                table: "FlagExposureEvents",
                columns: new[] { "UserKey", "FeatureFlagId" });

            migrationBuilder.CreateIndex(
                name: "IX_FlagExposureEvents_VariantId",
                table: "FlagExposureEvents",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlagExposureEvents");
        }
    }
}
