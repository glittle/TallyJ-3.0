CREATE
/*

Name: EraseElectionContents

Purpose: Erase all tally content from an election (not people)

Note: No security checks are done


*/
PROCEDURE tj.EraseElectionContents
    @ElectionGuidToClear uniqueidentifier
  , @EraseTallyContent bit
  , @ByLoginId varchar(15)
AS
   -- safety check
   if coalesce(@EraseTallyContent, 0) = 0
     begin
	   return
	 end

   delete from tj.Result
   where ElectionGuid = @ElectionGuidToClear

   delete from tj.ResultTie
   where ElectionGuid = @ElectionGuidToClear

   delete from tj.ResultSummary	
   where ElectionGuid = @ElectionGuidToClear

   delete from v
   from tj.Vote v
     join tj.Ballot b on b.BallotGuid = v.BallotGuid
	 join tj.Location l on l.LocationGuid = b.LocationGuid
   where l.ElectionGuid = @ElectionGuidToClear

   delete from b
   from tj.Ballot b
	 join tj.Location l on l.LocationGuid = b.LocationGuid
   where l.ElectionGuid = @ElectionGuidToClear
GO
GRANT EXECUTE
    ON OBJECT::[tj].[EraseElectionContents] TO [TallyJSite]
    AS [dbo];

