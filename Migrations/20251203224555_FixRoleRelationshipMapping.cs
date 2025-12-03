using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleRelationshipMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_RoleId1",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId1",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "RoleId1",
                table: "RolePermissions");

            migrationBuilder.AddColumn<string>(
                name: "AppRoleId",
                table: "RolePermissions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_AppRoleId",
                table: "RolePermissions",
                column: "AppRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_AppRoleId",
                table: "RolePermissions",
                column: "AppRoleId",
                principalTable: "Roles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_AppRoleId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_AppRoleId",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "AppRoleId",
                table: "RolePermissions");

            migrationBuilder.AddColumn<string>(
                name: "RoleId1",
                table: "RolePermissions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId1",
                table: "RolePermissions",
                column: "RoleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RoleId1",
                table: "RolePermissions",
                column: "RoleId1",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
