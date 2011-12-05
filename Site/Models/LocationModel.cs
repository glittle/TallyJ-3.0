using System;
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
      var ballotInfo = BallotModelFactory.GetForCurrentElection().GetCurrentBallotInfo(false);
      if (ballotInfo == null)
      {
        return null;
      }
      var location = Db.Locations.Single(l => l.LocationGuid == ballotInfo.LocationGuid);

      return LocationInfoForJson(location);
    }

    public static object LocationInfoForJson(Location location)
    {
      return new
               {
                 Id = location.C_RowId,
                 location.TallyStatus,
                 location.ContactInfo,
                 location.Name
               };
    }


    public JsonResult UpdateContactInfo(string info)
    {
      var location = UserSession.CurrentLocation;
      Db.Locations.Attach(location);

      location.ContactInfo = info;

      Db.SaveChanges();

      return new { Saved = true }.AsJsonResult();
    }
  }
}