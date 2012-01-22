if exists (select * from sysobjects where OBJECTPROPERTY(id, N'IsTrigger') = 1 and id = object_id(N'tj.Ballot_ClearResultSummary'))
drop trigger tj.Ballot_ClearResultSummary
GO

CREATE
/*

Purpose: Remove ResultSummary when a Ballot is touched


*/
TRIGGER tj.Ballot_ClearResultSummary ON Ballot
FOR INSERT, UPDATE, DELETE
AS

   delete from rs
   from ResultSummary rs
     join Location l on l.ElectionGuid = rs.ElectionGuid
	 join (select LocationGuid from inserted union select LocationGuid from deleted) id on id.LocationGuid = l.LocationGuid

GO

-- Testing code

