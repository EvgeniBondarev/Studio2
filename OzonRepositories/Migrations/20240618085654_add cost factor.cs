﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    /// <inheritdoc />
    public partial class addcostfactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostFactor",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostFactor",
                table: "Suppliers");
        }
    }
}
