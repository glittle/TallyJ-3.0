using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TallyJ.Code.Enumerations
{
  public class ElectionModelEnum : BaseEnumeration<ElectionModelEnum, string>
  {
    public static readonly ElectionModelEnum Normal = new ElectionModelEnum("N", "Normal");
    public static readonly ElectionModelEnum TieBreak = new ElectionModelEnum("T", "Tie Break");
    public static readonly ElectionModelEnum Parent = new ElectionModelEnum("P", "Central for Local Units");
    public static readonly ElectionModelEnum Child = new ElectionModelEnum("C", "Local Unit");

    static ElectionModelEnum()
    {
      Add(Normal);
      Add(TieBreak);
      Add(Parent);
      Add(Child);
    }

    public ElectionModelEnum(string key, string display)
      : base(key, display)
    {
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

    public static string AsJsonObject()
    {
      return BaseItems
        .Select(l => "{0}:{1}".FilledWith(l.Value.ToString().QuotedForJavascript(), l.Text.QuotedForJavascript()))
        .JoinedAsString(", ")
        .SurroundContentWith("{", "}");
    }

    public static string TextFor(string electionType)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == electionType);
      return item == null ? "" : item.DisplayText;
    }
  }
}