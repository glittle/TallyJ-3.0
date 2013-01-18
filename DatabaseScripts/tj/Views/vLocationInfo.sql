CREATE 
/*
  vLocationInfo

  Line per location/computer.

  Adds total votes, computer codes, etc. 
*/
View tj.vLocationInfo
as
  with v as (
    select BallotGuid
         , SUM(SingleNameElectionCount) SingleNameBallots
    from tj.Vote
    group by BallotGuid
  ),
  bv as (
  select b.LocationGuid
       , b.ComputerCode
       , COUNT(b.BallotGuid) Ballots
       , sum(v.SingleNameBallots) SingleNameBallots
	from tj.Ballot b
	  left join v on v.BallotGuid = b.BallotGuid
	group by b.LocationGuid, b.ComputerCode
  ),
  total as (
  select b.LocationGuid
       , COUNT(b.BallotGuid) Ballots
       , sum(v.SingleNameBallots) SingleNameBallots
	from tj.Ballot b
	  left join v on v.BallotGuid = b.BallotGuid
	group by b.LocationGuid
  )
  select l.*
	   , bv.ComputerCode ComputerCode
	   -- , bv.BallotId
	   , cast(case when e.NumberToElect = 1 then 1 else 0 end as bit) IsSingleNameElection
	   , case when e.NumberToElect = 1 then bv.SingleNameBallots
	          else bv.Ballots end [BallotsAtComputer]
	   , case when e.NumberToElect = 1 then total.SingleNameBallots
	          else total.Ballots end [BallotsAtLocation]
	   , c.LastContact
	   , coalesce(t1.Name + coalesce(', ' + t2.Name, '') ,'') [TellerName]
	   , isnull(ROW_NUMBER() over (order by sortorder, bv.computercode, l._RowId),0) SortOrder2
  from tj.Location l
    join tj.Election e on e.ElectionGuid = l.ElectionGuid
	left join bv on bv.LocationGuid = l.LocationGuid
	left join total on total.LocationGuid = l.LocationGuid
	left join tj.Computer c on c.LocationGuid = l.LocationGuid and c.ComputerCode = bv.ComputerCode
	left join tj.Teller t1 on t1.TellerGuid = c.Teller1
	left join tj.Teller t2 on t2.TellerGuid = c.Teller2
GO
GRANT UPDATE
    ON OBJECT::[tj].[vLocationInfo] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[vLocationInfo] TO [TallyJSite]
    AS [dbo];

