using System.Linq;

namespace TallyJ.Code.Enumerations
{
  public class VoterIdTypeEnum : BaseEnumeration<VoterIdTypeEnum, string>
  {
    public static readonly VoterIdTypeEnum Email = new VoterIdTypeEnum("E", "Email Address", "email", "an email");
    public static readonly VoterIdTypeEnum Phone = new VoterIdTypeEnum("P", "Mobile Phone Number", "text message", "a text message");

    static VoterIdTypeEnum()
    {
      Add(Email);
      Add(Phone);
    }

    public VoterIdTypeEnum(string value, string displayText, string messageType, string messageTypeA)
      : base(value, displayText)
    {
      MessageType = messageType;
      MessageTypeA = messageTypeA;
    }

    public string MessageTypeA { get; set; }

    public string MessageType { get; set; }

    public static string TextFor(string value, string defaultValue = "")
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? (defaultValue ?? value) : item.DisplayText;
    }

    public static string MessageTypeFor(string value, string defaultValue = "")
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? (defaultValue ?? value) : item.MessageType;
    }

    public static string MessageTypeAFor(string value, string defaultValue = "")
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? (defaultValue ?? value) : item.MessageTypeA;
    }

  }
}