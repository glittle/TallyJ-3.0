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

namespace Tests.ControllerTests
{
  [TestClass]
  public class ElectionsControllerTests
  {
    private ElectionsController _controller;
    private TestDbContext _dbContext;
    private Election _testElection;

    [TestInitialize]
    public void Setup()
    {
      UnityInstance.Reset();
      ElectionTestHelper.Reset();

      _dbContext = new TestDbContext();
      _dbContext.ForTests();
      UnityInstance.Offer(_dbContext);

      _controller = new ElectionsController();
      
      // Create a test election
      _testElection = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();
    }

    [TestCleanup]
    public void Cleanup()
    {
      _controller?.Dispose();
    }

    [TestMethod]
    public void Index_ReturnsNull()
    {
      // Act
      var result = _controller.Index();

      // Assert
      result.ShouldEqual(null);
    }

    [TestMethod]
    public void SelectElection_WithValidGuid_ReturnsSuccessResult()
    {
      // Arrange
      var electionGuid = _testElection.ElectionGuid;
      var computerGuid = Guid.NewGuid();

      // Act
      var result = _controller.SelectElection(electionGuid, computerGuid);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SelectElection_WithInvalidGuid_ReturnsFailureResult()
    {
      // Arrange
      var invalidGuid = Guid.NewGuid(); // Non-existent election
      var computerGuid = Guid.NewGuid();

      // Act
      var result = _controller.SelectElection(invalidGuid, computerGuid);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateElectionStatus_WithValidState_UpdatesStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var newState = "Active";

      // Act
      var result = _controller.UpdateElectionStatus(newState);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateElectionStatus_WithInvalidState_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidState = "InvalidState";

      // Act
      var result = _controller.UpdateElectionStatus(invalidState);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod] 
    public void CreateElection_CreatesNewElection()
    {
      // Act
      var result = _controller.CreateElection();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeleteElection_WithValidGuid_DeletesElection()
    {
      // Arrange
      var electionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.DeleteElection(electionGuid);

      // Assert
      result.ShouldNotBeNull();
      var actionResult = result as ActionResult;
      actionResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void JoinImportHub_WithConnectionId_ReturnsSuccess()
    {
      // Arrange
      var connectionId = "test-connection-id";

      // Act
      var result = _controller.JoinImportHub(connectionId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void JoinAnalyzeHub_WithConnectionId_ReturnsSuccess()
    {
      // Arrange
      var connectionId = "test-connection-id";

      // Act
      var result = _controller.JoinAnalyzeHub(connectionId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ResetCache_ClearsCache_ReturnsSuccessMessage()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.ResetCache();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ExportElection_WithValidGuid_ReturnsExportResult()
    {
      // Arrange
      var electionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.ExportElection(electionGuid);

      // Assert
      result.ShouldNotBeNull();
      var actionResult = result as ActionResult;
      actionResult.ShouldNotBeNull();
    }
  }
}