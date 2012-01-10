using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class ElectionTallyStatusEnum : BaseEnumeration<ElectionTallyStatusEnum, string>
  {
    //    <option value="NotStarted">Not started</option>
    //<option value="Tallying">Tally in Progress</option>
    //<option value="Reviewing">Reviewing Results</option>
    //<option value="Report">Reports Ready to Announce!</option>


    public static readonly ElectionTallyStatusEnum NotStarted = new ElectionTallyStatusEnum("NotStarted", "Not Started");
    public static readonly ElectionTallyStatusEnum Tallying = new ElectionTallyStatusEnum("Tallying", "Tally in Progress");
    public static readonly ElectionTallyStatusEnum Reviewing = new ElectionTallyStatusEnum("Reviewing", "Reviewing Results");
    public static readonly ElectionTallyStatusEnum Report = new ElectionTallyStatusEnum("Report", "Reports Ready to Announce!");

    static ElectionTallyStatusEnum()
    {
      AddAsDefault(NotStarted);
      Add(Tallying);
      Add(Reviewing);
      Add(Report);
    }

    public ElectionTallyStatusEnum(string key, string display)
      : base(key, display)
    {
    }

    public override IList<ElectionTallyStatusEnum> Items
    {
      get { return BaseItems; }
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
      return item == null ? "" : item.DisplayText;
    }
  }
}