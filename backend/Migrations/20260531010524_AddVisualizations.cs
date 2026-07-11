using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessOps.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "visualizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ChartType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AggregationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    DateRange = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visualizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visualizations_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_visualizations_ModuleId_CreatedAt",
                table: "visualizations",
                columns: new[] { "ModuleId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "visualizations");
        }
    }
}
