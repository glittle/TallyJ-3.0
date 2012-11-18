using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;

using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;
using Tests.Support;
using TallyJ.Code;
using System.Linq;

namespace Tests.BusinessTests
{
  [TestClass]
  public class VoteHelperTests
  {
    [TestMethod]
    public void ForBallot_Everyone_Test()
    {
      var vh = new VoteHelper(true, true);

      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(null);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);

      var alreadyIneligibleGuid = Guid.NewGuid();

      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);

      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
    }

    [TestMethod]
    public void ForListing_Everyone_Test()
    {
      var vh = new VoteHelper(false, true);

      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(null);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);

      var alreadyIneligibleGuid = Guid.NewGuid();

      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);

      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
    }

    [TestMethod]
    public void ForBallot_TieBreak_Test()
    {
      var vh = new VoteHelper(true, false);

      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(IneligibleReasonEnum.Ineligible_Not_in_TieBreak);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(IneligibleReasonEnum.Ineligible_Not_in_TieBreak);

      var alreadyIneligibleGuid = Guid.NewGuid();

      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);

      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(IneligibleReasonEnum.Ineligible_Not_in_TieBreak);
    }

    [TestMethod]
    public void ForEveryone_TieBreak_Test()
    {
      var vh = new VoteHelper(false, true);

      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(null);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);

      var alreadyIneligibleGuid = Guid.NewGuid();

      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);

      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
    }

  }

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
      var voteInfos = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, voteInfos, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotes_Test()
    {
      var voteInfos = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, voteInfos, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotesWithBlank_Test()
    {
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteIneligibleReasonGuid = IneligibleReasonEnum.Unreadable_Vote_is_blank},
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
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotesWithSpoiled_Test()
    {
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
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
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
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
      var votes = new List<vVoteInfo>
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
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {VoteIneligibleReasonGuid = IneligibleReasonEnum.Unreadable_Vote_is_blank},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
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
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(3, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount).ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);


      votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount).ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);
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

      var model = new BallotAnalyzer(5, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Dup);
    }

    [TestMethod]
    public void HasDuplicates_and_Too_Many_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                      new vVoteInfo {PersonGuid = dupPersonGuid},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
                    };

      var model = new BallotAnalyzer(5, _fakes.SaveChanges, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
    }

    [TestMethod]
    public void AllSpoiled_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonIneligibleReasonGuid = Guid.NewGuid()},
                      new vVoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
                      new vVoteInfo {PersonIneligibleReasonGuid = Guid.NewGuid()},
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
      var votes = new List<vVoteInfo>
                    {
                      new vVoteInfo {PersonGuid = Guid.NewGuid()},
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