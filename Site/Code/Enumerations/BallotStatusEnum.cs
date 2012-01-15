using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class BallotStatusEnum : BaseEnumeration<BallotStatusEnum, string>
  {
    public static readonly BallotStatusEnum Ok = new BallotStatusEnum("Ok", "Ok");
    public static readonly BallotStatusEnum Review = new BallotStatusEnum("Review", "Needs Review");
    public static readonly BallotStatusEnum TooMany = new BallotStatusEnum("TooMany", "Too Many");
    public static readonly BallotStatusEnum TooFew = new BallotStatusEnum("TooFew", "Too Few");
    public static readonly BallotStatusEnum Dup = new BallotStatusEnum("Dup", "Duplicates");

    static BallotStatusEnum()
    {
      Add(Ok);
      Add(Review);
      Add(TooMany);
      AddAsDefault(TooFew);
      Add(Dup);
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

    public override IList<BallotStatusEnum> Items
    {
      get { return BaseItems; }
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

  }
}