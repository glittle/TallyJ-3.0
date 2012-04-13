if exists (select * from sysobjects where OBJECTPROPERTY(id, 'IsProcedure') = 1 and id = object_id('tj.UpdateVoteStatus'))
drop procedure tj.UpdateVoteStatus
GO

CREATE
/*

Name: UpdateVoteStatus

*/
PROCEDURE [tj].[UpdateVoteStatus]
    @VoteRowId int
  , @StatusCode varchar(10)
AS
  update tj.Vote
    set StatusCode = @StatusCode
	where _RowId = @VoteRowId

GO

GRANT  EXECUTE  ON [tj].[UpdateVoteStatus]  TO [TallyJSite]
GO

-- Testing code
-- / *


-- * /
