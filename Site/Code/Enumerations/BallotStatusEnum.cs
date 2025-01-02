using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class BallotStatusEnum : BaseEnumeration<BallotStatusEnum, string>
  {
    public static readonly BallotStatusEnum Ok = new("Ok", "Ok");
    public static readonly BallotStatusEnum Review = new("Review", "Needs Review");
    public static readonly BallotStatusEnum Verify = new("Verify", "Needs Verification");
    public static readonly BallotStatusEnum TooMany = new("TooMany", "Too Many");
    public static readonly BallotStatusEnum TooFew = new("TooFew", "Too Few");
    public static readonly BallotStatusEnum Dup = new("Dup", "Duplicate names");
    public static readonly BallotStatusEnum Empty = new("Empty", "Empty");
    public static readonly BallotStatusEnum OnlineRaw = new("Raw", "Raw - Teller to Finish");

    static BallotStatusEnum()
    {
      Add(Ok);
      Add(Review);
      Add(Verify);
      Add(TooMany);
      Add(TooFew);
      Add(Dup);
      Add(OnlineRaw);
      AddAsDefault(Empty);
    }

    public BallotStatusEnum(string key, string display)
      : base(key, display)
    {
    }

    public static string TextFor(string value)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? "" : item.DisplayText;
    }

    public static IEnumerable<object> Listing
    {
      get { return BaseItems.Select(i => new { v = i.Value, d = i.DisplayText }); }
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

    /// <Summary>Find the status that matches this string. If not found, default to something... use Review needed.</Summary>
    public static BallotStatusEnum Parse(string code)
    {
      return BaseItems.SingleOrDefault(i => i.Value == code) ?? Verify;
    }
  }
}