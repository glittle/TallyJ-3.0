IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vImportFileInfo')
  BEGIN
    DROP  View tj.vImportFileInfo
  END
GO

create 
/*
  vImportFileInfo

*/
View [tj].[vImportFileInfo]
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

grant select, update ON tj.vImportFileInfo TO TallyJSite

GO
select top 100 * from tj.vImportFileInfo