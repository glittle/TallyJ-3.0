IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vLocationInfo')
  BEGIN
    DROP  View tj.vLocationInfo
  END
GO

create 
/*
  vLocationInfo

*/
View [tj].[vLocationInfo]
as
  select l.*
       , (select COUNT(*) from tj.Computer c where c.LocationGuid = l.LocationGuid) [ActiveComputers]
	   , case when e.IsSingleNameElection = 1 
			       then (select sum(v.singlenameElectionCount) 
						   from tj.Ballot b 
						     join tj.Vote v on v.BallotGuid = b.BallotGuid
						  where b.LocationGuid = l.LocationGuid)
				   else 
						(select COUNT(*) from tj.Ballot b where b.LocationGuid = l.LocationGuid)
				    end
				    [Ballots]

  from tj.Location l
    join tj.Election e on e.ElectionGuid = l.ElectionGuid

GO


grant select, update ON tj.vLocationInfo TO TallyJSite
GO

select top 100 * from tj.vLocationInfo

