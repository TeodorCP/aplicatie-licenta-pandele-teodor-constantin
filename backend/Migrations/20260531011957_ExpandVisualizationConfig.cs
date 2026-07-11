using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessOps.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ExpandVisualizationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecondaryFieldName",
                table: "visualizations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryMetric",
                table: "visualizations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WidgetSize",
                table: "visualizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "medium");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondaryFieldName",
                table: "visualizations");

            migrationBuilder.DropColumn(
                name: "SummaryMetric",
                table: "visualizations");

            migrationBuilder.DropColumn(
                name: "WidgetSize",
                table: "visualizations");
        }
    }
}
