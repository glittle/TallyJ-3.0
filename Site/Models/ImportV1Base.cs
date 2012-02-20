using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Xml;
using TallyJ.Code;
using TallyJ.EF;
using TallyJ.Models.Helper;

namespace TallyJ.Models
{
  public abstract class ImportV1Base
  {
    protected readonly IDbContext _db;
    protected readonly ImportFile _file;
    protected readonly XmlDocument _xmlDoc;

    protected readonly List<Person> _people;
    protected readonly Action<Person> AddPerson;
    protected readonly ILogHelper _logHelper;

    /// <Summary>Base for Importing V1.</Summary>
    /// <remarks>Need list of people to merge into.</remarks>
    protected ImportV1Base(IDbContext db, ImportFile file, XmlDocument xmlDoc, List<Person> people, Action<Person> addPerson, ILogHelper logHelper)
    {
      _db = db;
      _file = file;
      _xmlDoc = xmlDoc;
      _people = people;
      AddPerson = addPerson;
      _logHelper = logHelper;
    }

    protected string ImportSummaryMessage;

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


      switch (spoiledGroup)
      {
        case "Ineligible":
          switch (spoiledDetail)
          {
            case "Not Eligible":
              return IneligibleReason.Ineligible_Other;

            case "Moved elsewhere recently":
              return IneligibleReason.Ineligible_Moved_elsewhere_recently;

            case "Resides elsewhere":
              return IneligibleReason.Ineligible_Resides_elsewhere;

            case "On other Institution":
              return IneligibleReason.Ineligible_On_other_Institution;

            case "Rights removed":
              return IneligibleReason.Ineligible_Rights_removed;

            case "Non-Bahá'í":
              return IneligibleReason.Ineligible_Non_Bahai;

            case "Deceased":
              return IneligibleReason.Ineligible_Deceased;

            case "Other":
            default:
              return IneligibleReason.Ineligible_Other;
          }

        case "Unidentifiable":
          switch (spoiledDetail)
          {
            case "Multiple people with same name":
              return IneligibleReason.Unidentifiable_Multiple_people_with_identical_name;

            case "Unknown person":
            case "Not Identifiable":
            default:
              return IneligibleReason.Unidentifiable_Unknown_person;
          }
        
        case "UnReadable": // 1.7
        case "Unreadable": // 1.8
          switch (spoiledDetail)
          {
            case "In unknown language":
              return IneligibleReason.Unreadable_In_another_language_not_translatable;

            case "Not Legible":
            case "Writing unreadable":
            default:
              return IneligibleReason.Unreadable_Writing_illegible;
          }
      }

      return IneligibleReason.Ineligible_Other;
    }
  }
}