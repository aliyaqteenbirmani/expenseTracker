using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261107140000_EnforceInvitedUserPermissions")]
    public partial class EnforceInvitedUserPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetAllBusinessesByUserId @UserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @Data NVARCHAR(MAX)=(
                        SELECT DISTINCT b.Id, b.Name, b.Category, b.Description, b.FileName,
                            CASE WHEN (b.UserId = @UserId OR b.OwnerUserId = @UserId) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsOwner
                        FROM dbo.Businesses b
                        LEFT JOIN dbo.BusinessMembers bm ON bm.BusinessId = b.Id AND bm.UserId = @UserId AND bm.Status = N'Active' AND bm.IsDeleted = 0 AND bm.IsActive = 1
                        LEFT JOIN dbo.CashBooks cb ON cb.BusinessId = b.Id AND cb.IsDeleted = 0
                        LEFT JOIN dbo.CashbookMembers cm ON (cm.CashBookId = cb.Id OR cm.BusinessId = b.Id) AND cm.UserId = @UserId AND cm.Status = N'Active' AND cm.IsDeleted = 0 AND cm.IsActive = 1
                        WHERE b.IsDeleted = 0
                          AND (b.UserId = @UserId OR b.OwnerUserId = @UserId OR bm.Id IS NOT NULL OR cm.Id IS NOT NULL)
                        FOR JSON PATH
                    );
                    SELECT CAST(1 AS BIT) Success, N'Businesses fetched.' Message, @Data Data;
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
                    IF NOT EXISTS(SELECT 1 FROM dbo.Businesses WHERE Id=@BusinessId AND (OwnerUserId=@InvitedByUserId OR UserId=@InvitedByUserId) AND IsDeleted=0)
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Only the business owner can invite members to this cashbook.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    IF NOT EXISTS(SELECT 1 FROM dbo.CashBooks WHERE Id=@CashBookId AND BusinessId=@BusinessId AND IsDeleted=0)
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
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Members cannot change their own permissions.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    IF NOT EXISTS(SELECT 1 FROM dbo.Businesses WHERE Id=@BusinessId AND (OwnerUserId=@CurrentUserId OR UserId=@CurrentUserId) AND IsDeleted=0)
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Only the business owner can change member permissions.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
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
                CREATE OR ALTER PROCEDURE dbo.SP_UpdateBusinessMemberPermissions
                    @BusinessId UNIQUEIDENTIFIER,@UserId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER,@PermissionCodes dbo.StringList READONLY
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    IF @CurrentUserId = @UserId
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Members cannot change their own permissions.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    IF NOT EXISTS(SELECT 1 FROM dbo.Businesses WHERE Id=@BusinessId AND (OwnerUserId=@CurrentUserId OR UserId=@CurrentUserId) AND IsDeleted=0)
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Only the business owner can change member permissions.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    DECLARE @MemberId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.BusinessMembers WHERE BusinessId=@BusinessId AND UserId=@UserId AND IsDeleted=0);
                    IF @MemberId IS NULL BEGIN SELECT CAST(0 AS BIT) Success,N'Member not found.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    BEGIN TRANSACTION;
                    UPDATE dbo.BusinessMemberPermissions SET IsDeleted=1,IsActive=0,ModifiedOn=SYSUTCDATETIME() WHERE BusinessMemberId=@MemberId AND IsDeleted=0;
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'BUSINESS_VIEW' WHEN 2 THEN N'CASHBOOK_LIST_VIEW' WHEN 3 THEN N'BUSINESS_UPDATE'
                        WHEN 4 THEN N'CASHBOOK_CREATE' WHEN 5 THEN N'CASHBOOK_UPDATE' WHEN 6 THEN N'CASHBOOK_DELETE'
                        ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.Permissions(Id,Code,Name,Scope,CreatedOn,IsDeleted,IsActive)
                    SELECT NEWID(),C.Code,C.Code,N'BUSINESS',SYSUTCDATETIME(),0,1 FROM C
                    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions p WHERE p.Code=C.Code AND p.Scope=N'BUSINESS' AND p.IsDeleted=0);
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'BUSINESS_VIEW' WHEN 2 THEN N'CASHBOOK_LIST_VIEW' WHEN 3 THEN N'BUSINESS_UPDATE'
                        WHEN 4 THEN N'CASHBOOK_CREATE' WHEN 5 THEN N'CASHBOOK_UPDATE' WHEN 6 THEN N'CASHBOOK_DELETE'
                        ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.BusinessMemberPermissions(Id,CreatedOn,IsDeleted,IsActive,BusinessMemberId,PermissionId,Granted)
                    SELECT NEWID(),SYSUTCDATETIME(),0,1,@MemberId,p.Id,1 FROM C JOIN dbo.Permissions p ON p.Code=C.Code AND p.Scope=N'BUSINESS' AND p.IsDeleted=0;
                    COMMIT;
                    SELECT CAST(1 AS BIT) Success,N'Business member permissions updated.' Message,@MemberId Data;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
