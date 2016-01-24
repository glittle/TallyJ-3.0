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

    public Computer CreateAndSaveComputerForMe()
    {
      Computer computer;

      var computerCacher = new ComputerCacher(Db);

      var locationGuid = UserSession.CurrentLocationGuid;
      if (locationGuid == Guid.Empty)
      {
        UserSession.CurrentLocationGuid = locationGuid = new LocationCacher(Db).AllForThisElection.OrderBy(l => l.SortOrder).First().LocationGuid;
      }

      lock (ComputerModelLock)
      {
        var allComputers = computerCacher.AllForThisElection;

        computer = new Computer
        {
          ComputerGuid = Guid.NewGuid(),
          ComputerCode = DetermineNextFreeComputerCode(allComputers.Select(c => c.ComputerCode).Distinct().OrderBy(s => s)),
          LocationGuid = locationGuid,
          ElectionGuid = UserSession.CurrentElectionGuid,
          LastContact = DateTime.Now,
          AuthLevel = UserSession.AuthLevel,
          SessionId = HttpContext.Current.Session.SessionID
        };

        computerCacher.AddToCache(computer);
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

      new ComputerCacher(Db).UpdateComputerLocation(computer);

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
      if (UserSession.CurrentElectionGuid == Guid.Empty)
      {
        return;
      }

      new ComputerCacher(Db).UpdateLastContactOfCurrentComputer();
    }

    public bool ProcessPulse()
    {
      new ComputerCacher(Db).UpdateLastContactOfCurrentComputer();
      return true;
    }

    public void RemoveComputerRecord()
    {
      var computer = UserSession.CurrentComputer;
      if (computer != null)
      {
        new ComputerCacher(Db).RemoveItemAndSaveCache(computer);
      }
    }
  }
}