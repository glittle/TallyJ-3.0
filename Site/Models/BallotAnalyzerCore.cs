using System;
using System.Collections.Generic;
using TallyJ.EF;
using System.Linq;

namespace TallyJ.Models
{
  public abstract class BallotAnalyzerCore
  {
    public Election Election { get; set; }
    public List<Person> People { get; set; }
    public Func<int> SaveChanges { get; set; }

    protected BallotAnalyzerCore()
    {
    }

    protected BallotAnalyzerCore(Election election, List<Person> people, Func<int> saveChanges)
    {
      Election = election;
      People = people;
      SaveChanges = saveChanges;
    }
  }

  public class BallotAnalyzerNormal : BallotAnalyzerCore
  {
    public BallotAnalyzerNormal()
    {
    }

    public BallotAnalyzerNormal(Election election, List<Person> people, Func<int> saveChanges)
      : base(election, people, saveChanges)
    {
    }


    public void Analyze(vBallotInfo ballot, List<vVoteInfo> votes)
    {
      // if under review, don't change that status
      if (ballot.StatusCode == BallotHelper.BallotStatusCode.Review)
      {
        return;
      }

      // find duplicates
      if (votes.Any(vote => votes.Count(v => v.PersonGuid == vote.PersonGuid) > 1))
      {
        ballot.StatusCode = BallotHelper.BallotStatusCode.HasDup;
        return;
      }

      if (ballot.StatusCode == BallotHelper.BallotStatusCode.InEdit)
      {
        return;
      }

      // check counts
      var numVotes = votes.Count;
      var numberToElect = Election.NumberToElect;

      if (numVotes < numberToElect)
      {
        ballot.StatusCode = BallotHelper.BallotStatusCode.TooFew;
        return;
      }

      if (numVotes > numberToElect)
      {
        ballot.StatusCode = BallotHelper.BallotStatusCode.TooMany;
        return;
      }
      ballot.StatusCode = BallotHelper.BallotStatusCode.Ok;
    }
  }
  public class BallotAnalyzerSingleName : BallotAnalyzerCore
  {
  }
}