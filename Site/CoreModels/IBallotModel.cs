using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public interface IBallotModel
  {
    /// <Summary>Current Ballot... could be null</Summary>
    Ballot GetCurrentBallot(bool refresh = false);

    void SetAsCurrentBallot(int ballotId);

    int NextBallotNumAtComputer();
    object CurrentBallotInfo();
    IEnumerable<object> CurrentVotesForJs(Ballot ballotInfo, List<Vote> allVotes);
    JsonResult SaveVote(int personId, int voteId, int count, Guid? invalid);
    JsonResult DeleteVote(int vid);
    object CurrentBallotsInfoList();
    object SwitchToBallotAndGetInfo(int ballotId, bool refresh);
    bool SortVotes(List<int> ids, VoteCacher voteCacher);
    JsonResult StartNewBallotJson();
    JsonResult DeleteBallotJson();
    JsonResult SetNeedsReview(bool needsReview);
    object BallotInfoForJs(Ballot b, List<Vote> allVotes);
  }
}