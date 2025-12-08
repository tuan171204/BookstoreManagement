using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierBooks_SupplierID",
                table: "SupplierBooks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SupplierBooks",
                table: "SupplierBooks",
                columns: new[] { "SupplierID", "BookID" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SupplierBooks",
                table: "SupplierBooks");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBooks_SupplierID",
                table: "SupplierBooks",
                column: "SupplierID");
        }
    }
}
