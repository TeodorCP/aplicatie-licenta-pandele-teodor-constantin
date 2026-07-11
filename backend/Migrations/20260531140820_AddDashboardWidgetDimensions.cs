using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessOps.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardWidgetDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "dashboard_widgets",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "dashboard_widgets",
                type: "integer",
                nullable: false,
                defaultValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "dashboard_widgets");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "dashboard_widgets");
        }
    }
}
