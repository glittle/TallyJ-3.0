if exists (select * from sysobjects where OBJECTPROPERTY(id, 'IsProcedure') = 1 and id = object_id('tj.CloneElection'))
drop procedure tj.CloneElection
GO

CREATE
/*

Name: CloneElection

Purpose: Copy an election

Note: No security checks are done


*/
PROCEDURE [tj].[CloneElection]
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

GRANT  EXECUTE  ON [tj].[CloneElection]  TO [TallyJSite]
GO

-- Testing code
-- / *
-- exec tj.CloneElection 'xxx', 'xxx'




-- * /
