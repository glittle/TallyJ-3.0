using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class LocationModel : DataConnectedModel
  {
    private List<Location> _locations;
    private Dictionary<Guid, int> _idMap;

    public const string OnlineLocationName = "Online";
    public const string ImportedLocationName = "Imported";

    public LocationModel(ITallyJDbContext db) : base(db)
    {
    }

    public LocationModel()
    {
    }



    public List<Location> GetLocations_All()
    {
      var locations = _locations ?? (_locations = new LocationCacher(Db).AllForThisElection);
      return locations
        .OrderBy(l => l.SortOrder)
        .ToList();
    }

    public List<Location> GetLocations_Physical()
    {
      var locations = _locations ?? (_locations = new LocationCacher(Db).AllForThisElection);
      return locations
        .Where(l => !l.IsVirtual)
        .OrderBy(l => l.SortOrder)
        .ToList();
    }

    public List<Location> GetLocations_Virtual()
    {
      var locations = _locations ?? (_locations = new LocationCacher(Db).AllForThisElection);
      return locations
        .Where(l => l.IsVirtual)
        .OrderBy(l => l.SortOrder)
        .ToList();
    }

    public Dictionary<Guid, int> LocationIdMap
    {
      get
      {
        return _idMap ?? (_idMap = GetLocations_All().ToDictionary(l => l.LocationGuid, l => l.C_RowId));
      }
    }

    public string LocationRowIdMap
    {
      get
      {
        return GetLocations_All()
          .Select(l => "{0}:{1}".FilledWith(l.C_RowId, l.Name.SerializedAsJsonString()))
          .JoinedAsString(", ")
          .SurroundContentWith("{", "}");
      }
    }


    /// <summary>
    /// Get the RowId from a LocationGuid
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public int IdFor(Guid? guid, int defaultValue = 0)
    {
      if (!guid.HasValue)
      {
        return defaultValue;
      }
      if (LocationIdMap.ContainsKey(guid.Value))
      {
        return LocationIdMap[guid.Value];
      }
      return defaultValue;
    }

    public string ShowDisabled
    {
      get { return GetLocations_All().Count == 1 ? " disabled" : ""; }
    }

    /// <Summary>Does this election have more than one real location?</Summary>
    public bool HasMultiplePhysicalLocations
    {
      get { return GetLocations_Physical().Count > 1; }
    }

    public bool HasSomePhysicalLocations
    {
      get { return GetLocations_Physical().Count >= 1; }
    }

    public bool HasMultipleLocations
    {
      get { return GetLocations_All().Count > 1; }
    }

    public HtmlString GetLocationOptions(bool includeWhichIfNeeded = true, bool includeVirtual = false)
    {
      // for RollCall, don't need to show Online as an option to filter on

      var currentLocation = UserSession.CurrentLocation;
      var selected = 0;
      if (currentLocation != null)
      {
        selected = currentLocation.C_RowId;
      }

      return
        (
        (selected == 0 && includeWhichIfNeeded ? "<option value='-1'>Which Location?</option>" : "") +
        (includeVirtual ? GetLocations_All() : GetLocations_Physical())
        .OrderBy(l => l.IsVirtual)
        .ThenBy(l => l.SortOrder)
        .ThenBy(l => l.Name)
        .Select(l => new { l.C_RowId, l.Name, Selected = l.C_RowId == selected ? " selected" : "" })
        .Select(l => "<option value={C_RowId}{Selected}>{Name}</option>".FilledWith(l))
        .JoinedAsString())
        .AsRawHtml();
    }

    /// <Summary>Does this page need to show the location selector?</Summary>
    public bool ShowLocationSelector(MenuHelper currentMenu)
    {
      return currentMenu.ShowLocationSelection && HasMultipleLocations // HasMultiplePhysicalLocations
             || currentMenu.ShowLocationSelectionWithOnline && HasMultipleLocations;
    }

    public JsonResult UpdateStatus(int locationId, string status)
    {
      var locationCacher = new LocationCacher(Db);

      var location = GetLocations_Physical().SingleOrDefault(l => l.C_RowId == locationId);

      if (location == null)
      {
        return new
        {
          Saved = false
        }.AsJsonResult();
      }

      if (location.IsVirtual)
      {
        return new
        {
          Saved = false
        }.AsJsonResult();
      }

      if (location.TallyStatus != status)
      {
        Db.Location.Attach(location);
        location.TallyStatus = status;
        Db.SaveChanges();

        locationCacher.UpdateItemAndSaveCache(location);
      }

      return new
      {
        Saved = true,
        Location = LocationInfoForJson(location)
      }.AsJsonResult();
    }

    public string CurrentBallotLocationJsonString()
    {
      return CurrentBallotLocationInfo().SerializedAsJsonString();
    }

    public object CurrentBallotLocationInfo()
    {
      //var ballotInfo = BallotModelFactory.GetForCurrentElection().GetCurrentBallotInfo();
      //if (ballotInfo == null)
      //{
      //  return null;
      //}
      //var location = Db.Location.Single(l => l.LocationGuid == ballotInfo.LocationGuid);

      var currentLocation = UserSession.CurrentLocation;

      if (currentLocation == null)
      {
        currentLocation = new LocationModel().GetLocations_All().First();
      }

      return LocationInfoForJson(currentLocation);
    }

    public object LocationInfoForJson(Location location)
    {
      if (location == null)
      {
        return null;
      }

      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
      var sum = new BallotHelper().BallotCount(location.LocationGuid, isSingleName);

      return new
      {
        Id = location.C_RowId,
        TallyStatus = LocationStatusEnum.TextFor(location.TallyStatus),
        TallyStatusCode = location.TallyStatus,
        location.ContactInfo,
        location.BallotsCollected,
        location.Name,
        BallotsEntered = sum,
        IsOnline = location.IsTheOnlineLocation,
        IsImported = location.IsTheImportedLocation,
        location.IsVirtual,
        IsPhysical = !location.IsVirtual,

      };
    }

    public JsonResult UpdateNumCollected(int numCollected)
    {
      var location = UserSession.CurrentLocation;

      if (location == null)
      {
        return new { Message = "Must select your location first!" }.AsJsonResult();
      }

      Db.Location.Attach(location);

      location.BallotsCollected = numCollected;

      Db.SaveChanges();

      new LocationCacher(Db).UpdateItemAndSaveCache(location);

      return new
      {
        Saved = true,
        Location = LocationInfoForJson(location)
      }.AsJsonResult();
    }


    public JsonResult UpdateLocationInfo(string info)
    {
      var location = UserSession.CurrentLocation;
      Db.Location.Attach(location);

      location.ContactInfo = info;

      Db.SaveChanges();

      new LocationCacher(Db).UpdateItemAndSaveCache(location);

      return new { Saved = true }.AsJsonResult();
    }

    public Location GetOnlineLocation()
    {
      return GetLocations_All().FirstOrDefault(l => l.IsTheOnlineLocation);
    }

    public Location GetImportedLocation()
    {
      return GetLocations_All().FirstOrDefault(l => l.IsTheImportedLocation);
    }

    public JsonResult EditLocation(int id, string text, bool allowModifyOnline = false)
    {
      if (text == OnlineLocationName && !allowModifyOnline)
      {
        return new
        {
          Success = false,
          Status = $"Cannot name a location as \"{OnlineLocationName}\""
        }.AsJsonResult();
      }

      if (text == ImportedLocationName)
      {
        return new
        {
          Success = false,
          Status = $"Cannot name a location as \"{ImportedLocationName}\""
        }.AsJsonResult();
      }

      var locationCacher = new LocationCacher(Db);

      var location = locationCacher.AllForThisElection.SingleOrDefault(l => l.C_RowId == id);
      var changed = false;

      if (location == null)
      {
        location = new Location
        {
          ElectionGuid = UserSession.CurrentElectionGuid,
          LocationGuid = Guid.NewGuid()
        };
        Db.Location.Add(location);
        changed = true;
      }
      else
      {
        if (location.IsVirtual && !allowModifyOnline)
        {
          return new
          {
            Success = false,
            Status = "Cannot edit Online location"
          }.AsJsonResult();
        }

        Db.Location.Attach(location);
      }

      int locationId;
      string locationText;
      string status;
      var success = false;

      if (text.HasNoContent() && location.C_RowId > 0)
      {
        // deleting this location

        // don't delete last location
        if (location.IsTheOnlineLocation && allowModifyOnline
           || GetLocations_Physical().Count > 1)
        {
          // delete existing if we can
          if (!IsLocationInUse(location.LocationGuid))
          {
            Db.Location.Remove(location);
            Db.SaveChanges();
            locationCacher.RemoveItemAndSaveCache(location);

            status = "Deleted";
            success = true;
            locationId = 0;
            locationText = "";
          }
          else
          {
            status = "Cannot deleted this location because it has Ballots recorded in it";
            locationId = location.C_RowId;
            locationText = location.Name;
          }
        }
        else
        {
          // only one
          status = "At least one location is required";
          locationId = location.C_RowId;
          locationText = location.Name;
        }
      }
      else if (text.HasContent())
      {
        locationText = location.Name = text;
        locationId = location.C_RowId; // may be 0 if new

        changed = true;
        status = "Saved";
      }
      else
      {
        status = "Nothing to save";
        locationId = 0;
        locationText = "";
        success = true;
        changed = false;
      }

      if (changed)
      {
        Db.SaveChanges();

        locationId = location.C_RowId;
        locationCacher.UpdateItemAndSaveCache(location);
        success = true;
      }

      return new
      {
        // returns 0 if deleted or not created
        Id = locationId,
        Text = locationText,
        Success = success,
        Status = status
      }.AsJsonResult();
    }

    public bool IsLocationInUse(Guid locationGuid)
    {
      return new BallotCacher(Db).AllForThisElection.Any(b => b.LocationGuid == locationGuid);
    }

    public JsonResult SortLocations(List<int> idList)
    {
      //var ids = idList.Split(new[] { ',' }).AsInts().ToList();

      var locationCacher = new LocationCacher(Db);

      var locations = locationCacher.AllForThisElection
        .Where(l => idList.Contains(l.C_RowId))
        .ToList();

      var sortOrder = 1;
      foreach (var id in idList)
      {
        var location = locations.SingleOrDefault(l => l.C_RowId == id);
        if (location == null)
        {
          continue;
        }

        // keep "Online" at the bottom of this list.  Use Html/css to support this rule.
        var newOrder = location.IsTheOnlineLocation ? 90 : location.IsTheImportedLocation ? 91 : sortOrder++;

        if (location.SortOrder != newOrder)
        {
          Db.Location.Attach(location);
          location.SortOrder = newOrder;

          locationCacher.UpdateItemAndSaveCache(location);
        }
      }

      Db.SaveChanges();

      return new
      {
        Saved = true
      }.AsJsonResult();
    }
  }
}