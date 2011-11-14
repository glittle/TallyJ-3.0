using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Web;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class PublicHomeViewModel : DataAccessibleModel
  {
    /// <summary>
    /// Get elections listed for public access requests
    /// </summary>
    /// <remarks>Look for any listed in last x seconds. During this time, the main tellers' pulse will
    /// have to reset the Listed time.  If the main teller does not want it listed, set the Listed time
    /// to null.</remarks>
    public IDictionary<string, string> PublicElections
    {
      get
      {
        var now = DateTime.Now;
        var dictionary = Db.Elections
          .Where(e => SqlFunctions.DateDiff("s", e.ListedForPublicAsOf, now) < 60 && !string.IsNullOrEmpty(e.ElectionPasscode))
          .OrderBy(e => e.Name)
          .ToDictionary(e => e.ElectionPasscode, e=>e.Name);

        if (dictionary.Count == 0)
        {
          dictionary.Add("", "[Sorry, none available]");
        }

        return dictionary;
      }
    }
  }
}