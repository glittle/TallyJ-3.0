IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vResultInfo')
  BEGIN
    DROP  View tj.vResultInfo
  END
GO

create 
/*
  vResultInfo

*/
View [tj].[vResultInfo]
as
  select 
      r.ElectionGuid
	, r.Section
	, r.Rank
	, r.RankInExtra
	, p._FullName [PersonName]
	, r.IsTied
	, r.IsTieResolved
	, r.TieBreakGroup
	, r.TieBreakCount
	, r.VoteCount
	, ISNULL(ROW_NUMBER() over (order by r.Rank), 0) _FakeRowId
  from tj.Result r
     join tj.Person p on p.PersonGuid = r.PersonGuid

GO

grant select, update ON tj.vResultInfo TO TallyJSite

GO
select top 100 * from tj.vResultInfo