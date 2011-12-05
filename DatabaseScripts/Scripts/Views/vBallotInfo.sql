IF EXISTS (SELECT * FROM sysobjects WHERE type = 'V' AND name = 'vBallotInfo')
  BEGIN
    DROP  View tj.vBallotInfo
  END
GO

create 
/*
  vBallotInfo

*/
View [tj].[vBallotInfo]
as
  select b.*
		 , l.ElectionGuid
		 , l._RowId [LocationId]
		 , l.Name [LocationName]
		 , l.SortOrder [LocationSortOrder]
		 , l.TallyStatus
		 , tk.Name [TellerAtKeyboardName]
		 , ta.Name [TellerAssistingName]
		 , CAST(b._RowVersion as bigint) [RowVersionInt]
  from tj.Ballot b
    join tj.Location l on l.LocationGuid = b.LocationGuid
	left join tj.Teller tk on tk.TellerGuid = b.TellerAtKeyboard
	left join tj.Teller ta on ta.TellerGuid = b.TellerAssisting

GO

grant select, update ON tj.vBallotInfo TO TallyJSite

GO
select top 100 * from tj.vBallotInfo