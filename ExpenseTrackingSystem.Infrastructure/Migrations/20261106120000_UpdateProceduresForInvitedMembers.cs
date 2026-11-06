using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261106120000_UpdateProceduresForInvitedMembers")]
    public partial class UpdateProceduresForInvitedMembers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetAllBusinessesByUserId @UserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @Data NVARCHAR(MAX)=(
                        SELECT DISTINCT b.Id, b.Name, b.Category, b.Description, b.FileName
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
                CREATE OR ALTER PROCEDURE dbo.SP_GetCashBooksByBusinessId
                    @BusinessId UNIQUEIDENTIFIER,
                    @UserId UNIQUEIDENTIFIER = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @IsOwner BIT = 0;
                    DECLARE @HasBusinessView BIT = 0;

                    IF @UserId IS NOT NULL
                    BEGIN
                        IF EXISTS (SELECT 1 FROM dbo.Businesses WHERE Id = @BusinessId AND (UserId = @UserId OR OwnerUserId = @UserId) AND IsDeleted = 0)
                            SET @IsOwner = 1;
                        ELSE IF EXISTS (
                            SELECT 1 FROM dbo.BusinessMembers bm
                            JOIN dbo.BusinessMemberPermissions bmp ON bmp.BusinessMemberId = bm.Id AND bmp.Granted = 1 AND bmp.IsDeleted = 0
                            JOIN dbo.Permissions p ON p.Id = bmp.PermissionId AND p.IsDeleted = 0
                            WHERE bm.BusinessId = @BusinessId AND bm.UserId = @UserId AND bm.Status = N'Active' AND bm.IsDeleted = 0
                              AND (p.Code IN (N'CASHBOOK_LIST_VIEW', N'BUSINESS_VIEW', N'1', N'2'))
                        )
                            SET @HasBusinessView = 1;
                    END;

                    DECLARE @Data NVARCHAR(MAX)=(
                        SELECT Id, CashBookName = Name, Name, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDeleted, UserId, BusinessId
                        FROM dbo.CashBooks cb
                        WHERE cb.BusinessId = @BusinessId AND cb.IsDeleted = 0
                          AND (
                            @UserId IS NULL
                            OR @IsOwner = 1
                            OR @HasBusinessView = 1
                            OR cb.UserId = @UserId
                            OR EXISTS (
                                SELECT 1 FROM dbo.CashbookMembers cm
                                WHERE cm.CashBookId = cb.Id AND cm.UserId = @UserId AND cm.Status = N'Active' AND cm.IsDeleted = 0 AND cm.IsActive = 1
                            )
                          )
                        FOR JSON PATH
                    );

                    SELECT 200 ResponseCode, CAST(1 AS BIT) Success, N'Cashbooks fetched.' Message, @Data Data;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.sp_GetCashBookById @Id UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @Data NVARCHAR(MAX)=(
                        SELECT cb.Id, cb.Name, cb.CreatedOn, cb.CreatedBy, cb.ModifiedOn, cb.ModifiedBy, cb.IsActive, cb.IsDeleted, cb.UserId, cb.BusinessId
                        FROM dbo.CashBooks cb
                        WHERE cb.Id = @Id AND cb.IsDeleted = 0
                          AND (
                            cb.UserId = @UserId
                            OR EXISTS (
                                SELECT 1 FROM dbo.CashbookMembers cm
                                WHERE cm.CashBookId = @Id AND cm.UserId = @UserId AND cm.Status = N'Active' AND cm.IsDeleted = 0 AND cm.IsActive = 1
                            )
                            OR EXISTS (
                                SELECT 1 FROM dbo.Businesses b
                                WHERE b.Id = cb.BusinessId AND b.IsDeleted = 0
                                  AND (
                                    b.UserId = @UserId
                                    OR b.OwnerUserId = @UserId
                                    OR EXISTS (
                                        SELECT 1 FROM dbo.BusinessMembers bm
                                        WHERE bm.BusinessId = b.Id AND bm.UserId = @UserId AND bm.Status = N'Active' AND bm.IsDeleted = 0 AND bm.IsActive = 1
                                    )
                                  )
                            )
                          )
                        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                    );
                    SELECT IIF(@Data IS NULL,404,200) ResponseCode, CAST(IIF(@Data IS NULL,0,1) AS BIT) Success,
                           IIF(@Data IS NULL,N'Cashbook not found.',N'Cashbook fetched.') Message, @Data Data;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_HasAnyCashbookAccessInBusiness
                    @BusinessId UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.CashbookMembers cm
                        WHERE (cm.BusinessId = @BusinessId OR EXISTS (SELECT 1 FROM dbo.CashBooks cb WHERE cb.Id = cm.CashBookId AND cb.BusinessId = @BusinessId AND cb.IsDeleted = 0))
                          AND cm.UserId = @UserId AND cm.Status = N'Active' AND cm.IsDeleted = 0 AND cm.IsActive = 1
                    ) THEN 1 ELSE 0 END AS BIT) AS IsAllowed;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_AcceptInvitation @InvitationId UNIQUEIDENTIFIER,@CurrentUserId UNIQUEIDENTIFIER,@CurrentUserEmail NVARCHAR(320)
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    DECLARE @Type NVARCHAR(50),@BusinessId UNIQUEIDENTIFIER,@CashBookId UNIQUEIDENTIFIER,@InvitedUserId UNIQUEIDENTIFIER,@Email NVARCHAR(320),@Status NVARCHAR(50),@Expires DATETIME2,@Now DATETIME2=SYSUTCDATETIME();
                    SELECT @Type=InvitationType,@BusinessId=BusinessId,@CashBookId=CashBookId,@InvitedUserId=InvitedUserId,@Email=InvitedEmail,@Status=Status,@Expires=ExpiresOn
                    FROM dbo.Invitations WHERE Id=@InvitationId AND IsDeleted=0;
                    IF @Type IS NULL OR @Status<>N'Pending' OR (@Expires IS NOT NULL AND @Expires<=@Now) OR LOWER(RTRIM(LTRIM(@Email)))<>LOWER(RTRIM(LTRIM(@CurrentUserEmail))) OR (@InvitedUserId IS NOT NULL AND @InvitedUserId<>@CurrentUserId)
                    BEGIN SELECT CAST(0 AS BIT) Success,N'Invitation is invalid, expired, or does not belong to this user.' Message,CAST(NULL AS UNIQUEIDENTIFIER) Data; RETURN; END;
                    BEGIN TRANSACTION;
                    IF @Type=N'BUSINESS'
                    BEGIN
                        IF EXISTS(SELECT 1 FROM dbo.BusinessMembers WHERE BusinessId=@BusinessId AND UserId=@CurrentUserId)
                            UPDATE dbo.BusinessMembers SET IsDeleted=0,IsActive=1,Status=N'Active',JoinedOn=@Now,InvitationId=@InvitationId,ModifiedOn=@Now WHERE BusinessId=@BusinessId AND UserId=@CurrentUserId;
                        ELSE INSERT dbo.BusinessMembers(Id,CreatedOn,IsDeleted,IsActive,BusinessId,UserId,InvitationId,Status,JoinedOn,InvitedOn,InvitedByUserId,RespondedOn)
                            VALUES(NEWID(),@Now,0,1,@BusinessId,@CurrentUserId,@InvitationId,N'Active',@Now,@Now,(SELECT InvitedByUserId FROM dbo.Invitations WHERE Id=@InvitationId),@Now);
                        DECLARE @BM UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.BusinessMembers WHERE BusinessId=@BusinessId AND UserId=@CurrentUserId AND IsDeleted=0);
                        INSERT dbo.BusinessMemberPermissions(Id,CreatedOn,IsDeleted,IsActive,BusinessMemberId,PermissionId,Granted)
                        SELECT NEWID(),@Now,0,1,@BM,ip.PermissionId,1
                        FROM dbo.InvitationPermissions ip
                        WHERE ip.InvitationId=@InvitationId AND ip.IsDeleted=0 AND ip.Granted=1
                          AND NOT EXISTS (
                              SELECT 1 FROM dbo.BusinessMemberPermissions bmp
                              WHERE bmp.BusinessMemberId=@BM AND bmp.PermissionId=ip.PermissionId AND bmp.IsDeleted=0
                          );
                    END
                    ELSE
                    BEGIN
                        IF EXISTS(SELECT 1 FROM dbo.CashbookMembers WHERE CashBookId=@CashBookId AND UserId=@CurrentUserId)
                            UPDATE dbo.CashbookMembers SET IsDeleted=0,IsActive=1,Status=N'Active',AddedOn=@Now,InvitationId=@InvitationId,ModifiedOn=@Now WHERE CashBookId=@CashBookId AND UserId=@CurrentUserId;
                        ELSE INSERT dbo.CashbookMembers(Id,CreatedOn,IsDeleted,IsActive,CashBookId,BusinessId,UserId,InvitationId,Status,AddedOn,InvitedOn,InvitedByUserId,RespondedOn)
                            VALUES(NEWID(),@Now,0,1,@CashBookId,@BusinessId,@CurrentUserId,@InvitationId,N'Active',@Now,@Now,(SELECT InvitedByUserId FROM dbo.Invitations WHERE Id=@InvitationId),@Now);
                        DECLARE @CM UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.CashbookMembers WHERE CashBookId=@CashBookId AND UserId=@CurrentUserId AND IsDeleted=0);
                        INSERT dbo.CashbookMemberPermissions(Id,CreatedOn,IsDeleted,IsActive,CashbookMemberId,PermissionId,Granted)
                        SELECT NEWID(),@Now,0,1,@CM,ip.PermissionId,1
                        FROM dbo.InvitationPermissions ip
                        WHERE ip.InvitationId=@InvitationId AND ip.IsDeleted=0 AND ip.Granted=1
                          AND NOT EXISTS (
                              SELECT 1 FROM dbo.CashbookMemberPermissions cmp
                              WHERE cmp.CashbookMemberId=@CM AND cmp.PermissionId=ip.PermissionId AND cmp.IsDeleted=0
                          );
                    END;
                    UPDATE dbo.Invitations SET Status=N'Accepted',InvitedUserId=@CurrentUserId,RespondedOn=@Now,IsTokenConsumed=1 WHERE Id=@InvitationId;
                    COMMIT;
                    SELECT CAST(1 AS BIT) Success,N'Invitation accepted.' Message,@InvitationId Data;
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
                    FROM dbo.BusinessMembers bm LEFT JOIN dbo.Users u ON u.Id=bm.UserId
                    LEFT JOIN dbo.Invitations i ON i.Id=bm.InvitationId
                    WHERE bm.BusinessId=@BusinessId AND bm.IsDeleted=0;
                    SELECT bmp.BusinessMemberId MemberId,p.Code
                    FROM dbo.BusinessMemberPermissions bmp JOIN dbo.Permissions p ON p.Id=bmp.PermissionId
                    JOIN dbo.BusinessMembers bm ON bm.Id=bmp.BusinessMemberId
                    WHERE bm.BusinessId=@BusinessId AND bm.IsDeleted=0 AND bmp.IsDeleted=0 AND bmp.Granted=1;
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
                    SELECT cm.Id CashbookMemberId,cm.CashBookId,cm.BusinessId,cm.UserId,
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
