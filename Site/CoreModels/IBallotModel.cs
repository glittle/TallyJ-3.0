using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public interface IBallotModel
  {
    /// <param name="b"></param>
    /// <param name="createIfNeeded"></param>
    /// <Summary>Current Ballot... could be null</Summary>
    vBallotInfo GetCurrentBallotInfo(bool createIfNeeded = false);

    void SetAsCurrentBallot(int ballotId);

    int NextBallotNumAtComputer();
    string CurrentBallotJsonString();
    IEnumerable<object> CurrentVotesForJson();
    JsonResult SaveVote(int personId, int voteId, int count, Guid invalid);
    JsonResult DeleteVote(int vid);
    string InvalidReasonsByIdJsonString();
    string InvalidReasonsByGuidJsonString();
    object CurrentBallotsInfoList();
    JsonResult SwitchToBallotJson(int ballotId);
    bool SortVotes(List<int> ids);
    JsonResult StartNewBallotJson();
    JsonResult DeleteBallotJson();
    JsonResult SetNeedsReview(bool needsReview);
    object BallotForJson(vBallotInfo b);
  }
}