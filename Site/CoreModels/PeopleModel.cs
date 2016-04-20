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
    //private List<Person> _peopleforFrontDesk;

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
      get { return _locations ?? (_locations = new LocationCacher(Db).AllForThisElection); }
    }

    private List<Person> PeopleInElection
    {
      get { return _people ?? (_people = new PersonCacher(Db).AllForThisElection); }
    }

    public List<Person> PeopleWhoCanVote() //, bool includeIneligible = true
    {
      {
        return PeopleInElection
          .Where(p => p.CanVote.HasValue && p.CanVote.Value)
          .ToList();
      }
    }


    /// <summary>
    ///   Process each person record, preparing it BEFORE the election starts. Altered... too dangerous to wipe information!
    /// </summary>
    public void SetInvolvementFlagsToDefault()
    {
      var personCacher = new PersonCacher(Db);
      personCacher.DropThisCache();

      var peopleInElection = personCacher.MainQuery().ToList();
      var reason = new ElectionModel().GetDefaultIneligibleReason();
      var counter = 0;
      foreach (var person in peopleInElection)
      {
        SetInvolvementFlagsToDefault(person, reason);

        if (counter++ > 500)
        {
          Db.SaveChanges();
          counter = 0;
        }
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
    ///   Set person's flag based on what is default for this election
    /// </summary>
    public void SetInvolvementFlagsToDefault(Person person, IneligibleReasonEnum reason)
    {
      //var canVote = true; // person.AgeGroup.HasNoContent() || person.AgeGroup == AgeGroup.Adult;
      //person.IneligibleReasonGuid = canVote ? null : IneligibleReasonEnum.Ineligible_Not_Adult;

      //      if (person.IneligibleReasonGuid.HasValue)
      //      {
      //        var reason1 = IneligibleReasonEnum.Get(person.IneligibleReasonGuid.Value);
      //
      //        person.CanVote = reason1.CanVote;
      //        person.CanReceiveVotes = reason1.CanReceiveVotes;
      //        return;
      //      }

      //      var reason = new ElectionModel().GetDefaultIneligibleReason();
      if (reason != null)
      {
        person.IneligibleReasonGuid = reason;
        person.CanVote = reason.CanVote;
        person.CanReceiveVotes = reason.CanReceiveVotes;
      }
      else
      {
        person.IneligibleReasonGuid = null;
        person.CanVote = true;
        person.CanReceiveVotes = true;
      }
    }

    /// <summary>
    ///   Ensure the flags match the Guid
    /// </summary>
    /// <param name="people"></param>
    /// <param name="hub"></param>
    /// <param name="personSaver"></param>
    public void EnsureFlagsAreRight(List<Person> people, IStatusUpdateHub hub, Action<DbAction, Person> personSaver)
    {
      hub.StatusUpdate("Reviewing people", true);
      var currentElectionGuid = UserSession.CurrentElectionGuid;

      var numDone = 0;
      foreach (var person in people)
      {
        var changesMade = false;
        var unknownIneligibleGuid = false;

        numDone++;
        if (numDone % 10 == 0)
        {
          hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone), true);
        }

        if (currentElectionGuid != person.ElectionGuid)
        {
          hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone));
          hub.StatusUpdate("Found unexpected person. Please review. Name: " + person.C_FullNameFL);
        }

        if (person.IneligibleReasonGuid.HasValue)
        {
          var reason = IneligibleReasonEnum.Get(person.IneligibleReasonGuid);
          if (reason == null)
          {
            unknownIneligibleGuid = true;
          }
          else
          {
            var canVote = reason.CanVote;
            var canReceiveVotes = reason.CanReceiveVotes;

            if (canVote != person.CanVote || canReceiveVotes != person.CanReceiveVotes)
            {
              personSaver(DbAction.Attach, person);
              person.CanVote = canVote;
              person.CanReceiveVotes = canReceiveVotes;
              changesMade = true;
            }
          }
        }
        if (unknownIneligibleGuid)
        {
          personSaver(DbAction.Attach, person);
          person.IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other;

          hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone));
          hub.StatusUpdate("Found unknown ineligible reason. Set to Unknown. Name: " + person.C_FullNameFL);

          changesMade = true;
        }
        if (changesMade)
        {
          personSaver(DbAction.Save, person);
        }
      }
      hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone));
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
        C_FullName = person.C_FullNameFL
      };
    }

    public JsonResult SavePerson(Person personFromInput)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      var personInDatastore = PeopleInElection.SingleOrDefault(p => p.C_RowId == personFromInput.C_RowId);
      var changed = false;

      if (personInDatastore == null)
      {
        if (personFromInput.C_RowId != -1)
        {
          return new
          {
            Message = "Unknown ID"
          }.AsJsonResult();
        }

        // create new
        personInDatastore = new Person
        {
          PersonGuid = Guid.NewGuid(),
          ElectionGuid = CurrentElectionGuid
        };

        var reason = new ElectionModel().GetDefaultIneligibleReason();
        SetInvolvementFlagsToDefault(personInDatastore, reason);
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

        new PersonCacher(Db).ReplaceEntireCache(PeopleInElection);

        UpdateFrontDeskListing(personInDatastore);
      }

      var persons = new PersonCacher(Db).AllForThisElection;
      return new
      {
        Status = "Saved",
        Person = PersonForEdit(personInDatastore),
        OnFile = persons.Count(),
        Eligible = persons.Count(p => p.IneligibleReasonGuid == null),
      }.AsJsonResult();
    }

    /// <Summary>Everyone</Summary>
    public IEnumerable<object> FrontDeskPersonLines(FrontDeskSortEnum sortType = FrontDeskSortEnum.ByName)
    {
      return FrontDeskPersonLines(PeopleWhoCanVote());
    }


    private string LocationName(Guid? location)
    {
      if (!location.HasValue)
      {
        return "";
      }
      var matched = Locations.FirstOrDefault(l => l.LocationGuid == location.Value);
      if (matched == null)
      {
        return "?";
      }
      return matched.Name;
    }

    public IEnumerable<object> OldEnvelopes()
    {
      var timeOffset = UserSession.TimeOffsetServerAhead;

      var ballotSources = PeopleInElection // start with everyone
        .Where(
          p =>
            p.EnvNum.HasValue && (string.IsNullOrEmpty(p.VotingMethod) || p.VotingMethod == VotingMethodEnum.InPerson)
            || (string.IsNullOrEmpty(p.VotingMethod) && (p.Teller1 != null || p.Teller2 != null)))
        .ToList()
        .OrderBy(p => p.EnvNum)
        .Select(p => new
        {
          PersonId = p.C_RowId,
          C_FullName = p.FullName,
          VotedAt = LocationName(p.VotingLocationGuid),
          When = ShowRegistrationTime(timeOffset, p),
          p.VotingMethod,
          p.EnvNum,
          Tellers = ShowTellers(p)
        })
        .ToList();

      return ballotSources;
    }

    const int NotSet = -1;
    const int WantAllLocations = -2;

    public IEnumerable<object> BallotSources(int forLocationId = WantAllLocations)
    {
      if (forLocationId == NotSet)
      {
        return new List<string>();
      }
      var forLocationGuid = forLocationId == WantAllLocations
        ? Guid.Empty
        : Locations.Where(l => l.C_RowId == forLocationId)
          .Select(l => l.LocationGuid)
          .Single();
      var timeOffset = UserSession.TimeOffsetServerAhead;

      var ballotSources = PeopleInElection // start with everyone
        .Where(p => !string.IsNullOrEmpty(p.VotingMethod))
        .Where(p => forLocationId == WantAllLocations || p.VotingLocationGuid == forLocationGuid)
        .ToList()
        .OrderBy(p => p.VotingMethod)
        .ThenByDescending(p => p.RegistrationTime)
        .Select(p => new
        {
          PersonId = p.C_RowId,
          C_FullName = p.FullName,
          VotedAt = LocationName(p.VotingLocationGuid),
          When = ShowRegistrationTime(timeOffset, p),
          p.RegistrationTime,
          p.VotingMethod,
          EnvNum = ShowEnvNum(p),
          Tellers = ShowTellers(p)
        })
        .ToList();

      //var location = ContextItems.LocationModel.HasLocations && forLocationGuid.HasContent()
      //  ? new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == forLocationGuid)
      //  : new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == UserSession.CurrentLocationGuid);

      //      if (location.BallotsCollected.AsInt() == 0)
      //      {
      //        location.BallotsCollected = ballotSources.Count;
      //        Db.SaveChanges();
      //      }

      //      if (location.LocationGuid != UserSession.CurrentLocationGuid)
      //      {
      //        UserSession.CurrentLocationGuid = location.LocationGuid;
      //      }


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
      var showLocations = Locations.Count() > 1;
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
            showLocations ? LocationName(p.VotingLocationGuid) : "",
            ShowTellers(p),
            ShowRegistrationTime(timeOffset, p)
          }.JoinedAsString("; ", true),
          InPerson = p.VotingMethod == VotingMethodEnum.InPerson,
          DroppedOff = p.VotingMethod == VotingMethodEnum.DroppedOff,
          MailedIn = p.VotingMethod == VotingMethodEnum.MailedIn,
          CalledIn = p.VotingMethod == VotingMethodEnum.CalledIn,
          EnvNum = ShowEnvNum(p),
          p.BahaiId
        });
    }

    private static int? ShowEnvNum(Person p)
    {
      return p.VotingMethod.DefaultTo(VotingMethodEnum.InPerson) == VotingMethodEnum.InPerson
        ? null
        : p.EnvNum;
    }

    private static string ShowTellers(Person p)
    {
      var names = new List<string>
      {
        p.Teller1,
        p.Teller2
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
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized) {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }
      var locationModel = new LocationModel();
      if (locationModel.HasLocations && UserSession.CurrentLocation == null) {
        return new { Message = "Must select your location first!" }.AsJsonResult();
      }
      if (UserSession.GetCurrentTeller(1).HasNoContent()) {
        return new { Message = "Must select \"Teller at Keyboard\" first!" }.AsJsonResult();
      }
      if (!VotingMethodEnum.Exists(voteType))
      {
        return new { Message = "Invalid type" }.AsJsonResult();
      }

      var personCacher = new PersonCacher(Db);
      var person = personCacher.AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);
      if (person == null)
      {
        return new { Message = "Unknown person" }.AsJsonResult();
      }

      Db.Person.Attach(person);

      var votingMethodRemoved = false;
      Guid? oldVoteLocationGuid = null;
      Guid? newVoteLocationGuid = null;
      if (person.VotingMethod == voteType)
      {
        oldVoteLocationGuid = person.VotingLocationGuid;

        // it is already set this way...turn if off
        person.VotingMethod = null;
        person.VotingLocationGuid = null;
        person.RegistrationTime = null;
        votingMethodRemoved = true;
      }
      else
      {
        person.VotingMethod = voteType;

        oldVoteLocationGuid = person.VotingLocationGuid;

        person.VotingLocationGuid = UserSession.CurrentLocationGuid;
        person.RegistrationTime = DateTime.Now;

        newVoteLocationGuid = person.VotingLocationGuid;

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

            new ElectionCacher(Db).UpdateItemAndSaveCache(election);
          }
        }
      }

      person.Teller1 = UserSession.GetCurrentTeller(1);
      person.Teller2 = UserSession.GetCurrentTeller(2);

      Db.SaveChanges();

      personCacher.UpdateItemAndSaveCache(person);

      UpdateFrontDeskListing(person, votingMethodRemoved);


      if (lastRowVersion == 0)
      {
        lastRowVersion = person.C_RowVersionInt.AsLong() - 1;
      }

      UpdateLocationCounts(newVoteLocationGuid, oldVoteLocationGuid, personCacher);

      return true.AsJsonResult();
    }

    private void UpdateLocationCounts(Guid? newVoteLocationGuid, Guid? oldVoteLocationGuid, PersonCacher personCacher)
    {
      // would be great to throw this into a remote queue to be processed later
      var locationCacher = new LocationCacher(Db);
      var saveNeeded = false;

      UpdateLocation(newVoteLocationGuid, personCacher, locationCacher, ref saveNeeded);

      if (oldVoteLocationGuid != null && oldVoteLocationGuid != newVoteLocationGuid)
      {
        UpdateLocation(oldVoteLocationGuid, personCacher, locationCacher, ref saveNeeded);
      }

      if (saveNeeded)
      {
        Db.SaveChanges();
      }
    }

    private void UpdateLocation(Guid? locationGuid, PersonCacher personCacher, LocationCacher locationCacher,
      ref bool saveNeeded)
    {
      if (locationGuid == null)
      {
        return;
      }
      var location = locationCacher.AllForThisElection.FirstOrDefault(l => l.LocationGuid == locationGuid);
      if (location == null)
      {
        return;
      }

      var oldCount = location.BallotsCollected;
      var newCount = personCacher.AllForThisElection.Count(
        p => p.VotingLocationGuid == location.LocationGuid && !String.IsNullOrEmpty(p.VotingMethod));
      if (oldCount == newCount)
      {
        return;
      }

      Db.Location.Attach(location);

      location.BallotsCollected = newCount;

      locationCacher.UpdateItemAndSaveCache(location);
      saveNeeded = true;
    }

    //    public void UpdateFrontDeskListing(long lastRowVersion, bool votingMethodRemoved)
    //    {
    //      UpdateFrontDeskListing(new PersonCacher(Db).AllForThisElection
    //          .Where(p => p.C_RowVersionInt > lastRowVersion)
    //          .ToList(), votingMethodRemoved);
    //    }
    /// <summary>
    ///   Update listing for everyone updated since this version
    /// </summary>
    /// <param name="lastRowVersion"></param>
    /// <param name="votingMethodRemoved"></param>
    /// <summary>
    ///   Update listing for just one person
    /// </summary>
    /// <param name="person"></param>
    public void UpdateFrontDeskListing(Person person)
    {
      UpdateFrontDeskListing(person, false);
    }

    /// <summary>
    ///   Update listing
    /// </summary>
    /// <param name="person">The people to update</param>
    /// <param name="votingMethodRemoved"></param>
    public void UpdateFrontDeskListing(Person person, bool votingMethodRemoved)
    {
      var updateInfo = new
      {
        PersonLines = FrontDeskPersonLines(new List<Person> { person }),
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
        rows = Db.Person.Where(p => p.ElectionGuid == CurrentElectionGuid).Delete();
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

      new PersonCacher(Db).DropThisCache();

      return new { Results = "{0} {1} deleted".FilledWith(rows, rows.Plural("people", "person")) }.AsJsonResult();
    }
  }
}