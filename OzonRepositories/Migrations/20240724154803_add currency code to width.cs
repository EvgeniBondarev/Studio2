using Microsoft.EntityFrameworkCore.Migrations;


#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class addcurrencycodetowidth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeightFactorCurrencyCode",
                table: "Suppliers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightFactorCurrencyCode",
                table: "Suppliers");
        }
    }
}
