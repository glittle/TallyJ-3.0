using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;
using Tests.Support;

namespace Tests.BusinessTests;

[TestClass]
public class AnalyzerSingleNameTests
{
  private Guid _electionGuid;
  private AnalyzerFakes _fakes;
  private ITallyJDbContext Db;
  private List<Person> SamplePeople { get; set; }

  [TestInitialize]
  public void Init()
  {
    UnityInstance.Reset();
    ElectionTestHelper.Reset();

    _fakes = new AnalyzerFakes();
    Db = _fakes.DbContext;
    Db.ForTests();
    UnityInstance.Offer(Db);

    _electionGuid = Guid.NewGuid();

    // SessionKey.CurrentPeopleElectionGuid.SetInSession(_electionGuid);
    // SessionKey.CurrentElectionGuid.SetInSession(_electionGuid);
    // ElectionTestHelper.SaveElectionGuidForTests(_electionGuid);

    SessionKey.CurrentComputer.SetInSession(new Computer
    {
      ComputerGuid = Guid.NewGuid(),
      ComputerCode = "TEST",
      ElectionGuid = _electionGuid
    });
  }

  public void AfterElectionCreated()
  {
    UserSession.CurrentElectionGuid = ElectionTestHelper.ElectionGuid;

    SamplePeople =
    [
      // 0 - 7
      new Person { FirstName = "x0", CombinedInfo = "person", VotingMethod = VotingMethodEnum.InPerson }.ForTests(),
      new Person { FirstName = "x1", CombinedInfo = "person" }.ForTests(),
      new Person { FirstName = "x2", CombinedInfo = "person" }.ForTests(),
      new Person { FirstName = "x3", CombinedInfo = "person" }.ForTests(),
      new Person { FirstName = "x4", CombinedInfo = "person" }.ForTests(),
      new Person { FirstName = "x5", CombinedInfo = "person" }.ForTests(),
      new Person { FirstName = "x6", CombinedInfo = "person" }.ForTests(),
      new Person { FirstName = "x7", CombinedInfo = "person" }.ForTests()
    ];
  }

