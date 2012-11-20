using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class PeopleModel : DataConnectedModel
  {
    public PeopleModel()
    {
    }

    #region FrontDeskSortEnum enum

    public enum FrontDeskSortEnum
    {
      ByArea,
      ByName
    }

    #endregion

    private Election _election;

    private List<Location> _locations;
    private List<Person> _peopleforFrontDesk;
    private IQueryable<Person> _people;

    public PeopleModel(Election election)
    {
      _election = election;
    }

    public long LastRowVersion
    {
      get
      {
        var single = Db.CurrentRowVersion().Single();
        return single != null ? single.Value : 0;
      }
    }

    private Election CurrentElection
    {
      get { return _election ?? (_election = UserSession.CurrentElection); }
    }

    private Guid CurrentElectionGuid
    {
      get { return CurrentElection == null ? Guid.Empty : CurrentElection.ElectionGuid; }
    }

    private IEnumerable<Location> Locations
    {
      get
      {
        return _locations ??
               (_locations = Db.Locations.Where(l => l.ElectionGuid == CurrentElectionGuid).ToList());
      }
    }

    private IQueryable<Person> PeopleInElectionQuery
    {
      get
      {
        return _people ??
               (_people = Db.People.Where(l => l.ElectionGuid == CurrentElectionGuid));
      }
    }

    public IQueryable<Person> PeopleInCurrentElection(bool onlyIfCanVote = false, bool includeIneligible = true)
    {
      {
        return PeopleInElectionQuery
          .Where(p => p.ElectionGuid == CurrentElectionGuid)
          .Where(p => !onlyIfCanVote || (p.CanVote.HasValue && p.CanVote.Value && p.IneligibleReasonGuid == null))
          .Where(p => includeIneligible || p.IneligibleReasonGuid == null);
      }
    }


    /// <summary>
    ///     Process each person record, preparing it BEFORE the election starts. Altered... too dangerous to wipe information!
    /// </summary>
    public void ResetInvolvementFlags()
    {
      foreach (var person in PeopleInElectionQuery)
      {
        ResetInvolvementFlags(person);
      }
      Db.SaveChanges();
    }

    //public void ResetAllInfo(Person person)
    //{
    //  ResetCombinedInfos(person);
    //  ResetVotingRecords(person);
    //  ResetInvolvementFlags(person);
    //}

    /// <Summary>Only to be done before an election</Summary>
    public void SetCombinedInfoAtStart(Person person)
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

    //public void ResetVotingRecords(Person person)
    //{
    //  person.RegistrationTime = null;
    //  person.VotingLocationGuid = null;
    //  person.VotingMethod = null;
    //  person.EnvNum = null;
    //}

    /// <summary>
    ///     Set person's flag based on what is default for this election
    /// </summary>
    /// <param name="person"> </param>
    /// <param name="election"> </param>
    public void ResetInvolvementFlags(Person person)
    {
      //var canVote = true; // person.AgeGroup.HasNoContent() || person.AgeGroup == AgeGroup.Adult;
      //person.IneligibleReasonGuid = canVote ? null : IneligibleReasonEnum.Ineligible_Not_Adult;

      var whoCanVote = CurrentElection.CanVote;
      var whoCanReceiveVotes = CurrentElection.CanReceive;

      person.CanVote = whoCanVote == ElectionModel.CanVoteOrReceive.All;
      person.CanReceiveVotes = whoCanReceiveVotes == ElectionModel.CanVoteOrReceive.All;
    }

    public JsonResult DetailsFor(int personId)
    {
      var person =
        PeopleInElectionQuery.SingleOrDefault(p => p.C_RowId == personId);

      if (person == null)
      {
        return new
                 {
                   Error = "Unknown person"
                 }.AsJsonResult();
      }

      //var whoCanVote = CurrentElection.CanVote;
      //var whoCanReceiveVotes = CurrentElection.CanReceive;

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
                 //person.AgeGroup,
                 person.BahaiId,
                 person.CanReceiveVotes,
                 person.CanVote,
                 person.FirstName,
                 person.IneligibleReasonGuid,
                 person.LastName,
                 person.OtherInfo,
                 person.OtherLastNames,
                 person.OtherNames,
                 person.Area,
                 person.C_FullName
               };
    }

    public JsonResult SavePerson(Person personFromInput)
    {
      var savedPerson =
        PeopleInCurrentElection().SingleOrDefault(p => p.C_RowId == personFromInput.C_RowId && p.ElectionGuid == CurrentElectionGuid);

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
                          ElectionGuid = CurrentElectionGuid
                        };
        ResetInvolvementFlags(savedPerson);
        Db.People.Add(savedPerson);
      }

      if (personFromInput.IneligibleReasonGuid == Guid.Empty)
      {
        personFromInput.IneligibleReasonGuid = null;
      }

      var editableFields = new
                             {
                               // personFromInput.AgeGroup,
                               personFromInput.BahaiId,
                               personFromInput.FirstName,
                               personFromInput.IneligibleReasonGuid,
                               personFromInput.LastName,
                               personFromInput.OtherInfo,
                               personFromInput.OtherLastNames,
                               personFromInput.OtherNames,
                               personFromInput.Area,
                             }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();

      var changed = personFromInput.CopyPropertyValuesTo(savedPerson, editableFields);

      // these two may not be present, depending on the election type
      const string all = ElectionModel.CanVoteOrReceive.All;
      var canReceiveVotes = personFromInput.CanReceiveVotes.GetValueOrDefault(CurrentElection.CanReceive == all);
      if (savedPerson.CanReceiveVotes != canReceiveVotes)
      {
        savedPerson.CanReceiveVotes = canReceiveVotes;
        changed = true;
      }

      var canVote = personFromInput.CanVote.GetValueOrDefault(CurrentElection.CanVote == all);
      if (savedPerson.CanVote != canVote)
      {
        savedPerson.CanVote = canVote;
        changed = true;
      }

      if (changed)
      {
        SetCombinedInfos(savedPerson);

        Db.SaveChanges();
      }

      return new
               {
                 Status = "Saved",
                 Person = PersonForEdit(savedPerson),
                 OnFile = Db.People.Count(p => p.ElectionGuid == CurrentElectionGuid)
               }.AsJsonResult();
    }

    /// <Summary>Everyone</Summary>
    public IEnumerable<object> FrontDeskPersonLines(FrontDeskSortEnum sortType = FrontDeskSortEnum.ByName)
    {
      return FrontDeskPersonLines(PeopleForFrontDesk());
    }

    /// <Summary>People to tbe listed on Front Desk page. Called more than once, so separated out</Summary>
    public List<Person> PeopleForFrontDesk()
    {
      return _peopleforFrontDesk ?? (_peopleforFrontDesk = PeopleInCurrentElection(true).ToList());
    }

    public IEnumerable<object> BallotSources(int forLocationId = -1)
    {
      var locations = Locations.ToDictionary(l => l.LocationGuid, l => l.Name);
      var forLocationGuid = forLocationId == -1
                              ? Guid.Empty
                              : Locations.Where(l => l.C_RowId == forLocationId).Select(l => l.LocationGuid).Single();
      var timeOffset = UserSession.TimeOffsetServerAhead;
      var tellers =
        Db.Tellers.Where(t => t.ElectionGuid == CurrentElectionGuid).ToDictionary(t => t.TellerGuid,
                                                                                              t => t.Name);

      var ballotSources = PeopleInCurrentElection() // start with everyone
        .Where(p => !string.IsNullOrEmpty(p.VotingMethod))
        .Where(p => forLocationId == -1 || p.VotingLocationGuid == forLocationGuid)
        .ToList()
        .OrderBy(p => p.VotingMethod)
        .ThenBy(p => p.RegistrationTime)
        .Select(p => new
          {
            PersonId = p.C_RowId,
            p.C_FullName,
            VotedAt = p.VotingLocationGuid.HasValue ? locations[p.VotingLocationGuid.Value] : "",
            When = ShowRegistrationTime(timeOffset, p),
            p.VotingMethod,
            EnvNum = ShowEnvNum(p),
            Tellers = ShowTellers(tellers, p)
          })
         .ToList();

      var location = ContextItems.LocationModel.HasLocations
        ? Locations.Single(l => l.LocationGuid == forLocationGuid)
        : Locations.Single(l => l.LocationGuid == UserSession.CurrentLocationGuid);

      if (location.BallotsCollected.AsInt() == 0)
      {
        location.BallotsCollected = ballotSources.Count;
        Db.SaveChanges();

        if (location.LocationGuid == UserSession.CurrentLocationGuid)
        {
          UserSession.CurrentLocation = location;
        }
      }


      return ballotSources;
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
    public IEnumerable<object> FrontDeskPersonLines(List<Person> people,
                                                    FrontDeskSortEnum sortType = FrontDeskSortEnum.ByName)
    {
      var locations = Locations.ToDictionary(l => l.LocationGuid, l => l.Name);
      var showLocations = locations.Count > 1;
      var tellers =
        Db.Tellers.Where(t => t.ElectionGuid == CurrentElectionGuid).ToDictionary(t => t.TellerGuid,
                                                                                              t => t.Name);
      var timeOffset = UserSession.TimeOffsetServerAhead;

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
                                       ShowTellers(tellers, p),
                                       ShowRegistrationTime(timeOffset, p)
                                     }.JoinedAsString("; ", true),
                         InPerson = p.VotingMethod == VotingMethodEnum.InPerson,
                         DroppedOff = p.VotingMethod == VotingMethodEnum.DroppedOff,
                         MailedIn = p.VotingMethod == VotingMethodEnum.MailedIn,
                         CalledIn = p.VotingMethod == VotingMethodEnum.CalledIn,
                         EnvNum = ShowEnvNum(p)
                       });
    }

    private static int? ShowEnvNum(Person p)
    {
      return p.VotingMethod.DefaultTo(VotingMethodEnum.InPerson) == VotingMethodEnum.InPerson
               ? null
               : p.EnvNum;
    }

    private static string ShowTellers(Dictionary<Guid, string> tellers, Person p)
    {
      var names = new List<string>
        {
          p.TellerAtKeyboard.HasValue
            ? (tellers.ContainsKey(p.TellerAtKeyboard.Value) ? tellers[p.TellerAtKeyboard.Value] : "?")
            : "",
          p.TellerAssisting.HasValue
            ? (tellers.ContainsKey(p.TellerAssisting.Value) ? tellers[p.TellerAssisting.Value] : "?")
            : ""
        };
      return names.JoinedAsString(", ", true);
    }

    private static string ShowRegistrationTime(int timeOffset, Person p)
    {
      return p.RegistrationTime.HasValue
               ? p.RegistrationTime.Value.AddMilliseconds(0 - timeOffset).ToString("h:mm tt").ToLowerInvariant()
               : "";
    }

    public JsonResult RegisterVoteJson(int personId, string voteType, int lastRowVersion)
    {
      if (!VotingMethodEnum.Exists(voteType))
      {
        return new { Message = "Invalid type" }.AsJsonResult();
      }

      var person =
        Db.People.SingleOrDefault(p => p.ElectionGuid == CurrentElectionGuid && p.C_RowId == personId);
      if (person == null)
      {
        return new { Message = "Unknown person" }.AsJsonResult();
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

            // get election from DB, not session, as we need to update it now
            var election = new ElectionModel().GetFreshFromDb(CurrentElectionGuid);
            var nextNum = election.LastEnvNum.AsInt() + 1;

            person.EnvNum = nextNum;
            election.LastEnvNum = nextNum;

          }
        }
      }

      person.TellerAtKeyboard = UserSession.GetCurrentTeller(1);
      person.TellerAssisting = UserSession.GetCurrentTeller(2);

      Db.SaveChanges();

      List<Person> people;
      if (lastRowVersion == 0)
      {
        people = new List<Person> { person };
      }
      else
      {
        people = Db.People
  .Where(p => p.ElectionGuid == CurrentElectionGuid && p.C_RowVersionInt > lastRowVersion)
  .ToList();
      }

      var updateInfo = new
      {
        PersonLines = FrontDeskPersonLines(people),
        LastRowVersion
      };

      FrontDeskHub.UpdateAllConnectedClients(updateInfo);

      return updateInfo.AsJsonResult();
    }

    public JsonResult DeleteAllPeople()
    {
      int rows;
      try
      {
        rows = Db.Database.ExecuteSqlCommand("Delete from tj.Person where ElectionGuid={0}",
                                             CurrentElectionGuid);
      }
      catch (SqlException)
      {
        return
          new { Results = "Nothing was deleted. Once votes have been recorded, you cannot delete all the people" }.
            AsJsonResult();
      }

      return new { Results = "{0} {1} deleted".FilledWith(rows, rows.Plural("people", "person")) }.AsJsonResult();
    }
  }
}