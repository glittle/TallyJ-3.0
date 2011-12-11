
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
       v._RowId [VoteId]
	 , v.StatusCode [VoteStatusCode]
	 , v.SingleNameElectionCount
	 , v.PositionOnBallot
	 , v.InvalidReasonGuid [VoteInvalidReasonGuid]
	 , vr._RowId [VoteInvalidReasonId]
	 , vr.ReasonDescription [VoteInvalidReasonDesc]
	 , cast(v.PersonRowVersion as bigint) [PersonRowVersionInVote]
	 , cast(p._RowVersion as bigint) [PersonRowVersion]
	 , coalesce(p._RowVersion, cast(0 as binary(6))) [PersonRowVersionRaw]
     , v.PersonGuid
	 , p._RowId [PersonId]
	 , p._FullName [PersonFullName]
	 , p.CanReceiveVotes
	 , p.IneligibleReasonGuid [PersonIneligibleReasonGuid]
	 , pr._RowId [PersonIneligibleReasonId]
	 , pr.ReasonDescription [PersonIneligibleReasonDesc]
	 , r._RowId [ResultId]
	 , v.BallotGuid
	 , b._RowId [BallotId]
	 , b.StatusCode [BallotStatusCode]
	 , b._BallotCode
	 , l._RowId [LocationId]
	 , l.TallyStatus [LocationTallyStatus]
	 , l.ElectionGuid [ElectionGuid]
  from tj.Vote v
	join tj.Ballot b on b.BallotGuid = v.BallotGuid
	join tj.Location l on l.LocationGuid = b.LocationGuid
    left join tj.Person p on p.PersonGuid = v.PersonGuid
	left join tj.Result r on r.PersonGuid = p.PersonGuid and r.ElectionGuid = l.ElectionGuid
	left join tj.Reasons pr on pr.ReasonGuid = p.IneligibleReasonGuid
	left join tj.Reasons vr on vr.ReasonGuid = v.InvalidReasonGuid

GO

grant select, update ON tj.vVoteInfo TO TallyJSite

GO
select top 100 * from tj.vVoteInfo