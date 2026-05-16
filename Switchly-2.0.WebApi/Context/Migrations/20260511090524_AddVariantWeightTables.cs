using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Switchly_2._0.WebApi.Context.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantWeightTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureFlagEnvironmentVariantWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureFlagEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagEnvironmentVariantWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureFlagEnvironmentVariantWeights_FeatureFlagEnvironment~",
                        column: x => x.FeatureFlagEnvironmentId,
                        principalTable: "FeatureFlagEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureFlagEnvironmentVariantWeights_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagSegmentTargetingVariantWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureFlagSegmentTargetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagSegmentTargetingVariantWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureFlagSegmentTargetingVariantWeights_FeatureFlagSegmen~",
                        column: x => x.FeatureFlagSegmentTargetingId,
                        principalTable: "FeatureFlagSegmentTargetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureFlagSegmentTargetingVariantWeights_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagEnvironmentVariantWeights_FeatureFlagEnvironment~",
                table: "FeatureFlagEnvironmentVariantWeights",
                columns: new[] { "FeatureFlagEnvironmentId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagEnvironmentVariantWeights_VariantId",
                table: "FeatureFlagEnvironmentVariantWeights",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagSegmentTargetingVariantWeights_FeatureFlagSegmen~",
                table: "FeatureFlagSegmentTargetingVariantWeights",
                columns: new[] { "FeatureFlagSegmentTargetingId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagSegmentTargetingVariantWeights_VariantId",
                table: "FeatureFlagSegmentTargetingVariantWeights",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureFlagEnvironmentVariantWeights");

            migrationBuilder.DropTable(
                name: "FeatureFlagSegmentTargetingVariantWeights");
        }
    }
}
