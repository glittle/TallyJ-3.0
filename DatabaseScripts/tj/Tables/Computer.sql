CREATE TABLE [tj].[Computer] (
    [_RowId]               INT              IDENTITY (1, 1) NOT NULL,
    [LastContact]          DATETIME2 (1)    NULL,
    [ElectionGuid]         UNIQUEIDENTIFIER NULL,
    [LocationGuid]         UNIQUEIDENTIFIER NULL,
    [ComputerCode]         VARCHAR (2)      NULL,
    [ComputerInternalCode] INT              NULL,
    [LastBallotNum]        INT              CONSTRAINT [DF_Computer_LastNum] DEFAULT ((0)) NULL,
    [Teller1]              UNIQUEIDENTIFIER NULL,
    [Teller2]              UNIQUEIDENTIFIER NULL,
    [ShadowElectionGuid]   UNIQUEIDENTIFIER NULL,
    [BrowserInfo]          VARCHAR (MAX)    NULL,
    CONSTRAINT [PK_Computer] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Computer_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]),
    CONSTRAINT [FK_Computer_Location1] FOREIGN KEY ([LocationGuid]) REFERENCES [tj].[Location] ([LocationGuid])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Computer_1]
    ON [tj].[Computer]([ElectionGuid] ASC, [ShadowElectionGuid] ASC, [ComputerCode] ASC);


GO
GRANT UPDATE
    ON OBJECT::[tj].[Computer] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Computer] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Computer] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Computer] TO [TallyJSite]
    AS [dbo];


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'Supplied by remote computer', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'Computer', @level2type = N'COLUMN', @level2name = N'ComputerInternalCode';

