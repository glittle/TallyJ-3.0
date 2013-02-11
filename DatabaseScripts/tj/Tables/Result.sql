CREATE TABLE [tj].[Result] (
    [_RowId]           INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]     UNIQUEIDENTIFIER NOT NULL,
    [PersonGuid]       UNIQUEIDENTIFIER NOT NULL,
    [VoteCount]        INT              NULL,
    [Rank]             INT              NOT NULL,
    [Section]          CHAR (1)         NOT NULL,
    [CloseToPrev]      BIT              NULL,
    [CloseToNext]      BIT              NULL,
    [IsTied]           BIT              NULL,
    [TieBreakGroup]    INT              NULL,
    [TieBreakRequired] BIT              NULL,
    [TieBreakCount]    INT              NULL,
    [IsTieResolved]    BIT              NULL,
    [RankInExtra]      INT              NULL,
    [ForceShowInOther] BIT              NULL,
    CONSTRAINT [PK_Result] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Result_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]),
    CONSTRAINT [FK_Result_Person] FOREIGN KEY ([PersonGuid]) REFERENCES [tj].[Person] ([PersonGuid]) ON DELETE CASCADE
);




GO
GRANT UPDATE
    ON OBJECT::[tj].[Result] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Result] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Result] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Result] TO [TallyJSite]
    AS [dbo];

