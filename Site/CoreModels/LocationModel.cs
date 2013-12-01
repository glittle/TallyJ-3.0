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

    /// <Summary>List of Locations</Summary>
    public List<Location> Locations
    {
      get
      {
        return _locations ?? (_locations = Location.AllLocationsCached.ToList());
      }
    }

    public string ShowDisabled
    {
      get { return Locations.Count == 1 ? " disabled" : ""; }
    }

    public HtmlString GetLocationOptions()
    {
      var currentLocation = UserSession.CurrentLocation;
      var selected = 0;
      if (currentLocation != null)
      {
        selected = currentLocation.C_RowId;
      }

      return Locations
        .OrderBy(l => l.SortOrder)
        .Select(l => new { l.C_RowId, l.Name, Selected = l.C_RowId == selected ? " selected" : "" })
        .Select(l => "<option value={C_RowId}{Selected}>{Name}</option>".FilledWith(l))
        .JoinedAsString()
        .AsRawHtml();
    }

    /// <Summary>Does this page need to show the location selector?</Summary>
    public bool ShowLocationSelector(MenuHelper currentMenu)
    {
      return currentMenu.ShowLocationSelection && HasLocations;
    }

    /// <Summary>Does this election have more than one location?</Summary>
    public bool HasLocations
    {
      get { return Locations.Count > 1; }
    }

    public JsonResult UpdateStatus(int locationId, string status)
    {
      var location = Locations.SingleOrDefault(l => l.C_RowId == locationId);

      if (location == null)
      {
        return new
            {
              Saved = false
            }.AsJsonResult();
      }

      location.TallyStatus = status;

      Db.SaveChanges();

      SessionKey.CurrentLocation.SetInSession(location);

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
      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
      var sum = BallotModelCore.BallotCount(location.LocationGuid, isSingleName);

      return new
               {
                 Id = location.C_RowId,
                 TallyStatus = LocationStatusEnum.TextFor(location.TallyStatus),
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

      Location.DropCachedLocations();

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

      Location.DropCachedLocations();

      return new { Saved = true }.AsJsonResult();
    }

    public JsonResult EditLocation(int id, string text)
    {
      var location = Locations.SingleOrDefault(l => l.C_RowId == id);
      var changed = false;

      if (location == null)
      {
        location = new Location { ElectionGuid = UserSession.CurrentElectionGuid, LocationGuid = Guid.NewGuid() };
        Db.Location.Add(location);
        changed = true;
      }

      int locationId;
      string locationText;
      string status;
      var success = false;

      if (text.HasNoContent() && location.C_RowId != 0)
      {
        if (Locations.Count() > 1)
        {
          // delete existing if we can
          var used = Db.Ballot.Any(b => b.LocationGuid == location.LocationGuid);
          if (!used)
          {
            Db.Location.Remove(location);
            Db.SaveChanges();
            changed = true;

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
        { // only one
          status = "At least one location is required";
          locationId = location.C_RowId;
          locationText = location.Name;
        }
      }
      else if (text.HasContent())
      {
        location.Name = text;
        Db.SaveChanges();
        changed = true;

        status = "Saved";
        locationId = location.C_RowId;
        locationText = location.Name;
        success = true;
      }
      else
      {
        status= "Nothing to save";
        locationId = 0;
        success = true;
        locationText = "";
      }

      if (changed)
      {
        Location.DropCachedLocations();
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

      var locations = Locations.Where(l => idList.Contains(l.C_RowId)).ToList();

      var sortOrder = 1;
      foreach (var id in idList)
      {
        var location = locations.SingleOrDefault(l => l.C_RowId == id);
        if (location != null)
        {
          location.SortOrder = sortOrder++;
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