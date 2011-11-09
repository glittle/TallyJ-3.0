IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'tj.vElection')
  BEGIN
    DROP  View tj.vElection
  END
GO

create 
/*
  vElection

*/
View [tj].[vElection]
as
  select e.*
         , ISNULL(e._RowId,0) _RowId2  -- non nullable result for EF4 to use
		 , j.Role
		 , j.UserId
  from tj.Election e
    join tj.JoinElectionUser j on j.ElectionGuid = e.ElectionGuid


GO

grant select, update ON tj.vElection TO TallyJSite

GO
select top 100 * from tj.vElection