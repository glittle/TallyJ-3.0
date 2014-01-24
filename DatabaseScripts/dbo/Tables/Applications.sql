CREATE TABLE [dbo].[Applications] (
    [ApplicationName] NVARCHAR (235)   NOT NULL,
    [ApplicationId]   UNIQUEIDENTIFIER NOT NULL,
    [Description]     NVARCHAR (256)   NULL,
    PRIMARY KEY CLUSTERED ([ApplicationId] ASC)
);




GO
GRANT SELECT
    ON OBJECT::[dbo].[Applications] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[dbo].[Applications] TO [TallyJSite]
    AS [dbo];

