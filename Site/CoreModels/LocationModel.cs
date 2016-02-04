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

    /// <Summary>List of Locations</Summary>
    public List<Location> MyLocations
    {
      get { return _locations ?? (_locations = new LocationCacher(Db).AllForThisElection); }
    }

    public Dictionary<Guid, int> LocationIdMap
    {
      get
      {
        return _idMap ?? (_idMap = MyLocations.ToDictionary(l => l.LocationGuid, l => l.C_RowId));
      }
    }
    public string LocationRowIdMap
    {
      get
      {
        return MyLocations
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
      get { return MyLocations.Count == 1 ? " disabled" : ""; }
    }

    /// <Summary>Does this election have more than one location?</Summary>
    public bool HasLocations
    {
      get { return MyLocations.Count > 1; }
    }

    public HtmlString GetLocationOptions(bool includeWhichIfNeeded = true)
    {
      var currentLocation = UserSession.CurrentLocation;
      var selected = 0;
      if (currentLocation != null)
      {
        selected = currentLocation.C_RowId;
      }
      
      return
        (
        (selected == 0 && includeWhichIfNeeded ? "<option value='-1'>Which Location?</option>" : "") +
        MyLocations
        .OrderBy(l => l.SortOrder)
        .Select(l => new { l.C_RowId, l.Name, Selected = l.C_RowId == selected ? " selected" : "" })
        .Select(l => "<option value={C_RowId}{Selected}>{Name}</option>".FilledWith(l))
        .JoinedAsString())
        .AsRawHtml();
    }

    /// <Summary>Does this page need to show the location selector?</Summary>
    public bool ShowLocationSelector(MenuHelper currentMenu)
    {
      return currentMenu.ShowLocationSelection && HasLocations;
    }

    public JsonResult UpdateStatus(int locationId, string status)
    {
      var locationCacher = new LocationCacher(Db);

      var location = MyLocations.SingleOrDefault(l => l.C_RowId == locationId);

      if (location == null)
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

      return LocationInfoForJson(UserSession.CurrentLocation);
    }

    public object LocationInfoForJson(Location location)
    {
      if (location == null) {
        return null;
      }

      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
      var sum = new BallotHelper().BallotCount(location.LocationGuid, isSingleName);

      return new
      {
        Id = location.C_RowId,
        TallyStatus = location == null ? "" : LocationStatusEnum.TextFor(location.TallyStatus),
        TallyStatusCode = location.TallyStatus,
        location.ContactInfo,
        location.BallotsCollected,
        location.Name,
        BallotsEntered = sum
      };
    }

    public JsonResult UpdateNumCollected(int numCollected)
    {
      var location = UserSession.CurrentLocation;
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

    public JsonResult EditLocation(int id, string text)
    {
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
        Db.Location.Attach(location);
      }

      int locationId;
      string locationText;
      string status;
      var success = false;

      if (text.HasNoContent() && location.C_RowId > 0)
      {
        // don't delete last location
        if (MyLocations.Count() > 1)
        {
          // delete existing if we can
          var used = new BallotCacher(Db).AllForThisElection.Any(b => b.LocationGuid == location.LocationGuid);
          if (!used)
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

    public JsonResult SortLocations(List<int> idList)
    {
      //var ids = idList.Split(new[] { ',' }).AsInts().ToList();

      var locationCacher = new LocationCacher(Db);

      var locations = locationCacher.AllForThisElection.Where(l => idList.Contains(l.C_RowId)).ToList();

      var sortOrder = 1;
      foreach (var id in idList)
      {
        var newOrder = sortOrder++;

        var location = locations.SingleOrDefault(l => l.C_RowId == id);

        if (location != null && location.SortOrder != newOrder)
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