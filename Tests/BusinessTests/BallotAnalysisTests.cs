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
      var election = new Election { NumberToElect = 3 };
      var ballot = new vBallotInfo {StatusCode = null};
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzerNormal(election, SamplePeople, _fakes.SaveChanges);

      model.Analyze(ballot, votes);

      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.Ok);
    }

    [TestMethod]
    public void TooManyNumberOfVotes_Test()
    {
      var election = new Election { NumberToElect = 3 };
      var ballot = new vBallotInfo {StatusCode = null};
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzerNormal(election, SamplePeople, _fakes.SaveChanges);

      model.Analyze(ballot, votes);

      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.TooMany);
    }

    [TestMethod]
    public void TooFewNumberOfVotes_Test()
    {
      var election = new Election { NumberToElect = 3 };
      var ballot = new vBallotInfo {StatusCode = null};
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzerNormal(election, SamplePeople, _fakes.SaveChanges);

      model.Analyze(ballot, votes);

      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.TooFew);
    }


    [TestMethod]
    public void KeepReviewStatus_Test()
    {
      var election = new Election { NumberToElect = 3 };
      var ballot = new vBallotInfo {StatusCode = BallotHelper.BallotStatusCode.Review};
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzerNormal(election, SamplePeople, _fakes.SaveChanges);

      model.Analyze(ballot, votes);

      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.Review);

      votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      model.Analyze(ballot, votes);

      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.Review);

    }

    [TestMethod]
    public void HasDuplicates_Test()
    {
      var election = new Election
      {
        NumberToElect = 5,
      };

      var ballot = new vBallotInfo {StatusCode = BallotHelper.BallotStatusCode.Ok};

      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                    };

      var model = new BallotAnalyzerNormal(election, SamplePeople, _fakes.SaveChanges);

      model.Analyze(ballot, votes);

      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.HasDup);
    }

    [TestMethod]
    public void HasDuplicates1_DupOverrulesInEdit_Test()
    {
      var election = new Election
      {
        NumberToElect = 3,
      };

      var ballot = new vBallotInfo();

      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                    };

      var model = new BallotAnalyzerNormal(election, SamplePeople, _fakes.SaveChanges);


      ballot.StatusCode = BallotHelper.BallotStatusCode.InEdit;
      model.Analyze(ballot, votes);
      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.HasDup);
    }

    [TestMethod]
    public void HasDuplicates2_KeepStatusCode_Test()
    {
      var election = new Election
      {
        NumberToElect = 3,
      };

      var ballot = new vBallotInfo();

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzerNormal(election, SamplePeople, _fakes.SaveChanges);

      // keep InEdit
      ballot.StatusCode = BallotHelper.BallotStatusCode.InEdit;
      model.Analyze(ballot, votes);
      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.InEdit);

      // keep Review
      ballot.StatusCode = BallotHelper.BallotStatusCode.Review;
      model.Analyze(ballot, votes);
      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.Review);

      // override OK
      ballot.StatusCode = BallotHelper.BallotStatusCode.Ok;
      model.Analyze(ballot, votes);
      ballot.StatusCode.ShouldEqual(BallotHelper.BallotStatusCode.TooFew);
    }

  }
}