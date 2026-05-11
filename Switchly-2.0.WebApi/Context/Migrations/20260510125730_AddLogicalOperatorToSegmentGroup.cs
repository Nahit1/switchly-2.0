using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Switchly_2._0.WebApi.Context.Migrations
{
    /// <inheritdoc />
    public partial class AddLogicalOperatorToSegmentGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LogicalOperator",
                table: "SegmentGroups",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogicalOperator",
                table: "SegmentGroups");
        }
    }
}
