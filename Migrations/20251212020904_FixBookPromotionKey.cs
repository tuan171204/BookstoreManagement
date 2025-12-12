using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixBookPromotionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__BookPromo__Promo__0B91BA14",
                table: "BookPromotions");

            migrationBuilder.DropIndex(
                name: "IX_BookPromotions_BookID",
                table: "BookPromotions");

            migrationBuilder.AddColumn<int>(
                name: "BookId1",
                table: "BookPromotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookPromotions",
                table: "BookPromotions",
                columns: new[] { "BookID", "PromotionID" });

            migrationBuilder.CreateIndex(
                name: "IX_BookPromotions_BookId1",
                table: "BookPromotions",
                column: "BookId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BookPromotions_Books_BookId1",
                table: "BookPromotions",
                column: "BookId1",
                principalTable: "Books",
                principalColumn: "BookID");

            migrationBuilder.AddForeignKey(
                name: "FK__BookPromo__Promo__0B91BA14",
                table: "BookPromotions",
                column: "PromotionID",
                principalTable: "Promotions",
                principalColumn: "PromotionID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookPromotions_Books_BookId1",
                table: "BookPromotions");

            migrationBuilder.DropForeignKey(
                name: "FK__BookPromo__Promo__0B91BA14",
                table: "BookPromotions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookPromotions",
                table: "BookPromotions");

            migrationBuilder.DropIndex(
                name: "IX_BookPromotions_BookId1",
                table: "BookPromotions");

            migrationBuilder.DropColumn(
                name: "BookId1",
                table: "BookPromotions");

            migrationBuilder.CreateIndex(
                name: "IX_BookPromotions_BookID",
                table: "BookPromotions",
                column: "BookID");

            migrationBuilder.AddForeignKey(
                name: "FK__BookPromo__Promo__0B91BA14",
                table: "BookPromotions",
                column: "PromotionID",
                principalTable: "Promotions",
                principalColumn: "PromotionID");
        }
    }
}
