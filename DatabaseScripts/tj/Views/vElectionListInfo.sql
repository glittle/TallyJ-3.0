CREATE 
/*
  vElectionListInfo

*/
View tj.vElectionListInfo
as
  select _RowId
	  , ElectionGuid
      , Name
	  , ListForPublic
	  , ListedForPublicAsOf
	  , ElectionPasscode
	  , DateOfElection
	  , ElectionType
	  , ElectionMode
	  , ShowAsTest
	  , (select count(*) from tj.Person p where p.ElectionGuid = e.ElectionGuid)
	    NumVoters
  from tj.Election e
 -- where ListForPublic = 1
 --   and ElectionPasscode is not null
--and ListedForPublicAsOf is not null -- check using web server clock
GO
GRANT UPDATE
    ON OBJECT::[tj].[vElectionListInfo] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[vElectionListInfo] TO [TallyJSite]
    AS [dbo];

