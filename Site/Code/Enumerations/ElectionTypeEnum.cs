using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class ElectionTypeEnum : BaseEnumeration<ElectionTypeEnum, string>
  {
    public static readonly ElectionTypeEnum Lsa = new ElectionTypeEnum("LSA", "Local Spiritual Assembly");
    public static readonly ElectionTypeEnum Nsa = new ElectionTypeEnum("NSA", "National Spritual Assembly");
    public static readonly ElectionTypeEnum Con = new ElectionTypeEnum("Con", "Unit Convention");
    public static readonly ElectionTypeEnum Reg = new ElectionTypeEnum("Reg", "Regional Council");
    public static readonly ElectionTypeEnum Oth = new ElectionTypeEnum("Oth", "Other");

    static ElectionTypeEnum()
    {
      Add(Lsa);
      Add(Nsa);
      Add(Con);
      Add(Reg);
      Add(Oth);
    }

    public ElectionTypeEnum(string key, string display)
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

    public static string TextFor(string electionType)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == electionType);
      return item == null ? "" : item.DisplayText;
    }
  }
}