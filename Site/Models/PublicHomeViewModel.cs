using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NLog;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class PublicHomeViewModel : DataConnectedModel
  {
    public HtmlString VisibleElectionsOptions()
    {
      const string template = "<option value=\"{0}\">{1}</option>";
      var visibleElections = new ElectionModel().VisibleElections();
      var listing = visibleElections.OrderBy(e => e.Name).Select(x => template.FilledWith(x.C_RowId, x.Name)).JoinedAsString();
      return listing
        .DefaultTo(template.FilledWith(0, "(Sorry, no elections are active right now.)"))
        .AsRawHtml();
    }

    //private void TestLogging()
    //{
    //  //var logger = LogManager.GetCurrentClassLogger();
    //  //logger.WarnException("Test Warning", new ApplicationException("App Exception"));
    //}

    ///// <summary>
    /////     Get elections listed for public access requests
    ///// </summary>
    ///// <remarks>
    /////     Look for any listed in last x seconds. During this time, the main tellers' pulse will
    /////     have to reset the Listed time.  If the main teller does not want it listed, set the Listed time
    /////     to null.
    ///// </remarks>
    //public IDictionary<int, string> PublicElections
    //{
    //  get
    //  {
    //    var model = new ElectionModel();

    //    var dictionary = model.VisibleElectionInfo()
    //      .OrderBy(e => e.Name)
    //      .ToDictionary(e => e.C_RowId, e => e.Name);

    //    //var dictionary = new Dictionary<int, string>();

    //    if (dictionary.Count == 0)
    //    {
    //      dictionary.Add(0, "[No active elections]");
    //    }

    //    return dictionary;
    //  }
    //}
  }
}