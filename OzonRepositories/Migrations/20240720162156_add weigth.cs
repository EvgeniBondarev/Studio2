using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class addweigth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WeightFactor",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "Products",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightFactor",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Products");
        }
    }
}
