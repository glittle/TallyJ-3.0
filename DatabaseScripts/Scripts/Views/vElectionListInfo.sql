IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vElectionListInfo')
  BEGIN
    DROP  View tj.vElectionListInfo
  END
GO

create 
/*
  vElectionListInfo

*/
View [tj].[vElectionListInfo]
as
  select _RowId
	  , ElectionGuid
      , Name
	  , ListForPublic
	  , ListedForPublicAsOf
	  , ElectionPasscode
  from tj.Election
  where ListForPublic = 1
    and ElectionPasscode is not null
	and ListedForPublicAsOf is not null -- check using web server clock

GO

grant select, update ON tj.vElectionListInfo TO TallyJSite

GO
select top 100 * from tj.vElectionListInfo