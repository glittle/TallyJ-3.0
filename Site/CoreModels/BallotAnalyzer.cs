using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;

using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class BallotAnalyzer : DataConnectedModel
  {
    public Action<DbAction, Ballot> BallotSaver { get; set; }
    //private Func<int> _saveChangesToDatastore;

    /// <Summary>Create using current election to know number of votes needed</Summary>
    public BallotAnalyzer()
    {
      var currentElection = UserSession.CurrentElection;
      VotesNeededOnBallot = currentElection.NumberToElect.AsInt();
      IsSingleNameElection = currentElection.IsSingleNameElection;
      BallotSaver = new Savers(Db).BallotSaver;
    }

    /// <Summary>For testing</Summary>
    public BallotAnalyzer(int? votesNeededOnBallot, bool isSingleNameElection)
    {
      VotesNeededOnBallot = votesNeededOnBallot.AsInt();
      IsSingleNameElection = isSingleNameElection;
      BallotSaver = new Savers(Db).BallotSaver;
    }

    public BallotAnalyzer(Election election, Action<DbAction, Ballot> ballotSaver)
    {
      BallotSaver = ballotSaver;
      IsSingleNameElection = election.IsSingleNameElection;
      VotesNeededOnBallot = election.NumberToElect.AsInt();
    }

    //    private Func<int> SaveChangesToDatastore
    //    {
    //      get { return _saveChangesToDatastore ?? (_saveChangesToDatastore = Db.SaveChanges); }
    //      set { _saveChangesToDatastore = value; }
    //    }

    public bool IsSingleNameElection { get; set; }

    private int VotesNeededOnBallot { get; set; }

    /// <Summary>Update the Ballot status of this ballot, based on these Votes.</Summary>
    /// <param name="ballot">The Ballot or vBallotInfo to check and update.</param>
    /// <param name="currentVotes">The list of Votes in this Ballot</param>
    /// <param name="refreshVoteStatus"></param>
    /// <returns>Returns the updated status code</returns>
    public BallotStatusWithSpoilCount UpdateBallotStatus(Ballot ballot, List<VoteInfo> currentVotes, bool refreshVoteStatus)
    {
      if (IsSingleNameElection)
      {
        if (ballot.StatusCode != BallotStatusEnum.Ok)
        {
          BallotSaver(DbAction.Attach, ballot);

          ballot.StatusCode = BallotStatusEnum.Ok;

          BallotSaver(DbAction.Save, ballot);
        }
        return new BallotStatusWithSpoilCount
          {
            Status = BallotStatusEnum.Ok,
            SpoiledCount = 0
          };
      }


      //double check:
      currentVotes.ForEach(vi => AssertAtRuntime.That(vi.BallotGuid == ballot.BallotGuid));

      if (refreshVoteStatus)
      {
        VoteAnalyzer.UpdateAllStatuses(currentVotes, new VoteCacher(Db).AllForThisElection, new Savers(Db).VoteSaver);
      }

      string newStatus;
      int spoiledCount;

      if (DetermineStatusFromVotesList(ballot.StatusCode, currentVotes, out newStatus, out spoiledCount))
      {
        BallotSaver(DbAction.Attach, ballot);
        ballot.StatusCode = newStatus;
        BallotSaver(DbAction.Save, ballot);
      }
      return new BallotStatusWithSpoilCount
        {
          Status = BallotStatusEnum.Parse(newStatus),
          SpoiledCount = spoiledCount
        };
    }

    /// <Summary>Review the votes, and determine if the containing ballot's status code should change</Summary>
    /// <param name="currentStatusCode"> The current status code </param>
    /// <param name="voteInfos"> All the votes on this ballot</param>
    /// <param name="statusCode"> The new status code </param>
    /// <param name="spoiledCount"> </param>
    /// <returns> True if the new status code is different from the current status code </returns>
    public bool DetermineStatusFromVotesList(string currentStatusCode, List<VoteInfo> voteInfos, out string statusCode, out int spoiledCount)
    {
      spoiledCount = voteInfos.Count(v => v.VoteStatusCode != VoteHelper.VoteStatusCode.Ok);

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

      var needsVerification = voteInfos.Any(v => v.PersonCombinedInfo != v.PersonCombinedInfoInVote);
      if (needsVerification)
      {
        return StatusChanged(BallotStatusEnum.Verify, currentStatusCode, out statusCode);
      }

      // check counts
      var numVotes = voteInfos.Count();

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
      if (voteInfos.Any(vote => voteInfos.Count(v => v.PersonGuid.HasValue && v.PersonGuid == vote.PersonGuid) > 1))
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
    /// <param name="ballots">The list of Ballot records to update</param>
    /// <param name="voteInfos">All the Votes that are on all these Ballots.</param>
    public void UpdateAllBallotStatuses(List<Ballot> ballots, List<VoteInfo> voteInfos)
    {
      ballots.ToList().ForEach(b =>
                        {
                          var vVoteInfos = voteInfos.Where(vi => vi.BallotGuid == b.BallotGuid).ToList();
                          UpdateBallotStatus(b, vVoteInfos, false);
                        });
    }

    /// <summary>
    /// Needs review or verification
    /// </summary>
    /// <param name="ballot"></param>
    /// <returns></returns>
    public static bool BallotNeedsReview(Ballot ballot)
    {
      return ballot.StatusCode == BallotStatusEnum.Review || ballot.StatusCode == BallotStatusEnum.Verify;
    }
  }
}