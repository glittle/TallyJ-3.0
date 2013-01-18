CREATE TABLE [tj].[Location] (
    [_RowId]           INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]     UNIQUEIDENTIFIER NOT NULL,
    [LocationGuid]     UNIQUEIDENTIFIER CONSTRAINT [DF_Location_LocationGuid] DEFAULT (newsequentialid()) NOT NULL,
    [Name]             NVARCHAR (50)    NOT NULL,
    [ContactInfo]      NVARCHAR (250)   NULL,
    [Long]             VARCHAR (50)     NULL,
    [Lat]              VARCHAR (50)     NULL,
    [TallyStatus]      VARCHAR (15)     NULL,
    [SortOrder]        INT              NULL,
    [BallotsCollected] INT              NULL,
    CONSTRAINT [PK_VotingLocation] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Location_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Location]
    ON [tj].[Location]([LocationGuid] ASC);


GO
GRANT UPDATE
    ON OBJECT::[tj].[Location] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Location] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Location] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Location] TO [TallyJSite]
    AS [dbo];

