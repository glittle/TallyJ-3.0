using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class VotingMethodEnum : BaseEnumeration<VotingMethodEnum, string>
  {
    public static readonly VotingMethodEnum InPerson = new VotingMethodEnum("P", "In Person");
    public static readonly VotingMethodEnum DroppedOff = new VotingMethodEnum("D", "Dropped Off");
    public static readonly VotingMethodEnum MailedIn = new VotingMethodEnum("M", "Mailed In");
    public static readonly VotingMethodEnum CalledIn = new VotingMethodEnum("C", "Called In"); // C = Called In

       static VotingMethodEnum()
    {
      Add(InPerson);
      Add(DroppedOff);
      Add(MailedIn);
      Add(CalledIn);
    }

    public VotingMethodEnum(string key, string display)
      : base(key, display)
    {
    }

    public static string TextFor(string value)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? value : item.DisplayText;
    }

    public static string PresentAbsentTextFor(string value)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      if (item == null)
      {
        return value;
      }
      if (item == InPerson)
      {
        return "";
        //return "Present";
      }
      return "Envelope # ";
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

    public static bool Exists(string voteType)
    {
      return BaseItems.Any(i => i.Value == voteType);
    }
  }
}