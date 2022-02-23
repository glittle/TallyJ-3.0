using System.Linq;

namespace TallyJ.Code.Enumerations
{
  public class VoterIdTypeEnum : BaseEnumeration<VoterIdTypeEnum, string>
  {
    public static readonly VoterIdTypeEnum Email = new VoterIdTypeEnum("E", "Email Address", "email", "an email");
    public static readonly VoterIdTypeEnum Phone = new VoterIdTypeEnum("P", "Phone Number", "text message", "a text message");
    public static readonly VoterIdTypeEnum _unknown = new VoterIdTypeEnum("X", "(Unknown)", "unknown", "unknown");

    static VoterIdTypeEnum()
    {
      Add(_unknown);
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


    /// <Summary>Find the status that matches this string.</Summary>
    public static VoterIdTypeEnum Parse(string code)
    {
      if (code.HasNoContent()) return _unknown;
      code = code.ToUpper();
      return BaseItems.SingleOrDefault(i => code.StartsWith(i.Value)) ?? _unknown;
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