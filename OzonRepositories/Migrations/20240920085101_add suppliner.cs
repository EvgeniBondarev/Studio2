using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class addsuppliner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedSupplierId",
                table: "ColumnMappings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ColumnMappings_SelectedSupplierId",
                table: "ColumnMappings",
                column: "SelectedSupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_ColumnMappings_Suppliers_SelectedSupplierId",
                table: "ColumnMappings",
                column: "SelectedSupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ColumnMappings_Suppliers_SelectedSupplierId",
                table: "ColumnMappings");

            migrationBuilder.DropIndex(
                name: "IX_ColumnMappings_SelectedSupplierId",
                table: "ColumnMappings");

            migrationBuilder.DropColumn(
                name: "SelectedSupplierId",
                table: "ColumnMappings");
        }
    }
}
