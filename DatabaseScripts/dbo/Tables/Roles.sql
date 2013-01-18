CREATE TABLE [dbo].[Roles] (
    [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId]        UNIQUEIDENTIFIER NOT NULL,
    [RoleName]      NVARCHAR (256)   NOT NULL,
    [Description]   NVARCHAR (256)   NULL,
    PRIMARY KEY CLUSTERED ([RoleId] ASC),
    CONSTRAINT [RoleApplication] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId])
);


GO
GRANT UPDATE
    ON OBJECT::[dbo].[Roles] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[dbo].[Roles] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[dbo].[Roles] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[dbo].[Roles] TO [TallyJSite]
    AS [dbo];

