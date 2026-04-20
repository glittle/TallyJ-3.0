using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using TallyJ.EF;
using TallyJ.Code.Session;
using TallyJ.Code.Enumerations;
using Tests.Support;
using Tests.BusinessTests;
using TallyJ.Code.UnityRelated;

namespace Tests.DataValidationTests
{
  [TestClass]
  public class ElectionValidationTests
  {
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

      // Create test election
      _testElection = new Election
      {
        Name = "Validation Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All",
        NumberToElect = 9,
        NumberExtra = 0
      }.ForTests();
    }

    [TestMethod]
    public void Election_NameValidation_RequiresName()
    {
      // Arrange
      var election = new Election
      {
        Name = null, // Invalid - null name
        ElectionType = "LSA",
        ElectionMode = "Normal"
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_NameValidation_RequiresNonEmptyName()
    {
      // Arrange
      var election = new Election
      {
        Name = "", // Invalid - empty name
        ElectionType = "LSA",
        ElectionMode = "Normal"
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_NameValidation_AcceptsValidName()
    {
      // Arrange
      var election = new Election
      {
        Name = "Valid Election Name",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 9
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Election_TypeValidation_RequiresValidType()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "INVALID", // Invalid type
        ElectionMode = "Normal"
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_TypeValidation_AcceptsLSA()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 9
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Election_TypeValidation_AcceptsNSA()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "NSA",
        ElectionMode = "Normal",
        NumberToElect = 9
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Election_ModeValidation_RequiresValidMode()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "INVALID" // Invalid mode
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_NumberToElectValidation_RequiresPositiveNumber()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = -1 // Invalid - negative number
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_NumberToElectValidation_RequiresNonZeroNumber()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",  
        NumberToElect = 0 // Invalid - zero
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_NumberExtraValidation_AcceptsZero()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 9,
        NumberExtra = 0 // Valid - zero is allowed for extra
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Election_NumberExtraValidation_RejectsNegative()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 9,
        NumberExtra = -1 // Invalid - negative number
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_CanVoteValidation_RequiresValidOption()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 9,
        CanVote = "INVALID" // Invalid option
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_CanReceiveValidation_RequiresValidOption()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 9,
        CanVote = "All",
        CanReceive = "INVALID" // Invalid option
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_DateRangeValidation_StartDateBeforeEndDate()
    {
      // Arrange
      var tomorrow = DateTime.Now.AddDays(1);
      var yesterday = DateTime.Now.AddDays(-1);
      
      var election = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 9,
        DateOfElection = tomorrow,  // Start after end - invalid
        ElectionDateEnding = yesterday
      };

      // Act & Assert
      var isValid = ValidateElection(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_UniqueNameValidation_PreventsNameDuplication()
    {
      // Arrange - First election already exists
      var existingElection = _testElection;
      
      var duplicateElection = new Election
      {
        Name = existingElection.Name, // Duplicate name
        ElectionType = "NSA",
        ElectionMode = "Normal",
        NumberToElect = 5
      };

      // Act & Assert
      var isValid = ValidateElectionUniqueness(duplicateElection);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_BusinessRulesValidation_LSARequires9Positions()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test LSA Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        NumberToElect = 5 // Invalid - LSA should have 9
      };

      // Act & Assert
      var isValid = ValidateElectionBusinessRules(election);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_BusinessRulesValidation_NSAAllowsVariablePositions()
    {
      // Arrange
      var election = new Election
      {
        Name = "Test NSA Election",
        ElectionType = "NSA",
        ElectionMode = "Normal",
        NumberToElect = 5 // Valid - NSA can have variable number
      };

      // Act & Assert
      var isValid = ValidateElectionBusinessRules(election);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Election_StatusValidation_ValidatesStatusTransitions()
    {
      // Arrange
      _testElection.TallyStatus = ElectionTallyStatusEnum.Finalized;

      // Act - Try to go backwards to Tallying (invalid)
      var canTransition = ValidateStatusTransition(_testElection, ElectionTallyStatusEnum.Tallying);

      // Assert
      canTransition.ShouldEqual(false);
    }

    [TestMethod]
    public void Election_StatusValidation_AllowsValidTransitions()
    {
      // Arrange
      _testElection.TallyStatus = ElectionTallyStatusEnum.None;

      // Act - Go to Tallying (valid)
      var canTransition = ValidateStatusTransition(_testElection, ElectionTallyStatusEnum.Tallying);

      // Assert
      canTransition.ShouldEqual(true);
    }

    private bool ValidateElection(Election election)
    {
      // Basic validation logic
      if (string.IsNullOrEmpty(election.Name))
        return false;

      if (!IsValidElectionType(election.ElectionType))
        return false;

      if (!IsValidElectionMode(election.ElectionMode))
        return false;

      if (election.NumberToElect <= 0)
        return false;

      if (election.NumberExtra < 0)
        return false;

      if (!IsValidCanVoteOption(election.CanVote))
        return false;

      if (!IsValidCanReceiveOption(election.CanReceive))
        return false;

      if (election.DateOfElection.HasValue && election.ElectionDateEnding.HasValue &&
          election.DateOfElection > election.ElectionDateEnding)
        return false;

      return true;
    }

    private bool ValidateElectionUniqueness(Election election)
    {
      var existingElection = _dbContext.ElectionFake
        .FirstOrDefault(e => e.Name == election.Name && e.C_RowId != election.C_RowId);
      return existingElection == null;
    }

    private bool ValidateElectionBusinessRules(Election election)
    {
      if (election.ElectionType == "LSA" && election.NumberToElect != 9)
        return false;

      return true;
    }

    private bool ValidateStatusTransition(Election election, ElectionTallyStatusEnum newStatus)
    {
      // Can't go backwards from Finalized
      if (election.TallyStatus == ElectionTallyStatusEnum.Finalized && 
          newStatus != ElectionTallyStatusEnum.Finalized)
        return false;

      return true;
    }

    private bool IsValidElectionType(string type)
    {
      return new[] { "LSA", "NSA", "RDA", "UDA" }.Contains(type);
    }

    private bool IsValidElectionMode(string mode)
    {
      return new[] { "Normal", "TieBreak" }.Contains(mode);
    }

    private bool IsValidCanVoteOption(string option)
    {
      return new[] { "All", "InPersonOnly", "OnlineOnly" }.Contains(option);
    }

    private bool IsValidCanReceiveOption(string option)
    {
      return new[] { "All", "EligibleOnly", "Custom" }.Contains(option);
    }
  }
}