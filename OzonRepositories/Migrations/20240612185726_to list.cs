using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class tolist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainTableHeader",
                table: "ColumnMappings");

            migrationBuilder.RenameColumn(
                name: "TempTableHeader",
                table: "ColumnMappings",
                newName: "ColumnMappings");

            migrationBuilder.AlterColumn<string>(
                name: "MappingName",
                table: "ColumnMappings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ColumnMappings",
                table: "ColumnMappings",
                newName: "TempTableHeader");

            migrationBuilder.AlterColumn<string>(
                name: "MappingName",
                table: "ColumnMappings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "MainTableHeader",
                table: "ColumnMappings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
