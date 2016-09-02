--DROP DATABASE companyweb
CREATE DATABASE companyweb

USE companyweb

CREATE TABLE [company](
	[Id] int IDENTITY(1,1) NOT NULL,
	[Name] nvarchar(255) NOT NULL,
	[CEO] nvarchar(255) NOT NULL,
	[Founded] datetime NOT NULL,
	[Founder] nvarchar(255) NULL,
	[WebSite] nvarchar(255) NULL,
    [LogoUrl] nvarchar(255) NULL,
    [LastUpdate] datetime NOT NULL
 CONSTRAINT [PK_company] PRIMARY KEY CLUSTERED ([Id] ASC)
 WITH (PAD_INDEX = OFF, 
       STATISTICS_NORECOMPUTE = OFF, 
       IGNORE_DUP_KEY = OFF, 
       ALLOW_ROW_LOCKS = ON, 
       ALLOW_PAGE_LOCKS = ON)
)

