/*
================================================================================
Spendwise – Split Expenses (Splitwise-Style)
Phase: Database Schema
Script: SplitExpenses_Schema.sql
Description:
    Independent normalized database schema for the Split Expenses module.
    Contains tables, primary keys, foreign keys (referencing only Users and
    internal Split tables), check constraints, unique constraints, and indexes.

    Zero dependency on Cashbook tables (CashBooks, CashTransactions,
    CashbookMembers, etc.) or Business tables.
================================================================================
*/

USE [ETSDB];
GO

-- 1. SplitGroups: Stores expense-sharing group information
IF OBJECT_ID(N'dbo.SplitGroups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitGroups
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitGroups_Id DEFAULT (NEWID()),
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        Icon NVARCHAR(100) NULL,
        OwnerId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitGroups_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitGroups_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsActive BIT NOT NULL CONSTRAINT DF_SplitGroups_IsActive DEFAULT (1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_SplitGroups_IsDeleted DEFAULT (0),
        CONSTRAINT PK_SplitGroups PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitGroups_Users_OwnerId FOREIGN KEY (OwnerId) REFERENCES dbo.Users (Id)
    );
END;
GO

-- 2. SplitGroupMembers: Stores group membership information
IF OBJECT_ID(N'dbo.SplitGroupMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitGroupMembers
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitGroupMembers_Id DEFAULT (NEWID()),
        GroupId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Role NVARCHAR(50) NOT NULL CONSTRAINT DF_SplitGroupMembers_Role DEFAULT ('Member'),
        JoinedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitGroupMembers_JoinedAt DEFAULT (SYSUTCDATETIME()),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitGroupMembers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitGroupMembers_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsActive BIT NOT NULL CONSTRAINT DF_SplitGroupMembers_IsActive DEFAULT (1),
        CONSTRAINT PK_SplitGroupMembers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitGroupMembers_SplitGroups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.SplitGroups (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SplitGroupMembers_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_SplitGroupMembers_Role CHECK (Role IN ('Owner', 'Member'))
    );
END;
GO

-- 3. SplitGroupInvitations: Stores invitations to join a split group
IF OBJECT_ID(N'dbo.SplitGroupInvitations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitGroupInvitations
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitGroupInvitations_Id DEFAULT (NEWID()),
        GroupId UNIQUEIDENTIFIER NOT NULL,
        InvitedUserId UNIQUEIDENTIFIER NULL,
        InvitedEmail NVARCHAR(256) NOT NULL,
        InvitedBy UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SplitGroupInvitations_Status DEFAULT ('Pending'),
        InvitationToken NVARCHAR(200) NULL,
        ExpiresAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitGroupInvitations_CreatedAt DEFAULT (SYSUTCDATETIME()),
        RespondedAt DATETIME2 NULL,
        CONSTRAINT PK_SplitGroupInvitations PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitGroupInvitations_SplitGroups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.SplitGroups (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SplitGroupInvitations_Users_InvitedUserId FOREIGN KEY (InvitedUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_SplitGroupInvitations_Users_InvitedBy FOREIGN KEY (InvitedBy) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_SplitGroupInvitations_Status CHECK (Status IN ('Pending', 'Accepted', 'Rejected', 'Expired', 'Revoked'))
    );
END;
GO

-- 4. SplitExpenses: Stores expenses incurred within a split group
IF OBJECT_ID(N'dbo.SplitExpenses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitExpenses
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitExpenses_Id DEFAULT (NEWID()),
        GroupId UNIQUEIDENTIFIER NOT NULL,
        PayerId UNIQUEIDENTIFIER NOT NULL,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Note NVARCHAR(1000) NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Category NVARCHAR(100) NULL,
        ExpenseDate DATETIME2 NOT NULL,
        ReceiptUrl NVARCHAR(1000) NULL,
        SplitType NVARCHAR(50) NOT NULL CONSTRAINT DF_SplitExpenses_SplitType DEFAULT ('Equal'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitExpenses_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitExpenses_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsDeleted BIT NOT NULL CONSTRAINT DF_SplitExpenses_IsDeleted DEFAULT (0),
        CONSTRAINT PK_SplitExpenses PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitExpenses_SplitGroups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.SplitGroups (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SplitExpenses_Users_PayerId FOREIGN KEY (PayerId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_SplitExpenses_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_SplitExpenses_Amount CHECK (Amount > 0),
        CONSTRAINT CK_SplitExpenses_SplitType CHECK (SplitType IN ('Equal'))
    );
END;
GO

-- 5. SplitExpenseShares: Stores each member's portion of an expense
IF OBJECT_ID(N'dbo.SplitExpenseShares', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitExpenseShares
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitExpenseShares_Id DEFAULT (NEWID()),
        ExpenseId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        ShareAmount DECIMAL(18, 2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitExpenseShares_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitExpenseShares_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SplitExpenseShares PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitExpenseShares_SplitExpenses_ExpenseId FOREIGN KEY (ExpenseId) REFERENCES dbo.SplitExpenses (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SplitExpenseShares_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_SplitExpenseShares_ShareAmount CHECK (ShareAmount >= 0),
        CONSTRAINT UQ_SplitExpenseShares_Expense_User UNIQUE (ExpenseId, UserId)
    );
END;
GO

-- 6. SplitSettlements: Stores debt settlement payments between two members
IF OBJECT_ID(N'dbo.SplitSettlements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitSettlements
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitSettlements_Id DEFAULT (NEWID()),
        GroupId UNIQUEIDENTIFIER NOT NULL,
        FromUserId UNIQUEIDENTIFIER NOT NULL,
        ToUserId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        SettlementDate DATETIME2 NOT NULL,
        Note NVARCHAR(500) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SplitSettlements_Status DEFAULT ('PendingReceiver'),
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        ReceiverActionBy UNIQUEIDENTIFIER NULL,
        ReceiverActionAt DATETIME2 NULL,
        OwnerActionBy UNIQUEIDENTIFIER NULL,
        OwnerActionAt DATETIME2 NULL,
        DeclineReason NVARCHAR(500) NULL,
        CancelledAt DATETIME2 NULL,
        CancelledBy UNIQUEIDENTIFIER NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitSettlements_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitSettlements_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsDeleted BIT NOT NULL CONSTRAINT DF_SplitSettlements_IsDeleted DEFAULT (0),
        CONSTRAINT PK_SplitSettlements PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitSettlements_SplitGroups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.SplitGroups (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SplitSettlements_Users_FromUserId FOREIGN KEY (FromUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_SplitSettlements_Users_ToUserId FOREIGN KEY (ToUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_SplitSettlements_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_SplitSettlements_Users_ReceiverActionBy FOREIGN KEY (ReceiverActionBy) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_SplitSettlements_Users_OwnerActionBy FOREIGN KEY (OwnerActionBy) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_SplitSettlements_Users_CancelledBy FOREIGN KEY (CancelledBy) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_SplitSettlements_Amount CHECK (Amount > 0),
        CONSTRAINT CK_SplitSettlements_DifferentUsers CHECK (FromUserId <> ToUserId)
    );
END;
GO

-- 7. SplitGroupActivities: Chronological immutable activity audit trail
IF OBJECT_ID(N'dbo.SplitGroupActivities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitGroupActivities
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitGroupActivities_Id DEFAULT (NEWID()),
        GroupId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        ActivityType NVARCHAR(100) NOT NULL,
        ReferenceId UNIQUEIDENTIFIER NULL,
        Description NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitGroupActivities_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SplitGroupActivities PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitGroupActivities_SplitGroups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.SplitGroups (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SplitGroupActivities_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
    );
END;
GO

-- ============================================================================
-- INDEXES FOR PERFORMANCE AND DATA INTEGRITY
-- ============================================================================

-- SplitGroups Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroups') AND name = N'IX_SplitGroups_OwnerId')
    CREATE NONCLUSTERED INDEX IX_SplitGroups_OwnerId ON dbo.SplitGroups (OwnerId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroups') AND name = N'IX_SplitGroups_IsActive_IsDeleted')
    CREATE NONCLUSTERED INDEX IX_SplitGroups_IsActive_IsDeleted ON dbo.SplitGroups (IsActive, IsDeleted) INCLUDE (Name, OwnerId);

-- SplitGroupMembers Indexes & Unique Active Filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupMembers') AND name = N'UX_SplitGroupMembers_GroupId_UserId')
    CREATE UNIQUE NONCLUSTERED INDEX UX_SplitGroupMembers_GroupId_UserId ON dbo.SplitGroupMembers (GroupId, UserId) WHERE (IsActive = 1);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupMembers') AND name = N'IX_SplitGroupMembers_UserId_IsActive')
    CREATE NONCLUSTERED INDEX IX_SplitGroupMembers_UserId_IsActive ON dbo.SplitGroupMembers (UserId, IsActive) INCLUDE (GroupId, Role);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupMembers') AND name = N'IX_SplitGroupMembers_GroupId_IsActive')
    CREATE NONCLUSTERED INDEX IX_SplitGroupMembers_GroupId_IsActive ON dbo.SplitGroupMembers (GroupId, IsActive) INCLUDE (UserId, Role);

-- SplitGroupInvitations Indexes & Unique Pending Filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'UX_SplitGroupInvitations_GroupId_InvitedEmail')
    CREATE UNIQUE NONCLUSTERED INDEX UX_SplitGroupInvitations_GroupId_InvitedEmail ON dbo.SplitGroupInvitations (GroupId, InvitedEmail) WHERE (Status = 'Pending');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'IX_SplitGroupInvitations_InvitedEmail')
    CREATE NONCLUSTERED INDEX IX_SplitGroupInvitations_InvitedEmail ON dbo.SplitGroupInvitations (InvitedEmail);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'IX_SplitGroupInvitations_InvitedUserId')
    CREATE NONCLUSTERED INDEX IX_SplitGroupInvitations_InvitedUserId ON dbo.SplitGroupInvitations (InvitedUserId) WHERE (InvitedUserId IS NOT NULL);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'IX_SplitGroupInvitations_InvitationToken')
    CREATE NONCLUSTERED INDEX IX_SplitGroupInvitations_InvitationToken ON dbo.SplitGroupInvitations (InvitationToken) WHERE (InvitationToken IS NOT NULL);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'IX_SplitGroupInvitations_GroupId_Status')
    CREATE NONCLUSTERED INDEX IX_SplitGroupInvitations_GroupId_Status ON dbo.SplitGroupInvitations (GroupId, Status);

-- SplitExpenses Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitExpenses') AND name = N'IX_SplitExpenses_GroupId_ExpenseDate')
    CREATE NONCLUSTERED INDEX IX_SplitExpenses_GroupId_ExpenseDate ON dbo.SplitExpenses (GroupId, ExpenseDate DESC) INCLUDE (Amount, PayerId, IsDeleted);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitExpenses') AND name = N'IX_SplitExpenses_PayerId')
    CREATE NONCLUSTERED INDEX IX_SplitExpenses_PayerId ON dbo.SplitExpenses (PayerId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitExpenses') AND name = N'IX_SplitExpenses_CreatedBy')
    CREATE NONCLUSTERED INDEX IX_SplitExpenses_CreatedBy ON dbo.SplitExpenses (CreatedBy);

-- SplitExpenseShares Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitExpenseShares') AND name = N'IX_SplitExpenseShares_ExpenseId')
    CREATE NONCLUSTERED INDEX IX_SplitExpenseShares_ExpenseId ON dbo.SplitExpenseShares (ExpenseId) INCLUDE (UserId, ShareAmount);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitExpenseShares') AND name = N'IX_SplitExpenseShares_UserId')
    CREATE NONCLUSTERED INDEX IX_SplitExpenseShares_UserId ON dbo.SplitExpenseShares (UserId) INCLUDE (ExpenseId, ShareAmount);

-- SplitSettlements Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitSettlements') AND name = N'IX_SplitSettlements_GroupId_Status_IsDeleted')
    CREATE NONCLUSTERED INDEX IX_SplitSettlements_GroupId_Status_IsDeleted ON dbo.SplitSettlements (GroupId, Status, IsDeleted) INCLUDE (FromUserId, ToUserId, Amount, SettlementDate);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitSettlements') AND name = N'IX_SplitSettlements_GroupId_SettlementDate')
    CREATE NONCLUSTERED INDEX IX_SplitSettlements_GroupId_SettlementDate ON dbo.SplitSettlements (GroupId, SettlementDate DESC) INCLUDE (FromUserId, ToUserId, Amount, IsDeleted);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitSettlements') AND name = N'IX_SplitSettlements_FromUserId')
    CREATE NONCLUSTERED INDEX IX_SplitSettlements_FromUserId ON dbo.SplitSettlements (FromUserId, Status, IsDeleted);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitSettlements') AND name = N'IX_SplitSettlements_ToUserId')
    CREATE NONCLUSTERED INDEX IX_SplitSettlements_ToUserId ON dbo.SplitSettlements (ToUserId, Status, IsDeleted);

-- SplitGroupActivities Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupActivities') AND name = N'IX_SplitGroupActivities_GroupId_CreatedAt')
    CREATE NONCLUSTERED INDEX IX_SplitGroupActivities_GroupId_CreatedAt ON dbo.SplitGroupActivities (GroupId, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupActivities') AND name = N'IX_SplitGroupActivities_UserId')
    CREATE NONCLUSTERED INDEX IX_SplitGroupActivities_UserId ON dbo.SplitGroupActivities (UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitGroupActivities') AND name = N'IX_SplitGroupActivities_ReferenceId')
    CREATE NONCLUSTERED INDEX IX_SplitGroupActivities_ReferenceId ON dbo.SplitGroupActivities (ReferenceId) WHERE (ReferenceId IS NOT NULL);
GO
