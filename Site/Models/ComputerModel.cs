using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Web;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ComputerModel : DataConnectedModel
  {
    public Computer CreateComputerRecord()
    {
      ClearOutOldComputerRecords();

      SessionKey.CurrentElection.SetInSession<Election>(null);

      var computer = new Computer
                       {
                         ShadowElectionGuid = Guid.NewGuid(),
                         LastContact = DateTime.Now,
                         BrowserInfo = HttpContext.Current.Request.Browser.Type
                       };

      Db.Computers.Add(computer);
      Db.SaveChanges();

      return computer;
    }

    private void ClearOutOldComputerRecords()
    {
      const int maxMinutesOfNoContact = 5;

      var now = DateTime.Now;
      var oldComputers =
        Db.Computers.Where(c => SqlFunctions.DateDiff("n", c.LastContact.Value, now) > maxMinutesOfNoContact);

      foreach (var oldComputer in oldComputers)
      {
        Db.Computers.Remove(oldComputer);
      }
      Db.SaveChanges();
    }

    public bool AddCurrentComputerIntoLocation(int id)
    {
      var location =
        new ElectionModel().LocationsForCurrentElection.SingleOrDefault(
          l => l.C_RowId == id);

      var computer = Db.Computers.SingleOrDefault(c => c.C_RowId == UserSession.ComputerRowId);

      if (location == null || computer == null)
      {
        SessionKey.CurrentLocationGuid.SetInSession(Guid.Empty);
        return false;
      }

      SessionKey.CurrentLocationName.SetInSession(location.Name);
      SessionKey.CurrentLocationGuid.SetInSession(location.LocationGuid);
      computer.LocationGuid = location.LocationGuid;
      Db.SaveChanges();

      return true;
    }

    public void AddCurrentComputerIntoElection(Guid electionGuid)
    {
      var computer = Db.Computers.SingleOrDefault(c => c.C_RowId == UserSession.ComputerRowId);

      if (computer == null)
      {
        computer = CreateComputerRecord();
        UserSession.ComputerRowId = computer.C_RowId;
      }

      computer.ElectionGuid = electionGuid;
      computer.LocationGuid = null;
      SessionKey.CurrentLocationGuid.SetInSession(Guid.Empty);
      SessionKey.CurrentLocationName.SetInSession<string>(null);

      computer.ComputerCode =
        DetermineNextFreeComputerCode(
          Db.Computers.Where(c => c.ElectionGuid == electionGuid).OrderBy(c => c.ComputerCode).Select(
            c => c.ComputerCode));

      Db.SaveChanges();
    }

    public string DetermineNextFreeComputerCode(IEnumerable<string> existingCodesSortedAsc)
    {
      var code = 'A';
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
        if (testChar == code)
        {
          // push the answer to the next one
          code = (char)(code + 1);
          if (code > 'Z')
          {
            twoDigit = true;
            code = 'A';
            firstDigit = (char)(firstDigit + 1);
          }
        }
      }
      if (code > 'Z')
      {
        return "" + firstDigit + (char)('A' - 1 + code - 'Z');
      }
      if (twoDigit)
      {
        return "" + firstDigit + code;
      }
      return "" + code;
    }

    public bool ProcessPulse()
    {
      var computer = Db.Computers.SingleOrDefault(c => c.C_RowId == UserSession.ComputerRowId);

      if (computer == null)
      {
        return false;
      }

      computer.LastContact = DateTime.Now;
      Db.SaveChanges();

      if (computer.ElectionGuid != UserSession.CurrentElectionGuid)
      {
        return false;
      }

      return true;
    }

    public void DeleteAtLogout(int computerRowId)
    {
      //TODO: should this be deleted at logout??
    }
  }
}