Alter
/*

Name: CurrentRowVersion

*/
PROCEDURE tj.CurrentRowVersion
AS
    select cast(@@DbTs as bigint)
GO
GRANT EXECUTE
    ON OBJECT::[tj].[CurrentRowVersion] TO [TallyJSite]
    AS [dbo];

