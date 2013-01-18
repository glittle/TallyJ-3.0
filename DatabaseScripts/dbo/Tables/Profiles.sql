CREATE TABLE [dbo].[Profiles] (
    [UserId]               UNIQUEIDENTIFIER NOT NULL,
    [PropertyNames]        NVARCHAR (4000)  NOT NULL,
    [PropertyValueStrings] NVARCHAR (4000)  NOT NULL,
    [PropertyValueBinary]  IMAGE            NOT NULL,
    [LastUpdatedDate]      DATETIME         NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [UserProfile] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);


GO
GRANT UPDATE
    ON OBJECT::[dbo].[Profiles] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[dbo].[Profiles] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[dbo].[Profiles] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[dbo].[Profiles] TO [TallyJSite]
    AS [dbo];

