create 
/*
  vResultInfo

*/
View tj.vResultInfo
as
  select 
      r.*
	, p._FullName [PersonName]
  from tj.Result r
     join tj.Person p on p.PersonGuid = r.PersonGuid
GO
GRANT UPDATE
    ON OBJECT::[tj].[vResultInfo] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[vResultInfo] TO [TallyJSite]
    AS [dbo];

