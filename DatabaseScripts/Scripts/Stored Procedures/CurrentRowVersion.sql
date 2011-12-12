if exists (select * from sysobjects where OBJECTPROPERTY(id, 'IsProcedure') = 1 and id = object_id('tj.CurrentRowVersion'))
drop procedure tj.CurrentRowVersion
GO

CREATE
/*

Name: CurrentRowVersion

*/
PROCEDURE [tj].[CurrentRowVersion]
AS
select cast(@@DbTs as bigint)

   	return cast(@@DbTs as bigint)
GO

GRANT  EXECUTE  ON [tj].[CurrentRowVersion]  TO [TallyJSite]
GO

-- Testing code
-- / *
declare @x bigint
exec @x = tj.CurrentRowVersion
print @x



-- * /
