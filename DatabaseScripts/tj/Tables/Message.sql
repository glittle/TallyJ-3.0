CREATE TABLE [tj].[Message] (
    [_RowId]       INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid] UNIQUEIDENTIFIER NOT NULL,
    [Title]        NVARCHAR (150)   NOT NULL,
    [Details]      NVARCHAR (MAX)   NULL,
    [_RowVersion]  ROWVERSION       NOT NULL,
    [AsOf]         DATETIME2 (0)    NOT NULL,
    CONSTRAINT [PK_Message] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Message_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]) ON DELETE CASCADE
);


GO
GRANT UPDATE
    ON OBJECT::[tj].[Message] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Message] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Message] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Message] TO [TallyJSite]
    AS [dbo];

