IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vResultInfo')
  BEGIN
    DROP  View tj.vResultInfo
  END
GO

create 
/*
  vResultInfo

*/
View [tj].[vResultInfo]
as
  select 
      r.* 
	, p._Fullname [PersonName]
  from tj.Result r
     join tj.Person p on p.PersonGuid = r.PersonGuid

GO

grant select, update ON tj.vResultInfo TO TallyJSite

GO
select top 100 * from tj.vResultInfo