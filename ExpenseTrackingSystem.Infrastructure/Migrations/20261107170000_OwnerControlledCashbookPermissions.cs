using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261107170000_OwnerControlledCashbookPermissions")]
    public partial class OwnerControlledCashbookPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_IsCashbookOwner
                    @CashBookId UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.CashBooks cb
                        LEFT JOIN dbo.Businesses b ON b.Id = cb.BusinessId AND b.IsDeleted = 0
                        WHERE cb.Id = @CashBookId AND cb.IsDeleted = 0
                          AND (cb.UserId = @UserId OR b.OwnerUserId = @UserId OR b.UserId = @UserId)
                    ) THEN 1 ELSE 0 END AS BIT) AS IsAllowed;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_HasCashbookPermission
                    @CashBookId UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER, @PermissionCode NVARCHAR(200)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.CashbookMembers cm
                        WHERE cm.CashBookId = @CashBookId AND cm.UserId = @UserId AND cm.Status = N'Active' AND cm.IsDeleted = 0 AND cm.IsActive = 1
                          AND EXISTS (
                              SELECT 1 FROM dbo.CashbookMemberPermissions cmp
                              JOIN dbo.Permissions p ON p.Id = cmp.PermissionId AND p.IsDeleted = 0
                              WHERE cmp.CashbookMemberId = cm.Id AND cmp.Granted = 1 AND cmp.IsDeleted = 0
                                AND (
                                  p.Code = @PermissionCode
                                  OR (TRY_CONVERT(INT, p.Code) IS NOT NULL AND TRY_CONVERT(INT, @PermissionCode) IS NOT NULL AND TRY_CONVERT(INT, p.Code) = TRY_CONVERT(INT, @PermissionCode))
                                  OR (@PermissionCode IN (N'CASHBOOK_VIEW', N'1') AND p.Code IN (N'CASHBOOK_VIEW', N'1'))
                                  OR (@PermissionCode IN (N'TRANSACTION_VIEW', N'2') AND p.Code IN (N'TRANSACTION_VIEW', N'2'))
                                  OR (@PermissionCode IN (N'TRANSACTION_CREATE', N'3') AND p.Code IN (N'TRANSACTION_CREATE', N'3'))
                                  OR (@PermissionCode IN (N'TRANSACTION_UPDATE', N'4') AND p.Code IN (N'TRANSACTION_UPDATE', N'4'))
                                  OR (@PermissionCode IN (N'TRANSACTION_DELETE', N'5') AND p.Code IN (N'TRANSACTION_DELETE', N'5'))
                                )
                          )
                    ) THEN 1 ELSE 0 END AS BIT) AS IsAllowed;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_CreateCashbookInvitation
                    @BusinessId UNIQUEIDENTIFIER,@CashBookId UNIQUEIDENTIFIER,@InvitedEmail NVARCHAR(320),@InvitedByUserId UNIQUEIDENTIFIER,
                    @ExpiresOn DATETIME2=NULL,@TokenExpiresOn DATETIME2=NULL,@InviteToken NVARCHAR(200),@Remarks NVARCHAR(MAX)=NULL,
                    @PermissionCodes dbo.StringList READONLY
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    IF NOT EXISTS(
                        SELECT 1 FROM dbo.CashBooks cb
                        LEFT JOIN dbo.Businesses b ON b.Id = cb.BusinessId AND b.IsDeleted = 0
                        WHERE cb.Id = @CashBookId AND cb.IsDeleted = 0
                          AND (cb.UserId = @InvitedByUserId OR b.OwnerUserId = @InvitedByUserId OR b.UserId = @InvitedByUserId)
                    )
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Only the cashbook owner can invite members to this cashbook.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    IF NOT EXISTS(SELECT 1 FROM dbo.CashBooks WHERE Id=@CashBookId AND (BusinessId=@BusinessId OR @BusinessId IS NULL) AND IsDeleted=0)
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Cashbook not found for this business.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    DECLARE @Id UNIQUEIDENTIFIER=NEWID(),@Now DATETIME2=SYSUTCDATETIME(),@InvitedUserId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.Users WHERE Email=@InvitedEmail AND IsDeleted=0);
                    BEGIN TRANSACTION;
                    INSERT dbo.Invitations(Id,CreatedOn,CreatedBy,IsDeleted,IsActive,InvitationType,BusinessId,CashBookId,InvitedUserId,InvitedEmail,InvitedByUserId,Status,ExpiresOn,TokenExpiresOn,InviteToken,IsTokenConsumed,Remarks)
                    VALUES(@Id,@Now,CONVERT(NVARCHAR(36),@InvitedByUserId),0,1,N'CASHBOOK',@BusinessId,@CashBookId,@InvitedUserId,@InvitedEmail,@InvitedByUserId,N'Pending',@ExpiresOn,@TokenExpiresOn,@InviteToken,0,@Remarks);
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'CASHBOOK_VIEW' WHEN 2 THEN N'TRANSACTION_VIEW' WHEN 3 THEN N'TRANSACTION_CREATE'
                        WHEN 4 THEN N'TRANSACTION_UPDATE' WHEN 5 THEN N'TRANSACTION_DELETE' ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.Permissions(Id,Code,Name,Scope,CreatedOn,IsDeleted,IsActive)
                    SELECT NEWID(),C.Code,C.Code,N'CASHBOOK',@Now,0,1 FROM C
                    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions p WHERE p.Code=C.Code AND p.Scope=N'CASHBOOK' AND p.IsDeleted=0);
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'CASHBOOK_VIEW' WHEN 2 THEN N'TRANSACTION_VIEW' WHEN 3 THEN N'TRANSACTION_CREATE'
                        WHEN 4 THEN N'TRANSACTION_UPDATE' WHEN 5 THEN N'TRANSACTION_DELETE' ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.InvitationPermissions(Id,CreatedOn,CreatedBy,IsDeleted,IsActive,InvitationId,PermissionId,Granted)
                    SELECT NEWID(),@Now,CONVERT(NVARCHAR(36),@InvitedByUserId),0,1,@Id,p.Id,1
                    FROM C JOIN dbo.Permissions p ON p.Code=C.Code AND p.Scope=N'CASHBOOK' AND p.IsDeleted=0;
                    COMMIT;
                    SELECT CAST(1 AS BIT) Success,N'Cashbook invitation created.' Message,@Id Data;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_UpdateCashbookMemberPermissions
                    @BusinessId UNIQUEIDENTIFIER,@CashBookId UNIQUEIDENTIFIER,@UserId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER,@PermissionCodes dbo.StringList READONLY
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    IF @CurrentUserId = @UserId
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Members cannot modify their own permissions.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    IF NOT EXISTS(
                        SELECT 1 FROM dbo.CashBooks cb
                        LEFT JOIN dbo.Businesses b ON b.Id = cb.BusinessId AND b.IsDeleted = 0
                        WHERE cb.Id = @CashBookId AND cb.IsDeleted = 0
                          AND (cb.UserId = @CurrentUserId OR b.OwnerUserId = @CurrentUserId OR b.UserId = @CurrentUserId)
                    )
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Only the cashbook owner can manage member permissions.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    DECLARE @MemberId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.CashbookMembers WHERE (BusinessId=@BusinessId OR @BusinessId IS NULL) AND CashBookId=@CashBookId AND UserId=@UserId AND IsDeleted=0);
                    IF @MemberId IS NULL BEGIN SELECT CAST(0 AS BIT) Success,N'Member not found.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    BEGIN TRANSACTION;
                    UPDATE dbo.CashbookMemberPermissions SET IsDeleted=1,IsActive=0,ModifiedOn=SYSUTCDATETIME() WHERE CashbookMemberId=@MemberId AND IsDeleted=0;
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'CASHBOOK_VIEW' WHEN 2 THEN N'TRANSACTION_VIEW' WHEN 3 THEN N'TRANSACTION_CREATE'
                        WHEN 4 THEN N'TRANSACTION_UPDATE' WHEN 5 THEN N'TRANSACTION_DELETE' ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.Permissions(Id,Code,Name,Scope,CreatedOn,IsDeleted,IsActive)
                    SELECT NEWID(),C.Code,C.Code,N'CASHBOOK',SYSUTCDATETIME(),0,1 FROM C
                    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions p WHERE p.Code=C.Code AND p.Scope=N'CASHBOOK' AND p.IsDeleted=0);
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'CASHBOOK_VIEW' WHEN 2 THEN N'TRANSACTION_VIEW' WHEN 3 THEN N'TRANSACTION_CREATE'
                        WHEN 4 THEN N'TRANSACTION_UPDATE' WHEN 5 THEN N'TRANSACTION_DELETE' ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.CashbookMemberPermissions(Id,CreatedOn,IsDeleted,IsActive,CashbookMemberId,PermissionId,Granted)
                    SELECT NEWID(),SYSUTCDATETIME(),0,1,@MemberId,p.Id,1 FROM C JOIN dbo.Permissions p ON p.Code=C.Code AND p.Scope=N'CASHBOOK' AND p.IsDeleted=0;
                    COMMIT;
                    SELECT CAST(1 AS BIT) Success,N'Cashbook member permissions updated.' Message,@MemberId Data;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_RemoveCashbookMember
                    @BusinessId UNIQUEIDENTIFIER,@CashBookId UNIQUEIDENTIFIER,@UserId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF @CurrentUserId <> @UserId AND NOT EXISTS(
                        SELECT 1 FROM dbo.CashBooks cb
                        LEFT JOIN dbo.Businesses b ON b.Id = cb.BusinessId AND b.IsDeleted = 0
                        WHERE cb.Id = @CashBookId AND cb.IsDeleted = 0
                          AND (cb.UserId = @CurrentUserId OR b.OwnerUserId = @CurrentUserId OR b.UserId = @CurrentUserId)
                    )
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Only the cashbook owner can remove members from this cashbook.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    UPDATE dbo.CashbookMembers SET Status=N'Revoked',IsDeleted=1,IsActive=0,ModifiedOn=SYSUTCDATETIME()
                    WHERE (BusinessId=@BusinessId OR @BusinessId IS NULL) AND CashBookId=@CashBookId AND UserId=@UserId AND IsDeleted=0;
                    DECLARE @Ok BIT=IIF(@@ROWCOUNT>0,1,0);
                    SELECT @Ok Success,IIF(@Ok=1,N'Cashbook member removed.',N'Member not found.') Message,@UserId Data;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
