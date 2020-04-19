using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class VoteAnalyzer
  {
    public static string DetermineStatus(VoteInfo voteInfo)
    {
      if (voteInfo.OnlineVoteRaw.HasContent() 
          && voteInfo.PersonIneligibleReasonGuid == null 
          && voteInfo.PersonGuid == null
          && voteInfo.VoteIneligibleReasonGuid == null)
      {
        return VoteStatusCode.OnlineRaw;
      }

      return voteInfo.VoteIneligibleReasonGuid.HasValue || !voteInfo.PersonCanReceiveVotes
          ? VoteStatusCode.Spoiled
          : voteInfo.PersonCombinedInfo.HasContent() &&
            !voteInfo.PersonCombinedInfo.StartsWith(voteInfo.PersonCombinedInfoInVote ?? "NULL")
              ? VoteStatusCode.Changed
              : VoteStatusCode.Ok;
    }

    /// <Summary>Does this vote need to be reviewed? (Underlying person info was changed)</Summary>
    public static bool VoteNeedReview(VoteInfo voteInfo)
    {
      return voteInfo.PersonCombinedInfo.HasContent() &&
             !voteInfo.PersonCombinedInfo.StartsWith(voteInfo.PersonCombinedInfoInVote ?? "NULL");
    }

    //    /// <Summary>Is this Vote not valid?</Summary>
    //    public static bool IsNotValid(VoteInfo voteInfo)
    //    {
    //      return !VoteIsValid(voteInfo);
    //    }

    /// <Summary>Update statuses... return true if any were updated</Summary>
    public static void UpdateAllStatuses(List<VoteInfo> voteInfos, List<Vote> votes, Action<DbAction, Vote> voteSaver)
    {
      voteInfos.ForEach(voteInfo =>
      {
        var oldStatus = voteInfo.VoteStatusCode;
        var newStatus = DetermineStatus(voteInfo);
        if (newStatus == oldStatus) return;

        // update both the VoteInfo and the Vote
        voteInfo.VoteStatusCode = newStatus;
        var vote = votes.Single(v => v.C_RowId == voteInfo.VoteId);

        voteSaver(DbAction.Attach, vote);

        vote.StatusCode = newStatus;

        //        voteSaver(DbAction.Save, vote);
      });
    }
  }

  public class VoteGuidWithStatus
  {
    public Guid VoteGuid { get; set; }
  }
}