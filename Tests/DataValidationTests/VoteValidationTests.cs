using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using TallyJ.EF;
using TallyJ.Code.Session;
using TallyJ.Code.Enumerations;
using TallyJ.Code;
using Tests.Support;
using Tests.BusinessTests;
using TallyJ.Code.UnityRelated;

namespace Tests.DataValidationTests
{
  [TestClass]
  public class VoteValidationTests
  {
    private TestDbContext _dbContext;
    private Election _testElection;
    private Person _testPerson;
    private Ballot _testBallot;

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
        Name = "Vote Validation Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All",
        NumberToElect = 9
      }.ForTests();

      // Create test person
      _testPerson = new Person
      {
        FirstName = "Valid",
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
    }

    [TestMethod]
    public void Vote_PersonValidation_RequiresValidPerson()
    {
      // Arrange
      var vote = new Vote
      {
        PersonGuid = Guid.NewGuid(), // Non-existent person
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateVote(vote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_PersonValidation_AcceptsValidPerson()
    {
      // Arrange
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateVote(vote);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Vote_BallotValidation_RequiresValidBallot()
    {
      // Arrange
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = Guid.NewGuid(), // Non-existent ballot
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateVote(vote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_EligibilityValidation_RejectsIneligiblePerson()
    {
      // Arrange
      _testPerson.CanReceiveVotes = false; // Make person ineligible
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateVoteEligibility(vote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_EligibilityValidation_AcceptsEligiblePerson()
    {
      // Arrange
      _testPerson.CanReceiveVotes = true; // Person is eligible
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateVoteEligibility(vote);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Vote_CountValidation_RequiresPositiveCount()
    {
      // Arrange
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 0 // Invalid - zero count
      };

      // Act & Assert
      var isValid = ValidateVoteCount(vote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_CountValidation_RejectsNegativeCount()
    {
      // Arrange
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = -1 // Invalid - negative count
      };

      // Act & Assert
      var isValid = ValidateVoteCount(vote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_CountValidation_AcceptsValidCount()
    {
      // Arrange
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1 // Valid count
      };

      // Act & Assert
      var isValid = ValidateVoteCount(vote);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Vote_DuplicationValidation_PreventsDuplicateVotes()
    {
      // Arrange
      var existingVote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      }.ForTests(_testBallot, _testPerson);

      var duplicateVote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid, // Same person
        BallotGuid = _testBallot.BallotGuid, // Same ballot
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateVoteDuplication(duplicateVote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_DuplicationValidation_AllowsDifferentPersonSameBallot()
    {
      // Arrange
      var otherPerson = new Person
      {
        FirstName = "Other",
        LastName = "Candidate",
        CanReceiveVotes = true,
        CanVote = true
      }.ForTests();

      var existingVote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      }.ForTests(_testBallot, _testPerson);

      var newVote = new Vote
      {
        PersonGuid = otherPerson.PersonGuid, // Different person
        BallotGuid = _testBallot.BallotGuid, // Same ballot
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateVoteDuplication(newVote);
      isValid.ShouldEqual(true);
    }

    [TestMethod]
    public void Vote_StatusValidation_AcceptsValidStatuses()
    {
      // Arrange & Act & Assert
      var validStatuses = new[] { VoteStatusCode.Ok, VoteStatusCode.Spoiled, VoteStatusCode.Changed };
      
      foreach (var status in validStatuses)
      {
        var vote = new Vote
        {
          PersonGuid = _testPerson.PersonGuid,
          BallotGuid = _testBallot.BallotGuid,
          SingleNameElectionCount = 1,
          StatusCode = status
        };

        var isValid = ValidateVoteStatus(vote);
        isValid.ShouldEqual(true, $"Status {status} should be valid");
      }
    }

    [TestMethod]
    public void Vote_StatusValidation_RejectsInvalidStatus()
    {
      // Arrange
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1,
        StatusCode = "INVALID_STATUS"
      };

      // Act & Assert
      var isValid = ValidateVoteStatus(vote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_BallotCountValidation_EnforcesMaxVotesPerBallot()
    {
      // Arrange - Create votes up to the limit (9 for LSA)
      for (int i = 0; i < 9; i++)
      {
        var person = new Person
        {
          FirstName = $"Person{i}",
          LastName = "Test",
          CanReceiveVotes = true
        }.ForTests();

        new Vote
        {
          SingleNameElectionCount = 1
        }.ForTests(_testBallot, person);
      }

      // Try to add one more vote (should fail)
      var extraPerson = new Person
      {
        FirstName = "Extra",
        LastName = "Person",
        CanReceiveVotes = true
      }.ForTests();

      var extraVote = new Vote
      {
        PersonGuid = extraPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateBallotVoteLimit(extraVote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_ElectionStatusValidation_PreventsFinalizedElectionChanges()
    {
      // Arrange
      _testElection.TallyStatus = ElectionTallyStatusEnum.Finalized;
      
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateElectionStatus(vote);
      isValid.ShouldEqual(false);
    }

    [TestMethod]
    public void Vote_ElectionStatusValidation_AllowsActiveElectionChanges()
    {
      // Arrange
      _testElection.TallyStatus = ElectionTallyStatusEnum.Tallying;
      
      var vote = new Vote
      {
        PersonGuid = _testPerson.PersonGuid,
        BallotGuid = _testBallot.BallotGuid,
        SingleNameElectionCount = 1
      };

      // Act & Assert
      var isValid = ValidateElectionStatus(vote);
      isValid.ShouldEqual(true);
    }

    private bool ValidateVote(Vote vote)
    {
      // Check if person exists
      var person = _dbContext.PersonFake.FirstOrDefault(p => p.PersonGuid == vote.PersonGuid);
      if (person == null) return false;

      // Check if ballot exists
      var ballot = _dbContext.BallotFake.FirstOrDefault(b => b.BallotGuid == vote.BallotGuid);
      if (ballot == null) return false;

      return true;
    }

    private bool ValidateVoteEligibility(Vote vote)
    {
      var person = _dbContext.PersonFake.FirstOrDefault(p => p.PersonGuid == vote.PersonGuid);
      return person?.CanReceiveVotes == true;
    }

    private bool ValidateVoteCount(Vote vote)
    {
      return vote.SingleNameElectionCount > 0;
    }

    private bool ValidateVoteDuplication(Vote vote)
    {
      var existingVote = _dbContext.VoteFake.FirstOrDefault(v => 
        v.PersonGuid == vote.PersonGuid && v.BallotGuid == vote.BallotGuid);
      return existingVote == null;
    }

    private bool ValidateVoteStatus(Vote vote)
    {
      var validStatuses = new[] { VoteStatusCode.Ok, VoteStatusCode.Spoiled, VoteStatusCode.Changed, VoteStatusCode.OnlineRaw };
      return validStatuses.Contains(vote.StatusCode);
    }

    private bool ValidateBallotVoteLimit(Vote vote)
    {
      var existingVotes = _dbContext.VoteFake.Where(v => v.BallotGuid == vote.BallotGuid).Count();
      return existingVotes < _testElection.NumberToElect;
    }

    private bool ValidateElectionStatus(Vote vote)
    {
      return _testElection.TallyStatus != ElectionTallyStatusEnum.Finalized;
    }
  }
}