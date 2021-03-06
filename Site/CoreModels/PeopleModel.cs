using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
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
            .Where(p => p.CanVote.AsBoolean())
            .ToList();
      }
    }


    // /// <summary>
    // ///   Process each person record, preparing it BEFORE the election starts. Altered... too dangerous to wipe information!
    // /// </summary>
    // public void SetInvolvementFlagsToDefault()
    // {
    //   var personCacher = new PersonCacher();
    //   personCacher.DropThisCache();
    //
    //   var peopleInElection = personCacher.AllForThisElection;
    //   var reason = new ElectionModel().GetDefaultIneligibleReason();
    //   var counter = 0;
    //   foreach (var person in peopleInElection)
    //   {
    //     SetInvolvementFlagsToDefault(person, reason);
    //
    //     if (counter++ > 500)
    //     {
    //       Db.SaveChanges();
    //       counter = 0;
    //     }
    //   }
    //
    //   Db.SaveChanges();
    // }

    /// <Summary>Only to be done before an election</Summary>
    public void SetCombinedInfoAtStart(Person person)
    {
      person.CombinedInfoAtStart = person.CombinedInfo = person.MakeCombinedInfo();

      // person.UpdateCombinedSoundCodes();
    }

    public void SetCombinedInfos(Person person)
    {
      person.CombinedInfo = person.MakeCombinedInfo();
      //person.UpdateCombinedSoundCodes();
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

      // var defaultCanVote = UserSession.CurrentElection.CanVote == "A";
      // var defaultCanReceiveVotes = UserSession.CurrentElection.CanReceive == "A";

      var numDone = 0;
      foreach (var person in people)
      {
        var changesMade = false;

        numDone++;
        if (numDone % 10 == 0)
        {
          hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone), true);
        }

        if (currentElectionGuid != person.ElectionGuid)
        {
          hub.StatusUpdate("Found unexpected person. Please review. Name: " + person.C_FullName);
        }

        var matchedReason = IneligibleReasonEnum.Get(person.IneligibleReasonGuid);
        if (person.IneligibleReasonGuid.HasValue && matchedReason == null)
        {
          personSaver(DbAction.Attach, person);
          person.IneligibleReasonGuid = IneligibleReasonEnum.Ineligible_Other;

          hub.StatusUpdate("Found unknown ineligible reason. Set to Unknown. Name: " + person.C_FullName);

          changesMade = true;
        }

        if (ApplyVoteReasonFlags(person))
        {
          personSaver(DbAction.Attach, person);
          changesMade = true;
        }
        // var reason = IneligibleReasonEnum.Get(person.IneligibleReasonGuid);
        // if (reason == null)
        // {
        //   unknownIneligibleGuid = true;
        // }
        // else
        // {
        //   var canVote = reason.CanVote;
        //   var canReceiveVotes = reason.CanReceiveVotes;
        //
        //   if (canVote != person.CanVote || canReceiveVotes != person.CanReceiveVotes)
        //   {
        //     personSaver(DbAction.Attach, person);
        //     person.CanVote = canVote;
        //     person.CanReceiveVotes = canReceiveVotes;
        //   }
        // }
        // else
        // {
        //   if (defaultCanVote != person.CanVote || defaultCanReceiveVotes != person.CanReceiveVotes)
        //   {
        //     personSaver(DbAction.Attach, person);
        //     person.CanVote = defaultCanVote;
        //     person.CanReceiveVotes = defaultCanReceiveVotes;
        //     changesMade = true;
        //   }
        // }

        if (changesMade)
        {
          //          personSaver(DbAction.Save, person);
          Db.SaveChanges();
        }
      }

      hub.StatusUpdate("Reviewed {0} people".FilledWith(numDone));
    }

    /// <summary>
    ///   Set person's flag based on what is default for this election
    /// </summary>
    public bool ApplyVoteReasonFlags(Person person)
    {
      // using only what they have

      var changed = false;

      var reason = IneligibleReasonEnum.Get(person.IneligibleReasonGuid);

      if (reason == null)
      {
        // no reason, so set to true
        if (!person.CanVote.GetValueOrDefault())
        {
          person.CanVote = true;
          changed = true;
        }
        if (!person.CanReceiveVotes.GetValueOrDefault())
        {
          person.CanReceiveVotes = true;
          changed = true;
        }
      }
      else
      {
        // update to match what this reason implies
        if (person.CanVote != reason.CanVote)
        {
          person.CanVote = reason.CanVote;
          changed = true;
        }
        if (person.CanReceiveVotes != reason.CanReceiveVotes)
        {
          person.CanReceiveVotes = reason.CanReceiveVotes;
          changed = true;
        }
      }

      return changed;
    }


    public JsonResult DetailsFor(int personId)
    {
      var person = PeopleInElection.SingleOrDefault(p => p.C_RowId == personId);

      if (person == null)
      {
        return new
        {
          Error = "Unknown person"
        }.AsJsonResult();
      }

      //var whoCanVote = CurrentElection.CanVote;
      //var whoCanReceiveVotes = CurrentElection.CanReceive;
      var voteCacher = new VoteCacher(Db);
      var votedFor = voteCacher.AllForThisElection.Any(v => v.PersonGuid == person.PersonGuid);

      return new
      {
        Person = PersonForEdit(person),
        CanDelete = person.VotingMethod == null && !votedFor
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
        person.Email,
        person.Phone,
        person.C_FullName
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

        Db.Person.Add(personInDatastore);

        PeopleInElection.Add(personInDatastore);

        changed = true;
      }
      else
      {
        Db.Person.Attach(personInDatastore);
      }

      var beforeChanges = personInDatastore.GetAllProperties();

      if (personFromInput.IneligibleReasonGuid == Guid.Empty)
      {
        personFromInput.IneligibleReasonGuid = null;
      }

      if (personFromInput.Phone != null)
      {
        //check via Twilio to ensure real number?
        if (!new Regex(@"\+[0-9]{4,15}").IsMatch(personFromInput.Phone))
        {
          return new
          {
            Message = "Invalid phone number. Must start with + and only contain digits."
          }.AsJsonResult();
        }
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
        personFromInput.Email,
        personFromInput.Phone,
      }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();


      changed = personFromInput.CopyPropertyValuesTo(personInDatastore, editableFields) || changed;

      // var defaultReason = new ElectionModel().GetDefaultIneligibleReason();
      if (ApplyVoteReasonFlags(personInDatastore))
      {
        changed = true;
      }

      // const string all = ElectionModel.CanVoteOrReceive.All;
      // var canReceiveVotes = personFromInput.CanReceiveVotes.AsBoolean(CurrentElection.CanReceive == all);
      // if (personInDatastore.CanReceiveVotes != canReceiveVotes)
      // {
      //   personInDatastore.CanReceiveVotes = canReceiveVotes;
      //   changed = true;
      // }
      //
      // var canVote = personFromInput.CanVote.AsBoolean(CurrentElection.CanVote == all);
      // if (personInDatastore.CanVote != canVote)
      // {
      //   personInDatastore.CanVote = canVote;
      //   changed = true;
      // }

      if (changed)
      {
        SetCombinedInfos(personInDatastore);

        try
        {
          Db.SaveChanges();
        }
        catch (Exception e)
        {
          // revert person object back to what it was
          beforeChanges.CopyPropertyValuesTo(personInDatastore);

          if (e.GetAllMsgs(";").Contains("IX_PersonEmail"))
          {
            return new
            {
              Message = "That email is registered with another person.",
              Person = PersonForEdit(personInDatastore),
            }.AsJsonResult();
          }

          if (e.GetAllMsgs(";").Contains("IX_PersonPhone"))
          {
            return new
            {
              Message = "That phone number is registered with another person.",
              Person = PersonForEdit(personInDatastore),
            }.AsJsonResult();
          }

          return new
          {
            e.LastException().Message
          }.AsJsonResult();
        }

        new PersonCacher(Db).ReplaceEntireCache(PeopleInElection);

        UpdateFrontDeskListing(personInDatastore);
      }

      var persons = new PersonCacher(Db).AllForThisElection;
      return new
      {
        Status = "Saved",
        Person = PersonForEdit(personInDatastore),
        OnFile = persons.Count(),
        Eligible = persons.Count(p => p.IneligibleReasonGuid == null), //TODO? split to: can vote, can receive votes
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

    public IEnumerable<object> Deselected()
    {
      var hasMultipleLocations = ContextItems.LocationModel.HasMultipleLocations;

      var ballotSources = PeopleInElection // start with everyone
          .Where(
              p =>
                  p.EnvNum.HasValue && string.IsNullOrEmpty(p.VotingMethod)
                  || (string.IsNullOrEmpty(p.VotingMethod) && (p.Teller1 != null || p.Teller2 != null)))
          .ToList()
          .OrderBy(p => p.EnvNum)
          .Select(p => new
          {
            PersonId = p.C_RowId,
            C_FullName = p.FullName,
            VotedAt = hasMultipleLocations ? LocationName(p.VotingLocationGuid) : null,
            When = ShowRegistrationTime(p),
            p.RegistrationTime,
            Log = FormatRegistrationLog(p),
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

      var ballotSources = PeopleInElection // start with everyone
          .Where(p => !string.IsNullOrEmpty(p.VotingMethod))
          .Where(p => p.VotingMethod != VotingMethodEnum.Online)
          .Where(p => p.VotingMethod != VotingMethodEnum.Imported)
          .Where(p => p.VotingMethod != VotingMethodEnum.Registered)
          .Where(p => forLocationId == WantAllLocations || p.VotingLocationGuid == forLocationGuid)
          .ToList()
          .OrderBy(p => p.VotingMethod)
          .ThenByDescending(p => p.RegistrationTime)
          .Select(p => new
          {
            PersonId = p.C_RowId,
            C_FullName = p.FullName,
            VotedAt = LocationName(p.VotingLocationGuid),
            When = ShowRegistrationTime(p),
            p.RegistrationTime,
            Log = FormatRegistrationLog(p),
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
      var peopleCount = people.Count;
      if (peopleCount == 0)
      {
        return  new List<object>();
      }
      var hasMultipleLocations = ContextItems.LocationModel.HasMultipleLocations;
      var useOnline = UserSession.CurrentElection.OnlineEnabled;
      var firstPersonGuid = people[0].PersonGuid;

      var onlineProcessed = Db.OnlineVotingInfo
        .Where(ovi => ovi.ElectionGuid == UserSession.CurrentElectionGuid)
        .Where(ovi => ovi.Status == OnlineBallotStatusEnum.Processed.Value)
        .Where(ovi => peopleCount != 1 || firstPersonGuid == ovi.PersonGuid)
        .Select(ovi => ovi.PersonGuid)
        .ToList();

      return people
          .OrderBy(p => sortType == FrontDeskSortEnum.ByArea ? p.Area : "")
          .ThenBy(p => p.LastName)
          .ThenBy(p => p.FirstName)
          .ThenBy(p => p.C_RowId)
          .Select(p => new
          {
            PersonId = p.C_RowId,
            p.FullName,
            NameLower = (p.FullName + p.BahaiId).WithoutDiacritics(true).ReplacePunctuation(' ')
                  .Replace(" ", "").Replace("\"", "\\\""),
            p.Area,
            VotedAt = new[]
              {
                        ShowRegistrationTime(p),
                        ShowTellers(p),
                        hasMultipleLocations ? LocationName(p.VotingLocationGuid) : "",
              }.JoinedAsString("; ", true)
                      + FormatRegistrationLog(p),
            p.VotingMethod,
            InPerson = p.VotingMethod == VotingMethodEnum.InPerson,
            DroppedOff = p.VotingMethod == VotingMethodEnum.DroppedOff,
            MailedIn = p.VotingMethod == VotingMethodEnum.MailedIn,
            CalledIn = p.VotingMethod == VotingMethodEnum.CalledIn,
            Custom1 = p.VotingMethod == VotingMethodEnum.Custom1,
            Custom2 = p.VotingMethod == VotingMethodEnum.Custom2,
            Custom3 = p.VotingMethod == VotingMethodEnum.Custom3,
            Imported = p.VotingMethod == VotingMethodEnum.Imported,
            Online = useOnline && p.VotingMethod == VotingMethodEnum.Online,
            HasOnline = useOnline && p.HasOnlineBallot.GetValueOrDefault(),
            CanBeOnline = useOnline && (p.VotingMethod == VotingMethodEnum.Online || p.Email.HasContent() || p.Phone.HasContent()), // consider VotingMethod in case email/phone removed after
            OnlineProcessed = onlineProcessed.Contains(p.PersonGuid),
            Registered = p.VotingMethod == VotingMethodEnum.Registered,
            EnvNum = ShowEnvNum(p),
            p.CanVote,
            p.CanReceiveVotes, // for ballot entry page
            p.IneligibleReasonGuid, // for ballot entry page
            p.BahaiId
          });
    }

    private string FormatRegistrationLog(Person p)
    {
      return p.RegistrationLog.Count > 1
          ? p.RegistrationLog.JoinedAsString("\n").SurroundContentWith(" <span class=Log title=\"", "\"></span>")
          : "";
    }

    private static int? ShowEnvNum(Person p)
    {
      return p.EnvNum;

      //let client show/hide
      //return p.VotingMethod.HasNoContent() || p.VotingMethod == VotingMethodEnum.Registered
      //  ? null
      //  : p.EnvNum;
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

    public string ShowRegistrationTime(Person p, bool includeDate = false)
    {
      var timeOffset = UserSession.TimeOffsetServerAhead;
      var format = UserSession.CurrentElection.T24 ? "HH:mm" : "h:mm tt";
      if (includeDate)
      {
        format = "MMM d -" + format;
      }
      return p.RegistrationTime.HasValue
          ? p.RegistrationTime.Value.AddMilliseconds(0 - timeOffset).ToString(format)
          : "";
    }

    public JsonResult RegisterVotingMethod(int personId, string voteType, bool forceDeselect, int locationId)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      var locationModel = new LocationModel();

      var hasMultiplePhysicalLocations = locationModel.HasMultiplePhysicalLocations;

      if (hasMultiplePhysicalLocations && UserSession.CurrentLocation == null)
      {
        return new { Message = "Must select your location first!" }.AsJsonResult();
      }

      if (UserSession.CurrentLocation.C_RowId != locationId)
      {
        new ComputerModel().MoveCurrentComputerIntoLocation(locationId);
      }

      if (UserSession.GetCurrentTeller(1).HasNoContent())
      {
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

      if (person.VotingMethod == VotingMethodEnum.Online)
      {
        var onlineVoter = Db.OnlineVotingInfo.SingleOrDefault(ovi => ovi.PersonGuid == person.PersonGuid && ovi.ElectionGuid == person.ElectionGuid);
        if (onlineVoter != null)
        {
          if (onlineVoter.Status == OnlineBallotStatusEnum.Processed)
          {
            return new { Message = "This online ballot has been processed. Registration cannot be changed." }.AsJsonResult();
          }
        }
      }

      Db.Person.Attach(person);

      person.Teller1 = UserSession.GetCurrentTeller(1);
      person.Teller2 = UserSession.GetCurrentTeller(2);

      var votingMethodRemoved = false;
      Guid? oldVoteLocationGuid = null;
      Guid? newVoteLocationGuid = null;
      if (person.VotingMethod == voteType || forceDeselect || !person.CanVote.AsBoolean())
      {
        oldVoteLocationGuid = person.VotingLocationGuid;

        // it is already set this way...turn if off
        person.VotingMethod = null;
        person.VotingLocationGuid = null;
        person.RegistrationTime = DateTime.Now;
        votingMethodRemoved = true;

        var log = person.RegistrationLog;
        log.Add(new[]
        {
                    ShowRegistrationTime(person),
                    "De-selected",
                    ShowTellers(person),
                    hasMultiplePhysicalLocations ? LocationName(UserSession.CurrentLocationGuid) : null,
                }.JoinedAsString("; ", true));
        person.RegistrationLog = log;
      }
      else
      {
        person.VotingMethod = voteType;

        oldVoteLocationGuid = person.VotingLocationGuid;

        person.VotingLocationGuid = UserSession.CurrentLocationGuid;
        person.RegistrationTime = DateTime.Now;

        newVoteLocationGuid = person.VotingLocationGuid;

        var log = person.RegistrationLog;
        log.Add(new[]
        {
                    ShowRegistrationTime(person),
                    VotingMethodEnum.TextFor(person.VotingMethod),
                    ShowTellers(person),
                    hasMultiplePhysicalLocations ? LocationName(UserSession.CurrentLocationGuid) : null,
        }.JoinedAsString("; ", true));
        person.RegistrationLog = log;

        // make number for every method except Registered
        var needEnvNum = person.EnvNum == null && voteType != VotingMethodEnum.Registered;

        if (needEnvNum)
        {
          person.EnvNum = new ElectionModel().GetNextEnvelopeNumber();
        }
      }

      personCacher.UpdateItemAndSaveCache(person);

      UpdateFrontDeskListing(person, votingMethodRemoved);

      //      if (lastRowVersion == 0)
      //      {
      //        lastRowVersion = person.C_RowVersionInt.AsLong() - 1;
      //      }

      UpdateLocationCounts(newVoteLocationGuid, oldVoteLocationGuid, personCacher);

      Db.SaveChanges();

      return true.AsJsonResult();
    }

    private void UpdateLocationCounts(Guid? newVoteLocationGuid, Guid? oldVoteLocationGuid,
        PersonCacher personCacher)
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
      new FrontDeskHub().UpdatePeople(updateInfo);
      new VoterPersonalHub().Update(person);

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
      var hasOnline = Db.OnlineVotingInfo.Any(p => p.ElectionGuid == CurrentElectionGuid && p.ListPool != null);
      if (hasOnline)
      {
        return new
        {
          Success = false,
          Results = "Nothing was deleted. Once online votes have been recorded, you cannot delete all the people. ",
        }.AsJsonResult();
      }

      var rows = 0;
      try
      {
        int newRows;
        do
        {
          newRows = Db.Person.Where(p => p.ElectionGuid == CurrentElectionGuid).Take(500).Delete();
          rows += newRows;
        } while (newRows > 0);

        int oviRows;
        do
        {
          oviRows = Db.OnlineVotingInfo.Where(p => p.ElectionGuid == CurrentElectionGuid).Take(500).Delete();
        } while (oviRows > 0);
      }
      catch (Exception)
      {
        return
            new
            {
              Success = false,
              Results =
                    "Nothing was deleted. Once votes have been recorded, you cannot delete all the people. ",
            }.AsJsonResult();
      }

      new PersonCacher(Db).DropThisCache();

      return new
      {
        Success = true,
        Results = $"{rows:N0} {rows.Plural("people", "person")} deleted",
        count = NumberOfPeople
      }.AsJsonResult();
    }

    private int NumberOfPeople
    {
      get { return new PersonCacher(Db).AllForThisElection.Count(); }
    }
  }
}