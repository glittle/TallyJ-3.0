using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TallyJ.Code.Enumerations
{
  public class ElectionTypeEnum : BaseEnumeration<ElectionTypeEnum, string>
  {
    public static readonly ElectionTypeEnum Lsa = new ElectionTypeEnum("LSA", "Local Spiritual Assembly");
    public static readonly ElectionTypeEnum LsaC = new ElectionTypeEnum("LsaC", "Local Spiritual Assembly (Two-Stage) Central Listing for Local Units");
    public static readonly ElectionTypeEnum LsaU = new ElectionTypeEnum("LsaU", "Local Spiritual Assembly (Two-Stage) Local Unit", false);
    public static readonly ElectionTypeEnum LsaF = new ElectionTypeEnum("LsaF", "Local Spiritual Assembly (Two-Stage) Final");
    public static readonly ElectionTypeEnum Nsa = new ElectionTypeEnum("NSA", "National Spiritual Assembly");
    public static readonly ElectionTypeEnum Con = new ElectionTypeEnum("Con", "Unit Convention");
    public static readonly ElectionTypeEnum Reg = new ElectionTypeEnum("Reg", "Regional Council");
    public static readonly ElectionTypeEnum Tie = new ElectionTypeEnum("Tie", "Tie-Break", false);
    public static readonly ElectionTypeEnum Oth = new ElectionTypeEnum("Oth", "Other");

    static ElectionTypeEnum()
    {
      Add(Lsa);
      Add(LsaC);
      Add(LsaU);
      Add(LsaF);
      Add(Nsa);
      Add(Con);
      Add(Reg);
      Add(Tie);
      Add(Oth);
    }

    public ElectionTypeEnum(string key, string display, bool directlySelectable = true)
      : base(key, display)
    {
      DirectlySelectable = directlySelectable;
      ;
    }

    public bool DirectlySelectable { get; private set; }

    public static HtmlString ForHtmlSelect(string selected = "")
    {
      return
        BaseItems
          .Select(bi => $"<option value='{bi.Value}'{(bi.Value == selected ? " selected" : "")}{(bi.DirectlySelectable ? "" : " disabled")}>{bi.Text}</option>")
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