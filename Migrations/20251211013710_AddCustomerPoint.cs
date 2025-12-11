using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RankId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_RankId",
                table: "Customers",
                column: "RankId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Rank",
                table: "Customers",
                column: "RankId",
                principalTable: "Code",
                principalColumn: "CodeID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customer_Rank",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_RankId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RankId",
                table: "Customers");
        }
    }
}
