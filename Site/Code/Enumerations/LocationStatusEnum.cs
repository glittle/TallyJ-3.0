using System.Collections.Generic;
using System.Linq;
using System.Web;
using TallyJ.EF;

namespace TallyJ.Code.Enumerations
{
  public class LocationStatusEnum : BaseEnumeration<LocationStatusEnum, string>
  {
    public static readonly LocationStatusEnum NotStarted = new("NotStarted", "Not started");
    public static readonly LocationStatusEnum Receiving = new("Receiving", "Receiving ballots");
    public static readonly LocationStatusEnum Sorting = new("Sorting", "Sorting and opening ballots");
    public static readonly LocationStatusEnum Entering = new("Entering", "Entering ballots into TallyJ");
    public static readonly LocationStatusEnum Reviewing = new("Reviewing", "Doing final review");
    public static readonly LocationStatusEnum Done = new("Done", "Done - All ballots finalized");
    public static readonly LocationStatusEnum NeedHelp = new("NeedHelp", "Need Help!");

    static LocationStatusEnum()
    {
      AddAsDefault(NotStarted);
      Add(Receiving);
      Add(Sorting);
      Add(Entering);
      Add(Reviewing);
      Add(Done);
      Add(NeedHelp);
    }

    public LocationStatusEnum(string key, string display)
      : base(key, display)
    {
    }

    public static string TextFor(string value)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == value);
      return item == null ? "" : item.DisplayText;
    }

    public static HtmlString ForHtmlSelect(Location location)
    {
      var selected = location == null ? "" : location.TallyStatus;
      return
        BaseItems
          .Select(bi => "<option value='{0}'{2}>{1}</option>"
                          .FilledWith(bi.Value, bi.Text, bi.Value == selected ? " selected" : ""))
          .JoinedAsString()
          .AsRawHtml();
    }
  }
}