using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.EF;
using TallyJ.Models;
using TallyJ.Models.Helper;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class AnalyzerNormalTests
  {
    private AnalyzerFakes _fakes;
    private List<Person> _persons;

    private List<Person> SamplePeople
    {
      get { return _persons; }
    }

    [TestInitialize]
    public void Init()
    {
      _fakes = new AnalyzerFakes();

      _persons = new List<Person>
                        {
                          new Person {VotingMethod = VotingMethodEnum.InPerson},
                          new Person {},
                          new Person {},
                          new Person {},
                          new Person {},
                          new Person {IneligibleReasonGuid = IneligibleReasonEnum.Unidentifiable_Unknown_person},
                        };
      _persons.ForEach(delegate(Person p)
      {
        p.CanVote = true;
        p.PersonGuid = Guid.NewGuid();
      });
    }


    [TestMethod]
    public void Ballot_OnePerson()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
                       {
                         ElectionGuid = electionGuid,
                         NumberToElect = 1,
                         NumberExtra = 0
                       };

      var personGuid = Guid.NewGuid();

      var ballots = new List<Ballot>
                      {
                        new Ballot {BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok}
                      };
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.PersonGuid = personGuid; // all for one person in this test
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotGuid = ballots.Select(b => b.BallotGuid).First();
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results;

      results.Count.ShouldEqual(1);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(1);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);

      var resultSummaryAuto = model.ResultSummaryAuto;
      resultSummaryAuto.BallotsNeedingReview.ShouldEqual(0);
      resultSummaryAuto.BallotsReceived.ShouldEqual(1);

      resultSummaryAuto.DroppedOffBallots.ShouldEqual(0);
      resultSummaryAuto.InPersonBallots.ShouldEqual(1);
      resultSummaryAuto.MailedInBallots.ShouldEqual(0);
      resultSummaryAuto.NumEligibleToVote.ShouldEqual(5);
      resultSummaryAuto.NumVoters.ShouldEqual(1);
      resultSummaryAuto.ResultType.ShouldEqual(ResultType.Automatic);
    }

    [TestMethod]
    public void Election_3_people()
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
      var ballot3Guid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok}
                      };

      var person1Guid = Guid.NewGuid();
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 33, PersonGuid = person1Guid, BallotGuid = ballot1Guid},
                      new vVoteInfo {SingleNameElectionCount = 5, PersonGuid = person1Guid, BallotGuid = ballot2Guid},
                      new vVoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid(), BallotGuid = ballot3Guid},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = ballots.Single(b => b.BallotGuid == vVoteInfo.BallotGuid).StatusCode;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(2);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(2);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(false);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(1);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_3_people_with_Tie()
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
      var ballot3Guid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok}
                      };


      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid=ballot1Guid},
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid=ballot2Guid},
                      new vVoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid(), BallotGuid=ballot3Guid},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(1);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakGroup.ShouldEqual("A");
      result1.TieBreakRequired.ShouldEqual(true);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(1);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual("A");
      result2.ForceShowInOther.ShouldEqual(true);
      result2.TieBreakRequired.ShouldEqual(true);

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(1);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual("A");
      result3.ForceShowInOther.ShouldEqual(true);
      result3.TieBreakRequired.ShouldEqual(true);
    }

    [TestMethod]
    public void Election_3_people_with_Tie_Not_Required()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
                       {
                         ElectionGuid = electionGuid,
                         NumberToElect = 3,
                         NumberExtra = 0
                       };
      var location = new Location
      {
        LocationGuid = Guid.NewGuid(),
        ElectionGuid = electionGuid
      };

      var ballot1Guid = Guid.NewGuid();
      var ballot2Guid = Guid.NewGuid();
      var ballot3Guid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok}
                      };

      var person1Guid = Guid.NewGuid();
      var person2Guid = Guid.NewGuid();
      var person3Guid = Guid.NewGuid();
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = person1Guid, BallotGuid=ballot1Guid},
                      new vVoteInfo {PersonGuid = person1Guid, BallotGuid=ballot2Guid},
                      new vVoteInfo {PersonGuid = person1Guid, BallotGuid=ballot3Guid},
                      new vVoteInfo {PersonGuid = person2Guid, BallotGuid=ballot1Guid},
                      new vVoteInfo {PersonGuid = person2Guid, BallotGuid=ballot2Guid},
                      new vVoteInfo {PersonGuid = person2Guid, BallotGuid=ballot3Guid},
                      new vVoteInfo {PersonGuid = person3Guid, BallotGuid=ballot1Guid},
                      new vVoteInfo {PersonGuid = person3Guid, BallotGuid=ballot2Guid},
                      new vVoteInfo {PersonGuid = person3Guid, BallotGuid=ballot3Guid},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(0);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(false);

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(3);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakGroup.ShouldEqual("A");
      result1.TieBreakRequired.ShouldEqual(false);
      result1.ForceShowInOther.ShouldEqual(false);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(3);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Top);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual("A");
      result2.ForceShowInOther.ShouldEqual(false);
      result2.TieBreakRequired.ShouldEqual(false);

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(3);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Top);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual("A");
      result3.ForceShowInOther.ShouldEqual(false);
      result3.TieBreakRequired.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_3_people_with_3_way_Tie()
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
      var ballot3Guid = Guid.NewGuid();
      var ballots = new List<Ballot>
                      {
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok},
                      };
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid=ballot1Guid},
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid=ballot2Guid},
                      new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid=ballot3Guid},
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      results.Count.ShouldEqual(3);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(1);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakGroup.ShouldEqual("A");
      result1.TieBreakRequired = true;

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(1);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual("A");
      result2.TieBreakRequired = true;
      result2.ForceShowInOther = true;

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(1);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual("A");
      result3.TieBreakRequired = true;
      result3.ForceShowInOther = true;
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
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        vVoteInfo.BallotGuid = ballot1Guid;
        vVoteInfo.PersonGuid = Guid.NewGuid();
      }
      votes[3].VoteStatusCode = VoteHelper.VoteStatusCode.Changed;  
      votes[4].BallotStatusCode = "TooFew";
      votes[5].PersonCombinedInfo = "different";
      votes[6].PersonIneligibleReasonGuid = Guid.NewGuid();

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      ballots[0].StatusCode.ShouldEqual(BallotStatusEnum.TooMany);
      ballots[1].StatusCode.ShouldEqual(BallotStatusEnum.Empty);

      var summary = model.ResultSummaryAuto;
      summary.TotalVotes.ShouldEqual(2);
      summary.SpoiledBallots.ShouldEqual(2);
      summary.SpoiledVotes.ShouldEqual(0);
      summary.BallotsNeedingReview.ShouldEqual(0);

      results.Count.ShouldEqual(0);
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
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                        new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                      };

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {SingleNameElectionCount = 33, BallotGuid = ballots[0].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 5, BallotGuid = ballots[1].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 2, BallotGuid = ballots[2].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 4, BallotGuid = ballots[3].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[4].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[5].BallotGuid},
                      new vVoteInfo {SingleNameElectionCount = 27, BallotGuid = ballots[6].BallotGuid},
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

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      results.Count.ShouldEqual(3);

      var summary = model.ResultSummaryAuto;
      summary.TotalVotes.ShouldEqual(7);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(4);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);
      result1.IsTied.ShouldEqual(true);
      result1.TieBreakRequired = true;

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(1);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakRequired = true;
      result2.ForceShowInOther = true;

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(1);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakRequired = true;
      result3.ForceShowInOther = true;
    }


    [TestMethod]
    public void ElectionWithTwoSetsOfTies()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
      {
        ElectionGuid = electionGuid,
        NumberToElect = 2,
        NumberExtra = 2
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
                       new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                       new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                       new Ballot
                          {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
                      };

      // results wanted:
      //  person 0 = 3 votes
      //  person 1 = 2
      // ---
      //  person 2 = 2
      //  person 3 = 1
      // --
      //  person 4 = 1
      //  person 5 = 1
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid}, 
                      new vVoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid}, 
                      
                      new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[1].BallotGuid}, 
                      new vVoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[1].BallotGuid}, 
                      
                      new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[2].BallotGuid}, 
                      new vVoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[2].BallotGuid}, 

                      new vVoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[3].BallotGuid}, 
                      new vVoteInfo {PersonGuid = SamplePeople[3].PersonGuid, BallotGuid = ballots[3].BallotGuid}, 
                      
                      new vVoteInfo {PersonGuid = SamplePeople[4].PersonGuid, BallotGuid = ballots[4].BallotGuid}, 
                      new vVoteInfo {PersonGuid = SamplePeople[5].PersonGuid, BallotGuid = ballots[4].BallotGuid}, 
                    };
      foreach (var vVoteInfo in votes)
      {
        vVoteInfo.ElectionGuid = electionGuid;
        vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
        vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

      model.GenerateResults();

      var summary = model.ResultSummaryAuto;
      summary.BallotsReceived.ShouldEqual(5);
      summary.TotalVotes.ShouldEqual(10);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(0);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(2);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(2);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      resultTies[1].NumToElect.ShouldEqual(1);
      resultTies[1].NumInTie.ShouldEqual(3);
      resultTies[1].TieBreakRequired.ShouldEqual(true);

      results.Count.ShouldEqual(6);

      results[0].IsTied.ShouldEqual(false);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].TieBreakRequired = null;
      results[0].ForceShowInOther = null;

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual("A");
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired = true;
      results[1].ForceShowInOther = false;

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual("A");
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(true);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired = true;
      results[2].ForceShowInOther = false;

      results[3].IsTied.ShouldEqual(true);
      results[3].TieBreakGroup.ShouldEqual("B");
      results[3].CloseToPrev.ShouldEqual(true);
      results[3].CloseToNext.ShouldEqual(true);
      results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[3].TieBreakRequired = true;
      results[3].ForceShowInOther = false;

      results[4].IsTied.ShouldEqual(true);
      results[4].TieBreakGroup.ShouldEqual("B");
      results[4].CloseToPrev.ShouldEqual(true);
      results[4].CloseToNext.ShouldEqual(true);
      results[4].Section.ShouldEqual(ResultHelper.Section.Other);
      results[4].ForceShowInOther.ShouldEqual(true);
      results[4].TieBreakRequired = true;
      results[4].ForceShowInOther = true;

      results[5].IsTied.ShouldEqual(true);
      results[5].TieBreakGroup.ShouldEqual("B");
      results[5].CloseToPrev.ShouldEqual(true);
      results[5].CloseToNext.ShouldEqual(false);
      results[5].Section.ShouldEqual(ResultHelper.Section.Other);
      results[5].ForceShowInOther.ShouldEqual(true);
      results[5].TieBreakRequired = true;
      results[5].ForceShowInOther = true;

    }

  }
}