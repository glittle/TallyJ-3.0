using System;
using System.Collections.Generic;
using TallyJ.Code.Enumerations;
using TallyJ.EF;
using TallyJ.Code;

namespace TallyJ.Models
{
  public class VoteAnalyzer
  {
    /// <Summary>Is this Vote valid?</Summary>
    public static bool VoteIsValid(vVoteInfo voteInfo)
    {
      return !voteInfo.VoteIneligibleReasonGuid.HasValue
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

    /// <Summary>Update statuses... return true if any were updated</Summary>
    public static bool UpdateAllStatuses(List<vVoteInfo> voteInfos)
    {
      var changeMade = false;
      voteInfos.ForEach(delegate(vVoteInfo info)
                          {
                            var oldStatus = info.VoteStatusCode;
                            var newStatus = info.VoteIneligibleReasonGuid.HasValue
                                              ? VoteHelper.VoteStatusCode.Spoiled
                                              : info.PersonCombinedInfo.HasContent() &&
                                                info.PersonCombinedInfo != info.PersonCombinedInfoInVote
                                                  ? VoteHelper.VoteStatusCode.Changed
                                                  : VoteHelper.VoteStatusCode.Ok;
                            if (newStatus != oldStatus)
                            {
                              info.VoteStatusCode = newStatus;
                              changeMade = true;
                            }
                          });
      return changeMade;
    }
  }

  public class VoteGuidWithStatus
  {
    public Guid VoteGuid { get; set; }
  }
}