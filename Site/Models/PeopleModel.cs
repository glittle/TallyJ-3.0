using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;
using TallyJ.Models.Helper;

namespace TallyJ.Models
{
  public class PeopleModel : DataConnectedModel
  {
    #region FrontDeskSortEnum enum

    public enum FrontDeskSortEnum
    {
      ByArea,
      ByName
    }

    #endregion

    private List<Location> _locations;

    public long LastRowVersion
    {
      get { return Db.CurrentRowVersion().Single().Value; }
    }

    private IEnumerable<Location> Locations
    {
      get
      {
        return _locations ??
               (_locations = Db.Locations.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid).ToList());
      }
    }

    public IQueryable<Person> PeopleInCurrentElection(bool includeIneligible)
    {
      {
        return Db.People
          .Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid)
          .Where(p => includeIneligible || p.IneligibleReasonGuid == null);
      }
    }


    /// <summary>
    ///     Process each person record, preparing it BEFORE the election starts
    /// </summary>
    public void CleanAllPersonRecordsBeforeStarting()
    {
      foreach (var person in Db.People)
      {
        ResetAllInfo(person);
      }
      Db.SaveChanges();
    }

    public void ResetAllInfo(Person person)
    {
      ResetCombinedInfos(person);
      ClearVotingInfo(person);
      ClearEligibilityRestrictions(person);
    }

    /// <Summary>Only to be done before an election</Summary>
    public void ResetCombinedInfos(Person person)
    {
      person.CombinedInfoAtStart =
        person.CombinedInfo = person.MakeCombinedInfo();

      person.UpdateCombinedSoundCodes();
    }

    public void SetCombinedInfos(Person person)
    {
      person.CombinedInfo = person.MakeCombinedInfo();
      person.UpdateCombinedSoundCodes();
    }

    public void ClearVotingInfo(Person person)
    {
      person.RegistrationTime = null;
      person.VotingLocationGuid = null;
      person.VotingMethod = null;
      person.EnvNum = null;
    }

    /// <summary>
    ///     Use age group to determine eligibility.
    /// </summary>
    /// <param name="person"> </param>
    public void ClearEligibilityRestrictions(Person person)
    {
      var canVote = person.AgeGroup.HasNoContent() || person.AgeGroup == AgeGroup.Adult;

      person.IneligibleReasonGuid = canVote ? null : IneligibleReason.Ineligible_Not_Adult;

      var whoCanVote = UserSession.CurrentElection.CanVote;
      var whoCanReceiveVotes = UserSession.CurrentElection.CanReceive;

      person.CanVote = whoCanVote == "A";
      person.CanReceiveVotes = whoCanReceiveVotes == "A";
    }

    public JsonResult DetailsFor(int personId)
    {
      var person =
        Db.People.SingleOrDefault(p => p.C_RowId == personId && p.ElectionGuid == UserSession.CurrentElectionGuid);

      if (person == null)
      {
        return new
                 {
                   Error = "Unknown person"
                 }.AsJsonResult();
      }

      //var whoCanVote = UserSession.CurrentElection.CanVote;
      //var whoCanReceiveVotes = UserSession.CurrentElection.CanReceive;

      return new
               {
                 Person = PersonForEdit(person)
               }.AsJsonResult();
    }

    private static object PersonForEdit(Person person)
    {
      return new
               {
                 person.C_RowId,
                 person.AgeGroup,
                 person.BahaiId,
                 person.CanReceiveVotes,
                 person.CanVote,
                 person.FirstName,
                 person.IneligibleReasonGuid,
                 person.LastName,
                 person.OtherInfo,
                 person.OtherLastNames,
                 person.OtherNames,
                 person.Area
               };
    }

    public JsonResult SavePerson(Person personFromInput)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;

      var savedPerson =
        Db.People.SingleOrDefault(p => p.C_RowId == personFromInput.C_RowId && p.ElectionGuid == currentElectionGuid);

      if (savedPerson == null)
      {
        if (personFromInput.C_RowId != -1)
        {
          return new
                   {
                     Status = "Unknown ID"
                   }.AsJsonResult();
        }

        savedPerson = new Person
                        {
                          PersonGuid = Guid.NewGuid(),
                          ElectionGuid = currentElectionGuid
                        };
        Db.People.Add(savedPerson);
      }

      if (personFromInput.IneligibleReasonGuid == Guid.Empty)
      {
        personFromInput.IneligibleReasonGuid = null;
      }

      var editableFields = new
                             {
                               personFromInput.AgeGroup,
                               personFromInput.BahaiId,
                               personFromInput.CanReceiveVotes,
                               personFromInput.CanVote,
                               personFromInput.FirstName,
                               personFromInput.IneligibleReasonGuid,
                               personFromInput.LastName,
                               personFromInput.OtherInfo,
                               personFromInput.OtherLastNames,
                               personFromInput.OtherNames,
                               personFromInput.Area
                             }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();

      var changed = personFromInput.CopyPropertyValuesTo(savedPerson, editableFields);

      if (changed)
      {
        SetCombinedInfos(savedPerson);

        Db.SaveChanges();
      }

      return new
               {
                 Status = "Saved",
                 Person = PersonForEdit(savedPerson)
               }.AsJsonResult();
    }

    /// <Summary>Everyone</Summary>
    public IEnumerable<object> PersonLines(FrontDeskSortEnum sortType = FrontDeskSortEnum.ByName)
    {
      return PersonLines(PeopleInCurrentElection(true).ToList());
    }

    public IEnumerable<object> BallotSources(int forLocationId = -1)
    {
      var locations = Locations.ToDictionary(l => l.LocationGuid, l => l.Name);
      var forLocationGuid = forLocationId == -1
                              ? Guid.Empty
                              : Locations.Where(l => l.C_RowId == forLocationId).Select(l => l.LocationGuid).Single();

      return PeopleInCurrentElection(false)
        .Where(p => !string.IsNullOrEmpty(p.VotingMethod))
        .Where(p => forLocationId == -1 || p.VotingLocationGuid == forLocationGuid)
        .ToList()
        .OrderBy(p => p.VotingMethod).ThenBy(p => p.RegistrationTime)
        .Select(p => new
                       {
                         PersonId = p.C_RowId,
                         p.C_FullName,
                         VotedAt = p.VotingLocationGuid.HasValue ? locations[p.VotingLocationGuid.Value] : "",
                         When = p.RegistrationTime,
                         p.VotingMethod,
                         p.EnvNum
                       });
    }

    public HtmlString GetLocationOptions()
    {
      return Locations
        .OrderBy(l => l.SortOrder)
        .Select(l => "<option value={C_RowId}>{Name}</option>".FilledWith(l))
        .JoinedAsString()
        .AsRawHtml();
    }

    /// <Summary>Only those listed</Summary>
    public IEnumerable<object> PersonLines(List<Person> people, FrontDeskSortEnum sortType = FrontDeskSortEnum.ByName)
    {
      var locations = Locations.ToDictionary(l => l.LocationGuid, l => l.Name);
      var showLocations = locations.Count > 1;
      var tellers =
        Db.Tellers.Where(t => t.ElectionGuid == UserSession.CurrentElectionGuid).ToDictionary(t => t.TellerGuid,
                                                                                              t => t.Name);
      var timeOffset = UserSession.TimeOffset;

      return people
        .OrderBy(p => sortType == FrontDeskSortEnum.ByArea ? p.Area : "")
        .ThenBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .Select(p => new
                       {
                         PersonId = p.C_RowId,
                         FullName = p.C_FullName,
                         NameLower = p.C_FullName.WithoutDiacritics(true).Replace("\"", "\\\""),
                         p.Area,
                         VotedAt = new[]
                                     {
                                       showLocations && p.VotingLocationGuid.HasValue
                                         ? locations[p.VotingLocationGuid.Value]
                                         : "",
                                       p.TellerAtKeyboard.HasValue
                                         ? " (" + tellers[p.TellerAtKeyboard.Value]
                                           + (p.TellerAssisting.HasValue ? ", " + tellers[p.TellerAssisting.Value] : "") 
                                           + ")"
                                         : "",
                                       p.RegistrationTime.HasValue
                                         ? p.RegistrationTime.Value.AddMilliseconds(timeOffset).ToString("h:mm")
                                         : ""
                                     }.JoinedAsString(" ", true),
                         InPerson = p.VotingMethod == VotingMethodEnum.InPerson,
                         DroppedOff = p.VotingMethod == VotingMethodEnum.DroppedOff,
                         MailedIn = p.VotingMethod == VotingMethodEnum.MailedIn,
                         EnvNum = p.VotingMethod.DefaultTo(VotingMethodEnum.InPerson) == VotingMethodEnum.InPerson
                                    ? null
                                    : p.EnvNum
                       });
    }

    public JsonResult RegisterVoteJson(int personId, string voteType, int lastRowVersion)
    {
      if (!VotingMethodEnum.Exists(voteType))
      {
        return new {Message = "Invalid type"}.AsJsonResult();
      }

      var person =
        Db.People.SingleOrDefault(p => p.ElectionGuid == UserSession.CurrentElectionGuid && p.C_RowId == personId);
      if (person == null)
      {
        return new {Message = "Unknown person"}.AsJsonResult();
      }


      if (person.VotingMethod == voteType)
      {
        // already set this way...turn if off
        person.VotingMethod = null;
        person.VotingLocationGuid = null;
        person.RegistrationTime = null;
      }
      else
      {
        person.VotingMethod = voteType;
        person.VotingLocationGuid = UserSession.CurrentLocationGuid;
        person.RegistrationTime = DateTime.Now;

        if (voteType != VotingMethodEnum.InPerson)
        {
          if (person.EnvNum == null)
          {
            // create a new env number

            // get election from DB, not session, as we may need to update it now
            var election = Db.Elections.Single(e => e.ElectionGuid == UserSession.CurrentElectionGuid);
            var nextNum = election.LastEnvNum.AsInt() + 1;

            person.EnvNum = nextNum;
            election.LastEnvNum = nextNum;

            UserSession.CurrentElection = election;
          }
        }
      }

      person.TellerAtKeyboard = UserSession.GetCurrentTeller(1);
      person.TellerAssisting = UserSession.GetCurrentTeller(2);

      Db.SaveChanges();

      if (lastRowVersion == 0)
      {
        return new
                 {
                   PersonLines = PersonLines(new List<Person> {person}),
                   LastRowVersion
                 }.AsJsonResult();
      }

      var people = Db.People
        .Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid && p.C_RowVersionInt > lastRowVersion)
        .ToList();
      return new
               {
                 PersonLines = PersonLines(people),
                 LastRowVersion
               }.AsJsonResult();
    }

    public JsonResult DeleteAllPeople()
    {
      int rows;
      try
      {
        rows = Db.Database.ExecuteSqlCommand("Delete from tj.Person where ElectionGuid={0}",
                                             UserSession.CurrentElectionGuid);
      }
      catch (SqlException e)
      {
        return
          new {Results = "Nothing was deleted. Once votes have been recorded, you cannot delete all the people"}.
            AsJsonResult();
      }

      return new {Results = "{0} {1} deleted".FilledWith(rows, rows.Plural("people", "person"))}.AsJsonResult();
    }
  }
}