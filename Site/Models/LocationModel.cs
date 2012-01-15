using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
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
      var ballots = Db.vLocationInfoes.Where(l => l.LocationGuid == location.LocationGuid).Sum(l => l.Ballots);

      return new
               {
                 Id = location.C_RowId,
                 location.TallyStatus,
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
      var location =
        Db.Locations.SingleOrDefault(l => l.C_RowId == id && l.ElectionGuid == UserSession.CurrentElectionGuid);
      if (location == null)
      {
        location = new Location { ElectionGuid = UserSession.CurrentElectionGuid, LocationGuid = Guid.NewGuid() };
        Db.Locations.Add(location);
      }

      location.Name = text;

      Db.SaveChanges();

      return new
               {
                 Id = location.C_RowId
               }.AsJsonResult();
    }

    public JsonResult SortLocations(string idList)
    {
      var ids = idList.Split(new[] { ',' }).AsInts().ToList();

      var locations =
        Db.Locations.Where(l => ids.Contains(l.C_RowId) && l.ElectionGuid == UserSession.CurrentElectionGuid);

      var sortOrder = 1;
      foreach (var id in ids)
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