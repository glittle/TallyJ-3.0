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
      public static readonly string Ineligible = "Cannot Vote, Cannot be Voted For";
      public static readonly string IneligiblePartial1 = "Can Vote (but cannot be voted for)";
      public static readonly string IneligiblePartial2 = "Cannot Vote (but can be voted for)";
      public static readonly string Unidentifiable = "Unidentifiable";
      public static readonly string Unreadable = "Unreadable";
    }

    public static readonly IneligibleReasonEnum Unidentifiable_Could_refer_to_more_than_one_person = new("D927534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable, "Could refer to more than one person");
    public static readonly IneligibleReasonEnum Unidentifiable_Multiple_people_with_identical_name = new("D727534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable, "Multiple people with identical name");
    public static readonly IneligibleReasonEnum Unidentifiable_Name_is_a_mix_of_multiple_people = new("D827534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable, "Name is a mix of multiple people");
    public static readonly IneligibleReasonEnum Unidentifiable_Unknown_person = new("CE27534D-D7E8-E011-A095-002269C41D11", GroupName.Unidentifiable, "Unknown person");

    public static readonly IneligibleReasonEnum Ineligible_Deceased = new("D227534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible, "Deceased");
    public static readonly IneligibleReasonEnum Ineligible_Moved_elsewhere_recently = new("CF27534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible, "Moved elsewhere recently");
    public static readonly IneligibleReasonEnum Ineligible_Not_in_this_local_unit = new("2add3a15-ec2d-437c-916f-7c581e693baa", GroupName.Ineligible, "Not in this local unit");
    public static readonly IneligibleReasonEnum Ineligible_Non_Bahai = new("D127534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible, "Not a registered Bahá'í");
    public static readonly IneligibleReasonEnum Ineligible_Not_Adult = new("32e44592-a7d8-408a-b169-8871800f62aa", GroupName.Ineligible, "Under 18 years old");
    public static readonly IneligibleReasonEnum Ineligible_Resides_elsewhere = new("D327534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible, "Resides elsewhere");
    public static readonly IneligibleReasonEnum Ineligible_Rights_removed = new("D027534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible, "Rights removed (entirely)");
    public static readonly IneligibleReasonEnum Ineligible_NotDelegate_OnOther = new("E027534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible, "Not a delegate and on other Institution");
    public static readonly IneligibleReasonEnum Ineligible_Other = new("D527534D-D7E8-E011-A095-002269C41D11", GroupName.Ineligible, "Other (cannot vote or be voted for)");

    public static readonly IneligibleReasonEnum IneligiblePartial1_Older_Youth = new("e6dd1cdd-5da0-4222-9f17-f02ce6313b0a", GroupName.IneligiblePartial1, "Youth aged 18/19/20", true);
    public static readonly IneligibleReasonEnum IneligiblePartial1_On_Institution_already = new("C05EAE49-B01B-E111-A7FB-002269C41D11", GroupName.IneligiblePartial1, "By-election: On Institution already", true);
    public static readonly IneligibleReasonEnum IneligiblePartial1_On_other_Institution = new("D427534D-D7E8-E011-A095-002269C41D11", GroupName.IneligiblePartial1, "On other Institution (e.g. Counsellor)", true);
    public static readonly IneligibleReasonEnum IneligiblePartial1_Rights_removed = new("920A1A55-C4A5-42E5-9BCE-31756B6A20B9", GroupName.IneligiblePartial1, "Rights removed (cannot be voted for)", true);
    public static readonly IneligibleReasonEnum IneligiblePartial1_Not_in_TieBreak = new("EB159A43-FB09-4FA9-AC12-3F451073010B", GroupName.IneligiblePartial1, "Tie-break election: Not tied", true);
    public static readonly IneligibleReasonEnum IneligiblePartial1_Other = new("24278180-fe1b-4604-9f86-d453b151d824", GroupName.IneligiblePartial1, "Other (can vote but not be voted for)", true);

    public static readonly IneligibleReasonEnum IneligiblePartial2_Not_a_Delegate = new("4B2B0F32-4E14-43A4-9103-C5E9C81E8783", GroupName.IneligiblePartial2, "Not a delegate in this election", false, true);
    public static readonly IneligibleReasonEnum IneligiblePartial2_Rights_removed = new("84FA30C9-F007-44E8-B097-CCA430AAA3AA", GroupName.IneligiblePartial2, "Rights removed (cannot vote)", false, true);
    public static readonly IneligibleReasonEnum IneligiblePartial2_Other = new("f4c7de9e-d487-49ae-9868-5cd208cd863a", GroupName.IneligiblePartial2, "Other (cannot vote but can be voted for)", false, true);


    public static readonly IneligibleReasonEnum Unreadable_In_another_language_not_translatable = new("D627534D-D7E8-E011-A095-002269C41D11", GroupName.Unreadable, "In an unknown language");
    public static readonly IneligibleReasonEnum Unreadable_Not_a_complete_name = new("86DDBE4A-841D-E111-A7FB-002269C41D11", GroupName.Unreadable, "Not a complete name");
    //    public static readonly IneligibleReasonEnum Unreadable_Vote_is_blank = new IneligibleReasonEnum("DA27534D-D7E8-E011-A095-002269C41D11", GroupName.Unreadable,"Vote line is blank");
    public static readonly IneligibleReasonEnum Unreadable_Writing_illegible = new("CD27534D-D7E8-E011-A095-002269C41D11", GroupName.Unreadable, "Writing is illegible");

    static IneligibleReasonEnum()
    {
      Add(Unidentifiable_Name_is_a_mix_of_multiple_people);
      Add(Unidentifiable_Multiple_people_with_identical_name);
      Add(Unidentifiable_Could_refer_to_more_than_one_person);
      Add(Unidentifiable_Unknown_person);

      Add(Ineligible_Not_Adult);
      Add(Ineligible_Resides_elsewhere);
      Add(Ineligible_Moved_elsewhere_recently);
      Add(Ineligible_Not_in_this_local_unit); // for two-stage elections
      Add(Ineligible_Deceased);
      Add(Ineligible_NotDelegate_OnOther);
      Add(Ineligible_Non_Bahai);
      Add(Ineligible_Rights_removed);
      Add(Ineligible_Other);

      Add(IneligiblePartial1_Older_Youth);
      Add(IneligiblePartial1_On_other_Institution);
      Add(IneligiblePartial1_On_Institution_already);
      Add(IneligiblePartial1_Not_in_TieBreak);
      Add(IneligiblePartial1_Rights_removed);
      Add(IneligiblePartial1_Other);

      Add(IneligiblePartial2_Not_a_Delegate);
      Add(IneligiblePartial2_Rights_removed);
      Add(IneligiblePartial2_Other);

      //      Add(Unreadable_Vote_is_blank);
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
    public bool CanReceiveVotes { get; private set; }
    public bool CanVote { get; private set; }
    public int IndexNum { get; private set; }

    public IneligibleReasonEnum(string guid, string group, string description, bool canVote = false, bool canReceiveVotes = false) : this(Guid.Parse(guid), group, description, canVote, canReceiveVotes)
    {
    }

    public IneligibleReasonEnum(Guid key, string group, string description, bool canVote = false, bool canReceiveVotes = false)
      : base(key, group)
    {
      Description = description;
      CanVote = canVote;
      CanReceiveVotes = canReceiveVotes;
    }

    /// <Summary>Group of reasons</Summary>
    public string Group
    {
      get { return DisplayText; }
    }

    /// <summary>
    /// Get the reason matching this guid. If null or not matched, returns null.
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static IneligibleReasonEnum Get(Guid? guid)
    {
      return guid.HasValue ? BaseItems.SingleOrDefault(i => i.Value == guid.Value) : null;
    }


    public static string DescriptionFor(Guid key)
    {
      var item = BaseItems.SingleOrDefault(i => i.Value == key);
      return item == null ? "" : item.Description;
    }

    public static IneligibleReasonEnum GetFor(string description)
    {
      var item = BaseItems.SingleOrDefault(i => i.Description.Equals(description.Trim(), StringComparison.InvariantCultureIgnoreCase));
      if (item == null) {
        return null;
      }
      return item;
    }


    public new static IList<IneligibleReasonEnum> Items
    {
      get { return BaseItems; }
    }

    public static string InvalidReasonsJsonString()
    {
      return Items
        .Select(r => new
        {
          Guid = r.Value,
          r.Group,
          Desc = r.Description,
          r.CanVote,
          r.CanReceiveVotes
        }).SerializedAsJsonString();
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

    public static object ReasonNamesForImportPage()
    {
      return BaseItems
        .Where(r => r.Group != GroupName.Unreadable && r.Group != GroupName.Unidentifiable)
        .Select(r => new
        {
          r.Group,
          r.Description
        });
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