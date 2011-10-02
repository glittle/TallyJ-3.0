SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Glen Little
-- Create date: 
-- Description:	
-- =============================================
-- CREATE TRIGGER tj.PersonInfo 
ALTER TRIGGER tj.PersonInfo 
   ON  tj.Person 
   AFTER INSERT,UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

    -- Should/could be done as a persisted calculated column
    if 
       update(LastName)
       or update(FirstName)
       or update(OtherLastNames)
       or update(OtherNames)
       or update(OtherInfo)
	update p
	   set CombinedInfo =
	     rtrim(
	       lower(
			 coalesce(LastName + ' ', '')
			 + coalesce(FirstName + ' ', '')
			 + coalesce(OtherLastNames + ' ', '')
			 + coalesce(OtherNames + ' ', '')
			 + coalesce(OtherInfo + ' ', '')
			 )
		+ coalesce(soundex(lastName) + ' ', '')
		+ coalesce(soundex(firstName) + ' ', '')
		+ coalesce(soundex(OtherlastNames) + ' ', '')
		+ coalesce(soundex(OtherNames) + ' ', '')
		+ coalesce(soundex(OtherInfo) + ' ', '')

		+ coalesce(BahaiId, '')
		)
	  from tj.Person p 
	  where p._RowId in (select _RowId from inserted)
	    
	    			 
END
GO
