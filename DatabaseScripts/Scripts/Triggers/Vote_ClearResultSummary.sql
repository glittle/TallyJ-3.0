if exists (select * from sysobjects where OBJECTPROPERTY(id, N'IsTrigger') = 1 and id = object_id(N'tj.Vote_ClearResultSummary'))
drop trigger tj.Vote_ClearResultSummary
GO

CREATE
/*

Purpose: Remove ResultSummary when a Vote is touched


*/
TRIGGER tj.Vote_ClearResultSummary ON Vote
FOR INSERT, UPDATE, DELETE
AS

   delete from rs
   from ResultSummary rs
     join Location l on l.ElectionGuid = rs.ElectionGuid
	 join Ballot b on b.LocationGuid = l.LocationGuid
	 join (select BallotGuid from inserted union select BallotGuid from deleted) id on id.BallotGuid = b.BallotGuid

GO

-- Testing code

