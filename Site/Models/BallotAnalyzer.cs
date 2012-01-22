using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class BallotAnalyzer : DataConnectedModel
  {
    private Func<int> _saveChangesToDatastore;

    /// <Summary>Create using current election to know number of votes needed</Summary>
    public BallotAnalyzer()
    {
      var currentElection = UserSession.CurrentElection;
      VotesNeededOnBallot = currentElection.NumberToElect.AsInt();
      IsSingleNameElection = currentElection.IsSingleNameElection.AsBool();
    }

    /// <Summary>For testing</Summary>
    public BallotAnalyzer(int? votesNeededOnBallot, Func<int> saveChangesToDatastore, bool isSingleNameElection)
    {
      VotesNeededOnBallot = votesNeededOnBallot.AsInt();
      SaveChangesToDatastore = saveChangesToDatastore;
      IsSingleNameElection = isSingleNameElection;
    }

    public BallotAnalyzer(Election election, Func<int> saveChangesToDatastore)
    {
      IsSingleNameElection = election.IsSingleNameElection.AsBool();
      VotesNeededOnBallot = election.NumberToElect.AsInt();
      SaveChangesToDatastore = saveChangesToDatastore;
    }

    private Func<int> SaveChangesToDatastore
    {
      get { return _saveChangesToDatastore ?? (_saveChangesToDatastore = Db.SaveChanges); }
      set { _saveChangesToDatastore = value; }
    }

    public bool IsSingleNameElection { get; set; }

    private int VotesNeededOnBallot { get; set; }

    /// <Summary>Update the Ballot status of this ballot, based on these Votes.</Summary>
    /// <param name="ballot">The Ballot or vBallotInfo to check and update.</param>
    /// <param name="currentVotes">The list of Votes in this Ballot</param>
    /// <returns>Returns the updated status code</returns>
    public string UpdateBallotStatus(IBallotBase ballot, List<vVoteInfo> currentVotes)
    {
      if (IsSingleNameElection)
      {
        if (ballot.StatusCode != BallotStatusEnum.Ok)
        {
          ballot.StatusCode = BallotStatusEnum.Ok;
          SaveChangesToDatastore();
        }
        return BallotStatusEnum.Ok;
      }


      //double check:
      currentVotes.ForEach(vi => AssertAtRuntime.That(vi.BallotGuid == ballot.BallotGuid));

      string ballotStatus;
      if (DetermineStatusFromVotesList(ballot.StatusCode, currentVotes, out ballotStatus))
      {
        ballot.StatusCode = ballotStatus;
        SaveChangesToDatastore();
      }
      return ballotStatus;
    }

    /// <Summary>Review the votes, and determine if the containing ballot's status code should change</Summary>
    /// <param name="currentStatusCode"> The current status code </param>
    /// <param name="votes"> All the votes on this ballot </param>
    /// <param name="statusCode"> The new status code </param>
    /// <returns> True if the new status code is different from the current status code </returns>
    public bool DetermineStatusFromVotesList(string currentStatusCode, List<vVoteInfo> votes, out string statusCode)
    {
      // if under review, don't change that status
      if (currentStatusCode == BallotStatusEnum.Review)
      {
        statusCode = currentStatusCode;
        return false;
      }

      if (IsSingleNameElection)
      {
        return StatusChanged(BallotStatusEnum.Ok, currentStatusCode, out statusCode);
      }

      // check counts
      var numVotes = votes.Count(v => v.VoteInvalidReasonGuid != VoteHelper.IneligibleReason.BlankVote);

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
    private static bool StatusChanged(string newStatusCode, string currentStatusCode, out string finalStatusCode)
    {
      var isChanged = currentStatusCode != newStatusCode;
      finalStatusCode = newStatusCode;
      return isChanged;
    }

    /// <Summary>Run <see cref="UpdateBallotStatus"/> on each of these Ballots, updating the database if needed</Summary>
    /// <param name="ballotInfos">The list of Ballot records to update</param>
    /// <param name="voteInfos">All the Votes that are on all these Ballots.</param>
    public void UpdateAllBallotStatuses(List<Ballot> ballotInfos, List<vVoteInfo> voteInfos)
    {
      ballotInfos.ForEach(bi => UpdateBallotStatus(bi, voteInfos.Where(vi=>vi.BallotGuid==bi.BallotGuid).ToList()));
    }

    public bool BallotNeedsReview(Ballot ballotInfo)
    {
      return ballotInfo.StatusCode == BallotStatusEnum.Review;
    }
  }
}