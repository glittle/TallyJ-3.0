using TallyJ.Code.Enumerations;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class VoteAnalyzer
  {
    /// <Summary>Is this Vote valid?</Summary>
    public static bool VoteIsValid(vVoteInfo voteInfo)
    {
      return !voteInfo.VoteInvalidReasonGuid.HasValue
             && !voteInfo.PersonIneligibleReasonGuid.HasValue
             && voteInfo.BallotStatusCode == BallotStatusEnum.Ok
             && voteInfo.VoteStatusCode == VoteHelper.VoteStatusCode.Ok
             && voteInfo.PersonCombinedInfo == voteInfo.PersonCombinedInfoInVote;
    }

    /// <Summary>Does this vote need to be reviewed? (Underlying person info was changed)</Summary>
    public static bool VoteNeedReview(vVoteInfo voteInfo)
    {
      return voteInfo.PersonCombinedInfo != voteInfo.PersonCombinedInfoInVote
             || voteInfo.BallotStatusCode == BallotStatusEnum.Review;
    }

    /// <Summary>Is this Vote not valid?</Summary>
    public static bool IsNotValid(vVoteInfo voteInfo)
    {
      return !VoteIsValid(voteInfo);
    }
  }
}