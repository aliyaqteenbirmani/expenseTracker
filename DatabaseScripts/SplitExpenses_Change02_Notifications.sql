-- =========================================================================================
-- Spendwise – Split Expenses: Migration 2 (Notifications & Invitation Delivery Tracking)
-- =========================================================================================

-- 1. Create SplitNotifications table
IF OBJECT_ID(N'dbo.SplitNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SplitNotifications
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SplitNotifications_Id DEFAULT (NEWID()),
        UserId UNIQUEIDENTIFIER NOT NULL,
        Type NVARCHAR(100) NOT NULL, -- 'GroupInvitationReceived', 'InvitationAccepted', 'InvitationRejected'
        GroupId UNIQUEIDENTIFIER NULL,
        ReferenceId UNIQUEIDENTIFIER NULL, -- e.g. InvitationId
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_SplitNotifications_IsRead DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SplitNotifications_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ReadAt DATETIME2 NULL,
        CONSTRAINT PK_SplitNotifications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SplitNotifications_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SplitNotifications_SplitGroups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.SplitGroups (Id) ON DELETE NO ACTION
    );
END;
GO

-- 2. Indexes for SplitNotifications
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SplitNotifications') AND name = N'IX_SplitNotifications_UserId_IsRead_CreatedAt')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SplitNotifications_UserId_IsRead_CreatedAt 
    ON dbo.SplitNotifications (UserId, IsRead, CreatedAt DESC);
END;
GO

-- 3. Extend SplitGroupInvitations with resend & token hash tracking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'LastSentAt')
BEGIN
    ALTER TABLE dbo.SplitGroupInvitations ADD LastSentAt DATETIME2 NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'ResendCount')
BEGIN
    ALTER TABLE dbo.SplitGroupInvitations ADD ResendCount INT NOT NULL CONSTRAINT DF_SplitGroupInvitations_ResendCount DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SplitGroupInvitations') AND name = N'TokenHash')
BEGIN
    ALTER TABLE dbo.SplitGroupInvitations ADD TokenHash NVARCHAR(128) NULL;
END;
GO

-- 4. Record Migration History
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261112000000_AddSplitNotificationsAndInvitationTracking')
    BEGIN
        INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES (N'20261112000000_AddSplitNotificationsAndInvitationTracking', N'9.0.0');
    END;
END;
GO
