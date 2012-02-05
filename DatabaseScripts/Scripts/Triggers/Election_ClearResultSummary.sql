if exists (select * from sysobjects where OBJECTPROPERTY(id, N'IsTrigger') = 1 and id = object_id(N'tj.Election_ClearResultSummary'))
drop trigger tj.Election_ClearResultSummary
GO

CREATE
/*

Purpose: Remove ResultSummary when a Ballot is touched


*/
TRIGGER tj.Election_ClearResultSummary ON Election
FOR UPDATE
AS
   if( 
        UPDATE(ElectionType) or
        UPDATE(ElectionMode) or
        UPDATE(NumberToElect) or
        UPDATE(NumberExtra)
		)
   begin
	   delete from rs
	   from ResultSummary rs
		 join inserted id on id.ElectionGuid = rs.ElectionGuid
   end
GO

-- Testing code

