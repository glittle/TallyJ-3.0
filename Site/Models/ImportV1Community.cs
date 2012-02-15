using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;
using System.Web.WebPages;
using TallyJ.Code;

namespace TallyJ.Models
{
  public class ImportV1Community : ImportV1Base
  {

    public ImportV1Community(IDbContext db, ImportFile file, XmlDocument xml, List<Person> people, Action<Person> registerPerson)
      : base(db, file, xml, people, registerPerson)
    {
    }

    public override void Process()
    {
      // 	<Person LName="Accorti" FName="Pónt" AKAName="Paul" AgeGroup="Youth"></Person>
      //  <Person LName="Brown" FName="Lesley" AgeGroup="Adult" IneligibleToReceiveVotes="true" ReasonToNotReceive="Resides elsewhere"></Person>
      //  <Person FName="Aria" AgeGroup="Adult" LName="Danton" EnvNum="7" Voted="DroppedOff"></Person>

      foreach (XmlElement personXml in _xmlDoc.DocumentElement.SelectNodes("//Person"))
      {
        var ageGroup = personXml.GetAttribute("AgeGroup");
        if (ageGroup.DefaultTo("Adult") != "Adult")
        {
          nonAdults++;
          continue;
        }

        var lastName = personXml.GetAttribute("LName");
        var firstName = personXml.GetAttribute("FName");
        var akaName = personXml.GetAttribute("AKAName");

        // check for matches
        var matchedExisting =
          _people.Any(p => p.LastName.DefaultTo("") == lastName && p.FirstName.DefaultTo("") == firstName && p.OtherNames.DefaultTo("") == akaName);
        if (matchedExisting)
        {
          alreadyLoaded++;
          continue;
        }

        peopleAdded++;

        var newPerson = new Person
                          {
                            PersonGuid = Guid.NewGuid(),
                            LastName = lastName,
                            FirstName = firstName
                          };

        _registerPerson(newPerson);
        _people.Add(newPerson);


        if (akaName.HasContent())
        {
          newPerson.OtherNames = akaName;
        }

        var bahaiId = personXml.GetAttribute("BahaiId");
        if (bahaiId.HasContent())
        {
          newPerson.BahaiId = bahaiId;
        }

        var ineligible = personXml.GetAttribute("IneligibleToReceiveVotes").AsBool();
        newPerson.CanReceiveVotes = ineligible;

        var ineligibleReason = personXml.GetAttribute("ReasonToNotReceive").AsBool();

        var voteMethod = personXml.GetAttribute("Voted");
        switch (voteMethod)
        {
          case "VotedInPerson":
            newPerson.VotingMethod = VotingMethodEnum.InPerson;
            break;
          case "DroppedOff":
            newPerson.VotingMethod = VotingMethodEnum.DroppedOff;
            break;
          case "Mailed":
            newPerson.VotingMethod = VotingMethodEnum.MailedIn;
            break;
        }
        var envNum = personXml.GetAttribute("EnvNum").AsInt();
        if (envNum != 0)
        {
          newPerson.EnvNum = envNum;
        }


      }
    }

    private Guid GetReasonGuid(string reason)
    {
      return Guid.NewGuid();
      //SpoiledTypeIneligible|Not Eligible //1.80
      //SpoiledTypeIneligible1|Moved elsewhere recently //1.80
      //SpoiledTypeIneligible2|Resides elsewhere //1.80
      //SpoiledTypeIneligible3|On other Institution //1.80
      //SpoiledTypeIneligible4|Rights removed //1.80
      //SpoiledTypeIneligible5|Non-Bahá'í //1.80
      //SpoiledTypeIneligible6|Deceased //1.80
      //SpoiledTypeIneligible7|Other //1.80

      //SpoiledTypeUnidentifiable|Not Identifiable //1.80
      //SpoiledTypeUnidentifiable1|Unknown person //1.80
      //SpoiledTypeUnidentifiable2|Multiple people with same name  //1.80

      //SpoiledTypeUnreadable|Not Legible //1.80
      //SpoiledTypeUnreadable1|Writing unreadable //1.80
      //SpoiledTypeUnreadable2|In unknown language //1.80

      //SpoiledOther|Other // 1.80

    }
  }
}