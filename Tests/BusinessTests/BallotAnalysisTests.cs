using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.EF;
using TallyJ.Models;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class BallotAnalysisTests
  {
    private ResultAnalysisTests.Fakes _fakes;

    [TestInitialize]
    public void Init()
    {
      _fakes = new ResultAnalysisTests.Fakes();
    }

    private List<Person> SamplePeople
    {
      get
      {
        return new List<Person>
                 {
                   new Person{ PersonGuid=Guid.NewGuid()},
                   new Person{ PersonGuid=Guid.NewGuid()},
                   new Person{ PersonGuid=Guid.NewGuid()},
                   new Person{ PersonGuid=Guid.NewGuid()},
                   new Person{ PersonGuid=Guid.NewGuid()},
                 };
      }
    }

    [TestMethod]
    public void CorrectNumberOfVotes_Test()
    {
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3);

      string newStatus;
      model.DetermineStatus(null, votes, out newStatus).ShouldEqual(true);

      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.Ok);
    }

    [TestMethod]
    public void TooManyNumberOfVotes_Test()
    {
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3);

      string newStatus;
      model.DetermineStatus(null, votes, out newStatus).ShouldEqual(true);

      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.TooMany);
    }

    [TestMethod]
    public void TooFewNumberOfVotes_Test()
    {
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3);

      string newStatus;
      model.DetermineStatus(null, votes, out newStatus).ShouldEqual(true);

      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.TooFew);
    }


    [TestMethod]
    public void KeepReviewStatus_Test()
    {
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3);

      string newStatus;
      model.DetermineStatus(BallotHelper.BallotStatusCode.Review, votes, out newStatus).ShouldEqual(false);
      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.Review);


      votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      model.DetermineStatus(BallotHelper.BallotStatusCode.Review, votes, out newStatus).ShouldEqual(false);
      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.Review);
    }

    [TestMethod]
    public void HasDuplicates_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                    };

      var model = new BallotAnalyzer(5);

      string newStatus;
      model.DetermineStatus(BallotHelper.BallotStatusCode.Ok, votes, out newStatus).ShouldEqual(true);

      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.Dup);
    }

    [TestMethod]
    public void AllSpoiled_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {VoteInvalidReasonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteInvalidReasonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteInvalidReasonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3);

      string newStatus;
      model.DetermineStatus(BallotHelper.BallotStatusCode.Ok, votes, out newStatus).ShouldEqual(false);

      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.Ok);
    }

    [TestMethod]
    public void HasDuplicates2_KeepStatusCode_Test()
    {
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };


      var model = new BallotAnalyzer(3);

      string newStatus;

      // keep Review
      model.DetermineStatus(BallotHelper.BallotStatusCode.Review, votes, out newStatus).ShouldEqual(false);
      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.Review);

      // override OK
      model.DetermineStatus(BallotHelper.BallotStatusCode.Ok, votes, out newStatus).ShouldEqual(true);
      newStatus.ShouldEqual(BallotHelper.BallotStatusCode.TooFew);
    }

  }
}