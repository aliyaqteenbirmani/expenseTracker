using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261106000000_UpdateCashTransactionDateProcedure")]
    public partial class UpdateCashTransactionDateProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_UpdateCashTransaction
                    @CashTransactionId UNIQUEIDENTIFIER,
                    @Amount DECIMAL(18,2),
                    @TransactionType NVARCHAR(20),
                    @Remarks NVARCHAR(500),
                    @FileName NVARCHAR(MAX),
                    @ModifiedBy NVARCHAR(MAX),
                    @TransactionDate DATETIME2=NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @OldFileName NVARCHAR(MAX)=(SELECT FileName FROM dbo.CashTransactions WHERE Id=@CashTransactionId AND IsDeleted=0);
                    UPDATE dbo.CashTransactions SET Amount=@Amount,TransactionType=@TransactionType,Remarks=@Remarks,
                        TransactionDate=COALESCE(@TransactionDate,TransactionDate),
                        FileName=COALESCE(NULLIF(@FileName,N''),FileName),ModifiedBy=@ModifiedBy,ModifiedOn=SYSUTCDATETIME()
                    WHERE Id=@CashTransactionId AND IsDeleted=0;
                    DECLARE @Ok BIT=IIF(@@ROWCOUNT=1,1,0),@Data NVARCHAR(MAX);
                    IF @Ok=1 SELECT @Data=(SELECT Id,Amount,TransactionType,Remarks,FileName,
                        CASE WHEN NULLIF(@FileName,N'') IS NOT NULL AND @FileName<>COALESCE(@OldFileName,N'') THEN @OldFileName ELSE NULL END OldFileName
                        FROM dbo.CashTransactions WHERE Id=@CashTransactionId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                    SELECT IIF(@Ok=1,200,404) ResponseCode,@Ok Success,IIF(@Ok=1,N'Transaction updated.',N'Transaction not found.') Message,@Data Data;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_UpdateCashTransaction
                    @CashTransactionId UNIQUEIDENTIFIER,
                    @Amount DECIMAL(18,2),
                    @TransactionType NVARCHAR(20),
                    @Remarks NVARCHAR(500),
                    @FileName NVARCHAR(MAX),
                    @ModifiedBy NVARCHAR(MAX)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @OldFileName NVARCHAR(MAX)=(SELECT FileName FROM dbo.CashTransactions WHERE Id=@CashTransactionId AND IsDeleted=0);
                    UPDATE dbo.CashTransactions SET Amount=@Amount,TransactionType=@TransactionType,Remarks=@Remarks,
                        FileName=COALESCE(NULLIF(@FileName,N''),FileName),ModifiedBy=@ModifiedBy,ModifiedOn=SYSUTCDATETIME()
                    WHERE Id=@CashTransactionId AND IsDeleted=0;
                    DECLARE @Ok BIT=IIF(@@ROWCOUNT=1,1,0),@Data NVARCHAR(MAX);
                    IF @Ok=1 SELECT @Data=(SELECT Id,Amount,TransactionType,Remarks,FileName,
                        CASE WHEN NULLIF(@FileName,N'') IS NOT NULL AND @FileName<>COALESCE(@OldFileName,N'') THEN @OldFileName ELSE NULL END OldFileName
                        FROM dbo.CashTransactions WHERE Id=@CashTransactionId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                    SELECT IIF(@Ok=1,200,404) ResponseCode,@Ok Success,IIF(@Ok=1,N'Transaction updated.',N'Transaction not found.') Message,@Data Data;
                END;
                """);
        }
    }
}
