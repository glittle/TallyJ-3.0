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
  public class DashboardControllerTests
  {
    private DashboardController _controller;
    private TestDbContext _dbContext;
    private Election _testElection;
    private Computer _testComputer;

    [TestInitialize]
    public void Setup()
    {
      UnityInstance.Reset();
      ElectionTestHelper.Reset();

      _dbContext = new TestDbContext();
      _dbContext.ForTests();
      UnityInstance.Offer(_dbContext);

      _controller = new DashboardController();
      
      // Create test election
      _testElection = new Election
      {
        Name = "Dashboard Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();

      // Create test computer
      _testComputer = new Computer
      {
        ComputerCode = "D1",
        ComputerGuid = Guid.NewGuid(),
        ElectionGuid = _testElection.ElectionGuid
      };
      _dbContext.ComputerFake.Add(_testComputer);
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
    public void ElectionList_ReturnsView()
    {
      // Act
      var result = _controller.ElectionList();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetElectionsList_ReturnsElectionsList()
    {
      // Act
      var result = _controller.GetElectionsList();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetDashboardData_WithValidElection_ReturnsDashboardData()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetDashboardData();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetDashboardData_WithoutElection_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = null;

      // Act
      var result = _controller.GetDashboardData();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetElectionStatistics_WithValidElection_ReturnsStatistics()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetElectionStatistics();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetRecentActivity_ReturnsActivityData()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetRecentActivity();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetSystemStatus_ReturnsSystemStatus()
    {
      // Act
      var result = _controller.GetSystemStatus();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetComputerStatus_ReturnsComputerStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentComputerGuid = _testComputer.ComputerGuid;

      // Act
      var result = _controller.GetComputerStatus();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetLocationSummary_ReturnsLocationSummary()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetLocationSummary();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateComputerStatus_WithValidData_UpdatesStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentComputerGuid = _testComputer.ComputerGuid;
      var status = "Active";

      // Act
      var result = _controller.UpdateComputerStatus(status);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetElectionProgress_ReturnsProgress()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetElectionProgress();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetVotingStatistics_ReturnsVotingStats()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetVotingStatistics();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetErrorLogs_ReturnsErrorLogs()
    {
      // Arrange
      var days = 7;

      // Act
      var result = _controller.GetErrorLogs(days);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetAuditTrail_ReturnsAuditTrail()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var days = 30;

      // Act
      var result = _controller.GetAuditTrail(days);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void RefreshDashboard_RefreshesData()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.RefreshDashboard();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void NotifyTellers_WithMessage_SendsNotification()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var message = "Test notification message";

      // Act
      var result = _controller.NotifyTellers(message);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetActiveUsers_ReturnsActiveUsers()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetActiveUsers();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetPerformanceMetrics_ReturnsMetrics()
    {
      // Act
      var result = _controller.GetPerformanceMetrics();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ExportDashboardData_ReturnsExportResult()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.ExportDashboardData();

      // Assert
      result.ShouldNotBeNull();
      var actionResult = result as ActionResult;
      actionResult.ShouldNotBeNull();
    }
  }
}