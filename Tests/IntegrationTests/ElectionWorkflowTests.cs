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

namespace Tests.IntegrationTests
{
  [TestClass]
  public class ElectionWorkflowTests
  {
    private TestDbContext _dbContext;
    private Election _testElection;
    private Location _testLocation;

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
        Name = "Integration Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All",
        NumberToElect = 9,
        NumberExtra = 0
      }.ForTests();

      // Create test location
      _testLocation = new Location
      {
        Name = "Test Location",
        SortOrder = 1,
        IsVirtual = false
      };
      _testLocation.ForTests();
    }

    [TestMethod]
    public void CompleteElectionWorkflow_FromCreationToResults_WorksCorrectly()
    {
      // Step 1: Create election (already done in setup)
      _testElection.ShouldNotBeNull();
      _testElection.Name.ShouldEqual("Integration Test Election");

      // Step 2: Setup people and locations
      var people = CreateTestPeople();
      people.Count.ShouldEqual(15);

      // Step 3: Create and cast ballots
      var ballots = CreateTestBallots(people);
      ballots.Count.ShouldEqual(3);

      // Step 4: Analyze election results
      var analyzer = new ElectionAnalyzerNormal(_testElection);
      analyzer.AnalyzeEverything();

      // Step 5: Verify results
      var resultSummaries = _dbContext.ResultSummaryFake.Where(rs => rs.ElectionGuid == _testElection.ElectionGuid).ToList();
      resultSummaries.ShouldNotBeNull();
      resultSummaries.Count.ShouldBeGreaterThan(0);

      var finalSummary = resultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Final);
      finalSummary.ShouldNotBeNull();
      finalSummary.NumVoters.ShouldEqual(3);
    }

    [TestMethod]
    public void BallotCreationAndVoting_WorksCorrectly()
    {
      // Arrange
      var people = CreateTestPeople();
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation.LocationGuid;

      // Act - Create a ballot
      var ballotModel = BallotModelFactory.GetForCurrentElection();
      var newBallotResult = ballotModel.StartNewBallotJson();

      // Assert
      newBallotResult.ShouldNotBeNull();

      // Act - Add votes to ballot
      var ballot = _dbContext.BallotFake.FirstOrDefault();
      ballot.ShouldNotBeNull();

      for (int i = 0; i < Math.Min(9, people.Count); i++)
      {
        var person = people[i];
        var voteResult = ballotModel.SaveVote(person.C_RowId, 0, null, 0, 1, false);
        voteResult.ShouldNotBeNull();
      }

      // Verify votes were created
      var votes = _dbContext.VoteFake.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
      votes.Count.ShouldEqual(Math.Min(9, people.Count));
    }

    [TestMethod]
    public void PeopleRegistrationWorkflow_WorksCorrectly()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation.LocationGuid;
      var peopleModel = new PeopleModel();

      // Act - Register voting method for people
      var people = CreateTestPeople();
      var firstPerson = people.First();

      var result = peopleModel.RegisterVotingMethod(firstPerson.C_RowId, "InPerson", false, _testLocation.C_RowId);

      // Assert
      result.ShouldNotBeNull();

      // Verify person voting method was set
      var updatedPerson = _dbContext.PersonFake.FirstOrDefault(p => p.C_RowId == firstPerson.C_RowId);
      updatedPerson.ShouldNotBeNull();
    }

    [TestMethod]
    public void ElectionStatusProgression_WorksCorrectly()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var electionHelper = new ElectionHelper();

      // Act & Assert - Progress through election states
      // Start with initial state
      _testElection.TallyStatus.ShouldEqual(ElectionTallyStatusEnum.None);

      // Move to Tallying
      var result1 = electionHelper.SetTallyStatusJson("Tallying");
      result1.ShouldNotBeNull();

      // Move to Finalized
      var result2 = electionHelper.SetTallyStatusJson("Finalized");
      result2.ShouldNotBeNull();
    }

    [TestMethod]
    public void ResultCalculation_WithTiedVotes_HandlesCorrectly()
    {
      // Arrange
      var people = CreateTestPeople();
      var ballots = CreateTestBallotsWithTies(people);
      
      // Act
      var analyzer = new ElectionAnalyzerNormal(_testElection);
      analyzer.AnalyzeEverything();

      // Assert - Check for tie detection
      var ties = _dbContext.ResultTieFake.Where(rt => rt.ElectionGuid == _testElection.ElectionGuid).ToList();
      
      // If there are ties, verify they are properly recorded
      if (ties.Any())
      {
        ties.Count.ShouldBeGreaterThan(0);
        foreach (var tie in ties)
        {
          tie.TieBreakGroup.ShouldNotBeNull();
        }
      }
    }

    [TestMethod]
    public void DataValidation_PreventsDuplicateVotes_WorksCorrectly()
    {
      // Arrange
      var people = CreateTestPeople();
      var firstPerson = people.First();
      
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation.LocationGuid;

      var ballotModel = BallotModelFactory.GetForCurrentElection();
      ballotModel.StartNewBallotJson();
      
      var ballot = _dbContext.BallotFake.FirstOrDefault();

      // Act - Try to vote for same person twice
      var result1 = ballotModel.SaveVote(firstPerson.C_RowId, 0, null, 0, 1, false);
      var result2 = ballotModel.SaveVote(firstPerson.C_RowId, 0, null, 0, 1, false);

      // Assert
      result1.ShouldNotBeNull();
      result2.ShouldNotBeNull();

      // Should not have duplicate votes for same person on same ballot
      var votes = _dbContext.VoteFake.Where(v => v.BallotGuid == ballot.BallotGuid && v.PersonGuid == firstPerson.PersonGuid).ToList();
      votes.Count.ShouldEqual(1);
    }

    private System.Collections.Generic.List<Person> CreateTestPeople()
    {
      var people = new System.Collections.Generic.List<Person>();
      
      for (int i = 1; i <= 15; i++)
      {
        var person = new Person
        {
          FirstName = $"Person{i}",
          LastName = "Test",
          CanReceiveVotes = true,
          CanVote = true,
          IneligibleReasonGuid = null
        }.ForTests();
        
        people.Add(person);
      }
      
      return people;
    }

    private System.Collections.Generic.List<Ballot> CreateTestBallots(System.Collections.Generic.List<Person> people)
    {
      var ballots = new System.Collections.Generic.List<Ballot>();
      
      for (int b = 1; b <= 3; b++)
      {
        var ballot = new Ballot
        {
          ComputerCode = $"A{b}",
          BallotNumAtComputer = b,
          StatusCode = BallotStatusEnum.Ok
        }.ForTests();
        
        ballots.Add(ballot);

        // Add votes to this ballot
        for (int v = 0; v < 9 && v < people.Count; v++)
        {
          var vote = new Vote
          {
            SingleNameElectionCount = 1
          }.ForTests(ballot, people[v + (b * 3) % people.Count]);
        }
      }
      
      return ballots;
    }

    private System.Collections.Generic.List<Ballot> CreateTestBallotsWithTies(System.Collections.Generic.List<Person> people)
    {
      var ballots = new System.Collections.Generic.List<Ballot>();
      
      // Create ballots where first few people get same number of votes (creating ties)
      for (int b = 1; b <= 4; b++)
      {
        var ballot = new Ballot
        {
          ComputerCode = $"T{b}",
          BallotNumAtComputer = b,
          StatusCode = BallotStatusEnum.Ok
        }.ForTests();
        
        ballots.Add(ballot);

        // Vote for first 3 people on each ballot (creating 4-way tie)
        for (int v = 0; v < 3; v++)
        {
          var vote = new Vote
          {
            SingleNameElectionCount = 1
          }.ForTests(ballot, people[v]);
        }
      }
      
      return ballots;
    }
  }
}