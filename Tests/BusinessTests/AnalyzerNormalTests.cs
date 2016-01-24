using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;
using Tests.Support;
using TallyJ.Code.UnityRelated;
using TallyJ.Code.Session;
using TallyJ.Code;

namespace Tests.BusinessTests
{
  [TestClass]
  public class AnalyzerNormalTests
  {
    private AnalyzerFakes _fakes;
    private List<Person> _samplePeople;
    private ITallyJDbContext Db;
    private Guid _electionGuid;
    private Guid _locationGuid;
    private int _rowId = 1; // can be used (and incremented) everywhere

    private List<Person> SamplePeople
    {
      get { return _samplePeople; }
    }

    [TestInitialize]
    public void Init()
    {
      _fakes = new AnalyzerFakes();

      Db = _fakes.DbContext;
      UnityInstance.Offer(Db);

      _electionGuid = Guid.NewGuid();
      _locationGuid = Guid.NewGuid();

      SessionKey.CurrentElectionGuid.SetInSession(_electionGuid);
      SessionKey.CurrentLocationGuid.SetInSession(_locationGuid);

      SessionKey.CurrentComputer.SetInSession(new Computer
      {
        ComputerGuid = Guid.NewGuid(),
        ComputerCode = "TEST"
      });

      _samplePeople = new List<Person>
      {
        new Person {VotingMethod = VotingMethodEnum.InPerson},
        new Person {},
        new Person {},
        new Person {},
        new Person {},
        new Person {IneligibleReasonGuid = IneligibleReasonEnum.Unidentifiable_Unknown_person},
      };

      _samplePeople.ForEach(delegate (Person p)
      {
        p.C_RowId = _rowId++;
        p.ElectionGuid = _electionGuid;
        p.CanVote = true;
        p.CanReceiveVotes = true;
        p.PersonGuid = Guid.NewGuid();
      });

      Db.Person.AddRange(_samplePeople);
    }

    public void PrepElection(Election election)
    {
      Db.Election.Add(election);
      ItemKey.CurrentElection.SetInPageItems(election);

      Db.Location.Add(new Location
      {
        ElectionGuid = _electionGuid,
        LocationGuid = _locationGuid
      });
    }

    [TestMethod]
    public void Ballot_TwoPeople()
    {
      PrepElection(new Election
      {
        ElectionGuid = _electionGuid,
        NumberToElect = 2,
        NumberExtra = 0,
        CanReceive = ElectionModel.CanVoteOrReceive.All
      });

      var ballots = new[]
      {
        new Ballot().With(_locationGuid, _rowId++)
      };
      Db.Ballot.AddRange(ballots);

      Db.Vote.AddRange(new[]
      {
        new Vote().With(SamplePeople[0], ballots[0], _rowId++),
        new Vote().With(SamplePeople[1], ballots[0], _rowId++)
      });

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderBy(r => r.Rank).ToList();

      results.Count.ShouldEqual(2);

      var result1 = results[0];
      result1.VoteCount.ShouldEqual(1);
      result1.Rank.ShouldEqual(1);
      result1.Section.ShouldEqual(ResultHelper.Section.Top);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(1);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Top);

      var resultSummaryFinal = model.ResultSummaryFinal;
      resultSummaryFinal.BallotsNeedingReview.ShouldEqual(0);
      resultSummaryFinal.BallotsReceived.ShouldEqual(1);

