using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public abstract class ImportV1Base
  {
    protected readonly ImportFile _file;
    protected readonly XmlDocument _xmlDoc;

    protected readonly List<Person> _people;
    protected readonly Action<Person> AddPerson;
    protected readonly ILogHelper _logHelper;

    /// <Summary>Base for Importing V1.</Summary>
    /// <remarks>Need list of people to merge into.</remarks>
    protected ImportV1Base(ITallyJDbContext db, ImportFile file, XmlDocument xmlDoc, List<Person> people, Action<Person> addPerson, ILogHelper logHelper)
    {
      Db = db;
      _file = file;
      _xmlDoc = xmlDoc;
      _people = people;
      AddPerson = addPerson;
      _logHelper = logHelper;
    }

    protected string ImportSummaryMessage;

    public ITallyJDbContext Db { get; private set; }

    public JsonResult SendSummary()
    {
      return new
               {
                 importReport = ImportSummaryMessage
               }.AsJsonResult();

    }

    public abstract void Process();

    protected Guid? MapIneligible(string spoiledGroup, string spoiledDetail)
    {
      /*       TallyJ 1.8x
       * 
            SpoiledTypeIneligible|Not Eligible //1.80
            SpoiledTypeIneligible1|Moved elsewhere recently //1.80
            SpoiledTypeIneligible2|Resides elsewhere //1.80
            SpoiledTypeIneligible3|On other Institution //1.80
            SpoiledTypeIneligible4|Rights removed //1.80
            SpoiledTypeIneligible5|Non-Bahá'í //1.80
            SpoiledTypeIneligible6|Deceased //1.80
            SpoiledTypeIneligible7|Other //1.80

            SpoiledTypeUnidentifiable|Not Identifiable //1.80
            SpoiledTypeUnidentifiable1|Unknown person //1.80
            SpoiledTypeUnidentifiable2|Multiple people with same name  //1.80

            SpoiledTypeUnreadable|Not Legible //1.80
            SpoiledTypeUnreadable1|Writing unreadable //1.80
            SpoiledTypeUnreadable2|In unknown language //1.80

            SpoiledOther|Other // 1.80

       */

      var reason = IneligibleReasonEnum.Ineligible_Other;

      switch (spoiledGroup)
      {
        case "Ineligible":
          switch (spoiledDetail)
          {
            case "Not Eligible":
              reason = IneligibleReasonEnum.Ineligible_Other;
              break;

            case "Moved elsewhere recently":
              reason = IneligibleReasonEnum.Ineligible_Moved_elsewhere_recently;
              break;

            case "Resides elsewhere":
              reason = IneligibleReasonEnum.Ineligible_Resides_elsewhere;
              break;

            case "On other Institution":
              reason = IneligibleReasonEnum.IneligiblePartial1_On_other_Institution;
              break;

            case "Rights removed":
              reason = IneligibleReasonEnum.Ineligible_Rights_removed;
              break;

            case "Non-Bahá'í":
              reason = IneligibleReasonEnum.Ineligible_Non_Bahai;
              break;

            case "Deceased":
              reason = IneligibleReasonEnum.Ineligible_Deceased;
              break;

            case "Other":
            default:
              reason = IneligibleReasonEnum.Ineligible_Other;
              break;
          }
          break;

        case "Unidentifiable":
          switch (spoiledDetail)
          {
            case "Multiple people with same name":
              reason = IneligibleReasonEnum.Unidentifiable_Multiple_people_with_identical_name;
              break;

            case "Unknown person":
            case "Not Identifiable":
            default:
              reason = IneligibleReasonEnum.Unidentifiable_Unknown_person;
              break;
          }
          break;

        case "UnReadable": // 1.7
        case "Unreadable": // 1.8
          switch (spoiledDetail)
          {
            case "In unknown language":
              reason = IneligibleReasonEnum.Unreadable_In_another_language_not_translatable;
              break;

            case "Not Legible":
            case "Writing unreadable":
            default:
              reason = IneligibleReasonEnum.Unreadable_Writing_illegible;
              break;
          }
          break;
      }

      return reason;
    }
  }
}