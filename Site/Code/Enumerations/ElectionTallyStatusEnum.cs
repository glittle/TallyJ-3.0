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


    public static readonly ElectionTallyStatusEnum NotStarted = new ElectionTallyStatusEnum("NotStarted", "Setting Up");
    public static readonly ElectionTallyStatusEnum NamesReady = new ElectionTallyStatusEnum("NamesReady", "Election Day, Before Ballots Collected");
    public static readonly ElectionTallyStatusEnum Tallying = new ElectionTallyStatusEnum("Tallying", "Tellers Processing Ballots");
    public static readonly ElectionTallyStatusEnum Reviewing = new ElectionTallyStatusEnum("Reviewing", "Reviewing Results");
    public static readonly ElectionTallyStatusEnum TieBreakNeeded = new ElectionTallyStatusEnum("TieBreakNeeded", "Tie-Break Required");
    public static readonly ElectionTallyStatusEnum Report = new ElectionTallyStatusEnum("Report", "Results Ready to Announce!");

    static ElectionTallyStatusEnum()
    {
      AddAsDefault(NotStarted);
      Add(NamesReady);
      Add(Tallying);
      Add(Reviewing);
      Add(TieBreakNeeded);
      Add(Report);
    }

    public ElectionTallyStatusEnum(string key, string display)
      : base(key, display)
    {
    }

    public static HtmlString ForHtmlSelect(Election selected)
    {
      if (selected == null)
      {
        return ForHtmlSelect();
      }
      return ForHtmlSelect(selected.TallyStatus);
    }

    public static HtmlString ForHtmlSelect(string selected = "")
    {
      return
        BaseItems
          .Select(bi => "<option value='{0}'{2}>{1}</option>"
                          .FilledWith(bi.Value, bi.Text, bi.Value == selected ? " selected" : ""))
          .JoinedAsString()
          .AsRawHtml();
    }

    public static string TextFor(string electionType)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == electionType);
      return item == null ? NotStarted : item.DisplayText;
    }
  }
}