      resultSummaryFinal.DroppedOffBallots.ShouldEqual(0);
      resultSummaryFinal.InPersonBallots.ShouldEqual(1);
      resultSummaryFinal.MailedInBallots.ShouldEqual(0);
      resultSummaryFinal.CalledInBallots.ShouldEqual(0);
      resultSummaryFinal.NumEligibleToVote.ShouldEqual(5);
      resultSummaryFinal.NumVoters.ShouldEqual(1);
      resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);
    }

    [TestMethod]
    public void Ballot_TwoPeople_NameChanged()
    {
      PrepElection(new Election
      {
        ElectionGuid = _electionGuid,
        NumberToElect = 2,
        NumberExtra = 0,
        CanReceive = ElectionModel.CanVoteOrReceive.All
      });

      var ballots = new[]
      {
        new Ballot().With(_locationGuid, _rowId++)
      };
      Db.Ballot.AddRange(ballots);

      var votes = new[] {
        new Vote().With(SamplePeople[0], ballots[0], _rowId++),
        new Vote().With(SamplePeople[1], ballots[0], _rowId++),
      };
      votes[0].PersonCombinedInfo = "yy";
      Db.Vote.AddRange(votes);

      var model = new ElectionAnalyzerNormal(_fakes); //, election, vVoteInfos, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderBy(r => r.Rank).ToList();

      results.Count.ShouldEqual(0);

      var resultSummaryFinal = model.ResultSummaryFinal;
      resultSummaryFinal.BallotsNeedingReview.ShouldEqual(1);
      resultSummaryFinal.BallotsReceived.ShouldEqual(1);

      resultSummaryFinal.DroppedOffBallots.ShouldEqual(0);
      resultSummaryFinal.InPersonBallots.ShouldEqual(1);
      resultSummaryFinal.MailedInBallots.ShouldEqual(0);
      resultSummaryFinal.CalledInBallots.ShouldEqual(0);
      resultSummaryFinal.NumEligibleToVote.ShouldEqual(5);
      resultSummaryFinal.NumVoters.ShouldEqual(1);
      resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);
    }

    [TestMethod]
    public void Ballot_TwoPeople_AllSpoiled()
    {
      var electionGuid = Guid.NewGuid();
      var election = new Election
      {
        ElectionGuid = electionGuid,
        NumberToElect = 2,
        NumberExtra = 0,
        CanReceive = ElectionModel.CanVoteOrReceive.All
      };

      var personGuid = Guid.NewGuid();

      var ballots = new List<Ballot>
      {
        new Ballot {BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok}
      };
      var votes = new List<VoteInfo>
      {
        new VoteInfo {VoteId = -1, PersonGuid = personGuid, VoteIneligibleReasonGuid = Guid.NewGuid()},
        new VoteInfo {VoteId = -2, PersonGuid = Guid.NewGuid(), VoteIneligibleReasonGuid = Guid.NewGuid()},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotGuid = ballots.Select(b => b.BallotGuid).First();
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
      }

      var model = new ElectionAnalyzerNormal(_fakes);//, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderBy(r => r.Rank).ToList();
      results.Count.ShouldEqual(0);

      ballots[0].StatusCode.ShouldEqual(BallotStatusEnum.Ok);
      votes[0].VoteStatusCode.ShouldEqual(VoteHelper.VoteStatusCode.Spoiled);
      votes[1].VoteStatusCode.ShouldEqual(VoteHelper.VoteStatusCode.Spoiled);

      var spoiledCount = votes.Count(v => v.VoteIneligibleReasonGuid.HasValue || v.PersonIneligibleReasonGuid.HasValue || v.PersonCombinedInfo != v.PersonCombinedInfoInVote);
      spoiledCount.ShouldEqual(2);

      var resultSummaryFinal = model.ResultSummaryFinal;
      resultSummaryFinal.BallotsNeedingReview.ShouldEqual(0);
      resultSummaryFinal.BallotsReceived.ShouldEqual(1);

      resultSummaryFinal.DroppedOffBallots.ShouldEqual(0);
      resultSummaryFinal.InPersonBallots.ShouldEqual(1);
      resultSummaryFinal.MailedInBallots.ShouldEqual(0);
      resultSummaryFinal.CalledInBallots.ShouldEqual(0);
      resultSummaryFinal.NumEligibleToVote.ShouldEqual(5);
      resultSummaryFinal.NumVoters.ShouldEqual(1);
      resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);
    }

    [TestMethod]
    public void Election_3_people()
    {
      PrepElection(new Election
      {
        ElectionGuid = _electionGuid,
        NumberToElect = 1,
        NumberExtra = 0
      });

      var ballots = new[]
      {
        new Ballot().With(_locationGuid, _rowId++),
        new Ballot().With(_locationGuid, _rowId++),
        new Ballot().With(_locationGuid, _rowId++),
      };
      Db.Ballot.AddRange(ballots);

      var votes = new[] {
        new Vote {SingleNameElectionCount = 33 }.With(SamplePeople[0], ballots[0], _rowId++),
        new Vote {SingleNameElectionCount = 5 }.With(SamplePeople[0], ballots[1], _rowId++),
        new Vote {SingleNameElectionCount = 2 }.With(SamplePeople[1], ballots[2], _rowId++),
      };
      Db.Vote.AddRange(votes);

      var model = new ElectionAnalyzerNormal(_fakes); //, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

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
    public void Election_3_people_With_Manual_Results()
    {
      PrepElection(new Election
      {
        ElectionGuid = _electionGuid,
        NumberToElect = 1,
        NumberExtra = 0
      });

      var ballots = new[]
      {
        new Ballot().With(_locationGuid, _rowId++),
        new Ballot().With(_locationGuid, _rowId++),
        new Ballot().With(_locationGuid, _rowId++),
      };
      Db.Ballot.AddRange(ballots);

      var votes = new[] {
        new Vote {SingleNameElectionCount = 33 }.With(SamplePeople[0], ballots[0], _rowId++),
        new Vote {SingleNameElectionCount = 5 }.With(SamplePeople[0], ballots[1], _rowId++),
        new Vote {SingleNameElectionCount = 2 }.With(SamplePeople[1], ballots[2], _rowId++),
      };
      Db.Vote.AddRange(votes);

      Db.ResultSummary.Add(new ResultSummary
      {
        ResultType = ResultType.Manual,
        BallotsReceived = 4 // override the real count
      });

      var model = new ElectionAnalyzerNormal(_fakes); //, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var resultSummaryFinal = model.ResultSummaryFinal;
      resultSummaryFinal.BallotsNeedingReview.ShouldEqual(0);
      resultSummaryFinal.BallotsReceived.ShouldEqual(4);

      resultSummaryFinal.DroppedOffBallots.ShouldEqual(0);
      resultSummaryFinal.InPersonBallots.ShouldEqual(1);
      resultSummaryFinal.MailedInBallots.ShouldEqual(0);
      resultSummaryFinal.CalledInBallots.ShouldEqual(0);
      resultSummaryFinal.NumEligibleToVote.ShouldEqual(5);
      resultSummaryFinal.NumVoters.ShouldEqual(1);
      resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);


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


      var votes = new List<VoteInfo>
      {
        new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot1Guid},
        new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot2Guid},
        new VoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid(), BallotGuid = ballot3Guid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

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
      result1.TieBreakGroup.ShouldEqual(1);
      result1.TieBreakRequired.ShouldEqual(true);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(1);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual(1);
      result2.ForceShowInOther.ShouldEqual(true);
      result2.TieBreakRequired.ShouldEqual(true);

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(1);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual(1);
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
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = person1Guid, BallotGuid = ballot1Guid},
        new VoteInfo {PersonGuid = person1Guid, BallotGuid = ballot2Guid},
        new VoteInfo {PersonGuid = person1Guid, BallotGuid = ballot3Guid},
        new VoteInfo {PersonGuid = person2Guid, BallotGuid = ballot1Guid},
        new VoteInfo {PersonGuid = person2Guid, BallotGuid = ballot2Guid},
        new VoteInfo {PersonGuid = person2Guid, BallotGuid = ballot3Guid},
        new VoteInfo {PersonGuid = person3Guid, BallotGuid = ballot1Guid},
        new VoteInfo {PersonGuid = person3Guid, BallotGuid = ballot2Guid},
        new VoteInfo {PersonGuid = person3Guid, BallotGuid = ballot3Guid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

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
      result1.TieBreakGroup.ShouldEqual(1);
      result1.TieBreakRequired.ShouldEqual(false);
      result1.ForceShowInOther.ShouldEqual(false);

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(3);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Top);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual(1);
      result2.ForceShowInOther.ShouldEqual(false);
      result2.TieBreakRequired.ShouldEqual(false);

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(3);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Top);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual(1);
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
      var votes = new List<VoteInfo>
      {
        new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot1Guid},
        new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot2Guid},
        new VoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot3Guid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

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
      result1.TieBreakGroup.ShouldEqual(1);
      result1.TieBreakRequired = true;

      var result2 = results[1];
      result2.VoteCount.ShouldEqual(1);
      result2.Rank.ShouldEqual(2);
      result2.Section.ShouldEqual(ResultHelper.Section.Other);
      result2.IsTied.ShouldEqual(true);
      result2.TieBreakGroup.ShouldEqual(1);
      result2.TieBreakRequired = true;
      result2.ForceShowInOther = true;

      var result3 = results[2];
      result3.VoteCount.ShouldEqual(1);
      result3.Rank.ShouldEqual(3);
      result3.Section.ShouldEqual(ResultHelper.Section.Other);
      result3.IsTied.ShouldEqual(true);
      result3.TieBreakGroup.ShouldEqual(1);
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
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[2].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[2].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[3].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[3].PersonGuid, BallotGuid = ballots[3].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[4].PersonGuid, BallotGuid = ballots[4].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[5].PersonGuid, BallotGuid = ballots[4].BallotGuid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(5);
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
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired = true;
      results[1].ForceShowInOther = false;

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(true);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired = true;
      results[2].ForceShowInOther = false;

      results[3].IsTied.ShouldEqual(true);
      results[3].TieBreakGroup.ShouldEqual(2);
      results[3].CloseToPrev.ShouldEqual(true);
      results[3].CloseToNext.ShouldEqual(true);
      results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[3].TieBreakRequired = true;
      results[3].ForceShowInOther = false;

      results[4].IsTied.ShouldEqual(true);
      results[4].TieBreakGroup.ShouldEqual(2);
      results[4].CloseToPrev.ShouldEqual(true);
      results[4].CloseToNext.ShouldEqual(true);
      results[4].Section.ShouldEqual(ResultHelper.Section.Other);
      results[4].ForceShowInOther.ShouldEqual(true);
      results[4].TieBreakRequired = true;
      results[4].ForceShowInOther = true;

      results[5].IsTied.ShouldEqual(true);
      results[5].TieBreakGroup.ShouldEqual(2);
      results[5].CloseToPrev.ShouldEqual(true);
      results[5].CloseToNext.ShouldEqual(false);
      results[5].Section.ShouldEqual(ResultHelper.Section.Other);
      results[5].ForceShowInOther.ShouldEqual(true);
      results[5].TieBreakRequired = true;
      results[5].ForceShowInOther = true;
    }

    [TestMethod]
    public void ElectionTieSpanningTopExtraOther()
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
      };

      // results wanted:
      //  person 0 = 2 votes
      //  person 1 = 1
      // ---
      //  person 2 = 1
      //  person 3 = 1
      // --
      //  person 4 = 1
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[3].PersonGuid, BallotGuid = ballots[2].BallotGuid},
        new VoteInfo {PersonGuid = SamplePeople[4].PersonGuid, BallotGuid = ballots[2].BallotGuid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(3);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(0);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(4);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      results.Count.ShouldEqual(5);

      results[0].IsTied.ShouldEqual(false);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].TieBreakRequired.ShouldEqual(false);
      results[0].ForceShowInOther.ShouldEqual(false);

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired.ShouldEqual(true);
      results[1].ForceShowInOther.ShouldEqual(false);

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(true);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired.ShouldEqual(true);
      results[2].ForceShowInOther.ShouldEqual(false);

      results[3].IsTied.ShouldEqual(true);
      results[3].TieBreakGroup.ShouldEqual(1);
      results[3].CloseToPrev.ShouldEqual(true);
      results[3].CloseToNext.ShouldEqual(true);
      results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[3].TieBreakRequired.ShouldEqual(true);
      results[3].ForceShowInOther.ShouldEqual(false);

      results[4].IsTied.ShouldEqual(true);
      results[4].TieBreakGroup.ShouldEqual(1);
      results[4].CloseToPrev.ShouldEqual(true);
      results[4].CloseToNext.ShouldEqual(false);
      results[4].Section.ShouldEqual(ResultHelper.Section.Other);
      results[4].ForceShowInOther.ShouldEqual(true);
      results[4].TieBreakRequired.ShouldEqual(true);
      results[4].ForceShowInOther.ShouldEqual(true);
    }


    [TestMethod]
    public void ElectionTieWithTieBreakTiedInTopSection()
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
      };

      // test wanted:
      //  person 0 = 1  TieBreak: 1
      //  person 1 = 1            1
      // ---
      //  person 2 = 1            0
      // Ballot 0: 0,1
      // Ballot 1: 2,spoiled
      var votes = new List<VoteInfo>
      {
        new VoteInfo {VoteId=1, PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {VoteId=2,PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {VoteId=3,PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {VoteId=4,VoteIneligibleReasonGuid = Guid.NewGuid(), BallotGuid = ballots[1].BallotGuid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(2);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(1);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(2);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);


      results[0].IsTied.ShouldEqual(true);
      results[0].TieBreakGroup.ShouldEqual(1);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].TieBreakRequired.ShouldEqual(true);
      results[0].ForceShowInOther.ShouldEqual(false);

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired.ShouldEqual(true);
      results[1].ForceShowInOther.ShouldEqual(false);

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(false);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired.ShouldEqual(true);
      results[2].ForceShowInOther.ShouldEqual(false);


      // apply tie break counts
      results[0].TieBreakCount = 1;
      results[1].TieBreakCount = 1;
      results[2].TieBreakCount = 0;

      model.AnalyzeEverything();

      results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(2);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);
      resultTies[0].IsResolved.ShouldEqual(true);


      results[0].IsTied.ShouldEqual(true);
      results[0].TieBreakGroup.ShouldEqual(1);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].ForceShowInOther.ShouldEqual(false);
      results[0].TieBreakRequired.ShouldEqual(true);
      results[0].IsTieResolved.ShouldEqual(true);

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired.ShouldEqual(true);
      results[1].ForceShowInOther.ShouldEqual(false);
      results[1].IsTieResolved.ShouldEqual(true);

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(false);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired.ShouldEqual(true);
      results[2].ForceShowInOther.ShouldEqual(false);
      results[2].IsTieResolved.ShouldEqual(true);


    }

    [TestMethod]
    public void ElectionTieWithTieBreakTiedInExtraSection()
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
      };

      // test wanted:
      //  person 0 = 1  TieBreak: 2
      //  person 1 = 1            1
      // ---
      //  person 2 = 1            1
      // Ballot 0: 0,1
      // Ballot 1: 2,spoiled
      var votes = new List<VoteInfo>
      {
        new VoteInfo {VoteId=1, PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {VoteId=2,PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {VoteId=3,PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {VoteId=4,VoteIneligibleReasonGuid = Guid.NewGuid(), BallotGuid = ballots[1].BallotGuid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(2);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(1);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(2);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);


      results[0].IsTied.ShouldEqual(true);
      results[0].TieBreakGroup.ShouldEqual(1);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].TieBreakRequired.ShouldEqual(true);
      results[0].ForceShowInOther.ShouldEqual(false);

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired.ShouldEqual(true);
      results[1].ForceShowInOther.ShouldEqual(false);

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(false);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired.ShouldEqual(true);
      results[2].ForceShowInOther.ShouldEqual(false);


      // apply tie break counts
      results[0].TieBreakCount = 2;
      results[1].TieBreakCount = 1;
      results[2].TieBreakCount = 1;

      model.AnalyzeEverything();

      results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(3);

      resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(2);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      // not resolved
      resultTies[0].IsResolved.ShouldEqual(false);


      results[0].IsTied.ShouldEqual(true);
      results[0].TieBreakGroup.ShouldEqual(1);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].ForceShowInOther.ShouldEqual(false);
      results[0].TieBreakRequired.ShouldEqual(true);
      results[0].IsTieResolved.ShouldEqual(false);

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired.ShouldEqual(true);
      results[1].ForceShowInOther.ShouldEqual(false);
      results[1].IsTieResolved.ShouldEqual(false);

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(false);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired.ShouldEqual(true);
      results[2].ForceShowInOther.ShouldEqual(false);
      results[2].IsTieResolved.ShouldEqual(false);


    }


    // Not ready yet [TestMethod]
    public void ElectionTieWithTieBreakTiedInExtraSection2()
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
      };

      // test wanted:
      //  person 0 = 2  TieBreak: 
      //  person 1 = 1            2
      // ---
      //  person 2 = 1            1
      //  person 3 = 1            1

      // Ballot 0: 0,1
      // Ballot 1: 0,2
      // Ballot 2: 3,spoiled
      var votes = new List<VoteInfo>
      {
        new VoteInfo {VoteId=1,PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {VoteId=2,PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid},
        new VoteInfo {VoteId=3,PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {VoteId=4,PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[1].BallotGuid},
        new VoteInfo {VoteId=5,PersonGuid = SamplePeople[3].PersonGuid, BallotGuid = ballots[2].BallotGuid},
        new VoteInfo {VoteId=6,VoteIneligibleReasonGuid = Guid.NewGuid(), BallotGuid = ballots[2].BallotGuid},
      };
      foreach (var voteInfo in votes)
      {
        voteInfo.ElectionGuid = electionGuid;
        voteInfo.PersonCombinedInfo = voteInfo.PersonCombinedInfoInVote = "zz";
        voteInfo.BallotStatusCode = BallotStatusEnum.Ok;
        voteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
        voteInfo.PersonCanReceiveVotes = true;
      }

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(3);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(1);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(4);

      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      results[0].IsTied.ShouldEqual(false);
      //results[0].TieBreakGroup.ShouldEqual(1);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].TieBreakRequired.ShouldEqual(false);
      results[0].ForceShowInOther.ShouldEqual(false);

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired.ShouldEqual(true);
      results[1].ForceShowInOther.ShouldEqual(false);

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(true);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired.ShouldEqual(true);
      results[2].ForceShowInOther.ShouldEqual(false);

      results[3].IsTied.ShouldEqual(true);
      results[3].TieBreakGroup.ShouldEqual(1);
      results[3].CloseToPrev.ShouldEqual(true);
      results[3].CloseToNext.ShouldEqual(false);
      results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[3].TieBreakRequired.ShouldEqual(true);
      results[3].ForceShowInOther.ShouldEqual(false);


      // apply tie break counts
      results[1].TieBreakCount = 2;
      results[2].TieBreakCount = 1;
      results[3].TieBreakCount = 1;

      model.AnalyzeEverything();

      results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

      results.Count.ShouldEqual(4);

      resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      // not resolved
      resultTies[0].IsResolved.ShouldEqual(false);

      results[0].IsTied.ShouldEqual(false);
      //results[0].TieBreakGroup.ShouldEqual(1);
      results[0].CloseToPrev.ShouldEqual(false);
      results[0].CloseToNext.ShouldEqual(true);
      results[0].Section.ShouldEqual(ResultHelper.Section.Top);
      results[0].TieBreakRequired.ShouldEqual(false);
      results[0].ForceShowInOther.ShouldEqual(false);

      results[1].IsTied.ShouldEqual(true);
      results[1].TieBreakGroup.ShouldEqual(1);
      results[1].CloseToPrev.ShouldEqual(true);
      results[1].CloseToNext.ShouldEqual(true);
      results[1].Section.ShouldEqual(ResultHelper.Section.Top);
      results[1].TieBreakRequired.ShouldEqual(true);
      results[1].ForceShowInOther.ShouldEqual(false);

      results[2].IsTied.ShouldEqual(true);
      results[2].TieBreakGroup.ShouldEqual(1);
      results[2].CloseToPrev.ShouldEqual(true);
      results[2].CloseToNext.ShouldEqual(true);
      results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[2].TieBreakRequired.ShouldEqual(true);
      results[2].ForceShowInOther.ShouldEqual(false);

      results[3].IsTied.ShouldEqual(true);
      results[3].TieBreakGroup.ShouldEqual(1);
      results[3].CloseToPrev.ShouldEqual(true);
      results[3].CloseToNext.ShouldEqual(false);
      results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[3].TieBreakRequired.ShouldEqual(true);
      results[3].ForceShowInOther.ShouldEqual(false);



    }

  }

  static class Ext
  {

    public static Vote With(this Vote vote, Person person, Ballot ballot, int rowId, string status = VoteHelper.VoteStatusCode.Ok)
    {
      vote.PersonGuid = person.PersonGuid;
      vote.PersonCombinedInfo = person.CombinedInfo;
      vote.InvalidReasonGuid = person.IneligibleReasonGuid;
      vote.BallotGuid = ballot.BallotGuid;
      vote.StatusCode = status;
      vote.C_RowId = rowId;
      return vote;
    }

    public static Ballot With(this Ballot ballot, BallotStatusEnum status, Guid locationGuid, int rowId)
    {
      ballot.BallotGuid = Guid.NewGuid();
      ballot.C_RowId = rowId;
      ballot.StatusCode = status;
      ballot.LocationGuid = locationGuid;
      return ballot;
    }
    /// <summary>
    /// default to OK
    /// </summary>
    /// <param name="ballot"></param>
    /// <param name="rowId"></param>
    /// <returns></returns>
    public static Ballot With(this Ballot ballot, Guid locationGuid, int rowId)
    {
      return ballot.With(BallotStatusEnum.Ok, locationGuid, rowId);
    }
  }
}