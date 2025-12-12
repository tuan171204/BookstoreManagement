using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultProfitMarginToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DefaultProfitMargin",
                table: "Categories",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultProfitMargin",
                table: "Categories");
        }
    }
}
