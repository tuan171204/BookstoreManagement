using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplyChannel",
                table: "Promotions",
                type: "nvarchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PromotionId1",
                table: "BookPromotions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookPromotions_PromotionId1",
                table: "BookPromotions",
                column: "PromotionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BookPromotions_Promotions_PromotionId1",
                table: "BookPromotions",
                column: "PromotionId1",
                principalTable: "Promotions",
                principalColumn: "PromotionID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookPromotions_Promotions_PromotionId1",
                table: "BookPromotions");

            migrationBuilder.DropIndex(
                name: "IX_BookPromotions_PromotionId1",
                table: "BookPromotions");

            migrationBuilder.DropColumn(
                name: "ApplyChannel",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "PromotionId1",
                table: "BookPromotions");
        }
    }
}
