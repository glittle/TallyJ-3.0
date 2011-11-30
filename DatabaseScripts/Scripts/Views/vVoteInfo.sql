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
       v._RowId [VoteRowId]
	 , v.BallotGuid
	 , v.StatusCode [VoteStatusCode]
	 , v.InvalidReasonGuid
	 , v.SingleNameElectionCount
	 , v.PositionOnBallot
	 , cast(v.PersonRowVersion as bigint) [PersonRowVersionInVote]
	 , cast(p._RowVersion as bigint) [PersonRowVersion]
     , v.PersonGuid
	 , p._RowId [PersonRowId]
	 , p._FullName [PersonFullName]
	 , p.CanReceiveVotes
	 , p.IneligibleReasonGuid [PersonIneligibleReasonGuid]
	 , l.ElectionGuid [ElectionGuid]
	 , l.TallyStatus [LocationTallyStatus]
	 , b.StatusCode [BallotStatusCode]
	 , b._BallotCode
	 , r._RowId [ResultRowId]
	 , r.ElectionGuid [ResultElectionGuid]
  from tj.Vote v
	join tj.Ballot b on b.BallotGuid = v.BallotGuid
	join tj.Location l on l.LocationGuid = b.LocationGuid
    join tj.Person p on p.PersonGuid = v.PersonGuid
	left join tj.Result r on r.PersonGuid = p.PersonGuid

GO

grant select, update ON tj.vVoteInfo TO TallyJSite

GO
select top 100 * from tj.vVoteInfo