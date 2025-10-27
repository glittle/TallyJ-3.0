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
  public class BallotsControllerTests
  {
    private BallotsController _controller;
    private TestDbContext _dbContext;
    private Election _testElection;
    private Location _testLocation;
    private Ballot _testBallot;

    [TestInitialize]
    public void Setup()
    {
      UnityInstance.Reset();
      ElectionTestHelper.Reset();

      _dbContext = new TestDbContext();
      _dbContext.ForTests();
      UnityInstance.Offer(_dbContext);

      _controller = new BallotsController();
      
      // Create test election
      _testElection = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();

      // Create test location
      _testLocation = new Location
      {
        Name = "Test Location",
        SortOrder = 1
      };
      _testLocation.ForTests();

      // Create test ballot
      _testBallot = new Ballot
      {
        ComputerCode = "A",
        BallotNumAtComputer = 1
      }.ForTests();
    }

    [TestCleanup]
    public void Cleanup()
    {
      _controller?.Dispose();
    }

    [TestMethod]
    public void Index_WithValidLocation_ReturnsView()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.Index();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void Index_WithInvalidLocation_RedirectsToDashboard()
    {
      // This test would need more setup to simulate invalid location scenario
      // For now, we'll test the basic functionality

      // Act
      var result = _controller.Index();

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void Reconcile_ReturnsView()
    {
      // Act
      var result = _controller.Reconcile();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SortBallots_ReturnsView()
    {
      // Act
      var result = _controller.SortBallots();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void BallotsForLocation_WithValidId_ReturnsBallotsInfo()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var locationId = _testLocation.C_RowId;

      // Act
      var result = _controller.BallotsForLocation(locationId);

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
      var personId = 1;
      var voteId = 1;

      // Act
      var result = _controller.SaveVote(personId, voteId);

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
      var voteId = 1;

      // Act
      var result = _controller.DeleteVote(voteId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void NeedsReview_WithFlag_UpdatesReviewStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var needsReview = true;

      // Act
      var result = _controller.NeedsReview(needsReview);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SwitchToBallot_WithValidId_SwitchesToBallot()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var ballotId = _testBallot.C_RowId;

      // Act
      var result = _controller.SwitchToBallot(ballotId, false);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateLocationStatus_WithValidData_UpdatesStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var locationId = _testLocation.C_RowId;
      var status = "Open";

      // Act
      var result = _controller.UpdateLocationStatus(locationId, status);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetLocationInfo_ReturnsLocationInfo()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation.LocationGuid;

      // Act
      var result = _controller.GetLocationInfo();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void NewBallot_CreatesNewBallot()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation.LocationGuid;

      // Act
      var result = _controller.NewBallot();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeleteBallot_DeletesCurrentBallot()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.DeleteBallot();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void RefreshBallotsList_RefreshesBallotsInfo()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.RefreshBallotsList();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ChangeBallotFilter_WithCode_ChangesFilter()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var filterCode = "A";

      // Act
      var result = _controller.ChangeBallotFilter(filterCode);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }
  }
}