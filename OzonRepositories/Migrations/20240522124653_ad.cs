using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class ad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OzonClientId",
                table: "Orders",
                type: "int",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OzonClientId",
                table: "Orders",
                column: "OzonClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OzonClients_OzonClientId",
                table: "Orders",
                column: "OzonClientId",
                principalTable: "OzonClients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OzonClients_OzonClientId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OzonClientId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OzonClientId",
                table: "Orders")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrdersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
