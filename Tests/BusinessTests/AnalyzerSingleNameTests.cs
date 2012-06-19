using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;

using TallyJ.CoreModels;
using TallyJ.EF;
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
      result1.TieBreakRequired.ShouldEqual(false);
    }

    [TestMethod]
    public void Invalid_Ballots_Affect_Results()
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

      var ballot1Guid = Guid.NewGuid();
      var ballot2Guid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok}
                      };

      var votes = new List<vVoteInfo>
                    {
                      // TODO 2012-03-24 Glen Little: Needs attention... these test are for normal elections, not single name...
                      new vVoteInfo {SingleNameElectionCount = 33},
                      new vVoteInfo {SingleNameElectionCount = 5},
                      new vVoteInfo {SingleNameElectionCount = 2},
                      new vVoteInfo {SingleNameElectionCount = 4},
                      new vVoteInfo {SingleNameElectionCount = 27},
                      new vVoteInfo {SingleNameElectionCount = 27},
                      new vVoteInfo {SingleNameElectionCount = 27},
                    };
      foreach (var vote in votes)
      {
        vote.ElectionGuid = electionGuid;
        vote.PersonCombinedInfo = vote.PersonCombinedInfoInVote = "zz";
        vote.BallotStatusCode = BallotStatusEnum.Ok;
        vote.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        vote.BallotGuid = ballot1Guid;
        vote.PersonGuid = Guid.NewGuid();
      }
      votes[3].VoteStatusCode = VoteHelper.VoteStatusCode.Changed;
      votes[4].BallotStatusCode = "TooFew";
      votes[5].PersonCombinedInfo = "different";
      votes[6].PersonIneligibleReasonGuid = Guid.NewGuid();

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      ballots[0].StatusCode.ShouldEqual(BallotStatusEnum.Ok);
      ballots[1].StatusCode.ShouldEqual(BallotStatusEnum.Ok);

      var summary = model.ResultSummaryAuto;
      summary.TotalVotes.ShouldEqual(125);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(54);
      summary.BallotsNeedingReview.ShouldEqual(1);

      results.Count.ShouldEqual(5);
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
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                      };

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 33, BallotGuid = ballots[0].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 5, BallotGuid = ballots[0].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 5, BallotGuid = ballots[0].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 5, BallotGuid = ballots[0].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[1].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[1].BallotGuid},// spoiled
                      new vVoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[1].BallotGuid},// spoiled
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        vVoteInfo.PersonGuid = Guid.NewGuid();
      }
      votes[3].VoteStatusCode = VoteHelper.VoteStatusCode.Changed;
      votes[4].BallotStatusCode = "TooFew"; // will be reset to Okay
      votes[5].PersonCombinedInfo = "different";
      votes[6].PersonIneligibleReasonGuid = Guid.NewGuid();

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].TieBreakRequired.ShouldEqual(false);
      resultTies[0].NumToElect.ShouldEqual(0);
      resultTies[0].NumInTie.ShouldEqual(3);


      var summary = model.ResultSummaryAuto;
      summary.BallotsNeedingReview.ShouldEqual(1);
      summary.TotalVotes.ShouldEqual(33 + 5 + 5 + 5 + 27 + 27 + 27);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(27+27);

      results.Count.ShouldEqual(5);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(33);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(false);
      result1.TieBreakRequired = false;

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(27);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(false);
      result2.TieBreakRequired = false;
      result2.ForceShowInOther = false;

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(5);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakRequired = true;
      result3.ForceShowInOther = false;
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


  }
}