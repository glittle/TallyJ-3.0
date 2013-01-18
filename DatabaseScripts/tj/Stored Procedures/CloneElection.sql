create
/*

Name: CloneElection

Purpose: General purpose routine to return the Guid for a given RowId

Note: No security checks are done


*/
PROCEDURE tj.CloneElection
    @SourceElection uniqueidentifier
  , @ByLoginId varchar(15)
AS
   declare @success bit = 0
   declare @newElection uniqueidentifier

   -- to copy
   if not exists (select * from tj.Election where ElectionGuid = @SourceElection)
     begin
	    select @success Success,
		         @newElection NewElectionGuid,
				 'Not Found' Message
		return
	 end

	--insert into tj.Election ()




	select @success Success,
		        @newElection NewElectionGuid,
				'Not Implemented' Message
	return
GO
GRANT EXECUTE
    ON OBJECT::[tj].[CloneElection] TO [TallyJSite]
    AS [dbo];

