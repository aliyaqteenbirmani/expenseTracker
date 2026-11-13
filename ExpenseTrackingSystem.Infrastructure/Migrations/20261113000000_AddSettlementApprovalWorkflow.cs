using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpendwiseSystem.Infrastructure.Data.Migrations;

#nullable disable

namespace SpendwiseSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261113000000_AddSettlementApprovalWorkflow")]
    public partial class AddSettlementApprovalWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- ============================================================================
-- Migration: AddSettlementApprovalWorkflow
-- Adds status and audit columns to dbo.SplitSettlements, backfills existing
-- settlements as 'Confirmed' to preserve balances, and adds performance indexes.
-- ============================================================================

-- 1. Add Status column with default 'PendingReceiver'
IF COL_LENGTH('dbo.SplitSettlements', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SplitSettlements_Status DEFAULT ('PendingReceiver');
END;

-- 2. Add Audit and Action tracking columns
IF COL_LENGTH('dbo.SplitSettlements', 'ReceiverActionBy') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD ReceiverActionBy UNIQUEIDENTIFIER NULL;
END;

IF COL_LENGTH('dbo.SplitSettlements', 'ReceiverActionAt') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD ReceiverActionAt DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.SplitSettlements', 'OwnerActionBy') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD OwnerActionBy UNIQUEIDENTIFIER NULL;
END;

IF COL_LENGTH('dbo.SplitSettlements', 'OwnerActionAt') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD OwnerActionAt DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.SplitSettlements', 'DeclineReason') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD DeclineReason NVARCHAR(500) NULL;
END;

IF COL_LENGTH('dbo.SplitSettlements', 'CancelledAt') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD CancelledAt DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.SplitSettlements', 'CancelledBy') IS NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD CancelledBy UNIQUEIDENTIFIER NULL;
END;

-- 3. Backfill all existing settlements as 'Confirmed' so current balances remain unchanged
EXEC(N'UPDATE dbo.SplitSettlements SET Status = ''Confirmed'' WHERE Status IS NULL OR Status = ''PendingReceiver''');

-- 4. Foreign Keys
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SplitSettlements_Users_ReceiverActionBy')
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD CONSTRAINT FK_SplitSettlements_Users_ReceiverActionBy FOREIGN KEY (ReceiverActionBy) REFERENCES dbo.Users (Id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SplitSettlements_Users_OwnerActionBy')
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD CONSTRAINT FK_SplitSettlements_Users_OwnerActionBy FOREIGN KEY (OwnerActionBy) REFERENCES dbo.Users (Id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SplitSettlements_Users_CancelledBy')
BEGIN
    ALTER TABLE dbo.SplitSettlements
    ADD CONSTRAINT FK_SplitSettlements_Users_CancelledBy FOREIGN KEY (CancelledBy) REFERENCES dbo.Users (Id);
END;

-- 5. Indexes for status and pending lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitSettlements') AND name = N'IX_SplitSettlements_GroupId_Status_IsDeleted')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SplitSettlements_GroupId_Status_IsDeleted
    ON dbo.SplitSettlements (GroupId, Status, IsDeleted)
    INCLUDE (FromUserId, ToUserId, Amount, SettlementDate);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitSettlements') AND name = N'IX_SplitSettlements_FromUserId_Status')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SplitSettlements_FromUserId_Status
    ON dbo.SplitSettlements (FromUserId, Status, IsDeleted);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitSettlements') AND name = N'IX_SplitSettlements_ToUserId_Status')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SplitSettlements_ToUserId_Status
    ON dbo.SplitSettlements (ToUserId, Status, IsDeleted);
END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS IX_SplitSettlements_ToUserId_Status ON dbo.SplitSettlements;
DROP INDEX IF EXISTS IX_SplitSettlements_FromUserId_Status ON dbo.SplitSettlements;
DROP INDEX IF EXISTS IX_SplitSettlements_GroupId_Status_IsDeleted ON dbo.SplitSettlements;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SplitSettlements_Users_CancelledBy')
    ALTER TABLE dbo.SplitSettlements DROP CONSTRAINT FK_SplitSettlements_Users_CancelledBy;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SplitSettlements_Users_OwnerActionBy')
    ALTER TABLE dbo.SplitSettlements DROP CONSTRAINT FK_SplitSettlements_Users_OwnerActionBy;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SplitSettlements_Users_ReceiverActionBy')
    ALTER TABLE dbo.SplitSettlements DROP CONSTRAINT FK_SplitSettlements_Users_ReceiverActionBy;

IF COL_LENGTH('dbo.SplitSettlements', 'Status') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SplitSettlements DROP CONSTRAINT IF EXISTS DF_SplitSettlements_Status;
    ALTER TABLE dbo.SplitSettlements DROP COLUMN Status;
END;

IF COL_LENGTH('dbo.SplitSettlements', 'ReceiverActionBy') IS NOT NULL
    ALTER TABLE dbo.SplitSettlements DROP COLUMN ReceiverActionBy;

IF COL_LENGTH('dbo.SplitSettlements', 'ReceiverActionAt') IS NOT NULL
    ALTER TABLE dbo.SplitSettlements DROP COLUMN ReceiverActionAt;

IF COL_LENGTH('dbo.SplitSettlements', 'OwnerActionBy') IS NOT NULL
    ALTER TABLE dbo.SplitSettlements DROP COLUMN OwnerActionBy;

IF COL_LENGTH('dbo.SplitSettlements', 'OwnerActionAt') IS NOT NULL
    ALTER TABLE dbo.SplitSettlements DROP COLUMN OwnerActionAt;

IF COL_LENGTH('dbo.SplitSettlements', 'DeclineReason') IS NOT NULL
    ALTER TABLE dbo.SplitSettlements DROP COLUMN DeclineReason;

IF COL_LENGTH('dbo.SplitSettlements', 'CancelledAt') IS NOT NULL
    ALTER TABLE dbo.SplitSettlements DROP COLUMN CancelledAt;

IF COL_LENGTH('dbo.SplitSettlements', 'CancelledBy') IS NOT NULL
    ALTER TABLE dbo.SplitSettlements DROP COLUMN CancelledBy;
");
        }
    }
}
