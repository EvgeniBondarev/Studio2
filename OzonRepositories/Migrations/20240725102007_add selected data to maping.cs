using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class addselecteddatatomaping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedClientId",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SelectedCurrencyCode",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SelectedManufacturerId",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SelectedStatus",
                table: "ColumnMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedWarehouseId",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ColumnMappings_SelectedClientId",
                table: "ColumnMappings",
                column: "SelectedClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ColumnMappings_SelectedManufacturerId",
                table: "ColumnMappings",
                column: "SelectedManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ColumnMappings_SelectedWarehouseId",
                table: "ColumnMappings",
                column: "SelectedWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ColumnMappings_Manufacturers_SelectedManufacturerId",
                table: "ColumnMappings",
                column: "SelectedManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ColumnMappings_OzonClients_SelectedClientId",
                table: "ColumnMappings",
                column: "SelectedClientId",
                principalTable: "OzonClients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ColumnMappings_Warehouses_SelectedWarehouseId",
                table: "ColumnMappings",
                column: "SelectedWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ColumnMappings_Manufacturers_SelectedManufacturerId",
                table: "ColumnMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_ColumnMappings_OzonClients_SelectedClientId",
                table: "ColumnMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_ColumnMappings_Warehouses_SelectedWarehouseId",
                table: "ColumnMappings");

            migrationBuilder.DropIndex(
                name: "IX_ColumnMappings_SelectedClientId",
                table: "ColumnMappings");

            migrationBuilder.DropIndex(
                name: "IX_ColumnMappings_SelectedManufacturerId",
                table: "ColumnMappings");

            migrationBuilder.DropIndex(
                name: "IX_ColumnMappings_SelectedWarehouseId",
                table: "ColumnMappings");

            migrationBuilder.DropColumn(
                name: "SelectedClientId",
                table: "ColumnMappings");

            migrationBuilder.DropColumn(
                name: "SelectedCurrencyCode",
                table: "ColumnMappings");

            migrationBuilder.DropColumn(
                name: "SelectedManufacturerId",
                table: "ColumnMappings");

            migrationBuilder.DropColumn(
                name: "SelectedStatus",
                table: "ColumnMappings");

            migrationBuilder.DropColumn(
                name: "SelectedWarehouseId",
                table: "ColumnMappings");
        }
    }
}
