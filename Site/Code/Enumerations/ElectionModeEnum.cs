using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class ElectionModeEnum : BaseEnumeration<ElectionModeEnum, string>
  {
    public static readonly ElectionModeEnum Normal = new ElectionModeEnum("N", "Normal Election", true);
    public static readonly ElectionModeEnum Tie = new ElectionModeEnum("T", "Tie-Break");
    public static readonly ElectionModeEnum ByElection = new ElectionModeEnum("B", "By-election");

    static ElectionModeEnum()
    {
      Add(Normal);
      Add(Tie);
      Add(ByElection);
    }

    private bool _blankForDisplay;

    public ElectionModeEnum(string key, string display, bool blankForDisplay = false)
      : base(key, display)
    {
      _blankForDisplay = blankForDisplay;
    }

    public override IList<ElectionModeEnum> Items
    {
      get { return BaseItems; }
    }

    public static HtmlString ForHtmlSelect(string selected = "", Dictionary<string, string> extraAttribPerItem = null)
    {
      return
        BaseItems
          .Select(bi => "<option value='{0}'{2}>{1}</option>"
                          .FilledWith(
                            bi.Value,
                            bi.Text,
                            (bi.Value == selected ? " selected" : "")
                            + (extraAttribPerItem != null ? extraAttribPerItem.Where(kvp=>kvp.Key==bi.Value).Select(kvp=>kvp.Value).SingleOrDefault() : "")))
          .JoinedAsString()
          .AsRawHtml();
    }

    public static string TextFor(string electionType)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == electionType);
      return item == null ? "" : 
        item._blankForDisplay ? "" : item.DisplayText;
    }
  }
}