using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTickets_Date",
                table: "ImportTickets",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTickets_Status",
                table: "ImportTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTickets_Date",
                table: "ExportTickets",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTickets_Status",
                table: "ExportTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FullName",
                table: "Employees",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FullName",
                table: "Customers",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Book_Title",
                table: "Books",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Books_IsDeleted",
                table: "Books",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Name",
                table: "Authors",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_ImportTickets_Date",
                table: "ImportTickets");

            migrationBuilder.DropIndex(
                name: "IX_ImportTickets_Status",
                table: "ImportTickets");

            migrationBuilder.DropIndex(
                name: "IX_ExportTickets_Date",
                table: "ExportTickets");

            migrationBuilder.DropIndex(
                name: "IX_ExportTickets_Status",
                table: "ExportTickets");

            migrationBuilder.DropIndex(
                name: "IX_Employees_FullName",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Customers_FullName",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Book_Title",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_IsDeleted",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Authors_Name",
                table: "Authors");
        }
    }
}
