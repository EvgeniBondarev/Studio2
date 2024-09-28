using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations.OzonIdentityDb
{
    /// <inheritdoc />
    public partial class _623423 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserAccessId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserAccessId",
                table: "AspNetUsers",
                column: "UserAccessId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserAccess_UserAccessId",
                table: "AspNetUsers",
                column: "UserAccessId",
                principalTable: "UserAccess",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserAccess_UserAccessId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "UserAccess");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserAccessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserAccessId",
                table: "AspNetUsers");
        }
    }
}
