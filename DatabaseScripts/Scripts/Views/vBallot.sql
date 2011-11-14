IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'tj.vBallot')
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
         , ISNULL(b._RowId,0) _RowId2  -- non nullable result for EF4 to use
		 , l.ElectionGuid
  from tj.Ballot b
    join tj.Location l on l.LocationGuid = b.BallotGuid

GO

grant select, update ON tj.vBallot TO TallyJSite

GO
select top 100 * from tj.vBallot