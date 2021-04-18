using System.Linq;
using System.Net.Mime;
using System.Net.Sockets;
using System.Web;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Code.Enumerations
{
  public class VotingMethodEnum : BaseEnumeration<VotingMethodEnum, string>
  {
    public static readonly VotingMethodEnum InPerson = new VotingMethodEnum("P", "In Person");
    public static readonly VotingMethodEnum DroppedOff = new VotingMethodEnum("D", "Dropped Off");
    public static readonly VotingMethodEnum MailedIn = new VotingMethodEnum("M", "Mailed In");
    public static readonly VotingMethodEnum CalledIn = new VotingMethodEnum("C", "Called In");
    public static readonly VotingMethodEnum Registered = new VotingMethodEnum("R", "Registered");
    public static readonly VotingMethodEnum Online = new VotingMethodEnum("O", "Online");
    public static readonly VotingMethodEnum Imported = new VotingMethodEnum("I", "Imported");
    public static readonly VotingMethodEnum Custom1 = new VotingMethodEnum("1", "Custom1");
    public static readonly VotingMethodEnum Custom2 = new VotingMethodEnum("2", "Custom2");
    public static readonly VotingMethodEnum Custom3 = new VotingMethodEnum("3", "Custom3");
    public static readonly VotingMethodEnum Unknown = new VotingMethodEnum("U", "Unknown");

    static VotingMethodEnum()
    {
      Add(InPerson);
      Add(DroppedOff);
      Add(MailedIn);
      Add(CalledIn);
      Add(Registered);
      Add(Online);
      Add(Custom1);
      Add(Custom2);
      Add(Custom3);
      Add(Imported);
      Add(Unknown);
    }

    public VotingMethodEnum(string key, string display)
        : base(key, display)
    {
    }

    public static string TextFor(string value, string defaultValue = "")
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      if (item == null)
      {
        return defaultValue ?? value;
      }

      return TextFor(item);
    }

    private static string TextFor(VotingMethodEnum item)
    {
      var text = item.DisplayText;

      var currentElection = UserSession.CurrentElection;
      if (currentElection == null)
      {
        return text;
      }

      switch (text)
      {
        case "Custom1":
          return currentElection.Custom1Name;
        case "Custom2":
          return currentElection.Custom2Name;
        case "Custom3":
          return currentElection.Custom3Name;
      }

      return text;
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

    public static HtmlString ForHtmlSelect(string selected = "")
    {
      return
          BaseItems
              .Select(bi => "<option value='{0}'{2}>{1}</option>"
                                .FilledWith(bi.Value, TextFor(bi), bi.Value == selected ? " selected" : ""))
              .JoinedAsString()
              .AsRawHtml();
    }

    public static string AsJsonObject()
    {
      return BaseItems
        .Select(l => "{0}:{1}"
          .FilledWith(l.Value.ToString().QuotedForJavascript(),
          TextFor(l).QuotedForJavascript()))
        .JoinedAsString(", ")
        .SurroundContentWith("{", "}");
    }

    public static bool Exists(string voteType)
    {
      return BaseItems.Any(i => i.Value == voteType);
    }
  }
}