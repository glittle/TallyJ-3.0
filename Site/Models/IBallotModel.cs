using System.Collections.Generic;
using System.Web.Mvc;
using TallyJ.EF;

namespace TallyJ.Models
{
  public interface IBallotModel
  {
    /// <param name="b"></param>
    /// <param name="createIfNeeded"></param>
    /// <Summary>Current Ballot... could be null</Summary>
    vBallotInfo GetCurrentBallotInfo();

    void SetAsCurrentBallot(int ballotId);

    int NextBallotNumAtComputer();
    string CurrentBallotJsonString();
    IEnumerable<object> CurrentVotesForJson();
    JsonResult SaveVote(int personId, int voteId, int count, int invalid);
    JsonResult DeleteVote(int vid);
    string InvalidReasonsJsonString();
    object CurrentBallotsInfoList();
    JsonResult SwitchToBallotJson(int ballotId);
    bool SortVotes(List<int> ids);
    JsonResult StartNewBallotJson();
  }
}