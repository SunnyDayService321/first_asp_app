using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace first_asp_app.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteFlgToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeleteFlg",
                table: "Product",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteFlg",
                table: "Product");
        }
    }
}