  [TestMethod]
  public void Election_3_people()
  {
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests(),
      new Ballot().ForTests(),
      new Ballot().ForTests()
    };

    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 33 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[1], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 2 }.ForTests(ballots[2], SamplePeople[1])
    };

    var model = new ElectionAnalyzerSingleName(_fakes); //, election, votes, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();

    results.Count.ShouldEqual(2);

    var result1 = results[0];
    result1.VoteCount.ShouldEqual(38);
    result1.Rank.ShouldEqual(1);
    result1.Section.ShouldEqual(ResultHelper.Section.Top);
    result1.IsTied.ShouldEqual(false);

    var result2 = results[1];
    result2.VoteCount.ShouldEqual(2);
    result2.Rank.ShouldEqual(2);
    result2.Section.ShouldEqual(ResultHelper.Section.Other);
    result2.IsTied.ShouldEqual(false);
  }

  [TestMethod]
  public void Election_3_people_With_Manual_Results()
  {
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests(),
      new Ballot().ForTests(),
      new Ballot().ForTests()
    };

    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 33 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[1], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 2 }.ForTests(ballots[2], SamplePeople[1])
    };

    new ResultSummary
    {
      ResultType = ResultType.Manual,
      SpoiledManualBallots = 1 // override the real count
    }.ForTests();

    var model = new ElectionAnalyzerSingleName(_fakes); //, election, votes, ballots, SamplePeople);

    model.AnalyzeEverything();

    var resultSummaryFinal = model.ResultSummaryFinal;
    resultSummaryFinal.BallotsNeedingReview.ShouldEqual(0);
    resultSummaryFinal.NumBallotsWithManual.ShouldEqual(41);

    resultSummaryFinal.DroppedOffBallots.ShouldEqual(0);
    resultSummaryFinal.InPersonBallots.ShouldEqual(1);
    resultSummaryFinal.MailedInBallots.ShouldEqual(0);
    resultSummaryFinal.CalledInBallots.ShouldEqual(0);
    resultSummaryFinal.OnlineBallots.ShouldEqual(0);
    resultSummaryFinal.NumEligibleToVote.ShouldEqual(8);
    resultSummaryFinal.NumVoters.ShouldEqual(40);
    resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);


    var results = model.Results.OrderBy(r => r.Rank).ToList();

    results.Count.ShouldEqual(2);

    var result1 = results[0];
    result1.VoteCount.ShouldEqual(38);
    result1.Rank.ShouldEqual(1);
    result1.Section.ShouldEqual(ResultHelper.Section.Top);
    result1.IsTied.ShouldEqual(false);

    var result2 = results[1];
    result2.VoteCount.ShouldEqual(2);
    result2.Rank.ShouldEqual(2);
    result2.Section.ShouldEqual(ResultHelper.Section.Other);
    result2.IsTied.ShouldEqual(false);
  }

  [TestMethod]
  public void Election_3_people_with_Tie()
  {
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests(),
      new Ballot().ForTests(),
      new Ballot().ForTests()
    };

    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 10 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 10 }.ForTests(ballots[1], SamplePeople[1]),
      new Vote { SingleNameElectionCount = 2 }.ForTests(ballots[2], SamplePeople[2])
    };

    var model = new ElectionAnalyzerSingleName(_fakes); // election, votes, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();
    var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

    resultTies.Count.ShouldEqual(1);
    resultTies[0].NumToElect.ShouldEqual(1);
    resultTies[0].NumInTie.ShouldEqual(2);
    resultTies[0].TieBreakRequired.ShouldEqual(true);

    results.Count.ShouldEqual(3);

    var result1 = results[0];
    result1.VoteCount.ShouldEqual(10);
    result1.Rank.ShouldEqual(1);
    result1.Section.ShouldEqual(ResultHelper.Section.Top);
    result1.IsTied.ShouldEqual(true);
    result1.TieBreakGroup.ShouldEqual(1);
    result1.TieBreakRequired.ShouldEqual(true);

    var result2 = results[1];
    result2.VoteCount.ShouldEqual(10);
    result2.Rank.ShouldEqual(2);
    result2.Section.ShouldEqual(ResultHelper.Section.Other);
    result2.IsTied.ShouldEqual(true);
    result2.TieBreakGroup.ShouldEqual(1);
    result2.ForceShowInOther.ShouldEqual(true);
    result2.TieBreakRequired.ShouldEqual(true);

    var result3 = results[2];
    result3.VoteCount.ShouldEqual(2);
    result3.Rank.ShouldEqual(3);
    result3.Section.ShouldEqual(ResultHelper.Section.Other);
    result3.IsTied.ShouldEqual(false);
    result3.TieBreakGroup.ShouldEqual(null);
    result3.ForceShowInOther.ShouldEqual(false);
    result3.TieBreakRequired.ShouldEqual(false);
  }


  [TestMethod]
  public void SingleNameElection_1_person()
  {
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests()
    };

    var votes = new[]
    {
      // all for one person in this test
      new Vote { SingleNameElectionCount = 33 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 2 }.ForTests(ballots[0], SamplePeople[0])
    };

    var model = new ElectionAnalyzerSingleName(_fakes); //election, votes, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();

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
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests(),
      new Ballot().ForTests(),
      new Ballot().ForTests(BallotStatusEnum.TooFew)
    };

    // TODO 2012-03-24 Glen Little: Needs attention... these test are for normal elections, not single name...
    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 33 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[0], SamplePeople[1]),
      new Vote { SingleNameElectionCount = 2 }.ForTests(ballots[0], SamplePeople[2]),
      new Vote { SingleNameElectionCount = 4 }.ForTests(ballots[1], SamplePeople[3]),
      new Vote { SingleNameElectionCount = 27 }.ForTests(ballots[0], SamplePeople[4]),
      new Vote { SingleNameElectionCount = 27 }.ForTests(ballots[0], SamplePeople[5]), // 5 - will require review
      new Vote { SingleNameElectionCount = 27 }.ForTests(ballots[0],
        new Person { FirstName = "z1", IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other }.ForTests())
    };
    votes[5].PersonCombinedInfo = "different"; // these will be invalid

    //votes[6].PersonIneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other;
    //votes[6].PersonCanReceiveVotes = IneligibleReasonEnum.Ineligible_Other.CanReceiveVotes;

    var model = new ElectionAnalyzerSingleName(_fakes); //election, voteinfos, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();

    ballots[0].StatusCode.ShouldEqual(BallotStatusEnum.Verify);
    ballots[1].StatusCode.ShouldEqual(BallotStatusEnum.Ok);

    var summary = model.ResultSummaryFinal;
    summary.SpoiledBallots.ShouldEqual(1);
    summary.BallotsNeedingReview.ShouldEqual(1);
    summary.SpoiledVotes.ShouldEqual(0); // all are on the spoiled ballot

    results.Count.ShouldEqual(5);
  }


  [TestMethod]
  public void Invalid_People_Do_Not_Affect_Results()
  {
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests(),
      new Ballot().ForTests(),
      new Ballot().ForTests()
    };

    SamplePeople[5].IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Deceased;
    new PeopleModel().ApplyVoteReasonFlags(SamplePeople[5]);

    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 33 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[0], SamplePeople[1]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[0], SamplePeople[2]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[0], SamplePeople[3]),

      new Vote { SingleNameElectionCount = 27 }.ForTests(ballots[1], SamplePeople[5]), // spoiled person
      new Vote { SingleNameElectionCount = 27 }.ForTests(ballots[1],
        new Person { FirstName = "z2", IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other }
          .ForTests()), // spoiled

      new Vote { SingleNameElectionCount = 27 }.ForTests(ballots[2], SamplePeople[4]) // #6 - will be Needs Review
    };

    votes[6].PersonCombinedInfo = "different"; // force a Review

    var model = new ElectionAnalyzerSingleName(_fakes); //election, voteInfos, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();
    var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

    resultTies.Count.ShouldEqual(1);
    resultTies[0].TieBreakRequired.ShouldEqual(false);
    resultTies[0].NumToElect.ShouldEqual(0);
    resultTies[0].NumInTie.ShouldEqual(3);


    var summary = model.ResultSummaryFinal;
    summary.BallotsNeedingReview.ShouldEqual(1);
    summary.SpoiledBallots.ShouldEqual(1);
    summary.SpoiledVotes.ShouldEqual(27 + 27);
    summary.SumOfEnvelopesCollected.ShouldEqual(1); // sample data has 1 voting method
    summary.BallotsReceived.ShouldEqual(33 + 5 + 5 + 5 + 27 + 27 + 27);
    summary.NumBallotsWithManual.ShouldEqual(1 + 33 + 5 + 5 + 5 + 27 + 27 + 27);

    results.Count.ShouldEqual(4);

    var result1 = results[0];
    result1.VoteCount.ShouldEqual(33);
    result1.Section.ShouldEqual(ResultHelper.Section.Top);
    result1.IsTied.ShouldEqual(false);
    result1.TieBreakRequired = false;

    var result2 = results[1];
    result2.VoteCount.ShouldEqual(5);
    result2.Section.ShouldEqual(ResultHelper.Section.Other);
    result2.IsTied.ShouldEqual(true);
    result2.TieBreakRequired = false;
    result2.ForceShowInOther = false;

    var result3 = results[2];
    result3.VoteCount.ShouldEqual(5);
    result3.Section.ShouldEqual(ResultHelper.Section.Other);
    result3.IsTied.ShouldEqual(true);
    result3.TieBreakRequired = false;
    result3.ForceShowInOther = false;
  }


  [TestMethod]
  public void SingleNameElection_3_people()
  {
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests()
    };
    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 33 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 5 }.ForTests(ballots[0], SamplePeople[1]),
      new Vote { SingleNameElectionCount = 2 }.ForTests(ballots[0], SamplePeople[2])
    };

    var model = new ElectionAnalyzerSingleName(_fakes); //election, votes, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();

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
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests()
    };
    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 10 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 10 }.ForTests(ballots[0], SamplePeople[1]),
      new Vote { SingleNameElectionCount = 2 }.ForTests(ballots[0], SamplePeople[2])
    };

    var model = new ElectionAnalyzerSingleName(_fakes); //election, votes, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();

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
    new Election
    {
      NumberToElect = 1,
      NumberExtra = 0
    }.ForTests();

    AfterElectionCreated();

    var ballots = new[]
    {
      new Ballot().ForTests()
    };
    var votes = new[]
    {
      new Vote { SingleNameElectionCount = 10 }.ForTests(ballots[0], SamplePeople[0]),
      new Vote { SingleNameElectionCount = 10 }.ForTests(ballots[0], SamplePeople[1]),
      new Vote { SingleNameElectionCount = 10 }.ForTests(ballots[0], SamplePeople[2])
    };

    var model = new ElectionAnalyzerSingleName(_fakes); //election, votes, ballots, SamplePeople);

    model.AnalyzeEverything();

    var results = model.Results.OrderBy(r => r.Rank).ToList();

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