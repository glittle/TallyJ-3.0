using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TallyJ.EF;

namespace TallyJ.Code.Enumerations
{
  public class ElectionTallyStatusEnum : BaseEnumeration<ElectionTallyStatusEnum, string>
  {
    //    <option value="NotStarted">Not started</option>
    //<option value="Tallying">Tally in Progress</option>
    //<option value="Reviewing">Reviewing Results</option>
    //<option value="Report">Reports Ready to Announce!</option>


    public static readonly ElectionTallyStatusEnum NotStarted = new ElectionTallyStatusEnum("NotStarted", "Initial Setup");
    public static readonly ElectionTallyStatusEnum NamesReady = new ElectionTallyStatusEnum("NamesReady", "Before Collecting Ballots");
    public static readonly ElectionTallyStatusEnum Tallying = new ElectionTallyStatusEnum("Tallying", "Tallying Ballots");
    public static readonly ElectionTallyStatusEnum Reviewing = new ElectionTallyStatusEnum("Reviewing", "Reviewing");
    //public static readonly ElectionTallyStatusEnum TieBreakNeeded = new ElectionTallyStatusEnum("TieBreakNeeded", "Tie-Break Required");
    public static readonly ElectionTallyStatusEnum Report = new ElectionTallyStatusEnum("Report", "Review Complete");

    static ElectionTallyStatusEnum()
    {
      AddAsDefault(NotStarted);
      Add(NamesReady);
      Add(Tallying);
      Add(Reviewing);
      //Add(TieBreakNeeded);
      Add(Report);
    }

    public ElectionTallyStatusEnum(string key, string display)
      : base(key, display)
    {
    }

    public static HtmlString ForHtmlList(Election selected)
    {
      if (selected == null)
      {
        return ForHtmlList();
      }
      return ForHtmlList(selected.TallyStatus);
    }

    public static HtmlString ForHtmlList(string selected = "")
    {
      return
        BaseItems
          .Select(bi => "<li data-state='{0}' class='Active_{2}'>{1}</li>"
                          .FilledWith(bi.Value, bi.Text, bi.Value == selected))
          .JoinedAsString()
          .AsRawHtml();
    }

    public static string TextFor(string status)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == status);
      return item == null ? NotStarted : item.DisplayText;
    }
  }
}