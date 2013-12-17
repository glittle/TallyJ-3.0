using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

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
    public static void UpdateAllStatuses(List<VoteInfo> voteInfos, List<Vote> votes, Action<DbAction, Vote> voteSaver)
    {
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
        var vote = votes.Single(v => v.C_RowId == info.VoteId);

        voteSaver(DbAction.Attach, vote);

        vote.StatusCode = newStatus;

        voteSaver(DbAction.Save, vote);
      });
    }
  }

  public class VoteGuidWithStatus
  {
    public Guid VoteGuid { get; set; }
  }
}