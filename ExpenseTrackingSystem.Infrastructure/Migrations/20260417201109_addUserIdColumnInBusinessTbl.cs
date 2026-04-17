using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addUserIdColumnInBusinessTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Businesses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Businesses");

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "CashTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
