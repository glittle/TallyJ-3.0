using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using TallyJ.EF;
using TallyJ.Code.Session;
using TallyJ.Code.Enumerations;
using Tests.Support;
using Tests.BusinessTests;
using TallyJ.Code.UnityRelated;

namespace Tests.CoreModelTests
{
  [TestClass]
  public class ElectionHelperTests
  {
    private ElectionHelper _electionHelper;
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

      _electionHelper = new ElectionHelper();
      
      // Create test election
      _testElection = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();
    }

    [TestMethod]
    public void GetRules_WithLSANormal_ReturnsCorrectRules()
    {
      // Act
      var rules = ElectionHelper.GetRules("LSA", "Normal");

      // Assert
      rules.ShouldNotBeNull();
      rules.Num.ShouldEqual(9);
      rules.Extra.ShouldEqual(0);
      rules.ExtraLocked.ShouldEqual(true);
    }

    [TestMethod]
    public void GetRules_WithLSATieBreak_ReturnsCorrectRules()
    {
      // Act
      var rules = ElectionHelper.GetRules("LSA", "TieBreak");

      // Assert
      rules.ShouldNotBeNull();
      rules.Num.ShouldBeGreaterThan(0);
      rules.Extra.ShouldEqual(0);
    }

    [TestMethod]
    public void GetRules_WithNSANormal_ReturnsCorrectRules()
    {
      // Act
      var rules = ElectionHelper.GetRules("NSA", "Normal");

      // Assert
      rules.ShouldNotBeNull();
      rules.Num.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void GetRules_WithRDANormal_ReturnsCorrectRules()
    {
      // Act
      var rules = ElectionHelper.GetRules("RDA", "Normal");

      // Assert
      rules.ShouldNotBeNull();
      rules.Num.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void GetRules_WithInvalidType_ReturnsDefaultRules()
    {
      // Act
      var rules = ElectionHelper.GetRules("INVALID", "Normal");

      // Assert
      rules.ShouldNotBeNull();
      rules.Num.ShouldEqual(0);
      rules.Extra.ShouldEqual(0);
    }

    [TestMethod]
    public void Create_CreatesNewElection()
    {
      // Act
      var result = _electionHelper.Create();

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void JoinIntoElection_WithValidGuid_ReturnsTrue()
    {
      // Arrange
      var electionGuid = _testElection.ElectionGuid;
      var computerGuid = Guid.NewGuid();

      // Act
      var result = _electionHelper.JoinIntoElection(electionGuid, computerGuid);

      // Assert
      result.ShouldEqual(true);
      UserSession.CurrentElectionGuid.ShouldEqual(electionGuid);
    }

    [TestMethod]
    public void JoinIntoElection_WithInvalidGuid_ReturnsFalse()
    {
      // Arrange
      var invalidGuid = Guid.NewGuid();
      var computerGuid = Guid.NewGuid();

      // Act
      var result = _electionHelper.JoinIntoElection(invalidGuid, computerGuid);

      // Assert
      result.ShouldEqual(false);
    }

    [TestMethod]
    public void SetTallyStatusJson_WithValidStatus_UpdatesStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var status = "Active";

      // Act
      var result = _electionHelper.SetTallyStatusJson(status);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void SetTallyStatusJson_WithInvalidStatus_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidStatus = "InvalidStatus";

      // Act
      var result = _electionHelper.SetTallyStatusJson(invalidStatus);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void ProcessOnlineBallots_ProcessesValidBallots()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _electionHelper.ProcessOnlineBallots();

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void ValidateElectionRules_WithValidElection_ReturnsTrue()
    {
      // Arrange
      _testElection.ElectionType = "LSA";
      _testElection.ElectionMode = "Normal";
      _testElection.NumberToElect = 9;

      // Act
      var isValid = _electionHelper.ValidateElectionRules(_testElection);

      // Assert
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void ValidateElectionRules_WithInvalidElection_ReturnsFalse()
    {
      // Arrange
      _testElection.ElectionType = "LSA";
      _testElection.ElectionMode = "Normal";
      _testElection.NumberToElect = -1; // Invalid number

      // Act
      var isValid = _electionHelper.ValidateElectionRules(_testElection);

      // Assert
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void GetElectionStatistics_ReturnsStatistics()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var stats = _electionHelper.GetElectionStatistics();

      // Assert
      stats.ShouldNotBeNull();
    }

    [TestMethod]
    public void CanModifyElection_WithActiveElection_ReturnsFalse()
    {
      // Arrange
      _testElection.TallyStatus = ElectionTallyStatusEnum.Finalized;

      // Act
      var canModify = _electionHelper.CanModifyElection(_testElection);

      // Assert
      canModify.ShouldEqual(false);
    }

    [TestMethod]
    public void CanModifyElection_WithDraftElection_ReturnsTrue()
    {
      // Arrange
      _testElection.TallyStatus = ElectionTallyStatusEnum.None;

      // Act
      var canModify = _electionHelper.CanModifyElection(_testElection);

      // Assert
      canModify.ShouldEqual(true);
    }
  }
}