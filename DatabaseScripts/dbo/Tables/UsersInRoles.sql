CREATE TABLE [dbo].[UsersInRoles] (
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [UsersInRoleRole] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]),
    CONSTRAINT [UsersInRoleUser] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);


GO
GRANT UPDATE
    ON OBJECT::[dbo].[UsersInRoles] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[dbo].[UsersInRoles] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[dbo].[UsersInRoles] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[dbo].[UsersInRoles] TO [TallyJSite]
    AS [dbo];

