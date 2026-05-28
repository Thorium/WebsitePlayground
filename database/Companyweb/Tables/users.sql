CREATE TABLE [Companyweb].[users] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [Email]             NVARCHAR (255) NOT NULL,
    [PasswordHash]      NVARCHAR (500) NOT NULL,
    [IsActive]          BIT            NOT NULL DEFAULT 1,
    [FailedLoginAttempts] INT          NOT NULL DEFAULT 0,
    [LastFailedLogin]   DATETIME       NULL,
    [LockedUntil]       DATETIME       NULL,
    [CreatedAt]         DATETIME       NOT NULL DEFAULT GETUTCDATE(),
    [LastLoginAt]       DATETIME       NULL,
    CONSTRAINT [PK_users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_users_Email] UNIQUE NONCLUSTERED ([Email] ASC)
);
GO;
