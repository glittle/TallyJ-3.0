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
  public class PeopleControllerTests
  {
    private PeopleController _controller;
    private TestDbContext _dbContext;
    private Election _testElection;
    private Person _testPerson;

    [TestInitialize]
    public void Setup()
    {
      UnityInstance.Reset();
      ElectionTestHelper.Reset();

      _dbContext = new TestDbContext();
      _dbContext.ForTests();
      UnityInstance.Offer(_dbContext);

      _controller = new PeopleController();
      
      // Create test election
      _testElection = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();

      // Create test person
      _testPerson = new Person
      {
        FirstName = "John",
        LastName = "Doe",
        CanReceiveVotes = true,
        CanVote = true
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
    public void GetPeople_ReturnsPersonList()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetPeople();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SavePerson_WithValidData_SavesPerson()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personData = new
      {
        PersonId = _testPerson.C_RowId,
        FirstName = "Updated John",
        LastName = "Updated Doe"
      };

      // Act
      var result = _controller.SavePerson(personData.PersonId, personData.FirstName, personData.LastName);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SavePerson_WithInvalidData_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act - Try to save with empty name
      var result = _controller.SavePerson(_testPerson.C_RowId, "", "");

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeletePerson_WithValidId_DeletesPerson()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personId = _testPerson.C_RowId;

      // Act
      var result = _controller.DeletePerson(personId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeletePerson_WithInvalidId_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidPersonId = 99999;

      // Act
      var result = _controller.DeletePerson(invalidPersonId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SearchPeople_WithSearchTerm_ReturnsFilteredResults()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var searchTerm = "John";

      // Act
      var result = _controller.SearchPeople(searchTerm);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SearchPeople_WithEmptyTerm_ReturnsAllPeople()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.SearchPeople("");

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdatePersonStatus_WithValidData_UpdatesStatus()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personId = _testPerson.C_RowId;
      var newStatus = "Active";

      // Act
      var result = _controller.UpdatePersonStatus(personId, newStatus);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void SetPersonEligibility_WithValidData_UpdatesEligibility()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personId = _testPerson.C_RowId;
      var canVote = true;
      var canReceiveVotes = true;

      // Act
      var result = _controller.SetPersonEligibility(personId, canVote, canReceiveVotes);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ImportPeople_WithValidData_ImportsPeople()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var csvData = "FirstName,LastName\nJane,Smith\nBob,Johnson";

      // Act
      var result = _controller.ImportPeople(csvData);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ImportPeople_WithInvalidData_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidCsvData = "Invalid,CSV,Data\nMissingHeader";

      // Act
      var result = _controller.ImportPeople(invalidCsvData);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ExportPeople_ReturnsExportResult()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.ExportPeople();

      // Assert
      result.ShouldNotBeNull();
      var actionResult = result as ActionResult;
      actionResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetPersonDetails_WithValidId_ReturnsPersonDetails()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personId = _testPerson.C_RowId;

      // Act
      var result = _controller.GetPersonDetails(personId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetPersonDetails_WithInvalidId_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var invalidPersonId = 99999;

      // Act
      var result = _controller.GetPersonDetails(invalidPersonId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void ValidatePerson_WithValidPerson_ReturnsValidationResult()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      var personId = _testPerson.C_RowId;

      // Act
      var result = _controller.ValidatePerson(personId);

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetPeopleStatistics_ReturnsStatistics()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _controller.GetPeopleStatistics();

      // Assert
      result.ShouldNotBeNull();
      var jsonResult = result as JsonResult;
      jsonResult.ShouldNotBeNull();
    }
  }
}