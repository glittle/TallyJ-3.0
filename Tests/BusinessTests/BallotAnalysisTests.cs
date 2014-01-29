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
  public class VoteHelperTests
  {
//    [TestMethod]
//    public void ForBallot_Everyone_Test()
//    {
//      var vh = new VoteHelper(true, true);
//
//      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(null);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
//
//      var alreadyIneligibleGuid = Guid.NewGuid();
//
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);
//
//      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
//    }
//
//    [TestMethod]
//    public void ForListing_Everyone_Test()
//    {
//      var vh = new VoteHelper(false, true);
//
//      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(null);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
//
//      var alreadyIneligibleGuid = Guid.NewGuid();
//
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);
//
//      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
//    }
//
//    [TestMethod]
//    public void ForBallot_TieBreak_Test()
//    {
//      var vh = new VoteHelper(true, false);
//
//      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(IneligibleReasonEnum.IneligiblePartial1_Not_in_TieBreak);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(IneligibleReasonEnum.IneligiblePartial1_Not_in_TieBreak);
//
//      var alreadyIneligibleGuid = Guid.NewGuid();
//
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);
//
//      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(IneligibleReasonEnum.IneligiblePartial1_Not_in_TieBreak);
//    }

//    [TestMethod]
//    public void ForEveryone_TieBreak_Test()
//    {
//      var vh = new VoteHelper(false, true);
//
//      vh.IneligibleToReceiveVotes(null, null).ShouldEqual(null);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
//      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
//
//      var alreadyIneligibleGuid = Guid.NewGuid();
//
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, true).ShouldEqual(alreadyIneligibleGuid);
//      vh.IneligibleToReceiveVotes(alreadyIneligibleGuid, false).ShouldEqual(alreadyIneligibleGuid);
//
//      vh.IneligibleToReceiveVotes(Guid.Empty, true).ShouldEqual(Guid.Empty);
//      vh.IneligibleToReceiveVotes(Guid.Empty, false).ShouldEqual(Guid.Empty);
//    }
  }

  [TestClass]
  public class BallotAnalysisTests
  {
    //        private Fakes _fakes;

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
      //            _fakes = new Fakes();
    }

    [TestMethod]
    public void CorrectNumberOfVotes_Test()
    {
      var voteInfos = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, voteInfos, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotes_Test()
    {
      var voteInfos = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, voteInfos, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotesWithBlank_Test()
    {
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
//                      new VoteInfo {VoteIneligibleReasonGuid = IneligibleReasonEnum.Unreadable_Vote_is_blank},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotesWithIneligible_Test()
    {
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooManyNumberOfVotesWithSpoiled_Test()
    {
      var votes = new List<VoteInfo>
      {
        new VoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
        new VoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
        new VoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
        new VoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
      spoiledCount.ShouldEqual(0);
    }

    [TestMethod]
    public void TooFewNumberOfVotes_Test()
    {
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooFew);
    }

    [TestMethod]
    public void SingleIneligible_Test()
    {
      var votes = new List<VoteInfo>
      {
        new VoteInfo
        {
          VoteIneligibleReasonGuid = IneligibleReasonEnum.Unreadable_Not_a_complete_name,
          SingleNameElectionCount = 4
        },
      };

      var model = new BallotAnalyzer(1, true);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
    }

    [TestMethod]
    public void EmptyNumberOfVotes_Test()
    {
      var votes = new List<VoteInfo>
      {
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Empty);
    }


    [TestMethod]
    public void TooFewNumberOfVotesWithBlank_Test()
    {
      var votes = new List<VoteInfo>
      {
//                      new VoteInfo {VoteIneligibleReasonGuid = IneligibleReasonEnum.Unreadable_Vote_is_blank},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(null, votes, out newStatus, out spoiledCount).ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooFew);
      spoiledCount.ShouldEqual(0);
    }


    [TestMethod]
    public void KeepReviewStatus_Test()
    {
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount)
        .ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);


      votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };

      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount)
        .ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);
    }

    [TestMethod]
    public void HasDuplicates_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = dupPersonGuid},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = dupPersonGuid},
      };

      var model = new BallotAnalyzer(5, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount)
        .ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.Dup);
    }

    [TestMethod]
    public void HasDuplicates_and_Too_Many_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = dupPersonGuid},
        new VoteInfo {PersonGuid = dupPersonGuid},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };

      var model = new BallotAnalyzer(5, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount)
        .ShouldEqual(true);

      newStatus.ShouldEqual(BallotStatusEnum.TooMany);
    }

    [TestMethod]
    public void AllSpoiled_Test()
    {
      var dupPersonGuid = Guid.NewGuid();

      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonIneligibleReasonGuid = Guid.NewGuid(), VoteId=1},
        new VoteInfo {VoteIneligibleReasonGuid = Guid.NewGuid(), VoteId=2},
        new VoteInfo {PersonIneligibleReasonGuid = Guid.NewGuid(), VoteId=3},
      };

      VoteAnalyzer.UpdateAllStatuses(votes, votes.Select(v=>new Vote{C_RowId = v.VoteId}).ToList(), new Savers(true).VoteSaver);

      var model = new BallotAnalyzer(3, false);

      string newStatus;
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount)
        .ShouldEqual(false);

      newStatus.ShouldEqual(BallotStatusEnum.Ok);
      spoiledCount.ShouldEqual(3);
    }

    [TestMethod]
    public void HasDuplicates2_KeepStatusCode_Test()
    {
      var votes = new List<VoteInfo>
      {
        new VoteInfo {PersonGuid = Guid.NewGuid()},
      };


      var model = new BallotAnalyzer(3, false);

      string newStatus;

      // keep Review
      int spoiledCount;
      model.DetermineStatusFromVotesList(BallotStatusEnum.Review, votes, out newStatus, out spoiledCount)
        .ShouldEqual(false);
      newStatus.ShouldEqual(BallotStatusEnum.Review);
      spoiledCount.ShouldEqual(0);

      // override OK
      model.DetermineStatusFromVotesList(BallotStatusEnum.Ok, votes, out newStatus, out spoiledCount)
        .ShouldEqual(true);
      newStatus.ShouldEqual(BallotStatusEnum.TooFew);
      spoiledCount.ShouldEqual(0);
    }

    //internal class Fakes
    //{
    //    private int _saveChanges;

    //    public int CountOfCallsToSaveChanges
    //    {
    //        get { return _saveChanges; }
    //        set { _saveChanges = value; }
    //    }

    //    public int SaveChanges()
    //    {
    //        _saveChanges++;

    //        return 0;
    //    }
    //}
  }
}