CREATE TABLE [tj].[ImportFile] (
    [_RowId]           INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]     UNIQUEIDENTIFIER NOT NULL,
    [UploadTime]       DATETIME2 (2)    NULL,
    [ImportTime]       DATETIME2 (2)    NULL,
    [FileSize]         AS               (datalength([Contents])),
    [HasContent]       AS               (CONVERT([bit],case when [Contents] IS NULL then (0) else (1) end,(0))),
    [FirstDataRow]     INT              NULL,
    [ColumnsToRead]    NVARCHAR (MAX)   NULL,
    [OriginalFileName] NVARCHAR (50)    NULL,
    [ProcessingStatus] VARCHAR (20)     NULL,
    [FileType]         VARCHAR (10)     NULL,
    [CodePage]         INT              NULL,
    [Messages]         VARCHAR (MAX)    NULL,
    [Contents]         IMAGE            NULL,
    CONSTRAINT [PK_ImportFile] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_ImportFile_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]) ON DELETE CASCADE
);


GO
GRANT UPDATE
    ON OBJECT::[tj].[ImportFile] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[ImportFile] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[ImportFile] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[ImportFile] TO [TallyJSite]
    AS [dbo];

