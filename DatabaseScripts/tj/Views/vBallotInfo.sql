CREATE 
/*
  vBallotInfo

*/
View tj.vBallotInfo
as
  with v as (
    select BallotGuid, COUNT(*) Ineligibles
	  from tj.Vote v
	where InvalidReasonGuid is not null
	group by BallotGuid
  )
  , v2 as (
    select BallotGuid, COUNT(*) Changed
	  from tj.vVoteInfo v
	where v.PersonCombinedInfoInVote is not null and v.PersonCombinedInfoInVote != v.PersonCombinedInfo
	group by BallotGuid
  )
  select b.*
		 , l.ElectionGuid
		 , l._RowId [LocationId]
		 , l.Name [LocationName]
		 , l.SortOrder [LocationSortOrder]
		 , l.TallyStatus
		 , tk.Name [TellerAtKeyboardName]
		 , ta.Name [TellerAssistingName]
		 , CAST(b._RowVersion as bigint) [RowVersionInt]
		 , case when b.StatusCode = 'Ok' then v.Ineligibles else null end [SpoiledCount]
		 , v2.Changed [VotesChanged]
  from tj.Ballot b
    join tj.Location l on l.LocationGuid = b.LocationGuid
	left join tj.Teller tk on tk.TellerGuid = b.TellerAtKeyboard
	left join tj.Teller ta on ta.TellerGuid = b.TellerAssisting
	left join v on v.BallotGuid = b.BallotGuid
	left join v2 on v2.BallotGuid = b.BallotGuid
GO
GRANT UPDATE
    ON OBJECT::[tj].[vBallotInfo] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[vBallotInfo] TO [TallyJSite]
    AS [dbo];

