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

    private List<Person> SamplePeople
    {
      get { return _samplePeople; }
    }

    [TestInitialize]
    public void Init()
    {
      _fakes = new AnalyzerFakes();
      Db = _fakes.DbContext;
      Db.ForTests();
      UnityInstance.Offer(Db);

      _electionGuid = Guid.NewGuid();
      SessionKey.CurrentElectionGuid.SetInSession(_electionGuid);
      BallotTestHelper.SaveElectionGuidForTests(_electionGuid);

      SessionKey.CurrentComputer.SetInSession(new Computer
      {
        ComputerGuid = Guid.NewGuid(),
        ComputerCode = "TEST",
        ElectionGuid = _electionGuid
      });

      _samplePeople = new List<Person>
      {
        new Person {VotingMethod = VotingMethodEnum.InPerson}.ForTests(),
        new Person {}.ForTests(),
        new Person {}.ForTests(),
        new Person {}.ForTests(),
        new Person {}.ForTests(),
        new Person {IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Moved_elsewhere_recently}.ForTests(),
      };
    }

    [TestMethod]
    public void Ballot_TwoPeople()
    {
      new Election
      {
        NumberToElect = 2,
        NumberExtra = 0,
        CanReceive = ElectionModel.CanVoteOrReceive.All
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests()
      };

      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0], ballots[0]),
        new Vote().ForTests(SamplePeople[1], ballots[0])
      };

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
      new Election
      {
        NumberToElect = 2,
        NumberExtra = 0,
        CanReceive = ElectionModel.CanVoteOrReceive.All
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests()
      };

      var votes = new[] {
        new Vote().ForTests(SamplePeople[0], ballots[0]),
        new Vote().ForTests(SamplePeople[1], ballots[0]),
      };
      votes[0].PersonCombinedInfo = "yy";

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
      new Election
      {
        NumberToElect = 2,
        NumberExtra = 0,
        CanReceive = ElectionModel.CanVoteOrReceive.All
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests()
      };

      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0], ballots[0]).Status(IneligibleReasonEnum.Unidentifiable_Unknown_person),
        new Vote().ForTests(SamplePeople[1], ballots[0]).Status(IneligibleReasonEnum.Unidentifiable_Unknown_person)
      };

      var model = new ElectionAnalyzerNormal(_fakes);//, election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderBy(r => r.Rank).ToList();
      results.Count.ShouldEqual(0);

      ballots[0].StatusCode.ShouldEqual(BallotStatusEnum.Ok);
      votes[0].StatusCode.ShouldEqual(VoteHelper.VoteStatusCode.Spoiled);
      votes[1].StatusCode.ShouldEqual(VoteHelper.VoteStatusCode.Spoiled);

      //var spoiledCount = votes.Count(v => v.InvalidReasonGuid.HasValue || v.PersonIneligibleReasonGuid.HasValue || v.PersonCombinedInfo != v.PersonCombinedInfoInVote);
      //spoiledCount.ShouldEqual(2);

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
    public void Election_3_people_with_Tie_Not_Required()
    {
      new Election
      {
        NumberToElect = 3,
        NumberExtra = 0
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests(),
        new Ballot().ForTests(),
        new Ballot().ForTests(),
      };

      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0], ballots[0]),
        new Vote().ForTests(SamplePeople[0], ballots[1]),
        new Vote().ForTests(SamplePeople[0], ballots[2]),
        new Vote().ForTests(SamplePeople[1], ballots[0]),
        new Vote().ForTests(SamplePeople[1], ballots[1]),
        new Vote().ForTests(SamplePeople[1], ballots[2]),
        new Vote().ForTests(SamplePeople[2], ballots[0]),
        new Vote().ForTests(SamplePeople[2], ballots[1]),
        new Vote().ForTests(SamplePeople[2], ballots[2]),
      };

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderBy(r => r.Rank).ToList();
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
      new Election
      {
        NumberToElect = 1,
        NumberExtra = 0
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests(),
        new Ballot().ForTests(),
        new Ballot().ForTests(),
      };

      var votes = new[]
      {
        new Vote{SingleNameElectionCount = 10 }.ForTests(SamplePeople[0], ballots[0]),
        new Vote{SingleNameElectionCount = 10 }.ForTests(SamplePeople[1], ballots[1]),
        new Vote{SingleNameElectionCount = 10 }.ForTests(SamplePeople[2], ballots[2]),
      };

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var results = model.Results.OrderBy(r => r.Rank).ToList();

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
      new Election
      {
        NumberToElect = 2,
        NumberExtra = 2
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests(),
        new Ballot().ForTests(),
        new Ballot().ForTests(),
        new Ballot().ForTests(),
        new Ballot().ForTests(),
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
      SamplePeople[5].IneligibleReasonGuid = null;
      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0], ballots[0]),
        new Vote().ForTests(SamplePeople[1], ballots[0]),
        new Vote().ForTests(SamplePeople[0], ballots[1]),
        new Vote().ForTests(SamplePeople[1], ballots[1]),
        new Vote().ForTests(SamplePeople[0], ballots[2]),
        new Vote().ForTests(SamplePeople[2], ballots[2]),
        new Vote().ForTests(SamplePeople[2], ballots[3]),
        new Vote().ForTests(SamplePeople[3], ballots[3]),
        new Vote().ForTests(SamplePeople[4], ballots[4]),
        new Vote().ForTests(SamplePeople[5], ballots[4]),
      };

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(5);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(0);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderBy(r => r.Rank).ToList();
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
      new Election
      {
        ElectionGuid = _electionGuid,
        NumberToElect = 2,
        NumberExtra = 2
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests(),
        new Ballot().ForTests(),
        new Ballot().ForTests(),
      };

      // results wanted:
      //  person 0 = 2 votes
      //  person 1 = 1
      // ---
      //  person 2 = 1
      //  person 3 = 1
      // --
      //  person 4 = 1
      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0], ballots[0]),
        new Vote().ForTests(SamplePeople[1], ballots[0]),
        new Vote().ForTests(SamplePeople[0], ballots[1]),
        new Vote().ForTests(SamplePeople[2], ballots[1]),
        new Vote().ForTests(SamplePeople[3], ballots[2]),
        new Vote().ForTests(SamplePeople[4], ballots[2]),
      };

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(3);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(0);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderBy(r => r.Rank).ToList();
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
      new Election
      {
        NumberToElect = 2,
        NumberExtra = 2
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests(),
        new Ballot().ForTests(),
      };

      // test wanted:
      //  person 0 = 1  TieBreak: 1
      //  person 1 = 1            1
      // ---
      //  person 2 = 1            0
      // Ballot 0: 0,1
      // Ballot 1: 2,spoiled
      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0], ballots[0]),
        new Vote().ForTests(SamplePeople[1], ballots[0]),
        new Vote().ForTests(SamplePeople[2], ballots[1]),
        new Vote().ForTests(IneligibleReasonEnum.Unidentifiable_Unknown_person, ballots[1]),
      };

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(2);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(1);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderBy(r => r.Rank).ToList();

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

      results = model.Results.OrderBy(r => r.Rank).ToList();

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
      new Election
      {
        NumberToElect = 2,
        NumberExtra = 2
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests(),
        new Ballot().ForTests(),
      };

      // test wanted:
      //  person 0 = 1  TieBreak: 2
      //  person 1 = 1            1
      // ---extra
      //  person 2 = 1            1
      //--> not resolved
      
      // Ballot 0: 0,1
      // Ballot 1: 2,spoiled
      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0], ballots[0]),
        new Vote().ForTests(SamplePeople[1], ballots[0]),
        new Vote().ForTests(SamplePeople[2], ballots[1]),
        new Vote().ForTests(IneligibleReasonEnum.Unidentifiable_Unknown_person, ballots[1]),
      };

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(2);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(1);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderBy(r => r.Rank).ToList();

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

      results = model.Results.OrderBy(r => r.Rank).ToList();

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
      results[0].IsTieResolved.ShouldEqual(true);

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


    [TestMethod]
    public void ElectionTieWithTieBreakTiedInExtraSection2()
    {
      new Election
      {
        NumberToElect = 2,
        NumberExtra = 2
      }.ForTests();

      var ballots = new[]
      {
        new Ballot().ForTests(),
        new Ballot().ForTests(),
        new Ballot().ForTests(),
      };

      // test wanted:
      //  person 0 = 2  TieBreak: 
      //  person 1 = 1            2
      //  ---Extra
      //  person 2 = 1            1
      //  person 3 = 1            1
      //  ---
      //ballots:
      //  Ballot 0: 0,1
      //  Ballot 1: 0,2
      //  Ballot 2: 3,spoiled
      var votes = new[]
      {
        new Vote().ForTests(SamplePeople[0],ballots[0]),
        new Vote().ForTests(SamplePeople[1],ballots[0]),
        new Vote().ForTests(SamplePeople[0],ballots[1]),
        new Vote().ForTests(SamplePeople[2],ballots[1]),
        new Vote().ForTests(SamplePeople[3],ballots[2]),
        new Vote().ForTests(IneligibleReasonEnum.Ineligible_Other,ballots[2])
      };

      var model = new ElectionAnalyzerNormal(_fakes); // election, votes, ballots, SamplePeople);

      model.AnalyzeEverything();

      var summary = model.ResultSummaryFinal;
      summary.BallotsReceived.ShouldEqual(3);
      summary.SpoiledBallots.ShouldEqual(0);
      summary.SpoiledVotes.ShouldEqual(1);
      summary.BallotsNeedingReview.ShouldEqual(0);

      var results = model.Results.OrderBy(r => r.Rank).ToList();

      results.Count.ShouldEqual(4);

      var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

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
      results[3].CloseToNext.ShouldEqual(false);
      results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[3].TieBreakRequired.ShouldEqual(true);
      results[3].ForceShowInOther.ShouldEqual(false);


      // apply tie break counts
      results[1].TieBreakCount = 2;
      results[2].TieBreakCount = 1;
      results[3].TieBreakCount = 1;

      model.AnalyzeEverything();

      results = model.Results.OrderBy(r => r.Rank).ToList();

      results.Count.ShouldEqual(4);

      resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

      resultTies.Count.ShouldEqual(1);
      resultTies[0].NumToElect.ShouldEqual(1);
      resultTies[0].NumInTie.ShouldEqual(3);
      resultTies[0].TieBreakRequired.ShouldEqual(true);

      // not resolved
      resultTies[0].IsResolved.ShouldEqual(false);

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
      results[3].CloseToNext.ShouldEqual(false);
      results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
      results[3].TieBreakRequired.ShouldEqual(true);
      results[3].ForceShowInOther.ShouldEqual(false);
    }

  }

}