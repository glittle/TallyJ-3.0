using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.EF;
using TallyJ.Models;
using TallyJ.Models.Helper;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class BallotAnalysisTests
  {
    private Fakes _fakes;

    [TestInitialize]
    public void Init()
    {
      _fakes = new Fakes();
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
      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotes_Test()
    {
      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotesWithBlank_Test()
    {
      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {InvalidReasonGuid = IneligibleReasonEnum.Unreadable_Vote_is_blank},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
      spoiledCount.ShouldEqual(1);
    }
    [TestMethod]
    public void TooManyNumberOfVotesWithIneligible_Test()
    {
      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {InvalidReasonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooFewNumberOfVotes_Test()
    {
      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooFew);
    }

    [TestMethod]
    public void EmptyNumberOfVotes_Test()
    {
      var votes = new List<Vote>
                    {
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Empty);
    }


    [TestMethod]
    public void TooFewNumberOfVotesWithBlank_Test()
    {
      var votes = new List<Vote>
                    {
                      new Vote {InvalidReasonGuid = IneligibleReasonEnum.Unreadable_Vote_is_blank},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooFew);
      spoiledCount.ShouldEqual(0);
    }


    [TestMethod]
    public void KeepReviewStatus_Test()
    {
      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount).ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);


      votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                    };

      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount).ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);
    }

    [TestMethod]
    public void HasDuplicates_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = dupPersonGuid},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = Guid.NewGuid()},
                      new Vote {PersonGuid = dupPersonGuid},
                    };

      var model = new BallotAnalyzer(5, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Dup);
    }

    [TestMethod]
    public void AllSpoiled_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<Vote>
                    {
                      new Vote {InvalidReasonGuid = Guid.NewGuid()},
                      new Vote {InvalidReasonGuid = Guid.NewGuid()},
                      new Vote {InvalidReasonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount).ShouldEqual(false);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
      spoiledCount.ShouldEqual(3);
    }

    [TestMethod]
    public void HasDuplicates2_KeepStatusCode_Test()
    {
      var votes = new List<Vote>
                    {
                      new Vote {PersonGuid = Guid.NewGuid()},
                    };


      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;

      // keep Review
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount).ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);
      spoiledCount.ShouldEqual(0);

      // override OK
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount).ShouldEqual(true);
      newStatus.ShouldEqual(BallotStatusEnum.TooFew);
      spoiledCount.ShouldEqual(0);
    }

    internal class Fakes
    {
      private int _saveChanges;

      public int CountOfCallsToSaveChanges
      {
        get { return _saveChanges; }
        set { _saveChanges = value; }
      }

      public int SaveChanges()
      {
        _saveChanges++;

        return 0;
      }
    }

  }
}