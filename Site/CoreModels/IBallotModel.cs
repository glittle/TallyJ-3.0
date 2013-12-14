using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public interface IBallotModel
  {
    /// <Summary>Current Ballot... could be null</Summary>
    Ballot GetCurrentBallot();

    void SetAsCurrentBallot(int ballotId);

    int NextBallotNumAtComputer();
    object CurrentBallotInfo();
    IEnumerable<object> CurrentVotesForJs();
    JsonResult SaveVote(int personId, int voteId, int count, Guid? invalid);
    JsonResult DeleteVote(int vid);
    string InvalidReasonsByIdJsonString();
    string InvalidReasonsByGuidJsonString();
    object CurrentBallotsInfoList();
    object SwitchToBallotAndGetInfo(int ballotId);
    bool SortVotes(List<int> ids);
    JsonResult StartNewBallotJson();
    JsonResult DeleteBallotJson();
    JsonResult SetNeedsReview(bool needsReview);
    object BallotInfoForJs(Ballot b);
  }
}