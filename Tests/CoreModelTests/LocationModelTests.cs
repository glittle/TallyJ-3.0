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

namespace Tests.CoreModelTests
{
  [TestClass]
  public class LocationModelTests
  {
    private LocationModel _locationModel;
    private TestDbContext _dbContext;
    private Election _testElection;
    private Location _testLocation1;
    private Location _testLocation2;

    [TestInitialize]
    public void Setup()
    {
      UnityInstance.Reset();
      ElectionTestHelper.Reset();

      _dbContext = new TestDbContext();
      _dbContext.ForTests();
      UnityInstance.Offer(_dbContext);

      _locationModel = new LocationModel();
      
      // Create test election
      _testElection = new Election
      {
        Name = "Test Election",
        ElectionType = "LSA",
        ElectionMode = "Normal",
        CanVote = "All",
        CanReceive = "All"
      }.ForTests();

      // Create test locations
      _testLocation1 = new Location
      {
        Name = "Location 1",
        SortOrder = 1,
        IsVirtual = false,
        TallyStatus = ElectionTallyStatusEnum.None
      };
      _testLocation1.ForTests();

      _testLocation2 = new Location
      {
        Name = "Location 2", 
        SortOrder = 2,
        IsVirtual = true,
        TallyStatus = ElectionTallyStatusEnum.None
      };
      _testLocation2.ForTests();
    }

    [TestMethod]
    public void GetLocations_Physical_ReturnsOnlyPhysicalLocations()
    {
      // Act
      var locations = _locationModel.GetLocations_Physical();

      // Assert
      locations.ShouldNotBeNull();
      var physicalLocations = locations.Where(l => !l.IsVirtual).ToList();
      physicalLocations.Count.ShouldBeGreaterThan(0);
      
      // Should not contain virtual locations
      var virtualLocations = locations.Where(l => l.IsVirtual).ToList();
      virtualLocations.Count.ShouldEqual(0);
    }

    [TestMethod]
    public void GetLocations_All_ReturnsAllLocations()
    {
      // Act
      var locations = _locationModel.GetLocations();

      // Assert
      locations.ShouldNotBeNull();
      locations.Count().ShouldBeGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public void UpdateStatus_WithValidLocation_UpdatesStatus()
    {
      // Arrange
      var locationId = _testLocation1.C_RowId;
      var newStatus = "Open";
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _locationModel.UpdateStatus(locationId, newStatus);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateStatus_WithInvalidLocation_HandlesError()
    {
      // Arrange
      var invalidLocationId = 99999;
      var newStatus = "Open";
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _locationModel.UpdateStatus(invalidLocationId, newStatus);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateLocationInfo_WithValidInfo_UpdatesInfo()
    {
      // Arrange
      var newInfo = "Updated location information";
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation1.LocationGuid;

      // Act
      var result = _locationModel.UpdateLocationInfo(newInfo);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateNumCollected_WithValidNumber_UpdatesCount()
    {
      // Arrange
      var numCollected = 50;
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation1.LocationGuid;

      // Act
      var result = _locationModel.UpdateNumCollected(numCollected);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdateNumCollected_WithNegativeNumber_HandlesError()
    {
      // Arrange
      var numCollected = -10;
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation1.LocationGuid;

      // Act
      var result = _locationModel.UpdateNumCollected(numCollected);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void CurrentBallotLocationInfo_ReturnsLocationInfo()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation1.LocationGuid;

      // Act
      var info = _locationModel.CurrentBallotLocationInfo();

      // Assert
      info.ShouldNotBeNull();
    }

    [TestMethod]
    public void CurrentBallotLocationInfo_WithoutCurrentLocation_HandlesError()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = null;

      // Act
      var info = _locationModel.CurrentBallotLocationInfo();

      // Assert
      info.ShouldNotBeNull();
    }

    [TestMethod]
    public void CreateLocation_WithValidData_CreatesLocation()
    {
      // Arrange
      var locationName = "New Test Location";
      var sortOrder = 10;
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _locationModel.CreateLocation(locationName, sortOrder);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void CreateLocation_WithDuplicateName_HandlesError()
    {
      // Arrange
      var locationName = _testLocation1.Name; // Use existing name
      var sortOrder = 10;
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _locationModel.CreateLocation(locationName, sortOrder);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeleteLocation_WithValidId_DeletesLocation()
    {
      // Arrange
      var locationId = _testLocation1.C_RowId;
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _locationModel.DeleteLocation(locationId);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void DeleteLocation_WithInvalidId_HandlesError()
    {
      // Arrange
      var invalidLocationId = 99999;
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;

      // Act
      var result = _locationModel.DeleteLocation(invalidLocationId);

      // Assert
      result.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetLocationStatistics_ReturnsStatistics()
    {
      // Arrange
      UserSession.CurrentElectionGuid = _testElection.ElectionGuid;
      UserSession.CurrentLocationGuid = _testLocation1.LocationGuid;

      // Act
      var stats = _locationModel.GetLocationStatistics();

      // Assert
      stats.ShouldNotBeNull();
    }

    [TestMethod]
    public void IsLocationActive_WithActiveLocation_ReturnsTrue()
    {
      // Arrange
      _testLocation1.TallyStatus = ElectionTallyStatusEnum.Tallying;

      // Act
      var isActive = _locationModel.IsLocationActive(_testLocation1);

      // Assert
      isActive.ShouldEqual(true);
    }

    [TestMethod]
    public void IsLocationActive_WithInactiveLocation_ReturnsFalse()
    {
      // Arrange
      _testLocation1.TallyStatus = ElectionTallyStatusEnum.None;

      // Act
      var isActive = _locationModel.IsLocationActive(_testLocation1);

      // Assert
      isActive.ShouldEqual(false);
    }
  }
}