using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261102000000_AddReceiptDraftOcrResult")]
    public partial class AddReceiptDraftOcrResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OcrText",
                table: "ReceiptDrafts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OcrConfidence",
                table: "ReceiptDrafts",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OcrText", table: "ReceiptDrafts");
            migrationBuilder.DropColumn(name: "OcrConfidence", table: "ReceiptDrafts");
        }
    }
}
