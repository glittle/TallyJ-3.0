create
/*

Name: UpdateVoteStatus

*/
PROCEDURE tj.UpdateVoteStatus
    @VoteRowId int
  , @StatusCode varchar(10)
AS
  update tj.Vote
    set StatusCode = @StatusCode
	where _RowId = @VoteRowId
GO
GRANT EXECUTE
    ON OBJECT::[tj].[UpdateVoteStatus] TO [TallyJSite]
    AS [dbo];

