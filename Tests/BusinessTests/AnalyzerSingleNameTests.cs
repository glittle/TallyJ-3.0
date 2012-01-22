using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.EF;
using TallyJ.Models;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class AnalyzerSingleNameTests
  {
    private AnalyzerFakes _fakes;

    private List<Person> SamplePeople
    {
      get
      {
        return new List<Person>
                 {
                   new Person {PersonGuid = Guid.NewGuid()},
                   new Person {PersonGuid = Guid.NewGuid()},
                   new Person {PersonGuid = Guid.NewGuid()},
                   new Person {PersonGuid = Guid.NewGuid()},
                   new Person {PersonGuid = Guid.NewGuid()},
                 };
      }
    }

    [TestInitialize]
    public void Init()
    {
      _fakes = new AnalyzerFakes();
    }


    [TestMethod]
    public void SingleNameElection_1_person()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
                       {
                         ElectionGuid = electionGuid,
                         NumberToElect = 1,
                         NumberExtra = 0
                       };
      var location = new Location
                       {
                         LocationGuid = Guid.NewGuid(),
                         ElectionGuid = electionGuid
                       };
      var personGuid = Guid.NewGuid();
      var ballotGuid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballotGuid, StatusCode = BallotStatusEnum.Ok}
                      };

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 33},
                      new vVoteInfo {SingleNameElectionCount = 5},
                      new vVoteInfo {SingleNameElectionCount = 2},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.BallotGuid = ballotGuid;
        vVoteInfo.PersonGuid = personGuid; // all for one person in this test
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results;

      results.Count.ShouldEqual(1);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(33 + 5 + 2);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);

      result1.CloseToNext.ShouldEqual(false);
      result1.CloseToPrev.ShouldEqual(false);
      result1.ForceShowInOther.ShouldEqual(false);
      result1.IsTieResolved.ShouldEqual(null);
      result1.IsTied.ShouldEqual(false);
      result1.RankInExtra.ShouldEqual(null);
      result1.TieBreakCount.ShouldEqual(null);
      result1.TieBreakGroup.ShouldEqual(null);
      result1.TieBreakRequired.ShouldEqual(null);
    }

    [TestMethod]
    public void SingleNameElection_3_people()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
                       {
                         ElectionGuid = electionGuid,
                         NumberToElect = 1,
                         NumberExtra = 0
                       };
      var location = new Location
      {
        LocationGuid = Guid.NewGuid(),
        ElectionGuid = electionGuid
      };

      var ballotGuid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballotGuid, StatusCode = BallotStatusEnum.Ok}
                      };
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 33, PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {SingleNameElectionCount = 5, PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid()},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.BallotGuid = ballotGuid;
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(33);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(5);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(false);
    }

    [TestMethod]
    public void SingleNameElection_3_people_with_Tie()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
                       {
                         ElectionGuid = electionGuid,
                         NumberToElect = 1,
                         NumberExtra = 0
                       };
      var location = new Location
      {
        LocationGuid = Guid.NewGuid(),
        ElectionGuid = electionGuid
      };

      var ballotGuid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballotGuid, StatusCode = BallotStatusEnum.Ok}
                      };
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid()},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.BallotGuid = ballotGuid;
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(10);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakGroup.ShouldEqual("A");

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(10);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual("A");
    }

    [TestMethod]
    public void SingleNameElection_3_people_with_3_way_Tie()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
                       {
                         ElectionGuid = electionGuid,
                         NumberToElect = 1,
                         NumberExtra = 0
                       };
      var location = new Location
      {
        LocationGuid = Guid.NewGuid(),
        ElectionGuid = electionGuid
      };
      var ballotGuid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballotGuid, StatusCode = BallotStatusEnum.Ok}
                      };
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.BallotGuid = ballotGuid;
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(10);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakGroup.ShouldEqual("A");

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(10);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual("A");

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(10);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual("A");
    }


    [TestMethod]
    public void Invalid_People_Do_Not_Affect_Results()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
                       {
                         ElectionGuid = electionGuid,
                         NumberToElect = 1,
                         NumberExtra = 0
                       };
      var location = new Location
      {
        LocationGuid = Guid.NewGuid(),
        ElectionGuid = electionGuid
      };

      var ballotGuid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballotGuid, StatusCode = BallotStatusEnum.Ok}
                      };
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 33},
                      new vVoteInfo {SingleNameElectionCount = 5},
                      new vVoteInfo {SingleNameElectionCount = 2},
                      new vVoteInfo {SingleNameElectionCount = 4},
                      new vVoteInfo {SingleNameElectionCount = 27},
                      new vVoteInfo {SingleNameElectionCount = 27},
                      new vVoteInfo {SingleNameElectionCount = 27},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.BallotGuid = ballotGuid;
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        vVoteInfo.BallotGuid = Guid.NewGuid();
        vVoteInfo.PersonGuid = Guid.NewGuid();
      }
      votes[3].VoteStatusCode = VoteHelper.VoteStatusCode.Changed;
      votes[4].BallotStatusCode = "Incomplete"; // will be reset to Okay
      votes[6].PersonIneligibleReasonGuid = Guid.NewGuid();
      votes[5].PersonCombinedInfo = "different";

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var summary = model.ResultSummaryAuto;
      summary.TotalVotes.ShouldEqual(125);
      summary.SpoiledBallots.ShouldEqual(1);
      summary.SpoiledVotes.ShouldEqual(85);
      summary.BallotsNeedingReview.ShouldEqual(1);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(33);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(5);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(false);
    }

  }
}