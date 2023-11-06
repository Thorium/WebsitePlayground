CREATE TABLE [Companyweb].[company] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [Name]       NVARCHAR (255) NOT NULL,
    [CEO]        NVARCHAR (255) NOT NULL,
    [Founded]    DATETIME       NOT NULL,
    [Founder]    NVARCHAR (255) NULL,
    [WebSite]    NVARCHAR (255) NULL,
    [LogoUrl]    NVARCHAR (255) NULL,
    [LastUpdate] DATETIME       NOT NULL,
    CONSTRAINT [PK_company] PRIMARY KEY CLUSTERED ([Id] ASC)
);

