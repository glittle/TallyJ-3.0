using System.Collections.Generic;
using System.Web.Mvc;
using TallyJ.EF;

namespace TallyJ.Models
{
  public interface IBallotModel
  {
    /// <Summary>Current Ballot... could be null</Summary>
    Ballot GetCurrentBallot();

    void SetAsCurrentBallot(int ballotId);

    Ballot CreateBallot();
    int NextBallotNumAtComputer();
    string CurrentVotesJson();
    IEnumerable<object> CurrentVotes();
    JsonResult SaveVote(int personId, int voteId, int count, int invalid);
    JsonResult DeleteVote(int vid);
    string InvalidReasonsJson();
  }
}