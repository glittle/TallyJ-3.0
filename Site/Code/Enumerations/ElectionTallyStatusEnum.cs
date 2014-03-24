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
    public static readonly ElectionTallyStatusEnum NamesReady = new ElectionTallyStatusEnum("NamesReady", "Before Tallying");
    public static readonly ElectionTallyStatusEnum Tallying = new ElectionTallyStatusEnum("Tallying", "Tallying Ballots");
    public static readonly ElectionTallyStatusEnum Reviewing = new ElectionTallyStatusEnum("Reviewing", "Reviewing");
    //public static readonly ElectionTallyStatusEnum TieBreakNeeded = new ElectionTallyStatusEnum("TieBreakNeeded", "Tie-Break Required");
    public static readonly ElectionTallyStatusEnum Report = new ElectionTallyStatusEnum("Report", "Approved");

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

    public static HtmlString ForHtmlList(Election selected, bool showAll = true)
    {
      if (selected == null)
      {
        return ForHtmlList();
      }
      return ForHtmlList(selected.TallyStatus, showAll);
    }

    public new static IList<ElectionTallyStatusEnum> Items
    {
      get { return BaseItems; }
    }

    public static HtmlString ForHtmlList(string selected = "", bool showAll = true)
    {
      const string liTemplate = "<li data-state='{0}' class='Active_{2} {0}'>{1}</li>";
      var mainList = BaseItems
        .Where(bi => showAll || bi.Value == selected)
        .Select(bi => liTemplate.FilledWith(bi.Value, bi.Text, bi.Value == selected))
        .JoinedAsString();
     
      return (mainList + liTemplate.FilledWith("General", "Misc Pages", "General" == selected)).AsRawHtml();
    }

    public static string TextFor(string status)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == status);
      return item == null ? NotStarted : item.DisplayText;
    }
  }
}