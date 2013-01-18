CREATE TABLE [tj].[ResultTie] (
    [_RowId]           INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]     UNIQUEIDENTIFIER NOT NULL,
    [TieBreakGroup]    VARCHAR (1)      NOT NULL,
    [TieBreakRequired] BIT              NULL,
    [NumToElect]       INT              NOT NULL,
    [NumInTie]         INT              NOT NULL,
    [IsResolved]       BIT              NULL,
    CONSTRAINT [PK_ResultTie] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_ResultTie_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ResultTie]
    ON [tj].[ResultTie]([ElectionGuid] ASC, [TieBreakGroup] ASC);


GO
GRANT UPDATE
    ON OBJECT::[tj].[ResultTie] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[ResultTie] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[ResultTie] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[ResultTie] TO [TallyJSite]
    AS [dbo];

