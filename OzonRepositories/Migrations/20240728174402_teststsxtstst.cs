using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class teststsxtstst : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<int>(
                name: "SelectedWarehouseId",
                table: "ColumnMappings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedManufacturerId",
                table: "ColumnMappings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedCurrencyCode",
                table: "ColumnMappings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedClientId",
                table: "ColumnMappings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "ManufacturerFromArticle",
                table: "ColumnMappings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_ColumnMappings_Manufacturers_SelectedManufacturerId",
                table: "ColumnMappings",
                column: "SelectedManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ColumnMappings_OzonClients_SelectedClientId",
                table: "ColumnMappings",
                column: "SelectedClientId",
                principalTable: "OzonClients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ColumnMappings_Warehouses_SelectedWarehouseId",
                table: "ColumnMappings",
                column: "SelectedWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
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

            migrationBuilder.DropColumn(
                name: "ManufacturerFromArticle",
                table: "ColumnMappings");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedWarehouseId",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SelectedManufacturerId",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SelectedCurrencyCode",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SelectedClientId",
                table: "ColumnMappings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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
    }
}
