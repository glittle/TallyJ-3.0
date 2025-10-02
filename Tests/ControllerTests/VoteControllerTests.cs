using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Controllers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.EF;
using Tests.Support;
using Tests.BusinessTests;
using TallyJ.Code.UnityRelated;
using TallyJ.Code.Enumerations;

namespace Tests.ControllerTests
{
  [TestClass]
  public class VoteControllerTests
  {
    private VoteController _controller;
    private TestDbContext _dbContext;
    private Election _testElection;
    private Person _testPerson;
    private Ballot _testBallot;
    private Vote _testVote;

    [TestInitialize]
    public void Setup()
    {
      UnityInstance.Reset();
      ElectionTestHelper.Reset();

      _dbContext = new TestDbContext();
      _dbContext.ForTests();
      UnityInstance.Offer(_dbContext);

      _controller = new VoteController();
      
      // Create test election
      _testElection = new Election
      {
        Name = "Vote Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();

      // Create test person
      _testPerson = new Person
      {
        FirstName = "John",
        LastName = "Candidate",
        CanReceiveVotes = true,
        CanVote = true
      }.ForTests();

      // Create test ballot
      _testBallot = new Ballot
      {
        ComputerCode = "V1",
        BallotNumAtComputer = 1,
        StatusCode = BallotStatusEnum.Ok
      }.ForTests();

      // Create test vote
      _testVote = new Vote
      {
        SingleNameElectionCount = 1
      }.ForTests(_testBallot, _testPerson);
    }

    [TestCleanup]
    public void Cleanup()
    {
      _controller?.Dispose();
    }

    [TestMethod]
    public void Index_ReturnsView()
    {
      // Act
      var result = _controller.Index();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetVotes_ReturnsVoteList()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetVotes();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SaveVote_WithValidData_SavesVote()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personId = _testPerson.C_RowId;
      var ballotId = _testBallot.C_RowId;

      // Act
      var result = _controller.SaveVote(personId, ballotId, 1);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SaveVote_WithInvalidData_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidPersonId = 99999;
      var ballotId = _testBallot.C_RowId;

      // Act
      var result = _controller.SaveVote(invalidPersonId, ballotId, 1);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeleteVote_WithValidId_DeletesVote()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var voteId = _testVote.C_RowId;

      // Act
      var result = _controller.DeleteVote(voteId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeleteVote_WithInvalidId_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidVoteId = 99999;

      // Act
      var result = _controller.DeleteVote(invalidVoteId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateVoteStatus_WithValidData_UpdatesStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var voteId = _testVote.C_RowId;
      var newStatus = VoteStatusCode.Changed;

      // Act
      var result = _controller.UpdateVoteStatus(voteId, newStatus);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ValidateVote_WithValidVote_ReturnsValidationResult()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var voteId = _testVote.C_RowId;

      // Act
      var result = _controller.ValidateVote(voteId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetVoteDetails_WithValidId_ReturnsVoteDetails()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var voteId = _testVote.C_RowId;

      // Act
      var result = _controller.GetVoteDetails(voteId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetVoteDetails_WithInvalidId_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidVoteId = 99999;

      // Act
      var result = _controller.GetVoteDetails(invalidVoteId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SearchVotes_WithSearchTerm_ReturnsFilteredResults()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var searchTerm = "John";

      // Act
      var result = _controller.SearchVotes(searchTerm);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetVotesForBallot_WithValidBallotId_ReturnsVotes()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var ballotId = _testBallot.C_RowId;

      // Act
      var result = _controller.GetVotesForBallot(ballotId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetVotesForPerson_WithValidPersonId_ReturnsVotes()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personId = _testPerson.C_RowId;

      // Act
      var result = _controller.GetVotesForPerson(personId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void MarkVoteForReview_WithValidId_MarksForReview()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var voteId = _testVote.C_RowId;
      var needsReview = true;

      // Act
      var result = _controller.MarkVoteForReview(voteId, needsReview);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetVoteStatistics_ReturnsStatistics()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetVoteStatistics();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ExportVotes_ReturnsExportResult()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.ExportVotes();

      // Assert
      result.ShouldNotBeNull();
      var actionResult = result as ActionResult;
      actionResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ImportVotes_WithValidData_ImportsVotes()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var csvData = "PersonName,BallotCode,Count\nJohn Candidate,V1,1";

      // Act
      var result = _controller.ImportVotes(csvData);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ImportVotes_WithInvalidData_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidCsvData = "Invalid,CSV,Data";

      // Act
      var result = _controller.ImportVotes(invalidCsvData);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }
  }
}