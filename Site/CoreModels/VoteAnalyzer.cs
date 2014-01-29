using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class VoteAnalyzer
  {
//    /// <Summary>Is this Vote valid?</Summary>
//    public static bool VoteIsValid(VoteInfo voteInfo)
//    {
//      if (!voteInfo.ValidationResult.HasValue)
//      {
//        voteInfo.VoteStatusCode = DetermineStatus(voteInfo);
//        voteInfo.ValidationResult = voteInfo.VoteStatusCode == VoteHelper.VoteStatusCode.Ok;
////          !voteInfo.VoteIneligibleReasonGuid.HasValue
////                                    && !voteInfo.PersonIneligibleReasonGuid.HasValue
////                                    && voteInfo.VoteStatusCode == VoteHelper.VoteStatusCode.Ok
////                                    && voteInfo.PersonCanReceiveVotes
////                                    && voteInfo.PersonCombinedInfo == voteInfo.PersonCombinedInfoInVote;
//      }
//      return voteInfo.ValidationResult.Value;
//    }

    public static string DetermineStatus(VoteInfo voteInfo)
    {
      return voteInfo.VoteIneligibleReasonGuid.HasValue
//             || voteInfo.PersonIneligibleReasonGuid.HasValue
             || !voteInfo.PersonCanReceiveVotes
        ? VoteHelper.VoteStatusCode.Spoiled
        : voteInfo.PersonCombinedInfo.HasContent() && voteInfo.PersonCombinedInfo != voteInfo.PersonCombinedInfoInVote
          ? VoteHelper.VoteStatusCode.Changed
          : VoteHelper.VoteStatusCode.Ok;
    }

    /// <Summary>Does this vote need to be reviewed? (Underlying person info was changed)</Summary>
    public static bool VoteNeedReview(VoteInfo voteInfo)
    {
      return voteInfo.PersonCombinedInfo != voteInfo.PersonCombinedInfoInVote;
      //      || voteInfo.VoteStatusCode!= VoteHelper.VoteStatusCode.Ok;
      //       || voteInfo.BallotStatusCode == BallotStatusEnum.Review;
    }

//    /// <Summary>Is this Vote not valid?</Summary>
//    public static bool IsNotValid(VoteInfo voteInfo)
//    {
//      return !VoteIsValid(voteInfo);
//    }

    /// <Summary>Update statuses... return true if any were updated</Summary>
    public static void UpdateAllStatuses(List<VoteInfo> voteInfos, List<Vote> votes, Action<DbAction, Vote> voteSaver)
    {
      voteInfos.ForEach(delegate(VoteInfo voteInfo)
      {
        var oldStatus = voteInfo.VoteStatusCode;
        var newStatus = DetermineStatus(voteInfo);
        if (newStatus == oldStatus) return;

        // update both the VoteInfo and the Vote
        voteInfo.VoteStatusCode = newStatus;
        var vote = votes.Single(v => v.C_RowId == voteInfo.VoteId);

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