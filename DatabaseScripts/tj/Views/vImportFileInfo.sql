create 
/*
  vImportFileInfo

*/
View tj.vImportFileInfo
as

-- copy/paste all column names except Contents
SELECT [_RowId]
      ,[ElectionGuid]
      ,[UploadTime]
      ,[ImportTime]
      ,[FileSize]
      ,[HasContent]
      ,[FirstDataRow]
      ,[ColumnsToRead]
      ,[OriginalFileName]
      ,[ProcessingStatus]
      ,[FileType]
      ,[Messages]
      ,[CodePage]
      --,[Contents]
  FROM ImportFile
GO
GRANT UPDATE
    ON OBJECT::[tj].[vImportFileInfo] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[vImportFileInfo] TO [TallyJSite]
    AS [dbo];

