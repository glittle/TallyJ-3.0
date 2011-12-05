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
    vBallotInfo GetCurrentBallotInfo(bool createIfNeeded);

    void SetAsCurrentBallot(int ballotId);

    int NextBallotNumAtComputer();
    string CurrentBallotJsonString();
    IEnumerable<object> CurrentVotes();
    JsonResult SaveVote(int personId, int voteId, int count, int invalid);
    JsonResult DeleteVote(int vid);
    string InvalidReasonsJsonString();
    string CurrentBallotsJsonString();
    JsonResult SwitchToBallotJson(int ballotId);
  }
}