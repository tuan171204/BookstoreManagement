using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookstoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ__Customer__5C7E359E320E9AE8",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Customer__5C7E359E320E9AE8",
                table: "Customers",
                column: "Phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ__Customer__5C7E359E320E9AE8",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "UQ__Customer__5C7E359E320E9AE8",
                table: "Customers",
                column: "Phone",
                unique: true,
                filter: "[Phone] IS NOT NULL");
        }
    }
}
