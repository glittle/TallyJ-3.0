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

      var votes = new List<VoteInfo>
                    {
                      new VoteInfo {SingleNameElectionCount = 33},
                      new VoteInfo {SingleNameElectionCount = 5},
                      new VoteInfo {SingleNameElectionCount = 2},
                    };
      foreach (var voteInfo in votes)
      {
        voteInfo.BallotGuid = ballotGuid;
        voteInfo.PersonGuid = personGuid; // all for one person in this test
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

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

      var voteinfos = new List<VoteInfo>
                    {
                      // TODO 2012-03-24 Glen Little: Needs attention... these test are for normal elections, not single name...
                      new VoteInfo {SingleNameElectionCount = 33},
                      new VoteInfo {SingleNameElectionCount = 5},
                      new VoteInfo {SingleNameElectionCount = 2},
                      new VoteInfo {SingleNameElectionCount = 4},
                      new VoteInfo {SingleNameElectionCount = 27},
                      new VoteInfo {SingleNameElectionCount = 27},
                      new VoteInfo {SingleNameElectionCount = 27},
                    };
      var rowId = 1;
      foreach (var voteInfo in voteinfos)
      {
        voteInfo.VoteId = rowId++;
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.BallotGuid = ballot1Guid;
        voteInfo.PersonGuid = Guid.NewGuid();
        voteInfo.PersonCanReceiveVotes = true;
      }
      voteinfos[3].VoteStatusCode = VoteHelper.VoteStatusCode.Changed;
      voteinfos[4].BallotStatusCode = "TooFew";
      voteinfos[5].PersonCombinedInfo = "different"; // these will be invalid

      voteinfos[6].PersonIneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other;
      voteinfos[6].PersonCanReceiveVotes = IneligibleReasonEnum.Ineligible_Other.CanReceiveVotes;

      var model = new ElectionAnalyzerSingleName(_fakes, election, voteinfos, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      ballots[0].StatusCode.ShouldEqual(BallotStatusEnum.Ok);
      ballots[1].StatusCode.ShouldEqual(BallotStatusEnum.Ok);

      var summary = model.ResultSummaryFinal;
      summary.SpoiledBallots.ShouldEqual(0);
      summary.BallotsNeedingReview.ShouldEqual(1);
      summary.SpoiledVotes.ShouldEqual(54);

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

      var voteInfos = new List<VoteInfo>
                    {
                      new VoteInfo {SingleNameElectionCount = 33, BallotGuid = ballots[0].BallotGuid},
                      new VoteInfo {SingleNameElectionCount = 5, BallotGuid = ballots[0].BallotGuid},
                      new VoteInfo {SingleNameElectionCount = 5, BallotGuid = ballots[0].BallotGuid},
                      new VoteInfo {SingleNameElectionCount = 5, BallotGuid = ballots[0].BallotGuid},
                      new VoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[1].BallotGuid},
                      new VoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[1].BallotGuid},// spoiled
                      new VoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[1].BallotGuid},// spoiled
                    };
      var rowId = 1;
      foreach (var voteInfo in voteInfos)
      {
        voteInfo.VoteId = rowId++;
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonGuid = Guid.NewGuid();
        voteInfo.PersonCanReceiveVotes = true;
      }
      voteInfos[3].VoteStatusCode = VoteHelper.VoteStatusCode.Changed;
      voteInfos[4].BallotStatusCode = "TooFew"; // will be reset to Okay
      voteInfos[5].PersonCombinedInfo = "different";

      voteInfos[6].PersonIneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other;
      voteInfos[6].PersonCanReceiveVotes = IneligibleReasonEnum.Ineligible_Other.CanReceiveVotes;

      var model = new ElectionAnalyzerSingleName(_fakes, election, voteInfos, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].TieBreakRequired.ShouldEqual(false);
      resultTies[0].NumToElect.ShouldEqual(0);
      resultTies[0].NumInTie.ShouldEqual(3);


      var summary = model.ResultSummaryFinal;
      summary.BallotsNeedingReview.ShouldEqual(1);
      summary.NumBallotsWithManual.ShouldEqual(33 + 5 + 5 + 5 + 27 + 27 + 27);
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
      var votes = new List<VoteInfo>
                    {
                      new VoteInfo {SingleNameElectionCount = 33, PersonGuid = Guid.NewGuid()},
                      new VoteInfo {SingleNameElectionCount = 5, PersonGuid = Guid.NewGuid()},
                      new VoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid()},
                    };
      foreach (var voteInfo in votes)
      {
        voteInfo.BallotGuid = ballotGuid;
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

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
      var votes = new List<VoteInfo>
                    {
                      new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new VoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid()},
                    };
      foreach (var voteInfo in votes)
      {
        voteInfo.BallotGuid = ballotGuid;
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(10);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakGroup.ShouldEqual(1);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(10);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual(1);
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
      var votes = new List<VoteInfo>
                    {
                      new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                      new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid()},
                    };
      foreach (var voteInfo in votes)
      {
        voteInfo.BallotGuid = ballotGuid;
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerSingleName(_fakes, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(10);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakGroup.ShouldEqual(1);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(10);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual(1);

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(10);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual(1);
    }


  }
}