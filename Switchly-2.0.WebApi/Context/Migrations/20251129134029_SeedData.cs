using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Switchly_2._0.WebApi.Context.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "CreatedAt", "Name", "PublicKey", "SecretKey", "Slug" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), "Demo Organization", "demo-org-public", "demo-org-secret", "demo-org" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "Name", "PasswordHash" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), "demo@switchly.dev", "Demo User", "DEMO_HASH" });

            migrationBuilder.InsertData(
                table: "OrganizationMembers",
                columns: new[] { "Id", "CreatedAt", "OrganizationId", "Role", "UserId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Owner", new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "CreatedAt", "Description", "IsArchived", "Key", "Name", "OrganizationId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), "", false, "demo-project", "Demo Project", new Guid("00000000-0000-0000-0000-000000000010") });

            migrationBuilder.InsertData(
                table: "FeatureFlags",
                columns: new[] { "Id", "CreatedAt", "Description", "IsArchived", "Key", "Name", "ProjectId", "Type" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000040"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), "Enable new dashboard", false, "new_dashboard", "New Dashboard", new Guid("00000000-0000-0000-0000-000000000020"), "Boolean" });

            migrationBuilder.InsertData(
                table: "ProjectEnvironments",
                columns: new[] { "Id", "CreatedAt", "IsDefault", "Key", "Name", "ProjectId", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), true, "dev", "Development", new Guid("00000000-0000-0000-0000-000000000020"), 1 },
                    { new Guid("00000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), false, "stg", "Staging", new Guid("00000000-0000-0000-0000-000000000020"), 2 },
                    { new Guid("00000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)), false, "prod", "Production", new Guid("00000000-0000-0000-0000-000000000020"), 3 }
                });

            migrationBuilder.InsertData(
                table: "FeatureFlagEnvironments",
                columns: new[] { "Id", "DefaultRolloutKind", "DefaultRolloutPercentage", "FeatureFlagId", "IsEnabled", "ProjectEnvironmentId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000050"), "AllUsers", 100, new Guid("00000000-0000-0000-0000-000000000040"), true, new Guid("00000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000051"), "Off", 0, new Guid("00000000-0000-0000-0000-000000000040"), false, new Guid("00000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000052"), "Off", 0, new Guid("00000000-0000-0000-0000-000000000040"), false, new Guid("00000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2025, 11, 29, 13, 40, 29, 385, DateTimeKind.Unspecified).AddTicks(7700), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FeatureFlagEnvironments",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                table: "FeatureFlagEnvironments",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000051"));

            migrationBuilder.DeleteData(
                table: "FeatureFlagEnvironments",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000052"));

            migrationBuilder.DeleteData(
                table: "OrganizationMembers",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "FeatureFlags",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                table: "ProjectEnvironments",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                table: "ProjectEnvironments",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "ProjectEnvironments",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));
        }
    }
}
