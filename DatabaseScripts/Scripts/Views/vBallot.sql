IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vBallot')
  BEGIN
    DROP  View tj.vBallot
  END
GO

create 
/*
  vBallot

*/
View [tj].[vBallot]
as
  select b.*
		 , l.ElectionGuid
  from tj.Ballot b
    join tj.Location l on l.LocationGuid = b.BallotGuid

GO

grant select, update ON tj.vBallot TO TallyJSite

GO
select top 100 * from tj.vBallot