IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vVoteInfo')
  BEGIN
    DROP  View tj.vVoteInfo
  END
GO

create 
/*
  vVoteInfo

*/
View [tj].[vVoteInfo]
as
  select 
       v._RowId
	 , v.BallotGuid
     , v.PersonGuid -- may be null
	 , cast(v.PersonRowVersion as bigint) [PersonRowVersionInVote]
	 , v.StatusCode [VoteStatusCode]
	 , v.InvalidReasonGuid
	 , v.SingleNameElectionCount
	 , p.CanReceiveVotes
	 , p.IneligibleReasonGuid [PersonIneligibleReasonGuid]
	 , cast(p._RowVersion as bigint) [PersonRowVersion]
	 , l.ElectionGuid [ElectionGuid]
	 , l.TallyStatus [LocationTallyStatus]
	 , b.StatusCode [BallotStatusCode]
	 , b._BallotCode
	 , r._RowId [ResultRowId]
	 , r.ElectionGuid [ResultElectionGuid]
  from tj.Vote v
	join tj.Ballot b on b.BallotGuid = v.BallotGuid
	join tj.Location l on l.LocationGuid = b.LocationGuid
    left join tj.Person p on p.PersonGuid = v.PersonGuid
	left join tj.Result r on r.PersonGuid = p.PersonGuid

GO

grant select, update ON tj.vVoteInfo TO TallyJSite

GO
select top 100 * from tj.vVoteInfo