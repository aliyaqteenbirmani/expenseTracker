using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261117220000_UpdateSplitExpensesSplitTypeCheckConstraint")]
    public partial class UpdateSplitExpensesSplitTypeCheckConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SplitExpenses_SplitType')
BEGIN
    ALTER TABLE dbo.SplitExpenses DROP CONSTRAINT CK_SplitExpenses_SplitType;
END

ALTER TABLE dbo.SplitExpenses WITH CHECK
ADD CONSTRAINT CK_SplitExpenses_SplitType CHECK (SplitType IN ('Equal', 'Exact', 'Percentage', 'Shares'));
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SplitExpenses_SplitType')
BEGIN
    ALTER TABLE dbo.SplitExpenses DROP CONSTRAINT CK_SplitExpenses_SplitType;
END

ALTER TABLE dbo.SplitExpenses WITH CHECK
ADD CONSTRAINT CK_SplitExpenses_SplitType CHECK (SplitType IN ('Equal'));
");
        }
    }
}
