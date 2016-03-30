using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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


    public static readonly ElectionTallyStatusEnum NotStarted = new ElectionTallyStatusEnum("NotStarted", "Setup", true);
    public static readonly ElectionTallyStatusEnum NamesReady = new ElectionTallyStatusEnum("NamesReady", "Gathering Ballots", true);
    public static readonly ElectionTallyStatusEnum Tallying = new ElectionTallyStatusEnum("Tallying", "Processing Ballots", true);
    //public static readonly ElectionTallyStatusEnum Reviewing = new ElectionTallyStatusEnum("Reviewing", "Reviewing", false);
    //public static readonly ElectionTallyStatusEnum TieBreakNeeded = new ElectionTallyStatusEnum("TieBreakNeeded", "Tie-Break Required");
    public static readonly ElectionTallyStatusEnum Finalized = new ElectionTallyStatusEnum("Finalized", "Finalized", true);
    public bool Visible { get; private set; }

    static ElectionTallyStatusEnum()
    {
      AddAsDefault(NotStarted);
      Add(NamesReady);
      Add(Tallying);
      //Add(Reviewing);
      //Add(TieBreakNeeded);
      Add(Finalized);
    }

    public ElectionTallyStatusEnum(string key, string display, bool visible)
      : base(key, display)
    {
      Visible = visible;
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

    public static HtmlString ForHtmlList(string currentState = "", bool showAll = true)
    {
      const string liTemplate = "<span data-state='{0}' class='state Active_{2} {0}'>{1}</span>";
      var mainList = BaseItems
        .Where(bi => bi.Visible)
        .Where(bi => showAll || ShowAsSelected(bi, currentState))
        .Select(bi => liTemplate.FilledWith(bi.Value, bi.Text, ShowAsSelected(bi, currentState)))
        .JoinedAsString();
      return mainList.AsRawHtml();
    }

    private static bool ShowAsSelected(ElectionTallyStatusEnum testItem, string currentState)
    {
      if (testItem.Value == currentState)
      {
        return true;
      }
      //if (testItem == Tallying)
      //{
      //  return currentState == Finalized.Value;
      //}
      return false;
    }

    public static string TextFor(string status)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == status);
      return item == null ? NotStarted : item.DisplayText;
    }
  }
}