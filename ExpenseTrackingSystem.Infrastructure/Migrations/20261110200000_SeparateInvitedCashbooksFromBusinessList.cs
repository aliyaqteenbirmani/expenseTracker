using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261110200000_SeparateInvitedCashbooksFromBusinessList")]
    public partial class SeparateInvitedCashbooksFromBusinessList : Migration
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
                        WHERE b.IsDeleted = 0
                          AND (b.UserId = @UserId OR b.OwnerUserId = @UserId OR bm.Id IS NOT NULL)
                        FOR JSON PATH
                    );
                    SELECT CAST(1 AS BIT) Success, N'Businesses fetched.' Message, @Data Data;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SP_GetCashBooksByBusinessId
                    @BusinessId UNIQUEIDENTIFIER = NULL,
                    @UserId UNIQUEIDENTIFIER = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @IsOwner BIT = 0;
                    DECLARE @HasBusinessView BIT = 0;

                    IF @UserId IS NOT NULL AND @BusinessId IS NOT NULL
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
                        SELECT 
                            cb.Id, 
                            CashBookName = cb.Name, 
                            cb.Name, 
                            cb.CreatedOn, 
                            cb.CreatedBy, 
                            cb.ModifiedOn, 
                            cb.ModifiedBy, 
                            cb.IsActive, 
                            cb.IsDeleted, 
                            cb.UserId, 
                            cb.BusinessId,
                            CASE 
                                WHEN cb.UserId = @UserId OR EXISTS (
                                    SELECT 1 FROM dbo.Businesses b 
                                    WHERE b.Id = cb.BusinessId AND (b.UserId = @UserId OR b.OwnerUserId = @UserId)
                                ) THEN CAST(1 AS BIT) 
                                ELSE CAST(0 AS BIT) 
                            END AS IsOwner,
                            CASE 
                                WHEN cb.UserId = @UserId OR EXISTS (
                                    SELECT 1 FROM dbo.Businesses b 
                                    WHERE b.Id = cb.BusinessId AND (b.UserId = @UserId OR b.OwnerUserId = @UserId)
                                ) THEN N'Owner' 
                                ELSE N'Member' 
                            END AS Role,
                            CASE 
                                WHEN cb.UserId = @UserId OR EXISTS (
                                    SELECT 1 FROM dbo.Businesses b 
                                    WHERE b.Id = cb.BusinessId AND (b.UserId = @UserId OR b.OwnerUserId = @UserId)
                                ) THEN NULL 
                                ELSE COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(u.FirstName, N' ', u.LastName))), N''), u.Email, cb.CreatedBy, N'Owner')
                            END AS OwnerName
                        FROM dbo.CashBooks cb
                        LEFT JOIN dbo.Users u ON u.Id = cb.UserId
                        WHERE cb.IsDeleted = 0
                          AND (
                            (
                                @BusinessId IS NOT NULL 
                                AND cb.BusinessId = @BusinessId 
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
                            )
                            OR (
                                @UserId IS NOT NULL 
                                AND EXISTS (
                                    SELECT 1 FROM dbo.CashbookMembers cm
                                    WHERE cm.CashBookId = cb.Id AND cm.UserId = @UserId AND cm.Status = N'Active' AND cm.IsDeleted = 0 AND cm.IsActive = 1
                                )
                            )
                          )
                        FOR JSON PATH
                    );

                    SELECT 200 ResponseCode, CAST(1 AS BIT) Success, N'Cashbooks fetched.' Message, @Data Data;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
