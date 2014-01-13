CREATE TABLE [tj].[Teller] (
    [_RowId]            INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]      UNIQUEIDENTIFIER NOT NULL,
    [Name]              NVARCHAR (50)    NOT NULL,
    [UsingComputerCode] VARCHAR (2)      NULL,
    [IsHeadTeller]      BIT              NULL,
    [_RowVersion]       ROWVERSION       NOT NULL,
    CONSTRAINT [PK_Teller] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Teller_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Teller]
    ON [tj].[Teller]([ElectionGuid] ASC, [Name] ASC);


GO
GRANT UPDATE
    ON OBJECT::[tj].[Teller] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Teller] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Teller] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Teller] TO [TallyJSite]
    AS [dbo];

