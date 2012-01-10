
IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vLocationInfo')
  BEGIN
    DROP  View tj.vLocationInfo
  END
GO

create 
/*
  vLocationInfo

  Line per location/computer.

  Adds total votes, computer codes, etc. 
*/
View [tj].[vLocationInfo]
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
  )
  select l.*
	   , bv.ComputerCode ComputerCode
	   -- , bv.BallotId
	   , e.IsSingleNameElection
	   , case when e.IsSingleNameElection = 1 then bv.SingleNameBallots
	          else bv.Ballots end [Ballots]
	   , c.LastContact
	   , t.Name [TellerName]
	   , isnull(ROW_NUMBER() over (order by sortorder, bv.computercode, l._RowId),0) SortOrder2
  from tj.Location l
    join tj.Election e on e.ElectionGuid = l.ElectionGuid
	left join bv on bv.LocationGuid = l.LocationGuid
	left join tj.Computer c on c.LocationGuid = l.LocationGuid and c.ComputerCode = bv.ComputerCode
	left join tj.Teller t on t.TellerGuid = c.Teller1
  
GO


grant select, update ON tj.vLocationInfo TO TallyJSite
GO

select top 100 * from tj.vLocationInfo

