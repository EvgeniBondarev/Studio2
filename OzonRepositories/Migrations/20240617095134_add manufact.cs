using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class addmanufact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManufacturerId",
                table: "Orders",
                type: "int",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ManufacturerId",
                table: "Orders",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Manufacturers_ManufacturerId",
                table: "Orders",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Manufacturers_ManufacturerId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ManufacturerId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "Orders")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
