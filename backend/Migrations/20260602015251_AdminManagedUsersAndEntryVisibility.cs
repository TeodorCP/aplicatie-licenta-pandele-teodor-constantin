using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessOps.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AdminManagedUsersAndEntryVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "entry_visibility_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanEdit = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry_visibility_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_entry_visibility_permissions_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entry_visibility_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entry_visibility_permissions_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entries_CreatedByUserId",
                table: "entries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_entry_visibility_permissions_EntryId_RoleId",
                table: "entry_visibility_permissions",
                columns: new[] { "EntryId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entry_visibility_permissions_OwnerUserId",
                table: "entry_visibility_permissions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_entry_visibility_permissions_RoleId",
                table: "entry_visibility_permissions",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_entries_users_CreatedByUserId",
                table: "entries",
                column: "CreatedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_entries_users_CreatedByUserId",
                table: "entries");

            migrationBuilder.DropTable(
                name: "entry_visibility_permissions");

            migrationBuilder.DropIndex(
                name: "IX_entries_CreatedByUserId",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "entries");
        }
    }
}
