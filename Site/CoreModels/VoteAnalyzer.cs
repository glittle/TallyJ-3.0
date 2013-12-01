using System;
using System.Collections.Generic;
using TallyJ.Code.Enumerations;
using TallyJ.Code;
using TallyJ.EF;
using System.Linq;

namespace TallyJ.CoreModels
{
  public class VoteAnalyzer
  {
    /// <Summary>Is this Vote valid?</Summary>
    public static bool VoteIsValid(VoteInfo voteInfo)
    {
      if (!voteInfo.ValidationResult.HasValue)
      {
        voteInfo.ValidationResult = !voteInfo.VoteIneligibleReasonGuid.HasValue
               && !voteInfo.PersonIneligibleReasonGuid.HasValue
               && voteInfo.VoteStatusCode == VoteHelper.VoteStatusCode.Ok
               && voteInfo.PersonCombinedInfo == voteInfo.PersonCombinedInfoInVote;
      }
      return voteInfo.ValidationResult.Value;
    }

    /// <Summary>Does this vote need to be reviewed? (Underlying person info was changed)</Summary>
    public static bool VoteNeedReview(VoteInfo voteInfo)
    {
      return voteInfo.PersonCombinedInfo != voteInfo.PersonCombinedInfoInVote;
      //      || voteInfo.VoteStatusCode!= VoteHelper.VoteStatusCode.Ok;
      //       || voteInfo.BallotStatusCode == BallotStatusEnum.Review;
    }

    /// <Summary>Is this Vote not valid?</Summary>
    public static bool IsNotValid(VoteInfo voteInfo)
    {
      return !VoteIsValid(voteInfo);
    }

    /// <Summary>Update statuses... return true if any were updated</Summary>
    public static bool UpdateAllStatuses(List<VoteInfo> voteInfos, List<Vote> votes)
    {
      var changeMade = false;
      voteInfos.ForEach(delegate(VoteInfo info)
                          {
                            var oldStatus = info.VoteStatusCode;
                            var newStatus = info.VoteIneligibleReasonGuid.HasValue
                                              ? VoteHelper.VoteStatusCode.Spoiled
                                              : info.PersonCombinedInfo.HasContent() &&
                                                info.PersonCombinedInfo != info.PersonCombinedInfoInVote
                                                  ? VoteHelper.VoteStatusCode.Changed
                                                  : VoteHelper.VoteStatusCode.Ok;
                            if (newStatus == oldStatus) return;

                            // update both the VoteInfo and the Vote
                            info.VoteStatusCode = newStatus;
                            votes.Single(v => v.C_RowId == info.VoteId).StatusCode = newStatus;

                            changeMade = true;
                          });
      return changeMade;
    }
  }

  public class VoteGuidWithStatus
  {
    public Guid VoteGuid { get; set; }
  }
}