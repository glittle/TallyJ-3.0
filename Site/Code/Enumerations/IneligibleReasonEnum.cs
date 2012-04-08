using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TallyJ.Code.Enumerations
{
  public class IneligibleReasonEnum : BaseEnumeration<IneligibleReasonEnum, Guid>
  {
    public static class GroupName
    {
      public static readonly string Ineligible = "Ineligible";
      public static readonly string Unidentifiable = "Unidentifiable";
      public static readonly string Unreadable = "Unreadable";
    }

    public static readonly IneligibleReasonEnum Ineligible_Deceased = new IneligibleReasonEnum("D227534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"Deceased");
    public static readonly IneligibleReasonEnum Ineligible_Moved_elsewhere_recently = new IneligibleReasonEnum("CF27534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"Moved elsewhere recently");
    public static readonly IneligibleReasonEnum Ineligible_Non_Bahai = new IneligibleReasonEnum("D127534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"Non-Bahá'í");
    public static readonly IneligibleReasonEnum Ineligible_Not_Adult = new IneligibleReasonEnum("CC27534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"Under 21 years old");
    public static readonly IneligibleReasonEnum Ineligible_On_Institution_already = new IneligibleReasonEnum("C05EAE49-B01B-E111-A7FB-002269C41D11", GroupName.Ineligible,"On Institution already");
    public static readonly IneligibleReasonEnum Ineligible_On_other_Institution = new IneligibleReasonEnum("D427534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"On other Institution");
    public static readonly IneligibleReasonEnum Ineligible_Other = new IneligibleReasonEnum("D527534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"Other");
    public static readonly IneligibleReasonEnum Ineligible_Resides_elsewhere = new IneligibleReasonEnum("D327534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"Resides elsewhere");
    public static readonly IneligibleReasonEnum Ineligible_Rights_removed = new IneligibleReasonEnum("D027534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible,"Rights removed");
    public static readonly IneligibleReasonEnum Unidentifiable_Could_refer_to_more_than_one_person = new IneligibleReasonEnum("D927534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable,"Could refer to more than one person");
    public static readonly IneligibleReasonEnum Unidentifiable_Multiple_people_with_identical_name = new IneligibleReasonEnum("D727534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable,"Multiple people with identical name");
    public static readonly IneligibleReasonEnum Unidentifiable_Name_is_a_mix_of_multiple_people = new IneligibleReasonEnum("D827534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable,"Name is a mix of multiple people");
    public static readonly IneligibleReasonEnum Unidentifiable_Unknown_person = new IneligibleReasonEnum("CE27534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable,"Unknown person");
    public static readonly IneligibleReasonEnum Unreadable_In_another_language_not_translatable = new IneligibleReasonEnum("D627534D-D7E8-E011-A095-002269C41D11", GroupName.Unreadable,"In an unknown language");
    public static readonly IneligibleReasonEnum Unreadable_Not_a_complete_name = new IneligibleReasonEnum("86DDBE4A-841D-E111-A7FB-002269C41D11", GroupName.Unreadable,"Not a complete name");
    public static readonly IneligibleReasonEnum Unreadable_Vote_is_blank = new IneligibleReasonEnum("DA27534D-D7E8-E011-A095-002269C41D11", GroupName.Unreadable,"Vote line is blank");
    public static readonly IneligibleReasonEnum Unreadable_Writing_illegible = new IneligibleReasonEnum("CD27534D-D7E8-E011-A095-002269C41D11", GroupName.Unreadable,"Writing is illegible");

    static IneligibleReasonEnum()
    {
      Add(Ineligible_Moved_elsewhere_recently);
      Add(Ineligible_Resides_elsewhere);
      Add(Ineligible_Not_Adult);
      Add(Ineligible_On_other_Institution);
      Add(Ineligible_On_Institution_already);
      Add(Ineligible_Rights_removed);
      Add(Ineligible_Non_Bahai);
      Add(Ineligible_Deceased);
      Add(Ineligible_Other);

      Add(Unidentifiable_Unknown_person);
      Add(Unidentifiable_Could_refer_to_more_than_one_person);
      Add(Unidentifiable_Multiple_people_with_identical_name);
      Add(Unidentifiable_Name_is_a_mix_of_multiple_people);

      Add(Unreadable_Vote_is_blank);
      Add(Unreadable_Writing_illegible);
      Add(Unreadable_Not_a_complete_name);
      Add(Unreadable_In_another_language_not_translatable);
    }

    private static int _itemCount = 1;

    protected new static void Add(IneligibleReasonEnum item)
    {
      item.IndexNum = _itemCount++;
      BaseItems.Add(item);
    }

    public string Description { get; private set; }
    public int IndexNum { get; private set; }

    public IneligibleReasonEnum(string guid, string group, string description) : this(Guid.Parse(guid), group, description)
    {
    }

    public IneligibleReasonEnum(Guid key, string group, string description)
      : base(key, group)
    {
      Description = description;
    }

    /// <Summary>Group of reasons</Summary>
    public string Group
    {
      get { return DisplayText; }
    }

    public static string DescriptionFor(Guid key)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == key);
      return item == null ? "" : item.Description;
    }

    public new static IList<IneligibleReasonEnum> Items
    {
      get { return BaseItems; }
    }

    public static HtmlString ForHtmlSelect(Guid selected)
    {
      return
        BaseItems
          .Select(bi => "<option value='{0}'{2}>{1}</option>"
                          .FilledWith(bi.Value, bi.Text, bi.Value == selected ? " selected" : ""))
          .JoinedAsString()
          .AsRawHtml();
    }


    public static implicit operator Guid(IneligibleReasonEnum self)
    {
      return self.Value;
    }

    public new bool Equals(object x, object y)
    {
      if (x == null || y == null)
      {
        return false;
      }
      return ((IneligibleReasonEnum)x).Value.Equals(((IneligibleReasonEnum)y).Value);
    }

    public new int GetHashCode(object x)
    {
      return ((IneligibleReasonEnum)x).Value.GetHashCode();
    }


  }
}