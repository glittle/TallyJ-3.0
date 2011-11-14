using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ComputerModel : DataAccessibleModel
  {
    public Computer CreateComputerRecord()
    {
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

    public void AddComputerIntoElection(int computerRowId, Guid electionGuid)
    {
      var dbSet = Db.Computers;

      var computer = dbSet.Where(c => c.C_RowId == computerRowId).SingleOrDefault();

      if (computer == null)
      {
        computer = new ComputerModel().CreateComputerRecord();
        UserSession.ComputerRowId = computer.C_RowId;
      }

      computer.ElectionGuid = electionGuid;
      computer.ComputerCode =
        DetermineNextFreeComputerCode(
          dbSet.Where(c => c.ElectionGuid == electionGuid).OrderBy(c => c.ComputerCode).Select(c => c.ComputerCode));

      Db.SaveChanges();
    }

    public string DetermineNextFreeComputerCode(IEnumerable<string> existingCodesSortedAsc)
    {
      var code = 'A';
      var twoDigit = false;
      var firstDigit = (char) ('A' - 1);

      foreach (var computerCode in existingCodesSortedAsc)
      {
        var testChar = ' ';
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
          code = (char) (code + 1);
          if (code > 'Z')
          {
            twoDigit = true;
            code = 'A';
            firstDigit = (char) (firstDigit + 1);
          }
        }
      }
      if (code > 'Z')
      {
        return "" + firstDigit + (char) ('A' - 1 + code - 'Z');
      }
      if (twoDigit)
      {
        return "" + firstDigit + code;
      }
      return "" + code;
    }

    public bool ProcessPulse()
    {
      var computer = Db.Computers.Where(c => c.C_RowId == UserSession.ComputerRowId).SingleOrDefault();

      if (computer == null)
      {
        return false;
      }
      if (computer.ElectionGuid != UserSession.CurrentElectionGuid)
      {
        return false;
      }

      computer.LastContact = DateTime.Now;
      Db.SaveChanges();

      return true;
    }

    public void DeleteAtLogout(int computerRowId)
    {
      //TODO: should this be deleted at logout??
    }
  }
}