using System.Linq;
using System.Net.Sockets;
using System.Web;
using TallyJ.EF;

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

    public static string TextFor(string value, string defaultValue = "")
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? (defaultValue ?? value) : item.DisplayText;
    }

    //public static string DisplayVotingMethodFor(Election currentElection, Person person)
    //{
    //  //p.VotingMethod.DefaultTo(VotingMethodEnum.InPerson) == VotingMethodEnum.InPerson ? null : p.EnvNum

    //  var item = BaseItems.SingleOrDefault(i => i.Value == person.VotingMethod);
    //  if (item == null)
    //  {
    //    // unknown!
    //    return person.VotingMethod;
    //  }
    //  if (item == InPerson)
    //  {
    //    // don't show anything for In Person voters
    //    return "";
    //  }

    //  var envNum = person.EnvNum.AsInt(0);
    //  var envNumText = envNum == 0 ? "?" : envNum.ToString();

    //  if (currentElection.MaskVotingMethod.AsBoolean())
    //  {
    //    return "Envelope " + envNumText;
    //  }

    //  return string.Format("{1} <span>{0}</span> {2}", item.DisplayText, envNumText, " ".PadRight(envNumText.Length).Replace(" ", "&nbsp;"));
    //}

    public static string MethodMap()
    {
      return BaseItems
          .Select(l => "{0}:{1}".FilledWith(l.Value.QuotedForJavascript(), l.Text.QuotedForJavascript()))
          .JoinedAsString(", ")
          .SurroundContentWith("{", "}");
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