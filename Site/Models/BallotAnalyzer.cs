using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;
using TallyJ.Code;

namespace TallyJ.Models
{
  public class BallotAnalyzer
  {
    public int VotesNeededOnBallot { get; set; }

    /// <Summary>Create using current election to know number of votes needed</Summary>
    public BallotAnalyzer()
    {
      VotesNeededOnBallot = UserSession.CurrentElection.NumberToElect.AsInt();
    }

    public BallotAnalyzer(int votesNeededOnBallot)
    {
      VotesNeededOnBallot = votesNeededOnBallot;
    }

    /// <Summary>Review the votes, and determine if the containing ballot's status code should change</Summary>
    /// <param name="currentStatusCode">The current status code</param>
    /// <param name="votes">All the votes on this ballot</param>
    /// <param name="statusCode">The new status code</param>
    /// <returns>True if the new status code is different from the current status code</returns>
    public bool DetermineStatus(string currentStatusCode, List<vVoteInfo> votes, out string statusCode)
    {
      // if under review, don't change that status
      if (currentStatusCode == BallotStatusEnum.Review)
      {
        statusCode = currentStatusCode;
        return false;
      }

      // check counts
      var numVotes = votes.Count(v => v.VoteInvalidReasonGuid != BallotHelper.IneligibleReason.BlankVote);

      if (numVotes < VotesNeededOnBallot)
      {
        return StatusChanged(BallotStatusEnum.TooFew, currentStatusCode, out statusCode);
      }

      if (numVotes > VotesNeededOnBallot)
      {
        return StatusChanged(BallotStatusEnum.TooMany, currentStatusCode, out statusCode);
      }

      // find duplicates
      if (votes.Any(vote => votes.Count(v => v.PersonGuid.HasValue && v.PersonGuid == vote.PersonGuid) > 1))
      {
        return StatusChanged(BallotStatusEnum.Dup, currentStatusCode, out statusCode);
      }

      return StatusChanged(BallotStatusEnum.Ok, currentStatusCode, out statusCode);
    }

    /// <Summary>Determine if the new status is changed from the old status code.</Summary>
    private bool StatusChanged(string newStatusCode, string currentStatusCode, out string finalStatusCode)
    {
      var isChanged = currentStatusCode != newStatusCode;
      finalStatusCode = newStatusCode;
      return isChanged;
    }
  }
}