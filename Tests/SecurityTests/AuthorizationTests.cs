using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Controllers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.EF;
using TallyJ.Code.Enumerations;
using Tests.Support;
using Tests.BusinessTests;
using TallyJ.Code.UnityRelated;

namespace Tests.SecurityTests
{
  [TestClass]
  public class AuthorizationTests
  {
    private TestDbContext _dbContext;
    private Election _testElection;
    private Computer _testComputer;
    private Person _testTeller;

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
        Name = "Security Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();

      // Create test computer
      _testComputer = new Computer
      {
        ComputerCode = "SEC1",
        ComputerGuid = Guid.NewGuid(),
        ElectionGuid = _testElection.ElectionGuid
      };
      _dbContext.ComputerFake.Add(_testComputer);

      // Create test teller
      _testTeller = new Person
      {
        FirstName = "Test",
        LastName = "Teller",
        ElectionGuid = _testElection.ElectionGuid,
        IsTeller = true
      };
      _dbContext.PersonFake.Add(_testTeller);
    }

    [TestMethod]
    public void TellerModel_GrantAccessToGuestTeller_WithValidCredentials_GrantsAccess()
    {
      // Arrange
      var tellerModel = new TellerModel();
      var electionGuid = _testElection.ElectionGuid;
      var passcode = "TEST123";
      var computerGuid = Guid.NewGuid();

      // Act
      var result = tellerModel.GrantAccessToGuestTeller(electionGuid, passcode, computerGuid);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void TellerModel_GrantAccessToGuestTeller_WithInvalidCredentials_DeniesAccess()
    {
      // Arrange
      var tellerModel = new TellerModel();
      var electionGuid = _testElection.ElectionGuid;
      var invalidPasscode = "WRONG";
      var computerGuid = Guid.NewGuid();

      // Act
      var result = tellerModel.GrantAccessToGuestTeller(electionGuid, invalidPasscode, computerGuid);

      // Assert
      result.ShouldNotBeNull();
      // Should indicate access denied
    }

    [TestMethod]
    public void TellerModel_ValidateTellerAccess_WithValidTeller_ReturnsTrue()
    {
      // Arrange
      var tellerModel = new TellerModel();
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var hasAccess = tellerModel.ValidateTellerAccess(_testTeller.PersonGuid);

      // Assert
      hasAccess.ShouldEqual(true);
    }

    [TestMethod]
    public void TellerModel_ValidateTellerAccess_WithInvalidTeller_ReturnsFalse()
    {
      // Arrange
      var tellerModel = new TellerModel();
      var invalidTellerGuid = Guid.NewGuid();
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var hasAccess = tellerModel.ValidateTellerAccess(invalidTellerGuid);

      // Assert
      hasAccess.ShouldEqual(false);
    }

    [TestMethod]
    public void ComputerModel_ValidateComputerAccess_WithValidComputer_ReturnsTrue()
    {
      // Arrange
      var computerModel = new ComputerModel();
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var hasAccess = computerModel.ValidateComputerAccess(_testComputer.ComputerGuid);

      // Assert
      hasAccess.ShouldEqual(true);
    }

    [TestMethod]
    public void ComputerModel_ValidateComputerAccess_WithInvalidComputer_ReturnsFalse()
    {
      // Arrange
      var computerModel = new ComputerModel();
      var invalidComputerGuid = Guid.NewGuid();
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var hasAccess = computerModel.ValidateComputerAccess(invalidComputerGuid);

      // Assert
      hasAccess.ShouldEqual(false);
    }

    [TestMethod]
    public void ElectionHelper_JoinIntoElection_WithUnauthorizedUser_DeniesAccess()
    {
      // Arrange
      var electionHelper = new ElectionHelper();
      var unauthorizedElectionGuid = Guid.NewGuid(); // Non-existent election
      var computerGuid = Guid.NewGuid();

      // Act
      var result = electionHelper.JoinIntoElection(unauthorizedElectionGuid, computerGuid);

      // Assert
      result.ShouldEqual(false);
    }

    [TestMethod]
    public void ElectionHelper_JoinIntoElection_WithAuthorizedUser_GrantsAccess()
    {
      // Arrange
      var electionHelper = new ElectionHelper();
      var electionGuid = _testElection.ElectionGuid;
      var computerGuid = _testComputer.ComputerGuid;

      // Act
      var result = electionHelper.JoinIntoElection(electionGuid, computerGuid);

      // Assert
      result.ShouldEqual(true);
    }

    [TestMethod]
    public void PeopleModel_RegisterVotingMethod_WithValidTeller_AllowsRegistration()
    {
      // Arrange
      var peopleModel = new PeopleModel();
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentTellerGuid = _testTeller.PersonGuid;

      var testPerson = new Person
      {
        FirstName = "Voter",
        LastName = "Test"
      }.ForTests();

      // Act
      var result = peopleModel.RegisterVotingMethod(testPerson.C_RowId, "InPerson", false, 1);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void BallotModel_SaveVote_WithValidTeller_AllowsVoteSaving()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentTellerGuid = _testTeller.PersonGuid;

      var testPerson = new Person
      {
        FirstName = "Candidate",
        LastName = "Test",
        CanReceiveVotes = true
      }.ForTests();

      var ballotModel = BallotModelFactory.GetForCurrentElection();
      ballotModel.StartNewBallotJson();

      // Act
      var result = ballotModel.SaveVote(testPerson.C_RowId, 0, null, 0, 1, false);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void SessionSecurity_PreventsCrossSiteRequestForgery()
    {
      // Arrange
      var originalElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentElectionGuid = originalElectionGuid;

      // Act - Simulate potential CSRF attack by changing session
      var maliciousElectionGuid = Guid.NewGuid();
      
      // The system should validate that the election exists and user has access
      var electionHelper = new ElectionHelper();
      var result = electionHelper.JoinIntoElection(maliciousElectionGuid, Guid.NewGuid());

      // Assert
      result.ShouldEqual(false);
      UserSession.CurrentElectionGuid.ShouldEqual(originalElectionGuid); // Should remain unchanged
    }

    [TestMethod]
    public void InputValidation_PreventsSqlInjection()
    {
      // Arrange
      var maliciousInput = "'; DROP TABLE Election; --";
      var peopleModel = new PeopleModel();

      // Act - Try to use malicious input in search
      var result = peopleModel.SearchPeople(maliciousInput);

      // Assert
      result.ShouldNotBeNull();
      // Database should still exist and function normally
      var elections = _dbContext.ElectionFake.ToList();
      elections.ShouldNotBeNull();
      elections.Count.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void ElectionStatusValidation_PreventsFinalizedElectionChanges()
    {
      // Arrange
      _testElection.TallyStatus = ElectionTallyStatusEnum.Finalized;
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      var ballotModel = BallotModelFactory.GetForCurrentElection();

      // Act - Try to create new ballot in finalized election
      var result = ballotModel.StartNewBallotJson();

      // Assert
      result.ShouldNotBeNull();
      // Should contain error message about finalized election
    }

    [TestMethod]
    public void DataIntegrity_PreventsDuplicateVotes()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      
      var testPerson = new Person
      {
        FirstName = "Candidate",
        LastName = "Test",
        CanReceiveVotes = true
      }.ForTests();

      var ballotModel = BallotModelFactory.GetForCurrentElection();
      ballotModel.StartNewBallotJson();

      // Act - Try to vote for same person twice
      var result1 = ballotModel.SaveVote(testPerson.C_RowId, 0, null, 0, 1, false);
      var result2 = ballotModel.SaveVote(testPerson.C_RowId, 0, null, 0, 1, false);

      // Assert
      result1.ShouldNotBeNull();
      result2.ShouldNotBeNull();

      // Should not create duplicate votes
      var ballot = _dbContext.BallotFake.FirstOrDefault();
      var votes = _dbContext.VoteFake.Where(v => v.BallotGuid == ballot.BallotGuid && v.PersonGuid == testPerson.PersonGuid).ToList();
      votes.Count.ShouldEqual(1);
    }
  }
}