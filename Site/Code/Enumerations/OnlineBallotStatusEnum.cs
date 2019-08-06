using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class OnlineBallotStatusEnum : BaseEnumeration<OnlineBallotStatusEnum, string>
  {
    public static readonly OnlineBallotStatusEnum New = new OnlineBallotStatusEnum("New", "New");
    public static readonly OnlineBallotStatusEnum Pending = new OnlineBallotStatusEnum("Pending", "Created but not locked in");
    public static readonly OnlineBallotStatusEnum Ready = new OnlineBallotStatusEnum("Ready", "Ready to be Processed");
    public static readonly OnlineBallotStatusEnum Processed = new OnlineBallotStatusEnum("Processed", "Converted to regular ballot");

    static OnlineBallotStatusEnum()
    {
      Add(New);
      Add(Pending);
      Add(Ready);
      Add(Processed);
      AddAsDefault(New);
    }

    public OnlineBallotStatusEnum(string key, string display)
      : base(key, display)
    {
    }

    public static string TextFor(string value)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? "" : item.DisplayText;
    }

    public static IEnumerable<object> Listing
    {
      get { return BaseItems.Select(i => new { v = i.Value, d = i.DisplayText }); }
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

    /// <Summary>Find the status that matches this string. If not found, default to something... use Review needed.</Summary>
    public static OnlineBallotStatusEnum Parse(string code)
    {
      return BaseItems.SingleOrDefault(i => i.Value == code) ?? New;
    }


  }
}