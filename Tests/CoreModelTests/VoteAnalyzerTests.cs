using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using TallyJ.EF;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using Tests.Support;
using Tests.BusinessTests;
using TallyJ.Code.UnityRelated;

namespace Tests.CoreModelTests
{
  [TestClass]
  public class VoteAnalyzerTests
  {
    private TestDbContext _dbContext;
    private Election _testElection;
    private Person _testPerson;

    [TestInitialize]
    public void Setup()
    {
      UnityInstance.Reset();
      ElectionTestHelper.Reset();

      _dbContext = new TestDbContext();
      _dbContext.ForTests();
      UnityInstance.Offer(_dbContext);

      // Create test election
      _testElection = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();

      // Create test person
      _testPerson = new Person
      {
        FirstName = "John",
        LastName = "Doe",
        CanReceiveVotes = true,
        CanVote = true
      }.ForTests();
    }

    [TestMethod]
    public void DetermineStatus_WithOnlineRawVote_ReturnsOnlineRaw()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = "John Doe",
        PersonIneligibleReasonGuid = null,
        PersonGuid = null,
        VoteIneligibleReasonGuid = null
      };

      // Act
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.OnlineRaw);
    }

    [TestMethod]
    public void DetermineStatus_WithIneligiblePerson_ReturnsSpoiled()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = null,
        PersonIneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Deceased,
        PersonGuid = _testPerson.PersonGuid,
        VoteIneligibleReasonGuid = null,
        PersonCanReceiveVotes = false
      };

      // Act
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.Spoiled);
    }

    [TestMethod]
    public void DetermineStatus_WithValidVote_ReturnsOk()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = null,
        PersonIneligibleReasonGuid = null,
        PersonGuid = _testPerson.PersonGuid,
        VoteIneligibleReasonGuid = null,
        PersonCanReceiveVotes = true,
        PersonCombinedInfo = "John Doe",
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.Ok);
    }

    [TestMethod]
    public void DetermineStatus_WithChangedPersonInfo_ReturnsChanged()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = null,
        PersonIneligibleReasonGuid = null,
        PersonGuid = _testPerson.PersonGuid,
        VoteIneligibleReasonGuid = null,
        PersonCanReceiveVotes = true,
        PersonCombinedInfo = "John Smith",
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.Changed);
    }

    [TestMethod]
    public void DetermineStatus_WithPartialMatch_ReturnsOk()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = null,
        PersonIneligibleReasonGuid = null,
        PersonGuid = _testPerson.PersonGuid,
        VoteIneligibleReasonGuid = null,
        PersonCanReceiveVotes = true,
        PersonCombinedInfo = "John Doe (Updated)",
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.Ok);
    }

    [TestMethod]
    public void DetermineStatus_WithNullPersonCombinedInfo_HandlesGracefully()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = null,
        PersonIneligibleReasonGuid = null,
        PersonGuid = _testPerson.PersonGuid,
        VoteIneligibleReasonGuid = null,
        PersonCanReceiveVotes = true,
        PersonCombinedInfo = null,
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.Changed);
    }

    [TestMethod]
    public void DetermineStatus_WithEmptyPersonCombinedInfo_HandlesGracefully()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = null,
        PersonIneligibleReasonGuid = null,
        PersonGuid = _testPerson.PersonGuid,
        VoteIneligibleReasonGuid = null,
        PersonCanReceiveVotes = true,
        PersonCombinedInfo = "",
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.Changed);
    }

    [TestMethod]
    public void DetermineStatus_WithVoteIneligibleReason_ReturnsSpoiled()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        OnlineVoteRaw = null,
        PersonIneligibleReasonGuid = null,
        PersonGuid = _testPerson.PersonGuid,
        VoteIneligibleReasonGuid = InvalidReasonEnum.InvalidReasonGuid,
        PersonCanReceiveVotes = true,
        PersonCombinedInfo = "John Doe",
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act  
      var status = VoteAnalyzer.DetermineStatus(voteInfo);

      // Assert
      status.ShouldEqual(VoteStatusCode.Spoiled);
    }

    [TestMethod]
    public void NeedsReview_WithChangedInfo_ReturnsTrue()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        PersonCombinedInfo = "John Smith",
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act
      var needsReview = VoteAnalyzer.NeedsReview(voteInfo);

      // Assert
      needsReview.ShouldEqual(true);
    }

    [TestMethod]
    public void NeedsReview_WithUnchangedInfo_ReturnsFalse()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        PersonCombinedInfo = "John Doe",
        PersonCombinedInfoInVote = "John Doe"
      };

      // Act
      var needsReview = VoteAnalyzer.NeedsReview(voteInfo);

      // Assert
      needsReview.ShouldEqual(false);
    }

    [TestMethod]
    public void NeedsReview_WithNullValues_ReturnsFalse()
    {
      // Arrange
      var voteInfo = new VoteInfo
      {
        PersonCombinedInfo = null,
        PersonCombinedInfoInVote = null
      };

      // Act
      var needsReview = VoteAnalyzer.NeedsReview(voteInfo);

      // Assert
      needsReview.ShouldEqual(false);
    }
  }
}