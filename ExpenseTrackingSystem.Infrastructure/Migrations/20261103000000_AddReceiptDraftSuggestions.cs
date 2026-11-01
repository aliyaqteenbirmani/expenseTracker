using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261103000000_AddReceiptDraftSuggestions")]
    public partial class AddReceiptDraftSuggestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SuggestedMerchant",
                table: "ReceiptDrafts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedAmount",
                table: "ReceiptDrafts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuggestedTransactionDate",
                table: "ReceiptDrafts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedTransactionType",
                table: "ReceiptDrafts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedCategory",
                table: "ReceiptDrafts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ParsingConfidence",
                table: "ReceiptDrafts",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SuggestedMerchant", table: "ReceiptDrafts");
            migrationBuilder.DropColumn(name: "SuggestedAmount", table: "ReceiptDrafts");
            migrationBuilder.DropColumn(name: "SuggestedTransactionDate", table: "ReceiptDrafts");
            migrationBuilder.DropColumn(name: "SuggestedTransactionType", table: "ReceiptDrafts");
            migrationBuilder.DropColumn(name: "SuggestedCategory", table: "ReceiptDrafts");
            migrationBuilder.DropColumn(name: "ParsingConfidence", table: "ReceiptDrafts");
        }
    }
}
