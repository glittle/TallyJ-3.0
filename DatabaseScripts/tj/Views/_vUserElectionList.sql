create 
/*
  _vUserElectionList

*/
View tj._vUserElectionList
as
select u.UserName, u.LastActivityDate
  , e.Name [ElectionName]
  , e.DateOfElection
  , e.ElectionType + ':' + e.ElectionMode [Info]
  , e.NumberToElect
  , e.NumberExtra
  , e.TallyStatus
  , (select COUNT(*) from tj.Person p where p.ElectionGuid = e.ElectionGuid) [People]
from Users u
  join tj.JoinElectionUser j on j.UserId = u.UserId
  left join tj.Election e on e.ElectionGuid = j.ElectionGuid
GO
GRANT SELECT
    ON OBJECT::[tj].[_vUserElectionList] TO [TallyJSite]
    AS [dbo];

