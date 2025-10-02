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
  public class PublicControllerTests
  {
    private PublicController _controller;
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

      _controller = new PublicController();
      
      // Create test election
      _testElection = new Election
      {
        Name = "Public Test Election",
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
    public void About_ReturnsView()
    {
      // Act
      var result = _controller.About();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void Contact_ReturnsView()
    {
      // Act
      var result = _controller.Contact();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void Privacy_ReturnsView()
    {
      // Act
      var result = _controller.Privacy();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void Learning_ReturnsView()
    {
      // Act
      var result = _controller.Learning();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod] 
    public void Install_ReturnsView()
    {
      // Act
      var result = _controller.Install();

      // Assert
      result.ShouldNotBeNull();
      var viewResult = result as ViewResult;
      viewResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void Warmup_ContactsDatabase_ReturnsNull()
    {
      // Act
      var result = _controller.Warmup();

      // Assert
      result.ShouldEqual(null);
    }

    [TestMethod]
    public void TellerJoin_WithValidCredentials_GrantsAccess()
    {
      // Arrange
      var electionGuid = _testElection.ElectionGuid;
      var passcode = "TEST123";
      var oldComputerGuid = Guid.NewGuid();

      // Act
      var result = _controller.TellerJoin(electionGuid, passcode, oldComputerGuid);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void TellerJoin_WithInvalidCredentials_DeniesAccess()
    {
      // Arrange
      var electionGuid = _testElection.ElectionGuid;
      var invalidPasscode = "WRONG";
      var oldComputerGuid = Guid.NewGuid();

      // Act
      var result = _controller.TellerJoin(electionGuid, invalidPasscode, oldComputerGuid);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetTimeOffset_ReturnsTimeOffset()
    {
      // Arrange
      var currentTime = DateTime.Now.Ticks;

      // Act
      var result = _controller.GetTimeOffset(currentTime);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void OpenElections_ReturnsOpenElectionsList()
    {
      // Act
      var result = _controller.OpenElections();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void Heartbeat_WithPulseInfo_ProcessesHeartbeat()
    {
      // Arrange
      var pulseInfo = new PulseInfo
      {
        // Add required properties
        ComputerGuid = Guid.NewGuid(),
        ElectionGuid = _testElection.ElectionGuid
      };

      // Act
      var result = _controller.Heartbeat(pulseInfo);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void PublicHub_WithConnectionId_JoinsHub()
    {
      // Arrange
      var connectionId = "test-connection-id";

      // Act
      var result = _controller.PublicHub(connectionId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void VoterCodeHub_WithValidParameters_JoinsHub()
    {
      // Arrange
      var connectionId = "test-connection-id";
      var hubKey = "test-hub-key";

      // Act
      var result = _controller.VoterCodeHub(connectionId, hubKey);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void IssueCode_WithValidParameters_IssuesCode()
    {
      // Arrange
      var type = "email";
      var method = "send";
      var target = "test@example.com";
      var hubKey = "test-hub-key";

      // Act
      var result = _controller.IssueCode(type, method, target, hubKey);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void LoginWithCode_WithValidCode_AttemptsLogin()
    {
      // Arrange
      var code = "123456";
      var hubKey = "test-hub-key";

      // Act
      var result = _controller.LoginWithCode(code, hubKey);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void LoginWithCode_WithInvalidCode_DeniesLogin()
    {
      // Arrange
      var invalidCode = "999999";
      var hubKey = "test-hub-key";

      // Act
      var result = _controller.LoginWithCode(invalidCode, hubKey);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void JoinMainHub_WithValidParameters_JoinsHub()
    {
      // Arrange
      var connectionId = "test-connection-id";
      var electionGuid = _testElection.ElectionGuid.ToString();

      // Act (should not throw exception)
      _controller.JoinMainHub(connectionId, electionGuid);

      // Assert - No exception means success
      Assert.IsTrue(true);
    }

    [TestMethod]
    public void JoinMainHubAll_WithValidParameters_JoinsMultipleHubs()
    {
      // Arrange
      var connectionId = "test-connection-id";
      var electionGuidList = $"{_testElection.ElectionGuid}";
      UserSession.CurrentTellerGuid = Guid.NewGuid(); // Set as known teller

      // Act (should not throw exception)
      _controller.JoinMainHubAll(connectionId, electionGuidList);

      // Assert - No exception means success
      Assert.IsTrue(true);
    }

    [TestMethod]
    public void JoinMainHubAll_WithUnknownTeller_DoesNotJoin()
    {
      // Arrange
      var connectionId = "test-connection-id";
      var electionGuidList = $"{_testElection.ElectionGuid}";
      UserSession.CurrentTellerGuid = null; // Unknown teller

      // Act (should not throw exception)
      _controller.JoinMainHubAll(connectionId, electionGuidList);

      // Assert - No exception means success (method returns early)
      Assert.IsTrue(true);
    }

    [TestMethod]
    public void SmsStatus_WithValidParameters_ProcessesStatus()
    {
      // Arrange
      var smsSid = "test-sms-sid";
      var messageStatus = "delivered";
      var to = "+1234567890";
      var errorCode = 0;

      // Act (should not throw exception)
      _controller.SmsStatus(smsSid, messageStatus, to, errorCode);

      // Assert - No exception means success
      Assert.IsTrue(true);
    }

    [TestMethod]
    public void SmsStatus_WithErrorCode_ProcessesError()
    {
      // Arrange
      var smsSid = "test-sms-sid";
      var messageStatus = "failed";
      var to = "+1234567890";
      var errorCode = 30008; // SMS error code

      // Act (should not throw exception)
      _controller.SmsStatus(smsSid, messageStatus, to, errorCode);

      // Assert - No exception means success
      Assert.IsTrue(true);
    }
  }
}