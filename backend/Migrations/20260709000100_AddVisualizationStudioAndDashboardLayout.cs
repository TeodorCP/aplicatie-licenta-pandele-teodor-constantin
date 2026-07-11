using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BusinessOps.Backend.Data;

#nullable disable

namespace BusinessOps.Backend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260709000100_AddVisualizationStudioAndDashboardLayout")]
public partial class AddVisualizationStudioAndDashboardLayout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "VisualSettings",
            table: "dashboard_widgets",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "X",
            table: "dashboard_widgets",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Y",
            table: "dashboard_widgets",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ChartSpecificOptions",
            table: "visualizations",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CustomEndTimestamp",
            table: "visualizations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CustomStartTimestamp",
            table: "visualizations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DateRangeType",
            table: "visualizations",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "visualizations",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "GeneralOptions",
            table: "visualizations",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "visualizations",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");

        migrationBuilder.AddColumn<string>(
            name: "XAggregation",
            table: "visualizations",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "XField",
            table: "visualizations",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "YField",
            table: "visualizations",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_dashboard_widgets_Y_X",
            table: "dashboard_widgets",
            columns: new[] { "Y", "X" });

        migrationBuilder.Sql("""
            UPDATE visualizations
            SET "XField" = COALESCE("XField", "FieldName"),
                "YField" = COALESCE("YField", "SecondaryFieldName"),
                "XAggregation" = COALESCE("XAggregation", "AggregationType"),
                "DateRangeType" = COALESCE("DateRangeType", COALESCE("DateRange", 'all')),
                "UpdatedAt" = COALESCE("UpdatedAt", "CreatedAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_dashboard_widgets_Y_X",
            table: "dashboard_widgets");

        migrationBuilder.DropColumn(
            name: "VisualSettings",
            table: "dashboard_widgets");

        migrationBuilder.DropColumn(
            name: "X",
            table: "dashboard_widgets");

        migrationBuilder.DropColumn(
            name: "Y",
            table: "dashboard_widgets");

        migrationBuilder.DropColumn(
            name: "ChartSpecificOptions",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "CustomEndTimestamp",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "CustomStartTimestamp",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "DateRangeType",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "Description",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "GeneralOptions",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "UpdatedAt",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "XAggregation",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "XField",
            table: "visualizations");

        migrationBuilder.DropColumn(
            name: "YField",
            table: "visualizations");
    }
}
