using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ImportV1Community : ImportV1Base
  {
    int _peopleAdded = 0;
    int _nonAdults = 0;
    int _alreadyLoaded = 0;

    public ImportV1Community(ITallyJDbContext db, ImportFile file, XmlDocument xml, List<Person> people, Action<Person> addPerson, ILogHelper logHelper)
      : base(db, file, xml, people, addPerson, logHelper)
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
          _nonAdults++;
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
          _alreadyLoaded++;
          continue;
        }

        _peopleAdded++;

        var newPerson = new Person
                          {
                            PersonGuid = Guid.NewGuid(),
                            LastName = lastName,
                            FirstName = firstName
                          };

        AddPerson(newPerson);
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

        var ineligible = personXml.GetAttribute("IneligibleToReceiveVotes").AsBoolean();
        newPerson.CanReceiveVotes = ineligible;
        if (ineligible)
        {
          newPerson.IneligibleReasonGuid = MapIneligible("Ineligible", personXml.GetAttribute("ReasonToNotReceive"));
        }
        //TODO:

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


      _file.ProcessingStatus = "Imported";

      Db.SaveChanges();

      if (_peopleAdded > 0)
      {
        new PersonCacher(Db).DropThisCache();
      }

      ImportSummaryMessage = "Imported {0} {1}.".FilledWith(_peopleAdded, _peopleAdded.Plural("people", "person"));
      if (_alreadyLoaded > 0)
      {
        ImportSummaryMessage += " Skipped {0} {1} matching existing.".FilledWith(_alreadyLoaded, _alreadyLoaded.Plural("people", "person"));
      }
      if (_nonAdults > 0)
      {
        ImportSummaryMessage += " Skipped {0} non-adult{1}.".FilledWith(_nonAdults, _nonAdults.Plural());
      }

      _logHelper.Add("Imported v1 community file #" + _file.C_RowId + ": " + ImportSummaryMessage);

    }
  }
}