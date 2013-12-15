using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;

using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class BallotAnalyzer : DataConnectedModel
  {
    private Func<int> _saveChangesToDatastore;

    /// <Summary>Create using current election to know number of votes needed</Summary>
    public BallotAnalyzer()
    {
      var currentElection = UserSession.CurrentElection;
      VotesNeededOnBallot = currentElection.NumberToElect.AsInt();
      IsSingleNameElection = currentElection.IsSingleNameElection;
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
      IsSingleNameElection = election.IsSingleNameElection;
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
    /// <param name="currentVotes"> </param>
    /// <param name="currentVotes">The list of Votes in this Ballot</param>
    /// <returns>Returns the updated status code</returns>
    public BallotStatusWithSpoilCount UpdateBallotStatus(Ballot ballot, List<VoteInfo> currentVotes)
    {
      if (IsSingleNameElection)
      {
        if (ballot.StatusCode != BallotStatusEnum.Ok)
        {
          ballot.StatusCode = BallotStatusEnum.Ok;
          SaveChangesToDatastore();
        }
        return new BallotStatusWithSpoilCount
          {
            Status = BallotStatusEnum.Ok,
            SpoiledCount = 0
          };
      }


      //double check:
      currentVotes.ForEach(vi => AssertAtRuntime.That(vi.BallotGuid == ballot.BallotGuid));

      //var currentVotes = currentVoteInfos.AsVotes().ToList();

      string ballotStatus;
      int spoiledCount;
      if (DetermineStatusFromVotesList(ballot.StatusCode, currentVotes, out ballotStatus, out spoiledCount))
      {
        ballot.StatusCode = ballotStatus;
        SaveChangesToDatastore();
      }
      return new BallotStatusWithSpoilCount
        {
          Status = BallotStatusEnum.Parse(ballotStatus), 
          SpoiledCount = spoiledCount
        };
    }

    /// <Summary>Review the votes, and determine if the containing ballot's status code should change</Summary>
    /// <param name="currentStatusCode"> The current status code </param>
    /// <param name="votes">  </param>
    /// <param name="votes"> All the votes on this ballot</param>
    /// <param name="statusCode"> The new status code </param>
    /// <param name="spoiledCount"> </param>
    /// <returns> True if the new status code is different from the current status code </returns>
    public bool DetermineStatusFromVotesList(string currentStatusCode, List<VoteInfo> votes, out string statusCode, out int spoiledCount)
    {
      spoiledCount = 0;

      // if under review, don't change that status
      if (currentStatusCode == BallotStatusEnum.Review || currentStatusCode==BallotStatusEnum.Verify)
      {
        statusCode = currentStatusCode;
        return false;
      }

      if (IsSingleNameElection)
      {
        return StatusChanged(BallotStatusEnum.Ok, currentStatusCode, out statusCode);
      }

      var needsVerification = votes.Any(v => v.PersonCombinedInfo != v.PersonCombinedInfoInVote);
      if (needsVerification)
      {
        return StatusChanged(BallotStatusEnum.Verify, currentStatusCode, out statusCode);
      }

      // check counts
      var numVotes = votes.Count();

      if (numVotes == 0)
      {
        return StatusChanged(BallotStatusEnum.Empty, currentStatusCode, out statusCode);
      }

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

      spoiledCount = votes.Count(v => v.VoteIneligibleReasonGuid.HasValue || v.PersonIneligibleReasonGuid.HasValue || v.PersonCombinedInfo != v.PersonCombinedInfoInVote);

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
    /// <param name="ballots">The list of Ballot records to update</param>
    /// <param name="voteInfos">All the Votes that are on all these Ballots.</param>
    public void UpdateAllBallotStatuses(List<Ballot> ballots, List<VoteInfo> voteInfos)
    {
      ballots.ForEach(b =>
                        {
                          var vVoteInfos = voteInfos.Where(vi => vi.BallotGuid == b.BallotGuid).ToList();
                          UpdateBallotStatus(b, vVoteInfos);
                        });
    }

    public bool BallotNeedsReview(Ballot ballot)
    {
      return ballot.StatusCode == BallotStatusEnum.Review || ballot.StatusCode==BallotStatusEnum.Verify;
    }
  }
}