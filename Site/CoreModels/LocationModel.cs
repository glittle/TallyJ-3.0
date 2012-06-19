using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;


namespace TallyJ.CoreModels
{
  public class LocationModel : DataConnectedModel
  {
    /// <Summary>List of Locations</Summary>
    public IQueryable<Location> LocationsForCurrentElection
    {
      get
      {
        return
          Db.Locations
            .Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid);
      }
    }

    public JsonResult UpdateStatus(int locationId, string status)
    {
      var location = Db.Locations.SingleOrDefault(l => l.ElectionGuid == UserSession.CurrentElectionGuid && l.C_RowId == locationId);

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
      //var location = Db.Locations.Single(l => l.LocationGuid == ballotInfo.LocationGuid);

      return LocationInfoForJson(UserSession.CurrentLocation);
    }

    public object LocationInfoForJson(Location location)
    {
      var ballots = Db.vLocationInfoes.Where(l => l.LocationGuid == location.LocationGuid).Sum(l => l.BallotsAtComputer);

      return new
               {
                 Id = location.C_RowId,
                 TallyStatus = LocationStatusEnum.TextFor(location.TallyStatus),
                 TallyStatusCode = location.TallyStatus,
                 location.ContactInfo,
                 location.BallotsCollected,
                 location.Name,
                 BallotsEntered = ballots
               };
    }

    public JsonResult UpdateNumCollected(int numCollected)
    {
      var location = UserSession.CurrentLocation;
      Db.Locations.Attach(location);

      location.BallotsCollected = numCollected;

      Db.SaveChanges();

      return new
      {
        Saved = true,
        Location = LocationInfoForJson(location)
      }.AsJsonResult();
    }


    public JsonResult UpdateContactInfo(string info)
    {
      var location = UserSession.CurrentLocation;
      Db.Locations.Attach(location);

      location.ContactInfo = info;

      Db.SaveChanges();

      return new { Saved = true }.AsJsonResult();
    }

    public JsonResult EditLocation(int id, string text)
    {
      var locations = Db.Locations.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid).ToList();
      var location = locations.SingleOrDefault(l => l.C_RowId == id);

      if (location == null)
      {
        location = new Location { ElectionGuid = UserSession.CurrentElectionGuid, LocationGuid = Guid.NewGuid() };
        Db.Locations.Add(location);
      }

      int locationId;
      string locationText;
      string status;

      if (text.HasNoContent() && location.C_RowId != 0)
      {
        if (locations.Count() > 1)
        {
          // delete existing if we can
          var used = Db.Ballots.Any(b => b.LocationGuid == location.LocationGuid);
          if (!used)
          {
            Db.Locations.Remove(location);
            Db.SaveChanges();

            status = "Deleted";
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

        status = "Saved";
        locationId = location.C_RowId;
        locationText = location.Name;
      }
      else
      {
        status= "Nothing to save";
        locationId = 0;
        locationText = "";
      }

      return new
               {
                 // returns 0 if deleted or not created
                 Id = locationId,
                 Text = locationText,
                 Status = status
               }.AsJsonResult();
    }

    public JsonResult SortLocations(List<int> idList)
    {
      //var ids = idList.Split(new[] { ',' }).AsInts().ToList();

      var locations =
        Db.Locations.Where(l => idList.Contains(l.C_RowId) && l.ElectionGuid == UserSession.CurrentElectionGuid);

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