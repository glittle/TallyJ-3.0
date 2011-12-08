using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class PublicHomeViewModel : DataConnectedModel
  {
    public List<Election> TestElections()
    {
      var testElections = Db.Elections.ToList();

      var other2 = new ElectionModel().VisibleElectionInfo2();

      var other1 = new ElectionModel().VisibleElectionInfo1();

      return testElections;
    }

    /// <summary>
    ///     Get elections listed for public access requests
    /// </summary>
    /// <remarks>
    ///     Look for any listed in last x seconds. During this time, the main tellers' pulse will
    ///     have to reset the Listed time.  If the main teller does not want it listed, set the Listed time
    ///     to null.
    /// </remarks>
    public IDictionary<int, string> PublicElections
    {
      get
      {
        var model = new ElectionModel();

        var dictionary = model.VisibleElectionInfo1()
          .OrderBy(e => e.Name)
          .ToDictionary(e => e.C_RowId, e => e.Name);

        //var dictionary = new Dictionary<int, string>();

        if (dictionary.Count == 0)
        {
          dictionary.Add(0, "[No active elections]");
        }

        return dictionary;
      }
    }
  }
}