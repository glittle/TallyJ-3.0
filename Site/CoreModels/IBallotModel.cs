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
    JsonResult SaveVote(int personId, int voteId, Guid? invalid, int lastVid, int count, bool verifying);
    JsonResult DeleteVote(int vid);
    object CurrentBallotsInfoList(bool refresh = false);
    object SwitchToBallotAndGetInfo(int ballotId, bool refresh);
    bool SortVotes(List<int> ids, VoteCacher voteCacher);
    JsonResult StartNewBallotJson();
    JsonResult DeleteBallotJson();
    JsonResult SetNeedsReview(bool needsReview);
    object BallotInfoForJs(Ballot b, List<Vote> allVotes);
    bool CreateBallotForOnlineVoter(List<OnlineRawVote> poolIds, out string errorMessage);
  }
}