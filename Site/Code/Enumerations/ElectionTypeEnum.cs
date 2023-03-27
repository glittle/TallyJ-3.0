using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations;

public class ElectionTypeEnum : BaseEnumeration<ElectionTypeEnum, string>
{
  public static readonly ElectionTypeEnum LSA = new("LSA", "Local Spiritual Assembly");

  public static readonly ElectionTypeEnum LSAC = new("LSAC",
    "Voter Management for Units in Two-Stage LSA Election");

  public static readonly ElectionTypeEnum
    LSAU = new("LSAU", "Unit Election in Two-Stage LSA Election", false); // was Lsa1

  // public static readonly ElectionTypeEnum LSAF = new("LSAF", "Two-Stage Local Spiritual Assembly - Final"); // was Lsa2

  public static readonly ElectionTypeEnum NSA = new("NSA", "National Spiritual Assembly");
  public static readonly ElectionTypeEnum Con = new("Con", "Unit Convention");
  public static readonly ElectionTypeEnum Reg = new("Reg", "Regional Council");
  public static readonly ElectionTypeEnum Tie = new("Tie", "Tie-Break", false);
  public static readonly ElectionTypeEnum Oth = new("Oth", "Other");

  static ElectionTypeEnum()
  {
    Add(LSA);
    Add(LSAC);
    Add(LSAU);
    // Add(LSAF);
    Add(NSA);
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

  public bool DirectlySelectable { get; }

  public static HtmlString ForHtmlSelect(string selected = "")
  {
    return
      BaseItems
        .Select(bi =>
          $"<option value='{bi.Value}'{(bi.Value == selected ? " selected" : "")}{(bi.DirectlySelectable ? "" : " disabled")}>{bi.Text}</option>")
        .JoinedAsString()
        .AsRawHtml();
  }

  public static string LockedForJs => BaseItems.Where(i => !i.DirectlySelectable).Select(i => i.Value).JoinedAsString(",", "'", "'");

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