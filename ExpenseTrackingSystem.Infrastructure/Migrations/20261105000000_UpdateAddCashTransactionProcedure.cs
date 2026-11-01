using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261105000000_UpdateAddCashTransactionProcedure")]
    public partial class UpdateAddCashTransactionProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_AddCashTransaction
                    @CashBookId UNIQUEIDENTIFIER,
                    @Amount DECIMAL(18,2),
                    @TransactionType NVARCHAR(20),
                    @Remarks NVARCHAR(500),
                    @FileName NVARCHAR(MAX),
                    @CreatedBy NVARCHAR(MAX),
                    @TransactionDate DATETIME2=NULL,
                    @ReceiptDraftId UNIQUEIDENTIFIER=NULL,
                    @Merchant NVARCHAR(500)=NULL,
                    @Category NVARCHAR(50)=NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @Id UNIQUEIDENTIFIER=NEWID(),
                            @Now DATETIME2=SYSUTCDATETIME(),
                            @EffectiveTransactionDate DATETIME2=COALESCE(@TransactionDate,SYSUTCDATETIME());

                    BEGIN TRY
                        BEGIN TRANSACTION;

                        IF @ReceiptDraftId IS NOT NULL
                        BEGIN
                            UPDATE dbo.ReceiptDrafts WITH (UPDLOCK,ROWLOCK)
                            SET Status=N'Confirming'
                            WHERE Id=@ReceiptDraftId
                              AND CashBookId=@CashBookId
                              AND Status=N'ReadyForReview'
                              AND CreatedTransactionId IS NULL
                              AND ExpiresOn>SYSUTCDATETIME();

                            IF @@ROWCOUNT=0
                            BEGIN
                                ROLLBACK TRANSACTION;
                                SELECT 409 ResponseCode,CAST(0 AS BIT) Success,
                                       N'Receipt draft is not ready, expired, or already confirmed.' Message,NULL Data;
                                RETURN;
                            END;
                        END;

                        INSERT dbo.CashTransactions
                            (Id,CashBookId,Amount,TransactionType,Remarks,FileName,TransactionDate,
                             CreatedOn,CreatedBy,ModifiedOn,ModifiedBy,IsActive,IsDeleted)
                        VALUES
                            (@Id,@CashBookId,@Amount,@TransactionType,@Remarks,@FileName,@EffectiveTransactionDate,
                             @Now,@CreatedBy,@Now,@CreatedBy,1,0);

                        IF @ReceiptDraftId IS NOT NULL
                        BEGIN
                            UPDATE dbo.ReceiptDrafts
                            SET Status=N'Completed',CreatedTransactionId=@Id,
                                SuggestedAmount=@Amount,SuggestedTransactionType=@TransactionType,
                                SuggestedTransactionDate=@EffectiveTransactionDate,SuggestedMerchant=@Merchant,
                                SuggestedCategory=@Category
                            WHERE Id=@ReceiptDraftId;
                        END;

                        COMMIT TRANSACTION;

                        SELECT 201 ResponseCode,CAST(1 AS BIT) Success,N'Transaction created.' Message,
                            (SELECT Id,CashBookId,Amount,TransactionType,Remarks,FileName,TransactionDate,
                                    CreatedOn,CreatedBy,ModifiedOn,ModifiedBy,IsActive,IsDeleted
                             FROM dbo.CashTransactions WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) Data;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_AddCashTransaction
                    @CashBookId UNIQUEIDENTIFIER,
                    @Amount DECIMAL(18,2),
                    @TransactionType NVARCHAR(20),
                    @Remarks NVARCHAR(500),
                    @FileName NVARCHAR(MAX),
                    @CreatedBy NVARCHAR(MAX)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @Id UNIQUEIDENTIFIER=NEWID(),@Now DATETIME2=SYSUTCDATETIME();
                    INSERT dbo.CashTransactions
                        (Id,CashBookId,Amount,TransactionType,Remarks,FileName,TransactionDate,
                         CreatedOn,CreatedBy,ModifiedOn,ModifiedBy,IsActive,IsDeleted)
                    VALUES
                        (@Id,@CashBookId,@Amount,@TransactionType,@Remarks,@FileName,@Now,
                         @Now,@CreatedBy,@Now,@CreatedBy,1,0);
                    SELECT 200 ResponseCode,CAST(1 AS BIT) Success,N'Transaction created.' Message,
                        (SELECT Id,CashBookId,Amount,TransactionType,Remarks,FileName,TransactionDate,
                                CreatedOn,CreatedBy,ModifiedOn,ModifiedBy,IsActive,IsDeleted
                         FROM dbo.CashTransactions WHERE Id=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) Data;
                END;
                """);
        }
    }
}
