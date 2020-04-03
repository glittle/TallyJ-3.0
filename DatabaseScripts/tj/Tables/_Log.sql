CREATE TABLE [tj].[_Log] (
    [_RowId]         INT              IDENTITY (1, 1) NOT NULL,
    [AsOf]           DATETIME2 (2)    CONSTRAINT [DF__Log_AsOf] DEFAULT (getdate()) NOT NULL,
    [ElectionGuid]   UNIQUEIDENTIFIER NULL,
    [LocationGuid]   UNIQUEIDENTIFIER NULL,
    [VoterId]        NVARCHAR (250)   NULL,
    [ComputerCode]   VARCHAR (2)      NULL,
    [Details]        VARCHAR (MAX)    NULL,
    [HostAndVersion] VARCHAR (MAX)    NULL,
    CONSTRAINT [PK__Log] PRIMARY KEY CLUSTERED ([_RowId] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX__Log]
    ON [tj].[_Log]([AsOf] ASC)
    INCLUDE([VoterId]);




GO
GRANT SELECT
    ON OBJECT::[tj].[_Log] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[_Log] TO [TallyJSite]
    AS [dbo];

