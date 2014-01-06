using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class PeopleModel : DataConnectedModel
  {
    private Election _election;

    private List<Location> _locations;
    private List<Person> _people;
    private List<Person> _peopleforFrontDesk;

    public PeopleModel()
    {
    }

    public PeopleModel(Election election)
    {
      _election = election;
    }

    #region FrontDeskSortEnum enum

    public enum FrontDeskSortEnum
    {
      ByArea,
      ByName
    }

    #endregion

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
        return _locations ?? (_locations = new LocationCacher().AllForThisElection);
      }
    }

    private List<Person> PeopleInElection
    {
      get
      {
        return _people ?? (_people = new PersonCacher().AllForThisElection);
      }
    }

    public IEnumerable<Person> PeopleInElectionFiltered(bool onlyIfCanVote = false, bool includeIneligible = true)
    {
      {
        return PeopleInElection
            .Where(p => !onlyIfCanVote || ((!p.CanVote.HasValue || p.CanVote.Value) && p.IneligibleReasonGuid == null))
            .Where(p => includeIneligible || p.IneligibleReasonGuid == null);
      }
    }


    /// <summary>
    ///     Process each person record, preparing it BEFORE the election starts. Altered... too dangerous to wipe information!
    /// </summary>
    public void ResetInvolvementFlags()
    {
      foreach (var person in PeopleInElection)
      {
        Db.Person.Attach(person);
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
      person.CombinedInfoAtStart = person.CombinedInfo = person.MakeCombinedInfo();

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
          PeopleInElection.SingleOrDefault(p => p.C_RowId == personId);

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
            C_FullName = person.FullName
          };
    }

    public JsonResult SavePerson(Person personFromInput)
    {
      var personInDatastore = PeopleInElectionFiltered().SingleOrDefault(p => p.C_RowId == personFromInput.C_RowId);
      var changed = false;

      if (personInDatastore == null)
      {
        if (personFromInput.C_RowId != -1)
        {
          return new
          {
            Status = "Unknown ID"
          }.AsJsonResult();
        }

        personInDatastore = new Person
        {
          PersonGuid = Guid.NewGuid(),
          ElectionGuid = CurrentElectionGuid
        };

        ResetInvolvementFlags(personInDatastore);
        Db.Person.Add(personInDatastore);

        PeopleInElection.Add(personInDatastore);

        changed = true;

      }
      else
      {
        Db.Person.Attach(personInDatastore);
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


      changed = personFromInput.CopyPropertyValuesTo(personInDatastore, editableFields) || changed;

      // these two may not be present, depending on the election type
      const string all = ElectionModel.CanVoteOrReceive.All;
      var canReceiveVotes = personFromInput.CanReceiveVotes.GetValueOrDefault(CurrentElection.CanReceive == all);
      if (personInDatastore.CanReceiveVotes != canReceiveVotes)
      {
        personInDatastore.CanReceiveVotes = canReceiveVotes;
        changed = true;
      }

      var canVote = personFromInput.CanVote.GetValueOrDefault(CurrentElection.CanVote == all);
      if (personInDatastore.CanVote != canVote)
      {
        personInDatastore.CanVote = canVote;
        changed = true;
      }

      if (changed)
      {
        SetCombinedInfos(personInDatastore);

        Db.SaveChanges();

        new PersonCacher().ReplaceEntireCache(PeopleInElection);

        UpdateFrontDeskListing(personInDatastore);
      }

      return new
          {
            Status = "Saved",
            Person = PersonForEdit(personInDatastore),
            OnFile = new PersonCacher().AllForThisElection.Count()
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
      return _peopleforFrontDesk ?? (_peopleforFrontDesk = PeopleInElectionFiltered(true).ToList());
    }

    public IEnumerable<object> OldEnvelopes()
    {
      var timeOffset = UserSession.TimeOffsetServerAhead;
      var locations = Locations.ToDictionary(l => l.LocationGuid, l => l.Name);
      var tellers = new TellerCacher().AllForThisElection.ToDictionary(t => t.TellerGuid, t => t.Name);

      var ballotSources = PeopleInElectionFiltered() // start with everyone
          .Where(p => p.EnvNum.HasValue && (string.IsNullOrEmpty(p.VotingMethod) || p.VotingMethod == VotingMethodEnum.InPerson))
          .ToList()
          .OrderBy(p => p.EnvNum)
          .Select(p => new
              {
                PersonId = p.C_RowId,
                C_FullName = p.FullName,
                VotedAt = p.VotingLocationGuid.HasValue ? locations[p.VotingLocationGuid.Value] : "",
                When = ShowRegistrationTime(timeOffset, p),
                p.VotingMethod,
                p.EnvNum,
                Tellers = ShowTellers(tellers, p)
              })
          .ToList();

      return ballotSources;
    }

    public IEnumerable<object> BallotSources(int forLocationId = -1)
    {
      var locations = Locations.ToDictionary(l => l.LocationGuid, l => l.Name);
      var forLocationGuid = forLocationId == -1
                                ? Guid.Empty
                                : Locations.Where(l => l.C_RowId == forLocationId)
                                           .Select(l => l.LocationGuid)
                                           .Single();
      var timeOffset = UserSession.TimeOffsetServerAhead;
      var tellers = new TellerCacher().AllForThisElection.ToDictionary(t => t.TellerGuid, t => t.Name);

      var ballotSources = PeopleInElectionFiltered() // start with everyone
          .Where(p => !string.IsNullOrEmpty(p.VotingMethod))
          .Where(p => forLocationId == -1 || p.VotingLocationGuid == forLocationGuid)
          .ToList()
          .OrderBy(p => p.VotingMethod)
          .ThenBy(p => p.RegistrationTime)
          .Select(p => new
              {
                PersonId = p.C_RowId,
                C_FullName = p.FullName,
                VotedAt = p.VotingLocationGuid.HasValue ? locations[p.VotingLocationGuid.Value] : "",
                When = ShowRegistrationTime(timeOffset, p),
                p.VotingMethod,
                EnvNum = ShowEnvNum(p),
                Tellers = ShowTellers(tellers, p)
              })
          .ToList();

      var location = ContextItems.LocationModel.HasLocations && forLocationGuid.HasContent()
                         ? new LocationCacher().AllForThisElection.Single(l => l.LocationGuid == forLocationGuid)
                         : new LocationCacher().AllForThisElection.Single(l => l.LocationGuid == UserSession.CurrentLocationGuid);

      if (location.BallotsCollected.AsInt() == 0)
      {
        location.BallotsCollected = ballotSources.Count;
        Db.SaveChanges();

        if (location.LocationGuid != UserSession.CurrentLocationGuid)
        {
          UserSession.CurrentLocationGuid = location.LocationGuid;
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
      var tellers = new TellerCacher().AllForThisElection.ToDictionary(t => t.TellerGuid, t => t.Name);
      var timeOffset = UserSession.TimeOffsetServerAhead;

      return people
          .OrderBy(p => sortType == FrontDeskSortEnum.ByArea ? p.Area : "")
          .ThenBy(p => p.LastName)
          .ThenBy(p => p.FirstName)
          .ThenBy(p => p.C_RowId)
          .Select(p => new
              {
                PersonId = p.C_RowId,
                p.FullName,
                NameLower = p.FullName.WithoutDiacritics(true).ReplacePunctuation(' ').Replace("\"", "\\\""),
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

    public JsonResult RegisterVotingMethod(int personId, string voteType, long lastRowVersion)
    {
      if (!VotingMethodEnum.Exists(voteType))
      {
        return new { Message = "Invalid type" }.AsJsonResult();
      }

      var person = new PersonCacher().AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);
      if (person == null)
      {
        return new { Message = "Unknown person" }.AsJsonResult();
      }

      Db.Person.Attach(person);

      var votingMethodRemoved = false;
      if (person.VotingMethod == voteType)
      {
        // it is already set this way...turn if off
        person.VotingMethod = null;
        person.VotingLocationGuid = null;
        person.RegistrationTime = null;
        votingMethodRemoved = true;
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
            //var election = new ElectionModel().GetFreshFromDb(currentElectionGuid);
            var election = UserSession.CurrentElection;

            var nextNum = election.LastEnvNum.AsInt() + 1;

            Db.Election.Attach(election);

            person.EnvNum = nextNum;
            election.LastEnvNum = nextNum;

            new ElectionCacher().UpdateItemAndSaveCache(election);
          }
        }
      }

      person.TellerAtKeyboard = UserSession.GetCurrentTeller(1);
      person.TellerAssisting = UserSession.GetCurrentTeller(2);

      Db.SaveChanges();

      new PersonCacher().UpdateItemAndSaveCache(person);

      if (lastRowVersion == 0)
      {
        lastRowVersion = person.C_RowVersionInt.AsLong() - 1;
      }

      UpdateFrontDeskListing(person, votingMethodRemoved);

      return true.AsJsonResult();
    }

    /// <summary>
    /// Update listing for everyone updated since this version
    /// </summary>
    /// <param name="lastRowVersion"></param>
    /// <param name="votingMethodRemoved"></param>
//    public void UpdateFrontDeskListing(long lastRowVersion, bool votingMethodRemoved)
//    {
//      UpdateFrontDeskListing(new PersonCacher().AllForThisElection
//          .Where(p => p.C_RowVersionInt > lastRowVersion)
//          .ToList(), votingMethodRemoved);
//    }

    /// <summary>
    /// Update listing for just one person
    /// </summary>
    /// <param name="person"></param>
    public void UpdateFrontDeskListing(Person person)
    {
      UpdateFrontDeskListing(person, false);
    }

    /// <summary>
    /// Update listing
    /// </summary>
    /// <param name="person">The people to update</param>
    /// <param name="votingMethodRemoved"></param>
    public void UpdateFrontDeskListing(Person person, bool votingMethodRemoved)
    {
      var updateInfo = new
      {
        PersonLines = FrontDeskPersonLines(new List<Person>{person}),
        LastRowVersion = person.C_RowVersionInt
      };
      new FrontDeskHub().UpdateAllConnectedClients(updateInfo);


      var oldestStamp = person.C_RowVersionInt.AsLong() - 5; // send last 5, to ensure none are missed
      long newStamp;
      var rollCallModel = new RollCallModel();
      var rollCallInfo = new
      {
        changed = rollCallModel.GetMorePeople(oldestStamp, out newStamp),
        removedId = votingMethodRemoved ? person.C_RowId : 0,
        newStamp
      };
      if (rollCallInfo.newStamp != 0 || rollCallInfo.removedId != 0)
      {
        new RollCallHub().UpdateAllConnectedClients(rollCallInfo);
      }
    }

    public JsonResult DeleteAllPeople()
    {
      int rows;
      try
      {
        rows = Db.Person.Delete(p => p.ElectionGuid == CurrentElectionGuid);
      }
      catch (SqlException)
      {
        return
            new
                {
                  Results = "Nothing was deleted. Once votes have been recorded, you cannot delete all the people"
                }.
                AsJsonResult();
      }

      new PersonCacher().DropThisCache();

      return new { Results = "{0} {1} deleted".FilledWith(rows, rows.Plural("people", "person")) }.AsJsonResult();
    }
  }
}