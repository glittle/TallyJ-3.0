using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ComputerModel : DataConnectedModel
  {
    private static object _computerModelLock;

    private static object ComputerModelLock
    {
      get
      {
        return _computerModelLock ?? (_computerModelLock = new object());
      }
    }

    public Computer GetComputerForMe(Guid oldComputerGuid)
    {
      Computer computer;

      var computerCacher = new ComputerCacher();

      var locationGuid = UserSession.CurrentLocationGuid;
      var locationModel = new LocationModel();
      var hasMultiplePhysicalLocations = locationModel.HasMultiplePhysicalLocations;
      if (locationGuid == Guid.Empty && !hasMultiplePhysicalLocations)
      {
        // if only one location, learn what it is
        var locations = locationModel.GetLocations_Physical();
        // var locations = new LocationCacher(Db)
        //   .AllForThisElection
        //   .Where(l => l.Name != LocationModel.OnlineLocationName)
        //   .Where(l => l.Name != LocationModel.ImportedLocationName)
        //   .OrderBy(l => l.SortOrder).ToList();
        if (locations.Count == 0)
        {
          // missing location?  fix it
          var location = new Location
          {
            ElectionGuid = UserSession.CurrentElectionGuid,
            Name = "Main Location",
            LocationGuid = Guid.NewGuid()
          };
          Db.Location.Add(location);
          Db.SaveChanges();
          locationGuid = location.LocationGuid;
        }
        else
        {
          locationGuid = locations.First().LocationGuid;
        }

        UserSession.CurrentLocationGuid = locationGuid;
      }

      var allMyElectionGuids = new ElectionsListViewModel()
        .MyElections()
        .Select(e => e.ElectionGuid)
        .ToList();

      lock (ComputerModelLock)
      {
        var allComputersInThisElection = computerCacher.AllForThisElection;

        computer = allComputersInThisElection.FirstOrDefault(c => c.ComputerGuid == oldComputerGuid && c.ElectionGuid == UserSession.CurrentElectionGuid);
        if (computer == null)
        {
          computer = new Computer
          {
            ComputerGuid = Guid.NewGuid(),
            ComputerCode = DetermineNextFreeComputerCode(allComputersInThisElection.Select(c => c.ComputerCode).Distinct().OrderBy(s => s)),
            ElectionGuid = UserSession.CurrentElectionGuid,
            AllMyElections = allMyElectionGuids
          };
          computerCacher.UpdateComputer(computer);
        }

        computer.LastContact = DateTime.Now;
        computer.LocationGuid = locationGuid;
        computer.AuthLevel = UserSession.AuthLevel;
        computer.SessionId = HttpContext.Current.Session.SessionID;
      }

      UserSession.CurrentComputer = computer;

      return computer;
    }

    public Computer GetTempComputerForMe()
    {
      Computer computer;

      var computerCacher = new ComputerCacher();

      var allMyElectionGuids = new ElectionsListViewModel()
        .MyElections()
        .Select(e => e.ElectionGuid)
        .ToList();

      lock (ComputerModelLock)
      {
        computer = new Computer
        {
          ComputerGuid = Guid.NewGuid(),
          ComputerCode = "-",
          ElectionGuid = Guid.Empty,
          AllMyElections = allMyElectionGuids,
          LastContact = DateTime.Now,
          AuthLevel = UserSession.AuthLevel,
          SessionId = HttpContext.Current.Session.SessionID
        };

        computerCacher.UpdateComputer(computer);
      }

      UserSession.CurrentComputer = computer;

      return computer;
    }

    //    private void ClearOutOldComputerRecords()
    //    {
    //      // backward compatible... remove all records from database
    //
    //
    //      const int maxMinutesOfNoContact = 30;
    //      var computerCacher = new ComputerCacher(Db);
    //
    //      var now = DateTime.Now;
    //      var computers = computerCacher.AllForThisElection;
    //
    //      computerCacher.RemoveItemsAndSaveCache(
    //        computers.Where(c => !c.LastContact.HasValue
    //                             || (now - c.LastContact.Value).TotalMinutes > maxMinutesOfNoContact));
    //    }

    /// <Summary>Move this computer into this location (don't change the computer code)</Summary>
    public bool MoveCurrentComputerIntoLocation(int locationId)
    {
      var location = new LocationCacher(Db).AllForThisElection.SingleOrDefault(l => l.C_RowId == locationId);

      if (location == null)
      {
        // ignore the request
        return false;
      }

      var computer = UserSession.CurrentComputer;
      AssertAtRuntime.That(computer != null, "computer missing");
      AssertAtRuntime.That(computer.ElectionGuid == location.ElectionGuid, "can't switch elections");

      computer.LocationGuid = location.LocationGuid;
      new ComputerCacher().UpdateComputer(computer);

      SessionKey.CurrentLocationGuid.SetInSession(location.LocationGuid);

      // reset ballot #
      SessionKey.CurrentBallotId.SetInSession(0);
      if (UserSession.CurrentElection.IsSingleNameElection)
      {
        // for single name elections, only have one ballot per computer per location. (But if altered from a normal election to a single name election, may have multiple.)
        var ballotId =
          new BallotCacher(Db).AllForThisElection.Where(b => b.LocationGuid == location.LocationGuid
                                                           && b.ComputerCode == computer.ComputerCode)
            .OrderBy(b => b.BallotNumAtComputer)
            .Select(b => b.C_RowId).FirstOrDefault();
        if (ballotId != 0)
        {
          SessionKey.CurrentBallotId.SetInSession(ballotId);
        }
      }

      return true;
    }

    public string DetermineNextFreeComputerCode(IEnumerable<string> existingCodesSortedAsc)
    {
      // assign a new code
      var codeToUse = 'A';
      var twoDigit = false;
      var firstDigit = (char)('A' - 1);

      foreach (var computerCode in existingCodesSortedAsc)
      {
        char testChar;
        if (computerCode.Length == 2)
        {
          twoDigit = true;
          testChar = computerCode[1];
          firstDigit = computerCode[0];
        }
        else
        {
          testChar = computerCode[0];
        }
        if (testChar == codeToUse)
        {
          // push the answer to the next one
          codeToUse = (char)(codeToUse + 1);
          if (codeToUse == 'I' || codeToUse == 'L' || codeToUse == 'O')
          {
            codeToUse++;
          }
          if (codeToUse > 'Z')
          {
            twoDigit = true;
            codeToUse = 'A';
            firstDigit = (char)(firstDigit + 1);
          }
        }
      }
      if (codeToUse > 'Z')
      {
        return "" + firstDigit + (char)('A' - 1 + codeToUse - 'Z');
      }
      if (twoDigit)
      {
        return "" + firstDigit + codeToUse;
      }
      return "" + codeToUse;
    }

    // refresh computer and election info
    public void RefreshLastContact()
    {
      // if (UserSession.CurrentElectionGuid == Guid.Empty)
      if (!UserSession.IsKnownTeller)
      {
        return;
      }

      new ComputerCacher().UpdateLastContactOfCurrentComputer();
    }

    public bool ProcessPulse()
    {
      new ComputerCacher().UpdateLastContactOfCurrentComputer();
      return true;
    }

    //public void RemoveComputerRecord()
    //{
    //  var computer = UserSession.CurrentComputer;
    //  if (computer != null)
    //  {
    //    new ComputerCacher(Db).RemoveItemAndSaveCache(computer);
    //  }
    //}
    public const string ComputerCodeForOnline = "OL";
    public const string ComputerCodeForImported = "IM";
  }
}