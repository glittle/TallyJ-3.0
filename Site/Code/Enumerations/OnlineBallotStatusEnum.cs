using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class OnlineBallotStatusEnum : BaseEnumeration<OnlineBallotStatusEnum, string>
  {
    public static readonly OnlineBallotStatusEnum New = new("New", "New");
    public static readonly OnlineBallotStatusEnum Draft = new("Draft", "Draft");
    public static readonly OnlineBallotStatusEnum Submitted = new("Submitted", "Submitted");
    public static readonly OnlineBallotStatusEnum Processed = new("Processed", "Processed by tellers");

    static OnlineBallotStatusEnum()
    {
      Add(Draft);
      Add(Submitted);
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

    public static string AsJsonObject()
    {
      return BaseItems
        .Select(l => "{0}:{1}".FilledWith(l.Value.ToString().QuotedForJavascript(), l.Text.QuotedForJavascript()))
        .JoinedAsString(", ")
        .SurroundContentWith("{", "}");
    }
  }
}