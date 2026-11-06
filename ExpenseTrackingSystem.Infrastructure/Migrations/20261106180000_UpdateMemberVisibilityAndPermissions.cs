using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261106180000_UpdateMemberVisibilityAndPermissions")]
    public partial class UpdateMemberVisibilityAndPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_HasCashbookPermission
                    @CashBookId UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER, @PermissionCode NVARCHAR(200)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.CashbookMembers cm
                        WHERE cm.CashBookId=@CashBookId AND cm.UserId=@UserId AND cm.Status=N'Active' AND cm.IsDeleted=0 AND cm.IsActive=1
                          AND (
                            @PermissionCode IN (N'CASHBOOK_VIEW', N'1', N'TRANSACTION_VIEW', N'2')
                            OR EXISTS (
                                SELECT 1 FROM dbo.CashbookMemberPermissions cmp
                                JOIN dbo.Permissions p ON p.Id=cmp.PermissionId AND p.IsDeleted=0
                                WHERE cmp.CashbookMemberId=cm.Id AND cmp.Granted=1 AND cmp.IsDeleted=0
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
                            OR (
                                @PermissionCode IN (N'TRANSACTION_CREATE', N'3')
                                AND NOT EXISTS (
                                    SELECT 1 FROM dbo.CashbookMemberPermissions cmp2
                                    WHERE cmp2.CashbookMemberId=cm.Id AND cmp2.IsDeleted=0
                                )
                            )
                          )
                    ) THEN 1 ELSE 0 END AS BIT) AS IsAllowed;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_HasBusinessPermission
                    @BusinessId UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER, @PermissionCode NVARCHAR(200)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.BusinessMembers bm
                        WHERE bm.BusinessId=@BusinessId AND bm.UserId=@UserId AND bm.Status=N'Active' AND bm.IsDeleted=0 AND bm.IsActive=1
                          AND (
                            @PermissionCode IN (N'BUSINESS_VIEW', N'1')
                            OR EXISTS (
                                SELECT 1 FROM dbo.BusinessMemberPermissions bmp
                                JOIN dbo.Permissions p ON p.Id=bmp.PermissionId AND p.IsDeleted=0
                                WHERE bmp.BusinessMemberId=bm.Id AND bmp.Granted=1 AND bmp.IsDeleted=0
                                  AND (
                                    p.Code = @PermissionCode
                                    OR (TRY_CONVERT(INT, p.Code) IS NOT NULL AND TRY_CONVERT(INT, @PermissionCode) IS NOT NULL AND TRY_CONVERT(INT, p.Code) = TRY_CONVERT(INT, @PermissionCode))
                                    OR (@PermissionCode IN (N'BUSINESS_VIEW', N'1') AND p.Code IN (N'BUSINESS_VIEW', N'1'))
                                    OR (@PermissionCode IN (N'CASHBOOK_LIST_VIEW', N'2') AND p.Code IN (N'CASHBOOK_LIST_VIEW', N'2'))
                                    OR (@PermissionCode IN (N'BUSINESS_UPDATE', N'3') AND p.Code IN (N'BUSINESS_UPDATE', N'3'))
                                    OR (@PermissionCode IN (N'CASHBOOK_CREATE', N'4') AND p.Code IN (N'CASHBOOK_CREATE', N'4'))
                                    OR (@PermissionCode IN (N'CASHBOOK_UPDATE', N'5') AND p.Code IN (N'CASHBOOK_UPDATE', N'5'))
                                    OR (@PermissionCode IN (N'CASHBOOK_DELETE', N'6') AND p.Code IN (N'CASHBOOK_DELETE', N'6'))
                                  )
                            )
                          )
                    ) THEN 1 ELSE 0 END AS BIT) AS IsAllowed;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetBusinessMembers @BusinessId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF NOT EXISTS(
                        SELECT 1 FROM dbo.Businesses WHERE Id=@BusinessId AND IsDeleted=0
                          AND (OwnerUserId=@CurrentUserId OR UserId=@CurrentUserId OR EXISTS (
                              SELECT 1 FROM dbo.BusinessMembers bm WHERE bm.BusinessId=@BusinessId AND bm.UserId=@CurrentUserId AND bm.Status=N'Active' AND bm.IsDeleted=0
                          ) OR EXISTS (
                              SELECT 1 FROM dbo.CashbookMembers cm WHERE (cm.BusinessId=@BusinessId OR EXISTS (SELECT 1 FROM dbo.CashBooks cb WHERE cb.Id=cm.CashBookId AND cb.BusinessId=@BusinessId AND cb.IsDeleted=0)) AND cm.UserId=@CurrentUserId AND cm.Status=N'Active' AND cm.IsDeleted=0
                          ))
                    )
                    BEGIN
                        SELECT CAST(NULL AS UNIQUEIDENTIFIER) BusinessMemberId,CAST(NULL AS UNIQUEIDENTIFIER) BusinessId,CAST(NULL AS UNIQUEIDENTIFIER) UserId,
                            CAST(NULL AS NVARCHAR(MAX)) FullName,CAST(NULL AS NVARCHAR(MAX)) Email,CAST(NULL AS NVARCHAR(MAX)) Status,
                            CAST(NULL AS DATETIME2) JoinedOn,CAST(NULL AS DATETIME2) InvitedOn WHERE 1=0;
                        SELECT CAST(NULL AS UNIQUEIDENTIFIER) MemberId,CAST(NULL AS NVARCHAR(MAX)) Code WHERE 1=0;
                        RETURN;
                    END;

                    SELECT bm.Id BusinessMemberId,bm.BusinessId,bm.UserId,
                        LTRIM(RTRIM(CONCAT(u.FirstName,N' ',u.LastName))) FullName,COALESCE(u.Email,i.InvitedEmail,N'') Email,
                        bm.Status,bm.JoinedOn,bm.InvitedOn
                    FROM dbo.BusinessMembers bm
                    LEFT JOIN dbo.Users u ON u.Id=bm.UserId
                    LEFT JOIN dbo.Invitations i ON i.Id=bm.InvitationId
                    WHERE bm.BusinessId=@BusinessId AND bm.IsDeleted=0

                    UNION ALL

                    SELECT cm.Id BusinessMemberId,@BusinessId BusinessId,cm.UserId,
                        LTRIM(RTRIM(CONCAT(u.FirstName,N' ',u.LastName))) FullName,COALESCE(u.Email,i.InvitedEmail,N'') Email,
                        cm.Status,cm.AddedOn JoinedOn,cm.InvitedOn
                    FROM dbo.CashbookMembers cm
                    LEFT JOIN dbo.Users u ON u.Id=cm.UserId
                    LEFT JOIN dbo.Invitations i ON i.Id=cm.InvitationId
                    WHERE (cm.BusinessId=@BusinessId OR EXISTS (SELECT 1 FROM dbo.CashBooks cb WHERE cb.Id=cm.CashBookId AND cb.BusinessId=@BusinessId AND cb.IsDeleted=0))
                      AND cm.IsDeleted=0
                      AND NOT EXISTS (SELECT 1 FROM dbo.BusinessMembers bm2 WHERE bm2.BusinessId=@BusinessId AND bm2.UserId=cm.UserId AND bm2.IsDeleted=0);

                    SELECT bmp.BusinessMemberId MemberId,p.Code
                    FROM dbo.BusinessMemberPermissions bmp
                    JOIN dbo.Permissions p ON p.Id=bmp.PermissionId
                    JOIN dbo.BusinessMembers bm ON bm.Id=bmp.BusinessMemberId
                    WHERE bm.BusinessId=@BusinessId AND bm.IsDeleted=0 AND bmp.IsDeleted=0 AND bmp.Granted=1

                    UNION ALL

                    SELECT cmp.CashbookMemberId MemberId,p.Code
                    FROM dbo.CashbookMemberPermissions cmp
                    JOIN dbo.Permissions p ON p.Id=cmp.PermissionId
                    JOIN dbo.CashbookMembers cm ON cm.Id=cmp.CashbookMemberId
                    WHERE (cm.BusinessId=@BusinessId OR EXISTS (SELECT 1 FROM dbo.CashBooks cb WHERE cb.Id=cm.CashBookId AND cb.BusinessId=@BusinessId AND cb.IsDeleted=0))
                      AND cm.IsDeleted=0 AND cmp.IsDeleted=0 AND cmp.Granted=1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetCashbookMembers @BusinessId UNIQUEIDENTIFIER,@CashBookId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF NOT EXISTS(
                        SELECT 1 FROM dbo.Businesses WHERE Id=@BusinessId AND IsDeleted=0
                          AND (
                            OwnerUserId=@CurrentUserId OR UserId=@CurrentUserId
                            OR EXISTS (SELECT 1 FROM dbo.BusinessMembers bm WHERE bm.BusinessId=@BusinessId AND bm.UserId=@CurrentUserId AND bm.Status=N'Active' AND bm.IsDeleted=0)
                            OR EXISTS (SELECT 1 FROM dbo.CashbookMembers cm WHERE cm.CashBookId=@CashBookId AND cm.UserId=@CurrentUserId AND cm.Status=N'Active' AND cm.IsDeleted=0)
                          )
                    )
                    BEGIN
                        SELECT CAST(NULL AS UNIQUEIDENTIFIER) CashbookMemberId,CAST(NULL AS UNIQUEIDENTIFIER) CashBookId,CAST(NULL AS UNIQUEIDENTIFIER) BusinessId,
                            CAST(NULL AS UNIQUEIDENTIFIER) UserId,CAST(NULL AS NVARCHAR(MAX)) FullName,CAST(NULL AS NVARCHAR(MAX)) Email,
                            CAST(NULL AS NVARCHAR(MAX)) Status,CAST(NULL AS DATETIME2) AddedOn,CAST(NULL AS DATETIME2) InvitedOn WHERE 1=0;
                        SELECT CAST(NULL AS UNIQUEIDENTIFIER) MemberId,CAST(NULL AS NVARCHAR(MAX)) Code WHERE 1=0;
                        RETURN;
                    END;
                    SELECT cm.Id CashbookMemberId,cm.CashBookId,COALESCE(cm.BusinessId, @BusinessId) BusinessId,cm.UserId,
                        LTRIM(RTRIM(CONCAT(u.FirstName,N' ',u.LastName))) FullName,COALESCE(u.Email,i.InvitedEmail,N'') Email,
                        cm.Status,cm.AddedOn,cm.InvitedOn
                    FROM dbo.CashbookMembers cm LEFT JOIN dbo.Users u ON u.Id=cm.UserId
                    LEFT JOIN dbo.Invitations i ON i.Id=cm.InvitationId
                    WHERE cm.CashBookId=@CashBookId AND cm.IsDeleted=0;

                    SELECT cmp.CashbookMemberId MemberId,p.Code
                    FROM dbo.CashbookMemberPermissions cmp JOIN dbo.Permissions p ON p.Id=cmp.PermissionId
                    JOIN dbo.CashbookMembers cm ON cm.Id=cmp.CashbookMemberId
                    WHERE cm.CashBookId=@CashBookId AND cm.IsDeleted=0 AND cmp.IsDeleted=0 AND cmp.Granted=1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetMySentInvitations @UserId UNIQUEIDENTIFIER,@Status NVARCHAR(50)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT i.Id,i.InvitationType,i.BusinessId,b.Name BusinessName,i.CashBookId,cb.Name CashbookName,
                        i.InvitedUserId,i.InvitedEmail,i.InvitedByUserId,
                        LTRIM(RTRIM(CONCAT(sender.FirstName,N' ',sender.LastName))) InvitedByName,sender.Email InvitedByEmail,
                        i.Status,i.ExpiresOn,i.RespondedOn,i.Remarks,i.CreatedOn
                    FROM dbo.Invitations i JOIN dbo.Businesses b ON b.Id=i.BusinessId
                    LEFT JOIN dbo.CashBooks cb ON cb.Id=i.CashBookId LEFT JOIN dbo.Users sender ON sender.Id=i.InvitedByUserId
                    WHERE i.InvitedByUserId=@UserId AND i.IsDeleted=0
                      AND (@Status IS NULL OR @Status = N'' OR @Status = N'All' OR i.Status=@Status);

                    SELECT ip.InvitationId,p.Code FROM dbo.InvitationPermissions ip JOIN dbo.Permissions p ON p.Id=ip.PermissionId
                    JOIN dbo.Invitations i ON i.Id=ip.InvitationId
                    WHERE i.InvitedByUserId=@UserId AND i.IsDeleted=0
                      AND (@Status IS NULL OR @Status = N'' OR @Status = N'All' OR i.Status=@Status) AND ip.IsDeleted=0 AND ip.Granted=1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetInvitationByToken @InviteToken NVARCHAR(200)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT i.Id,i.InvitationType,i.BusinessId,b.Name BusinessName,i.CashBookId,cb.Name CashbookName,
                        i.InvitedUserId,i.InvitedEmail,
                        i.InvitedByUserId,LTRIM(RTRIM(CONCAT(sender.FirstName,N' ',sender.LastName))) InvitedByName,
                        sender.Email InvitedByEmail,i.Status,i.ExpiresOn,i.TokenExpiresOn,i.IsTokenConsumed,i.Remarks,i.CreatedOn
                    FROM dbo.Invitations i JOIN dbo.Businesses b ON b.Id=i.BusinessId
                    LEFT JOIN dbo.CashBooks cb ON cb.Id=i.CashBookId LEFT JOIN dbo.Users sender ON sender.Id=i.InvitedByUserId
                    WHERE (i.InviteToken=@InviteToken OR CONVERT(NVARCHAR(36), i.Id)=@InviteToken) AND i.IsDeleted=0;

                    SELECT ip.InvitationId,p.Code FROM dbo.InvitationPermissions ip JOIN dbo.Permissions p ON p.Id=ip.PermissionId
                    JOIN dbo.Invitations i ON i.Id=ip.InvitationId
                    WHERE (i.InviteToken=@InviteToken OR CONVERT(NVARCHAR(36), i.Id)=@InviteToken) AND ip.IsDeleted=0 AND ip.Granted=1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_UpdateBusinessMemberPermissions
                    @BusinessId UNIQUEIDENTIFIER,@UserId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER,@PermissionCodes dbo.StringList READONLY
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    IF NOT EXISTS(SELECT 1 FROM dbo.Businesses WHERE Id=@BusinessId AND (OwnerUserId=@CurrentUserId OR UserId=@CurrentUserId) AND IsDeleted=0)
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Only the business owner can change member permissions.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    DECLARE @MemberId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.BusinessMembers WHERE BusinessId=@BusinessId AND UserId=@UserId AND IsDeleted=0);
                    IF @MemberId IS NULL
                    BEGIN
                        IF EXISTS (SELECT 1 FROM dbo.CashbookMembers WHERE (BusinessId=@BusinessId OR EXISTS (SELECT 1 FROM dbo.CashBooks cb WHERE cb.Id=CashBookId AND cb.BusinessId=@BusinessId)) AND UserId=@UserId AND IsDeleted=0)
                           OR EXISTS (SELECT 1 FROM dbo.Invitations WHERE BusinessId=@BusinessId AND InvitedUserId=@UserId)
                        BEGIN
                            SET @MemberId = NEWID();
                            INSERT dbo.BusinessMembers(Id,CreatedOn,IsDeleted,IsActive,BusinessId,UserId,Status,JoinedOn,InvitedOn)
                            VALUES(@MemberId,SYSUTCDATETIME(),0,1,@BusinessId,@UserId,N'Active',SYSUTCDATETIME(),SYSUTCDATETIME());
                        END
                        ELSE
                        BEGIN
                            SELECT CAST(0 AS BIT) Success,N'Member not found.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data;
                            RETURN;
                        END
                    END;
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

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_UpdateCashbookMemberPermissions
                    @BusinessId UNIQUEIDENTIFIER,@CashBookId UNIQUEIDENTIFIER,@UserId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER,@PermissionCodes dbo.StringList READONLY
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
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
                CREATE OR ALTER PROCEDURE dbo.SP_CreateCashbookInvitation
                    @BusinessId UNIQUEIDENTIFIER,@CashBookId UNIQUEIDENTIFIER,@InvitedEmail NVARCHAR(320),@InvitedByUserId UNIQUEIDENTIFIER,
                    @ExpiresOn DATETIME2=NULL,@TokenExpiresOn DATETIME2=NULL,@InviteToken NVARCHAR(200),@Remarks NVARCHAR(MAX)=NULL,
                    @PermissionCodes dbo.StringList READONLY
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
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
                CREATE OR ALTER PROCEDURE dbo.SP_CreateBusinessInvitation
                    @BusinessId UNIQUEIDENTIFIER,@InvitedEmail NVARCHAR(320),@InvitedByUserId UNIQUEIDENTIFIER,
                    @ExpiresOn DATETIME2=NULL,@TokenExpiresOn DATETIME2=NULL,@InviteToken NVARCHAR(200),@Remarks NVARCHAR(MAX)=NULL,
                    @PermissionCodes dbo.StringList READONLY
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    IF NOT EXISTS(SELECT 1 FROM dbo.Businesses WHERE Id=@BusinessId AND (OwnerUserId=@InvitedByUserId OR UserId=@InvitedByUserId) AND IsDeleted=0)
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Business not found or access denied.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    DECLARE @Id UNIQUEIDENTIFIER=NEWID(),@Now DATETIME2=SYSUTCDATETIME(),@InvitedUserId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.Users WHERE Email=@InvitedEmail AND IsDeleted=0);
                    BEGIN TRANSACTION;
                    INSERT dbo.Invitations(Id,CreatedOn,CreatedBy,IsDeleted,IsActive,InvitationType,BusinessId,InvitedUserId,InvitedEmail,InvitedByUserId,Status,ExpiresOn,TokenExpiresOn,InviteToken,IsTokenConsumed,Remarks)
                    VALUES(@Id,@Now,CONVERT(NVARCHAR(36),@InvitedByUserId),0,1,N'BUSINESS',@BusinessId,@InvitedUserId,@InvitedEmail,@InvitedByUserId,N'Pending',@ExpiresOn,@TokenExpiresOn,@InviteToken,0,@Remarks);
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'BUSINESS_VIEW' WHEN 2 THEN N'CASHBOOK_LIST_VIEW' WHEN 3 THEN N'BUSINESS_UPDATE'
                        WHEN 4 THEN N'CASHBOOK_CREATE' WHEN 5 THEN N'CASHBOOK_UPDATE' WHEN 6 THEN N'CASHBOOK_DELETE'
                        ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.Permissions(Id,Code,Name,Scope,CreatedOn,IsDeleted,IsActive)
                    SELECT NEWID(),C.Code,C.Code,N'BUSINESS',@Now,0,1 FROM C
                    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions p WHERE p.Code=C.Code AND p.Scope=N'BUSINESS' AND p.IsDeleted=0);
                    ;WITH C AS (SELECT DISTINCT CASE TRY_CONVERT(INT,Value)
                        WHEN 1 THEN N'BUSINESS_VIEW' WHEN 2 THEN N'CASHBOOK_LIST_VIEW' WHEN 3 THEN N'BUSINESS_UPDATE'
                        WHEN 4 THEN N'CASHBOOK_CREATE' WHEN 5 THEN N'CASHBOOK_UPDATE' WHEN 6 THEN N'CASHBOOK_DELETE'
                        ELSE Value END Code FROM @PermissionCodes)
                    INSERT dbo.InvitationPermissions(Id,CreatedOn,CreatedBy,IsDeleted,IsActive,InvitationId,PermissionId,Granted)
                    SELECT NEWID(),@Now,CONVERT(NVARCHAR(36),@InvitedByUserId),0,1,@Id,p.Id,1
                    FROM C JOIN dbo.Permissions p ON p.Code=C.Code AND p.Scope=N'BUSINESS' AND p.IsDeleted=0;
                    COMMIT;
                    SELECT CAST(1 AS BIT) Success,N'Business invitation created.' Message,@Id Data;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
